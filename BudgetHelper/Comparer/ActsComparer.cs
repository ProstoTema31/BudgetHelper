using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using BudgetHelper.Models;

namespace BudgetHelper.Comparer
{
    public class OperationMismatch
    {
        public string Date { get; set; }
        public string Description { get; set; }
        public decimal Amount { get; set; }
        public string Reason { get; set; }
        public decimal ExpectedAmount { get; set; }
        public int RowIndex { get; set; }
    }

    public class ComparisonResult
    {
        public bool IsMatch { get; set; }
        public decimal OpeningBalanceFirst { get; set; }
        public decimal OpeningBalanceSecond { get; set; }
        public decimal ClosingBalanceFirst { get; set; }
        public decimal ClosingBalanceSecond { get; set; }
        public decimal TotalDifference { get; set; }
        public List<OperationMismatch> Mismatches { get; set; } = new List<OperationMismatch>();
        public int TotalFirstOps { get; set; }
        public int TotalSecondOps { get; set; }
        public int MatchedOps { get; set; }
        public bool OpeningBalancesMatch => Math.Abs(OpeningBalanceFirst - OpeningBalanceSecond) <= 0.01m;
        public bool ClosingBalancesMatch => Math.Abs(ClosingBalanceFirst - ClosingBalanceSecond) <= 0.01m;
    }

    public class ActsComparer
    {
        private class NormOp
        {
            public DocumentContent Original { get; set; }
            public string NormDesc { get; set; }
            public string DateStr { get; set; }
            public DateTime? ParsedDate { get; set; }
            public decimal Amount { get; set; }
        }

        private const int DateToleranceDays = 5;

        public ComparisonResult Compare(List<DocumentContent> firstOps, List<DocumentContent> secondOps,
                                        decimal firstOpening, decimal secondOpening,
                                        decimal firstClosing, decimal secondClosing)
        {
            var result = new ComparisonResult
            {
                OpeningBalanceFirst = firstOpening,
                OpeningBalanceSecond = secondOpening,
                ClosingBalanceFirst = firstClosing,
                ClosingBalanceSecond = secondClosing,
                TotalFirstOps = firstOps.Count(x => !x.IsOpeningBalance && !x.IsClosingBalance),
                TotalSecondOps = secondOps.Count(x => !x.IsOpeningBalance && !x.IsClosingBalance)
            };

            var firstReal = firstOps.Where(x => !x.IsOpeningBalance && !x.IsClosingBalance).ToList();
            var secondReal = secondOps.Where(x => !x.IsOpeningBalance && !x.IsClosingBalance).ToList();

            var firstNorm = firstReal.Select(op => new NormOp
            {
                Original = op,
                NormDesc = NormalizeDescription(op.OperationDescription + " " + op.DocumentNumber),
                DateStr = op.OperationDate,
                ParsedDate = ParseDate(op.OperationDate),
                Amount = op.Amount
            }).ToList();

            var secondNorm = secondReal.Select(op => new NormOp
            {
                Original = op,
                NormDesc = NormalizeDescription(op.OperationDescription + " " + op.DocumentNumber),
                DateStr = op.OperationDate,
                ParsedDate = ParseDate(op.OperationDate),
                Amount = op.Amount
            }).ToList();

            bool[] matchedFirst = new bool[firstNorm.Count];
            bool[] matchedSecond = new bool[secondNorm.Count];
            var mismatches = new List<OperationMismatch>();

            bool IsAmountMatch(NormOp f, NormOp s) => Math.Abs(Math.Abs(f.Amount) - Math.Abs(s.Amount)) <= 0.01m;

            MatchOperations(firstNorm, secondNorm, matchedFirst, matchedSecond, (f, s) =>
                IsAmountMatch(f, s) &&
                f.ParsedDate.HasValue && s.ParsedDate.HasValue &&
                f.ParsedDate.Value.Date == s.ParsedDate.Value.Date &&
                f.NormDesc == s.NormDesc);

            MatchOperations(firstNorm, secondNorm, matchedFirst, matchedSecond, (f, s) =>
                IsAmountMatch(f, s) &&
                f.ParsedDate.HasValue && s.ParsedDate.HasValue &&
                f.ParsedDate.Value.Date == s.ParsedDate.Value.Date);

            MatchOperations(firstNorm, secondNorm, matchedFirst, matchedSecond, (f, s) =>
            {
                if (!IsAmountMatch(f, s)) return false;
                if (!f.ParsedDate.HasValue || !s.ParsedDate.HasValue) return false;
                return Math.Abs((f.ParsedDate.Value - s.ParsedDate.Value).TotalDays) <= DateToleranceDays;
            });

            for (int i = 0; i < firstNorm.Count; i++)
            {
                if (matchedFirst[i]) continue;
                var f = firstNorm[i];

                bool foundByAmount = false;
                for (int j = 0; j < secondNorm.Count; j++)
                {
                    if (matchedSecond[j]) continue;
                    var s = secondNorm[j];

                    if (IsAmountMatch(f, s))
                    {
                        matchedFirst[i] = true;
                        matchedSecond[j] = true;
                        mismatches.Add(new OperationMismatch
                        {
                            Date = f.DateStr,
                            Description = f.Original.OperationDescription,
                            Amount = f.Amount,
                            ExpectedAmount = s.Amount,
                            Reason = "Дата сильно расходится",
                            RowIndex = f.Original.RowIndex
                        });
                        foundByAmount = true;
                        break;
                    }
                }

                if (!foundByAmount)
                {
                    mismatches.Add(new OperationMismatch
                    {
                        Date = f.DateStr,
                        Description = f.Original.OperationDescription,
                        Amount = f.Amount,
                        ExpectedAmount = 0,
                        Reason = "Только в первом акте",
                        RowIndex = f.Original.RowIndex
                    });
                }
            }

            for (int j = 0; j < secondNorm.Count; j++)
            {
                if (!matchedSecond[j])
                {
                    var s = secondNorm[j];
                    mismatches.Add(new OperationMismatch
                    {
                        Date = s.DateStr,
                        Description = s.Original.OperationDescription,
                        Amount = s.Amount,
                        ExpectedAmount = 0,
                        Reason = "Только во втором акте",
                        RowIndex = s.Original.RowIndex
                    });
                }
            }

            result.MatchedOps = matchedFirst.Count(x => x);
            result.Mismatches = mismatches;
            result.TotalDifference = Math.Abs(firstReal.Sum(x => x.Amount) - secondReal.Sum(x => x.Amount));
            result.IsMatch = mismatches.Count == 0 && result.OpeningBalancesMatch && result.ClosingBalancesMatch;

            return result;
        }

        private void MatchOperations(List<NormOp> first, List<NormOp> second, bool[] matchedFirst, bool[] matchedSecond, Func<NormOp, NormOp, bool> condition)
        {
            for (int i = 0; i < first.Count; i++)
            {
                if (matchedFirst[i]) continue;

                for (int j = 0; j < second.Count; j++)
                {
                    if (matchedSecond[j]) continue;

                    if (condition(first[i], second[j]))
                    {
                        matchedFirst[i] = true;
                        matchedSecond[j] = true;
                        break;
                    }
                }
            }
        }

        private string NormalizeDescription(string desc)
        {
            if (string.IsNullOrEmpty(desc)) return "";
            desc = desc.ToLowerInvariant();
            desc = Regex.Replace(desc, @"\b(б-)?\d{6,}\b", "");
            desc = Regex.Replace(desc, @"\b\d{2,6}\b", "");
            desc = Regex.Replace(desc, @"\s+", " ").Trim();
            return desc;
        }

        private DateTime? ParseDate(string dateStr)
        {
            if (string.IsNullOrWhiteSpace(dateStr)) return null;

            string[] formats = { "dd.MM.yyyy", "dd.MM.yy", "d.M.yyyy", "d.M.yy" };
            if (DateTime.TryParseExact(dateStr.Trim(), formats, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out DateTime dt))
            {
                return dt;
            }

            if (DateTime.TryParse(dateStr.Trim(), out DateTime dt2))
                return dt2;

            return null;
        }
    }
}