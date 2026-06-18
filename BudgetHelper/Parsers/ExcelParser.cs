using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using OfficeOpenXml;
using BudgetHelper.Models;

namespace BudgetHelper.Parsers
{
    public class ExcelParser
    {
        public static (DocumentHeader header, List<DocumentContent> operations) ParseExcel(string filePath)
        {
            using (var package = new ExcelPackage(new FileInfo(filePath)))
            {
                var worksheet = package.Workbook.Worksheets[1];
                if (worksheet.Dimension == null)
                    throw new Exception("Лист пуст");

                // Определяем тип файла по первым 20 строкам
                bool isFirstFile = IsFirstFile(worksheet);
                var (startRow, dateCol, docCol, numCol, debetCol, creditCol) = GetColumnMapping(worksheet, isFirstFile);

                var header = new DocumentHeader
                {
                    DocumentNumber = Path.GetFileNameWithoutExtension(filePath),
                    DocumentDate = DateTime.Now
                };

                var operations = new List<DocumentContent>();
                decimal opening = 0, closing = 0;
                bool foundOpening = false;

                for (int row = startRow; row <= worksheet.Dimension.End.Row; row++)
                {
                    // Склеиваем первые три колонки для надежного поиска ключевых слов
                    string col1 = GetCellText(worksheet, row, 1);
                    string col2 = GetCellText(worksheet, row, 2);
                    string col3 = GetCellText(worksheet, row, 3);

                    string rowStartText = $"{col1} {col2} {col3}".Trim().ToLower().Replace("\r", " ").Replace("\n", " ");

                    // Проверка на промежуточные итоги (пропускаем их, но продолжаем искать конечное сальдо)
                    if (rowStartText.Contains("итого") || rowStartText.Contains("обороты"))
                    {
                        continue;
                    }

                    // Обработка строк сальдо
                    if (rowStartText.Contains("сальдо"))
                    {
                        decimal saldo = 0;
                        // Сумма сальдо может 'гулять' по колонкам (Дебет, Кредит или отдельная). 
                        // Надежнее просканировать ячейки с 4 по 8 и взять первое ненулевое число.
                        for (int c = 4; c <= 8; c++)
                        {
                            saldo = ParseDecimal(GetCellText(worksheet, row, c));
                            if (saldo != 0) break;
                        }

                        if (rowStartText.Contains("начальное") || rowStartText.Contains("01.01") || !foundOpening)
                        {
                            opening = saldo;
                            foundOpening = true;
                        }
                        else if (rowStartText.Contains("конечное") || rowStartText.Contains("31.03"))
                        {
                            closing = saldo;
                            break; // Конечное сальдо найдено, парсинг таблицы можно завершать
                        }
                        continue;
                    }

                    string dateStr = dateCol != -1 ? GetCellText(worksheet, row, dateCol) : "";
                    string document = docCol != -1 ? GetCellText(worksheet, row, docCol) : "";
                    string number = numCol != -1 ? GetCellText(worksheet, row, numCol) : "";
                    string debetStr = GetCellText(worksheet, row, debetCol);
                    string creditStr = GetCellText(worksheet, row, creditCol);

                    // Очистка текста документа от скрытых переносов строк из Excel
                    document = document.Replace("\r", " ").Replace("\n", " ").Trim();
                    number = number.Replace("\r", " ").Replace("\n", " ").Trim();

                    // Пропускаем строки без даты (если колонка даты ожидается)
                    if (dateCol != -1 && string.IsNullOrWhiteSpace(dateStr))
                        continue;

                    decimal debet = ParseDecimal(debetStr);
                    decimal credit = ParseDecimal(creditStr);
                    decimal amount = debet - credit;

                    // Если и дебет, и кредит нулевые, а документа нет – пропускаем (пустая строка)
                    if (debet == 0 && credit == 0 && string.IsNullOrWhiteSpace(document))
                        continue;

                    string fullDesc = document;
                    if (!string.IsNullOrWhiteSpace(number))
                        fullDesc += " " + number;

                    operations.Add(new DocumentContent
                    {
                        OperationDate = dateStr,
                        OperationDescription = fullDesc.Trim(),
                        DocumentNumber = number,
                        Amount = amount,
                        RowIndex = row
                    });
                }

                header.OpeningBalance = opening;
                header.ClosingBalance = closing;

                return (header, operations);
            }
        }

        private static bool IsFirstFile(ExcelWorksheet sheet)
        {
            for (int row = 1; row <= 20; row++)
            {
                // Добавлена очистка от переносов строк
                string text = (GetCellText(sheet, row, 1) + GetCellText(sheet, row, 2)).Replace("\r", " ").Replace("\n", " ");
                if (text.Contains("По данным 1 Компании"))
                    return true;
                if (text.Contains("По данным 2Компанией"))
                    return false;
            }
            return true;
        }

        private static (int startRow, int dateCol, int docCol, int numCol, int debetCol, int creditCol) GetColumnMapping(ExcelWorksheet sheet, bool isFirstFile)
        {
            int headerRow = FindHeaderRow(sheet, isFirstFile);
            if (headerRow == -1)
                throw new Exception("Не найдена строка заголовков");

            if (isFirstFile)
            {
                // Файл 1Акт: дата в B(2), документ в C(3), номер нет(-1), дебет в E(5), кредит в G(7)
                return (headerRow + 1, 2, 3, -1, 5, 7);
            }
            else
            {
                // Файл 2Акт: первая колонка пустая!
                // дата в B(2), документ в C(3), номер в D(4), дебет в E(5), кредит в F(6)
                return (headerRow + 1, 2, 3, 4, 5, 6);
            }
        }

        private static int FindHeaderRow(ExcelWorksheet sheet, bool isFirstFile)
        {
            int maxRow = Math.Min(sheet.Dimension.End.Row, 50);
            for (int row = 1; row <= maxRow; row++)
            {
                string rowText = "";
                for (int col = 1; col <= 10; col++)
                    rowText += GetCellText(sheet, row, col) + " ";

                if (rowText.Contains("Документ") && (rowText.Contains("Дебет") || rowText.Contains("Кредит")))
                    return row;
            }
            return -1;
        }

        private static string GetCellText(ExcelWorksheet sheet, int row, int col)
        {
            try
            {
                if (sheet.Dimension == null) return "";
                if (row > sheet.Dimension.End.Row || col > sheet.Dimension.End.Column) return "";
                var cell = sheet.Cells[row, col];
                return cell?.Value?.ToString().Trim() ?? "";
            }
            catch { return ""; }
        }

        private static decimal ParseDecimal(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return 0;

            value = value.Replace(",", ".").Replace(" ", "");
            value = Regex.Replace(value, @"[^0-9.\-]", "");

            if (decimal.TryParse(value, System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out decimal res))
                return res;

            return 0;
        }

        private static string DetectContractor(ExcelWorksheet sheet)
        {
            for (int row = 1; row <= 10; row++)
            {
                // Добавлена очистка от переносов строк, чтобы регулярное выражение не ломалось
                string text = (GetCellText(sheet, row, 1) + " " + GetCellText(sheet, row, 2)).Replace("\r", " ").Replace("\n", " ");
                var match = Regex.Match(text, @"Между\s+(.+?)\s+и\s+(.+)");

                if (match.Success)
                    return match.Groups[1].Value + " / " + match.Groups[2].Value;
            }
            return "";
        }
    }
}