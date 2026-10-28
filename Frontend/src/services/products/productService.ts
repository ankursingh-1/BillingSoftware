import apiClient from "../api/apiClient";

import type { PagedResult } from "../../interfaces/common/pagedResult";
import type {
    CreateProductResponse,
    Product,
    ProductSearchRequest,
    SaveProductRequest,
} from "../../interfaces/product/product";

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
