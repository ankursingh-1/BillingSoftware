namespace Billing.Application.DTOs.Tax
{
    public class TaxDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal Percentage { get; set; }
        public string? Description { get; set; }
        public bool IsActive { get; set; }
    }
}