import apiClient from "../api/apiClient";

import type { PagedResult } from "../../interfaces/common/pagedResult";
import type {
    WarrantyClaim,
    WarrantyClaimReportItem,
    WarrantyDetail,
    WarrantyListItem,
    WarrantyReportRequest,
    WarrantySearchRequest,
} from "../../interfaces/warranty/warranty";

const toPage = <T>(
    body: Omit<PagedResult<T>, "data"> & { items?: T[]; data?: T[] }
): PagedResult<T> => ({
    totalRecords: body.totalRecords,
    totalPages: body.totalPages,
    pageNumber: body.pageNumber,
    pageSize: body.pageSize,
    data: body.items ?? body.data ?? [],
});

export const searchWarranties = async (
    request: WarrantySearchRequest
): Promise<PagedResult<WarrantyListItem>> => {
    const response = await apiClient.post<
        Omit<PagedResult<WarrantyListItem>, "data"> & { items: WarrantyListItem[] }
    >("/Warranty/search", request);

    return toPage(response.data);
};

const CLAIM_REPORT_KINDS = new Set([
    "claims",
    "approved",
    "rejected",
    "replacements",
    "inspection",
]);

/** Warranty register, or the claim report when status is a claim kind. */
export const loadWarrantyRows = async (
    request: WarrantySearchRequest
): Promise<PagedResult<WarrantyListItem | WarrantyClaimReportItem>> => {
    const kind = request.status?.trim().toLowerCase();

    if (kind && CLAIM_REPORT_KINDS.has(kind)) {
        return getWarrantyReport({
            kind,
            search: request.search,
            fromDate: request.fromDate,
            toDate: request.toDate,
            pageNumber: request.pageNumber,
            pageSize: request.pageSize,
        });
    }

    return searchWarranties(request);
};

export const getWarranty = async (id: number): Promise<WarrantyDetail> => {
    const response = await apiClient.get<WarrantyDetail>(`/Warranty/${id}`);
    return response.data;
};

export const getExpiringWarranties = async (): Promise<WarrantyListItem[]> => {
    const response = await apiClient.get<WarrantyListItem[]>(
        "/Dashboard/expiring-warranties"
    );
    return response.data;
};

export const getWarrantyReport = async (
    request: WarrantyReportRequest
): Promise<PagedResult<WarrantyListItem | WarrantyClaimReportItem>> => {
    const response = await apiClient.post<
        Omit<PagedResult<WarrantyListItem | WarrantyClaimReportItem>, "data"> & {
            items: Array<WarrantyListItem | WarrantyClaimReportItem>;
        }
    >("/Warranty/reports", request);
    return toPage(response.data);
};

export const submitWarrantyClaim = async (
    warrantyId: number,
    reportedProblem: string,
    notes?: string
): Promise<WarrantyClaim> => {
    const response = await apiClient.post<WarrantyClaim>(
        `/Warranty/${warrantyId}/claims`,
        { reportedProblem, notes }
    );
    return response.data;
};

export const updateWarrantyClaim = async (
    claimId: number,
    body: {
        status: string;
        inspectionNotes?: string;
        decisionReason?: string;
        notes?: string;
        defectiveSerial?: string;
        replacementProductId?: number | null;
        replacementSerial?: string;
    }
): Promise<WarrantyClaim> => {
    const response = await apiClient.post<WarrantyClaim>(
        `/Warranty/claims/${claimId}`,
        body
    );
    return response.data;
};
