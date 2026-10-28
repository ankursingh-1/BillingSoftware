import apiClient from "../api/apiClient";

import type { PagedResult } from "../../interfaces/common/pagedResult";
import type {
    Expense,
    ExpenseSearchRequest,
    SaveExpenseRequest,
} from "../../interfaces/expense/expense";

export const searchExpenses = async (
    request: ExpenseSearchRequest
): Promise<PagedResult<Expense>> => {
    const params = Object.fromEntries(
        Object.entries(request).filter(
            ([, value]) => value !== undefined && value !== ""
        )
    );

    const response = await apiClient.post<
        Omit<PagedResult<Expense>, "data"> & { items: Expense[] }
    >("/Expense/search", params);

    const body = response.data;

    return {
        totalRecords: body.totalRecords,
        totalPages: body.totalPages,
        pageNumber: body.pageNumber,
        pageSize: body.pageSize,
        data: body.items ?? [],
    };
};

export const createExpense = async (
    request: SaveExpenseRequest
): Promise<void> => {
    await apiClient.post("/Expense", request);
};

export const updateExpense = async (
    id: number,
    request: SaveExpenseRequest
): Promise<void> => {
    await apiClient.put(`/Expense/${id}`, request);
};

export const deleteExpense = async (id: number): Promise<void> => {
    await apiClient.delete(`/Expense/${id}`);
};
