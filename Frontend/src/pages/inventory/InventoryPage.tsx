import { useEffect, useState } from "react";

import DateRangeInputs, {
    DateRangeWarning,
} from "../../components/common/DateRangeInputs";
import PagedTableCard from "../../components/common/PagedTableCard";
import AdjustStockModal from "../../components/inventory/AdjustStockModal";
import StockStatusBadge from "../../components/inventory/StockStatusBadge";

import { useDateRange } from "../../hooks/useDateRange";
import { usePagedSearch } from "../../hooks/usePagedSearch";
import {
    getStockSummary,
    searchAdjustments,
    searchLedger,
    searchStock,
} from "../../services/inventory/inventoryService";
import { getApiErrorMessage } from "../../utils/apiError";
import { formatCurrency, formatDateTime } from "../../utils/format";
import { isAdmin } from "../../utils/session";

import type {
    StockItem,
    StockSearchRequest,
    StockSummary,
} from "../../interfaces/inventory/inventory";

import "../../styles/data-pages.css";
import "../../styles/sales.css";

type Tab = "stock" | "movements" | "adjustments";

const dash = <span className="text-muted">—</span>;

const MOVEMENT_TYPES = [
    "OpeningStock",
    "Purchase",
    "Sale",
    "PurchaseReturn",
    "SalesReturn",
    "Adjustment",
    "AdjustmentOut",
    "Damaged",
    "Expired",
    "Lost",
];

const typeLabel = (value: string) =>
    value.replace(/([a-z])([A-Z])/g, "$1 $2");

function SummaryCards({ summary }: { summary: StockSummary | null }) {
    const cards = [
        { label: "Products", value: summary?.totalProducts, tone: "" },
        {
            label: "Low stock",
            value: summary?.lowStock,
            tone: "text-warning",
        },
        {
            label: "Out of stock",
            value: summary?.outOfStock,
            tone: "text-danger",
        },
        { label: "Units on hand", value: summary?.totalUnits, tone: "" },
    ];

    return (
        <div className="row g-3 mb-3">
            {cards.map((card) => (
                <div className="col-6 col-lg-3" key={card.label}>
                    <div className="data-card p-3">
                        <div className="text-muted small">{card.label}</div>
                        <div className={`fs-4 fw-bold ${card.tone}`}>
                            {card.value ?? "…"}
                        </div>
                    </div>
                </div>
            ))}

            <div className="col-12">
                <div className="data-card p-3 d-flex flex-wrap justify-content-between gap-2">
                    <span>
                        Stock value at cost:{" "}
                        <strong>
                            {summary
                                ? formatCurrency(summary.stockValueAtCost)
                                : "…"}
                        </strong>
                    </span>
                    {summary && summary.productsWithoutCost > 0 && (
                        <span className="text-warning">
                            {summary.productsWithoutCost} product(s) have no
                            cost price and are not included in this value.
                        </span>
                    )}
                    {summary && (
                        <span className="text-muted">
                            Low stock means {summary.lowStockThreshold} or
                            fewer units.
                        </span>
                    )}
                </div>
            </div>
        </div>
    );
}

function StockTab({ admin }: { admin: boolean }) {
    const [status, setStatus] = useState<StockSearchRequest["status"]>("");
    const [summary, setSummary] = useState<StockSummary | null>(null);
    const [summaryError, setSummaryError] = useState<string | null>(null);
    const [adjusting, setAdjusting] = useState<StockItem | null>(null);

    const list = usePagedSearch(searchStock, "Unable to load stock.", {
        status,
    });

    const { reload } = list;
    const [summaryKey, setSummaryKey] = useState(0);

    useEffect(() => {
        let cancelled = false;

        getStockSummary()
            .then((data) => {
                if (!cancelled) {
                    setSummary(data);
                    setSummaryError(null);
                }
            })
            .catch((err) => {
                if (!cancelled) {
                    setSummaryError(
                        getApiErrorMessage(err, "Unable to load the summary.")
                    );
                }
            });

        return () => {
            cancelled = true;
        };
    }, [summaryKey]);

    return (
        <>
            {summaryError ? (
                <div className="alert alert-danger" role="alert">
                    {summaryError}
                </div>
            ) : (
                <SummaryCards summary={summary} />
            )}

            <PagedTableCard
                list={list}
                noun="products"
                searchPlaceholder="Product name or SKU"
                isFiltering={list.debouncedSearch.length > 0 || status !== ""}
                onClearFilters={() => {
                    list.setSearch("");
                    setStatus("");
                }}
                emptyIcon="bi-boxes"
                emptyText="No products yet."
                filters={
                    <select
                        className="form-select form-select-sm"
                        style={{ width: "auto" }}
                        aria-label="Stock status"
                        value={status}
                        onChange={(e) =>
                            setStatus(
                                e.target.value as StockSearchRequest["status"]
                            )
                        }
                    >
                        <option value="">All stock</option>
                        <option value="in">In stock</option>
                        <option value="low">Low stock</option>
                        <option value="out">Out of stock</option>
                    </select>
                }
                head={
                    <>
                        <th>Product</th>
                        <th className="d-none d-md-table-cell">Category</th>
                        <th className="text-end">Stock</th>
                        <th>Status</th>
                        <th className="text-end d-none d-md-table-cell">
                            Cost
                        </th>
                        <th className="text-end d-none d-lg-table-cell">
                            Value
                        </th>
                        {admin && <th className="text-end">Actions</th>}
                    </>
                }
                renderRow={(item) => (
                    <tr key={item.productId}>
                        <td>
                            <strong>{item.name}</strong>
                            <div className="small text-muted">{item.sku}</div>
                        </td>
                        <td className="d-none d-md-table-cell">
                            {item.categoryName ?? dash}
                        </td>
                        <td className="text-end">
                            {item.stock}
                            {item.unitName && (
                                <span className="text-muted small">
                                    {" "}
                                    {item.unitName}
                                </span>
                            )}
                        </td>
                        <td>
                            <StockStatusBadge status={item.status} />
                        </td>
                        <td className="text-end text-nowrap d-none d-md-table-cell">
                            {item.purchasePrice > 0
                                ? formatCurrency(item.purchasePrice)
                                : dash}
                        </td>
                        <td className="text-end text-nowrap d-none d-lg-table-cell">
                            {item.stockValue !== null
                                ? formatCurrency(item.stockValue)
                                : dash}
                        </td>
                        {admin && (
                            <td className="text-end">
                                <button
                                    type="button"
                                    className="btn btn-sm btn-outline-primary"
                                    onClick={() => setAdjusting(item)}
                                >
                                    Adjust
                                </button>
                            </td>
                        )}
                    </tr>
                )}
            />

            {adjusting && (
                <AdjustStockModal
                    item={adjusting}
                    onClose={() => setAdjusting(null)}
                    onSaved={() => {
                        setAdjusting(null);
                        reload();
                        setSummaryKey((key) => key + 1);
                    }}
                />
            )}
        </>
    );
}

function MovementsTab() {
    const range = useDateRange();
    const [type, setType] = useState("");

    const list = usePagedSearch(searchLedger, "Unable to load stock movements.", {
        ...range.filters,
        transactionType: type,
    });

    return (
        <PagedTableCard
            list={list}
            noun="stock movements"
            searchPlaceholder="Product, SKU or reference"
            isFiltering={
                list.debouncedSearch.length > 0 || range.active || type !== ""
            }
            onClearFilters={() => {
                list.setSearch("");
                range.reset();
                setType("");
            }}
            emptyIcon="bi-journal-text"
            emptyText="No stock movements yet."
            banner={<DateRangeWarning range={range} />}
            filters={
                <>
                    <DateRangeInputs range={range} idPrefix="mv" />
                    <select
                        className="form-select form-select-sm"
                        style={{ width: "auto" }}
                        aria-label="Movement type"
                        value={type}
                        onChange={(e) => setType(e.target.value)}
                    >
                        <option value="">All types</option>
                        {MOVEMENT_TYPES.map((value) => (
                            <option key={value} value={value}>
                                {typeLabel(value)}
                            </option>
                        ))}
                    </select>
                </>
            }
            head={
                <>
                    <th>When</th>
                    <th>Product</th>
                    <th>Type</th>
                    <th className="text-end">Change</th>
                    <th className="text-end d-none d-md-table-cell">Before</th>
                    <th className="text-end">After</th>
                    <th className="d-none d-lg-table-cell">Reference</th>
                    <th className="d-none d-lg-table-cell">By</th>
                </>
            }
            renderRow={(entry) => (
                <tr key={entry.id}>
                    <td className="text-nowrap">
                        {formatDateTime(entry.createdOn)}
                    </td>
                    <td>
                        <strong>{entry.productName}</strong>
                        {entry.sku && (
                            <div className="small text-muted">{entry.sku}</div>
                        )}
                    </td>
                    <td>{typeLabel(entry.transactionType)}</td>
                    <td
                        className={`text-end fw-semibold ${
                            entry.quantityChange < 0
                                ? "text-danger"
                                : "text-success"
                        }`}
                    >
                        {entry.quantityChange > 0 ? "+" : ""}
                        {entry.quantityChange}
                    </td>
                    <td className="text-end d-none d-md-table-cell">
                        {entry.previousStock}
                    </td>
                    <td className="text-end">{entry.currentStock}</td>
                    <td className="d-none d-lg-table-cell">
                        {entry.referenceNo ?? dash}
                    </td>
                    <td className="d-none d-lg-table-cell">
                        {entry.createdBy ?? dash}
                    </td>
                </tr>
            )}
        />
    );
}

function AdjustmentsTab() {
    const range = useDateRange();
    const list = usePagedSearch(
        searchAdjustments,
        "Unable to load stock adjustments.",
        range.filters
    );

    return (
        <PagedTableCard
            list={list}
            noun="stock adjustments"
            searchPlaceholder="Product, SKU, reference or remarks"
            isFiltering={list.debouncedSearch.length > 0 || range.active}
            onClearFilters={() => {
                list.setSearch("");
                range.reset();
            }}
            emptyIcon="bi-sliders"
            emptyText="No stock adjustments yet. Use Adjust on the Stock tab."
            banner={<DateRangeWarning range={range} />}
            filters={<DateRangeInputs range={range} idPrefix="adj" />}
            head={
                <>
                    <th>When</th>
                    <th>Reference</th>
                    <th>Product</th>
                    <th>Reason</th>
                    <th className="text-end">Change</th>
                    <th className="text-end d-none d-md-table-cell">
                        Before → After
                    </th>
                    <th className="d-none d-lg-table-cell">Remarks</th>
                    <th className="d-none d-lg-table-cell">By</th>
                </>
            }
            renderRow={(entry) => (
                <tr key={entry.id}>
                    <td className="text-nowrap">
                        {formatDateTime(entry.adjustmentDate)}
                    </td>
                    <td className="text-nowrap">{entry.referenceNo ?? dash}</td>
                    <td>
                        <strong>{entry.productName}</strong>
                        {entry.sku && (
                            <div className="small text-muted">{entry.sku}</div>
                        )}
                    </td>
                    <td>{typeLabel(entry.reason)}</td>
                    <td
                        className={`text-end fw-semibold ${
                            entry.increaseStock ? "text-success" : "text-danger"
                        }`}
                    >
                        {entry.increaseStock ? "+" : "−"}
                        {entry.quantity}
                    </td>
                    <td className="text-end text-nowrap d-none d-md-table-cell">
                        {entry.previousStock} → {entry.currentStock}
                    </td>
                    <td className="d-none d-lg-table-cell">
                        {entry.remarks ?? dash}
                    </td>
                    <td className="d-none d-lg-table-cell">
                        {entry.createdBy ?? dash}
                    </td>
                </tr>
            )}
        />
    );
}

function InventoryPage() {
    const admin = isAdmin();
    const [tab, setTab] = useState<Tab>("stock");

    const tabs: [Tab, string][] = [
        ["stock", "Stock"],
        ["movements", "Movements"],
        ...(admin ? ([["adjustments", "Adjustments"]] as [Tab, string][]) : []),
    ];

    return (
        <div className="data-page">
            <div className="data-page-header">
                <div>
                    <h2 className="page-title">Inventory</h2>
                    <p className="data-page-subtitle">
                        Current stock, every movement, and manual adjustments
                    </p>
                </div>
            </div>

            <ul className="nav nav-tabs mb-3" role="tablist">
                {tabs.map(([key, label]) => (
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

            {tab === "stock" && <StockTab admin={admin} />}
            {tab === "movements" && <MovementsTab />}
            {tab === "adjustments" && admin && <AdjustmentsTab />}
        </div>
    );
}

export default InventoryPage;