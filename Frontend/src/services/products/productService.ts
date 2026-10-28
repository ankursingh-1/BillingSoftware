import apiClient from "../api/apiClient";

import type { PagedResult } from "../../interfaces/common/pagedResult";
import type {
    CreateProductResponse,
    Product,
    ProductImportPreview,
    ProductImportResult,
    ProductSearchRequest,
    SaveProductRequest,
} from "../../interfaces/product/product";

const saveBlob = (data: Blob, fileName: string): void => {
    const url = URL.createObjectURL(data);
    const link = document.createElement("a");

    link.href = url;
    link.download = fileName;
    document.body.appendChild(link);
    link.click();
    link.remove();
    URL.revokeObjectURL(url);
};

const asForm = (file: File): FormData => {
    const body = new FormData();

    body.append("file", file);

    return body;
};

export const searchProducts = async (
    request: ProductSearchRequest
): Promise<PagedResult<Product>> => {
    const response = await apiClient.post<PagedResult<Product>>(
        "/Products/search",
        request
    );

    return response.data;
};

export const getProduct = async (id: number): Promise<Product> => {
    const response = await apiClient.get<Product>(`/Products/${id}`);

    return response.data;
};

export const createProduct = async (
    request: SaveProductRequest
): Promise<CreateProductResponse> => {
    const response = await apiClient.post<CreateProductResponse>(
        "/Products",
        request
    );

    return response.data;
};

export const updateProduct = async (
    id: number,
    request: SaveProductRequest
): Promise<void> => {
    await apiClient.put(`/Products/${id}`, request);
};

export const deleteProduct = async (id: number): Promise<void> => {
    await apiClient.delete(`/Products/${id}`);
};

export const downloadProductTemplate = async (): Promise<void> => {
    const response = await apiClient.get<Blob>("/Products/import/template", {
        responseType: "blob",
    });

    saveBlob(response.data, "product-import-template.xlsx");
};

export const exportProducts = async (): Promise<void> => {
    const response = await apiClient.get<Blob>("/Products/export", {
        responseType: "blob",
    });

    saveBlob(response.data, "products.xlsx");
};

export const previewProductImport = async (
    file: File,
    signal?: AbortSignal
): Promise<ProductImportPreview> => {
    const response = await apiClient.post<ProductImportPreview>(
        "/Products/import/preview",
        asForm(file),
        { signal }
    );

    return response.data;
};

export const importProducts = async (
    file: File,
    signal?: AbortSignal
): Promise<ProductImportResult> => {
    const response = await apiClient.post<ProductImportResult>(
        "/Products/import",
        asForm(file),
        { signal }
    );

    return response.data;
};

export const downloadProductImportErrors = async (file: File): Promise<void> => {
    const response = await apiClient.post<Blob>(
        "/Products/import/error-report",
        asForm(file),
        { responseType: "blob" }
    );

    saveBlob(response.data, "product-import-errors.xlsx");
};
