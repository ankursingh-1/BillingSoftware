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

                {/* Protected Application */}
                <Route
                    path="/dashboard"
                    element={
                        <ProtectedRoute>
                            <MainLayout />
                        </ProtectedRoute>
                    }
                >
                    {/* Dashboard */}
                    <Route
                        index
                        element={<DashboardPage />}
                    />

                    {/* Company Settings */}
                    <Route
                        path="settings"
                        element={<CompanySettingsPage />}
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