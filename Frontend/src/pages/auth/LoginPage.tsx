import { useState } from "react";
import type { FormEvent } from "react";
import { Navigate, useNavigate } from "react-router-dom";
import { toast } from "react-toastify";
import axios from "axios";

import { login } from "../../services/auth/authService";
import { isStoredTokenExpired } from "../../utils/session";

function LoginPage() {
    const navigate = useNavigate();
    const existing = localStorage.getItem("token");

    if (existing && isStoredTokenExpired()) {
        localStorage.removeItem("token");
    }

    const [email, setEmail] = useState("");
    const [password, setPassword] = useState("");
    const [loading, setLoading] = useState(false);

    if (existing && !isStoredTokenExpired()) {
        return <Navigate to="/dashboard" replace />;
    }

    const handleLogin = async (event: FormEvent) => {
        event.preventDefault();
        try {
            setLoading(true);

            const response = await login({
                email,
                password,
            });

            if (response.success) {
                localStorage.setItem("token", response.token);

                navigate("/dashboard", { replace: true });
            } else {
                toast.error(response.message ?? "Invalid Email or Password");
            }
        } catch (error: unknown) {
            if (axios.isAxiosError(error)) {
                if (error.response?.status === 401) {
                    toast.error("Invalid Email or Password");
                } else {
                    toast.error("Unable to connect to server");
                }
            } else {
                toast.error("Unexpected error occurred");
            }
        } finally {
            setLoading(false);
        }
    };

    return (
        <div className="container-fluid vh-100">
            <div className="row h-100">

                <div className="col-lg-7 d-none d-lg-flex bg-primary justify-content-center align-items-center">

                    <div className="text-center text-white">

                        <h1 className="display-3 fw-bold">
                            Billing ERP
                        </h1>

                        <p className="fs-4">
                            Inventory & Billing Software
                        </p>

                    </div>

                </div>

                <div className="col-lg-5 d-flex justify-content-center align-items-center">

                    <form
                        className="card shadow-lg border-0 p-5"
                        style={{
                            width: "430px",
                            borderRadius: "18px"
                        }}
                        onSubmit={(event) => void handleLogin(event)}
                    >

                        <h2 className="fw-bold mb-4">
                            Login
                        </h2>

                        <div className="mb-3">

                            <label className="form-label">
                                Email
                            </label>

                            <input
                                type="email"
                                autoComplete="email"
                                className="form-control"
                                placeholder="Enter Email"
                                value={email}
                                onChange={(e) => setEmail(e.target.value)}
                            />

                        </div>

                        <div className="mb-4">

                            <label className="form-label">
                                Password
                            </label>

                            <input
                                type="password"
                                autoComplete="current-password"
                                className="form-control"
                                placeholder="Enter Password"
                                value={password}
                                onChange={(e) => setPassword(e.target.value)}
                            />

                        </div>

                        <button
                            type="submit"
                            className="btn btn-primary w-100"
                            disabled={
                                loading ||
                                email.trim() === "" ||
                                password.trim() === ""
                            }
                        >
                            {loading ? "Signing In..." : "Login"}
                        </button>

                    </form>

                </div>

            </div>
        </div>
    );
}

export default LoginPage;