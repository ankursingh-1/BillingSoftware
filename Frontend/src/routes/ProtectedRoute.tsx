import { Navigate } from "react-router-dom";
import type { PropsWithChildren } from "react";

function isTokenExpired(token: string): boolean {
    try {
        const payload = token.split(".")[1];

        if (!payload) {
            return true;
        }

        const decodedPayload = JSON.parse(
            atob(
                payload
                    .replace(/-/g, "+")
                    .replace(/_/g, "/")
            )
        );

        if (!decodedPayload.exp) {
            return true;
        }
        const currentTime = Math.floor(
            Date.now() / 1000
        );
        return decodedPayload.exp <= currentTime;
    } catch {
        return true;
    }
}

function ProtectedRoute({
    children,
}: PropsWithChildren) {

    const token = localStorage.getItem("token");

    if (!token) {
        return (
            <Navigate
                to="/login"
                replace
            />
        );
    }

    if (isTokenExpired(token)) {
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