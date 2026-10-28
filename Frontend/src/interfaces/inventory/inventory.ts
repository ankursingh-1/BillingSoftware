export type StockStatus = "InStock" | "Low" | "Out";

export interface StockItem {
    productId: number;
    name: string;
    sku: string;
    categoryName: string | null;
    unitName: string | null;
    stock: number;
    status: StockStatus;
    purchasePrice: number;
    sellingPrice: number;
    /** Null when the product has no cost price. */
    stockValue: number | null;
}

export interface StockSummary {
    totalProducts: number;
    inStock: number;
    lowStock: number;
    outOfStock: number;
    lowStockThreshold: number;
    totalUnits: number;
    stockValueAtCost: number;
    productsWithoutCost: number;
}

export interface StockSearchRequest {
    search?: string;
    categoryId?: number;
    status?: "" | "in" | "low" | "out";
    pageNumber: number;
    pageSize: number;
}

export interface StockLedgerEntry {
    id: number;
    productId: number;
    productName: string;
    sku: string | null;
    transactionType: string;
    quantity: number;
    quantityChange: number;
    previousStock: number;
    currentStock: number;
    referenceNo: string | null;
    remarks: string | null;
    createdBy: string | null;
    createdOn: string;
}

export interface StockLedgerSearchRequest {
    search?: string;
    productId?: number;
    transactionType?: string;
    fromDate?: string;
    toDate?: string;
    pageNumber: number;
    pageSize: number;
}

export type AdjustmentReason = "Correction" | "Damaged" | "Expired" | "Lost";

export interface StockAdjustment {
    id: number;
    referenceNo: string | null;
    productId: number;
    productName: string;
    sku: string | null;
    reason: string;
    quantity: number;
    increaseStock: boolean;
    previousStock: number;
    currentStock: number;
    remarks: string | null;
    createdBy: string | null;
    adjustmentDate: string;
}

export interface StockAdjustmentSearchRequest {
    search?: string;
    fromDate?: string;
    toDate?: string;
    pageNumber: number;
    pageSize: number;
}

export interface CreateStockAdjustmentRequest {
    productId: number;
    quantity: number;
    increaseStock: boolean;
    reason: AdjustmentReason;
    remarks: string;
}

export interface StockAdjustmentResponse {
    success: boolean;
    message: string;
    referenceNo: string;
    productId: number;
    previousStock: number;
    currentStock: number;
}