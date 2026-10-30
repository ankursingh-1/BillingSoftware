import { useEffect } from "react";
import { NavLink, useLocation } from "react-router-dom";

import { useLayout } from "../../hooks/useLayout";
import { logout } from "../../services/auth/authService";
import { isAdmin } from "../../utils/session";

interface MenuItem {
    title: string;
    path: string;
    icon: string;
    /** Hidden from non-admin users (the API enforces it as well). */
    adminOnly?: boolean;
}

const menuItems: MenuItem[] = [
    {
        title: "Dashboard",
        path: "/dashboard",
        icon: "bi-speedometer2",
    },
    {
        title: "Products",
        path: "/products",
        icon: "bi-box-seam",
    },
    {
        title: "Categories",
        path: "/categories",
        icon: "bi-grid",
    },
    {
        title: "Brands, units & tax",
        path: "/catalog",
        icon: "bi-tags",
        adminOnly: true,
    },
    {
        title: "Inventory",
        path: "/inventory",
        icon: "bi-boxes",
    },
    {
        title: "Purchases",
        path: "/purchases",
        icon: "bi-cart-check",
        adminOnly: true,
    },
    {
        title: "Sales",
        path: "/sales",
        icon: "bi-receipt",
    },
    {
        title: "Returns",
        path: "/returns",
        icon: "bi-arrow-return-left",
    },
    {
        title: "Warranties",
        path: "/warranties",
        icon: "bi-shield-check",
    },
    {
        title: "Customers",
        path: "/customers",
        icon: "bi-people",
    },
    {
        title: "Suppliers",
        path: "/suppliers",
        icon: "bi-truck",
    },
    {
        title: "Expenses",
        path: "/expenses",
        icon: "bi-wallet2",
        adminOnly: true,
    },
    {
        title: "Reports",
        path: "/reports",
        icon: "bi-graph-up-arrow",
        adminOnly: true,
    },
    {
        title: "Settings",
        path: "/dashboard/settings",
        icon: "bi-gear",
        adminOnly: true,
    },
];

function Sidebar() {
    const admin = isAdmin();
    const location = useLocation();
    const { navOpen, closeNav } = useLayout();
    const visibleItems = menuItems.filter((item) => admin || !item.adminOnly);

    useEffect(() => {
        const menu = document.querySelector<HTMLElement>(".sidebar-menu");
        const active = menu?.querySelector<HTMLElement>(".active-menu");
        if (!menu || !active) {
            return;
        }

        const menuBox = menu.getBoundingClientRect();
        const itemBox = active.getBoundingClientRect();
        if (itemBox.top < menuBox.top || itemBox.bottom > menuBox.bottom) {
            active.scrollIntoView({ block: "nearest", inline: "nearest" });
        }
    }, [location.pathname, navOpen]);

    return (
        <aside className={`sidebar${navOpen ? " is-open" : ""}`}>

            <div className="sidebar-logo">
                <div className="logo-circle">
                    BS
                </div>

                <div>
                    <h5>Billing ERP</h5>
                    <small>Version 1.0</small>
                </div>
            </div>

            <ul className="sidebar-menu">

                {visibleItems.map((item) => (
                    <li key={item.title}>

                        <NavLink
                            to={item.path}
                            end={item.path === "/dashboard"}
                            onClick={closeNav}
                            className={({ isActive }) =>
                                isActive
                                    ? "menu-link active-menu"
                                    : "menu-link"
                            }
                        >
                            <i
                                className={`bi ${item.icon}`}
                            ></i>
                            <span>
                                {item.title}
                            </span>
                        </NavLink>
                    </li>
                ))}
            </ul>

            <div className="sidebar-footer">

                <button
                    type="button"
                    className="logout-btn"
                    onClick={logout}
                >
                    <i className="bi bi-box-arrow-right"></i>
                    <span>Logout</span>
                </button>
            </div>
        </aside>
    );
}

export default Sidebar;