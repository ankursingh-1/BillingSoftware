import apiClient from "../api/apiClient";

import type { PagedResult } from "../../interfaces/common/pagedResult";
import type {
    CreatePurchaseRequest,
    CreatePurchaseResponse,
    PurchaseDetail,
    PurchaseListItem,
    PurchaseSearchRequest,
} from "../../interfaces/purchase/purchase";

/** Drops empty filters so they are not sent as blank query values. */
export const cleanParams = <T extends object>(request: T) =>
    Object.fromEntries(
        Object.entries(request).filter(
            ([, value]) => value !== undefined && value !== ""
        )
    );

export const searchPurchases = async (
    request: PurchaseSearchRequest
): Promise<PagedResult<PurchaseListItem>> => {
    const response = await apiClient.get<PagedResult<PurchaseListItem>>(
        "/Purchases",
        { params: cleanParams(request) }
    );

    return response.data;
};

export const getPurchase = async (id: number): Promise<PurchaseDetail> => {
    const response = await apiClient.get<PurchaseDetail>(`/Purchases/${id}`);

    return response.data;
};

export const createPurchase = async (
    request: CreatePurchaseRequest
): Promise<CreatePurchaseResponse> => {
    const response = await apiClient.post<CreatePurchaseResponse>(
        "/Purchases",
        request
    );

    return response.data;
};