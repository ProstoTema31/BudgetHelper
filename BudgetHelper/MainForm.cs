using BudgetHelper.Comparer;
using BudgetHelper.Models;
using BudgetHelper.Parsers;
using BudgetHelper.Storage;
using OfficeOpenXml;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace BudgetHelper
{
    public partial class MainForm : Form
    {
        private string firstFilePath;
        private string secondFilePath;
        private ComparisonResult lastComparisonResult;

        // Имена JSON-файлов для временного хранения
        private readonly string Act1Json = Path.Combine(Path.GetTempPath(), "act1.json");
        private readonly string Act2Json = Path.Combine(Path.GetTempPath(), "act2.json");

        public MainForm()
        {
            InitializeComponent();

            btnSelectFirst.Click += BtnSelectFirst_Click;
            btnSelectSecond.Click += BtnSelectSecond_Click;
            btnCompare.Click += BtnCompare_Click;
            btnExport.Click += BtnExport_Click;
        }

        private void BtnSelectFirst_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog ofd = new OpenFileDialog())
            {
                ofd.Filter = "Допустимые файлы|*.xls;*.xlsx;*.pdf|Excel файлы|*.xls;*.xlsx|PDF файлы|*.pdf|Все файлы|*.*";
                ofd.Title = "Выберите первый акт";
                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    firstFilePath = ofd.FileName;
                    txtFirstFile.Text = Path.GetFileName(firstFilePath);
                    AppendColoredText($"[OK] Выбран первый файл: {Path.GetFileName(firstFilePath)}\n", Color.Green);
                }
            }
        }

        private void BtnSelectSecond_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog ofd = new OpenFileDialog())
            {
                ofd.Filter = "Допустимые файлы|*.xls;*.xlsx;*.pdf|Excel файлы|*.xls;*.xlsx|PDF файлы|*.pdf|Все файлы|*.*";
                ofd.Title = "Выберите второй акт";
                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    secondFilePath = ofd.FileName;
                    txtSecondFile.Text = Path.GetFileName(secondFilePath);
                    AppendColoredText($"[OK] Выбран второй файл: {Path.GetFileName(secondFilePath)}\n", Color.Green);
                }
            }
        }

        private async void BtnCompare_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(firstFilePath) || string.IsNullOrEmpty(secondFilePath))
            {
                MessageBox.Show("Выберите оба файла!", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (firstFilePath == secondFilePath && !firstFilePath.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
            {
                MessageBox.Show("Вы выбрали один и тот же Excel-файл для обоих актов!", "Внимание", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            btnCompare.Enabled = false;
            progressBar.Visible = true;
            txtResult.Clear();

            try
            {
                AppendColoredText("[СТАРТ] Начинаю сверку...\n\n", Color.Blue);
                JsonStorageHelper.ClearStorage(Act1Json, Act2Json);

                AppendColoredText("[1/3] Парсинг первого файла...\n", Color.DarkBlue);
                var (header1, ops1) = await ParseFileAsync(firstFilePath, isSecondFile: false);
                AppendColoredText($"      Загружено операций: {ops1.Count}, начальное сальдо: {header1.OpeningBalance:N2}, конечное: {header1.ClosingBalance:N2}\n", Color.Green);
                JsonStorageHelper.SaveAct(Act1Json, header1, ops1);

                AppendColoredText("[2/3] Парсинг второго файла...\n", Color.DarkBlue);
                var (header2, ops2) = await ParseFileAsync(secondFilePath, isSecondFile: true);
                AppendColoredText($"      Загружено операций: {ops2.Count}, начальное сальдо: {header2.OpeningBalance:N2}, конечное: {header2.ClosingBalance:N2}\n", Color.Green);
                JsonStorageHelper.SaveAct(Act2Json, header2, ops2);
                
                AppendColoredText("[3/3] Загрузка из JSON и сравнение...\n\n", Color.DarkBlue);

                var (loadedHeader1, loadedOps1) = JsonStorageHelper.LoadAct(Act1Json);
                var (loadedHeader2, loadedOps2) = JsonStorageHelper.LoadAct(Act2Json);

                var comparer = new ActsComparer();

                var result = await Task.Run(() => comparer.Compare(loadedOps1, loadedOps2,
                                              loadedHeader1.OpeningBalance, loadedHeader2.OpeningBalance,
                                              loadedHeader1.ClosingBalance, loadedHeader2.ClosingBalance));

                lastComparisonResult = result;
                DisplayResult(result);
            }
            catch (Exception ex)
            {
                AppendColoredText($"\n[ОШИБКА] {ex.Message}\n", Color.Red);
                AppendColoredText(ex.StackTrace + "\n", Color.DarkGray);
            }
            finally
            {
                btnCompare.Enabled = true;
                progressBar.Visible = false;
            }
        }

        private async Task<(DocumentHeader header, List<DocumentContent> operations)> ParseFileAsync(string path, bool isSecondFile)
        {
            return await Task.Run(() =>
            {
                if (path.EndsWith(".xls") || path.EndsWith(".xlsx"))
                    return ExcelParser.ParseExcel(path);
                else if (PdfParser.IsPdfFile(path))
                    return PdfParser.ParsePdf(path, extractCounterparty: isSecondFile);
                else
                    throw new NotSupportedException("Поддерживаются только Excel и PDF файлы");
            });
        }

        private void DisplayResult(ComparisonResult r)
        {
            AppendText(new string('=', 80) + "\n");
            AppendColoredText("РЕЗУЛЬТАТ СВЕРКИ\n", Color.Blue);
            AppendText(new string('=', 80) + "\n\n");

            AppendText($"Начальное сальдо: {r.OpeningBalanceFirst:N2} (файл 1) vs {r.OpeningBalanceSecond:N2} (файл 2)");
            if (r.OpeningBalancesMatch) AppendColoredText(" - СОВПАДАЕТ\n", Color.Green);
            else AppendColoredText(" - НЕ СОВПАДАЕТ\n", Color.Red);

            AppendText($"Конечное сальдо:   {r.ClosingBalanceFirst:N2} (файл 1) vs {r.ClosingBalanceSecond:N2} (файл 2)");
            if (r.ClosingBalancesMatch) AppendColoredText(" - СОВПАДАЕТ\n", Color.Green);
            else AppendColoredText(" - НЕ СОВПАДАЕТ\n", Color.Red);
            AppendText("\n");

            if (r.IsMatch)
            {
                AppendColoredText("[OK] АКТЫ СХОДЯТСЯ!\n", Color.Green);
                AppendText($"   Общая сумма операций (дебет-кредит): {r.ClosingBalanceFirst - r.OpeningBalanceFirst:N2} руб.\n");
                AppendText($"   Совпало операций: {r.MatchedOps} из {r.TotalFirstOps}\n");
                return;
            }

            AppendColoredText("[ОШИБКА] АКТЫ НЕ СХОДЯТСЯ!\n", Color.Red);
            AppendText($"   Расхождение по сумме операций: {r.TotalDifference:N2} руб.\n");
            AppendText($"   Операций в первом: {r.TotalFirstOps}, во втором: {r.TotalSecondOps}\n\n");

            if (r.Mismatches.Count > 0)
            {
                AppendColoredText($"РАСХОЖДЕНИЯ ({r.Mismatches.Count}):\n", Color.Orange);
                AppendText(new string('-', 80) + "\n");
                foreach (var m in r.Mismatches.Take(50))
                {
                    string line = $"   {m.Date,-12} {m.Amount,12:N2} руб.  {TruncateString(m.Description, 40)}";
                    if (!string.IsNullOrEmpty(m.Reason))
                        line += $"  [{m.Reason}]";
                    AppendColoredText(line + "\n", Color.Brown);
                }
                if (r.Mismatches.Count > 50)
                    AppendText($"   ... и еще {r.Mismatches.Count - 50}\n");
            }
        }

        private void AppendColoredText(string text, Color color)
        {
            if (txtResult.InvokeRequired)
            {
                txtResult.Invoke(new Action(() => AppendColoredText(text, color)));
                return;
            }
            txtResult.SelectionStart = txtResult.TextLength;
            txtResult.SelectionLength = 0;
            txtResult.SelectionColor = color;
            txtResult.AppendText(text);
            txtResult.SelectionColor = txtResult.ForeColor;
        }

        private void AppendText(string text)
        {
            if (txtResult.InvokeRequired)
            {
                txtResult.Invoke(new Action(() => AppendText(text)));
                return;
            }
            txtResult.AppendText(text);
        }

        private string TruncateString(string text, int maxLength)
        {
            if (string.IsNullOrEmpty(text)) return "";
            if (text.Length <= maxLength) return text;
            return text.Substring(0, maxLength - 3) + "...";
        }

        private void panel2_Resize(object sender, EventArgs e)
        {
            panel1.Left = (this.ClientSize.Width - panel1.Width) / 2;
        }

        private void BtnExport_Click(object sender, EventArgs e)
        {
            // Проверяем, была ли вообще выполнена сверка
            if (lastComparisonResult == null)
            {
                MessageBox.Show("Сначала выполните сверку актов!", "Предупреждение", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            using (SaveFileDialog sfd = new SaveFileDialog())
            {
                sfd.Filter = "Excel файлы (*.xlsx)|*.xlsx";
                sfd.FileName = $"Протокол_сверки_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
                sfd.Title = "Сохранить протокол сверки";

                if (sfd.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        ExportToExcel(sfd.FileName, lastComparisonResult);
                        AppendColoredText($"\n[УСПЕХ] Протокол успешно сохранен в файл: {Path.GetFileName(sfd.FileName)}\n", Color.Green);
                        MessageBox.Show("Протокол успешно экспортирован!", "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Ошибка при экспорте: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        AppendColoredText($"\n[ОШИБКА ЭКСПОРТА] {ex.Message}\n", Color.Red);
                    }
                }
            }
        }

        private void ExportToExcel(string filePath, ComparisonResult r)
        {

            using (var package = new ExcelPackage())
            {
                // Создаем лист
                var ws = package.Workbook.Worksheets.Add("Проверка операций");

                // Исправление для корректного отображения сетки во всех версиях
                ws.View.ShowGridLines = true;

                // 1. Главный заголовок документа
                ws.Cells["A1"].Value = "ПРОТОКОЛ СВЕРКИ ОПЕРАЦИЙ";
                ws.Cells["A1"].Style.Font.Size = 16;
                ws.Cells["A1"].Style.Font.Bold = true;
                ws.Cells["A1:E1"].Merge = true;

                // 2. Шапка блока Сальдо
                ws.Cells["A3"].Value = "Параметр";
                ws.Cells["B3"].Value = "Акт 1 (Наша компания)";
                ws.Cells["C3"].Value = "Акт 2 (Контрагент)";
                ws.Cells["D3"].Value = "Расхождение";
                ws.Cells["E3"].Value = "Статус";

                using (var range = ws.Cells["A3:E3"])
                {
                    range.Style.Font.Bold = true;
                    range.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                    range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.FromArgb(235, 235, 235)); // Мягкий серый цвет
                    range.Style.Border.Bottom.Style = OfficeOpenXml.Style.ExcelBorderStyle.Thin;
                }

                // Данные Начального сальдо
                ws.Cells["A4"].Value = "Начальное сальдо";
                ws.Cells["B4"].Value = r.OpeningBalanceFirst;
                ws.Cells["C4"].Value = r.OpeningBalanceSecond;
                ws.Cells["D4"].Value = Math.Abs(r.OpeningBalanceFirst - r.OpeningBalanceSecond);
                ws.Cells["E4"].Value = r.OpeningBalancesMatch ? "СОВПАДАЕТ" : "НЕ СОВПАДАЕТ";

                // Данные Конечного сальдо
                ws.Cells["A5"].Value = "Конечное сальдо";
                ws.Cells["B5"].Value = r.ClosingBalanceFirst;
                ws.Cells["C5"].Value = r.ClosingBalanceSecond;
                ws.Cells["D5"].Value = Math.Abs(r.ClosingBalanceFirst - r.ClosingBalanceSecond);
                ws.Cells["E5"].Value = r.ClosingBalancesMatch ? "СОВПАДАЕТ" : "НЕ СОВПАДАЕТ";

                // Применяем денежный формат чисел для сумм сальдо
                ws.Cells["B4:D5"].Style.Numberformat.Format = "#,##0.00";

                // Интеллектуальная подсветка статусов сальдо
                for (int row = 4; row <= 5; row++)
                {
                    var statusCell = ws.Cells[row, 5];
                    statusCell.Style.Font.Bold = true;
                    statusCell.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;

                    if (statusCell.Text == "СОВПАДАЕТ")
                        statusCell.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.FromArgb(226, 239, 218)); // Мягкий зеленый
                    else
                        statusCell.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.FromArgb(252, 228, 214)); // Мягкий красный
                }

                // Сводная статистика
                ws.Cells["A7"].Value = "Всего операций в Акте 1:"; ws.Cells["B7"].Value = r.TotalFirstOps;
                ws.Cells["A8"].Value = "Всего операций в Акте 2:"; ws.Cells["B8"].Value = r.TotalSecondOps;
                ws.Cells["A9"].Value = "Совпало операций:"; ws.Cells["B9"].Value = r.MatchedOps;
                ws.Cells["A10"].Value = "Сумма расхождения по оборотам:"; ws.Cells["B10"].Value = r.TotalDifference;
                ws.Cells["B10"].Style.Numberformat.Format = "#,##0.00";
                ws.Cells["A7:A10"].Style.Font.Italic = true;

                // 3. Таблица расхождений
                ws.Cells["A12"].Value = "Таблица выявленных расхождений";
                ws.Cells["A12"].Style.Font.Size = 13;
                ws.Cells["A12"].Style.Font.Bold = true;

                ws.Cells["A13"].Value = "Дата";
                ws.Cells["B13"].Value = "Сумма операции";
                ws.Cells["C13"].Value = "Описание (содержание операции)";
                ws.Cells["D13"].Value = "Причина расхождения";
                ws.Cells["E13"].Value = "Строка в исходнике";

                using (var range = ws.Cells["A13:E13"])
                {
                    range.Style.Font.Bold = true;
                    range.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                    range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.FromArgb(218, 227, 243)); // Приятный бухгалтерский синий оттенок
                    range.Style.Border.Bottom.Style = OfficeOpenXml.Style.ExcelBorderStyle.Medium;
                }

                int currentRow = 14;

                if (r.Mismatches.Count > 0)
                {
                    foreach (var m in r.Mismatches)
                    {
                        ws.Cells[currentRow, 1].Value = m.Date;
                        ws.Cells[currentRow, 2].Value = Math.Abs(m.Amount); // Выводим сумму по модулю
                        ws.Cells[currentRow, 3].Value = m.Description;
                        ws.Cells[currentRow, 4].Value = m.Reason;
                        ws.Cells[currentRow, 5].Value = m.RowIndex == 0 ? "-" : m.RowIndex.ToString();

                        // Форматируем сумму
                        ws.Cells[currentRow, 2].Style.Numberformat.Format = "#,##0.00";

                        // Подсвечиваем ячейку причины в зависимости от типа ошибки
                        var reasonCell = ws.Cells[currentRow, 4];
                        reasonCell.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;

                        if (m.Reason.Contains("Только"))
                        {
                            reasonCell.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.FromArgb(255, 242, 204)); // Мягкий желтый (отсутствие)
                        }
                        else
                        {
                            reasonCell.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.FromArgb(252, 228, 214)); // Мягкий оранжевый (сильное расхождение дат)
                        }

                        currentRow++;
                    }

                    // Накладываем тонкие границы на всю таблицу расхождений
                    using (var range = ws.Cells[13, 1, currentRow - 1, 5])
                    {
                        range.Style.Border.Top.Style = OfficeOpenXml.Style.ExcelBorderStyle.Thin;
                        range.Style.Border.Bottom.Style = OfficeOpenXml.Style.ExcelBorderStyle.Thin;
                        range.Style.Border.Left.Style = OfficeOpenXml.Style.ExcelBorderStyle.Thin;
                        range.Style.Border.Right.Style = OfficeOpenXml.Style.ExcelBorderStyle.Thin;
                    }
                }
                else
                {
                    // Если расхождений нет вообще
                    ws.Cells["A14"].Value = "Расхождений не обнаружено! Все операции сходятся идеально.";
                    ws.Cells["A14:E14"].Merge = true;
                    ws.Cells["A14"].Style.Font.Italic = true;
                    ws.Cells["A14"].Style.Font.Color.SetColor(System.Drawing.Color.Green);
                }

                // Автоподбор ширины для всех колонок
                ws.Cells[ws.Dimension.Address].AutoFitColumns();

                // Корректируем ширину колонок под длинные тексты, чтобы отчет не растягивался до бесконечности
                ws.Column(3).Width = 50; // Колонка описания
                ws.Column(4).Width = 28; // Колонка причины

                // Сохраняем файл на диск
                package.SaveAs(new FileInfo(filePath));
            }
        }
    }
}