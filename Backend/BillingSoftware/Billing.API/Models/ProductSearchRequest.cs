namespace Billing.API.Models;

public class ProductSearchRequest
{
    public string? Search { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}