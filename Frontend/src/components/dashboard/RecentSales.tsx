function RecentSales() {
    return (
        <div className="dashboard-widget">

            <div className="widget-header">
                <h5>Recent Sales</h5>
            </div>

            <table className="table table-hover align-middle mb-0">

                <thead>

                    <tr>
                        <th>Invoice</th>
                        <th>Customer</th>
                        <th>Amount</th>
                    </tr>

                </thead>

                <tbody>

                    <tr>
                        <td colSpan={3} className="text-center text-muted py-4">
                            No Sales Available
                        </td>
                    </tr>

                </tbody>

            </table>

        </div>
    );
}

export default RecentSales;