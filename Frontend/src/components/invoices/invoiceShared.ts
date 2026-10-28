import { API_URL } from "../../config/api";

import type { InvoiceCompany, SaleDetail } from "../../interfaces/sale/sale";

export const dash = "—";

export function present(
    ...parts: Array<string | null | undefined>
): string | null {
    const value = parts.filter((part) => part && part.trim()).join(" · ");

    return value.length > 0 ? value : null;
}

export function companyAddress(company: InvoiceCompany): string[] {
    const lines: string[] = [];

    if (company.address) {
        lines.push(company.address);
    }

    const locality = [company.city, company.state, company.pincode]
        .filter(Boolean)
        .join(", ");

    if (locality) {
        lines.push(locality);
    }

    return lines;
}

export function companyLogoSrc(company: InvoiceCompany): string | null {
    if (company.logoFile) {
        return `${API_URL}/Company/logo/${encodeURIComponent(company.logoFile)}`;
    }

    if (company.hasLogo) {
        return `${API_URL}/Company/logo`;
    }

    return null;
}

export function paymentStamp(sale: SaleDetail): string | null {
    if (!sale.paymentTracked || sale.paymentStatus === "NotRecorded") {
        return null;
    }

    if (sale.paymentStatus === "Paid" || sale.paymentStatus === "Settled") {
        return "PAID";
    }

    if (sale.paymentStatus === "Partial") {
        return "PARTLY PAID";
    }

    if (sale.paymentStatus === "Unpaid") {
        return "UNPAID";
    }

    return null;
}

export function taxRateLabel(rate: number | null | undefined): string {
    if (rate === null || rate === undefined) {
        return dash;
    }

    return `${rate % 1 === 0 ? rate.toFixed(0) : rate.toFixed(2)}%`;
}
