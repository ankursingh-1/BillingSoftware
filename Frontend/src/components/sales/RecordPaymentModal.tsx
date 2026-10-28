import { useForm } from "react-hook-form";
import { toast } from "react-toastify";

import Modal from "../common/Modal";

import { PAYMENT_METHODS } from "../../constants/payments";
import { recordPayment } from "../../services/sales/salesService";
import { getApiErrorMessage } from "../../utils/apiError";
import { formatCurrency, hasMaxTwoDecimals } from "../../utils/format";

import type { PaymentMethod } from "../../interfaces/lookups/lookups";
import type { SaleDetail } from "../../interfaces/sale/sale";

interface FormValues {
    method: PaymentMethod;
    amount: string;
    reference: string;
    notes: string;
    startTracking: boolean;
}

interface RecordPaymentModalProps {
    sale: SaleDetail;
    onClose: () => void;
    onSaved: (sale: SaleDetail) => void;
}

const FORM_ID = "record-payment-form";

function RecordPaymentModal({
    sale,
    onClose,
    onSaved,
}: RecordPaymentModalProps) {
    // Older invoices were created before payments were tracked. Recording a
    // payment on one starts tracking, and the user must confirm that.
    const isLegacy = !sale.paymentTracked;
    const balance = isLegacy ? sale.totalAmount : sale.outstanding;

    const {
        register,
        handleSubmit,
        setValue,
        setError,
        formState: { errors, isSubmitting },
    } = useForm<FormValues>({
        defaultValues: {
            method: "Cash",
            amount: balance > 0 ? balance.toFixed(2) : "",
            reference: "",
            notes: "",
            startTracking: false,
        },
    });

    const onSubmit = async (values: FormValues) => {
        try {
            const updated = await recordPayment(sale.id, {
                method: values.method,
                amount: Number(values.amount),
                reference: values.reference.trim() || null,
                notes: values.notes.trim() || null,
                startTracking: isLegacy ? values.startTracking : undefined,
            });

            toast.success("Payment recorded.");
            onSaved(updated);
        } catch (error) {
            const message = getApiErrorMessage(
                error,
                "Unable to record the payment."
            );

            if (/amount|balance|outstanding|exceed/i.test(message)) {
                setError("amount", { type: "server", message });
            } else {
                toast.error(message);
            }
        }
    };

    return (
        <Modal
            show
            title={`Record payment · ${sale.invoiceNumber}`}
            onClose={onClose}
            busy={isSubmitting}
            footer={
                <>
                    <button
                        type="button"
                        className="btn btn-outline-secondary"
                        onClick={onClose}
                        disabled={isSubmitting}
                    >
                        Cancel
                    </button>

                    <button
                        type="submit"
                        form={FORM_ID}
                        className="btn btn-primary"
                        disabled={isSubmitting}
                    >
                        {isSubmitting && (
                            <span
                                className="spinner-border spinner-border-sm me-2"
                                aria-hidden="true"
                            ></span>
                        )}
                        Record payment
                    </button>
                </>
            }
        >
            <form
                id={FORM_ID}
                onSubmit={handleSubmit(onSubmit)}
                noValidate
            >
                <div className="alert alert-light border py-2">
                    {isLegacy ? (
                        <>
                            Invoice total{" "}
                            <strong>{formatCurrency(sale.totalAmount)}</strong>
                            . Payments were not tracked for this older
                            invoice.
                        </>
                    ) : (
                        <>
                            Balance due{" "}
                            <strong>{formatCurrency(sale.outstanding)}</strong>
                        </>
                    )}
                </div>

                <div className="row g-3">
                    <div className="col-sm-6">
                        <label htmlFor="pay-method" className="form-label">
                            Method
                        </label>

                        <select
                            id="pay-method"
                            className="form-select"
                            {...register("method")}
                        >
                            {PAYMENT_METHODS.map((method) => (
                                <option
                                    key={method.value}
                                    value={method.value}
                                >
                                    {method.label}
                                </option>
                            ))}
                        </select>
                    </div>

                    <div className="col-sm-6">
                        <label htmlFor="pay-amount" className="form-label">
                            Amount <span className="text-danger">*</span>
                        </label>

                        <div className="input-group">
                            <input
                                id="pay-amount"
                                type="number"
                                inputMode="decimal"
                                step="0.01"
                                min="0"
                                autoFocus
                                className={`form-control${
                                    errors.amount ? " is-invalid" : ""
                                }`}
                                aria-invalid={errors.amount ? "true" : "false"}
                                {...register("amount", {
                                    validate: (value) => {
                                        const amount = Number(value);

                                        if (!value || !(amount > 0)) {
                                            return "Enter an amount greater than 0.";
                                        }

                                        if (!hasMaxTwoDecimals(amount)) {
                                            return "Use at most 2 decimal places.";
                                        }

                                        if (!isLegacy && amount > balance) {
                                            return `Cannot exceed the balance of ${formatCurrency(
                                                balance
                                            )}.`;
                                        }

                                        return true;
                                    },
                                })}
                            />

                            {balance > 0 && (
                                <button
                                    type="button"
                                    className="btn btn-outline-secondary"
                                    onClick={() =>
                                        setValue(
                                            "amount",
                                            balance.toFixed(2),
                                            { shouldValidate: true }
                                        )
                                    }
                                >
                                    Full
                                </button>
                            )}

                            {errors.amount && (
                                <div
                                    className="invalid-feedback"
                                    role="alert"
                                >
                                    {errors.amount.message}
                                </div>
                            )}
                        </div>
                    </div>

                    <div className="col-12">
                        <label htmlFor="pay-ref" className="form-label">
                            Reference
                        </label>

                        <input
                            id="pay-ref"
                            type="text"
                            maxLength={100}
                            autoComplete="off"
                            placeholder="UPI / cheque / transaction number"
                            className="form-control"
                            {...register("reference")}
                        />
                    </div>

                    <div className="col-12">
                        <label htmlFor="pay-notes" className="form-label">
                            Notes
                        </label>

                        <input
                            id="pay-notes"
                            type="text"
                            maxLength={500}
                            autoComplete="off"
                            className="form-control"
                            {...register("notes")}
                        />
                    </div>

                    {isLegacy && (
                        <div className="col-12">
                            <div className="form-check">
                                <input
                                    id="pay-start"
                                    type="checkbox"
                                    className={`form-check-input${
                                        errors.startTracking
                                            ? " is-invalid"
                                            : ""
                                    }`}
                                    {...register("startTracking", {
                                        validate: (value) =>
                                            value ||
                                            "Confirm to start tracking payments on this invoice.",
                                    })}
                                />

                                <label
                                    htmlFor="pay-start"
                                    className="form-check-label"
                                >
                                    Start tracking payments on this older
                                    invoice. Earlier payments, if any, will
                                    not be known.
                                </label>

                                {errors.startTracking && (
                                    <div
                                        className="invalid-feedback d-block"
                                        role="alert"
                                    >
                                        {errors.startTracking.message}
                                    </div>
                                )}
                            </div>
                        </div>
                    )}
                </div>
            </form>
        </Modal>
    );
}

export default RecordPaymentModal;