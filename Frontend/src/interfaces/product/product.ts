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
}

/**
 * Request body for create/update (same ProductDto on the server).
 * `stock` is the opening stock on create and is ignored on update.
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
