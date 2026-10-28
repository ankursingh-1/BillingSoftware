import { useState } from "react";

import { paymentMethodLabel } from "../../constants/payments";
import { formatCurrency, formatDate, formatDateTime } from "../../utils/format";

import type { InvoiceFormat } from "../../interfaces/company/company";
import type { PaymentReceipt } from "../../interfaces/sale/receipt";

import { companyAddress, companyLogoSrc, present } from "./invoiceShared";

interface ReceiptViewProps {
    receipt: PaymentReceipt;
    format: InvoiceFormat;
    thermalWidthMm: 58 | 80;
}

function Logo({
    name,
    src,
}: {
    name: string;
    src: string | null;
}) {
    const [failed, setFailed] = useState(false);

    if (!src || failed) {
        return null;
    }

    return (
        <img
            className="inv-logo"
            src={src}
            alt={`${name} logo`}
            onError={() => setFailed(true)}
        />
    );
}

function Banner({ receipt }: { receipt: PaymentReceipt }) {
    if (receipt.isVoided) {
        return (
            <div className="rcp-banner rcp-voided" role="status">
                VOIDED
                {receipt.voidReason ? ` — ${receipt.voidReason}` : ""}
            </div>
        );
    }

    if (receipt.settlesInvoice) {
        return (
            <div className="rcp-banner rcp-final" role="status">
                Final payment — invoice settled
            </div>
        );
    }

    return (
        <div className="rcp-banner rcp-part" role="status">
            Part payment — balance {formatCurrency(receipt.balanceAfter)} still
            due
        </div>
    );
}

function ReceiptView({ receipt, format, thermalWidthMm }: ReceiptViewProps) {
    const company = receipt.company;
    const logo =
        receipt.settings.showLogo && !receipt.isVoided
            ? companyLogoSrc(company)
            : null;
    const thermal = format === "Thermal";
    const contact = present(company.phone && `Ph: ${company.phone}`);

    return (
        <article
            className={`invoice-sheet rcp-sheet${thermal ? ` inv-thermal paper-${thermalWidthMm}` : " rcp-a5"}`}
            aria-label="Payment receipt"
        >
            <header className={thermal ? "text-center" : "invoice-head"}>
                <div className="inv-company">
                    <Logo name={company.name} src={logo} />
                    <div>
                        <h3>{company.name}</h3>
                        {!thermal &&
                            companyAddress(company).map((line) => (
                                <p key={line}>{line}</p>
                            ))}
                        {contact && <p>{contact}</p>}
                    </div>
                </div>
                <div className="invoice-meta">
                    <div className="invoice-title">PAYMENT RECEIPT</div>
                    <p>
                        Receipt No: <strong>{receipt.receiptNumber}</strong>
                    </p>
                    <p>Date: {formatDateTime(receipt.paymentDate)}</p>
                </div>
            </header>

            {thermal && <div className="inv-dash" />}

            <table className="rcp-grid">
                <tbody>
                    <tr>
                        <th>Received from</th>
                        <td>{receipt.customer.name}</td>
                    </tr>
                    <tr>
                        <th>Against invoice</th>
                        <td>
                            {receipt.invoiceNumber} (
                            {formatDate(receipt.invoiceDate)})
                        </td>
                    </tr>
                    <tr>
                        <th>Payment method</th>
                        <td>{paymentMethodLabel(receipt.method)}</td>
                    </tr>
                    {receipt.reference && (
                        <tr>
                            <th>Reference</th>
                            <td>{receipt.reference}</td>
                        </tr>
                    )}
                    {receipt.receivedBy && (
                        <tr>
                            <th>Received by</th>
                            <td>{receipt.receivedBy}</td>
                        </tr>
                    )}
                </tbody>
            </table>

            <div className="rcp-amounts">
                <div className="d-flex justify-content-between">
                    <span>Invoice total</span>
                    <span>{formatCurrency(receipt.invoiceTotal)}</span>
                </div>
                <div className="d-flex justify-content-between">
                    <span>Paid earlier</span>
                    <span>{formatCurrency(receipt.previouslyPaid)}</span>
                </div>
                <div className="d-flex justify-content-between rcp-received">
                    <span>Amount received</span>
                    <span>{formatCurrency(receipt.amount)}</span>
                </div>
                <div className="d-flex justify-content-between">
                    <span>Balance after this payment</span>
                    <span>{formatCurrency(receipt.balanceAfter)}</span>
                </div>
            </div>

            <p className="mt-2">{receipt.amountInWords}</p>

            <Banner receipt={receipt} />

            {receipt.notes && <p className="small mt-2">{receipt.notes}</p>}

            {receipt.settings.showFooter && company.footer && (
                <footer className="invoice-footer text-center">
                    {company.footer}
                    <div className="small text-muted mt-1">
                        This is a payment receipt, not a tax invoice.
                    </div>
                </footer>
            )}
        </article>
    );
}

export default ReceiptView;
