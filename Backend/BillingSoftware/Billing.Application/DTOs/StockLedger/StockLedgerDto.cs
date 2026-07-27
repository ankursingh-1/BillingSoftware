using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Billing.Application.DTOs.StockLedger
{
    public class StockLedgerDto
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string TransactionType { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public int PreviousStock { get; set; }
        public int CurrentStock { get; set; }
        public string? ReferenceNo { get; set; }
        public string? Remarks { get; set; }
        public DateTime CreatedOn { get; set; }
    }
}