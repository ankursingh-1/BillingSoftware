using Billing.Application.DTOs.Tax;
using Billing.Application.Interfaces;
using Billing.Domain.Entities;
using Billing.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace Billing.API.Services
{
    public class TaxService : ITaxService
    {
        private readonly BillingDbContext _context;

        public TaxService(BillingDbContext context)
        {
            _context = context;
        }

        public async Task<List<TaxDto>> GetAllAsync()
        {
            return await _context.Taxes
                .AsNoTracking()
                .Where(x => !x.IsDeleted)
                .OrderBy(x => x.Percentage)
                .Select(x => new TaxDto
                {
                    Id = x.Id,
                    Name = x.Name,
                    Percentage = x.Percentage,
                    Description = x.Description,
                    IsActive = x.IsActive
                })
                .ToListAsync();
        }

        public async Task<TaxDto?> GetByIdAsync(int id)
        {
            return await _context.Taxes
                .AsNoTracking()
                .Where(x => x.Id == id && !x.IsDeleted)
                .Select(x => new TaxDto
                {
                    Id = x.Id,
                    Name = x.Name,
                    Percentage = x.Percentage,
                    Description = x.Description,
                    IsActive = x.IsActive
                })
                .FirstOrDefaultAsync();
        }

        public async Task<TaxDto> CreateAsync(SaveTaxRequest request)
        {
            var exists = await _context.Taxes
                .AnyAsync(x => !x.IsDeleted &&
                               x.Name.ToLower() == request.Name.ToLower());

            if (exists)
                throw new Exception("Tax already exists.");

            var tax = new Tax
            {
                Name = request.Name,
                Percentage = request.Percentage,
                Description = request.Description,
                IsActive = request.IsActive,
                CreatedOn = DateTime.UtcNow
            };

            _context.Taxes.Add(tax);
            await _context.SaveChangesAsync();

            return new TaxDto
            {
                Id = tax.Id,
                Name = tax.Name,
                Percentage = tax.Percentage,
                Description = tax.Description,
                IsActive = tax.IsActive
            };
        }

        public async Task<TaxDto?> UpdateAsync(int id, SaveTaxRequest request)
        {
            var tax = await _context.Taxes
                .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);

            if (tax == null)
                throw new Exception("Tax not found.");

            var exists = await _context.Taxes.AnyAsync(x =>
                x.Id != id &&
                !x.IsDeleted &&
                x.Name.ToLower() == request.Name.ToLower());

            if (exists)
                throw new Exception("Tax already exists.");

            tax.Name = request.Name;
            tax.Percentage = request.Percentage;
            tax.Description = request.Description;
            tax.IsActive = request.IsActive;
            tax.modifieson = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return new TaxDto
            {
                Id = tax.Id,
                Name = tax.Name,
                Percentage = tax.Percentage,
                Description = tax.Description,
                IsActive = tax.IsActive
            };
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var tax = await _context.Taxes
                .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);

            if (tax == null)
                throw new Exception("Tax not found.");

            tax.IsDeleted = true;
            tax.modifieson = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return true;
        }
    }
}