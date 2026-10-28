import { useEffect, useMemo, useState } from "react";
import { Link } from "react-router-dom";
import { toast } from "react-toastify";

import ConfirmDialog from "../../components/common/ConfirmDialog";
import Pagination from "../../components/common/Pagination";
import CategoryFormModal from "../../components/forms/CategoryFormModal";

import { useDebounce } from "../../hooks/useDebounce";
import {
    deleteCategory,
    getCategories,
} from "../../services/categories/categoryService";
import { getApiErrorMessage } from "../../utils/apiError";

import type { Category } from "../../interfaces/category/category";

import "../../styles/data-pages.css";

const PAGE_SIZE = 10;

// null = closed, "new" = create, Category = edit
type FormState = null | "new" | Category;

function CategoriesPage() {
    const [categories, setCategories] = useState<Category[] | null>(null);
    const [loading, setLoading] = useState(true);
    const [error, setError] = useState<string | null>(null);
    const [reloadKey, setReloadKey] = useState(0);

    const [search, setSearch] = useState("");
    const debouncedSearch = useDebounce(search.trim().toLowerCase(), 250);
    const [pageState, setPageState] = useState({ page: 1, forSearch: "" });

    const [formState, setFormState] = useState<FormState>(null);
    const [deleteTarget, setDeleteTarget] = useState<Category | null>(null);
    const [deleting, setDeleting] = useState(false);

    useEffect(() => {
        let cancelled = false;

        const load = async () => {
            setLoading(true);
            setError(null);

            try {
                const data = await getCategories();

                if (!cancelled) {
                    setCategories(data);
                }
            } catch (err) {
                if (!cancelled) {
                    setError(
                        getApiErrorMessage(
                            err,
                            "Unable to load categories."
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
    }, [reloadKey]);

    // The API returns the full (small) category list, so search and
    // pagination are done in the browser.
    const filtered = useMemo(() => {
        if (!categories) {
            return [];
        }

        if (!debouncedSearch) {
            return categories;
        }

        return categories.filter(
            (category) =>
                category.name.toLowerCase().includes(debouncedSearch) ||
                (category.description ?? "")
                    .toLowerCase()
                    .includes(debouncedSearch)
        );
    }, [categories, debouncedSearch]);

    const totalPages = Math.max(1, Math.ceil(filtered.length / PAGE_SIZE));
    const requestedPage =
        pageState.forSearch === debouncedSearch ? pageState.page : 1;
    const page = Math.min(requestedPage, totalPages);

    const pageItems = filtered.slice(
        (page - 1) * PAGE_SIZE,
        page * PAGE_SIZE
    );

    const handleSaved = () => {
        setFormState(null);
        setReloadKey((key) => key + 1);
    };

    const handleConfirmDelete = async () => {
        if (!deleteTarget) {
            return;
        }

        try {
            setDeleting(true);
            await deleteCategory(deleteTarget.id);
            toast.success(`Category "${deleteTarget.name}" deleted.`);
            setDeleteTarget(null);
            setReloadKey((key) => key + 1);
        } catch (err) {
            // e.g. 409: the category is still used by products
            toast.error(
                getApiErrorMessage(err, "Unable to delete the category.")
            );
            setDeleteTarget(null);
        } finally {
            setDeleting(false);
        }
    };

    const isFiltering = debouncedSearch.length > 0;

    return (
        <div className="data-page">
            <div className="data-page-header">
                <div>
                    <h2 className="page-title">Categories</h2>
                    <p className="data-page-subtitle">
                        Organise products into categories
                    </p>
                </div>

                <div className="data-page-actions">
                    <Link to="/products" className="btn btn-outline-secondary">
                        <i className="bi bi-box-seam me-2"></i>
                        Products
                    </Link>

                    <button
                        type="button"
                        className="btn btn-primary"
                        onClick={() => setFormState("new")}
                    >
                        <i className="bi bi-plus-lg me-2"></i>
                        Add Category
                    </button>
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
                            placeholder="Search categories"
                            aria-label="Search categories"
                            value={search}
                            onChange={(e) => setSearch(e.target.value)}
                        />
                    </div>
                </div>

                {error && !categories ? (
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
                ) : loading && !categories ? (
                    <div className="state-block">
                        <div
                            className="spinner-border text-primary"
                            role="status"
                        >
                            <span className="visually-hidden">
                                Loading categories...
                            </span>
                        </div>
                        <p>Loading categories...</p>
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

                        {filtered.length === 0 ? (
                            <div className="state-block">
                                <i className="bi bi-grid state-icon"></i>
                                <p>
                                    {isFiltering
                                        ? "No categories match your search."
                                        : "No categories yet. Add one to organise your products."}
                                </p>
                                {!isFiltering && (
                                    <button
                                        type="button"
                                        className="btn btn-primary btn-sm"
                                        onClick={() => setFormState("new")}
                                    >
                                        <i className="bi bi-plus-lg me-1"></i>
                                        Add Category
                                    </button>
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
                                                <th>Name</th>
                                                <th className="d-none d-md-table-cell">
                                                    Description
                                                </th>
                                                <th>Status</th>
                                                <th className="text-end">
                                                    Actions
                                                </th>
                                            </tr>
                                        </thead>

                                        <tbody>
                                            {pageItems.map((category) => (
                                                <tr key={category.id}>
                                                    <td className="fw-semibold">
                                                        {category.name}
                                                    </td>

                                                    <td className="d-none d-md-table-cell text-muted">
                                                        {category.description || "—"}
                                                    </td>

                                                    <td>
                                                        <span
                                                            className={`badge ${
                                                                category.isActive
                                                                    ? "text-bg-success"
                                                                    : "text-bg-secondary"
                                                            }`}
                                                        >
                                                            {category.isActive
                                                                ? "Active"
                                                                : "Inactive"}
                                                        </span>
                                                    </td>

                                                    <td className="text-end text-nowrap">
                                                        <button
                                                            type="button"
                                                            className="btn btn-sm btn-outline-primary me-2"
                                                            aria-label={`Edit ${category.name}`}
                                                            onClick={() =>
                                                                setFormState(
                                                                    category
                                                                )
                                                            }
                                                        >
                                                            <i className="bi bi-pencil"></i>
                                                        </button>

                                                        <button
                                                            type="button"
                                                            className="btn btn-sm btn-outline-danger"
                                                            aria-label={`Delete ${category.name}`}
                                                            onClick={() =>
                                                                setDeleteTarget(
                                                                    category
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
                                    page={page}
                                    totalPages={totalPages}
                                    totalRecords={filtered.length}
                                    pageSize={PAGE_SIZE}
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

            {formState !== null && (
                <CategoryFormModal
                    key={formState === "new" ? "new" : formState.id}
                    category={formState === "new" ? null : formState}
                    onClose={() => setFormState(null)}
                    onSaved={handleSaved}
                />
            )}

            <ConfirmDialog
                show={deleteTarget !== null}
                title="Delete category"
                message={
                    <>
                        <p className="mb-2">
                            Delete category{" "}
                            <strong>{deleteTarget?.name}</strong>?
                        </p>
                        <p className="mb-0 text-muted">
                            A category that is still used by products
                            cannot be deleted. You can mark it inactive
                            instead.
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

export default CategoriesPage;
