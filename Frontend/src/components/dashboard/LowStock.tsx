function LowStock() {
    return (
        <div className="dashboard-widget">

            <div className="widget-header">

                <h5>Low Stock Products</h5>

            </div>

            <table className="table table-hover align-middle mb-0">

                <thead>

                    <tr>

                        <th>Product</th>

                        <th>Stock</th>

                    </tr>

                </thead>

                <tbody>

                    <tr>

                        <td
                            colSpan={2}
                            className="text-center text-muted py-4"
                        >
                            No Product

                        </td>

                    </tr>

                </tbody>

            </table>

        </div>
    );
}

export default LowStock;