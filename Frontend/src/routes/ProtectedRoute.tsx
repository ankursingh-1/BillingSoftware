import { Navigate } from "react-router-dom";
import type { PropsWithChildren } from "react";

import { isStoredTokenExpired } from "../utils/session";

function ProtectedRoute({
    children,
}: PropsWithChildren) {

    const token = localStorage.getItem("token");

    if (!token || isStoredTokenExpired()) {
        localStorage.removeItem("token");

        return (
            <Navigate
                to="/login"
                replace
            />
        );
    }

    return children;
}

export default ProtectedRoute;