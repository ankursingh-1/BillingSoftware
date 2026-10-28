export interface Brand {
    id: number;
    name: string;
    description: string | null;
    isActive: boolean;
}

export interface Unit {
    id: number;
    name: string;
    shortName: string | null;
    description: string | null;
    isActive: boolean;
}

export interface Tax {
    id: number;
    name: string;
    percentage: number;
    description: string | null;
    isActive: boolean;
}

export interface GstState {
    code: string;
    name: string;
}

export interface PaymentMethodOption {
    value: PaymentMethod;
    label: string;
}

export type PaymentMethod =
    | "Cash"
    | "Upi"
    | "Card"
    | "BankTransfer"
    | "Cheque"
    | "Other";