import { useCallback, useEffect, useState } from "react";
import { Link } from "react-router-dom";
import { toast } from "react-toastify";

import StatCard from "../../components/dashboard/StatCard";
import RecentSales from "../../components/dashboard/RecentSales";
import RecentPurchases from "../../components/dashboard/RecentPurchases";
import LowStock from "../../components/dashboard/LowStock";
import SalesPurchaseChart from "../../components/dashboard/SalesPurchaseChart";

import {
    getDashboardSummary,
    getRecentSales,
    getRecentPurchases,
    getLowStockProducts,
    getTopProducts,
} from "../../services/dashboard/dashboardService";
import { getExpiringWarranties } from "../../services/warranty/warrantyService";
import { getApiErrorMessage } from "../../utils/apiError";

import type {
    DashboardSummary,
    RecentSale,
    RecentPurchase,
    LowStockProduct,
    TopProduct,
} from "../../interfaces/dashboard/dashboard";
import type { WarrantyListItem } from "../../interfaces/warranty/warranty";
import { isAdmin } from "../../utils/session";

import "../../styles/dashboard.css";

function DashboardPage() {
    const admin = isAdmin();
    const [summary, setSummary] =
        useState<DashboardSummary | null>(null);

    const [recentSales, setRecentSales] =
        useState<RecentSale[]>([]);

    const [lowStock, setLowStock] =
        useState<LowStockProduct[]>([]);

    const [recentPurchases, setRecentPurchases] =
        useState<RecentPurchase[]>([]);

    const [topProducts, setTopProducts] =
        useState<TopProduct[]>([]);
    const [expiring, setExpiring] = useState<WarrantyListItem[]>([]);
    const [warrantyError, setWarrantyError] = useState<string | null>(null);
    const [recentSalesError, setRecentSalesError] = useState<string | null>(
        null
    );
    const [recentPurchasesError, setRecentPurchasesError] = useState<
        string | null
    >(null);
    const [lowStockError, setLowStockError] = useState<string | null>(null);
    const [topProductsError, setTopProductsError] = useState<string | null>(
        null
    );

    const [loading, setLoading] = useState(true);
    const [refreshing, setRefreshing] = useState(false);
    const [loadFailed, setLoadFailed] = useState(false);
    const [refreshToken, setRefreshToken] = useState(0);

    const loadDashboard = useCallback(async () => {
        setRefreshing(true);

        const load = async <T,>(
            fn: () => Promise<T>,
            fallback: string
        ): Promise<{ ok: true; data: T } | { ok: false; message: string }> => {
            try {
                return { ok: true, data: await fn() };
            } catch (error) {
                return {
                    ok: false,
                    message: getApiErrorMessage(error, fallback),
                };
            }
        };

        const summaryResult = await load(
            getDashboardSummary,
            "Unable to load dashboard data."
        );

        if (summaryResult.ok) {
            setSummary(summaryResult.data);
            setLoadFailed(false);
        } else {
            setLoadFailed(true);
            toast.error(summaryResult.message);
        }

        const [
            recentSalesResult,
            recentPurchasesResult,
            lowStockResult,
            topProductsResult,
            expiringResult,
        ] = await Promise.all([
            load(getRecentSales, "Unable to load recent sales."),
            load(getRecentPurchases, "Unable to load recent purchases."),
            load(getLowStockProducts, "Unable to load low-stock products."),
            load(getTopProducts, "Unable to load top products."),
            load(getExpiringWarranties, "Unable to load expiring warranties."),
        ]);

        if (recentSalesResult.ok) {
            setRecentSales(recentSalesResult.data);
            setRecentSalesError(null);
        } else {
            setRecentSales([]);
            setRecentSalesError(recentSalesResult.message);
        }

        if (recentPurchasesResult.ok) {
            setRecentPurchases(recentPurchasesResult.data);
            setRecentPurchasesError(null);
        } else {
            setRecentPurchases([]);
            setRecentPurchasesError(recentPurchasesResult.message);
        }

        if (lowStockResult.ok) {
            setLowStock(lowStockResult.data);
            setLowStockError(null);
        } else {
            setLowStock([]);
            setLowStockError(lowStockResult.message);
        }

        if (topProductsResult.ok) {
            setTopProducts(topProductsResult.data);
            setTopProductsError(null);
        } else {
            setTopProducts([]);
            setTopProductsError(topProductsResult.message);
        }

        if (expiringResult.ok) {
            setExpiring(expiringResult.data);
            setWarrantyError(
                summaryResult.ok &&
                    summaryResult.data.expiringWarrantiesAvailable === false
                    ? "Warranty figures are unavailable on this database."
                    : null
            );
        } else {
            setExpiring([]);
            setWarrantyError(expiringResult.message);
        }

        setRefreshToken((token) => token + 1);
        setLoading(false);
        setRefreshing(false);
    }, []);

    useEffect(() => {
        // eslint-disable-next-line react-hooks/set-state-in-effect
        void loadDashboard();
    }, [loadDashboard]);

    if (loading) {
        return (
            <div className="dashboard-loading">
                <div
                    className="spinner-border text-primary"
                    role="status"
                >
                    <span className="visually-hidden">
                        Loading...
                    </span>
                </div>

                <p>Loading dashboard...</p>
            </div>
        );
    }

    return (
        <>
            <div className="dashboard-header">
                <div>
                    <h2 className="page-title">
                        Dashboard
                    </h2>

                    <p className="dashboard-subtitle">
                        Business overview and performance
                    </p>
                </div>

                <button
                    type="button"
                    className="btn btn-outline-primary"
                    onClick={() => void loadDashboard()}
                    disabled={refreshing}
                >
                    <i
                        className={`bi bi-arrow-clockwise me-2${
                            refreshing ? " spin-icon" : ""
                        }`}
                    ></i>
                    {refreshing ? "Refreshing..." : "Refresh"}
                </button>
            </div>

            {loadFailed && (
                <div className="alert alert-danger" role="alert">
                    Dashboard data could not be loaded
                    {summary ? "; showing the last loaded data." : "."}{" "}
                    Use Refresh to try again.
                </div>
            )}

            <div className="row">

                <StatCard
                    title="Today's Sales"
                    value={
                        summary
                            ? `₹${summary.todaySales.toFixed(2)}`
                            : "—"
                    }
                    icon="bi-currency-rupee"
                    color="#2563eb"
                    to="/sales"
                />

                <StatCard
                    title="Today's Purchase"
                    value={
                        summary
                            ? `₹${summary.todayPurchase.toFixed(2)}`
                            : "—"
                    }
                    icon="bi-cart-check"
                    color="#16a34a"
                    to={admin ? "/purchases" : undefined}
                />

                <StatCard
                    title="Products"
                    value={
                        summary ? String(summary.totalProducts) : "—"
                    }
                    icon="bi-box-seam"
                    color="#ea580c"
                    to="/products"
                />

                <StatCard
                    title={
                        !summary
                            ? "Gross profit (this month)"
                            : summary.grossProfitComplete
                              ? "Gross profit (this month)"
                              : summary.grossProfit !== null
                                ? "Known gross profit (this month)"
                                : "Gross profit (this month)"
                    }
                    value={
                        !summary
                            ? "—"
                            : summary.grossProfit !== null
                              ? `₹${summary.grossProfit.toFixed(2)}`
                              : "Unavailable"
                    }
                    icon="bi-graph-up-arrow"
                    color="#9333ea"
                    to={admin ? "/reports" : undefined}
                />

                <StatCard
                    title="Expiring warranties"
                    value={
                        summary?.expiringWarrantiesAvailable === false ||
                        warrantyError
                            ? "Unavailable"
                            : String(
                                  summary?.expiringWarranties ??
                                      expiring.length
                              )
                    }
                    icon="bi-shield-exclamation"
                    color="#ca8a04"
                    to="/warranties"
                />

            </div>

            {summary?.grossProfitLimitation && (
                <div className="alert alert-warning" role="status">
                    {summary.grossProfitLimitation}
                </div>
            )}

            <div className="row mt-4">

                <div className="col-lg-8 mb-4">
                    <SalesPurchaseChart
                        refreshToken={refreshToken}
                    />
                </div>

                <div className="col-lg-4 mb-4">
                    {lowStockError && (
                        <div className="alert alert-warning" role="status">
                            {lowStockError}
                        </div>
                    )}
                    <LowStock
                        products={lowStock}
                    />
                    {warrantyError && (
                        <div className="alert alert-warning mt-4" role="status">
                            {warrantyError}
                        </div>
                    )}
                    {expiring.length > 0 && (
                        <div className="dashboard-widget mt-4">
                            <div className="widget-header">
                                <h5>Warranties expiring soon</h5>
                            </div>
                            <ul className="list-group list-group-flush">
                                {expiring.map((item) => (
                                    <li
                                        key={item.id}
                                        className="list-group-item d-flex justify-content-between"
                                    >
                                        <Link to={`/warranties/${item.id}`}>
                                            {item.warrantyNumber}
                                        </Link>
                                        <span className="small text-muted">
                                            {item.daysRemaining} days
                                        </span>
                                    </li>
                                ))}
                            </ul>
                        </div>
                    )}
                </div>

            </div>

            <div className="row">

                <div className="col-lg-6 mb-4">
                    {recentSalesError && (
                        <div className="alert alert-warning" role="status">
                            {recentSalesError}
                        </div>
                    )}
                    <RecentSales
                        sales={recentSales}
                    />
                </div>

                <div className="col-lg-6 mb-4">
                    {recentPurchasesError && (
                        <div className="alert alert-warning" role="status">
                            {recentPurchasesError}
                        </div>
                    )}
                    <RecentPurchases
                        purchases={recentPurchases}
                    />
                </div>

            </div>

            <div className="row">

                <div className="col-lg-6 mb-4">
                    <div className="dashboard-widget">

                        <div className="widget-header">
                            <h5>Top Selling Products</h5>
                        </div>

                        <div className="table-responsive">

                            <table className="table table-hover align-middle mb-0">

                                <thead>
                                    <tr>
                                        <th>Product</th>
                                        <th>Sold</th>
                                        <th>Amount</th>
                                    </tr>
                                </thead>

                                <tbody>

                                    {topProductsError ? (
                                        <tr>
                                            <td
                                                colSpan={3}
                                                className="text-center text-warning py-4"
                                            >
                                                {topProductsError}
                                            </td>
                                        </tr>
                                    ) : topProducts.length === 0 ? (
                                        <tr>
                                            <td
                                                colSpan={3}
                                                className="text-center text-muted py-4"
                                            >
                                                No Sales Available
                                            </td>
                                        </tr>
                                    ) : (
                                        topProducts.map(
                                            (product) => (
                                                <tr
                                                    key={
                                                        product.productId
                                                    }
                                                >
                                                    <td>
                                                        {
                                                            product.productName
                                                        }
                                                    </td>

                                                    <td>
                                                        {
                                                            product.totalSold
                                                        }
                                                    </td>

                                                    <td>
                                                        ₹
                                                        {product.salesAmount.toFixed(
                                                            2
                                                        )}
                                                    </td>
                                                </tr>
                                            )
                                        )
                                    )}

                                </tbody>

                            </table>

                        </div>

                    </div>
                </div>

                <div className="col-lg-6 mb-4">
                    <div className="dashboard-widget">

                        <div className="widget-header">
                            <h5>Business Overview</h5>
                        </div>

                        <div className="dashboard-overview">

                            <Link to="/sales">
                                <span>Total Sales</span>
                                <strong>
                                    {summary
                                        ? `₹${summary.totalSales.toFixed(2)}`
                                        : "—"}
                                </strong>
                            </Link>

                            {admin ? (
                            <Link to="/purchases">
                                <span>Total Purchase</span>
                                <strong>
                                    {summary
                                        ? `₹${summary.totalPurchase.toFixed(2)}`
                                        : "—"}
                                </strong>
                            </Link>
                            ) : (
                            <div>
                                <span>Total Purchase</span>
                                <strong>
                                    {summary
                                        ? `₹${summary.totalPurchase.toFixed(2)}`
                                        : "—"}
                                </strong>
                            </div>
                            )}

                            <Link to="/customers">
                                <span>Customers</span>
                                <strong>
                                    {summary
                                        ? summary.totalCustomers
                                        : "—"}
                                </strong>
                            </Link>

                            <Link to="/suppliers">
                                <span>Suppliers</span>
                                <strong>
                                    {summary
                                        ? summary.totalSuppliers
                                        : "—"}
                                </strong>
                            </Link>

                            <Link to="/inventory">
                                <span>Low Stock</span>
                                <strong>
                                    {summary
                                        ? summary.lowStockProducts
                                        : "—"}
                                </strong>
                            </Link>

                            <Link to="/inventory">
                                <span>Out of Stock</span>
                                <strong>
                                    {summary
                                        ? summary.outOfStockProducts
                                        : "—"}
                                </strong>
                            </Link>

                            <Link to="/sales">
                                <span>Outstanding collections</span>
                                <strong>
                                    {summary
                                        ? `₹${(summary.outstandingCollections ?? 0).toFixed(2)}`
                                        : "—"}
                                </strong>
                            </Link>

                        </div>

                    </div>
                </div>

            </div>
        </>
    );
}

export default DashboardPage;