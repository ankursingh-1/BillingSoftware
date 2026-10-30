// Matches Billing.API.Models.ProductDto
export interface Product {
    id: number;
    name: string;
    sku: string;
    purchasePrice: number;
    sellingPrice: number;
    stock: number;
    categoryId: number | null;
    categoryName: string | null;
    brandId: number | null;
    brandName: string | null;
    unitId: number | null;
    unitName: string | null;
    taxId: number | null;
    taxName: string | null;
    taxPercentage: number | null;
    hsnCode: string | null;
    barcode: string | null;
    warrantyDurationValue: number | null;
    warrantyDurationUnit: "Days" | "Months" | "Years" | null;
    requiresSerialNumber: boolean;
}

/**
 * Request body for create/update (same ProductDto on the server).
 * `stock` is opening stock on create and the desired on-hand quantity on update.
 */
export interface SaveProductRequest {
    name: string;
    sku: string;
    purchasePrice: number;
    sellingPrice: number;
    stock: number;
    categoryId: number | null;
    brandId: number | null;
    unitId: number | null;
    taxId: number | null;
    hsnCode: string | null;
    barcode: string | null;
    warrantyDurationValue: number | null;
    warrantyDurationUnit: "Days" | "Months" | "Years" | null;
    requiresSerialNumber: boolean;
}

export interface ProductSearchRequest {
    search?: string;
    pageNumber: number;
    pageSize: number;
}

export interface CreateProductResponse {
    success: boolean;
    message: string;
    productId: number;
}

export type ProductImportStatus =
    | "Valid"
    | "Invalid"
    | "Duplicate"
    | "Imported"
    | "Skipped"
    | "Unchanged"
    | "Conflict"
    | "Updated"
    | "Added"
    | "StockIncreased"
    | "Failed";

export interface ProductImportRow {
    excelRow: number;
    name: string;
    sku: string;
    category: string | null;
    brand: string | null;
    unit: string | null;
    tax: string | null;
    hsn: string | null;
    purchasePrice: number | null;
    sellingPrice: number | null;
    openingStock: number;
    stockQuantity: number | null;
    addStockQuantity: number | null;
    barcode: string | null;
    warrantyDurationValue: number | null;
    warrantyDurationUnit: string | null;
    requiresSerialNumber: boolean;
    productId: number | null;
    receiptId: string | null;
    status: ProductImportStatus;
    error: string | null;
    action: string | null;
    fieldChanges: string | null;
    matchedProductId: number | null;
    currentStock: number | null;
    proposedStockChange: number;
    willAddProduct: boolean;
    willUpdateFields: boolean;
    willIncreaseStock: boolean;
}

export interface ProductImportPreview {
    totalRows: number;
    valid: number;
    invalid: number;
    duplicate: number;
    unchanged: number;
    conflict: number;
    canImport: boolean;
    policy: string;
    rows: ProductImportRow[];
}

export interface ProductImportResult extends ProductImportPreview {
    imported: number;
    skipped: number;
    added: number;
    updated: number;
    stockIncreased: number;
    replayed: boolean;
}
