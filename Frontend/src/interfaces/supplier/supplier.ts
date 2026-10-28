// Matches Billing.API.Models.SupplierDto
export interface Supplier {
    id: number;
    name: string;
    companyName: string;
    mobile: string;
    email: string | null;
    address: string | null;
    state: string | null;
    stateCode: string | null;
    gstin: string | null;
}

// Body for POST /Suppliers and PUT /Suppliers/{id}.
// `id` is ignored by the API (the route id is used), so it is not sent.
export interface SaveSupplierRequest {
    name: string;
    companyName: string;
    mobile: string;
    email: string | null;
    address: string | null;
    stateCode: string | null;
    gstin: string | null;
}

// Matches Billing.API.Models.SupplierSearchRequest
export interface SupplierSearchRequest {
    search?: string;
    pageNumber: number;
    pageSize: number;
}
