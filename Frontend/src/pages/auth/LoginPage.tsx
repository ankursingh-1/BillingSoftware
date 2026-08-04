function LoginPage() {
    return (
        <div className="container-fluid vh-100">
            <div className="row h-100">
                <div className="col-lg-7 d-none d-lg-flex bg-primary justify-content-center align-items-center">
                    <div className="text-center text-white">
                        <h1 className="display-4 fw-bold">
                            Billing Software
                        </h1>
                        <p className="lead">
                            Professional Billing & Inventory Management System
                        </p>

                    </div>
                </div>
                <div className="col-lg-5 d-flex justify-content-center align-items-center">
                    <div
                        className="shadow p-5 bg-white rounded"
                        style={{ width: "420px" }}
                    >
                        <h2 className="mb-4 text-center">
                            Login
                        </h2>
                        <div className="mb-3">
                            <label className="form-label">
                                Email
                            </label>
                            <input
                                type="email"
                                className="form-control"
                            />
                        </div>
                        <div className="mb-4">
                            <label className="form-label">
                                Password
                            </label>
                            <input
                                type="password"
                                className="form-control"
                            />
                        </div>
                        <button
                            className="btn btn-primary w-100"
                        >
                            Login
                        </button>
                    </div>
                </div>
            </div>
        </div>
    );
}

export default LoginPage;