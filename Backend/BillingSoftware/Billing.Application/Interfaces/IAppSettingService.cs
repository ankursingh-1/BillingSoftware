using Billing.Application.DTOs.AppSetting;

namespace Billing.Application.Interfaces
{
    public interface IAppSettingService
    {
        Task<AppSettingDto?> GetAsync();
        Task SaveAsync(CreateAppSettingDto dto);
    }
}