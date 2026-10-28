import { useForm } from "react-hook-form";
import { toast } from "react-toastify";

import Modal from "../common/Modal";
import PartyTaxFields from "./PartyTaxFields";

import {
    createSupplier,
    updateSupplier,
} from "../../services/suppliers/supplierService";
import {
    getApiErrorMessage,
    getApiStatus,
    getFieldErrors,
} from "../../utils/apiError";
import {
    EMAIL_PATTERN,
    GSTIN_PATTERN,
    MOBILE_PATTERN,
    emptyToNull,
    normalizeMobile,
} from "../../utils/validation";

import type { Supplier } from "../../interfaces/supplier/supplier";

interface SupplierFormValues {
    companyName: string;
    name: string;
    mobile: string;
    email: string;
    address: string;
    stateCode: string;
    gstin: string;
}

type FieldName = keyof SupplierFormValues;

// Server property names (lower-cased) -> form field
const FIELD_LOOKUP: Record<string, FieldName> = {
    companyname: "companyName",
    name: "name",
    mobile: "mobile",
    email: "email",
    address: "address",
    statecode: "stateCode",
    gstin: "gstin",
};

interface SupplierFormModalProps {
    /** Supplier being edited, or null to create a new one. */
    supplier: Supplier | null;
    onClose: () => void;
    onSaved: () => void;
}

const FORM_ID = "supplier-form";

function SupplierFormModal({
    supplier,
    onClose,
    onSaved,
}: SupplierFormModalProps) {
    const isEdit = supplier !== null;

    const {
        register,
        handleSubmit,
        setError,
        formState: { errors, isSubmitting },
    } = useForm<SupplierFormValues>({
        defaultValues: {
            companyName: supplier?.companyName ?? "",
            name: supplier?.name ?? "",
            mobile: supplier?.mobile ?? "",
            email: supplier?.email ?? "",
            address: supplier?.address ?? "",
            stateCode: supplier?.stateCode ?? "",
            gstin: supplier?.gstin ?? "",
        },
    });

    const onSubmit = async (values: SupplierFormValues) => {
        const payload = {
            name: values.name.trim(),
            companyName: values.companyName.trim(),
            mobile: normalizeMobile(values.mobile.trim()),
            email: emptyToNull(values.email),
            address: emptyToNull(values.address),
            stateCode: emptyToNull(values.stateCode),
            gstin: emptyToNull(values.gstin.toUpperCase()),
        };

        try {
            if (supplier) {
                await updateSupplier(supplier.id, payload);
                toast.success("Supplier updated.");
            } else {
                await createSupplier(payload);
                toast.success("Supplier created.");
            }

            onSaved();
        } catch (error) {
            let handled = false;

            for (const [field, message] of Object.entries(
                getFieldErrors(error)
            )) {
                const key = FIELD_LOOKUP[field.toLowerCase()];

                if (key) {
                    setError(key, { type: "server", message });
                    handled = true;
                }
            }

            if (handled) {
                return;
            }

            const message = getApiErrorMessage(
                error,
                "Unable to save the supplier."
            );

            if (getApiStatus(error) === 409) {
                // Duplicate mobile number
                setError("mobile", { type: "server", message });
            } else {
                toast.error(message);
            }
        }
    };

    return (
        <Modal
            show
            title={isEdit ? "Edit Supplier" : "Add Supplier"}
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
                        {isEdit ? "Save Changes" : "Create Supplier"}
                    </button>
                </>
            }
        >
            <form
                id={FORM_ID}
                onSubmit={handleSubmit(onSubmit)}
                noValidate
            >
                <div className="mb-3">
                    <label htmlFor="supplier-company" className="form-label">
                        Company name <span className="text-danger">*</span>
                    </label>

                    <input
                        id="supplier-company"
                        type="text"
                        className={`form-control${
                            errors.companyName ? " is-invalid" : ""
                        }`}
                        autoFocus
                        autoComplete="off"
                        aria-invalid={errors.companyName ? "true" : "false"}
                        {...register("companyName", {
                            validate: (value) =>
                                value.trim().length > 0 ||
                                "Company name is required.",
                            maxLength: {
                                value: 200,
                                message:
                                    "Company name cannot exceed 200 characters.",
                            },
                        })}
                    />

                    {errors.companyName && (
                        <div className="invalid-feedback" role="alert">
                            {errors.companyName.message}
                        </div>
                    )}
                </div>

                <div className="mb-3">
                    <label htmlFor="supplier-name" className="form-label">
                        Contact name <span className="text-danger">*</span>
                    </label>

                    <input
                        id="supplier-name"
                        type="text"
                        className={`form-control${
                            errors.name ? " is-invalid" : ""
                        }`}
                        autoComplete="off"
                        aria-invalid={errors.name ? "true" : "false"}
                        {...register("name", {
                            validate: (value) =>
                                value.trim().length > 0 ||
                                "Contact name is required.",
                            maxLength: {
                                value: 200,
                                message:
                                    "Contact name cannot exceed 200 characters.",
                            },
                        })}
                    />

                    {errors.name && (
                        <div className="invalid-feedback" role="alert">
                            {errors.name.message}
                        </div>
                    )}
                </div>

                <div className="mb-3">
                    <label htmlFor="supplier-mobile" className="form-label">
                        Mobile <span className="text-danger">*</span>
                    </label>

                    <input
                        id="supplier-mobile"
                        type="tel"
                        inputMode="tel"
                        className={`form-control${
                            errors.mobile ? " is-invalid" : ""
                        }`}
                        autoComplete="off"
                        aria-invalid={errors.mobile ? "true" : "false"}
                        {...register("mobile", {
                            validate: (value) => {
                                const mobile = normalizeMobile(value.trim());

                                if (mobile.length === 0) {
                                    return "Mobile number is required.";
                                }

                                return (
                                    MOBILE_PATTERN.test(mobile) ||
                                    "Enter a valid mobile number (7 to 15 digits, optional leading +)."
                                );
                            },
                        })}
                    />

                    {errors.mobile && (
                        <div className="invalid-feedback" role="alert">
                            {errors.mobile.message}
                        </div>
                    )}
                </div>

                <div className="mb-3">
                    <label htmlFor="supplier-email" className="form-label">
                        Email
                    </label>

                    <input
                        id="supplier-email"
                        type="email"
                        className={`form-control${
                            errors.email ? " is-invalid" : ""
                        }`}
                        autoComplete="off"
                        aria-invalid={errors.email ? "true" : "false"}
                        {...register("email", {
                            validate: (value) => {
                                const email = value.trim();

                                if (email.length === 0) {
                                    return true;
                                }

                                if (email.length > 200) {
                                    return "Email cannot exceed 200 characters.";
                                }

                                return (
                                    EMAIL_PATTERN.test(email) ||
                                    "Enter a valid email address."
                                );
                            },
                        })}
                    />

                    {errors.email && (
                        <div className="invalid-feedback" role="alert">
                            {errors.email.message}
                        </div>
                    )}
                </div>

                <PartyTaxFields
                    idPrefix="supplier"
                    stateProps={register("stateCode")}
                    gstinProps={register("gstin", {
                        validate: (value) => {
                            const gstin = value.trim().toUpperCase();

                            return (
                                gstin.length === 0 ||
                                GSTIN_PATTERN.test(gstin) ||
                                "Enter a valid 15-character GSTIN."
                            );
                        },
                    })}
                    stateError={errors.stateCode?.message}
                    gstinError={errors.gstin?.message}
                />

                <div className="mb-0">
                    <label htmlFor="supplier-address" className="form-label">
                        Address
                    </label>

                    <textarea
                        id="supplier-address"
                        rows={3}
                        className={`form-control${
                            errors.address ? " is-invalid" : ""
                        }`}
                        aria-invalid={errors.address ? "true" : "false"}
                        {...register("address", {
                            maxLength: {
                                value: 500,
                                message:
                                    "Address cannot exceed 500 characters.",
                            },
                        })}
                    />

                    {errors.address && (
                        <div className="invalid-feedback" role="alert">
                            {errors.address.message}
                        </div>
                    )}
                </div>
            </form>
        </Modal>
    );
}

export default SupplierFormModal;
