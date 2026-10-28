import { useState } from "react";

import TaxTotals from "../common/TaxTotals";

import { formatCurrency, formatDate } from "../../utils/format";

import type { InvoiceFormat } from "../../interfaces/company/company";
import type { InvoiceCompany, SaleDetail } from "../../interfaces/sale/sale";

import {
    companyAddress,
    companyLogoSrc,
    paymentStamp,
    present,
    taxRateLabel,
} from "./invoiceShared";

interface InvoiceViewProps {
    sale: SaleDetail;
    format: InvoiceFormat;
    thermalWidthMm: 58 | 80;
}

function Logo({
    company,
    show,
}: {
    company: InvoiceCompany;
    show: boolean;
}) {
    const src = show ? companyLogoSrc(company) : null;
    const [failed, setFailed] = useState(false);

    if (!src || failed) {
        return null;
    }

    return (
        <img
            className="inv-logo"
            src={src}
            alt={`${company.name} logo`}
            onError={() => setFailed(true)}
        />
    );
}

function CompanyBlock({
    sale,
    compact,
}: {
    sale: SaleDetail;
    compact?: boolean;
}) {
    const company = sale.company;
    const contact = present(
        company.phone && `Ph: ${company.phone}`,
        company.email && `Email: ${company.email}`
    );
    const tax = present(
        company.gstin && `GSTIN: ${company.gstin}`,
        company.pan && `PAN: ${company.pan}`
    );

    return (
        <div className="inv-company">
            <Logo company={company} show={sale.settings.showLogo} />
            <div>
                <h3>{company.name}</h3>
                {companyAddress(company).map((line) => (
                    <p key={line}>{line}</p>
                ))}
                {contact && <p>{contact}</p>}
                {tax && <p>{tax}</p>}
                {!compact && company.website && <p>{company.website}</p>}
            </div>
        </div>
    );
}

function BankBlock({ company }: { company: InvoiceCompany }) {
    const lines = [
        company.bankName && `Bank: ${company.bankName}`,
        company.accountNumber && `A/c: ${company.accountNumber}`,
        company.ifscCode && `IFSC: ${company.ifscCode}`,
        company.upiId && `UPI: ${company.upiId}`,
    ].filter(Boolean) as string[];

    if (lines.length === 0) {
        return null;
    }

    return (
        <div>
            <div className="inv-label">Bank details</div>
            {lines.map((line) => (
                <div key={line}>{line}</div>
            ))}
        </div>
    );
}

function Totals({ sale }: { sale: SaleDetail }) {
    return (
        <TaxTotals
            subtotal={sale.subtotal}
            showTax={sale.taxCalculated}
            isInterState={sale.isInterState}
            cgstTotal={sale.cgstTotal}
            sgstTotal={sale.sgstTotal}
            igstTotal={sale.igstTotal}
            roundOff={sale.roundOff}
            total={sale.totalAmount}
            totalLabel="Grand total"
        >
            {sale.paymentTracked && (
                <>
                    <div className="row-line">
                        <span>Paid</span>
                        <span>
                            {formatCurrency(
                                sale.paidAmount - sale.refundedAmount
                            )}
                        </span>
                    </div>
                    {sale.returnedAmount > 0 && (
                        <div className="row-line">
                            <span>Returned</span>
                            <span>−{formatCurrency(sale.returnedAmount)}</span>
                        </div>
                    )}
                    <div className="row-line fw-semibold">
                        <span>Balance due</span>
                        <span>{formatCurrency(sale.outstanding)}</span>
                    </div>
                    {sale.refundDue > 0 && (
                        <div className="row-line text-danger fw-semibold">
                            <span>Refund due</span>
                            <span>{formatCurrency(sale.refundDue)}</span>
                        </div>
                    )}
                </>
            )}
        </TaxTotals>
    );
}

function A4Invoice({ sale }: { sale: SaleDetail }) {
    const { customer, company } = sale;
    const isTaxInvoice = sale.taxCalculated && !!company.gstin;
    const stamp = paymentStamp(sale);
    const intra = sale.taxCalculated && !sale.isInterState;
    const inter = sale.taxCalculated && sale.isInterState;

    return (
        <article className="invoice-sheet inv-a4" aria-label="Invoice">
            <header className="invoice-head">
                <CompanyBlock sale={sale} />
                <div className="invoice-meta">
                    <div className="invoice-title">
                        {isTaxInvoice ? "TAX INVOICE" : "INVOICE"}
                    </div>
                    <p>
                        Invoice No: <strong>{sale.invoiceNumber}</strong>
                    </p>
                    <p>Date: {formatDate(sale.saleDate)}</p>
                    {stamp && (
                        <p className={`inv-stamp inv-stamp-${sale.paymentStatus.toLowerCase()}`}>
                            {stamp}
                        </p>
                    )}
                </div>
            </header>

            <section className="invoice-parties">
                <div>
                    <div className="inv-label">Bill to</div>
                    <strong>{customer.name}</strong>
                    {customer.mobile && <div>Mobile: {customer.mobile}</div>}
                    {customer.email && <div>{customer.email}</div>}
                    {customer.address && (
                        <div style={{ whiteSpace: "pre-line" }}>
                            {customer.address}
                        </div>
                    )}
                    {customer.gstin && <div>GSTIN: {customer.gstin}</div>}
                    {(customer.state || customer.stateCode) && (
                        <div>
                            State: {customer.state}
                            {customer.stateCode && ` (${customer.stateCode})`}
                        </div>
                    )}
                </div>

                <div>
                    <div className="inv-label">Supply details</div>
                    <div>Invoice No: {sale.invoiceNumber}</div>
                    <div>Invoice Date: {formatDate(sale.saleDate)}</div>
                    {sale.taxCalculated ? (
                        <>
                            <div>
                                Place of supply: {sale.placeOfSupplyState}
                                {sale.placeOfSupplyStateCode &&
                                    ` (${sale.placeOfSupplyStateCode})`}
                            </div>
                            <div>
                                {sale.isInterState
                                    ? "Inter-state supply (IGST)"
                                    : "Intra-state supply (CGST + SGST)"}
                            </div>
                        </>
                    ) : (
                        <div className="text-muted">
                            Tax details were not recorded for this older
                            invoice.
                        </div>
                    )}
                </div>
            </section>

            <table className="table invoice-table align-middle mb-0">
                <thead>
                    <tr>
                        <th>#</th>
                        <th>Description</th>
                        {sale.taxCalculated && <th>HSN</th>}
                        <th className="text-end">Qty</th>
                        <th className="text-end">Unit price</th>
                        {sale.taxCalculated && (
                            <>
                                <th className="text-end">Taxable</th>
                                <th className="text-end">GST %</th>
                                {intra && (
                                    <>
                                        <th className="text-end">CGST</th>
                                        <th className="text-end">SGST</th>
                                    </>
                                )}
                                {inter && <th className="text-end">IGST</th>}
                            </>
                        )}
                        <th className="text-end">Amount</th>
                    </tr>
                </thead>
                <tbody>
                    {sale.items.map((item, index) => (
                        <tr key={item.id}>
                            <td>{index + 1}</td>
                            <td>
                                {item.productName}
                                {item.sku && (
                                    <div className="small text-muted">
                                        SKU: {item.sku}
                                    </div>
                                )}
                            </td>
                            {sale.taxCalculated && (
                                <td>{item.hsnCode ?? "—"}</td>
                            )}
                            <td className="text-end">{item.quantity}</td>
                            <td className="text-end text-nowrap">
                                {formatCurrency(item.unitPrice)}
                            </td>
                            {sale.taxCalculated && (
                                <>
                                    <td className="text-end text-nowrap">
                                        {formatCurrency(item.total)}
                                    </td>
                                    <td className="text-end">
                                        {taxRateLabel(item.taxRate)}
                                    </td>
                                    {intra && (
                                        <>
                                            <td className="text-end text-nowrap">
                                                {formatCurrency(
                                                    item.cgstAmount ?? 0
                                                )}
                                            </td>
                                            <td className="text-end text-nowrap">
                                                {formatCurrency(
                                                    item.sgstAmount ?? 0
                                                )}
                                            </td>
                                        </>
                                    )}
                                    {inter && (
                                        <td className="text-end text-nowrap">
                                            {formatCurrency(
                                                item.igstAmount ?? 0
                                            )}
                                        </td>
                                    )}
                                </>
                            )}
                            <td className="text-end text-nowrap">
                                {formatCurrency(item.lineTotal ?? item.total)}
                            </td>
                        </tr>
                    ))}
                </tbody>
            </table>

            <section className="invoice-summary">
                <div>
                    <div className="inv-label">Amount in words</div>
                    <div>{sale.amountInWords}</div>
                    {sale.remarks && (
                        <>
                            <div className="inv-label mt-3">Remarks</div>
                            <div>{sale.remarks}</div>
                        </>
                    )}
                    {sale.settings.showBankDetails && (
                        <div className="mt-3">
                            <BankBlock company={company} />
                        </div>
                    )}
                </div>
                <Totals sale={sale} />
            </section>

            {sale.settings.showTerms && company.terms && (
                <div className="inv-terms">
                    <div className="inv-label">Terms &amp; conditions</div>
                    <div>{company.terms}</div>
                </div>
            )}

            {sale.settings.showFooter && company.footer && (
                <footer className="invoice-footer text-center">
                    {company.footer}
                </footer>
            )}
        </article>
    );
}

function CompactInvoice({ sale }: { sale: SaleDetail }) {
    const { customer, company } = sale;
    const stamp = paymentStamp(sale);
    const contact = present(customer.name, customer.mobile);

    return (
        <article className="invoice-sheet inv-compact" aria-label="Invoice">
            <header className="inv-compact-head">
                <CompanyBlock sale={sale} compact />
                <div className="invoice-meta">
                    <div className="invoice-title">
                        {sale.taxCalculated && company.gstin
                            ? "TAX INVOICE"
                            : "INVOICE"}
                    </div>
                    <p>
                        <strong>{sale.invoiceNumber}</strong>
                    </p>
                    <p>{formatDate(sale.saleDate)}</p>
                    {stamp && (
                        <p className={`inv-stamp inv-stamp-${sale.paymentStatus.toLowerCase()}`}>
                            {stamp}
                        </p>
                    )}
                </div>
            </header>

            <div className="inv-compact-cust">
                <span>
                    <strong>Customer:</strong> {contact}
                </span>
                {customer.gstin && <span>GSTIN: {customer.gstin}</span>}
            </div>

            <table className="table invoice-table align-middle mb-0">
                <thead>
                    <tr>
                        <th>Item</th>
                        <th className="text-end">Qty</th>
                        <th className="text-end">Rate</th>
                        {sale.taxCalculated && <th className="text-end">GST</th>}
                        <th className="text-end">Amount</th>
                    </tr>
                </thead>
                <tbody>
                    {sale.items.map((item) => (
                        <tr key={item.id}>
                            <td>
                                {item.productName}
                                <div className="small text-muted">
                                    {[item.sku, item.hsnCode && `HSN ${item.hsnCode}`]
                                        .filter(Boolean)
                                        .join(" · ")}
                                </div>
                            </td>
                            <td className="text-end">{item.quantity}</td>
                            <td className="text-end text-nowrap">
                                {formatCurrency(item.unitPrice)}
                            </td>
                            {sale.taxCalculated && (
                                <td className="text-end">
                                    {taxRateLabel(item.taxRate)}
                                </td>
                            )}
                            <td className="text-end text-nowrap">
                                {formatCurrency(item.lineTotal ?? item.total)}
                            </td>
                        </tr>
                    ))}
                </tbody>
            </table>

            <section className="invoice-summary">
                <div>
                    {stamp && (
                        <div className={`inv-stamp inv-stamp-${sale.paymentStatus.toLowerCase()} mb-2`}>
                            {stamp}
                        </div>
                    )}
                    <div>{sale.amountInWords}</div>
                    {sale.settings.showTerms && company.terms && (
                        <div className="small text-muted mt-2">{company.terms}</div>
                    )}
                </div>
                <Totals sale={sale} />
            </section>

            {sale.settings.showFooter && company.footer && (
                <footer className="invoice-footer text-center">
                    {company.footer}
                </footer>
            )}
        </article>
    );
}

function ThermalInvoice({
    sale,
    width,
}: {
    sale: SaleDetail;
    width: 58 | 80;
}) {
    const { customer, company } = sale;
    const stamp = paymentStamp(sale);

    return (
        <article
            className={`invoice-sheet inv-thermal paper-${width}`}
            aria-label="Invoice"
        >
            <header className="text-center">
                <Logo company={company} show={sale.settings.showLogo} />
                <h3>{company.name}</h3>
                {companyAddress(company).map((line) => (
                    <p key={line}>{line}</p>
                ))}
                {company.phone && <p>Ph: {company.phone}</p>}
                {company.gstin && <p>GSTIN: {company.gstin}</p>}
                <div className="inv-dash" />
                <strong>
                    {sale.taxCalculated && company.gstin
                        ? "TAX INVOICE"
                        : "INVOICE"}
                </strong>
                <p>{sale.invoiceNumber}</p>
                <p>{formatDate(sale.saleDate)}</p>
                {stamp && <p className="fw-bold">{stamp}</p>}
                <div className="inv-dash" />
            </header>

            <p>
                <strong>{customer.name}</strong>
                {customer.mobile && <span> · {customer.mobile}</span>}
            </p>
            {customer.gstin && <p>GSTIN: {customer.gstin}</p>}

            <div className="inv-dash" />

            {sale.items.map((item) => (
                <div key={item.id} className="inv-thermal-line">
                    <div>{item.productName}</div>
                    <div className="d-flex justify-content-between">
                        <span>
                            {item.quantity} × {formatCurrency(item.unitPrice)}
                            {sale.taxCalculated &&
                                `  ${taxRateLabel(item.taxRate)}`}
                        </span>
                        <span>
                            {formatCurrency(item.lineTotal ?? item.total)}
                        </span>
                    </div>
                </div>
            ))}

            <div className="inv-dash" />

            {sale.taxCalculated && (
                <>
                    <div className="d-flex justify-content-between">
                        <span>Taxable</span>
                        <span>{formatCurrency(sale.subtotal)}</span>
                    </div>
                    {sale.isInterState ? (
                        <div className="d-flex justify-content-between">
                            <span>IGST</span>
                            <span>{formatCurrency(sale.igstTotal)}</span>
                        </div>
                    ) : (
                        <>
                            <div className="d-flex justify-content-between">
                                <span>CGST</span>
                                <span>{formatCurrency(sale.cgstTotal)}</span>
                            </div>
                            <div className="d-flex justify-content-between">
                                <span>SGST</span>
                                <span>{formatCurrency(sale.sgstTotal)}</span>
                            </div>
                        </>
                    )}
                    {sale.roundOff !== 0 && (
                        <div className="d-flex justify-content-between">
                            <span>Round off</span>
                            <span>{formatCurrency(sale.roundOff)}</span>
                        </div>
                    )}
                </>
            )}

            <div className="d-flex justify-content-between fw-bold fs-6">
                <span>TOTAL</span>
                <span>{formatCurrency(sale.totalAmount)}</span>
            </div>

            {sale.paymentTracked && (
                <>
                    <div className="d-flex justify-content-between">
                        <span>Paid</span>
                        <span>
                            {formatCurrency(
                                sale.paidAmount - sale.refundedAmount
                            )}
                        </span>
                    </div>
                    <div className="d-flex justify-content-between">
                        <span>Balance</span>
                        <span>{formatCurrency(sale.outstanding)}</span>
                    </div>
                </>
            )}

            <p className="mt-2">{sale.amountInWords}</p>
            {sale.settings.showFooter && company.footer && (
                <p className="text-center mt-3">{company.footer}</p>
            )}
        </article>
    );
}

function InvoiceView({ sale, format, thermalWidthMm }: InvoiceViewProps) {
    if (format === "Thermal") {
        return <ThermalInvoice sale={sale} width={thermalWidthMm} />;
    }

    if (format === "Compact") {
        return <CompactInvoice sale={sale} />;
    }

    return <A4Invoice sale={sale} />;
}

export default InvoiceView;
