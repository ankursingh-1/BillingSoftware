using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Billing.Domain.Common;
using Billing.Domain.Enums;

namespace Billing.Domain.Entities
{
    public class Product : SoftDeleteEntity
    {
        public string Name { get; set; } = string.Empty;
        public string SKU { get; set; } = string.Empty;

        /// <summary>
        /// Optional barcode (EAN/UPC/shop code). Distinct from SKU. Used by POS scan.
        /// </summary>
        public string? Barcode { get; set; }

        public decimal PurchasePrice { get; set; }
        public decimal SellingPrice { get; set; }
        public int Stock { get; set; }

        /// <summary>Default warranty length. Null means this product has no default warranty.</summary>
        public int? WarrantyDurationValue { get; set; }

        public WarrantyDurationUnit? WarrantyDurationUnit { get; set; }

        /// <summary>
        /// When true, a sale that registers a warranty must supply one serial per unit.
        /// Serials are recorded on the warranty, not as serial-level inventory.
        /// </summary>
        public bool RequiresSerialNumber { get; set; }

        /// <summary>HSN (goods) or SAC (services) code printed on tax invoices.</summary>
        public string? HsnCode { get; set; }
        public int? CategoryId { get; set; }
        public Category? Category { get; set; }
        public int? BrandId { get; set; }
        public Brand? Brand { get; set; }
        public int? UnitId { get; set; }
        public Unit? Unit { get; set; }
        public int? TaxId { get; set; }
        public Tax? Tax { get; set; }
        public ICollection<StockLedger> StockLedgers { get; set; }
        = new List<StockLedger>();
    }
}
