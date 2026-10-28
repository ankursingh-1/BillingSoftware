import { Link, useParams } from "react-router-dom";

import DetailPageState from "../../components/common/DetailPageState";
import TaxTotals from "../../components/common/TaxTotals";

import { paymentMethodLabel } from "../../constants/payments";
import { useLoadedDetail } from "../../hooks/useLoadedDetail";
import {
    getPurchaseReturn,
    getSalesReturn,
} from "../../services/returns/returnService";
import { formatCurrency, formatDate } from "../../utils/format";

import "../../styles/data-pages.css";
import "../../styles/sales.css";

const dash = <span className="text-muted">—</span>;

interface ReturnDetailPageProps {
    kind: "sales" | "purchases";
}

/** Credit note (sales return) or debit note (purchase return). */
function ReturnDetailPage({ kind }: ReturnDetailPageProps) {
    return kind === "sales" ? <SalesReturnView /> : <PurchaseReturnView />;
}

function SalesReturnView() {
    const { id } = useParams();
    const { data, loading, error, notFound, reload } = useLoadedDetail(
        getSalesReturn,
        id,
        "Unable to load the credit note."
    );

    if (!data) {
        return (
            <DetailPageState
                loading={loading}
                error={error}
                notFound={notFound}
                noun="credit note"
                backTo="/returns"
                backLabel="Back to returns"
                onRetry={reload}
            />
        );
    }

    return (
        <NoteView
            title="CREDIT NOTE"
            number={data.returnNumber}
            date={data.returnDate}
            reason={data.reason}
            createdBy={data.createdBy}
            partyLabel="Customer"
            partyName={data.customerName}
            partyGstin={data.customerGstin}
            refLabel="Against invoice"
            refText={data.invoiceNumber}
            refTo={`/sales/${data.saleId}`}
            isInterState={data.isInterState}
            subtotal={data.subtotal}
            cgstTotal={data.cgstTotal}
            sgstTotal={data.sgstTotal}
            igstTotal={data.igstTotal}
            roundOff={data.roundOff}
            totalAmount={data.totalAmount}
            items={data.items}
            settlement={
                <>
                    <div className="row-line">
                        <span>Refunded to customer</span>
                        <span>{formatCurrency(data.refundAmount)}</span>
                    </div>
                    {data.refundMethod && (
                        <div className="row-line">
                            <span>Method</span>
                            <span>{paymentMethodLabel(data.refundMethod)}</span>
                        </div>
                    )}
                    {data.refundDue > 0 && (
                        <div className="row-line">
                            <span>Still owed on the invoice</span>
                            <span>{formatCurrency(data.refundDue)}</span>
                        </div>
                    )}
                </>
            }
            backTo="/returns"
        />
    );
}

function PurchaseReturnView() {
    const { id } = useParams();
    const { data, loading, error, notFound, reload } = useLoadedDetail(
        getPurchaseReturn,
        id,
        "Unable to load the debit note."
    );

    if (!data) {
        return (
            <DetailPageState
                loading={loading}
                error={error}
                notFound={notFound}
                noun="debit note"
                backTo="/returns"
                backLabel="Back to returns"
                onRetry={reload}
            />
        );
    }

    return (
        <NoteView
            title="DEBIT NOTE"
            number={data.returnNumber}
            date={data.returnDate}
            reason={data.reason}
            createdBy={data.createdBy}
            partyLabel="Supplier"
            partyName={data.supplierName}
            partyGstin={data.supplierGstin}
            refLabel="Against purchase"
            refText={`${data.purchaseNumber}${
                data.supplierInvoiceNumber
                    ? ` (supplier invoice ${data.supplierInvoiceNumber})`
                    : ""
            }`}
            refTo={`/purchases/${data.purchaseId}`}
            isInterState={data.isInterState}
            subtotal={data.subtotal}
            cgstTotal={data.cgstTotal}
            sgstTotal={data.sgstTotal}
            igstTotal={data.igstTotal}
            roundOff={data.roundOff}
            totalAmount={data.totalAmount}
            items={data.items}
            settlement={
                <>
                    <div className="row-line">
                        <span>Refund received</span>
                        <span>{formatCurrency(data.refundReceived)}</span>
                    </div>
                    {data.refundMethod && (
                        <div className="row-line">
                            <span>Method</span>
                            <span>{paymentMethodLabel(data.refundMethod)}</span>
                        </div>
                    )}
                    {data.adjustmentNote && (
                        <div className="row-line">
                            <span>Settlement</span>
                            <span>{data.adjustmentNote}</span>
                        </div>
                    )}
                </>
            }
            backTo="/returns"
        />
    );
}

interface NoteViewProps {
    title: string;
    number: string;
    date: string;
    reason: string | null;
    createdBy: string | null;
    partyLabel: string;
    partyName: string;
    partyGstin: string | null;
    refLabel: string;
    refText: string;
    refTo: string;
    isInterState: boolean;
    subtotal: number;
    cgstTotal: number;
    sgstTotal: number;
    igstTotal: number;
    roundOff: number;
    totalAmount: number;
    items: {
        id: number;
        productName: string;
        sku: string | null;
        hsnCode: string | null;
        quantity: number;
        unitPrice: number;
        taxableAmount: number;
        taxRate: number;
        taxAmount: number;
        total: number;
    }[];
    settlement: React.ReactNode;
    backTo: string;
}

function NoteView(props: NoteViewProps) {
    return (
        <div className="data-page data-page-narrow">
            <div className="invoice-toolbar no-print">
                <Link to={props.backTo} className="btn btn-outline-secondary">
                    <i className="bi bi-arrow-left me-2"></i>
                    Returns
                </Link>

                <button
                    type="button"
                    className="btn btn-outline-primary"
                    onClick={() => window.print()}
                >
                    <i className="bi bi-printer me-2"></i>
                    Print
                </button>
            </div>

            <article className="invoice-sheet" aria-label={props.title}>
                <header className="invoice-head">
                    <div>
                        <div className="invoice-label">{props.partyLabel}</div>
                        <h3>{props.partyName}</h3>
                        {props.partyGstin && <p>GSTIN: {props.partyGstin}</p>}
                    </div>

                    <div className="invoice-meta">
                        <div className="invoice-title">{props.title}</div>
                        <p>
                            <strong>{props.number}</strong>
                        </p>
                        <p>Date: {formatDate(props.date)}</p>
                        <p>
                            {props.refLabel}:{" "}
                            <Link to={props.refTo} className="text-decoration-none">
                                {props.refText}
                            </Link>
                        </p>
                        {props.createdBy && <p>Entered by: {props.createdBy}</p>}
                    </div>
                </header>

                <div className="table-responsive mt-3">
                    <table className="table align-middle mb-0">
                        <thead>
                            <tr>
                                <th>Item</th>
                                <th>HSN</th>
                                <th className="text-end">Qty</th>
                                <th className="text-end">Price</th>
                                <th className="text-end">Taxable</th>
                                <th className="text-end">Tax</th>
                                <th className="text-end">Amount</th>
                            </tr>
                        </thead>
                        <tbody>
                            {props.items.map((item) => (
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
                                        {formatCurrency(item.taxableAmount)}
                                    </td>
                                    <td className="text-end text-nowrap">
                                        {formatCurrency(item.taxAmount)}
                                        {item.taxRate > 0 && (
                                            <div className="small text-muted">
                                                {item.taxRate}%
                                            </div>
                                        )}
                                    </td>
                                    <td className="text-end text-nowrap">
                                        {formatCurrency(item.total)}
                                    </td>
                                </tr>
                            ))}
                        </tbody>
                    </table>
                </div>

                <TaxTotals
                    subtotal={props.subtotal}
                    showTax
                    isInterState={props.isInterState}
                    cgstTotal={props.cgstTotal}
                    sgstTotal={props.sgstTotal}
                    igstTotal={props.igstTotal}
                    roundOff={props.roundOff}
                    total={props.totalAmount}
                >
                    {props.settlement}
                </TaxTotals>

                {props.reason && (
                    <p className="mt-3 mb-0">
                        <strong>Reason:</strong> {props.reason}
                    </p>
                )}
            </article>
        </div>
    );
}

export default ReturnDetailPage;