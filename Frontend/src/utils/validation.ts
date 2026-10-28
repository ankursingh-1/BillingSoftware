// Mirrors the server-side rules in Billing.API.Models.CustomerDto / SupplierDto.

/** Optional leading +, then 7 to 15 digits. */
export const MOBILE_PATTERN = /^\+?[0-9]{7,15}$/;

/** Pragmatic email check; the server performs its own validation too. */
export const EMAIL_PATTERN = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;

/** Removes the spaces and hyphens people type into phone numbers. */
export function normalizeMobile(value: string): string {
    return value.replace(/[\s-]/g, "");
}

/** Trims; returns null for blank so optional fields are stored as NULL. */
export function emptyToNull(value: string): string | null {
    const trimmed = value.trim();

    return trimmed.length > 0 ? trimmed : null;
}
