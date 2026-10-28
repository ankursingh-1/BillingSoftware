import { useState } from "react";
import { Link } from "react-router-dom";
import { toast } from "react-toastify";

import Pagination from "../../components/common/Pagination";
import PaymentStatusBadge from "../../components/sales/PaymentStatusBadge";

import {
    PAGE_SIZE_OPTIONS,
    usePagedSearch,
} from "../../hooks/usePagedSearch";
import {
    downloadInvoicePdf,
    searchSales,
} from "../../services/sales/salesService";
import { getApiErrorMessage } from "../../utils/apiError";
import { formatCurrency, formatDate } from "../../utils/format";

import type { PaymentStatus } from "../../interfaces/sale/sale";

import "../../styles/data-pages.css";
import "../../styles/sales.css";

const LOAD_ERROR = "Unable to load sales.";

const STATUS_OPTIONS: { value: PaymentStatus | ""; label: string }[] = [
    { value: "", label: "All payments" },
    { value: "Unpaid", label: "Unpaid" },
    { value: "Partial", label: "Partial" },
    { value: "Paid", label: "Paid" },
    { value: "Settled", label: "Settled (after returns)" },
    { value: "NotRecorded", label: "Not recorded (older)" },
];

function SalesPage() {
    const [fromDate, setFromDate] = useState("");
    const [toDate, setToDate] = useState("");
    const [status, setStatus] = useState<PaymentStatus | "">("");
    const [downloadingId, setDownloadingId] = useState<number | null>(null);

    const dateRangeInvalid =
        fromDate !== "" && toDate !== "" && fromDate > toDate;

    const {
        search,
        setSearch,
        debouncedSearch,
        page,
        pageSize,
        result,
        loading,
        error,
        goToPage,
        changePageSize,
        reload,
    } = usePagedSearch(searchSales, LOAD_ERROR, {
        // An inverted range would return nothing; wait until it is fixed.
        fromDate: dateRangeInvalid ? undefined : fromDate || undefined,
        toDate: dateRangeInvalid ? undefined : toDate || undefined,
        paymentStatus: status,
    });

    const sales = result?.data ?? [];
    const isFiltering =
        debouncedSearch.length > 0 ||
        fromDate !== "" ||
        toDate !== "" ||
        status !== "";

    const clearFilters = () => {
        setSearch("");
        setFromDate("");
        setToDate("");
        setStatus("");
    };

    const handleDownload = async (id: number, invoiceNumber: string) => {
        try {
            setDownloadingId(id);
            await downloadInvoicePdf(id, invoiceNumber);
        } catch (err) {
            toast.error(
                getApiErrorMessage(err, "Unable to download the invoice.")
            );
        } finally {
            setDownloadingId(null);
        }
    };

    return (
        <div className="data-page">
            <div className="data-page-header">
                <div>
                    <h2 className="page-title">Sales</h2>
                    <p className="data-page-subtitle">
                        Invoices, payments and outstanding balances
                    </p>
                </div>

                <div className="data-page-actions">
                    <Link to="/sales/new" className="btn btn-primary">
                        <i className="bi bi-plus-lg me-2"></i>
                        New Sale
                    </Link>
                </div>
            </div>

            <div className="data-card">
                <div className="data-toolbar">
                    <div className="input-group data-search">
                        <span className="input-group-text">
                            <i className="bi bi-search"></i>
                        </span>

                        <input
                            type="search"
                            className="form-control"
                            placeholder="Invoice number or customer"
                            aria-label="Search sales by invoice number or customer"
                            value={search}
                            onChange={(e) => setSearch(e.target.value)}
                        />
                    </div>

                    <div className="data-toolbar-right d-flex flex-wrap gap-2 align-items-center">
                        <label htmlFor="sales-from" className="visually-hidden">
                            From date
                        </label>
                        <input
                            id="sales-from"
                            type="date"
                            className={`form-control form-control-sm${
                                dateRangeInvalid ? " is-invalid" : ""
                            }`}
                            style={{ width: "auto" }}
                            value={fromDate}
                            max={toDate || undefined}
                            onChange={(e) => setFromDate(e.target.value)}
                        />

                        <span className="text-muted">to</span>

                        <label htmlFor="sales-to" className="visually-hidden">
                            To date
                        </label>
                        <input
                            id="sales-to"
                            type="date"
                            className={`form-control form-control-sm${
                                dateRangeInvalid ? " is-invalid" : ""
                            }`}
                            style={{ width: "auto" }}
                            value={toDate}
                            min={fromDate || undefined}
                            onChange={(e) => setToDate(e.target.value)}
                        />

                        <label
                            htmlFor="sales-status"
                            className="visually-hidden"
                        >
                            Payment status
                        </label>
                        <select
                            id="sales-status"
                            className="form-select form-select-sm"
                            style={{ width: "auto" }}
                            value={status}
                            onChange={(e) =>
                                setStatus(e.target.value as PaymentStatus | "")
                            }
                        >
                            {STATUS_OPTIONS.map((option) => (
                                <option key={option.value} value={option.value}>
                                    {option.label}
                                </option>
                            ))}
                        </select>

                        <label
                            htmlFor="sales-page-size"
                            className="visually-hidden"
                        >
                            Rows per page
                        </label>
                        <select
                            id="sales-page-size"
                            className="form-select form-select-sm"
                            style={{ width: "auto" }}
                            value={pageSize}
                            onChange={(e) =>
                                changePageSize(Number(e.target.value))
                            }
                        >
                            {PAGE_SIZE_OPTIONS.map((size) => (
                                <option key={size} value={size}>
                                    {size} / page
                                </option>
                            ))}
                        </select>
                    </div>
                </div>

                {dateRangeInvalid && (
                    <div className="alert alert-warning m-3 mb-0" role="alert">
                        The &quot;from&quot; date is after the &quot;to&quot;
                        date.
                    </div>
                )}

                {error && !result ? (
                    <div className="state-block" role="alert">
                        <i className="bi bi-exclamation-octagon state-icon text-danger"></i>
                        <p>{error}</p>
                        <button
                            type="button"
                            className="btn btn-outline-primary btn-sm"
                            onClick={reload}
                        >
                            <i className="bi bi-arrow-clockwise me-1"></i>
                            Retry
                        </button>
                    </div>
                ) : loading && !result ? (
                    <div className="state-block">
                        <div
                            className="spinner-border text-primary"
                            role="status"
                        >
                            <span className="visually-hidden">
                                Loading sales...
                            </span>
                        </div>
                        <p>Loading sales...</p>
                    </div>
                ) : (
                    <>
                        {error && (
                            <div
                                className="alert alert-danger m-3 mb-0"
                                role="alert"
                            >
                                {error}{" "}
                                <button
                                    type="button"
                                    className="btn btn-link p-0 align-baseline"
                                    onClick={reload}
                                >
                                    Retry
                                </button>
                            </div>
                        )}

                        {sales.length === 0 ? (
                            <div className="state-block">
                                <i className="bi bi-receipt state-icon"></i>
                                <p>
                                    {isFiltering
                                        ? "No sales match these filters."
                                        : "No sales yet. Create your first invoice to get started."}
                                </p>
                                {isFiltering ? (
                                    <button
                                        type="button"
                                        className="btn btn-outline-primary btn-sm"
                                        onClick={clearFilters}
                                    >
                                        Clear filters
                                    </button>
                                ) : (
                                    <Link
                                        to="/sales/new"
                                        className="btn btn-primary btn-sm"
                                    >
                                        <i className="bi bi-plus-lg me-1"></i>
                                        New Sale
                                    </Link>
                                )}
                            </div>
                        ) : (
                            <div
                                className={`table-wrapper${
                                    loading ? " is-loading" : ""
                                }`}
                                aria-busy={loading}
                            >
                                <div className="table-responsive">
                                    <table className="table table-hover align-middle mb-0">
                                        <thead>
                                            <tr>
                                                <th>Invoice</th>
                                                <th>Date</th>
                                                <th>Customer</th>
                                                <th className="text-end">
                                                    Total
                                                </th>
                                                <th className="text-end d-none d-md-table-cell">
                                                    Paid
                                                </th>
                                                <th className="text-end">
                                                    Balance
                                                </th>
                                                <th>Payment</th>
                                                <th className="text-end">
                                                    Actions
                                                </th>
                                            </tr>
                                        </thead>

                                        <tbody>
                                            {sales.map((sale) => (
                                                <tr key={sale.id}>
                                                    <td className="text-nowrap">
                                                        <Link
                                                            to={`/sales/${sale.id}`}
                                                            className="fw-semibold text-decoration-none"
                                                        >
                                                            {sale.invoiceNumber}
                                                        </Link>
                                                    </td>
                                                    <td className="text-nowrap">
                                                        {formatDate(
                                                            sale.saleDate
                                                        )}
                                                    </td>
                                                    <td>{sale.customerName}</td>
                                                    <td className="text-end text-nowrap">
                                                        {formatCurrency(
                                                            sale.totalAmount
                                                        )}
                                                    </td>
                                                    <td className="text-end text-nowrap d-none d-md-table-cell">
                                                        {sale.paymentStatus ===
                                                        "NotRecorded" ? (
                                                            <span className="text-muted">
                                                                —
                                                            </span>
                                                        ) : (
                                                            formatCurrency(
                                                                sale.paidAmount
                                                            )
                                                        )}
                                                    </td>
                                                    <td className="text-end text-nowrap">
                                                        {sale.paymentStatus ===
                                                        "NotRecorded" ? (
                                                            <span className="text-muted">
                                                                —
                                                            </span>
                                                        ) : (
                                                            formatCurrency(
                                                                sale.outstanding
                                                            )
                                                        )}
                                                    </td>
                                                    <td>
                                                        <PaymentStatusBadge
                                                            status={
                                                                sale.paymentStatus
                                                            }
                                                        />
                                                    </td>
                                                    <td className="text-end text-nowrap">
                                                        <Link
                                                            to={`/sales/${sale.id}`}
                                                            className="btn btn-sm btn-outline-secondary me-2"
                                                            aria-label={`View ${sale.invoiceNumber}`}
                                                        >
                                                            <i className="bi bi-eye"></i>
                                                        </Link>

                                                        <button
                                                            type="button"
                                                            className="btn btn-sm btn-outline-primary"
                                                            aria-label={`Download ${sale.invoiceNumber} as PDF`}
                                                            disabled={
                                                                downloadingId ===
                                                                sale.id
                                                            }
                                                            onClick={() =>
                                                                void handleDownload(
                                                                    sale.id,
                                                                    sale.invoiceNumber
                                                                )
                                                            }
                                                        >
                                                            <i className="bi bi-file-earmark-pdf"></i>
                                                        </button>
                                                    </td>
                                                </tr>
                                            ))}
                                        </tbody>
                                    </table>
                                </div>

                                <Pagination
                                    page={result?.pageNumber ?? page}
                                    totalPages={result?.totalPages ?? 1}
                                    totalRecords={result?.totalRecords ?? 0}
                                    pageSize={result?.pageSize ?? pageSize}
                                    disabled={loading}
                                    onPageChange={goToPage}
                                />
                            </div>
                        )}
                    </>
                )}
            </div>
        </div>
    );
}

export default SalesPage;