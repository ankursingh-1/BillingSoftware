import "./../../styles/sidebar.css";

function Header() {
    return (
        <header className="top-header">

            <div className="header-left">

                <button className="menu-btn">
                    <i className="bi bi-list"></i>
                </button>

                <div className="search-box">

                    <i className="bi bi-search"></i>

                    <input
                        type="text"
                        placeholder="Search Product, Customer..."
                    />

                </div>

            </div>

            <div className="header-right">

                <button className="icon-btn">
                    <i className="bi bi-moon"></i>
                </button>

                <button className="icon-btn">
                    <i className="bi bi-bell"></i>
                </button>

                <div className="profile-box">

                    <img
                        src="https://ui-avatars.com/api/?name=Admin"
                        alt="Admin"
                    />

                    <div>

                        <h6>Administrator</h6>

                        <small>Super Admin</small>

                    </div>

                </div>

            </div>

        </header>
    );
}

export default Header;