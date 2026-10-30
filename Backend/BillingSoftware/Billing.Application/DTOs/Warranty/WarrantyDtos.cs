using Billing.Application.DTOs.Common;

namespace Billing.Application.DTOs.Warranty;

public class WarrantySearchRequest : PaginationRequest
{
    public string? Search { get; set; }
    public string? Status { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
}

public class WarrantyListItemDto
{
    public int Id { get; set; }
    public string WarrantyNumber { get; set; } = string.Empty;
    public string InvoiceNumber { get; set; } = string.Empty;
    public int SaleId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string? SerialNumber { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerMobile { get; set; } = string.Empty;
    public string Duration { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public int DaysRemaining { get; set; }
    public string Status { get; set; } = string.Empty;
    public bool Eligible { get; set; }
}

public class WarrantyDetailDto : WarrantyListItemDto
{
    public int SaleItemId { get; set; }
    public int ProductId { get; set; }
    public string? ProductSku { get; set; }
    public int? CustomerId { get; set; }
    public int DurationValue { get; set; }
    public string DurationUnit { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public bool IsVoided { get; set; }
    public string EligibilityNote { get; set; } = string.Empty;
    public List<WarrantyClaimDto> Claims { get; set; } = new();
}

public class WarrantyClaimDto
{
    public int Id { get; set; }
    public string ClaimNumber { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string ReportedProblem { get; set; } = string.Empty;
    public string? InspectionNotes { get; set; }
    public string? DecisionReason { get; set; }
    public string? Notes { get; set; }
    public string? DefectiveSerial { get; set; }
    public int? ReplacementProductId { get; set; }
    public string? ReplacementProductName { get; set; }
    public string? ReplacementSerial { get; set; }
    public bool ReplacementStockMoved { get; set; }
    public DateTime SubmittedOn { get; set; }
    public DateTime? ResolvedOn { get; set; }
    public List<WarrantyClaimEventDto> Events { get; set; } = new();
}

public class WarrantyClaimEventDto
{
    public string Status { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public DateTime At { get; set; }
    public string? By { get; set; }
}

public class SubmitWarrantyClaimRequest
{
    public string ReportedProblem { get; set; } = string.Empty;
    public string? Notes { get; set; }
}

public class UpdateWarrantyClaimRequest
{
    public string Status { get; set; } = string.Empty;
    public string? InspectionNotes { get; set; }
    public string? DecisionReason { get; set; }
    public string? Notes { get; set; }
    public string? DefectiveSerial { get; set; }
    public int? ReplacementProductId { get; set; }
    public string? ReplacementSerial { get; set; }
}

public class WarrantyInvoiceLineDto
{
    public int SaleItemId { get; set; }
    public string WarrantyNumber { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string? SerialNumber { get; set; }
    public string Duration { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
}

public class WarrantyReportRequest : PaginationRequest
{
    public string Kind { get; set; } = "active";
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public string? Search { get; set; }
}

public class WarrantyClaimReportItemDto
{
    public int WarrantyId { get; set; }
    public int SaleId { get; set; }
    public string ClaimNumber { get; set; } = string.Empty;
    public string WarrantyNumber { get; set; } = string.Empty;
    public string InvoiceNumber { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime SubmittedOn { get; set; }
    public string? ReplacementProductName { get; set; }
}
