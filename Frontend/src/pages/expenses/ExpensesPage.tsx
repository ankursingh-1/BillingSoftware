import { useState } from "react";
import { toast } from "react-toastify";

import ConfirmDialog from "../../components/common/ConfirmDialog";
import DateRangeInputs, {
    DateRangeWarning,
} from "../../components/common/DateRangeInputs";
import Modal from "../../components/common/Modal";
import PagedTableCard from "../../components/common/PagedTableCard";

import { useDateRange } from "../../hooks/useDateRange";
import { usePagedSearch } from "../../hooks/usePagedSearch";
import {
    createExpense,
    deleteExpense,
    searchExpenses,
    updateExpense,
} from "../../services/expenses/expenseService";
import { getApiErrorMessage } from "../../utils/apiError";
import { emptyToNull } from "../../utils/validation";
import { formatCurrency, formatDate, todayLocal } from "../../utils/format";

import type { Expense } from "../../interfaces/expense/expense";

import "../../styles/data-pages.css";

const LOAD_ERROR = "Unable to load expenses.";

type FormState =
    | { mode: "closed" }
    | { mode: "create" }
    | { mode: "edit"; expense: Expense };

function ExpensesPage() {
    const range = useDateRange();
    const list = usePagedSearch(searchExpenses, LOAD_ERROR, range.filters);

    const [form, setForm] = useState<FormState>({ mode: "closed" });
    const [title, setTitle] = useState("");
    const [amount, setAmount] = useState("");
    const [date, setDate] = useState(todayLocal());
    const [category, setCategory] = useState("");
    const [remarks, setRemarks] = useState("");
    const [saving, setSaving] = useState(false);
    const [deleteTarget, setDeleteTarget] = useState<Expense | null>(null);
    const [deleting, setDeleting] = useState(false);

    const openCreate = () => {
        setTitle("");
        setAmount("");
        setDate(todayLocal());
        setCategory("");
        setRemarks("");
        setForm({ mode: "create" });
    };

    const openEdit = (expense: Expense) => {
        setTitle(expense.title);
        setAmount(String(expense.amount));
        setDate(expense.expenseDate.slice(0, 10));
        setCategory(expense.category ?? "");
        setRemarks(expense.remarks ?? "");
        setForm({ mode: "edit", expense });
    };

    const save = async () => {
        const parsed = Number(amount);

        if (!title.trim()) {
            toast.error("Title is required.");
            return;
        }

        if (!(parsed > 0)) {
            toast.error("Amount must be greater than 0.");
            return;
        }

        try {
            setSaving(true);
            const body = {
                title: title.trim(),
                amount: parsed,
                expenseDate: date,
                category: emptyToNull(category),
                remarks: emptyToNull(remarks),
            };

            if (form.mode === "edit") {
                await updateExpense(form.expense.id, body);
                toast.success("Expense updated.");
            } else {
                await createExpense(body);
                toast.success("Expense recorded.");
            }

            setForm({ mode: "closed" });
            list.reload();
        } catch (error) {
            toast.error(getApiErrorMessage(error, "Unable to save the expense."));
        } finally {
            setSaving(false);
        }
    };

    return (
        <div className="data-page">
            <div className="data-page-header">
                <div>
                    <h2 className="page-title">Expenses</h2>
                    <p className="data-page-subtitle">
                        Shop costs (rent, utilities, and so on). These are not
                        product purchases.
                    </p>
                </div>
                <button
                    type="button"
                    className="btn btn-primary"
                    onClick={openCreate}
                >
                    <i className="bi bi-plus-lg me-2"></i>
                    Add expense
                </button>
            </div>

            <PagedTableCard
                list={list}
                noun="expenses"
                searchPlaceholder="Search title, category or notes"
                filters={
                    <>
                        <DateRangeInputs range={range} idPrefix="exp" />
                    </>
                }
                banner={<DateRangeWarning range={range} />}
                emptyIcon="bi-wallet2"
                emptyText="No expenses recorded yet."
                isFiltering={
                    list.debouncedSearch.length > 0 ||
                    range.from.length > 0 ||
                    range.to.length > 0
                }
                onClearFilters={() => {
                    list.setSearch("");
                    range.setFrom("");
                    range.setTo("");
                }}
                head={
                    <tr>
                        <th>Date</th>
                        <th>Title</th>
                        <th>Category</th>
                        <th className="text-end">Amount</th>
                        <th></th>
                    </tr>
                }
                renderRow={(item) => (
                    <tr key={item.id}>
                        <td>{formatDate(item.expenseDate)}</td>
                        <td>
                            <div className="fw-semibold">{item.title}</div>
                            {item.remarks && (
                                <small className="text-muted">{item.remarks}</small>
                            )}
                        </td>
                        <td>{item.category ?? "—"}</td>
                        <td className="text-end">{formatCurrency(item.amount)}</td>
                        <td className="text-end text-nowrap">
                            <button
                                type="button"
                                className="btn btn-sm btn-outline-primary me-2"
                                onClick={() => openEdit(item)}
                            >
                                Edit
                            </button>
                            <button
                                type="button"
                                className="btn btn-sm btn-outline-danger"
                                onClick={() => setDeleteTarget(item)}
                            >
                                Delete
                            </button>
                        </td>
                    </tr>
                )}
            />

            <Modal
                show={form.mode !== "closed"}
                title={form.mode === "edit" ? "Edit expense" : "Add expense"}
                busy={saving}
                onClose={() => setForm({ mode: "closed" })}
                footer={
                    <>
                        <button
                            type="button"
                            className="btn btn-outline-secondary"
                            onClick={() => setForm({ mode: "closed" })}
                            disabled={saving}
                        >
                            Cancel
                        </button>
                        <button
                            type="button"
                            className="btn btn-primary"
                            disabled={saving}
                            onClick={() => void save()}
                        >
                            Save
                        </button>
                    </>
                }
            >
                <label className="form-label" htmlFor="exp-title">
                    Title <span className="text-danger">*</span>
                </label>
                <input
                    id="exp-title"
                    className="form-control mb-3"
                    value={title}
                    onChange={(e) => setTitle(e.target.value)}
                    maxLength={200}
                />
                <div className="row g-3">
                    <div className="col-md-6">
                        <label className="form-label" htmlFor="exp-amount">
                            Amount <span className="text-danger">*</span>
                        </label>
                        <input
                            id="exp-amount"
                            type="number"
                            min="0.01"
                            step="0.01"
                            className="form-control"
                            value={amount}
                            onChange={(e) => setAmount(e.target.value)}
                        />
                    </div>
                    <div className="col-md-6">
                        <label className="form-label" htmlFor="exp-date">
                            Date <span className="text-danger">*</span>
                        </label>
                        <input
                            id="exp-date"
                            type="date"
                            className="form-control"
                            value={date}
                            onChange={(e) => setDate(e.target.value)}
                        />
                    </div>
                </div>
                <label className="form-label mt-3" htmlFor="exp-cat">
                    Category
                </label>
                <input
                    id="exp-cat"
                    className="form-control mb-3"
                    value={category}
                    onChange={(e) => setCategory(e.target.value)}
                    maxLength={100}
                    placeholder="Rent, utilities, transport…"
                />
                <label className="form-label" htmlFor="exp-notes">
                    Notes
                </label>
                <textarea
                    id="exp-notes"
                    className="form-control"
                    rows={2}
                    value={remarks}
                    onChange={(e) => setRemarks(e.target.value)}
                    maxLength={500}
                />
            </Modal>

            <ConfirmDialog
                show={deleteTarget !== null}
                title="Delete expense"
                message={
                    <>
                        Delete <strong>{deleteTarget?.title}</strong>?
                    </>
                }
                busy={deleting}
                onCancel={() => setDeleteTarget(null)}
                onConfirm={() => {
                    if (!deleteTarget) return;
                    setDeleting(true);
                    void deleteExpense(deleteTarget.id)
                        .then(() => {
                            toast.success("Expense deleted.");
                            setDeleteTarget(null);
                            list.reload();
                        })
                        .catch((error) =>
                            toast.error(
                                getApiErrorMessage(
                                    error,
                                    "Unable to delete the expense."
                                )
                            )
                        )
                        .finally(() => setDeleting(false));
                }}
            />
        </div>
    );
}

export default ExpensesPage;
