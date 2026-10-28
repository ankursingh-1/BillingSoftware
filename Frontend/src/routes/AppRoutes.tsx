import {
    BrowserRouter,
    Navigate,
    Route,
    Routes,
} from "react-router-dom";

import MainLayout from "../layouts/MainLayout";
import LoginPage from "../pages/auth/LoginPage";
import DashboardPage from "../pages/dashboard/DashboardPage";
import CompanySettingsPage from "../pages/settings/CompanySettingsPage";
import ProductsPage from "../pages/products/ProductsPage";
import ProductFormPage from "../pages/products/ProductFormPage";
import CategoriesPage from "../pages/categories/CategoriesPage";
import CustomersPage from "../pages/customers/CustomersPage";
import SuppliersPage from "../pages/suppliers/SuppliersPage";
import SalesPage from "../pages/sales/SalesPage";
import NewSalePage from "../pages/sales/NewSalePage";
import SaleDetailPage from "../pages/sales/SaleDetailPage";
import PaymentReceiptPage from "../pages/sales/PaymentReceiptPage";
import PurchasesPage from "../pages/purchases/PurchasesPage";
import NewPurchasePage from "../pages/purchases/NewPurchasePage";
import PurchaseDetailPage from "../pages/purchases/PurchaseDetailPage";
import InventoryPage from "../pages/inventory/InventoryPage";
import ReturnsPage from "../pages/returns/ReturnsPage";
import ReturnDetailPage from "../pages/returns/ReturnDetailPage";
import ExpensesPage from "../pages/expenses/ExpensesPage";
import ReportsPage from "../pages/reports/ReportsPage";
import CatalogPage from "../pages/catalog/CatalogPage";

import AdminRoute from "./AdminRoute";
import ProtectedRoute from "./ProtectedRoute";

function HomeRedirect() {
    const token = localStorage.getItem("token");

    return token ? (
        <Navigate to="/dashboard" replace />
    ) : (
        <Navigate to="/login" replace />
    );
}

export default function AppRoutes() {
    return (
        <BrowserRouter>
            <Routes>

                {/* Root */}
                <Route
                    path="/"
                    element={<HomeRedirect />}
                />

                {/* Login */}
                <Route
                    path="/login"
                    element={<LoginPage />}
                />

                {/* Protected Application (shared layout, no path of its own) */}
                <Route
                    element={
                        <ProtectedRoute>
                            <MainLayout />
                        </ProtectedRoute>
                    }
                >
                    {/* Dashboard */}
                    <Route
                        path="/dashboard"
                        element={<DashboardPage />}
                    />

                    {/* Company Settings */}
                    <Route
                        path="/dashboard/settings"
                        element={
                            <AdminRoute>
                                <CompanySettingsPage />
                            </AdminRoute>
                        }
                    />

                    {/* Products */}
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

                    {/* Categories */}
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

                    {/* Customers */}
                    <Route
                        path="/customers"
                        element={<CustomersPage />}
                    />

                    {/* Suppliers */}
                    <Route
                        path="/suppliers"
                        element={<SuppliersPage />}
                    />

                    {/* Sales */}
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

                    {/* Purchases (Admin) */}
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

                    {/* Inventory */}
                    <Route
                        path="/inventory"
                        element={<InventoryPage />}
                    />

                    {/* Returns */}
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

                {/* Unknown URL */}
                <Route
                    path="*"
                    element={<HomeRedirect />}
                />

            </Routes>
        </BrowserRouter>
    );
}