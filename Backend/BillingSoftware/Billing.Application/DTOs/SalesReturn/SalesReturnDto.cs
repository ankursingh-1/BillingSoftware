using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Billing.Application.DTOs.SalesReturn
{
    public class SalesReturnDto
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public string? ReferenceNo { get; set; }
        public string? Remarks { get; set; }
        public DateTime ReturnDate { get; set; }
    }
}