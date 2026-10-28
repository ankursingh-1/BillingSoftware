// Matches Billing.Application.DTOs.Purchases.*

export interface PurchaseListItem {
    id: number;
    purchaseNumber: string;
    supplierInvoiceNumber: string | null;
    supplierId: number;
    supplierName: string;
    purchaseDate: string;
    totalAmount: number;
    itemCount: number;
    returnedAmount: number;
}

export interface PurchaseSearchRequest {
    search?: string;
    fromDate?: string;
    toDate?: string;
    supplierId?: number;
    pageNumber: number;
    pageSize: number;
}

export interface PurchaseSupplier {
    id: number;
    name: string;
    companyName: string;
    mobile: string;
    email: string | null;
    address: string | null;
    state: string | null;
    stateCode: string | null;
    gstin: string | null;
}

export interface PurchaseItemDetail {
    id: number;
    productId: number;
    productName: string;
    sku: string | null;
    hsnCode: string | null;
    quantity: number;
    unitPrice: number;
    /** Taxable (pre-tax) amount of the line. */
    total: number;
    taxName: string | null;
    taxRate: number | null;
    cgstAmount: number | null;
    sgstAmount: number | null;
    igstAmount: number | null;
    taxAmount: number | null;
    lineTotal: number | null;
    returnedQuantity: number;
    returnableQuantity: number;
}

export interface PurchaseReturnSummary {
    id: number;
    returnNumber: string;
    returnDate: string;
    totalAmount: number;
    refundReceived: number;
}

export interface PurchaseDetail {
    id: number;
    purchaseNumber: string;
    supplierInvoiceNumber: string | null;
    purchaseDate: string;
    remarks: string | null;
    createdBy: string | null;
    supplier: PurchaseSupplier;
    subtotal: number;
    cgstTotal: number;
    sgstTotal: number;
    igstTotal: number;
    taxTotal: number;
    roundOff: number;
    totalAmount: number;
    taxCalculated: boolean;
    isInterState: boolean;
    returnedAmount: number;
    items: PurchaseItemDetail[];
    returns: PurchaseReturnSummary[];
}

export interface CreatePurchaseRequest {
    supplierId: number;
    purchaseDate: string;
    supplierInvoiceNumber: string | null;
    remarks: string | null;
    items: { productId: number; quantity: number; purchasePrice: number }[];
    updateCatalogueCost: boolean;
    clientRequestId: string;
}

export interface CreatePurchaseResponse {
    success: boolean;
    message: string;
    purchaseId: number;
    purchaseNumber: string;
    totalAmount: number;
    isDuplicate: boolean;
}