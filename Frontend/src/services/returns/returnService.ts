import apiClient from "../api/apiClient";
import { cleanParams } from "../purchases/purchaseService";

import type { PagedResult } from "../../interfaces/common/pagedResult";
import type { PaymentMethod } from "../../interfaces/lookups/lookups";
import type {
    CreatePurchaseReturnRequest,
    CreateSalesReturnRequest,
    PurchaseReturnDetail,
    PurchaseReturnListItem,
    PurchaseReturnResponse,
    ReturnSearchRequest,
    SalesReturnDetail,
    SalesReturnListItem,
    SalesReturnResponse,
} from "../../interfaces/returns/returns";

export const searchSalesReturns = async (
    request: ReturnSearchRequest
): Promise<PagedResult<SalesReturnListItem>> => {
    const response = await apiClient.get<PagedResult<SalesReturnListItem>>(
        "/SalesReturns",
        { params: cleanParams(request) }
    );

    return response.data;
};

export const getSalesReturn = async (
    id: number
): Promise<SalesReturnDetail> => {
    const response = await apiClient.get<SalesReturnDetail>(
        `/SalesReturns/${id}`
    );

    return response.data;
};

export const createSalesReturn = async (
    request: CreateSalesReturnRequest
): Promise<SalesReturnResponse> => {
    const response = await apiClient.post<SalesReturnResponse>(
        "/SalesReturns",
        request
    );

    return response.data;
};

export const recordSalesReturnRefund = async (
    returnId: number,
    amount: number,
    method: PaymentMethod
): Promise<SalesReturnResponse> => {
    const response = await apiClient.post<SalesReturnResponse>(
        `/SalesReturns/${returnId}/refund`,
        { amount, method }
    );

    return response.data;
};

export const searchPurchaseReturns = async (
    request: ReturnSearchRequest
): Promise<PagedResult<PurchaseReturnListItem>> => {
    const response = await apiClient.get<PagedResult<PurchaseReturnListItem>>(
        "/PurchaseReturns",
        { params: cleanParams(request) }
    );

    return response.data;
};

export const getPurchaseReturn = async (
    id: number
): Promise<PurchaseReturnDetail> => {
    const response = await apiClient.get<PurchaseReturnDetail>(
        `/PurchaseReturns/${id}`
    );

    return response.data;
};

export const createPurchaseReturn = async (
    request: CreatePurchaseReturnRequest
): Promise<PurchaseReturnResponse> => {
    const response = await apiClient.post<PurchaseReturnResponse>(
        "/PurchaseReturns",
        request
    );

    return response.data;
};