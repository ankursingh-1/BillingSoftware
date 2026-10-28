import apiClient from "../api/apiClient";
import { cleanParams } from "../purchases/purchaseService";

import type { PagedResult } from "../../interfaces/common/pagedResult";
import type {
    CreateStockAdjustmentRequest,
    StockAdjustment,
    StockAdjustmentResponse,
    StockAdjustmentSearchRequest,
    StockItem,
    StockLedgerEntry,
    StockLedgerSearchRequest,
    StockSearchRequest,
    StockSummary,
} from "../../interfaces/inventory/inventory";

export const searchStock = async (
    request: StockSearchRequest
): Promise<PagedResult<StockItem>> => {
    const response = await apiClient.get<PagedResult<StockItem>>(
        "/Inventory/stock",
        { params: cleanParams(request) }
    );

    return response.data;
};

export const getStockSummary = async (): Promise<StockSummary> => {
    const response = await apiClient.get<StockSummary>("/Inventory/summary");

    return response.data;
};

export const searchLedger = async (
    request: StockLedgerSearchRequest
): Promise<PagedResult<StockLedgerEntry>> => {
    const response = await apiClient.get<PagedResult<StockLedgerEntry>>(
        "/StockLedger",
        { params: cleanParams(request) }
    );

    return response.data;
};

export const searchAdjustments = async (
    request: StockAdjustmentSearchRequest
): Promise<PagedResult<StockAdjustment>> => {
    const response = await apiClient.get<PagedResult<StockAdjustment>>(
        "/StockAdjustment",
        { params: cleanParams(request) }
    );

    return response.data;
};

export const createAdjustment = async (
    request: CreateStockAdjustmentRequest
): Promise<StockAdjustmentResponse> => {
    const response = await apiClient.post<StockAdjustmentResponse>(
        "/StockAdjustment",
        request
    );

    return response.data;
};