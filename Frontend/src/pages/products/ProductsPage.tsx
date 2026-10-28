import { useEffect, useState } from "react";
import { Link } from "react-router-dom";
import { toast } from "react-toastify";

import ConfirmDialog from "../../components/common/ConfirmDialog";
import Pagination from "../../components/common/Pagination";

import { LOW_STOCK_THRESHOLD } from "../../constants/inventory";
import { useDebounce } from "../../hooks/useDebounce";
import {
    deleteProduct,
    searchProducts,
} from "../../services/products/productService";
import { getApiErrorMessage } from "../../utils/apiError";
import { formatCurrency } from "../../utils/format";

import type { PagedResult } from "../../interfaces/common/pagedResult";
import type { Product } from "../../interfaces/product/product";

import "../../styles/data-pages.css";

const PAGE_SIZE_OPTIONS = [10, 25, 50];

function StockBadge({ stock }: { stock: number }) {
    if (stock <= 0) {
        return <span className="badge text-bg-danger">Out of stock</span>;
    }

    if (stock <= LOW_STOCK_THRESHOLD) {
        return <span className="badge text-bg-warning">{stock}</span>;
    }

    return <span className="badge text-bg-success">{stock}</span>;
}

function ProductsPage() {
    const [search, setSearch] = useState("");
    const debouncedSearch = useDebounce(search.trim(), 400);

    // The page belongs to the search term it was chosen for, so a new
    // search always starts on page 1 without an extra render/request.
    const [pageState, setPageState] = useState({ page: 1, forSearch: "" });
    const page =
        pageState.forSearch === debouncedSearch ? pageState.page : 1;

    const [pageSize, setPageSize] = useState(PAGE_SIZE_OPTIONS[0]);
    const [reloadKey, setReloadKey] = useState(0);

    const [result, setResult] =
        useState<PagedResult<Product> | null>(null);
    const [loading, setLoading] = useState(true);
    const [error, setError] = useState<string | null>(null);

    const [deleteTarget, setDeleteTarget] = useState<Product | null>(null);
    const [deleting, setDeleting] = useState(false);

    useEffect(() => {
        let cancelled = false;

        const load = async () => {
            setLoading(true);
            setError(null);

            try {
                const data = await searchProducts({
                    search: debouncedSearch || undefined,
                    pageNumber: page,
                    pageSize,
                });

                if (cancelled) {
                    return;
                }

                // Page no longer exists (e.g. last item on it was deleted).
                if (data.data.length === 0 && page > 1 && data.totalPages > 0) {
                    setPageState({
                        page: data.totalPages,
                        forSearch: debouncedSearch,
                    });
                    return;
                }

                setResult(data);
            } catch (err) {
                if (!cancelled) {
                    setError(
                        getApiErrorMessage(
                            err,
                            "Unable to load products."
                        )
                    );
                }
            } finally {
                if (!cancelled) {
                    setLoading(false);
                }
            }
        };

        void load();

        return () => {
            cancelled = true;
        };
    }, [debouncedSearch, page, pageSize, reloadKey]);

    const handleConfirmDelete = async () => {
        if (!deleteTarget) {
            return;
        }

        try {
            setDeleting(true);
            await deleteProduct(deleteTarget.id);
            toast.success(`"${deleteTarget.name}" deleted.`);
            setDeleteTarget(null);
            setReloadKey((key) => key + 1);
        } catch (err) {
            toast.error(
                getApiErrorMessage(err, "Unable to delete the product.")
            );
        } finally {
            setDeleting(false);
        }
    };

    const products = result?.data ?? [];
    const isFiltering = debouncedSearch.length > 0;

    return (
        <div className="data-page">
            <div className="data-page-header">
                <div>
                    <h2 className="page-title">Products</h2>
                    <p className="data-page-subtitle">
                        Manage your products, prices and stock levels
                    </p>
                </div>

                <div className="data-page-actions">
                    <Link to="/categories" className="btn btn-outline-secondary">
                        <i className="bi bi-grid me-2"></i>
                        Categories
                    </Link>

                    <Link to="/products/new" className="btn btn-primary">
                        <i className="bi bi-plus-lg me-2"></i>
                        Add Product
                    </Link>
                </div>
            </div>

            <div className="data-card">
                <div className="data-toolbar">
                    <div className="input-group data-search">
                        <span className="input-group-text">
                            <i className="bi bi-search"></i>
                        </span>

                        <input
                            type="search"
                            className="form-control"
                            placeholder="Search by name or SKU"
                            aria-label="Search products by name or SKU"
                            value={search}
                            onChange={(e) => setSearch(e.target.value)}
                        />
                    </div>

                    <div className="data-toolbar-right">
                        <label
                            htmlFor="product-page-size"
                            className="visually-hidden"
                        >
                            Rows per page
                        </label>

                        <select
                            id="product-page-size"
                            className="form-select form-select-sm"
                            value={pageSize}
                            onChange={(e) => {
                                setPageSize(Number(e.target.value));
                                setPageState({
                                    page: 1,
                                    forSearch: debouncedSearch,
                                });
                            }}
                        >
                            {PAGE_SIZE_OPTIONS.map((size) => (
                                <option key={size} value={size}>
                                    {size} / page
                                </option>
                            ))}
                        </select>
                    </div>
                </div>

                {error && !result ? (
                    <div className="state-block" role="alert">
                        <i className="bi bi-exclamation-octagon state-icon text-danger"></i>
                        <p>{error}</p>
                        <button
                            type="button"
                            className="btn btn-outline-primary btn-sm"
                            onClick={() => setReloadKey((key) => key + 1)}
                        >
                            <i className="bi bi-arrow-clockwise me-1"></i>
                            Retry
                        </button>
                    </div>
                ) : loading && !result ? (
                    <div className="state-block">
                        <div
                            className="spinner-border text-primary"
                            role="status"
                        >
                            <span className="visually-hidden">
                                Loading products...
                            </span>
                        </div>
                        <p>Loading products...</p>
                    </div>
                ) : (
                    <>
                        {error && (
                            <div
                                className="alert alert-danger m-3 mb-0"
                                role="alert"
                            >
                                {error}{" "}
                                <button
                                    type="button"
                                    className="btn btn-link p-0 align-baseline"
                                    onClick={() =>
                                        setReloadKey((key) => key + 1)
                                    }
                                >
                                    Retry
                                </button>
                            </div>
                        )}

                        {products.length === 0 ? (
                            <div className="state-block">
                                <i className="bi bi-box-seam state-icon"></i>
                                <p>
                                    {isFiltering
                                        ? `No products match "${debouncedSearch}".`
                                        : "No products yet. Add your first product to get started."}
                                </p>
                                {!isFiltering && (
                                    <Link
                                        to="/products/new"
                                        className="btn btn-primary btn-sm"
                                    >
                                        <i className="bi bi-plus-lg me-1"></i>
                                        Add Product
                                    </Link>
                                )}
                            </div>
                        ) : (
                            <div
                                className={`table-wrapper${
                                    loading ? " is-loading" : ""
                                }`}
                                aria-busy={loading}
                            >
                                <div className="table-responsive">
                                    <table className="table table-hover align-middle mb-0">
                                        <thead>
                                            <tr>
                                                <th>Product</th>
                                                <th>Category</th>
                                                <th className="d-none d-lg-table-cell">
                                                    Brand
                                                </th>
                                                <th className="text-end">
                                                    Purchase
                                                </th>
                                                <th className="text-end">
                                                    Selling
                                                </th>
                                                <th className="d-none d-xl-table-cell">
                                                    Tax
                                                </th>
                                                <th>Stock</th>
                                                <th className="text-end">
                                                    Actions
                                                </th>
                                            </tr>
                                        </thead>

                                        <tbody>
                                            {products.map((product) => (
                                                <tr key={product.id}>
                                                    <td>
                                                        <div className="fw-semibold">
                                                            {product.name}
                                                        </div>
                                                        <small className="text-muted">
                                                            SKU: {product.sku}
                                                        </small>
                                                    </td>

                                                    <td>
                                                        {product.categoryName ?? (
                                                            <span className="text-muted">
                                                                —
                                                            </span>
                                                        )}
                                                    </td>

                                                    <td className="d-none d-lg-table-cell">
                                                        {product.brandName ?? (
                                                            <span className="text-muted">
                                                                —
                                                            </span>
                                                        )}
                                                    </td>

                                                    <td className="text-end">
                                                        {formatCurrency(
                                                            product.purchasePrice
                                                        )}
                                                    </td>

                                                    <td className="text-end">
                                                        {formatCurrency(
                                                            product.sellingPrice
                                                        )}
                                                    </td>

                                                    <td className="d-none d-xl-table-cell">
                                                        {product.taxName ? (
                                                            `${product.taxName}${
                                                                product.taxPercentage !==
                                                                null
                                                                    ? ` (${product.taxPercentage}%)`
                                                                    : ""
                                                            }`
                                                        ) : (
                                                            <span className="text-muted">
                                                                —
                                                            </span>
                                                        )}
                                                    </td>

                                                    <td>
                                                        <StockBadge
                                                            stock={product.stock}
                                                        />{" "}
                                                        {product.unitName && product.stock > 0 && (
                                                            <small className="text-muted">
                                                                {product.unitName}
                                                            </small>
                                                        )}
                                                    </td>

                                                    <td className="text-end text-nowrap">
                                                        <Link
                                                            to={`/products/${product.id}/edit`}
                                                            className="btn btn-sm btn-outline-primary me-2"
                                                            aria-label={`Edit ${product.name}`}
                                                        >
                                                            <i className="bi bi-pencil"></i>
                                                        </Link>

                                                        <button
                                                            type="button"
                                                            className="btn btn-sm btn-outline-danger"
                                                            aria-label={`Delete ${product.name}`}
                                                            onClick={() =>
                                                                setDeleteTarget(
                                                                    product
                                                                )
                                                            }
                                                        >
                                                            <i className="bi bi-trash"></i>
                                                        </button>
                                                    </td>
                                                </tr>
                                            ))}
                                        </tbody>
                                    </table>
                                </div>

                                <Pagination
                                    page={result?.pageNumber ?? page}
                                    totalPages={result?.totalPages ?? 1}
                                    totalRecords={result?.totalRecords ?? 0}
                                    pageSize={result?.pageSize ?? pageSize}
                                    disabled={loading}
                                    onPageChange={(nextPage) =>
                                        setPageState({
                                            page: nextPage,
                                            forSearch: debouncedSearch,
                                        })
                                    }
                                />
                            </div>
                        )}
                    </>
                )}
            </div>

            <ConfirmDialog
                show={deleteTarget !== null}
                title="Delete product"
                message={
                    <>
                        <p className="mb-2">
                            Delete{" "}
                            <strong>{deleteTarget?.name}</strong>?
                        </p>
                        <p className="mb-0 text-muted">
                            It will be removed from the product list and
                            can no longer be sold or purchased. Existing
                            sales, purchases and stock history are kept.
                        </p>
                    </>
                }
                busy={deleting}
                onConfirm={() => void handleConfirmDelete()}
                onCancel={() => setDeleteTarget(null)}
            />
        </div>
    );
}

export default ProductsPage;
