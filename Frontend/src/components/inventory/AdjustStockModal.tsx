import { useRef, useState } from "react";
import { toast } from "react-toastify";

import Modal from "../common/Modal";

import { createAdjustment } from "../../services/inventory/inventoryService";
import { getApiErrorMessage } from "../../utils/apiError";

import type {
    AdjustmentReason,
    StockItem,
} from "../../interfaces/inventory/inventory";

interface AdjustStockModalProps {
    item: StockItem;
    onClose: () => void;
    onSaved: () => void;
}

const FORM_ID = "adjust-stock-form";

const REASONS: { value: AdjustmentReason; label: string }[] = [
    { value: "Correction", label: "Stock count correction" },
    { value: "Damaged", label: "Damaged" },
    { value: "Expired", label: "Expired" },
    { value: "Lost", label: "Lost / stolen" },
];

function AdjustStockModal({ item, onClose, onSaved }: AdjustStockModalProps) {
    const [increase, setIncrease] = useState(false);
    const [quantity, setQuantity] = useState("");
    const [reason, setReason] = useState<AdjustmentReason>("Correction");
    const [remarks, setRemarks] = useState("");
    const [touched, setTouched] = useState(false);
    const [submitting, setSubmitting] = useState(false);
    const [formError, setFormError] = useState<string | null>(null);
    const submittingRef = useRef(false);

    // Damaged/expired/lost can only reduce stock.
    const reduceOnly = reason !== "Correction";
    const adding = increase && !reduceOnly;

    const quantityNumber = Number(quantity);
    const quantityError =
        !Number.isInteger(quantityNumber) || quantityNumber < 1
            ? "Enter a whole number, 1 or more."
            : !adding && quantityNumber > item.stock
              ? `Only ${item.stock} in stock.`
              : null;
    const remarksError =
        remarks.trim().length < 3
            ? "Enter a remark (at least 3 characters)."
            : null;

    const newStock = quantityError
        ? null
        : item.stock + (adding ? quantityNumber : -quantityNumber);

    const handleSubmit = async (event: React.FormEvent) => {
        event.preventDefault();
        setTouched(true);

        if (submittingRef.current || quantityError || remarksError) {
            return;
        }

        submittingRef.current = true;
        setSubmitting(true);
        setFormError(null);

        try {
            const result = await createAdjustment({
                productId: item.productId,
                quantity: quantityNumber,
                increaseStock: adding,
                reason,
                remarks: remarks.trim(),
            });

            toast.success(
                `Stock adjusted (${result.referenceNo}): ${result.previousStock} → ${result.currentStock}.`
            );
            onSaved();
        } catch (err) {
            setFormError(getApiErrorMessage(err, "Unable to adjust the stock."));
        } finally {
            submittingRef.current = false;
            setSubmitting(false);
        }
    };

    return (
        <Modal
            show
            title={`Adjust stock · ${item.name}`}
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
                        Adjust stock
                    </button>
                </>
            }
        >
            <form id={FORM_ID} onSubmit={(e) => void handleSubmit(e)} noValidate>
                <div className="alert alert-light border py-2">
                    Current stock: <strong>{item.stock}</strong> ({item.sku})
                </div>

                {formError && (
                    <div className="alert alert-danger" role="alert">
                        {formError}
                    </div>
                )}

                <div className="mb-3">
                    <label htmlFor="adj-reason" className="form-label">
                        Reason
                    </label>
                    <select
                        id="adj-reason"
                        className="form-select"
                        value={reason}
                        disabled={submitting}
                        onChange={(e) =>
                            setReason(e.target.value as AdjustmentReason)
                        }
                    >
                        {REASONS.map((option) => (
                            <option key={option.value} value={option.value}>
                                {option.label}
                            </option>
                        ))}
                    </select>
                </div>

                <div className="row g-3 mb-3">
                    <div className="col-sm-6">
                        <label htmlFor="adj-direction" className="form-label">
                            Change
                        </label>
                        <select
                            id="adj-direction"
                            className="form-select"
                            value={adding ? "add" : "remove"}
                            disabled={submitting || reduceOnly}
                            onChange={(e) => setIncrease(e.target.value === "add")}
                        >
                            <option value="remove">Reduce stock</option>
                            <option value="add">Increase stock</option>
                        </select>
                    </div>

                    <div className="col-sm-6">
                        <label htmlFor="adj-qty" className="form-label">
                            Quantity
                        </label>
                        <input
                            id="adj-qty"
                            type="number"
                            min={1}
                            step={1}
                            className={`form-control${
                                touched && quantityError ? " is-invalid" : ""
                            }`}
                            value={quantity}
                            disabled={submitting}
                            onChange={(e) => setQuantity(e.target.value)}
                        />
                        {touched && quantityError && (
                            <div className="invalid-feedback" role="alert">
                                {quantityError}
                            </div>
                        )}
                    </div>
                </div>

                {newStock !== null && (
                    <div className="form-text mb-3">
                        New stock will be <strong>{newStock}</strong>.
                    </div>
                )}

                <div>
                    <label htmlFor="adj-remarks" className="form-label">
                        Remarks <span className="text-danger">*</span>
                    </label>
                    <textarea
                        id="adj-remarks"
                        className={`form-control${
                            touched && remarksError ? " is-invalid" : ""
                        }`}
                        rows={2}
                        maxLength={500}
                        value={remarks}
                        disabled={submitting}
                        onChange={(e) => setRemarks(e.target.value)}
                    />
                    {touched && remarksError && (
                        <div className="invalid-feedback" role="alert">
                            {remarksError}
                        </div>
                    )}
                </div>
            </form>
        </Modal>
    );
}

export default AdjustStockModal;