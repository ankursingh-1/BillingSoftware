import { NavLink } from "react-router-dom";

const menuItems = [
  {
    title: "Dashboard",
    path: "/",
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
    title: "Purchase",
    path: "/purchase",
    icon: "bi-cart-check",
  },
  {
    title: "Sales",
    path: "/sales",
    icon: "bi-receipt",
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
    title: "Reports",
    path: "/reports",
    icon: "bi-graph-up-arrow",
  },
  {
    title: "Settings",
    path: "/settings",
    icon: "bi-gear",
  },
];

function Sidebar() {
  return (
    <aside className="sidebar">

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

        {menuItems.map((item) => (

          <li key={item.title}>

            <NavLink
              to={item.path}
              className={({ isActive }) =>
                isActive
                  ? "menu-link active-menu"
                  : "menu-link"
              }
            >
              <i className={`bi ${item.icon}`}></i>

              <span>{item.title}</span>

            </NavLink>

          </li>

        ))}

      </ul>

      <div className="sidebar-footer">

        <button className="logout-btn">

          <i className="bi bi-box-arrow-right"></i>

          Logout

        </button>

      </div>

    </aside>
  );
}

export default Sidebar;