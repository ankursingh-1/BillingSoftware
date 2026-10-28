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
                        element={<CompanySettingsPage />}
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