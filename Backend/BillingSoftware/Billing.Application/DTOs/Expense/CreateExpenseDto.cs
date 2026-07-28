using System.ComponentModel.DataAnnotations;

namespace Billing.Application.DTOs.Expense
{
    public class CreateExpenseDto
    {
        [Required(ErrorMessage = "Expense title is required.")]
        [StringLength(200, ErrorMessage = "Expense title cannot exceed 200 characters.")]
        public string Title { get; set; } = string.Empty;

        [Range(0.01, double.MaxValue, ErrorMessage = "Amount must be greater than 0.")]
        public decimal Amount { get; set; }

        [Required(ErrorMessage = "Expense date is required.")]
        public DateTime ExpenseDate { get; set; }

        [StringLength(100, ErrorMessage = "Category cannot exceed 100 characters.")]
        public string? Category { get; set; }

        [StringLength(500, ErrorMessage = "Remarks cannot exceed 500 characters.")]
        public string? Remarks { get; set; }
    }
}