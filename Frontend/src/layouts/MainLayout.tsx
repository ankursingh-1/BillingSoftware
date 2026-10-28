import { Outlet } from "react-router-dom";

import Header from "../components/layout/Header";
import Sidebar from "../components/layout/Sidebar";
import { LayoutProvider } from "../context/LayoutContext";
import { useLayout } from "../hooks/useLayout";

function Shell() {
    const { navOpen, closeNav } = useLayout();

    return (
        <div className="app-shell">
            {navOpen && (
                <button
                    type="button"
                    className="sidebar-backdrop"
                    aria-label="Close menu"
                    onClick={closeNav}
                />
            )}

            <Sidebar />

            <div className="app-main">
                <Header />

                <main className="app-content">
                    <Outlet />
                </main>
            </div>
        </div>
    );
}

function MainLayout() {
    return (
        <LayoutProvider>
            <Shell />
        </LayoutProvider>
    );
}

export default MainLayout;
