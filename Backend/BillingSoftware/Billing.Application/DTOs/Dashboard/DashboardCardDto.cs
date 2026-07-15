namespace Billing.Application.DTOs.Dashboard
{
    public class DashboardCardDto
    {
        public string Title { get; set; } = string.Empty;
        public decimal Value { get; set; }
        public string Icon { get; set; } = string.Empty;
        public string Color { get; set; } = string.Empty;
    }
}