using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Billing.Application.DTOs.PurchaseReturn
{
    public class PurchaseReturnDto
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public string? ReferenceNo { get; set; }
        public string? Remarks { get; set; }
        public DateTime ReturnDate { get; set; }
    }
}