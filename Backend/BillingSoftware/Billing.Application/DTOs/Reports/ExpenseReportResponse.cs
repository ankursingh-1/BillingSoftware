using Billing.Application.DTOs.Expense;

namespace Billing.Application.DTOs.Reports;

public class ExpenseReportResponse
{
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalRecords { get; set; }
    public int TotalPages { get; set; }
    public decimal GrandTotal { get; set; }
    public List<ExpenseDto> Items { get; set; } = new();
}
