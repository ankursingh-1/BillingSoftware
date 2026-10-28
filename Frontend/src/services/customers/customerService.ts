import apiClient from "../api/apiClient";

import type { PagedResult } from "../../interfaces/common/pagedResult";
import type {
    Customer,
    CustomerSearchRequest,
    SaveCustomerRequest,
} from "../../interfaces/customer/customer";

export const searchCustomers = async (
    request: CustomerSearchRequest
): Promise<PagedResult<Customer>> => {
    const response = await apiClient.post<PagedResult<Customer>>(
        "/Customers/search",
        request
    );

    return response.data;
};

export const createCustomer = async (
    request: SaveCustomerRequest
): Promise<void> => {
    await apiClient.post("/Customers", request);
};

export const updateCustomer = async (
    id: number,
    request: SaveCustomerRequest
): Promise<void> => {
    await apiClient.put(`/Customers/${id}`, request);
};

export const deleteCustomer = async (id: number): Promise<void> => {
    await apiClient.delete(`/Customers/${id}`);
};
