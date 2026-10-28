// Matches Billing.API.Models.CustomerDto
export interface Customer {
    id: number;
    name: string;
    mobile: string;
    email: string | null;
    address: string | null;
    state: string | null;
    stateCode: string | null;
    gstin: string | null;
}

// Body for POST /Customers and PUT /Customers/{id}.
// `id` is ignored by the API (the route id is used), so it is not sent.
// The server derives the state name from `stateCode` (or the GSTIN prefix).
export interface SaveCustomerRequest {
    name: string;
    mobile: string;
    email: string | null;
    address: string | null;
    stateCode: string | null;
    gstin: string | null;
}

// Matches Billing.API.Models.CustomerSearchRequest
export interface CustomerSearchRequest {
    search?: string;
    pageNumber: number;
    pageSize: number;
}