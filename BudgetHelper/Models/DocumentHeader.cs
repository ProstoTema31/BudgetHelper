using System;

namespace BudgetHelper.Models
{
    public class DocumentHeader
    {
        public string DocumentNumber { get; set; }
        public DateTime DocumentDate { get; set; }
        public decimal OpeningBalance { get; set; }
        public decimal ClosingBalance { get; set; }
    }
}