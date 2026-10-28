import type { RecentSale } from "../../interfaces/dashboard/dashboard";

interface RecentSalesProps {
    sales: RecentSale[];
}

function RecentSales({
    sales,
}: RecentSalesProps) {
    return (
        <div className="dashboard-widget">

            <div className="widget-header">
                <h5>Recent Sales</h5>
            </div>

            <div className="table-responsive">

                <table className="table table-hover align-middle mb-0">

                    <thead>
                        <tr>
                            <th>Invoice</th>
                            <th>Customer</th>
                            <th>Amount</th>
                            <th>Date</th>
                        </tr>
                    </thead>

                    <tbody>

                        {sales.length === 0 ? (
                            <tr>
                                <td
                                    colSpan={4}
                                    className="text-center text-muted py-4"
                                >
                                    No Sales Available
                                </td>
                            </tr>
                        ) : (
                            sales.map((sale) => (
                                <tr key={sale.saleId}>

                                    <td>
                                        {sale.invoiceNumber}
                                    </td>

                                    <td>
                                        {sale.customerName}
                                    </td>

                                    <td>
                                        ₹
                                        {sale.totalAmount.toFixed(
                                            2
                                        )}
                                    </td>

                                    <td>
                                        {new Date(
                                            sale.saleDate
                                        ).toLocaleDateString(
                                            "en-IN"
                                        )}
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

export default RecentSales;