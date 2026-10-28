import { useCallback, useEffect, useState } from "react";
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

import type {
    DashboardSummary,
    RecentSale,
    RecentPurchase,
    LowStockProduct,
    TopProduct,
} from "../../interfaces/dashboard/dashboard";

import "../../styles/dashboard.css";

function DashboardPage() {
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

    const [loading, setLoading] = useState(true);
    const [refreshing, setRefreshing] = useState(false);
    const [loadFailed, setLoadFailed] = useState(false);
    const [refreshToken, setRefreshToken] = useState(0);

    const loadDashboard = useCallback(async () => {
        try {
            setRefreshing(true);

            const [
                summaryData,
                recentSalesData,
                recentPurchasesData,
                lowStockData,
                topProductsData,
            ] = await Promise.all([
                getDashboardSummary(),
                getRecentSales(),
                getRecentPurchases(),
                getLowStockProducts(),
                getTopProducts(),
            ]);

            setSummary(summaryData);
            setRecentSales(recentSalesData);
            setRecentPurchases(recentPurchasesData);
            setLowStock(lowStockData);
            setTopProducts(topProductsData);
            setLoadFailed(false);
            setRefreshToken((token) => token + 1);
        } catch {
            setLoadFailed(true);
            toast.error(
                "Unable to load dashboard data."
            );
        } finally {
            setLoading(false);
            setRefreshing(false);
        }
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
                    value={`₹${(
                        summary?.todaySales ?? 0
                    ).toFixed(2)}`}
                    icon="bi-currency-rupee"
                    color="#2563eb"
                />

                <StatCard
                    title="Today's Purchase"
                    value={`₹${(
                        summary?.todayPurchase ?? 0
                    ).toFixed(2)}`}
                    icon="bi-cart-check"
                    color="#16a34a"
                />

                <StatCard
                    title="Products"
                    value={String(
                        summary?.totalProducts ?? 0
                    )}
                    icon="bi-box-seam"
                    color="#ea580c"
                />

                <StatCard
                    title={
                        summary?.grossProfitComplete
                            ? "Gross profit (this month)"
                            : "Gross profit (incomplete)"
                    }
                    value={
                        summary?.grossProfitComplete &&
                        summary.grossProfit !== null
                            ? `₹${summary.grossProfit.toFixed(2)}`
                            : "Unavailable"
                    }
                    icon="bi-graph-up-arrow"
                    color="#9333ea"
                />

            </div>

            <div className="row mt-4">

                <div className="col-lg-8 mb-4">
                    <SalesPurchaseChart
                        refreshToken={refreshToken}
                    />
                </div>

                <div className="col-lg-4 mb-4">
                    <LowStock
                        products={lowStock}
                    />
                </div>

            </div>

            <div className="row">

                <div className="col-lg-6 mb-4">
                    <RecentSales
                        sales={recentSales}
                    />
                </div>

                <div className="col-lg-6 mb-4">
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

                                    {topProducts.length === 0 ? (
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

                            <div>
                                <span>Total Sales</span>
                                <strong>
                                    ₹
                                    {(
                                        summary?.totalSales ??
                                        0
                                    ).toFixed(2)}
                                </strong>
                            </div>

                            <div>
                                <span>Total Purchase</span>
                                <strong>
                                    ₹
                                    {(
                                        summary?.totalPurchase ??
                                        0
                                    ).toFixed(2)}
                                </strong>
                            </div>

                            <div>
                                <span>Customers</span>
                                <strong>
                                    {summary?.totalCustomers ??
                                        0}
                                </strong>
                            </div>

                            <div>
                                <span>Suppliers</span>
                                <strong>
                                    {summary?.totalSuppliers ??
                                        0}
                                </strong>
                            </div>

                            <div>
                                <span>Low Stock</span>
                                <strong>
                                    {summary?.lowStockProducts ??
                                        0}
                                </strong>
                            </div>

                            <div>
                                <span>Out of Stock</span>
                                <strong>
                                    {summary?.outOfStockProducts ??
                                        0}
                                </strong>
                            </div>

                        </div>

                    </div>
                </div>

            </div>
        </>
    );
}

export default DashboardPage;