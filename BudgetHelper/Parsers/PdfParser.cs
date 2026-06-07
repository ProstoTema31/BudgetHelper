using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using UglyToad.PdfPig;
using BudgetHelper.Models;

namespace BudgetHelper.Parsers
{
    public class PdfParser
    {
        public static (DocumentHeader header, List<DocumentContent> contents) ParsePdf(string filePath)
        {
            var header = new DocumentHeader
            {
                DocumentNumber = Path.GetFileNameWithoutExtension(filePath),
                DocumentDate = DateTime.Now,
                TotalAmount = 0
            };

            var contents = new List<DocumentContent>();
            var allText = new StringBuilder();

            using (var pdf = PdfDocument.Open(filePath))
            {
                foreach (var page in pdf.GetPages())
                {
                    allText.Append(page.Text);
                    allText.Append(" ");
                }
            }

            string text = allText.ToString();

            bool isFirstFile = Path.GetFileName(filePath).Contains("1Акт") ||
                               text.Contains("1 Компания");

            var amountPattern = new Regex(@"\s(\d{1,3}(?:[\s]?\d{3})*(?:[.,]\d{2})?)\s");
            var amounts = amountPattern.Matches(text);

            var datePattern = new Regex(@"(\d{2}\.\d{2}\.\d{2,4})");
            var dates = datePattern.Matches(text);

            if (dates.Count > 0 && amounts.Count > 0)
            {
                int count = Math.Min(dates.Count, amounts.Count);

                for (int i = 0; i < count; i++)
                {
                    string date = dates[i].Groups[1].Value;
                    decimal amount = ParseDecimal(amounts[i].Groups[1].Value);

                    if (amount > 0 && amount < 10000000 && date.Length >= 8)
                    {
                        contents.Add(new DocumentContent
                        {
                            OperationDate = date,
                            OperationDescription = "Операция",
                            Amount = amount,
                            RowIndex = i
                        });
                        header.TotalAmount += amount;
                    }
                }
            }

            if (contents.Count == 0)
            {
                var lines = text.Split(new[] { '\n', '\r', ' ' }, StringSplitOptions.RemoveEmptyEntries);

                foreach (var line in lines)
                {
                    var dateMatch = Regex.Match(line, @"^(\d{2}\.\d{2}\.\d{2,4})");
                    if (dateMatch.Success)
                    {
                        string date = dateMatch.Groups[1].Value;

                        var numMatch = Regex.Match(line, @"(\d{1,3}(?:[\s]?\d{3})*(?:[.,]\d{2})?)$");
                        if (numMatch.Success)
                        {
                            decimal amount = ParseDecimal(numMatch.Groups[1].Value);
                            if (amount > 0 && amount < 10000000)
                            {
                                contents.Add(new DocumentContent
                                {
                                    OperationDate = date,
                                    OperationDescription = "Операция",
                                    Amount = amount,
                                    RowIndex = 0
                                });
                                header.TotalAmount += amount;
                            }
                        }
                    }
                }
            }

            return (header, contents);
        }

        private static decimal ParseDecimal(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return 0;

            try
            {
                value = value.Replace(" ", "").Replace("\t", "").Replace("\n", "").Replace("\r", "");
                value = value.Replace(",", ".");

                if (decimal.TryParse(value, System.Globalization.NumberStyles.Any,
                    System.Globalization.CultureInfo.InvariantCulture, out decimal result))
                {
                    return result;
                }
            }
            catch { }

            return 0;
        }

        public static bool IsPdfFile(string filePath)
        {
            return Path.GetExtension(filePath).ToLower() == ".pdf";
        }
    }
}