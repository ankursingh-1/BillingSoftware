import type { ReactNode } from "react";

import { formatCurrency } from "../../utils/format";

interface TaxTotalsProps {
    subtotal: number;
    /** False for older documents that have no tax breakdown. */
    showTax: boolean;
    isInterState: boolean;
    cgstTotal: number;
    sgstTotal: number;
    igstTotal: number;
    roundOff: number;
    total: number;
    totalLabel?: string;
    /** Extra rows below the total (paid, returned, ...). */
    children?: ReactNode;
}

const Row = ({
    label,
    value,
    grand,
}: {
    label: string;
    value: string;
    grand?: boolean;
}) => (
    <div className={`row-line${grand ? " grand" : ""}`}>
        <span>{label}</span>
        <span>{value}</span>
    </div>
);

/** Subtotal / tax split / round-off / total block shared by documents. */
function TaxTotals({
    subtotal,
    showTax,
    isInterState,
    cgstTotal,
    sgstTotal,
    igstTotal,
    roundOff,
    total,
    totalLabel = "Total",
    children,
}: TaxTotalsProps) {
    return (
        <div className="invoice-totals">
            <Row label="Subtotal" value={formatCurrency(subtotal)} />

            {showTax && isInterState && (
                <Row label="IGST" value={formatCurrency(igstTotal)} />
            )}

            {showTax && !isInterState && (
                <>
                    <Row label="CGST" value={formatCurrency(cgstTotal)} />
                    <Row label="SGST" value={formatCurrency(sgstTotal)} />
                </>
            )}

            {roundOff !== 0 && (
                <Row label="Round off" value={formatCurrency(roundOff)} />
            )}

            <Row label={totalLabel} value={formatCurrency(total)} grand />

            {children}
        </div>
    );
}

export default TaxTotals;