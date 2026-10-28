import { Link } from "react-router-dom";

import type { RecentPurchase } from "../../interfaces/dashboard/dashboard";
import { isAdmin } from "../../utils/session";

interface RecentPurchasesProps {
    purchases: RecentPurchase[];
}

function RecentPurchases({ purchases }: RecentPurchasesProps) {
    const admin = isAdmin();

    return (
        <div className="dashboard-widget">
            <div className="widget-header">
                <h5>Recent Purchases</h5>
                {admin && (
                    <Link to="/purchases" className="small">
                        View all
                    </Link>
                )}
            </div>

            <div className="table-responsive">
                <table className="table table-hover align-middle mb-0">
                    <thead>
                        <tr>
                            <th>Purchase No</th>
                            <th>Supplier</th>
                            <th>Amount</th>
                            <th>Date</th>
                        </tr>
                    </thead>

                    <tbody>
                        {purchases.length === 0 ? (
                            <tr>
                                <td
                                    colSpan={4}
                                    className="text-center text-muted py-4"
                                >
                                    No Purchases Available
                                </td>
                            </tr>
                        ) : (
                            purchases.map((purchase) => (
                                <tr key={purchase.purchaseId}>
                                    <td>
                                        {admin ? (
                                            <Link
                                                to={`/purchases/${purchase.purchaseId}`}
                                            >
                                                {purchase.purchaseNumber}
                                            </Link>
                                        ) : (
                                            purchase.purchaseNumber
                                        )}
                                    </td>
                                    <td>{purchase.supplierName}</td>
                                    <td>
                                        ₹{purchase.totalAmount.toFixed(2)}
                                    </td>
                                    <td>
                                        {new Date(
                                            purchase.purchaseDate
                                        ).toLocaleDateString("en-IN")}
                                    </td>
                                </tr>
                            ))
                        )}
                    </tbody>
                </table>
            </div>
        </div>
    );
}

export default RecentPurchases;
