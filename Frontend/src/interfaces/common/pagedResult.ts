// Matches Billing.API.Models.PagedResult<T>
export interface PagedResult<T> {
    totalRecords: number;
    totalPages: number;
    pageNumber: number;
    pageSize: number;
    data: T[];
}
