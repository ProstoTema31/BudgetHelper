using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;
using BudgetHelper.Models;

namespace BudgetHelper.Parsers
{
    public class PdfParser
    {
        public static (DocumentHeader header, List<DocumentContent> contents) ParsePdf(string filePath, bool extractCounterparty = false)
        {
            var header = new DocumentHeader
            {
                DocumentNumber = Path.GetFileNameWithoutExtension(filePath),
                DocumentDate = DateTime.Now
            };

            var contents = new List<DocumentContent>();
            decimal opening = 0, closing = 0;

            using (var pdf = PdfDocument.Open(filePath))
            {
                foreach (var page in pdf.GetPages())
                {
                    var words = page.GetWords().ToList();
                    if (words.Count == 0) continue;

                    string fullPageText = string.Join(" ", words.Select(w => w.Text));

                    int colHeadersCount = Regex.Matches(fullPageText, @"Дебет|Кредит", RegexOptions.IgnoreCase).Count;
                    bool isTwoSided = colHeadersCount >= 3;

                    var targetWords = words;
                    if (isTwoSided)
                    {
                        double midX = page.Width / 2;
                        if (!extractCounterparty)
                        {
                            targetWords = words.Where(w => w.BoundingBox.Left < (midX + 20)).ToList();
                        }
                        else
                        {
                            targetWords = words.Where(w => w.BoundingBox.Right > (midX - 20)).ToList();
                        }
                    }

                    var lines = new List<List<Word>>();
                    foreach (var word in targetWords.OrderByDescending(w => w.BoundingBox.Bottom))
                    {
                        var line = lines.FirstOrDefault(l => Math.Abs(l.First().BoundingBox.Bottom - word.BoundingBox.Bottom) < 4);
                        if (line == null) lines.Add(new List<Word> { word });
                        else line.Add(word);
                    }

                    DocumentContent currentOp = null;
                    bool isLookingForOpening = false;
                    bool isLookingForClosing = false;

                    foreach (var lineWords in lines)
                    {
                        var sortedWords = lineWords.OrderBy(w => w.BoundingBox.Left).ToList();
                        string lineText = string.Join(" ", sortedWords.Select(w => w.Text)).ToLowerInvariant();

                        if (lineText.Contains("сальдо"))
                        {
                            if (currentOp != null && currentOp.Amount != 0)
                            {
                                contents.Add(currentOp);
                                currentOp = null;
                            }

                            var amounts = ExtractAmounts(lineText);
                            if (lineText.Contains("начальное") || lineText.Contains("01.01"))
                            {
                                if (amounts.Count > 0) opening = amounts.Last();
                                else isLookingForOpening = true;
                            }
                            else if (lineText.Contains("конечное") || lineText.Contains("31.") || lineText.Contains("30."))
                            {
                                if (amounts.Count > 0) closing = amounts.Last();
                                else isLookingForClosing = true;
                            }
                            continue;
                        }

                        if (isLookingForOpening)
                        {
                            var amounts = ExtractAmounts(lineText);
                            if (amounts.Count > 0) { opening = amounts.Last(); isLookingForOpening = false; continue; }
                        }
                        if (isLookingForClosing)
                        {
                            var amounts = ExtractAmounts(lineText);
                            if (amounts.Count > 0) { closing = amounts.Last(); isLookingForClosing = false; continue; }
                        }

                        if (lineText.Contains("обороты") || lineText.Contains("итого")) continue;

                        var dateMatch = Regex.Match(lineText, @"\b(\d{2}\.\d{2}\.\d{2,4})\b");

                        if (dateMatch.Success)
                        {
                            if (currentOp != null && currentOp.Amount != 0)
                            {
                                contents.Add(currentOp);
                            }

                            currentOp = new DocumentContent
                            {
                                OperationDate = dateMatch.Groups[1].Value,
                                OperationDescription = lineText,
                                Amount = 0
                            };
                        }
                        else if (currentOp != null)
                        {
                            currentOp.OperationDescription += " " + lineText;
                        }

                        if (currentOp != null)
                        {
                            var amounts = ExtractAmounts(lineText);
                            if (amounts.Count > 0 && currentOp.Amount == 0)
                            {
                                currentOp.Amount = amounts.First();
                            }
                        }
                    }

                    if (currentOp != null && currentOp.Amount != 0)
                    {
                        contents.Add(currentOp);
                    }
                }
            }

            foreach (var op in contents)
            {
                string desc = op.OperationDescription;
                desc = Regex.Replace(desc, @"\b\d{2}\.\d{2}\.\d{2,4}\b", "");
                desc = Regex.Replace(desc, @"(?<!\d)\d{1,3}(?:[\s\u00A0\u202F]*\d{3})*(?:[.,]\d{2})(?!\d)", "");
                op.OperationDescription = Regex.Replace(desc, @"\s+", " ").Trim();

                if (string.IsNullOrWhiteSpace(op.OperationDescription))
                    op.OperationDescription = "Операция";
            }

            for (int i = 0; i < contents.Count; i++) contents[i].RowIndex = i + 1;

            header.OpeningBalance = opening;
            header.ClosingBalance = closing;

            return (header, contents);
        }

        private static List<decimal> ExtractAmounts(string text)
        {
            var results = new List<decimal>();
            var matches = Regex.Matches(text, @"(?<!\d)\d{1,3}(?:[\s\u00A0\u202F]*\d{3})*(?:[.,]\d{2})(?!\d)");
            foreach (Match match in matches)
            {
                decimal val = ParseDecimal(match.Value);
                if (val != 0) results.Add(val);
            }
            return results;
        }

        private static decimal ParseDecimal(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return 0;
            value = Regex.Replace(value, @"\s+", "");
            value = value.Replace("\u00A0", "").Replace("\u202F", "");
            value = value.Replace(",", ".");

            if (decimal.TryParse(value, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out decimal result))
            {
                return result;
            }
            return 0;
        }

        public static bool IsPdfFile(string filePath)
        {
            return Path.GetExtension(filePath).ToLower() == ".pdf";
        }
    }
}