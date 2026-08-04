import { BrowserRouter, Routes, Route } from "react-router-dom";

import LoginPage from "../pages/auth/LoginPage";
import MainLayout from "../layouts/MainLayout";
import DashboardPage from "../pages/dashboard/DashboardPage";

function AppRoutes() {
    return (
        <BrowserRouter>
            <Routes>
                <Route
                    path="/login"
                    element={<LoginPage />}
                />
                <Route element={<MainLayout />}>
                    <Route
                        path="/"
                        element={<DashboardPage />}
                    />
                </Route>
            </Routes>
        </BrowserRouter>
    );
}

export default AppRoutes;