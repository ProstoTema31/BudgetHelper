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

            var firstNorm = firstReal.Select(op => new
            {
                Original = op,
                NormDesc = NormalizeDescription(op.OperationDescription + " " + op.DocumentNumber),
                Date = op.OperationDate,
                Amount = op.Amount
            }).ToList();

            var secondNorm = secondReal.Select(op => new
            {
                Original = op,
                NormDesc = NormalizeDescription(op.OperationDescription + " " + op.DocumentNumber),
                Date = op.OperationDate,
                Amount = op.Amount
            }).ToList();

            bool[] matchedFirst = new bool[firstNorm.Count];
            bool[] matchedSecond = new bool[secondNorm.Count];
            var mismatches = new List<OperationMismatch>();

            for (int i = 0; i < firstNorm.Count; i++)
            {
                var f = firstNorm[i];
                bool found = false;

                for (int j = 0; j < secondNorm.Count; j++)
                {
                    if (matchedSecond[j]) continue;
                    var s = secondNorm[j];
                    if (f.Date == s.Date && f.NormDesc == s.NormDesc && Math.Abs(f.Amount - s.Amount) <= 0.01m)
                    {
                        matchedFirst[i] = true;
                        matchedSecond[j] = true;
                        found = true;
                        break;
                    }
                }
                if (found) continue;

                for (int j = 0; j < secondNorm.Count; j++)
                {
                    if (matchedSecond[j]) continue;
                    var s = secondNorm[j];
                    if (f.Date == s.Date && Math.Abs(f.Amount - s.Amount) <= 0.01m)
                    {
                        matchedFirst[i] = true;
                        matchedSecond[j] = true;
                        mismatches.Add(new OperationMismatch
                        {
                            Date = f.Date,
                            Description = f.Original.OperationDescription,
                            Amount = f.Amount,
                            ExpectedAmount = s.Amount,
                            Reason = "Описание не совпадает",
                            RowIndex = f.Original.RowIndex
                        });
                        found = true;
                        break;
                    }
                }
                if (found) continue;

                // По сумме
                for (int j = 0; j < secondNorm.Count; j++)
                {
                    if (matchedSecond[j]) continue;
                    var s = secondNorm[j];
                    if (Math.Abs(f.Amount - s.Amount) <= 0.01m)
                    {
                        matchedFirst[i] = true;
                        matchedSecond[j] = true;
                        mismatches.Add(new OperationMismatch
                        {
                            Date = f.Date,
                            Description = f.Original.OperationDescription,
                            Amount = f.Amount,
                            ExpectedAmount = s.Amount,
                            Reason = "Дата не совпадает",
                            RowIndex = f.Original.RowIndex
                        });
                        found = true;
                        break;
                    }
                }
                if (!found)
                {
                    mismatches.Add(new OperationMismatch
                    {
                        Date = f.Date,
                        Description = f.Original.OperationDescription,
                        Amount = f.Amount,
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
                        Date = s.Date,
                        Description = s.Original.OperationDescription,
                        Amount = s.Amount,
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

        private string NormalizeDescription(string desc)
        {
            if (string.IsNullOrEmpty(desc)) return "";
            desc = desc.ToLowerInvariant();
            desc = Regex.Replace(desc, @"\b(б-)?\d{6,}\b", "");
            desc = Regex.Replace(desc, @"\b\d{2,6}\b", "");
            desc = Regex.Replace(desc, @"\s+", " ").Trim();
            return desc;
        }
    }
}