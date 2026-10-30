import { lazy, Suspense } from "react";
import type { ComponentType, LazyExoticComponent } from "react";
import {
    BrowserRouter,
    Navigate,
    Route,
    Routes,
} from "react-router-dom";

import MainLayout from "../layouts/MainLayout";
import { isStoredTokenExpired } from "../utils/session";

import AdminRoute from "./AdminRoute";
import ProtectedRoute from "./ProtectedRoute";

function HomeRedirect() {
    const token = localStorage.getItem("token");

    if (!token || isStoredTokenExpired()) {
        if (token) {
            localStorage.removeItem("token");
        }

        return <Navigate to="/login" replace />;
    }

    return <Navigate to="/dashboard" replace />;
}

function PageFallback() {
    return (
        <div className="state-block">
            <div className="spinner-border text-primary" role="status">
                <span className="visually-hidden">Loading...</span>
            </div>
        </div>
    );
}

function lazyPage<P extends object>(
    factory: () => Promise<{ default: ComponentType<P> }>
) {
    const Page: LazyExoticComponent<ComponentType<P>> = lazy(factory);

    return function LazyRoute(props: P) {
        return (
            <Suspense fallback={<PageFallback />}>
                <Page {...props} />
            </Suspense>
        );
    };
}

const LoginPage = lazyPage(() => import("../pages/auth/LoginPage"));
const DashboardPage = lazyPage(() => import("../pages/dashboard/DashboardPage"));
const CompanySettingsPage = lazyPage(() => import("../pages/settings/CompanySettingsPage"));
const ProductsPage = lazyPage(() => import("../pages/products/ProductsPage"));
const ProductFormPage = lazyPage(() => import("../pages/products/ProductFormPage"));
const CategoriesPage = lazyPage(() => import("../pages/categories/CategoriesPage"));
const CustomersPage = lazyPage(() => import("../pages/customers/CustomersPage"));
const SuppliersPage = lazyPage(() => import("../pages/suppliers/SuppliersPage"));
const SalesPage = lazyPage(() => import("../pages/sales/SalesPage"));
const NewSalePage = lazyPage(() => import("../pages/sales/NewSalePage"));
const SaleDetailPage = lazyPage(() => import("../pages/sales/SaleDetailPage"));
const PaymentReceiptPage = lazyPage(() => import("../pages/sales/PaymentReceiptPage"));
const PurchasesPage = lazyPage(() => import("../pages/purchases/PurchasesPage"));
const NewPurchasePage = lazyPage(() => import("../pages/purchases/NewPurchasePage"));
const PurchaseDetailPage = lazyPage(() => import("../pages/purchases/PurchaseDetailPage"));
const InventoryPage = lazyPage(() => import("../pages/inventory/InventoryPage"));
const ReturnsPage = lazyPage(() => import("../pages/returns/ReturnsPage"));
const ReturnDetailPage = lazyPage(() => import("../pages/returns/ReturnDetailPage"));
const ExpensesPage = lazyPage(() => import("../pages/expenses/ExpensesPage"));
const ReportsPage = lazyPage(() => import("../pages/reports/ReportsPage"));
const CatalogPage = lazyPage(() => import("../pages/catalog/CatalogPage"));
const WarrantiesPage = lazyPage(() => import("../pages/warranties/WarrantiesPage"));
const WarrantyDetailPage = lazyPage(() => import("../pages/warranties/WarrantyDetailPage"));

export default function AppRoutes() {
    return (
        <BrowserRouter>
            <Routes>

                <Route
                    path="/"
                    element={<HomeRedirect />}
                />

                <Route
                    path="/login"
                    element={<LoginPage />}
                />

                <Route
                    element={
                        <ProtectedRoute>
                            <MainLayout />
                        </ProtectedRoute>
                    }
                >
                    <Route
                        path="/dashboard"
                        element={<DashboardPage />}
                    />

                    <Route
                        path="/dashboard/settings"
                        element={
                            <AdminRoute>
                                <CompanySettingsPage />
                            </AdminRoute>
                        }
                    />

                    <Route
                        path="/products"
                        element={<ProductsPage />}
                    />
                    <Route
                        path="/products/new"
                        element={<ProductFormPage />}
                    />
                    <Route
                        path="/products/:id/edit"
                        element={<ProductFormPage />}
                    />

                    <Route
                        path="/categories"
                        element={<CategoriesPage />}
                    />
                    <Route
                        path="/catalog"
                        element={
                            <AdminRoute>
                                <CatalogPage />
                            </AdminRoute>
                        }
                    />

                    <Route
                        path="/customers"
                        element={<CustomersPage />}
                    />

                    <Route
                        path="/suppliers"
                        element={<SuppliersPage />}
                    />

                    <Route
                        path="/sales"
                        element={<SalesPage />}
                    />
                    <Route
                        path="/sales/new"
                        element={<NewSalePage />}
                    />
                    <Route
                        path="/sales/:id"
                        element={<SaleDetailPage />}
                    />
                    <Route
                        path="/sales/:id/payments/:paymentId/receipt"
                        element={<PaymentReceiptPage />}
                    />
                    <Route
                        path="/warranties"
                        element={<WarrantiesPage />}
                    />
                    <Route
                        path="/warranties/:id"
                        element={<WarrantyDetailPage />}
                    />

                    <Route
                        path="/purchases"
                        element={
                            <AdminRoute>
                                <PurchasesPage />
                            </AdminRoute>
                        }
                    />
                    <Route
                        path="/purchases/new"
                        element={
                            <AdminRoute>
                                <NewPurchasePage />
                            </AdminRoute>
                        }
                    />
                    <Route
                        path="/purchases/:id"
                        element={
                            <AdminRoute>
                                <PurchaseDetailPage />
                            </AdminRoute>
                        }
                    />

                    <Route
                        path="/inventory"
                        element={<InventoryPage />}
                    />

                    <Route
                        path="/expenses"
                        element={
                            <AdminRoute>
                                <ExpensesPage />
                            </AdminRoute>
                        }
                    />
                    <Route
                        path="/reports"
                        element={
                            <AdminRoute>
                                <ReportsPage />
                            </AdminRoute>
                        }
                    />
                    <Route
                        path="/returns"
                        element={<ReturnsPage />}
                    />
                    <Route
                        path="/returns/sales/:id"
                        element={<ReturnDetailPage kind="sales" />}
                    />
                    <Route
                        path="/returns/purchases/:id"
                        element={
                            <AdminRoute>
                                <ReturnDetailPage kind="purchases" />
                            </AdminRoute>
                        }
                    />
                </Route>

                <Route
                    path="*"
                    element={<HomeRedirect />}
                />

            </Routes>
        </BrowserRouter>
    );
}
