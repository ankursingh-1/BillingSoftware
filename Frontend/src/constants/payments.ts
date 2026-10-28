import type { PaymentMethod } from "../interfaces/lookups/lookups";

// Mirrors Billing.Domain.Enums.PaymentMethod (serialised as strings).
export const PAYMENT_METHODS: { value: PaymentMethod; label: string }[] = [
    { value: "Cash", label: "Cash" },
    { value: "Upi", label: "UPI" },
    { value: "Card", label: "Card" },
    { value: "BankTransfer", label: "Bank Transfer" },
    { value: "Cheque", label: "Cheque" },
    { value: "Other", label: "Other" },
];

export const paymentMethodLabel = (method: string): string =>
    PAYMENT_METHODS.find((m) => m.value === method)?.label ?? method;