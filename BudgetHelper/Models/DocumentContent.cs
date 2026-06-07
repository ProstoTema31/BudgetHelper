using System;

namespace BudgetHelper.Models
{
    public class DocumentContent
    {
        public int Id { get; set; }
        public int HeaderId { get; set; }
        public string OperationDate { get; set; }
        public string OperationDescription { get; set; }
        public string DocumentNumber { get; set; } 
        public decimal Debit { get; set; } 
        public decimal Credit { get; set; }
        public decimal Amount { get; set; } 
        public bool IsOpeningBalance { get; set; }
        public bool IsClosingBalance { get; set; }
        public int RowIndex { get; set; } 
        public string SheetName { get; set; }
        public string CellAddress { get; set; }
    }
}