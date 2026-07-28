using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Billing.Application.DTOs.PurchaseReturn
{
    public class CreatePurchaseReturnDto
    {
        public int ProductId { get; set; }
        public int Quantity { get; set; }
        public string? ReferenceNo { get; set; }
        public string? Remarks { get; set; }
    }
}