using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Billing.Application.DTOs.StockAdjustment
{
    public class CreateStockAdjustmentDto
    {
        public int ProductId { get; set; }
        public int Quantity { get; set; }
        public bool IncreaseStock { get; set; }
        public string? Remarks { get; set; }
    }
}