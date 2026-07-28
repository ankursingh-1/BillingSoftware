using Billing.Domain.Common;

namespace Billing.Domain.Entities
{
    public class Expense : SoftDeleteEntity
    {
        public string Title { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public DateTime ExpenseDate { get; set; }
        public string? Category { get; set; }
        public string? Remarks { get; set; }
    }
}