using Billing.Application.DTOs.Common;
using Billing.Application.DTOs.Warranty;

namespace Billing.Application.Interfaces;

public interface IWarrantyService
{
    Task<PagedResult<WarrantyListItemDto>> SearchAsync(WarrantySearchRequest request);
    Task<WarrantyDetailDto> GetByIdAsync(int id);
    Task<List<WarrantyListItemDto>> ExpiringAsync(int take = 10);
    Task<PagedResult<WarrantyListItemDto>> ReportAsync(WarrantyReportRequest request);
    Task<PagedResult<WarrantyClaimReportItemDto>> ClaimReportAsync(WarrantyReportRequest request);
    Task<WarrantyClaimDto> SubmitClaimAsync(int warrantyId, SubmitWarrantyClaimRequest request);
    Task<WarrantyClaimDto> UpdateClaimAsync(int claimId, UpdateWarrantyClaimRequest request);
    Task<int> CountExpiringAsync();
}
