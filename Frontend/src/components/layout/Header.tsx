import { useState } from "react";

import ChangePasswordModal from "../account/ChangePasswordModal";
import { useLayout } from "../../hooks/useLayout";
import { getCurrentEmail, getRoleLabel } from "../../utils/session";

import "./../../styles/sidebar.css";

function Header() {
    const { toggleNav } = useLayout();
    const [changingPassword, setChangingPassword] = useState(false);
    const email = getCurrentEmail();
    const role = getRoleLabel();

    return (
        <header className="top-header">
            <div className="header-left">
                <button
                    type="button"
                    className="menu-btn"
                    aria-label="Open menu"
                    onClick={toggleNav}
                >
                    <i className="bi bi-list"></i>
                </button>
            </div>

            <div className="header-right">
                <div className="profile-box">
                    <div>
                        <h6>{email ?? "Signed in"}</h6>
                        <small>{role}</small>
                    </div>
                    <button
                        type="button"
                        className="btn btn-outline-secondary btn-sm"
                        onClick={() => setChangingPassword(true)}
                    >
                        Change password
                    </button>
                </div>
            </div>

            {changingPassword && (
                <ChangePasswordModal
                    onClose={() => setChangingPassword(false)}
                />
            )}
        </header>
    );
}

export default Header;
