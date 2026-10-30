import { useEffect, useState } from "react";
import { Link } from "react-router-dom";
import { toast } from "react-toastify";

import DateRangeInputs, {
    DateRangeWarning,
} from "../../components/common/DateRangeInputs";
import Pagination from "../../components/common/Pagination";

import { useDateRange } from "../../hooks/useDateRange";
import {
    downloadReportExcel,
    downloadReportPdf,
    getCustomerReport,
    getExpenseReport,
    getPaymentReport,
    getProfitReport,
    getPurchaseReport,
    getSalesReport,
    getStockReport,
    getSupplierReport,
} from "../../services/reports/reportService";
import { getApiErrorMessage } from "../../utils/apiError";
import { formatCurrency, formatDate } from "../../utils/format";

import type { ReportKind } from "../../services/reports/reportService";
import type {
    CustomerReportRow,
    ExpenseReportRow,
    PaymentReportRow,
    ProfitReport,
    PurchaseReportRow,
    SalesReportRow,
    StockReportRow,
    SupplierReportRow,
} from "../../services/reports/reportService";

import "../../styles/data-pages.css";

type StockFilter = "all" | "low" | "out";

function ReportsPage() {
    const range = useDateRange();
    const [tab, setTab] = useState<ReportKind>("sales");
    const [stockFilter, setStockFilter] = useState<StockFilter>("all");
    const [loading, setLoading] = useState(true);
    const [exporting, setExporting] = useState(false);
    const [sales, setSales] = useState<SalesReportRow[]>([]);
    const [purchases, setPurchases] = useState<PurchaseReportRow[]>([]);
    const [stock, setStock] = useState<StockReportRow[]>([]);
    const [expenses, setExpenses] = useState<ExpenseReportRow[]>([]);
    const [customers, setCustomers] = useState<CustomerReportRow[]>([]);
    const [suppliers, setSuppliers] = useState<SupplierReportRow[]>([]);
    const [payments, setPayments] = useState<PaymentReportRow[]>([]);
    const [profit, setProfit] = useState<ProfitReport | null>(null);
    const [total, setTotal] = useState<number | null>(null);
    const [note, setNote] = useState<string | null>(null);
    const [paging, setPaging] = useState({
        totalRecords: 0,
        totalPages: 1,
        pageSize: 50,
    });

    const pageSize = 50;
    const filterKey = `${tab}|${stockFilter}|${range.from}|${range.to}|${String(range.invalid)}`;
    const [pageState, setPageState] = useState({ page: 1, forQuery: filterKey });
    const page = pageState.forQuery === filterKey ? pageState.page : 1;

    const query = {
        ...range.filters,
        pageNumber: page,
        pageSize,
        lowStockOnly: tab === "stock" && stockFilter === "low" ? true : undefined,
        outOfStockOnly:
            tab === "stock" && stockFilter === "out" ? true : undefined,
    };

    const rememberPage = (data: {
        totalRecords?: number;
        totalPages?: number;
        pageSize?: number;
        items?: unknown[];
    }) => {
        const records = data.totalRecords ?? data.items?.length ?? 0;
        setPaging({
            totalRecords: records,
            totalPages: data.totalPages ?? 1,
            pageSize: data.pageSize ?? pageSize,
        });
    };

    useEffect(() => {
        let cancelled = false;

        const load = async () => {
            setLoading(true);
            setNote(null);

            try {
                if (tab === "sales") {
                    const data = await getSalesReport(query);
                    if (!cancelled) {
                        setSales(data.items);
                        setTotal(data.grandTotal ?? 0);
                        rememberPage(data);
                    }
                } else if (tab === "purchases") {
                    const data = await getPurchaseReport(query);
                    if (!cancelled) {
                        setPurchases(data.items);
                        setTotal(data.grandTotal ?? 0);
                        rememberPage(data);
                    }
                } else if (tab === "stock") {
                    const data = await getStockReport(query);
                    if (!cancelled) {
                        setStock(data.items);
                        setTotal(data.totalStockValue ?? 0);
                        rememberPage(data);
                    }
                } else if (tab === "profit") {
                    const data = await getProfitReport(query);
                    if (!cancelled) {
                        setProfit(data);
                        setTotal(data.grossProfit);
                        rememberPage({ items: data.items });
                    }
                } else if (tab === "expenses") {
                    const data = await getExpenseReport(query);
                    if (!cancelled) {
                        setExpenses(data.items);
                        setTotal(data.grandTotal ?? 0);
                        rememberPage(data);
                    }
                } else if (tab === "customers") {
                    const data = await getCustomerReport(query);
                    if (!cancelled) {
                        setCustomers(data.items);
                        setTotal(data.totalOutstanding ?? 0);
                        rememberPage(data);
                    }
                } else if (tab === "suppliers") {
                    const data = await getSupplierReport(query);
                    if (!cancelled) {
                        setSuppliers(data.items);
                        setTotal(data.totalPurchased ?? 0);
                        setNote(data.limitation ?? null);
                        rememberPage(data);
                    }
                } else {
                    const data = await getPaymentReport(query);
                    if (!cancelled) {
                        setPayments(data.items);
                        setTotal(data.totalCollected ?? 0);
                        rememberPage(data);
                    }
                }
            } catch (error) {
                if (!cancelled) {
                    toast.error(
                        getApiErrorMessage(error, "Unable to load the report.")
                    );
                }
            } finally {
                if (!cancelled) setLoading(false);
            }
        };

        void load();

        return () => {
            cancelled = true;
        };
        // Query fields are listed directly. page resets when filterKey changes.
        // eslint-disable-next-line react-hooks/exhaustive-deps
    }, [tab, stockFilter, range.from, range.to, range.invalid, page]);

    const handleExcel = () => {
        setExporting(true);
        void downloadReportExcel(tab, query)
            .catch((error) =>
                toast.error(
                    getApiErrorMessage(error, "Unable to download Excel.")
                )
            )
            .finally(() => setExporting(false));
    };

    const handlePdf = () => {
        if (tab !== "sales" && tab !== "profit") {
            return;
        }

        setExporting(true);
        void downloadReportPdf(tab, query)
            .catch((error) =>
                toast.error(getApiErrorMessage(error, "Unable to download PDF."))
            )
            .finally(() => setExporting(false));
    };

    const tabs: { id: ReportKind; label: string }[] = [
        { id: "sales", label: "Sales" },
        { id: "purchases", label: "Purchases" },
        { id: "stock", label: "Stock" },
        { id: "customers", label: "Customers" },
        { id: "suppliers", label: "Suppliers" },
        { id: "payments", label: "Collections" },
        { id: "expenses", label: "Expenses" },
        { id: "profit", label: "Gross profit" },
    ];

    const warrantyKinds = [
        ["active", "Active warranties"],
        ["expiring", "Expiring"],
        ["expired", "Expired"],
        ["claims", "All claims"],
        ["approved", "Approved claims"],
        ["rejected", "Rejected claims"],
        ["replacements", "Replacements"],
    ] as const;

    return (
        <div className="data-page">
            <div className="data-page-header">
                <div>
                    <h2 className="page-title">Reports</h2>
                    <p className="data-page-subtitle">
                        Figures come from saved invoices, purchases and stock.
                        Gross profit uses the cost recorded on each sale line,
                        never sales minus purchases. Voided payments are
                        excluded from collections. Warranty lists live on the
                        Warranty page (in-app only — no SMS or email).
                    </p>
                </div>
                <div className="d-flex flex-wrap gap-2">
                    {(tab === "sales" || tab === "profit") && (
                        <button
                            type="button"
                            className="btn btn-outline-secondary"
                            disabled={exporting}
                            onClick={handlePdf}
                        >
                            Download PDF
                        </button>
                    )}
                    <button
                        type="button"
                        className="btn btn-outline-primary"
                        disabled={exporting}
                        onClick={handleExcel}
                    >
                        Download Excel
                    </button>
                </div>
            </div>

            <div className="data-card mb-3 p-3">
                <h6 className="mb-2">Warranty reports</h6>
                <div className="d-flex flex-wrap gap-2">
                    {warrantyKinds.map(([kind, label]) => (
                        <Link
                            key={kind}
                            className="btn btn-sm btn-outline-secondary"
                            to={`/warranties?status=${kind}`}
                        >
                            {label}
                        </Link>
                    ))}
                </div>
            </div>

            <div className="data-card">
                <div className="data-toolbar">
                    <div className="d-flex flex-wrap gap-2">
                        {tabs.map((item) => (
                            <button
                                key={item.id}
                                type="button"
                                className={`btn btn-sm ${
                                    tab === item.id
                                        ? "btn-primary"
                                        : "btn-outline-secondary"
                                }`}
                                onClick={() => setTab(item.id)}
                            >
                                {item.label}
                            </button>
                        ))}
                    </div>
                    {tab !== "stock" && (
                        <div className="d-flex align-items-center gap-2">
                            <DateRangeInputs range={range} idPrefix="rep" />
                        </div>
                    )}
                </div>

                {tab === "stock" && (
                    <div className="d-flex flex-wrap gap-2 px-3 pb-2">
                        {(
                            [
                                ["all", "All stock"],
                                ["low", "Low stock"],
                                ["out", "Out of stock"],
                            ] as const
                        ).map(([id, label]) => (
                            <button
                                key={id}
                                type="button"
                                className={`btn btn-sm ${
                                    stockFilter === id
                                        ? "btn-primary"
                                        : "btn-outline-secondary"
                                }`}
                                onClick={() => setStockFilter(id)}
                            >
                                {label}
                            </button>
                        ))}
                    </div>
                )}

                <DateRangeWarning range={range} />

                {loading ? (
                    <div className="state-block">
                        <div
                            className="spinner-border text-primary"
                            role="status"
                        >
                            <span className="visually-hidden">Loading...</span>
                        </div>
                    </div>
                ) : (
                    <div className="table-responsive">
                        {tab === "profit" && profit && (
                            <div className="p-3">
                                {profit.limitation && (
                                    <div className="alert alert-warning">
                                        {profit.limitation}
                                    </div>
                                )}
                                <p className="mb-1">
                                    Net sales (ex-tax, after returns){" "}
                                    {formatCurrency(profit.totalSales)}.
                                </p>
                                <p className="mb-1">
                                    {profit.costComplete
                                        ? `Gross profit ${formatCurrency(profit.grossProfit ?? 0)}.`
                                        : profit.grossProfit !== null
                                          ? `Known gross profit ${formatCurrency(profit.grossProfit)} (complete invoices only).`
                                          : "Gross profit unavailable — no invoice in this range has a full cost snapshot."}
                                    {profit.profitPercentage !== null
                                        ? ` Margin ${profit.profitPercentage.toFixed(2)}%.`
                                        : ""}
                                </p>
                                <p className="mb-0 text-muted small">
                                    Complete invoices{" "}
                                    {profit.completeInvoices ?? 0}. Incomplete{" "}
                                    {profit.incompleteInvoices ?? 0}. Coverage{" "}
                                    {(profit.coveragePercent ?? 0).toFixed(1)}%.
                                    Lines with cost {profit.linesWithCost},
                                    missing {profit.linesMissingCost}.
                                </p>
                            </div>
                        )}

                        {note && (
                            <div className="alert alert-info mx-3 mt-3">
                                {note}
                            </div>
                        )}

                        <table className="table align-middle mb-0">
                            <thead>
                                {tab === "sales" && (
                                    <tr>
                                        <th>Invoice</th>
                                        <th>Customer</th>
                                        <th>Date</th>
                                        <th className="text-end">Amount</th>
                                    </tr>
                                )}
                                {tab === "purchases" && (
                                    <tr>
                                        <th>Bill</th>
                                        <th>Supplier</th>
                                        <th>Date</th>
                                        <th className="text-end">Amount</th>
                                    </tr>
                                )}
                                {tab === "stock" && (
                                    <tr>
                                        <th>Product</th>
                                        <th>SKU</th>
                                        <th className="text-end">Stock</th>
                                        <th className="text-end">Value</th>
                                    </tr>
                                )}
                                {tab === "profit" && (
                                    <tr>
                                        <th>Invoice</th>
                                        <th>Date</th>
                                        <th className="text-end">Net sales</th>
                                        <th className="text-end">Cost</th>
                                        <th className="text-end">Profit</th>
                                        <th>Cost data</th>
                                    </tr>
                                )}
                                {tab === "expenses" && (
                                    <tr>
                                        <th>Date</th>
                                        <th>Title</th>
                                        <th>Category</th>
                                        <th className="text-end">Amount</th>
                                    </tr>
                                )}
                                {tab === "customers" && (
                                    <tr>
                                        <th>Customer</th>
                                        <th>Mobile</th>
                                        <th className="text-end">Invoices</th>
                                        <th className="text-end">Billed</th>
                                        <th className="text-end">Outstanding</th>
                                    </tr>
                                )}
                                {tab === "suppliers" && (
                                    <tr>
                                        <th>Supplier</th>
                                        <th>Company</th>
                                        <th className="text-end">Purchases</th>
                                        <th className="text-end">Purchased</th>
                                    </tr>
                                )}
                                {tab === "payments" && (
                                    <tr>
                                        <th>Receipt</th>
                                        <th>Invoice</th>
                                        <th>Customer</th>
                                        <th>Date</th>
                                        <th>Method</th>
                                        <th className="text-end">Amount</th>
                                    </tr>
                                )}
                            </thead>
                            <tbody>
                                {tab === "sales" &&
                                    sales.map((row) => (
                                        <tr key={row.saleId}>
                                            <td>{row.invoiceNumber}</td>
                                            <td>{row.customerName}</td>
                                            <td>{formatDate(row.saleDate)}</td>
                                            <td className="text-end">
                                                {formatCurrency(row.totalAmount)}
                                            </td>
                                        </tr>
                                    ))}
                                {tab === "purchases" &&
                                    purchases.map((row) => (
                                        <tr key={row.purchaseId}>
                                            <td>{row.purchaseNumber}</td>
                                            <td>{row.supplierName}</td>
                                            <td>
                                                {formatDate(row.purchaseDate)}
                                            </td>
                                            <td className="text-end">
                                                {formatCurrency(row.totalAmount)}
                                            </td>
                                        </tr>
                                    ))}
                                {tab === "stock" &&
                                    stock.map((row) => (
                                        <tr key={row.productId}>
                                            <td>{row.productName}</td>
                                            <td>{row.sku}</td>
                                            <td className="text-end">
                                                {row.currentStock}
                                            </td>
                                            <td className="text-end">
                                                {formatCurrency(row.stockValue)}
                                            </td>
                                        </tr>
                                    ))}
                                {tab === "profit" &&
                                    profit?.items.map((row) => (
                                        <tr key={row.saleId || row.invoiceNumber}>
                                            <td>{row.invoiceNumber}</td>
                                            <td>{formatDate(row.saleDate)}</td>
                                            <td className="text-end">
                                                {formatCurrency(row.saleAmount)}
                                            </td>
                                            <td className="text-end">
                                                {row.costOfGoodsSold == null
                                                    ? "Cost unavailable"
                                                    : formatCurrency(
                                                          row.costOfGoodsSold
                                                      )}
                                            </td>
                                            <td className="text-end">
                                                {row.profit == null
                                                    ? "—"
                                                    : formatCurrency(row.profit)}
                                            </td>
                                            <td>{row.costStatus}</td>
                                        </tr>
                                    ))}
                                {tab === "expenses" &&
                                    expenses.map((row) => (
                                        <tr key={row.id}>
                                            <td>
                                                {formatDate(row.expenseDate)}
                                            </td>
                                            <td>{row.title}</td>
                                            <td>{row.category ?? "—"}</td>
                                            <td className="text-end">
                                                {formatCurrency(row.amount)}
                                            </td>
                                        </tr>
                                    ))}
                                {tab === "customers" &&
                                    customers.map((row) => (
                                        <tr key={row.customerId}>
                                            <td>{row.name}</td>
                                            <td>{row.mobile}</td>
                                            <td className="text-end">
                                                {row.invoiceCount}
                                            </td>
                                            <td className="text-end">
                                                {formatCurrency(row.billed)}
                                            </td>
                                            <td className="text-end">
                                                {formatCurrency(row.outstanding)}
                                            </td>
                                        </tr>
                                    ))}
                                {tab === "suppliers" &&
                                    suppliers.map((row) => (
                                        <tr key={row.supplierId}>
                                            <td>{row.name}</td>
                                            <td>{row.companyName}</td>
                                            <td className="text-end">
                                                {row.purchaseCount}
                                            </td>
                                            <td className="text-end">
                                                {formatCurrency(row.purchased)}
                                            </td>
                                        </tr>
                                    ))}
                                {tab === "payments" &&
                                    payments.map((row) => (
                                        <tr key={row.paymentId}>
                                            <td>{row.receiptNumber ?? "—"}</td>
                                            <td>{row.invoiceNumber}</td>
                                            <td>{row.customerName}</td>
                                            <td>
                                                {formatDate(row.paymentDate)}
                                            </td>
                                            <td>{row.method}</td>
                                            <td className="text-end">
                                                {formatCurrency(row.amount)}
                                            </td>
                                        </tr>
                                    ))}
                            </tbody>
                        </table>

                        {total !== null && tab !== "profit" && (
                            <div className="p-3 border-top fw-semibold">
                                {tab === "customers"
                                    ? "Outstanding "
                                    : tab === "payments"
                                      ? "Collected "
                                      : "Total "}
                                {formatCurrency(total)}
                            </div>
                        )}

                        {tab !== "profit" && (
                            <Pagination
                                page={page}
                                totalPages={paging.totalPages}
                                totalRecords={paging.totalRecords}
                                pageSize={paging.pageSize}
                                disabled={loading}
                                onPageChange={(next) =>
                                    setPageState({ page: next, forQuery: filterKey })
                                }
                            />
                        )}
                    </div>
                )}
            </div>
        </div>
    );
}

export default ReportsPage;
