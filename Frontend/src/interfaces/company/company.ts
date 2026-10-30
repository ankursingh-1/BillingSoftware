export type InvoiceFormat = "A4" | "Compact" | "Thermal";

export type ShopType = "GeneralRetail" | "Electronics" | "Grocery";

export interface Company {
    id: number;
    companyName: string;
    gstNumber: string | null;
    panNumber: string | null;
    address: string | null;
    city: string | null;
    state: string | null;
    stateCode: string | null;
    country: string | null;
    pincode: string | null;
    phone: string | null;
    email: string | null;
    website: string | null;
    hasLogo: boolean;
    invoicePrefix: string;
    currency: string;
    bankName: string | null;
    accountNumber: string | null;
    ifscCode: string | null;
    upiId: string | null;
    termsAndConditions: string | null;
    footerMessage: string | null;
    defaultInvoiceFormat: InvoiceFormat;
    thermalPaperWidthMm: 58 | 80;
    showBankDetails: boolean;
    showLogo: boolean;
    showTerms: boolean;
    showFooter: boolean;
    isActive: boolean;
    shopType: ShopType;
}

/** Body for POST /Company. Empty optional fields are sent as null. */
export interface SaveCompanyRequest {
    companyName: string;
    gstNumber: string | null;
    panNumber: string | null;
    address: string | null;
    city: string | null;
    state: string | null;
    stateCode: string | null;
    country: string | null;
    pincode: string | null;
    phone: string | null;
    email: string | null;
    website: string | null;
    invoicePrefix: string;
    currency: string;
    bankName: string | null;
    accountNumber: string | null;
    ifscCode: string | null;
    upiId: string | null;
    termsAndConditions: string | null;
    footerMessage: string | null;
    defaultInvoiceFormat: InvoiceFormat;
    thermalPaperWidthMm: 58 | 80;
    showBankDetails: boolean;
    showLogo: boolean;
    showTerms: boolean;
    showFooter: boolean;
    isActive: boolean;
    shopType: ShopType;
}
