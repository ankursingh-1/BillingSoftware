import apiClient from "../api/apiClient";

import type { PagedResult } from "../../interfaces/common/pagedResult";
import type {
    CreateSaleRequest,
    CreateSaleResponse,
    RecordPaymentRequest,
    SaleDetail,
    SaleListItem,
    SaleSearchRequest,
} from "../../interfaces/sale/sale";

export const searchSales = async (
    request: SaleSearchRequest
): Promise<PagedResult<SaleListItem>> => {
    // Drop empty filters so they are not sent as blank query values.
    const params = Object.fromEntries(
        Object.entries(request).filter(
            ([, value]) => value !== undefined && value !== ""
        )
    );

    const response = await apiClient.get<PagedResult<SaleListItem>>(
        "/Sales",
        { params }
    );

    return response.data;
};

export const getSale = async (id: number): Promise<SaleDetail> => {
    const response = await apiClient.get<SaleDetail>(`/Sales/${id}`);

    return response.data;
};

export const createSale = async (
    request: CreateSaleRequest
): Promise<CreateSaleResponse> => {
    const response = await apiClient.post<CreateSaleResponse>(
        "/Sales",
        request
    );

    return response.data;
};

export const recordPayment = async (
    saleId: number,
    request: RecordPaymentRequest
): Promise<SaleDetail> => {
    const response = await apiClient.post<SaleDetail>(
        `/Sales/${saleId}/payments`,
        request
    );

    return response.data;
};

export const voidPayment = async (
    saleId: number,
    paymentId: number,
    reason: string
): Promise<SaleDetail> => {
    const response = await apiClient.post<SaleDetail>(
        `/Sales/${saleId}/payments/${paymentId}/void`,
        { reason }
    );

    return response.data;
};

/** Downloads the invoice PDF through the authenticated client. */
export const downloadInvoicePdf = async (
    saleId: number,
    fileName: string
): Promise<void> => {
    const response = await apiClient.get<Blob>(`/Sales/${saleId}/pdf`, {
        responseType: "blob",
    });

    const url = URL.createObjectURL(response.data);
    const link = document.createElement("a");

    link.href = url;
    link.download = `${fileName}.pdf`;
    document.body.appendChild(link);
    link.click();
    link.remove();
    URL.revokeObjectURL(url);
};