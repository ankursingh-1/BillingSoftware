using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Billing.Domain.Enums
{
    public enum StockTransactionType
    {
        OpeningStock = 1,
        Purchase = 2,
        Sale = 3,
        PurchaseReturn = 4,
        SalesReturn = 5,
        Adjustment = 6,
        Damaged = 7,
        Expired = 8,
        Lost = 9
    }
}