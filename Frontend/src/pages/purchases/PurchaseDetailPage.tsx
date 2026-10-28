import { useState } from "react";
import { Link, useParams } from "react-router-dom";
import { toast } from "react-toastify";

import DetailPageState from "../../components/common/DetailPageState";
import TaxTotals from "../../components/common/TaxTotals";
import ReturnItemsModal from "../../components/returns/ReturnItemsModal";

import { useLoadedDetail } from "../../hooks/useLoadedDetail";
import { getPurchase } from "../../services/purchases/purchaseService";
import { createPurchaseReturn } from "../../services/returns/returnService";
import { formatCurrency, formatDate } from "../../utils/format";

import "../../styles/data-pages.css";
import "../../styles/sales.css";

const LOAD_ERROR = "Unable to load the purchase.";
const dash = <span className="text-muted">—</span>;

function PurchaseDetailPage() {
    const { id } = useParams();
    const { data: purchase, loading, error, notFound, reload } =
        useLoadedDetail(getPurchase, id, LOAD_ERROR);

    const [returning, setReturning] = useState(false);

    if (!purchase) {
        return (
            <DetailPageState
                loading={loading}
                error={error}
                notFound={notFound}
                noun="purchase"
                backTo="/purchases"
                backLabel="Back to purchases"
                onRetry={reload}
            />
        );
    }

    const supplier = purchase.supplier;
    const showTax = purchase.taxCalculated;
    const canReturn = purchase.items.some((item) => item.returnableQuantity > 0);

    return (
        <div className="data-page data-page-narrow">
            <div className="invoice-toolbar no-print">
                <Link to="/purchases" className="btn btn-outline-secondary">
                    <i className="bi bi-arrow-left me-2"></i>
                    Purchases
                </Link>

                <div className="d-flex flex-wrap gap-2">
                    {canReturn && (
                        <button
                            type="button"
                            className="btn btn-outline-danger"
                            onClick={() => setReturning(true)}
                        >
                            <i className="bi bi-arrow-return-left me-2"></i>
                            Return goods
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
                </div>
            </div>

            <article className="invoice-sheet" aria-label="Purchase">
                <header className="invoice-head">
                    <div className="invoice-company">
                        <div>
                            <h3>{supplier.companyName}</h3>
                            <p>{supplier.name}</p>
                            <p>{supplier.mobile}</p>
                            {supplier.address && (
                                <p style={{ whiteSpace: "pre-line" }}>
                                    {supplier.address}
                                </p>
                            )}
                            {supplier.gstin && <p>GSTIN: {supplier.gstin}</p>}
                            {supplier.state && <p>State: {supplier.state}</p>}
                        </div>
                    </div>

                    <div className="invoice-meta">
                        <div className="invoice-title">PURCHASE</div>
                        <p>
                            <strong>{purchase.purchaseNumber}</strong>
                        </p>
                        <p>Date: {formatDate(purchase.purchaseDate)}</p>
                        {purchase.supplierInvoiceNumber && (
                            <p>
                                Supplier invoice:{" "}
                                {purchase.supplierInvoiceNumber}
                            </p>
                        )}
                        {purchase.createdBy && <p>Entered by: {purchase.createdBy}</p>}
                    </div>
                </header>

                {!showTax && (
                    <div className="alert alert-light border py-2 mt-3">
                        Tax was not recorded for this older purchase.
                    </div>
                )}

                <div className="table-responsive mt-3">
                    <table className="table align-middle mb-0">
                        <thead>
                            <tr>
                                <th>Item</th>
                                <th>HSN</th>
                                <th className="text-end">Qty</th>
                                <th className="text-end">Price</th>
                                <th className="text-end">Taxable</th>
                                {showTax && (
                                    <>
                                        <th className="text-end">Tax</th>
                                        <th className="text-end">Amount</th>
                                    </>
                                )}
                                <th className="text-end no-print">Returned</th>
                            </tr>
                        </thead>
                        <tbody>
                            {purchase.items.map((item) => (
                                <tr key={item.id}>
                                    <td>
                                        <strong>{item.productName}</strong>
                                        {item.sku && (
                                            <div className="small text-muted">
                                                {item.sku}
                                            </div>
                                        )}
                                    </td>
                                    <td>{item.hsnCode ?? dash}</td>
                                    <td className="text-end">{item.quantity}</td>
                                    <td className="text-end text-nowrap">
                                        {formatCurrency(item.unitPrice)}
                                    </td>
                                    <td className="text-end text-nowrap">
                                        {formatCurrency(item.total)}
                                    </td>
                                    {showTax && (
                                        <>
                                            <td className="text-end text-nowrap">
                                                {formatCurrency(item.taxAmount ?? 0)}
                                                {item.taxRate != null && (
                                                    <div className="small text-muted">
                                                        {item.taxRate}%
                                                    </div>
                                                )}
                                            </td>
                                            <td className="text-end text-nowrap">
                                                {formatCurrency(
                                                    item.lineTotal ?? item.total
                                                )}
                                            </td>
                                        </>
                                    )}
                                    <td className="text-end no-print">
                                        {item.returnedQuantity || dash}
                                    </td>
                                </tr>
                            ))}
                        </tbody>
                    </table>
                </div>

                <TaxTotals
                    subtotal={purchase.subtotal}
                    showTax={showTax}
                    isInterState={purchase.isInterState}
                    cgstTotal={purchase.cgstTotal}
                    sgstTotal={purchase.sgstTotal}
                    igstTotal={purchase.igstTotal}
                    roundOff={purchase.roundOff}
                    total={purchase.totalAmount}
                >
                    {purchase.returnedAmount > 0 && (
                        <div className="row-line">
                            <span>Returned to supplier</span>
                            <span>−{formatCurrency(purchase.returnedAmount)}</span>
                        </div>
                    )}
                </TaxTotals>
                {purchase.remarks && (
                    <p className="mt-3 mb-0">
                        <strong>Remarks:</strong> {purchase.remarks}
                    </p>
                )}
            </article>

            {purchase.returns.length > 0 && (
                <section className="data-card no-print mb-4">
                    <div className="p-3 border-bottom">
                        <h5 className="mb-0">Returns (debit notes)</h5>
                    </div>
                    <div className="table-responsive">
                        <table className="table align-middle mb-0">
                            <thead>
                                <tr>
                                    <th>Debit note</th>
                                    <th>Date</th>
                                    <th className="text-end">Value</th>
                                    <th className="text-end">Refund received</th>
                                </tr>
                            </thead>
                            <tbody>
                                {purchase.returns.map((ret) => (
                                    <tr key={ret.id}>
                                        <td>
                                            <Link
                                                to={`/returns/purchases/${ret.id}`}
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
                                            {formatCurrency(ret.refundReceived)}
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
                    title={`Return goods · ${purchase.purchaseNumber}`}
                    intro={
                        <>
                            Stock is removed and a debit note is created. A
                            return fails if the units have already been sold.
                        </>
                    }
                    lines={purchase.items.map((item) => ({
                        id: item.id,
                        name: item.productName,
                        sku: item.sku,
                        returnable: item.returnableQuantity,
                        unitValue:
                            (item.lineTotal ?? item.total) /
                            Math.max(item.quantity, 1),
                    }))}
                    amountLabel="Refund received now (optional)"
                    amountHelp="Money the supplier has already paid back. It cannot exceed the value of this return."
                    showAdjustmentNote
                    submitLabel="Create debit note"
                    onClose={() => setReturning(false)}
                    onSubmit={async (submission) => {
                        const result = await createPurchaseReturn({
                            purchaseId: purchase.id,
                            items: submission.items.map((entry) => ({
                                purchaseItemId: entry.id,
                                quantity: entry.quantity,
                            })),
                            reason: submission.reason,
                            refundReceived: submission.amount,
                            refundMethod: submission.method,
                            adjustmentNote: submission.note ?? null,
                        });

                        toast.success(
                            `Debit note ${result.returnNumber} created.`
                        );
                        setReturning(false);
                        reload();
                    }}
                />
            )}
        </div>
    );
}

export default PurchaseDetailPage;