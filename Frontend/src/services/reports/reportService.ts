import apiClient from "../api/apiClient";

const saveBlob = (data: Blob, fileName: string): void => {
    const url = URL.createObjectURL(data);
    const link = document.createElement("a");
    link.href = url;
    link.download = fileName;
    document.body.appendChild(link);
    link.click();
    link.remove();
    URL.revokeObjectURL(url);
};

export type ReportKind =
    | "sales"
    | "purchases"
    | "stock"
    | "profit"
    | "expenses"
    | "customers"
    | "suppliers"
    | "payments";

export interface ReportQuery {
    fromDate?: string;
    toDate?: string;
    search?: string;
    pageNumber: number;
    pageSize: number;
    lowStockOnly?: boolean;
    outOfStockOnly?: boolean;
}

export interface SalesReportRow {
    saleId: number;
    invoiceNumber: string;
    customerName: string;
    saleDate: string;
    totalAmount: number;
}

export interface PurchaseReportRow {
    purchaseId: number;
    purchaseNumber: string;
    supplierName: string;
    purchaseDate: string;
    totalAmount: number;
}

export interface StockReportRow {
    productId: number;
    productName: string;
    sku: string;
    currentStock: number;
    purchasePrice: number;
    sellingPrice: number;
    stockValue: number;
}

export interface ProfitReport {
    totalSales: number;
    costOfGoodsSold: number;
    grossProfit: number | null;
    profitPercentage: number | null;
    linesWithCost: number;
    linesMissingCost: number;
    costComplete: boolean;
    limitation: string | null;
    items: {
        invoiceNumber: string;
        saleDate: string;
        saleAmount: number;
        costOfGoodsSold: number | null;
        profit: number | null;
        costComplete: boolean;
    }[];
}

export interface ExpenseReportRow {
    id: number;
    title: string;
    amount: number;
    expenseDate: string;
    category: string | null;
}

export interface CustomerReportRow {
    customerId: number;
    name: string;
    mobile: string;
    gstin: string | null;
    invoiceCount: number;
    billed: number;
    outstanding: number;
}

export interface SupplierReportRow {
    supplierId: number;
    name: string;
    companyName: string;
    mobile: string;
    gstin: string | null;
    purchaseCount: number;
    purchased: number;
}

export interface PaymentReportRow {
    paymentId: number;
    receiptNumber: string | null;
    invoiceNumber: string;
    customerName: string;
    paymentDate: string;
    method: string;
    amount: number;
    reference: string | null;
}

interface PagedMoney<T> {
    items: T[];
    grandTotal?: number;
    totalStockValue?: number;
    totalBilled?: number;
    totalOutstanding?: number;
    totalPurchased?: number;
    totalCollected?: number;
    limitation?: string;
}

export const getSalesReport = async (q: ReportQuery) =>
    (await apiClient.post<PagedMoney<SalesReportRow>>("/Reports/sales", q)).data;

export const getPurchaseReport = async (q: ReportQuery) =>
    (await apiClient.post<PagedMoney<PurchaseReportRow>>("/Reports/purchases", q))
        .data;

export const getStockReport = async (q: ReportQuery) =>
    (await apiClient.post<PagedMoney<StockReportRow>>("/Reports/stock", q)).data;

export const getProfitReport = async (q: ReportQuery) =>
    (await apiClient.post<ProfitReport>("/Reports/profit", q)).data;

export const getExpenseReport = async (q: ReportQuery) =>
    (await apiClient.post<PagedMoney<ExpenseReportRow>>("/Reports/expenses", q))
        .data;

export const getCustomerReport = async (q: ReportQuery) =>
    (await apiClient.post<PagedMoney<CustomerReportRow>>("/Reports/customers", q))
        .data;

export const getSupplierReport = async (q: ReportQuery) =>
    (await apiClient.post<PagedMoney<SupplierReportRow>>("/Reports/suppliers", q))
        .data;

export const getPaymentReport = async (q: ReportQuery) =>
    (await apiClient.post<PagedMoney<PaymentReportRow>>("/Reports/payments", q))
        .data;

export const downloadReportExcel = async (
    kind: ReportKind,
    query: ReportQuery
): Promise<void> => {
    const response = await apiClient.post<Blob>(`/Reports/${kind}/excel`, query, {
        responseType: "blob",
    });

    saveBlob(response.data, `${kind}-report.xlsx`);
};
