import "../../styles/dashboard.css";

import StatCard from "../../components/dashboard/StatCard";
import RecentSales from "../../components/dashboard/RecentSales";
import LowStock from "../../components/dashboard/LowStock";

function DashboardPage() {
    return (
        <>

            <h2 className="page-title mb-4">
                Dashboard
            </h2>

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
                    title="Products"
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

            <div className="row mt-4">

                <div className="col-lg-8">

                    <RecentSales />

                </div>

                <div className="col-lg-4">

                    <LowStock />

                </div>

            </div>

        </>
    );
}

export default DashboardPage;