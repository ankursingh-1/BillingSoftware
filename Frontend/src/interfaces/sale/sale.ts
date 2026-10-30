import type { InvoiceFormat } from "../company/company";
import type { PaymentMethod } from "../lookups/lookups";

export type { InvoiceFormat };

export type PaymentStatus =
    | "NotRecorded"
    | "Unpaid"
    | "Partial"
    | "Paid"
    | "Settled";

// Matches Billing.Application.DTOs.Sales.SaleListItemDto
export interface SaleListItem {
    id: number;
    invoiceNumber: string;
    customerId: number;
    customerName: string;
    saleDate: string;
    totalAmount: number;
    itemCount: number;
    returnedAmount: number;
    paidAmount: number;
    outstanding: number;
    paymentStatus: PaymentStatus;
}

export interface SaleSearchRequest {
    search?: string;
    fromDate?: string;
    toDate?: string;
    customerId?: number;
    paymentStatus?: PaymentStatus | "";
    pageNumber: number;
    pageSize: number;
}

export interface SaleCustomer {
    id: number;
    name: string;
    mobile: string;
    email: string | null;
    address: string | null;
    state: string | null;
    stateCode: string | null;
    gstin: string | null;
}

export interface InvoiceCompany {
    name: string;
    address: string | null;
    city: string | null;
    state: string | null;
    stateCode: string | null;
    pincode: string | null;
    phone: string | null;
    email: string | null;
    gstin: string | null;
    pan: string | null;
    hasLogo: boolean;
    website: string | null;
    /** Stored file for this invoice; served at /api/Company/logo/{file}. */
    logoFile: string | null;
    bankName: string | null;
    accountNumber: string | null;
    ifscCode: string | null;
    upiId: string | null;
    terms: string | null;
    footer: string | null;
    currency: string;
}

export interface SaleItemDetail {
    id: number;
    productId: number;
    productName: string;
    sku: string | null;
    hsnCode: string | null;
    quantity: number;
    unitPrice: number;
    /** Taxable (pre-tax) amount of the line. */
    total: number;
    taxName: string | null;
    taxRate: number | null;
    cgstAmount: number | null;
    sgstAmount: number | null;
    igstAmount: number | null;
    taxAmount: number | null;
    lineTotal: number | null;
    returnedQuantity: number;
    returnableQuantity: number;
}

export interface InvoiceSettings {
    defaultFormat: InvoiceFormat;
    thermalPaperWidthMm: 58 | 80;
    showBankDetails: boolean;
    showLogo: boolean;
    showTerms: boolean;
    showFooter: boolean;
}

export interface SalePayment {
    id: number;
    amount: number;
    paymentDate: string;
    method: PaymentMethod;
    reference: string | null;
    receiptNumber: string | null;
    notes: string | null;
    recordedBy: string | null;
    isVoided: boolean;
    voidedOn: string | null;
    voidReason: string | null;
}

export interface SaleReturnSummary {
    id: number;
    returnNumber: string;
    returnDate: string;
    totalAmount: number;
    refundAmount: number;
}

// Matches Billing.Application.DTOs.Sales.SaleDetailDto
export interface SaleDetail {
    id: number;
    invoiceNumber: string;
    saleDate: string;
    remarks: string | null;
    customer: SaleCustomer;
    company: InvoiceCompany;
    /** Print preferences from current company settings (not frozen on the invoice). */
    settings: InvoiceSettings;
    subtotal: number;
    cgstTotal: number;
    sgstTotal: number;
    igstTotal: number;
    taxTotal: number;
    roundOff: number;
    totalAmount: number;
    amountInWords: string;
    taxCalculated: boolean;
    isInterState: boolean;
    placeOfSupplyStateCode: string | null;
    placeOfSupplyState: string | null;
    paymentTracked: boolean;
    paymentStatus: PaymentStatus;
    paidAmount: number;
    refundedAmount: number;
    returnedAmount: number;
    outstanding: number;
    refundDue: number;
    items: SaleItemDetail[];
    payments: SalePayment[];
    returns: SaleReturnSummary[];
    warranties: SaleWarrantyLine[];
}

export interface SaleWarrantyLine {
    saleItemId: number;
    warrantyNumber: string;
    productName: string;
    serialNumber: string | null;
    duration: string;
    startDate: string;
    endDate: string;
}

export interface CreateSaleItemRequest {
    productId: number;
    quantity: number;
    /** Price the cashier saw; the server rejects the sale if it changed. */
    unitPrice?: number;
    registerWarranty?: boolean;
    warrantyDurationValue?: number | null;
    warrantyDurationUnit?: "Days" | "Months" | "Years" | null;
    warrantyStartDate?: string | null;
    warrantyNotes?: string | null;
    serialNumbers?: string[];
}

export interface CreateSalePaymentRequest {
    method: PaymentMethod;
    amount: number;
    reference?: string | null;
}

export interface CreateSaleRequest {
    customerId: number;
    items: CreateSaleItemRequest[];
    payments?: CreateSalePaymentRequest[];
    remarks?: string | null;
    clientRequestId: string;
}

export interface CreateSaleResponse {
    message: string;
    saleId: number;
    invoiceNumber: string;
    totalAmount: number;
    paidAmount: number;
    outstanding: number;
    paymentStatus: PaymentStatus;
    isDuplicate: boolean;
}

export interface RecordPaymentRequest {
    method: PaymentMethod;
    amount: number;
    reference?: string | null;
    notes?: string | null;
    startTracking?: boolean;
}