import { Navigate } from "react-router-dom";
import type { PropsWithChildren } from "react";

function ProtectedRoute({ children }: PropsWithChildren) {

    const token = localStorage.getItem("token");

    if (!token) {
        return <Navigate to="/login" replace />;
    }

    return children;
}

export default ProtectedRoute;