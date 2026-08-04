import "../../styles/dashboard.css";

import StatCard from "../../components/dashboard/StatCard";

function DashboardPage() {
    return (
        <>

            <div className="mb-4">

                <h2 className="page-title">
                    Dashboard
                </h2>

            </div>

            <div className="row">

                <StatCard
                    title="Today's Sales"
                    value="₹0.00"
                    icon="bi-currency-rupee"
                    color="#2563eb"
                />

                <StatCard
                    title="Today's Purchase"
                    value="₹0.00"
                    icon="bi-cart-check"
                    color="#16a34a"
                />

                <StatCard
                    title="Total Products"
                    value="0"
                    icon="bi-box-seam"
                    color="#ea580c"
                />

                <StatCard
                    title="Profit"
                    value="₹0.00"
                    icon="bi-graph-up-arrow"
                    color="#9333ea"
                />

            </div>

        </>
    );
}

export default DashboardPage;