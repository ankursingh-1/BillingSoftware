import { Link } from "react-router-dom";

import type {
    LowStockProduct,
} from "../../interfaces/dashboard/dashboard";

interface LowStockProps {
    products: LowStockProduct[];
}

function LowStock({
    products,
}: LowStockProps) {
    return (
        <div className="dashboard-widget">

            <div className="widget-header">
                <h5>Low &amp; Out-of-Stock Products</h5>
                <Link to="/inventory" className="small">
                    Inventory
                </Link>
            </div>

            <div className="table-responsive">

                <table className="table table-hover align-middle mb-0">

                    <thead>
                        <tr>
                            <th>Product</th>
                            <th>Stock</th>
                        </tr>
                    </thead>

                    <tbody>

                        {products.length === 0 ? (
                            <tr>
                                <td
                                    colSpan={2}
                                    className="text-center text-muted py-4"
                                >
                                    No Low Stock Products
                                </td>
                            </tr>
                        ) : (
                            products.map((product) => (
                                <tr
                                    key={
                                        product.productId
                                    }
                                >
                                    <td>
                                        <Link
                                            to={`/products/${product.productId}/edit`}
                                        >
                                            {product.productName}
                                        </Link>
                                    </td>

                                    <td>
                                        <span
                                            className={
                                                product.currentStock <= 0
                                                    ? "badge text-bg-danger"
                                                    : "badge text-bg-warning"
                                            }
                                        >
                                            {product.currentStock <= 0
                                                ? "Out of stock"
                                                : product.currentStock}
                                        </span>
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

export default LowStock;