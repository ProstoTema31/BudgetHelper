using System;

namespace BudgetHelper.Models
{
    public class DocumentContent
    {
        public string OperationDate { get; set; }
        public string OperationDescription { get; set; }
        public string DocumentNumber { get; set; }
        public decimal Amount { get; set; }
        public int RowIndex { get; set; }
    }
}