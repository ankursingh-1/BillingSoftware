import { useState } from "react";
import { Link } from "react-router-dom";

import DateRangeInputs, {
    DateRangeWarning,
} from "../../components/common/DateRangeInputs";
import PagedTableCard from "../../components/common/PagedTableCard";

import { useDateRange } from "../../hooks/useDateRange";
import { usePagedSearch } from "../../hooks/usePagedSearch";
import {
    searchPurchaseReturns,
    searchSalesReturns,
} from "../../services/returns/returnService";
import { formatCurrency, formatDate } from "../../utils/format";
import { isAdmin } from "../../utils/session";

import "../../styles/data-pages.css";
import "../../styles/sales.css";

type Tab = "sales" | "purchases";

function SalesReturnsTab() {
    const range = useDateRange();
    const list = usePagedSearch(
        searchSalesReturns,
        "Unable to load sales returns.",
        range.filters
    );

    return (
        <PagedTableCard
            list={list}
            noun="sales returns"
            searchPlaceholder="Credit note, invoice or customer"
            isFiltering={list.debouncedSearch.length > 0 || range.active}
            onClearFilters={() => {
                list.setSearch("");
                range.reset();
            }}
            emptyIcon="bi-arrow-return-left"
            emptyText="No sales returns yet. Open an invoice and choose Return items."
            banner={<DateRangeWarning range={range} />}
            filters={<DateRangeInputs range={range} idPrefix="sr" />}
            head={
                <>
                    <th>Credit note</th>
                    <th>Date</th>
                    <th>Invoice</th>
                    <th>Customer</th>
                    <th className="text-end">Value</th>
                    <th className="text-end d-none d-md-table-cell">Refunded</th>
                </>
            }
            renderRow={(item) => (
                <tr key={item.id}>
                    <td className="text-nowrap">
                        <Link
                            to={`/returns/sales/${item.id}`}
                            className="fw-semibold text-decoration-none"
                        >
                            {item.returnNumber}
                        </Link>
                    </td>
                    <td className="text-nowrap">{formatDate(item.returnDate)}</td>
                    <td className="text-nowrap">
                        <Link
                            to={`/sales/${item.saleId}`}
                            className="text-decoration-none"
                        >
                            {item.invoiceNumber}
                        </Link>
                    </td>
                    <td>{item.customerName}</td>
                    <td className="text-end text-nowrap">
                        {formatCurrency(item.totalAmount)}
                    </td>
                    <td className="text-end text-nowrap d-none d-md-table-cell">
                        {formatCurrency(item.refundAmount)}
                    </td>
                </tr>
            )}
        />
    );
}

function PurchaseReturnsTab() {
    const range = useDateRange();
    const list = usePagedSearch(
        searchPurchaseReturns,
        "Unable to load purchase returns.",
        range.filters
    );

    return (
        <PagedTableCard
            list={list}
            noun="purchase returns"
            searchPlaceholder="Debit note, purchase or supplier"
            isFiltering={list.debouncedSearch.length > 0 || range.active}
            onClearFilters={() => {
                list.setSearch("");
                range.reset();
            }}
            emptyIcon="bi-arrow-return-right"
            emptyText="No purchase returns yet. Open a purchase and choose Return goods."
            banner={<DateRangeWarning range={range} />}
            filters={<DateRangeInputs range={range} idPrefix="pr" />}
            head={
                <>
                    <th>Debit note</th>
                    <th>Date</th>
                    <th>Purchase</th>
                    <th>Supplier</th>
                    <th className="text-end">Value</th>
                    <th className="text-end d-none d-md-table-cell">
                        Refund received
                    </th>
                </>
            }
            renderRow={(item) => (
                <tr key={item.id}>
                    <td className="text-nowrap">
                        <Link
                            to={`/returns/purchases/${item.id}`}
                            className="fw-semibold text-decoration-none"
                        >
                            {item.returnNumber}
                        </Link>
                    </td>
                    <td className="text-nowrap">{formatDate(item.returnDate)}</td>
                    <td className="text-nowrap">
                        <Link
                            to={`/purchases/${item.purchaseId}`}
                            className="text-decoration-none"
                        >
                            {item.purchaseNumber}
                        </Link>
                    </td>
                    <td>{item.supplierName}</td>
                    <td className="text-end text-nowrap">
                        {formatCurrency(item.totalAmount)}
                    </td>
                    <td className="text-end text-nowrap d-none d-md-table-cell">
                        {formatCurrency(item.refundReceived)}
                    </td>
                </tr>
            )}
        />
    );
}

function ReturnsPage() {
    const admin = isAdmin();
    const [tab, setTab] = useState<Tab>("sales");

    return (
        <div className="data-page">
            <div className="data-page-header">
                <div>
                    <h2 className="page-title">Returns</h2>
                    <p className="data-page-subtitle">
                        Credit notes for customers
                        {admin ? " and debit notes for suppliers" : ""}
                    </p>
                </div>
            </div>

            {admin && (
                <ul className="nav nav-tabs mb-3" role="tablist">
                    {(
                        [
                            ["sales", "Sales returns"],
                            ["purchases", "Purchase returns"],
                        ] as [Tab, string][]
                    ).map(([key, label]) => (
                        <li className="nav-item" key={key} role="presentation">
                            <button
                                type="button"
                                role="tab"
                                aria-selected={tab === key}
                                className={`nav-link${tab === key ? " active" : ""}`}
                                onClick={() => setTab(key)}
                            >
                                {label}
                            </button>
                        </li>
                    ))}
                </ul>
            )}

            {tab === "purchases" && admin ? (
                <PurchaseReturnsTab />
            ) : (
                <SalesReturnsTab />
            )}
        </div>
    );
}

export default ReturnsPage;