import { useState } from "react";
import { toast } from "react-toastify";

import ConfirmDialog from "../../components/common/ConfirmDialog";
import DetailsModal from "../../components/common/DetailsModal";
import Pagination from "../../components/common/Pagination";
import CustomerFormModal from "../../components/forms/CustomerFormModal";

import {
    PAGE_SIZE_OPTIONS,
    usePagedSearch,
} from "../../hooks/usePagedSearch";
import {
    deleteCustomer,
    searchCustomers,
} from "../../services/customers/customerService";
import { getApiErrorMessage } from "../../utils/apiError";

import type { Customer } from "../../interfaces/customer/customer";

import "../../styles/data-pages.css";

const LOAD_ERROR = "Unable to load customers.";

type FormState =
    | { mode: "closed" }
    | { mode: "create" }
    | { mode: "edit"; customer: Customer };

function CustomersPage() {
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
    } = usePagedSearch(searchCustomers, LOAD_ERROR);

    const [formState, setFormState] = useState<FormState>({
        mode: "closed",
    });
    const [viewTarget, setViewTarget] = useState<Customer | null>(null);
    const [deleteTarget, setDeleteTarget] = useState<Customer | null>(null);
    const [deleting, setDeleting] = useState(false);

    const handleConfirmDelete = async () => {
        if (!deleteTarget) {
            return;
        }

        try {
            setDeleting(true);
            await deleteCustomer(deleteTarget.id);
            toast.success(`"${deleteTarget.name}" deleted.`);
            setDeleteTarget(null);
            reload();
        } catch (err) {
            toast.error(
                getApiErrorMessage(err, "Unable to delete the customer.")
            );
        } finally {
            setDeleting(false);
        }
    };

    const handleSaved = () => {
        setFormState({ mode: "closed" });
        reload();
    };

    const customers = result?.data ?? [];
    const isFiltering = debouncedSearch.length > 0;

    return (
        <div className="data-page">
            <div className="data-page-header">
                <div>
                    <h2 className="page-title">Customers</h2>
                    <p className="data-page-subtitle">
                        Manage the people and businesses you sell to
                    </p>
                </div>

                <div className="data-page-actions">
                    <button
                        type="button"
                        className="btn btn-primary"
                        onClick={() => setFormState({ mode: "create" })}
                    >
                        <i className="bi bi-plus-lg me-2"></i>
                        Add Customer
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
                            placeholder="Search by name or mobile"
                            aria-label="Search customers by name or mobile"
                            value={search}
                            onChange={(e) => setSearch(e.target.value)}
                        />
                    </div>

                    <div className="data-toolbar-right">
                        <label
                            htmlFor="customer-page-size"
                            className="visually-hidden"
                        >
                            Rows per page
                        </label>

                        <select
                            id="customer-page-size"
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
                                Loading customers...
                            </span>
                        </div>
                        <p>Loading customers...</p>
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

                        {customers.length === 0 ? (
                            <div className="state-block">
                                <i className="bi bi-people state-icon"></i>
                                <p>
                                    {isFiltering
                                        ? `No customers match "${debouncedSearch}".`
                                        : "No customers yet. Add your first customer to get started."}
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
                                        Add Customer
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
                                                <th>Customer</th>
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
                                            {customers.map((customer) => (
                                                <tr key={customer.id}>
                                                    <td>
                                                        <button
                                                            type="button"
                                                            className="btn btn-link p-0 fw-semibold text-start text-decoration-none"
                                                            onClick={() =>
                                                                setViewTarget(
                                                                    customer
                                                                )
                                                            }
                                                        >
                                                            {customer.name}
                                                        </button>
                                                    </td>

                                                    <td className="text-nowrap">
                                                        {customer.mobile}
                                                    </td>

                                                    <td className="d-none d-md-table-cell">
                                                        {customer.email ?? (
                                                            <span className="text-muted">
                                                                —
                                                            </span>
                                                        )}
                                                    </td>

                                                    <td className="d-none d-lg-table-cell cell-truncate">
                                                        {customer.address ?? (
                                                            <span className="text-muted">
                                                                —
                                                            </span>
                                                        )}
                                                    </td>

                                                    <td className="text-end text-nowrap">
                                                        <button
                                                            type="button"
                                                            className="btn btn-sm btn-outline-secondary me-2"
                                                            aria-label={`View ${customer.name}`}
                                                            onClick={() =>
                                                                setViewTarget(
                                                                    customer
                                                                )
                                                            }
                                                        >
                                                            <i className="bi bi-eye"></i>
                                                        </button>

                                                        <button
                                                            type="button"
                                                            className="btn btn-sm btn-outline-primary me-2"
                                                            aria-label={`Edit ${customer.name}`}
                                                            onClick={() =>
                                                                setFormState({
                                                                    mode: "edit",
                                                                    customer,
                                                                })
                                                            }
                                                        >
                                                            <i className="bi bi-pencil"></i>
                                                        </button>

                                                        <button
                                                            type="button"
                                                            className="btn btn-sm btn-outline-danger"
                                                            aria-label={`Delete ${customer.name}`}
                                                            onClick={() =>
                                                                setDeleteTarget(
                                                                    customer
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
                <CustomerFormModal
                    key={
                        formState.mode === "edit"
                            ? formState.customer.id
                            : "new"
                    }
                    customer={
                        formState.mode === "edit" ? formState.customer : null
                    }
                    onClose={() => setFormState({ mode: "closed" })}
                    onSaved={handleSaved}
                />
            )}

            <DetailsModal
                show={viewTarget !== null}
                title="Customer details"
                items={[
                    { label: "Name", value: viewTarget?.name },
                    { label: "Mobile", value: viewTarget?.mobile },
                    { label: "Email", value: viewTarget?.email },
                    { label: "Address", value: viewTarget?.address },
                ]}
                onClose={() => setViewTarget(null)}
                onEdit={() => {
                    if (viewTarget) {
                        setFormState({ mode: "edit", customer: viewTarget });
                        setViewTarget(null);
                    }
                }}
            />

            <ConfirmDialog
                show={deleteTarget !== null}
                title="Delete customer"
                message={
                    <>
                        <p className="mb-2">
                            Delete <strong>{deleteTarget?.name}</strong>?
                        </p>
                        <p className="mb-0 text-muted">
                            The customer will be removed from this list.
                            Existing sales and reports keep their customer
                            details.
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

export default CustomersPage;
