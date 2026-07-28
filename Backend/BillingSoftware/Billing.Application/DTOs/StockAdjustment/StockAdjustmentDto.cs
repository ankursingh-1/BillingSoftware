using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Billing.Application.DTOs.StockAdjustment
{
    public class StockAdjustmentDto
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public bool IncreaseStock { get; set; }
        public string? Remarks { get; set; }
        public DateTime AdjustmentDate { get; set; }
    }
}