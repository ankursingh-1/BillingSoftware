import { Link } from "react-router-dom";
import type { PropsWithChildren } from "react";

import { isAdmin } from "../utils/session";

/**
 * Shows a "no access" message to non-admin users. This is a convenience:
 * the API enforces the same rule on every admin endpoint.
 */
function AdminRoute({ children }: PropsWithChildren) {
    if (isAdmin()) {
        return children;
    }

    return (
        <div className="data-page">
            <div className="data-card">
                <div className="state-block" role="alert">
                    <i className="bi bi-shield-lock state-icon"></i>
                    <p>You do not have access to this page.</p>
                    <Link to="/dashboard" className="btn btn-primary btn-sm">
                        Back to dashboard
                    </Link>
                </div>
            </div>
        </div>
    );
}

export default AdminRoute;