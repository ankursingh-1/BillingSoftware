import { useRef, useState } from "react";
import type { ReactNode } from "react";

import Modal from "../common/Modal";

import { PAYMENT_METHODS } from "../../constants/payments";
import { getApiErrorMessage } from "../../utils/apiError";
import {
    formatCurrency,
    hasMaxTwoDecimals,
    roundMoney,
} from "../../utils/format";

import type { PaymentMethod } from "../../interfaces/lookups/lookups";

export interface ReturnableLine {
    id: number;
    name: string;
    sku: string | null;
    returnable: number;
    /** Approximate value of one unit including tax (a hint only). */
    unitValue: number;
}

export interface ReturnSubmission {
    items: { id: number; quantity: number }[];
    reason: string;
    amount: number;
    method?: PaymentMethod;
    note?: string;
}

interface ReturnItemsModalProps {
    title: string;
    intro?: ReactNode;
    lines: ReturnableLine[];
    amountLabel: string;
    amountHelp: ReactNode;
    /** Purchase returns can record how the remainder is settled. */
    showAdjustmentNote?: boolean;
    submitLabel: string;
    onClose: () => void;
    /** Throw to keep the modal open and show the error. */
    onSubmit: (submission: ReturnSubmission) => Promise<void>;
}

const FORM_ID = "return-items-form";

function ReturnItemsModal({
    title,
    intro,
    lines,
    amountLabel,
    amountHelp,
    showAdjustmentNote = false,
    submitLabel,
    onClose,
    onSubmit,
}: ReturnItemsModalProps) {
    const [quantities, setQuantities] = useState<Record<number, string>>({});
    const [reason, setReason] = useState("");
    const [amount, setAmount] = useState("");
    const [method, setMethod] = useState<PaymentMethod>("Cash");
    const [note, setNote] = useState("");
    const [submitting, setSubmitting] = useState(false);
    const [formError, setFormError] = useState<string | null>(null);
    const [touched, setTouched] = useState(false);

    // Guards against a double click before React re-renders the button.
    const submittingRef = useRef(false);

    const selected = lines
        .map((line) => ({
            line,
            quantity: Number(quantities[line.id] ?? ""),
        }))
        .filter((entry) => entry.quantity > 0);

    const lineError = (line: ReturnableLine): string | null => {
        const raw = quantities[line.id];

        if (raw === undefined || raw === "") {
            return null;
        }

        const quantity = Number(raw);

        if (!Number.isInteger(quantity) || quantity < 0) {
            return "Enter a whole number.";
        }

        if (quantity > line.returnable) {
            return `At most ${line.returnable}.`;
        }

        return null;
    };

    const estimate = roundMoney(
        selected.reduce(
            (sum, entry) => sum + entry.line.unitValue * entry.quantity,
            0
        )
    );

    const amountNumber = Number(amount || 0);
    const amountError =
        amount === ""
            ? null
            : !Number.isFinite(amountNumber) || amountNumber < 0
              ? "Enter a valid amount."
              : !hasMaxTwoDecimals(amountNumber)
                ? "Use at most 2 decimal places."
                : null;

    const reasonError =
        reason.trim().length < 3 ? "Enter a reason (at least 3 characters)." : null;
    const itemsError =
        selected.length === 0 ? "Enter a quantity for at least one item." : null;
    const hasLineError = lines.some((line) => lineError(line) !== null);

    const valid =
        !reasonError && !itemsError && !hasLineError && !amountError;

    const handleSubmit = async (event: React.FormEvent) => {
        event.preventDefault();
        setTouched(true);

        if (submittingRef.current || !valid) {
            return;
        }

        submittingRef.current = true;
        setSubmitting(true);
        setFormError(null);

        try {
            await onSubmit({
                items: selected.map((entry) => ({
                    id: entry.line.id,
                    quantity: entry.quantity,
                })),
                reason: reason.trim(),
                amount: amountNumber,
                method: amountNumber > 0 ? method : undefined,
                note: note.trim() || undefined,
            });
        } catch (err) {
            setFormError(getApiErrorMessage(err, "Unable to save the return."));
        } finally {
            submittingRef.current = false;
            setSubmitting(false);
        }
    };

    return (
        <Modal
            show
            title={title}
            onClose={onClose}
            busy={submitting}
            footer={
                <>
                    <button
                        type="button"
                        className="btn btn-outline-secondary"
                        onClick={onClose}
                        disabled={submitting}
                    >
                        Cancel
                    </button>

                    <button
                        type="submit"
                        form={FORM_ID}
                        className="btn btn-primary"
                        disabled={submitting}
                    >
                        {submitting && (
                            <span
                                className="spinner-border spinner-border-sm me-2"
                                aria-hidden="true"
                            ></span>
                        )}
                        {submitLabel}
                    </button>
                </>
            }
        >
            <form id={FORM_ID} onSubmit={(e) => void handleSubmit(e)} noValidate>
                {intro && <div className="alert alert-light border py-2">{intro}</div>}

                {formError && (
                    <div className="alert alert-danger" role="alert">
                        {formError}
                    </div>
                )}

                <div className="table-responsive mb-2">
                    <table className="table align-middle mb-0">
                        <thead>
                            <tr>
                                <th>Item</th>
                                <th className="text-end">Can return</th>
                                <th style={{ width: 110 }}>Return qty</th>
                            </tr>
                        </thead>
                        <tbody>
                            {lines.map((line) => {
                                const error = lineError(line);

                                return (
                                    <tr key={line.id}>
                                        <td>
                                            <strong>{line.name}</strong>
                                            {line.sku && (
                                                <div className="small text-muted">
                                                    {line.sku}
                                                </div>
                                            )}
                                        </td>
                                        <td className="text-end">
                                            {line.returnable}
                                        </td>
                                        <td>
                                            <input
                                                type="number"
                                                min={0}
                                                max={line.returnable}
                                                step={1}
                                                className={`form-control form-control-sm${
                                                    error ? " is-invalid" : ""
                                                }`}
                                                aria-label={`Return quantity of ${line.name}`}
                                                disabled={
                                                    line.returnable <= 0 ||
                                                    submitting
                                                }
                                                value={quantities[line.id] ?? ""}
                                                onChange={(e) =>
                                                    setQuantities((current) => ({
                                                        ...current,
                                                        [line.id]: e.target.value,
                                                    }))
                                                }
                                            />
                                            {error && (
                                                <div
                                                    className="invalid-feedback d-block"
                                                    role="alert"
                                                >
                                                    {error}
                                                </div>
                                            )}
                                        </td>
                                    </tr>
                                );
                            })}
                        </tbody>
                    </table>
                </div>

                {touched && itemsError && (
                    <div className="text-danger small mb-2" role="alert">
                        {itemsError}
                    </div>
                )}

                <div className="mb-3">
                    <label htmlFor="return-reason" className="form-label">
                        Reason <span className="text-danger">*</span>
                    </label>
                    <textarea
                        id="return-reason"
                        className={`form-control${
                            touched && reasonError ? " is-invalid" : ""
                        }`}
                        rows={2}
                        maxLength={500}
                        value={reason}
                        disabled={submitting}
                        onChange={(e) => setReason(e.target.value)}
                    />
                    {touched && reasonError && (
                        <div className="invalid-feedback" role="alert">
                            {reasonError}
                        </div>
                    )}
                </div>

                <div className="row g-3">
                    <div className="col-sm-6">
                        <label htmlFor="return-amount" className="form-label">
                            {amountLabel}
                        </label>
                        <input
                            id="return-amount"
                            type="number"
                            min={0}
                            step="0.01"
                            className={`form-control${
                                amountError ? " is-invalid" : ""
                            }`}
                            placeholder="0.00"
                            value={amount}
                            disabled={submitting}
                            onChange={(e) => setAmount(e.target.value)}
                        />
                        {amountError && (
                            <div className="invalid-feedback" role="alert">
                                {amountError}
                            </div>
                        )}
                    </div>

                    <div className="col-sm-6">
                        <label htmlFor="return-method" className="form-label">
                            Method
                        </label>
                        <select
                            id="return-method"
                            className="form-select"
                            value={method}
                            disabled={submitting || amountNumber <= 0}
                            onChange={(e) =>
                                setMethod(e.target.value as PaymentMethod)
                            }
                        >
                            {PAYMENT_METHODS.map((option) => (
                                <option key={option.value} value={option.value}>
                                    {option.label}
                                </option>
                            ))}
                        </select>
                    </div>
                </div>

                <div className="form-text mt-2">
                    {estimate > 0 && (
                        <>
                            Approximate value of the selected items:{" "}
                            <strong>{formatCurrency(estimate)}</strong>{" "}
                            <button
                                type="button"
                                className="btn btn-link btn-sm p-0 align-baseline"
                                onClick={() => setAmount(estimate.toFixed(2))}
                            >
                                use this amount
                            </button>
                            .{" "}
                        </>
                    )}
                    {amountHelp}
                </div>

                {showAdjustmentNote && (
                    <div className="mt-3">
                        <label htmlFor="return-note" className="form-label">
                            Settlement note
                        </label>
                        <input
                            id="return-note"
                            type="text"
                            className="form-control"
                            maxLength={500}
                            placeholder="e.g. credit against the next order"
                            value={note}
                            disabled={submitting}
                            onChange={(e) => setNote(e.target.value)}
                        />
                    </div>
                )}
            </form>
        </Modal>
    );
}

export default ReturnItemsModal;