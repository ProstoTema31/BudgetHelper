using System;

namespace BudgetHelper.Models
{
    public class DocumentHeader
    {
        public int Id { get; set; }
        public string DocumentNumber { get; set; }
        public DateTime DocumentDate { get; set; }
        public string Contractor { get; set; }
        public string Inn { get; set; }
        public string Kpp { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal OpeningBalance { get; set; }
        public decimal ClosingBalance { get; set; }
    }
}