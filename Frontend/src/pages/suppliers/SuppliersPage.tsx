import { useState } from "react";
import { toast } from "react-toastify";

import ConfirmDialog from "../../components/common/ConfirmDialog";
import DetailsModal from "../../components/common/DetailsModal";
import Pagination from "../../components/common/Pagination";
import SupplierFormModal from "../../components/forms/SupplierFormModal";

import {
    PAGE_SIZE_OPTIONS,
    usePagedSearch,
} from "../../hooks/usePagedSearch";
import {
    deleteSupplier,
    searchSuppliers,
} from "../../services/suppliers/supplierService";
import { getApiErrorMessage } from "../../utils/apiError";

import type { Supplier } from "../../interfaces/supplier/supplier";

import "../../styles/data-pages.css";

const LOAD_ERROR = "Unable to load suppliers.";

type FormState =
    | { mode: "closed" }
    | { mode: "create" }
    | { mode: "edit"; supplier: Supplier };

function SuppliersPage() {
    const {
        search,
        setSearch,
        debouncedSearch,
        page,
        pageSize,
        result,
        loading,
        error,
        goToPage,
        changePageSize,
        reload,
    } = usePagedSearch(searchSuppliers, LOAD_ERROR);

    const [formState, setFormState] = useState<FormState>({
        mode: "closed",
    });
    const [viewTarget, setViewTarget] = useState<Supplier | null>(null);
    const [deleteTarget, setDeleteTarget] = useState<Supplier | null>(null);
    const [deleting, setDeleting] = useState(false);

    const handleConfirmDelete = async () => {
        if (!deleteTarget) {
            return;
        }

        try {
            setDeleting(true);
            await deleteSupplier(deleteTarget.id);
            toast.success(`"${deleteTarget.companyName}" deleted.`);
            setDeleteTarget(null);
            reload();
        } catch (err) {
            toast.error(
                getApiErrorMessage(err, "Unable to delete the supplier.")
            );
        } finally {
            setDeleting(false);
        }
    };

    const handleSaved = () => {
        setFormState({ mode: "closed" });
        reload();
    };

    const suppliers = result?.data ?? [];
    const isFiltering = debouncedSearch.length > 0;

    return (
        <div className="data-page">
            <div className="data-page-header">
                <div>
                    <h2 className="page-title">Suppliers</h2>
                    <p className="data-page-subtitle">
                        Manage the vendors you purchase from
                    </p>
                </div>

                <div className="data-page-actions">
                    <button
                        type="button"
                        className="btn btn-primary"
                        onClick={() => setFormState({ mode: "create" })}
                    >
                        <i className="bi bi-plus-lg me-2"></i>
                        Add Supplier
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
                            placeholder="Search by name, company or mobile"
                            aria-label="Search suppliers by name, company or mobile"
                            value={search}
                            onChange={(e) => setSearch(e.target.value)}
                        />
                    </div>

                    <div className="data-toolbar-right">
                        <label
                            htmlFor="supplier-page-size"
                            className="visually-hidden"
                        >
                            Rows per page
                        </label>

                        <select
                            id="supplier-page-size"
                            className="form-select form-select-sm"
                            value={pageSize}
                            onChange={(e) =>
                                changePageSize(Number(e.target.value))
                            }
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
                            onClick={reload}
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
                                Loading suppliers...
                            </span>
                        </div>
                        <p>Loading suppliers...</p>
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
                                    onClick={reload}
                                >
                                    Retry
                                </button>
                            </div>
                        )}

                        {suppliers.length === 0 ? (
                            <div className="state-block">
                                <i className="bi bi-truck state-icon"></i>
                                <p>
                                    {isFiltering
                                        ? `No suppliers match "${debouncedSearch}".`
                                        : "No suppliers yet. Add your first supplier to get started."}
                                </p>
                                {!isFiltering && (
                                    <button
                                        type="button"
                                        className="btn btn-primary btn-sm"
                                        onClick={() =>
                                            setFormState({ mode: "create" })
                                        }
                                    >
                                        <i className="bi bi-plus-lg me-1"></i>
                                        Add Supplier
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
                                                <th>Supplier</th>
                                                <th>Mobile</th>
                                                <th className="d-none d-md-table-cell">
                                                    Email
                                                </th>
                                                <th className="d-none d-lg-table-cell">
                                                    Address
                                                </th>
                                                <th className="text-end">
                                                    Actions
                                                </th>
                                            </tr>
                                        </thead>

                                        <tbody>
                                            {suppliers.map((supplier) => (
                                                <tr key={supplier.id}>
                                                    <td>
                                                        <button
                                                            type="button"
                                                            className="btn btn-link p-0 fw-semibold text-start text-decoration-none"
                                                            onClick={() =>
                                                                setViewTarget(
                                                                    supplier
                                                                )
                                                            }
                                                        >
                                                            {
                                                                supplier.companyName
                                                            }
                                                        </button>
                                                        <div>
                                                            <small className="text-muted">
                                                                {supplier.name}
                                                            </small>
                                                        </div>
                                                    </td>

                                                    <td className="text-nowrap">
                                                        {supplier.mobile}
                                                    </td>

                                                    <td className="d-none d-md-table-cell">
                                                        {supplier.email ?? (
                                                            <span className="text-muted">
                                                                —
                                                            </span>
                                                        )}
                                                    </td>

                                                    <td className="d-none d-lg-table-cell cell-truncate">
                                                        {supplier.address ?? (
                                                            <span className="text-muted">
                                                                —
                                                            </span>
                                                        )}
                                                    </td>

                                                    <td className="text-end text-nowrap">
                                                        <button
                                                            type="button"
                                                            className="btn btn-sm btn-outline-secondary me-2"
                                                            aria-label={`View ${supplier.companyName}`}
                                                            onClick={() =>
                                                                setViewTarget(
                                                                    supplier
                                                                )
                                                            }
                                                        >
                                                            <i className="bi bi-eye"></i>
                                                        </button>

                                                        <button
                                                            type="button"
                                                            className="btn btn-sm btn-outline-primary me-2"
                                                            aria-label={`Edit ${supplier.companyName}`}
                                                            onClick={() =>
                                                                setFormState({
                                                                    mode: "edit",
                                                                    supplier,
                                                                })
                                                            }
                                                        >
                                                            <i className="bi bi-pencil"></i>
                                                        </button>

                                                        <button
                                                            type="button"
                                                            className="btn btn-sm btn-outline-danger"
                                                            aria-label={`Delete ${supplier.companyName}`}
                                                            onClick={() =>
                                                                setDeleteTarget(
                                                                    supplier
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
                                    onPageChange={goToPage}
                                />
                            </div>
                        )}
                    </>
                )}
            </div>

            {formState.mode !== "closed" && (
                <SupplierFormModal
                    key={
                        formState.mode === "edit"
                            ? formState.supplier.id
                            : "new"
                    }
                    supplier={
                        formState.mode === "edit" ? formState.supplier : null
                    }
                    onClose={() => setFormState({ mode: "closed" })}
                    onSaved={handleSaved}
                />
            )}

            <DetailsModal
                show={viewTarget !== null}
                title="Supplier details"
                items={[
                    { label: "Company", value: viewTarget?.companyName },
                    { label: "Contact name", value: viewTarget?.name },
                    { label: "Mobile", value: viewTarget?.mobile },
                    { label: "Email", value: viewTarget?.email },
                    { label: "State", value: viewTarget?.state },
                    { label: "GSTIN", value: viewTarget?.gstin },
                    { label: "Address", value: viewTarget?.address },
                ]}
                onClose={() => setViewTarget(null)}
                onEdit={() => {
                    if (viewTarget) {
                        setFormState({ mode: "edit", supplier: viewTarget });
                        setViewTarget(null);
                    }
                }}
            />

            <ConfirmDialog
                show={deleteTarget !== null}
                title="Delete supplier"
                message={
                    <>
                        <p className="mb-2">
                            Delete{" "}
                            <strong>{deleteTarget?.companyName}</strong>?
                        </p>
                        <p className="mb-0 text-muted">
                            The supplier will be removed from this list.
                            Existing purchases and reports keep their
                            supplier details.
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

export default SuppliersPage;
