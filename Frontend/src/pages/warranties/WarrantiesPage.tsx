import { Link, useSearchParams } from "react-router-dom";

import DateRangeInputs, {
    DateRangeWarning,
} from "../../components/common/DateRangeInputs";
import PagedTableCard from "../../components/common/PagedTableCard";
import { useDateRange } from "../../hooks/useDateRange";
import { usePagedSearch } from "../../hooks/usePagedSearch";
import { loadWarrantyRows } from "../../services/warranty/warrantyService";
import { formatDate } from "../../utils/format";

import type {
    WarrantyClaimReportItem,
    WarrantyListItem,
} from "../../interfaces/warranty/warranty";

import "../../styles/data-pages.css";

const LOAD_ERROR = "Unable to load warranties.";

const FILTERS = [
    ["", "All"],
    ["active", "Active"],
    ["expiring", "Expiring soon"],
    ["expired", "Expired"],
    ["claims", "All claims"],
    ["approved", "Approved claims"],
    ["rejected", "Rejected"],
    ["replacements", "Replacements"],
    ["inspection", "Under inspection"],
] as const;

const CLAIM_KINDS = new Set([
    "claims",
    "approved",
    "rejected",
    "replacements",
    "inspection",
]);

function statusClass(status: string): string {
    if (status === "Expired" || status === "Voided" || status === "Rejected") {
        return "text-bg-secondary";
    }
    if (status === "ExpiringSoon" || status === "UnderInspection") {
        return "text-bg-warning";
    }
    return "text-bg-success";
}

function isClaimRow(
    row: WarrantyListItem | WarrantyClaimReportItem
): row is WarrantyClaimReportItem {
    return "claimNumber" in row;
}

function WarrantiesPage() {
    const range = useDateRange();
    const [params, setParams] = useSearchParams();
    const status = (params.get("status") ?? "").trim().toLowerCase();
    const claimView = CLAIM_KINDS.has(status);
    const list = usePagedSearch(loadWarrantyRows, LOAD_ERROR, {
        ...range.filters,
        status: status || undefined,
    });

    const setStatus = (id: string) => {
        const next = new URLSearchParams(params);
        if (id) {
            next.set("status", id);
        } else {
            next.delete("status");
        }
        setParams(next, { replace: true });
    };

    return (
        <div className="data-page">
            <div className="data-page-header">
                <div>
                    <h2 className="page-title">Warranty management</h2>
                    <p className="data-page-subtitle">
                        Search by warranty number, invoice, mobile, customer,
                        product or serial. Expired warranties stay searchable.
                        Claim filters list claims, not the warranty register.
                    </p>
                </div>
            </div>

            <PagedTableCard
                list={list}
                noun={claimView ? "claims" : "warranties"}
                searchPlaceholder="Warranty no, invoice, mobile, serial…"
                filters={
                    <>
                        {FILTERS.map(([id, label]) => (
                            <button
                                key={id || "all"}
                                type="button"
                                className={`btn btn-sm ${
                                    status === id
                                        ? "btn-primary"
                                        : "btn-outline-secondary"
                                }`}
                                onClick={() => setStatus(id)}
                            >
                                {label}
                            </button>
                        ))}
                        <DateRangeInputs range={range} idPrefix="wrn" />
                    </>
                }
                banner={<DateRangeWarning range={range} />}
                emptyIcon="bi-shield-check"
                emptyText={
                    claimView ? "No claims match." : "No warranties match."
                }
                isFiltering={
                    list.debouncedSearch.length > 0 ||
                    status.length > 0 ||
                    range.from.length > 0 ||
                    range.to.length > 0
                }
                onClearFilters={() => {
                    list.setSearch("");
                    setStatus("");
                    range.setFrom("");
                    range.setTo("");
                }}
                head={
                    claimView ? (
                        <tr>
                            <th>Claim</th>
                            <th>Warranty</th>
                            <th>Invoice</th>
                            <th>Customer</th>
                            <th>Product</th>
                            <th>Submitted</th>
                            <th>Status</th>
                        </tr>
                    ) : (
                        <tr>
                            <th>Warranty</th>
                            <th>Invoice</th>
                            <th>Customer</th>
                            <th>Product</th>
                            <th>Duration</th>
                            <th>End</th>
                            <th>Days left</th>
                            <th>Status</th>
                        </tr>
                    )
                }
                renderRow={(row) =>
                    isClaimRow(row) ? (
                        <tr key={row.claimNumber}>
                            <td>
                                {row.warrantyId ? (
                                    <Link to={`/warranties/${row.warrantyId}`}>
                                        {row.claimNumber}
                                    </Link>
                                ) : (
                                    row.claimNumber
                                )}
                            </td>
                            <td>{row.warrantyNumber}</td>
                            <td>
                                {row.saleId ? (
                                    <Link to={`/sales/${row.saleId}`}>
                                        {row.invoiceNumber}
                                    </Link>
                                ) : (
                                    row.invoiceNumber
                                )}
                            </td>
                            <td>{row.customerName}</td>
                            <td>
                                {row.productName}
                                {row.replacementProductName && (
                                    <div className="small text-muted">
                                        Replacement {row.replacementProductName}
                                    </div>
                                )}
                            </td>
                            <td>{formatDate(row.submittedOn)}</td>
                            <td>
                                <span className={`badge ${statusClass(row.status)}`}>
                                    {row.status}
                                </span>
                            </td>
                        </tr>
                    ) : (
                        <tr key={row.id}>
                            <td>
                                <Link to={`/warranties/${row.id}`}>
                                    {row.warrantyNumber}
                                </Link>
                                {row.serialNumber && (
                                    <div className="small text-muted">
                                        SN {row.serialNumber}
                                    </div>
                                )}
                            </td>
                            <td>
                                <Link to={`/sales/${row.saleId}`}>
                                    {row.invoiceNumber}
                                </Link>
                            </td>
                            <td>
                                {row.customerName}
                                <div className="small text-muted">
                                    {row.customerMobile}
                                </div>
                            </td>
                            <td>{row.productName}</td>
                            <td>{row.duration}</td>
                            <td>{formatDate(row.endDate)}</td>
                            <td>{row.daysRemaining}</td>
                            <td>
                                <span className={`badge ${statusClass(row.status)}`}>
                                    {row.status}
                                </span>
                            </td>
                        </tr>
                    )
                }
            />
        </div>
    );
}

export default WarrantiesPage;
