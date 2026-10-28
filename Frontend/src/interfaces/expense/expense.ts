export interface Expense {
    id: number;
    title: string;
    amount: number;
    expenseDate: string;
    category: string | null;
    remarks: string | null;
}

export interface SaveExpenseRequest {
    title: string;
    amount: number;
    expenseDate: string;
    category: string | null;
    remarks: string | null;
}

export interface ExpenseSearchRequest {
    search?: string;
    fromDate?: string;
    toDate?: string;
    category?: string;
    pageNumber: number;
    pageSize: number;
}
