// Matches Billing.API.Models.CustomerDto
export interface Customer {
    id: number;
    name: string;
    mobile: string;
    email: string | null;
    address: string | null;
}

// Body for POST /Customers and PUT /Customers/{id}.
// `id` is ignored by the API (the route id is used), so it is not sent.
export interface SaveCustomerRequest {
    name: string;
    mobile: string;
    email: string | null;
    address: string | null;
}

// Matches Billing.API.Models.CustomerSearchRequest
export interface CustomerSearchRequest {
    search?: string;
    pageNumber: number;
    pageSize: number;
}
