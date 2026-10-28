import type { PaymentMethod } from "../lookups/lookups";

export interface ReturnItemDetail {
    id: number;
    productId: number;
    productName: string;
    sku: string | null;
    hsnCode: string | null;
    quantity: number;
    unitPrice: number;
    taxableAmount: number;
    taxRate: number;
    cgstAmount: number;
    sgstAmount: number;
    igstAmount: number;
    taxAmount: number;
    total: number;
}

export interface SalesReturnListItem {
    id: number;
    returnNumber: string;
    returnDate: string;
    saleId: number;
    invoiceNumber: string;
    customerName: string;
    totalAmount: number;
    refundAmount: number;
}

export interface SalesReturnDetail {
    id: number;
    returnNumber: string;
    returnDate: string;
    reason: string | null;
    createdBy: string | null;
    saleId: number;
    invoiceNumber: string;
    saleDate: string;
    customerName: string;
    customerGstin: string | null;
    isInterState: boolean;
    subtotal: number;
    cgstTotal: number;
    sgstTotal: number;
    igstTotal: number;
    taxTotal: number;
    roundOff: number;
    totalAmount: number;
    refundAmount: number;
    refundMethod: string | null;
    refundDue: number;
    items: ReturnItemDetail[];
}

export interface PurchaseReturnListItem {
    id: number;
    returnNumber: string;
    returnDate: string;
    purchaseId: number;
    purchaseNumber: string;
    supplierName: string;
    totalAmount: number;
    refundReceived: number;
}

export interface PurchaseReturnDetail {
    id: number;
    returnNumber: string;
    returnDate: string;
    reason: string | null;
    createdBy: string | null;
    purchaseId: number;
    purchaseNumber: string;
    supplierInvoiceNumber: string | null;
    supplierName: string;
    supplierGstin: string | null;
    isInterState: boolean;
    subtotal: number;
    cgstTotal: number;
    sgstTotal: number;
    igstTotal: number;
    taxTotal: number;
    roundOff: number;
    totalAmount: number;
    refundReceived: number;
    refundMethod: string | null;
    adjustmentNote: string | null;
    items: ReturnItemDetail[];
}

export interface ReturnSearchRequest {
    search?: string;
    fromDate?: string;
    toDate?: string;
    pageNumber: number;
    pageSize: number;
}

export interface CreateSalesReturnRequest {
    saleId: number;
    items: { saleItemId: number; quantity: number }[];
    reason: string;
    refundAmount: number;
    refundMethod?: PaymentMethod;
}

export interface CreatePurchaseReturnRequest {
    purchaseId: number;
    items: { purchaseItemId: number; quantity: number }[];
    reason: string;
    refundReceived: number;
    refundMethod?: PaymentMethod;
    adjustmentNote?: string | null;
}

export interface PurchaseReturnResponse {
    message?: string;
    returnId: number;
    returnNumber: string;
    totalAmount: number;
    refundReceived: number;
}

export interface SalesReturnResponse {
    message?: string;
    returnId: number;
    returnNumber: string;
    totalAmount: number;
    refundAmount: number;
    refundDue: number;
    outstanding: number;
}