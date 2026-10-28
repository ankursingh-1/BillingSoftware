using Billing.Application.DTOs.Common;
using System.ComponentModel.DataAnnotations;

namespace Billing.Application.DTOs.Expense;

public class ExpenseSearchRequest : PaginationRequest
{
    [StringLength(200)]
    public string? Search { get; set; }

    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }

    [StringLength(100)]
    public string? Category { get; set; }
}
