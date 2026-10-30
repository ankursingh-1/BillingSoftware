export type WarrantyStatus =
    | "Active"
    | "ExpiringSoon"
    | "Expired"
    | "Voided";

export type WarrantyClaimStatus =
    | "Submitted"
    | "UnderInspection"
    | "Approved"
    | "Rejected"
    | "Repaired"
    | "ReplacementIssued"
    | "Closed";

export interface WarrantyListItem {
    id: number;
    warrantyNumber: string;
    invoiceNumber: string;
    saleId: number;
    productName: string;
    serialNumber: string | null;
    customerName: string;
    customerMobile: string;
    duration: string;
    startDate: string;
    endDate: string;
    daysRemaining: number;
    status: WarrantyStatus;
    eligible: boolean;
}

export interface WarrantyDetail extends WarrantyListItem {
    saleItemId: number;
    productId: number;
    productSku: string | null;
    customerId: number | null;
    durationValue: number;
    durationUnit: string;
    notes: string | null;
    isVoided: boolean;
    eligibilityNote: string;
    claims: WarrantyClaim[];
}

export interface WarrantyClaim {
    id: number;
    claimNumber: string;
    status: WarrantyClaimStatus;
    reportedProblem: string;
    inspectionNotes: string | null;
    decisionReason: string | null;
    notes: string | null;
    defectiveSerial: string | null;
    replacementProductId: number | null;
    replacementProductName: string | null;
    replacementSerial: string | null;
    replacementStockMoved: boolean;
    submittedOn: string;
    resolvedOn: string | null;
    events: WarrantyClaimEvent[];
}

export interface WarrantyClaimEvent {
    status: string;
    notes: string | null;
    at: string;
    by: string | null;
}

export interface WarrantySearchRequest {
    search?: string;
    status?: string;
    fromDate?: string;
    toDate?: string;
    pageNumber: number;
    pageSize: number;
}

export interface WarrantyReportRequest {
    kind: string;
    search?: string;
    fromDate?: string;
    toDate?: string;
    pageNumber: number;
    pageSize: number;
}

export interface WarrantyClaimReportItem {
    warrantyId: number;
    saleId: number;
    claimNumber: string;
    warrantyNumber: string;
    invoiceNumber: string;
    customerName: string;
    productName: string;
    status: string;
    submittedOn: string;
    replacementProductName: string | null;
}
