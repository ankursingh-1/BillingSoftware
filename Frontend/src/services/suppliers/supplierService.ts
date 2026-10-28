import apiClient from "../api/apiClient";

import type { PagedResult } from "../../interfaces/common/pagedResult";
import type {
    SaveSupplierRequest,
    Supplier,
    SupplierSearchRequest,
} from "../../interfaces/supplier/supplier";

export const searchSuppliers = async (
    request: SupplierSearchRequest
): Promise<PagedResult<Supplier>> => {
    const response = await apiClient.post<PagedResult<Supplier>>(
        "/Suppliers/search",
        request
    );

    return response.data;
};

export const createSupplier = async (
    request: SaveSupplierRequest
): Promise<void> => {
    await apiClient.post("/Suppliers", request);
};

export const updateSupplier = async (
    id: number,
    request: SaveSupplierRequest
): Promise<void> => {
    await apiClient.put(`/Suppliers/${id}`, request);
};

export const deleteSupplier = async (id: number): Promise<void> => {
    await apiClient.delete(`/Suppliers/${id}`);
};
