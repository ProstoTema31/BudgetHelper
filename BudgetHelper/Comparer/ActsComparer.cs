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
        public DateTime? ParsedDate { get; set; } // Скрытое поле для правильной хронологической сортировки
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
        // Внутренний класс теперь сам знает, нашел ли он пару
        private class NormOp
        {
            public DocumentContent Original { get; set; }
            public string NormDesc { get; set; }
            public string DateStr { get; set; }
            public DateTime? ParsedDate { get; set; }
            public decimal Amount { get; set; }
            public bool IsMatched { get; set; } = false;
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
                TotalFirstOps = firstOps.Count,
                TotalSecondOps = secondOps.Count
            };

            var firstNorm = firstOps.Select(op => new NormOp
            {
                Original = op,
                NormDesc = NormalizeDescription(op.OperationDescription + " " + op.DocumentNumber),
                DateStr = op.OperationDate,
                ParsedDate = ParseDate(op.OperationDate),
                Amount = op.Amount
            }).ToList();

            var secondNorm = secondOps.Select(op => new NormOp
            {
                Original = op,
                NormDesc = NormalizeDescription(op.OperationDescription + " " + op.DocumentNumber),
                DateStr = op.OperationDate,
                ParsedDate = ParseDate(op.OperationDate),
                Amount = op.Amount
            }).ToList();

            var mismatches = new List<OperationMismatch>();

            // ОПТИМИЗАЦИЯ: Разделяем операции по "корзинам" на основе суммы
            var lookup1 = firstNorm.ToLookup(x => Math.Round(Math.Abs(x.Amount), 2));
            var lookup2 = secondNorm.ToLookup(x => Math.Round(Math.Abs(x.Amount), 2));

            // Локальная функция для проходов только ВНУТРИ одной суммовой группы (работает мгновенно)
            void MatchLocal(List<NormOp> l1, List<NormOp> l2, Func<NormOp, NormOp, bool> condition, string mismatchReason = null)
            {
                foreach (var f in l1)
                {
                    if (f.IsMatched) continue;

                    foreach (var s in l2)
                    {
                        if (s.IsMatched) continue;

                        if (condition(f, s))
                        {
                            f.IsMatched = true;
                            s.IsMatched = true;

                            if (mismatchReason != null)
                            {
                                mismatches.Add(new OperationMismatch
                                {
                                    Date = f.DateStr,
                                    ParsedDate = f.ParsedDate,
                                    Description = f.Original.OperationDescription,
                                    Amount = f.Amount,
                                    ExpectedAmount = s.Amount,
                                    Reason = mismatchReason,
                                    RowIndex = f.Original.RowIndex
                                });
                            }
                            break;
                        }
                    }
                }
            }

            // Пробегаемся только по тем суммам, которые есть в первом акте
            foreach (var group in lookup1)
            {
                var amount = group.Key;
                var list1 = group.ToList();

                // Берем из второго акта только те операции, суммы которых совпадают (с допуском в копейку)
                var list2 = lookup2.Where(g => Math.Abs(g.Key - amount) <= 0.01m)
                                   .SelectMany(g => g)
                                   .ToList();

                if (!list2.Any()) continue;

                // 1. Идеальное совпадение: точная дата и описание
                MatchLocal(list1, list2, (f, s) =>
                    f.ParsedDate.HasValue && s.ParsedDate.HasValue &&
                    f.ParsedDate.Value.Date == s.ParsedDate.Value.Date &&
                    f.NormDesc == s.NormDesc);

                // 2. Хорошее совпадение: точная дата (описание может отличаться)
                MatchLocal(list1, list2, (f, s) =>
                    f.ParsedDate.HasValue && s.ParsedDate.HasValue &&
                    f.ParsedDate.Value.Date == s.ParsedDate.Value.Date);

                // 3. Совпадение с допуском (Даты в пределах DateToleranceDays)
                MatchLocal(list1, list2, (f, s) =>
                    f.ParsedDate.HasValue && s.ParsedDate.HasValue &&
                    Math.Abs((f.ParsedDate.Value - s.ParsedDate.Value).TotalDays) <= DateToleranceDays,
                    "Расхождение в датах операций");

                // 4. Сумма сходится, но дата сильно расходится (> 5 дней)
                // Так как мы УЖЕ находимся в группе одинаковых сумм, любые оставшиеся элементы подходят
                MatchLocal(list1, list2, (f, s) => true, "Дата сильно расходится");
            }

            // 5. Собираем все операции, которым вообще не нашлось пары по сумме
            foreach (var f in firstNorm.Where(x => !x.IsMatched))
            {
                mismatches.Add(new OperationMismatch
                {
                    Date = f.DateStr,
                    ParsedDate = f.ParsedDate,
                    Description = f.Original.OperationDescription,
                    Amount = f.Amount,
                    ExpectedAmount = 0,
                    Reason = "Только в первом акте",
                    RowIndex = f.Original.RowIndex
                });
            }

            foreach (var s in secondNorm.Where(x => !x.IsMatched))
            {
                mismatches.Add(new OperationMismatch
                {
                    Date = s.DateStr,
                    ParsedDate = s.ParsedDate,
                    Description = s.Original.OperationDescription,
                    Amount = s.Amount,
                    ExpectedAmount = 0,
                    Reason = "Только во втором акте",
                    RowIndex = s.Original.RowIndex
                });
            }

            // Финальный подсчет статистики
            result.MatchedOps = firstNorm.Count(x => x.IsMatched)
                              - mismatches.Count(m => m.Reason == "Расхождение в датах операций")
                              - mismatches.Count(m => m.Reason == "Дата сильно расходится");

            // Сортировка расхождений теперь идет по настоящим датам, а не по алфавиту
            result.Mismatches = mismatches.OrderBy(m => m.ParsedDate ?? DateTime.MaxValue).ToList();
            result.TotalDifference = Math.Abs(firstOps.Sum(x => x.Amount) - secondOps.Sum(x => x.Amount));
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