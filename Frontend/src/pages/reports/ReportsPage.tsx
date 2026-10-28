import { useEffect, useState } from "react";
import { toast } from "react-toastify";

import DateRangeInputs, {
    DateRangeWarning,
} from "../../components/common/DateRangeInputs";

import { useDateRange } from "../../hooks/useDateRange";
import {
    downloadReportExcel,
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

    const query = {
        ...range.filters,
        pageNumber: 1,
        pageSize: 50,
        lowStockOnly: tab === "stock" && stockFilter === "low" ? true : undefined,
        outOfStockOnly:
            tab === "stock" && stockFilter === "out" ? true : undefined,
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
                    }
                } else if (tab === "purchases") {
                    const data = await getPurchaseReport(query);
                    if (!cancelled) {
                        setPurchases(data.items);
                        setTotal(data.grandTotal ?? 0);
                    }
                } else if (tab === "stock") {
                    const data = await getStockReport(query);
                    if (!cancelled) {
                        setStock(data.items);
                        setTotal(data.totalStockValue ?? 0);
                    }
                } else if (tab === "profit") {
                    const data = await getProfitReport(query);
                    if (!cancelled) {
                        setProfit(data);
                        setTotal(data.grossProfit);
                    }
                } else if (tab === "expenses") {
                    const data = await getExpenseReport(query);
                    if (!cancelled) {
                        setExpenses(data.items);
                        setTotal(data.grandTotal ?? 0);
                    }
                } else if (tab === "customers") {
                    const data = await getCustomerReport(query);
                    if (!cancelled) {
                        setCustomers(data.items);
                        setTotal(data.totalOutstanding ?? 0);
                    }
                } else if (tab === "suppliers") {
                    const data = await getSupplierReport(query);
                    if (!cancelled) {
                        setSuppliers(data.items);
                        setTotal(data.totalPurchased ?? 0);
                        setNote(data.limitation ?? null);
                    }
                } else {
                    const data = await getPaymentReport(query);
                    if (!cancelled) {
                        setPayments(data.items);
                        setTotal(data.totalCollected ?? 0);
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
        // eslint-disable-next-line react-hooks/exhaustive-deps
    }, [tab, stockFilter, range.from, range.to, range.invalid]);

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

    return (
        <div className="data-page">
            <div className="data-page-header">
                <div>
                    <h2 className="page-title">Reports</h2>
                    <p className="data-page-subtitle">
                        Figures come from saved invoices, purchases and stock.
                        Gross profit uses the cost recorded on each sale line,
                        never sales minus purchases. Voided payments are
                        excluded from collections.
                    </p>
                </div>
                <button
                    type="button"
                    className="btn btn-outline-primary"
                    disabled={exporting}
                    onClick={handleExcel}
                >
                    Download Excel
                </button>
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
                                <p className="mb-2">
                                    Sales {formatCurrency(profit.totalSales)}.
                                    {profit.costComplete
                                        ? ` Cost of goods sold ${formatCurrency(profit.costOfGoodsSold)}. Gross profit ${formatCurrency(profit.grossProfit ?? 0)}.`
                                        : " Gross profit is not shown while cost is missing on some lines."}
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
                                        <th className="text-end">Sale</th>
                                        <th className="text-end">Cost</th>
                                        <th className="text-end">Profit</th>
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
                                        <tr key={row.invoiceNumber}>
                                            <td>{row.invoiceNumber}</td>
                                            <td>{formatDate(row.saleDate)}</td>
                                            <td className="text-end">
                                                {formatCurrency(row.saleAmount)}
                                            </td>
                                            <td className="text-end">
                                                {row.costOfGoodsSold == null
                                                    ? "incomplete"
                                                    : formatCurrency(
                                                          row.costOfGoodsSold
                                                      )}
                                            </td>
                                            <td className="text-end">
                                                {row.profit == null
                                                    ? "—"
                                                    : formatCurrency(row.profit)}
                                            </td>
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
                    </div>
                )}
            </div>
        </div>
    );
}

export default ReportsPage;
