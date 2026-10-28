using Billing.Application.DTOs.Common;
using System.ComponentModel.DataAnnotations;

namespace Billing.Application.DTOs.Reports;

public class PartyReportRequest : PaginationRequest
{
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }

    [StringLength(100)]
    public string? Search { get; set; }
}

public class CustomerReportItemDto
{
    public int CustomerId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Mobile { get; set; } = string.Empty;
    public string? Gstin { get; set; }
    public int InvoiceCount { get; set; }
    public decimal Billed { get; set; }
    public decimal Outstanding { get; set; }
}

public class CustomerReportResponse
{
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalRecords { get; set; }
    public int TotalPages { get; set; }
    public decimal TotalBilled { get; set; }
    public decimal TotalOutstanding { get; set; }
    public List<CustomerReportItemDto> Items { get; set; } = new();
}

public class SupplierReportItemDto
{
    public int SupplierId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string CompanyName { get; set; } = string.Empty;
    public string Mobile { get; set; } = string.Empty;
    public string? Gstin { get; set; }
    public int PurchaseCount { get; set; }
    public decimal Purchased { get; set; }
}

public class SupplierReportResponse
{
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalRecords { get; set; }
    public int TotalPages { get; set; }
    public decimal TotalPurchased { get; set; }
    public string Limitation { get; set; } =
        "Supplier payables are not tracked. This is purchase history, not an amount owed.";
    public List<SupplierReportItemDto> Items { get; set; } = new();
}

public class PaymentReportRequest : PaginationRequest
{
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }

    [StringLength(100)]
    public string? Search { get; set; }
}

public class PaymentReportItemDto
{
    public int PaymentId { get; set; }
    public string? ReceiptNumber { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public DateTime PaymentDate { get; set; }
    public string Method { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string? Reference { get; set; }
}

public class PaymentReportResponse
{
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalRecords { get; set; }
    public int TotalPages { get; set; }
    public decimal TotalCollected { get; set; }
    public List<PaymentReportItemDto> Items { get; set; } = new();
}
