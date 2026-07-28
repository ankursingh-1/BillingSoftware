using Billing.Application.DTOs.AppSetting;
using Billing.Application.Interfaces;
using Billing.Domain.Entities;
using Billing.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace Billing.API.Services;

public class AppSettingService : IAppSettingService
{
    private readonly BillingDbContext _context;

    public AppSettingService(BillingDbContext context)
    {
        _context = context;
    }

    public async Task<AppSettingDto?> GetAsync()
    {
        return await _context.AppSettings
            .Where(x => !x.IsDeleted)
            .Select(x => new AppSettingDto
            {
                Id = x.Id,
                CompanyName = x.CompanyName,
                CompanyAddress = x.CompanyAddress,
                Phone = x.Phone,
                Email = x.Email,
                GSTNumber = x.GSTNumber,
                Currency = x.Currency,
                InvoicePrefix = x.InvoicePrefix,
                LogoUrl = x.LogoUrl
            })
            .FirstOrDefaultAsync();
    }

    public async Task SaveAsync(CreateAppSettingDto dto)
    {
        var setting = await _context.AppSettings
            .FirstOrDefaultAsync(x => !x.IsDeleted);

        if (setting == null)
        {
            setting = new AppSetting
            {
                CompanyName = dto.CompanyName,
                CompanyAddress = dto.CompanyAddress,
                Phone = dto.Phone,
                Email = dto.Email,
                GSTNumber = dto.GSTNumber,
                Currency = dto.Currency,
                InvoicePrefix = dto.InvoicePrefix,
                LogoUrl = dto.LogoUrl,
                CreatedOn = DateTime.UtcNow
            };

            _context.AppSettings.Add(setting);
        }
        else
        {
            setting.CompanyName = dto.CompanyName;
            setting.CompanyAddress = dto.CompanyAddress;
            setting.Phone = dto.Phone;
            setting.Email = dto.Email;
            setting.GSTNumber = dto.GSTNumber;
            setting.Currency = dto.Currency;
            setting.InvoicePrefix = dto.InvoicePrefix;
            setting.LogoUrl = dto.LogoUrl;
            setting.modifieson = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();
    }
}