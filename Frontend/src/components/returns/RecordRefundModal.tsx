import { useRef, useState } from "react";

import Modal from "../common/Modal";

import { PAYMENT_METHODS } from "../../constants/payments";
import { getApiErrorMessage } from "../../utils/apiError";
import { formatCurrency, hasMaxTwoDecimals } from "../../utils/format";

import type { PaymentMethod } from "../../interfaces/lookups/lookups";

interface RecordRefundModalProps {
    returnNumber: string;
    refundDue: number;
    onClose: () => void;
    /** Throw to keep the modal open and show the error. */
    onSubmit: (amount: number, method: PaymentMethod) => Promise<void>;
}

const FORM_ID = "record-refund-form";

function RecordRefundModal({
    returnNumber,
    refundDue,
    onClose,
    onSubmit,
}: RecordRefundModalProps) {
    const [amount, setAmount] = useState(refundDue.toFixed(2));
    const [method, setMethod] = useState<PaymentMethod>("Cash");
    const [submitting, setSubmitting] = useState(false);
    const [formError, setFormError] = useState<string | null>(null);
    const submittingRef = useRef(false);

    const amountNumber = Number(amount);
    const amountError =
        !Number.isFinite(amountNumber) || amountNumber <= 0
            ? "Enter an amount greater than 0."
            : !hasMaxTwoDecimals(amountNumber)
              ? "Use at most 2 decimal places."
              : amountNumber > refundDue + 0.005
                ? `Cannot exceed the refund due of ${formatCurrency(refundDue)}.`
                : null;

    const handleSubmit = async (event: React.FormEvent) => {
        event.preventDefault();

        if (submittingRef.current || amountError) {
            return;
        }

        submittingRef.current = true;
        setSubmitting(true);
        setFormError(null);

        try {
            await onSubmit(amountNumber, method);
        } catch (err) {
            setFormError(getApiErrorMessage(err, "Unable to record the refund."));
        } finally {
            submittingRef.current = false;
            setSubmitting(false);
        }
    };

    return (
        <Modal
            show
            title={`Record refund · ${returnNumber}`}
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
                        disabled={submitting || amountError !== null}
                    >
                        {submitting && (
                            <span
                                className="spinner-border spinner-border-sm me-2"
                                aria-hidden="true"
                            ></span>
                        )}
                        Record refund
                    </button>
                </>
            }
        >
            <form id={FORM_ID} onSubmit={(e) => void handleSubmit(e)} noValidate>
                <div className="alert alert-light border py-2">
                    Refund still owed to the customer:{" "}
                    <strong>{formatCurrency(refundDue)}</strong>
                </div>

                {formError && (
                    <div className="alert alert-danger" role="alert">
                        {formError}
                    </div>
                )}

                <div className="row g-3">
                    <div className="col-sm-6">
                        <label htmlFor="refund-amount" className="form-label">
                            Amount
                        </label>
                        <input
                            id="refund-amount"
                            type="number"
                            min={0.01}
                            step="0.01"
                            className={`form-control${
                                amountError ? " is-invalid" : ""
                            }`}
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
                        <label htmlFor="refund-method" className="form-label">
                            Method
                        </label>
                        <select
                            id="refund-method"
                            className="form-select"
                            value={method}
                            disabled={submitting}
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
            </form>
        </Modal>
    );
}

export default RecordRefundModal;