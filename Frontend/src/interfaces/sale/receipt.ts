import type { InvoiceCompany, InvoiceSettings, SaleCustomer } from "./sale";

/** Matches Billing.Application.DTOs.Sales.PaymentReceiptDto */
export interface PaymentReceipt {
    paymentId: number;
    receiptNumber: string;
    paymentDate: string;
    method: string;
    amount: number;
    reference: string | null;
    notes: string | null;
    receivedBy: string | null;
    status: "Received" | "Voided" | string;
    isVoided: boolean;
    voidedOn: string | null;
    voidReason: string | null;
    saleId: number;
    invoiceNumber: string;
    invoiceDate: string;
    invoiceTotal: number;
    previouslyPaid: number;
    balanceAfter: number;
    /** True only when this payment cleared the invoice. */
    settlesInvoice: boolean;
    description: string;
    amountInWords: string;
    customer: SaleCustomer;
    company: InvoiceCompany;
    settings: InvoiceSettings;
}
