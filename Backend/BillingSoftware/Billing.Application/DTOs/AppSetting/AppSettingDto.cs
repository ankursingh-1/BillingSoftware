using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Billing.Application.DTOs.AppSetting
{
    public class AppSettingDto
    {
        public int Id { get; set; }
        public string CompanyName { get; set; } = string.Empty;
        public string? CompanyAddress { get; set; }
        public string? Phone { get; set; }
        public string? Email { get; set; }
        public string? GSTNumber { get; set; }
        public string? Currency { get; set; }
        public string? InvoicePrefix { get; set; }
        public string? LogoUrl { get; set; }
    }
}