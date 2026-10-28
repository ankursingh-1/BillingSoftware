import { useState } from "react";
import { Link } from "react-router-dom";

import PagedTableCard from "../../components/common/PagedTableCard";

import { usePagedSearch } from "../../hooks/usePagedSearch";
import { searchPurchases } from "../../services/purchases/purchaseService";
import { formatCurrency, formatDate } from "../../utils/format";

import "../../styles/data-pages.css";
import "../../styles/sales.css";

const LOAD_ERROR = "Unable to load purchases.";

function PurchasesPage() {
    const [fromDate, setFromDate] = useState("");
    const [toDate, setToDate] = useState("");

    const dateRangeInvalid =
        fromDate !== "" && toDate !== "" && fromDate > toDate;

    const list = usePagedSearch(searchPurchases, LOAD_ERROR, {
        // An inverted range would return nothing; wait until it is fixed.
        fromDate: dateRangeInvalid ? undefined : fromDate || undefined,
        toDate: dateRangeInvalid ? undefined : toDate || undefined,
    });

    const isFiltering =
        list.debouncedSearch.length > 0 || fromDate !== "" || toDate !== "";

    const clearFilters = () => {
        list.setSearch("");
        setFromDate("");
        setToDate("");
    };

    return (
        <div className="data-page">
            <div className="data-page-header">
                <div>
                    <h2 className="page-title">Purchases</h2>
                    <p className="data-page-subtitle">
                        Stock bought from suppliers
                    </p>
                </div>

                <div className="data-page-actions">
                    <Link to="/purchases/new" className="btn btn-primary">
                        <i className="bi bi-plus-lg me-2"></i>
                        New Purchase
                    </Link>
                </div>
            </div>

            <PagedTableCard
                list={list}
                noun="purchases"
                searchPlaceholder="Purchase number, supplier invoice or supplier"
                isFiltering={isFiltering}
                onClearFilters={clearFilters}
                emptyIcon="bi-cart-check"
                emptyText="No purchases yet. Record your first purchase to add stock."
                emptyAction={
                    <Link to="/purchases/new" className="btn btn-primary btn-sm">
                        <i className="bi bi-plus-lg me-1"></i>
                        New Purchase
                    </Link>
                }
                banner={
                    dateRangeInvalid ? (
                        <div className="alert alert-warning m-3 mb-0" role="alert">
                            The &quot;from&quot; date is after the
                            &quot;to&quot; date.
                        </div>
                    ) : undefined
                }
                filters={
                    <>
                        <label htmlFor="pur-from" className="visually-hidden">
                            From date
                        </label>
                        <input
                            id="pur-from"
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
                        <label htmlFor="pur-to" className="visually-hidden">
                            To date
                        </label>
                        <input
                            id="pur-to"
                            type="date"
                            className={`form-control form-control-sm${
                                dateRangeInvalid ? " is-invalid" : ""
                            }`}
                            style={{ width: "auto" }}
                            value={toDate}
                            min={fromDate || undefined}
                            onChange={(e) => setToDate(e.target.value)}
                        />
                    </>
                }
                head={
                    <>
                        <th>Purchase</th>
                        <th>Date</th>
                        <th>Supplier</th>
                        <th className="d-none d-md-table-cell">
                            Supplier invoice
                        </th>
                        <th className="text-end">Total</th>
                        <th className="text-end d-none d-md-table-cell">
                            Returned
                        </th>
                        <th className="text-end">Actions</th>
                    </>
                }
                renderRow={(purchase) => (
                    <tr key={purchase.id}>
                        <td className="text-nowrap">
                            <Link
                                to={`/purchases/${purchase.id}`}
                                className="fw-semibold text-decoration-none"
                            >
                                {purchase.purchaseNumber}
                            </Link>
                        </td>
                        <td className="text-nowrap">
                            {formatDate(purchase.purchaseDate)}
                        </td>
                        <td>{purchase.supplierName}</td>
                        <td className="d-none d-md-table-cell">
                            {purchase.supplierInvoiceNumber ?? (
                                <span className="text-muted">—</span>
                            )}
                        </td>
                        <td className="text-end text-nowrap">
                            {formatCurrency(purchase.totalAmount)}
                        </td>
                        <td className="text-end text-nowrap d-none d-md-table-cell">
                            {purchase.returnedAmount > 0 ? (
                                formatCurrency(purchase.returnedAmount)
                            ) : (
                                <span className="text-muted">—</span>
                            )}
                        </td>
                        <td className="text-end">
                            <Link
                                to={`/purchases/${purchase.id}`}
                                className="btn btn-sm btn-outline-secondary"
                                aria-label={`View ${purchase.purchaseNumber}`}
                            >
                                <i className="bi bi-eye"></i>
                            </Link>
                        </td>
                    </tr>
                )}
            />
        </div>
    );
}

export default PurchasesPage;