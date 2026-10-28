import apiClient from "../api/apiClient";

import type {
    Category,
    SaveCategoryRequest,
} from "../../interfaces/category/category";

export const getCategories = async (): Promise<Category[]> => {
    const response = await apiClient.get<Category[]>("/Category");

    return response.data;
};

export const createCategory = async (
    request: SaveCategoryRequest
): Promise<Category> => {
    const response = await apiClient.post<Category>(
        "/Category",
        request
    );

    return response.data;
};

export const updateCategory = async (
    id: number,
    request: SaveCategoryRequest
): Promise<Category> => {
    const response = await apiClient.put<Category>(
        `/Category/${id}`,
        request
    );

    return response.data;
};

export const deleteCategory = async (id: number): Promise<void> => {
    await apiClient.delete(`/Category/${id}`);
};
