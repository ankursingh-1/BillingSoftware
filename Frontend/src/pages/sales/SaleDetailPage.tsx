import { useCallback, useEffect, useState } from "react";
import { Link, useParams } from "react-router-dom";
import { toast } from "react-toastify";

import Modal from "../../components/common/Modal";
import PaymentStatusBadge from "../../components/sales/PaymentStatusBadge";
import RecordPaymentModal from "../../components/sales/RecordPaymentModal";
import RecordRefundModal from "../../components/returns/RecordRefundModal";
import ReturnItemsModal from "../../components/returns/ReturnItemsModal";

import { API_URL } from "../../config/api";
import { paymentMethodLabel } from "../../constants/payments";
import {
    createSalesReturn,
    recordSalesReturnRefund,
} from "../../services/returns/returnService";
import {
    downloadInvoicePdf,
    getSale,
    voidPayment,
} from "../../services/sales/salesService";
import { getApiErrorMessage, getApiStatus } from "../../utils/apiError";
import {
    formatCurrency,
    formatDate,
    formatDateTime,
} from "../../utils/format";
import { isAdmin } from "../../utils/session";

import type { SaleDetail, SalePayment } from "../../interfaces/sale/sale";

import "../../styles/data-pages.css";
import "../../styles/sales.css";

const dash = <span className="text-muted">—</span>;

function SaleDetailPage() {
    const { id } = useParams();
    const saleId = Number(id);
    const validId = Number.isInteger(saleId) && saleId > 0;

    const [sale, setSale] = useState<SaleDetail | null>(null);
    const [loading, setLoading] = useState(validId);
    const [error, setError] = useState<{
        message: string;
        notFound: boolean;
    } | null>(null);
    const [reloadKey, setReloadKey] = useState(0);

    const [paying, setPaying] = useState(false);
    const [returning, setReturning] = useState(false);
    const [refunding, setRefunding] = useState(false);
    const [voidTarget, setVoidTarget] = useState<SalePayment | null>(null);
    const [voidReason, setVoidReason] = useState("");
    const [voiding, setVoiding] = useState(false);
    const [downloading, setDownloading] = useState(false);
    const [logoFailed, setLogoFailed] = useState(false);

    const admin = isAdmin();

    useEffect(() => {
        if (!validId) {
            return;
        }

        let cancelled = false;

        const load = async () => {
            setLoading(true);
            setError(null);

            try {
                const data = await getSale(saleId);

                if (!cancelled) {
                    setSale(data);
                }
            } catch (err) {
                if (!cancelled) {
                    setError({
                        message: getApiErrorMessage(
                            err,
                            "Unable to load the invoice."
                        ),
                        notFound: getApiStatus(err) === 404,
                    });
                }
            } finally {
                if (!cancelled) {
                    setLoading(false);
                }
            }
        };

        void load();

        return () => {
            cancelled = true;
        };
    }, [saleId, validId, reloadKey]);

    const reload = useCallback(() => setReloadKey((key) => key + 1), []);

    const handleDownload = async () => {
        if (!sale) {
            return;
        }

        try {
            setDownloading(true);
            await downloadInvoicePdf(sale.id, sale.invoiceNumber);
        } catch (err) {
            toast.error(
                getApiErrorMessage(err, "Unable to download the invoice.")
            );
        } finally {
            setDownloading(false);
        }
    };

    const handleVoid = async () => {
        if (!sale || !voidTarget) {
            return;
        }

        if (voidReason.trim().length === 0) {
            toast.error("Enter a reason for voiding this payment.");
            return;
        }

        try {
            setVoiding(true);
            const updated = await voidPayment(
                sale.id,
                voidTarget.id,
                voidReason.trim()
            );

            setSale(updated);
            toast.success("Payment voided.");
            setVoidTarget(null);
            setVoidReason("");
        } catch (err) {
            toast.error(getApiErrorMessage(err, "Unable to void the payment."));
        } finally {
            setVoiding(false);
        }
    };

    if (!validId || (error?.notFound ?? false)) {
        return (
            <div className="data-page">
                <div className="data-card">
                    <div className="state-block">
                        <i className="bi bi-receipt state-icon"></i>
                        <p>This invoice does not exist.</p>
                        <Link to="/sales" className="btn btn-primary btn-sm">
                            Back to sales
                        </Link>
                    </div>
                </div>
            </div>
        );
    }

    if (loading && !sale) {
        return (
            <div className="data-page">
                <div className="data-card">
                    <div className="state-block">
                        <div
                            className="spinner-border text-primary"
                            role="status"
                        >
                            <span className="visually-hidden">
                                Loading invoice...
                            </span>
                        </div>
                        <p>Loading invoice...</p>
                    </div>
                </div>
            </div>
        );
    }

    if (error || !sale) {
        return (
            <div className="data-page">
                <div className="data-card">
                    <div className="state-block" role="alert">
                        <i className="bi bi-exclamation-octagon state-icon text-danger"></i>
                        <p>{error?.message ?? "Unable to load the invoice."}</p>
                        <button
                            type="button"
                            className="btn btn-outline-primary btn-sm"
                            onClick={reload}
                        >
                            <i className="bi bi-arrow-clockwise me-1"></i>
                            Retry
                        </button>
                    </div>
                </div>
            </div>
        );
    }

    const company = sale.company;
    const customer = sale.customer;
    const isTaxInvoice = sale.taxCalculated && !!company.gstin;
    const showTaxColumns = sale.taxCalculated;
    const canRecordPayment =
        !sale.paymentTracked ||
        (sale.outstanding > 0 && sale.paymentStatus !== "Settled");
    const companyAddress = [
        company.address,
        [company.city, company.state, company.pincode]
            .filter(Boolean)
            .join(", "),
    ].filter(Boolean);
    const hasBank =
        !!company.bankName ||
        !!company.accountNumber ||
        !!company.ifscCode ||
        !!company.upiId;
    const activePayments = sale.payments.filter((p) => !p.isVoided);
    const canReturn = sale.items.some((item) => item.returnableQuantity > 0);
    // Refunds are recorded against the most recent credit note; the server
    // caps the amount at what is actually owed on the invoice.
    const latestReturn =
        sale.returns.length > 0
            ? sale.returns.reduce((a, b) => (a.id > b.id ? a : b))
            : null;
    const canRefund = sale.refundDue > 0 && latestReturn !== null;

    return (
        <div className="data-page data-page-narrow">
            <div className="invoice-toolbar no-print">
                <Link to="/sales" className="btn btn-outline-secondary">
                    <i className="bi bi-arrow-left me-2"></i>
                    Sales
                </Link>

                <div className="d-flex flex-wrap gap-2">
                    {canRecordPayment && (
                        <button
                            type="button"
                            className="btn btn-success"
                            onClick={() => setPaying(true)}
                        >
                            <i className="bi bi-cash-coin me-2"></i>
                            Record payment
                        </button>
                    )}

                    {canReturn && (
                        <button
                            type="button"
                            className="btn btn-outline-danger"
                            onClick={() => setReturning(true)}
                        >
                            <i className="bi bi-arrow-return-left me-2"></i>
                            Return items
                        </button>
                    )}

                    {canRefund && (
                        <button
                            type="button"
                            className="btn btn-warning"
                            onClick={() => setRefunding(true)}
                        >
                            <i className="bi bi-cash-stack me-2"></i>
                            Record refund
                        </button>
                    )}

                    <button
                        type="button"
                        className="btn btn-outline-primary"
                        onClick={() => window.print()}
                    >
                        <i className="bi bi-printer me-2"></i>
                        Print
                    </button>

                    <button
                        type="button"
                        className="btn btn-primary"
                        disabled={downloading}
                        onClick={() => void handleDownload()}
                    >
                        {downloading ? (
                            <span
                                className="spinner-border spinner-border-sm me-2"
                                aria-hidden="true"
                            ></span>
                        ) : (
                            <i className="bi bi-file-earmark-pdf me-2"></i>
                        )}
                        Download PDF
                    </button>
                </div>
            </div>

            <article className="invoice-sheet" aria-label="Invoice">
                <header className="invoice-head">
                    <div className="invoice-company">
                        {company.hasLogo && !logoFailed && (
                            <img
                                className="invoice-logo"
                                src={`${API_URL}/Company/logo`}
                                alt={`${company.name} logo`}
                                onError={() => setLogoFailed(true)}
                            />
                        )}

                        <div>
                            <h3>{company.name}</h3>
                            {companyAddress.map((line) => (
                                <p key={line}>{line}</p>
                            ))}
                            {(company.phone || company.email) && (
                                <p>
                                    {[company.phone, company.email]
                                        .filter(Boolean)
                                        .join(" · ")}
                                </p>
                            )}
                            {company.gstin && <p>GSTIN: {company.gstin}</p>}
                            {company.pan && <p>PAN: {company.pan}</p>}
                        </div>
                    </div>

                    <div className="invoice-meta">
                        <div className="invoice-title">
                            {isTaxInvoice ? "TAX INVOICE" : "INVOICE"}
                        </div>
                        <p>
                            <strong>{sale.invoiceNumber}</strong>
                        </p>
                        <p>Date: {formatDate(sale.saleDate)}</p>
                        {sale.paymentStatus !== "NotRecorded" && (
                            <p className="mt-2">
                                <PaymentStatusBadge
                                    status={sale.paymentStatus}
                                />
                            </p>
                        )}
                    </div>
                </header>

                <section className="invoice-parties">
                    <div>
                        <div className="invoice-label">Bill to</div>
                        <strong>{customer.name}</strong>
                        <div>{customer.mobile}</div>
                        {customer.email && <div>{customer.email}</div>}
                        {customer.address && (
                            <div style={{ whiteSpace: "pre-line" }}>
                                {customer.address}
                            </div>
                        )}
                        {customer.gstin && <div>GSTIN: {customer.gstin}</div>}
                    </div>

                    <div>
                        <div className="invoice-label">Supply</div>
                        {sale.taxCalculated ? (
                            <>
                                <div>
                                    Place of supply:{" "}
                                    {sale.placeOfSupplyState ?? dash}
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

                <div className="table-responsive">
                    <table className="table invoice-table align-middle mb-0">
                        <thead>
                            <tr>
                                <th>#</th>
                                <th>Item</th>
                                {showTaxColumns && <th>HSN</th>}
                                <th className="text-end">Qty</th>
                                <th className="text-end">Rate</th>
                                {showTaxColumns && (
                                    <>
                                        <th className="text-end">Taxable</th>
                                        <th className="text-end">Tax</th>
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
                                                {item.sku}
                                            </div>
                                        )}
                                    </td>
                                    {showTaxColumns && (
                                        <td>{item.hsnCode ?? dash}</td>
                                    )}
                                    <td className="text-end">
                                        {item.quantity}
                                    </td>
                                    <td className="text-end text-nowrap">
                                        {formatCurrency(item.unitPrice)}
                                    </td>
                                    {showTaxColumns && (
                                        <>
                                            <td className="text-end text-nowrap">
                                                {formatCurrency(item.total)}
                                            </td>
                                            <td className="text-end text-nowrap">
                                                {formatCurrency(
                                                    item.taxAmount ?? 0
                                                )}
                                                <div className="small text-muted">
                                                    {item.taxRate ?? 0}%
                                                </div>
                                            </td>
                                        </>
                                    )}
                                    <td className="text-end text-nowrap">
                                        {formatCurrency(
                                            item.lineTotal ?? item.total
                                        )}
                                    </td>
                                </tr>
                            ))}
                        </tbody>
                    </table>
                </div>

                <section className="invoice-summary">
                    <div>
                        <div className="invoice-label">Amount in words</div>
                        <div>{sale.amountInWords}</div>

                        {sale.remarks && (
                            <>
                                <div className="invoice-label mt-3">
                                    Remarks
                                </div>
                                <div>{sale.remarks}</div>
                            </>
                        )}

                        {hasBank && (
                            <>
                                <div className="invoice-label mt-3">
                                    Bank details
                                </div>
                                {company.bankName && (
                                    <div>Bank: {company.bankName}</div>
                                )}
                                {company.accountNumber && (
                                    <div>A/c: {company.accountNumber}</div>
                                )}
                                {company.ifscCode && (
                                    <div>IFSC: {company.ifscCode}</div>
                                )}
                                {company.upiId && (
                                    <div>UPI: {company.upiId}</div>
                                )}
                            </>
                        )}
                    </div>

                    <div className="invoice-totals">
                        {sale.taxCalculated ? (
                            <>
                                <div className="row-line">
                                    <span>Subtotal</span>
                                    <span>{formatCurrency(sale.subtotal)}</span>
                                </div>
                                {sale.isInterState ? (
                                    <div className="row-line">
                                        <span>IGST</span>
                                        <span>
                                            {formatCurrency(sale.igstTotal)}
                                        </span>
                                    </div>
                                ) : (
                                    <>
                                        <div className="row-line">
                                            <span>CGST</span>
                                            <span>
                                                {formatCurrency(sale.cgstTotal)}
                                            </span>
                                        </div>
                                        <div className="row-line">
                                            <span>SGST</span>
                                            <span>
                                                {formatCurrency(sale.sgstTotal)}
                                            </span>
                                        </div>
                                    </>
                                )}
                                {sale.roundOff !== 0 && (
                                    <div className="row-line">
                                        <span>Round off</span>
                                        <span>
                                            {sale.roundOff > 0 ? "+" : "−"}
                                            {formatCurrency(
                                                Math.abs(sale.roundOff)
                                            )}
                                        </span>
                                    </div>
                                )}
                            </>
                        ) : null}

                        <div className="row-line grand">
                            <span>Total</span>
                            <span>{formatCurrency(sale.totalAmount)}</span>
                        </div>

                        {sale.paymentTracked && (
                            <>
                                <div className="row-line">
                                    <span>Paid</span>
                                    <span>
                                        {formatCurrency(
                                            sale.paidAmount -
                                                sale.refundedAmount
                                        )}
                                    </span>
                                </div>
                                {sale.returnedAmount > 0 && (
                                    <div className="row-line">
                                        <span>Returned</span>
                                        <span>
                                            −{formatCurrency(sale.returnedAmount)}
                                        </span>
                                    </div>
                                )}
                                <div className="row-line fw-semibold">
                                    <span>Balance due</span>
                                    <span>
                                        {formatCurrency(sale.outstanding)}
                                    </span>
                                </div>
                                {sale.refundDue > 0 && (
                                    <div className="row-line text-danger fw-semibold">
                                        <span>Refund due</span>
                                        <span>
                                            {formatCurrency(sale.refundDue)}
                                        </span>
                                    </div>
                                )}
                            </>
                        )}
                    </div>
                </section>

                {(company.terms || company.footer) && (
                    <footer className="invoice-footer">
                        {company.terms && (
                            <>
                                <div className="invoice-label">
                                    Terms &amp; conditions
                                </div>
                                <div className="mb-2">{company.terms}</div>
                            </>
                        )}
                        {company.footer && (
                            <div className="text-center">{company.footer}</div>
                        )}
                    </footer>
                )}
            </article>

            <section className="data-card no-print mb-4">
                <div className="p-3 border-bottom d-flex justify-content-between align-items-center">
                    <h5 className="mb-0">Payments</h5>
                    <PaymentStatusBadge status={sale.paymentStatus} />
                </div>

                {!sale.paymentTracked ? (
                    <div className="p-3 text-muted">
                        Payments were not tracked for this older invoice, so
                        the balance is unknown. Recording a payment starts
                        tracking from now.
                    </div>
                ) : sale.payments.length === 0 ? (
                    <div className="p-3 text-muted">
                        No payments recorded yet.
                    </div>
                ) : (
                    <div className="table-responsive">
                        <table className="table align-middle mb-0">
                            <thead>
                                <tr>
                                    <th>Date</th>
                                    <th>Method</th>
                                    <th>Reference</th>
                                    <th className="text-end">Amount</th>
                                    <th>By</th>
                                    <th></th>
                                </tr>
                            </thead>
                            <tbody>
                                {sale.payments.map((payment) => (
                                    <tr
                                        key={payment.id}
                                        className={
                                            payment.isVoided
                                                ? "text-muted"
                                                : undefined
                                        }
                                    >
                                        <td className="text-nowrap">
                                            {formatDateTime(
                                                payment.paymentDate
                                            )}
                                        </td>
                                        <td>
                                            {paymentMethodLabel(
                                                payment.method
                                            )}
                                        </td>
                                        <td>{payment.reference ?? dash}</td>
                                        <td
                                            className="text-end text-nowrap"
                                            style={
                                                payment.isVoided
                                                    ? {
                                                          textDecoration:
                                                              "line-through",
                                                      }
                                                    : undefined
                                            }
                                        >
                                            {formatCurrency(payment.amount)}
                                        </td>
                                        <td>{payment.recordedBy ?? dash}</td>
                                        <td className="text-end">
                                            {payment.isVoided ? (
                                                <span
                                                    className="badge text-bg-secondary"
                                                    title={
                                                        payment.voidReason ??
                                                        undefined
                                                    }
                                                >
                                                    Voided
                                                    {payment.voidReason
                                                        ? `: ${payment.voidReason}`
                                                        : ""}
                                                </span>
                                            ) : (
                                                admin && (
                                                    <button
                                                        type="button"
                                                        className="btn btn-sm btn-outline-danger"
                                                        onClick={() => {
                                                            setVoidReason("");
                                                            setVoidTarget(
                                                                payment
                                                            );
                                                        }}
                                                    >
                                                        Void
                                                    </button>
                                                )
                                            )}
                                        </td>
                                    </tr>
                                ))}
                            </tbody>
                        </table>
                    </div>
                )}

                {sale.paymentTracked && activePayments.length > 0 && (
                    <div className="p-3 border-top small text-muted">
                        Only an administrator can void a payment. Voided
                        payments stay in the history.
                    </div>
                )}
            </section>

            {sale.returns.length > 0 && (
                <section className="data-card no-print mb-4">
                    <div className="p-3 border-bottom">
                        <h5 className="mb-0">Returns</h5>
                    </div>
                    <div className="table-responsive">
                        <table className="table align-middle mb-0">
                            <thead>
                                <tr>
                                    <th>Credit note</th>
                                    <th>Date</th>
                                    <th className="text-end">Value</th>
                                    <th className="text-end">Refunded</th>
                                </tr>
                            </thead>
                            <tbody>
                                {sale.returns.map((ret) => (
                                    <tr key={ret.id}>
                                        <td>
                                            <Link
                                                to={`/returns/sales/${ret.id}`}
                                                className="text-decoration-none"
                                            >
                                                {ret.returnNumber}
                                            </Link>
                                        </td>
                                        <td>{formatDate(ret.returnDate)}</td>
                                        <td className="text-end">
                                            {formatCurrency(ret.totalAmount)}
                                        </td>
                                        <td className="text-end">
                                            {formatCurrency(ret.refundAmount)}
                                        </td>
                                    </tr>
                                ))}
                            </tbody>
                        </table>
                    </div>
                </section>
            )}

            {returning && (
                <ReturnItemsModal
                    title={`Return items · ${sale.invoiceNumber}`}
                    intro={
                        <>
                            Stock is added back and a credit note is created.
                            Paid so far:{" "}
                            <strong>
                                {formatCurrency(
                                    sale.paidAmount - sale.refundedAmount
                                )}
                            </strong>
                            .
                        </>
                    }
                    lines={sale.items.map((item) => ({
                        id: item.id,
                        name: item.productName,
                        sku: item.sku,
                        returnable: item.returnableQuantity,
                        unitValue:
                            (item.lineTotal ?? item.total) /
                            Math.max(item.quantity, 1),
                    }))}
                    amountLabel="Refund now (optional)"
                    amountHelp="Leave blank to refund later. You cannot refund more than the customer has paid."
                    submitLabel="Create credit note"
                    onClose={() => setReturning(false)}
                    onSubmit={async (submission) => {
                        const result = await createSalesReturn({
                            saleId: sale.id,
                            items: submission.items.map((entry) => ({
                                saleItemId: entry.id,
                                quantity: entry.quantity,
                            })),
                            reason: submission.reason,
                            refundAmount: submission.amount,
                            refundMethod: submission.method,
                        });

                        toast.success(
                            `Credit note ${result.returnNumber} created.`
                        );
                        setReturning(false);
                        reload();
                    }}
                />
            )}

            {refunding && latestReturn && (
                <RecordRefundModal
                    returnNumber={latestReturn.returnNumber}
                    refundDue={sale.refundDue}
                    onClose={() => setRefunding(false)}
                    onSubmit={async (amount, method) => {
                        await recordSalesReturnRefund(
                            latestReturn.id,
                            amount,
                            method
                        );

                        toast.success("Refund recorded.");
                        setRefunding(false);
                        reload();
                    }}
                />
            )}

            {paying && (
                <RecordPaymentModal
                    sale={sale}
                    onClose={() => setPaying(false)}
                    onSaved={(updated) => {
                        setSale(updated);
                        setPaying(false);
                    }}
                />
            )}

            <Modal
                show={voidTarget !== null}
                title="Void payment"
                busy={voiding}
                onClose={() => setVoidTarget(null)}
                footer={
                    <>
                        <button
                            type="button"
                            className="btn btn-outline-secondary"
                            onClick={() => setVoidTarget(null)}
                            disabled={voiding}
                        >
                            Cancel
                        </button>
                        <button
                            type="button"
                            className="btn btn-danger"
                            onClick={() => void handleVoid()}
                            disabled={voiding}
                        >
                            {voiding && (
                                <span
                                    className="spinner-border spinner-border-sm me-2"
                                    aria-hidden="true"
                                ></span>
                            )}
                            Void payment
                        </button>
                    </>
                }
            >
                <p>
                    Void the{" "}
                    <strong>
                        {voidTarget ? formatCurrency(voidTarget.amount) : ""}
                    </strong>{" "}
                    payment? The invoice balance will increase again. The
                    payment stays in the history.
                </p>

                <label htmlFor="void-reason" className="form-label">
                    Reason <span className="text-danger">*</span>
                </label>
                <textarea
                    id="void-reason"
                    className="form-control"
                    rows={2}
                    maxLength={500}
                    value={voidReason}
                    onChange={(e) => setVoidReason(e.target.value)}
                />
            </Modal>
        </div>
    );
}

export default SaleDetailPage;