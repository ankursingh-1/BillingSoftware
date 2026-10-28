import type { RecentPurchase } from "../../interfaces/dashboard/dashboard";

interface RecentPurchasesProps {
    purchases: RecentPurchase[];
}

function RecentPurchases({ purchases }: RecentPurchasesProps) {
    return (
        <div className="dashboard-widget">
            <div className="widget-header">
                <h5>Recent Purchases</h5>
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
                                    <td>{purchase.purchaseNumber}</td>
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
