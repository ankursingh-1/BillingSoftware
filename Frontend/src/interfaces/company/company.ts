export interface Company {
    id: number;
    companyName: string;
    gstNumber: string | null;
    panNumber: string | null;
    address: string | null;
    city: string | null;
    state: string | null;
    country: string | null;
    pincode: string | null;
    phone: string | null;
    email: string | null;
    website: string | null;
    logoPath: string | null;
    invoicePrefix: string;
    currency: string;
    bankName: string | null;
    accountNumber: string | null;
    ifscCode: string | null;
    upiId: string | null;
    termsAndConditions: string | null;
    footerMessage: string | null;
    isActive: boolean;
}

export interface SaveCompanyRequest {
    companyName: string;
    gstNumber: string;
    panNumber: string;
    address: string;
    city: string;
    state: string;
    country: string;
    pincode: string;
    phone: string;
    email: string;
    website: string;
    logoPath: string;
    invoicePrefix: string;
    currency: string;
    bankName: string;
    accountNumber: string;
    ifscCode: string;
    upiId: string;
    termsAndConditions: string;
    footerMessage: string;
    isActive: boolean;
}