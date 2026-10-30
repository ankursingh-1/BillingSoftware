import { useForm, useWatch } from "react-hook-form";
import { Link, useNavigate } from "react-router-dom";
import { toast } from "react-toastify";

import {
    createProduct,
    updateProduct,
} from "../../services/products/productService";
import {
    getApiErrorMessage,
    getApiStatus,
    getFieldErrors,
} from "../../utils/apiError";
import { hasMaxTwoDecimals } from "../../utils/format";

import type { Category } from "../../interfaces/category/category";
import type { Brand, Tax, Unit } from "../../interfaces/lookups/lookups";
import type {
    Product,
    SaveProductRequest,
} from "../../interfaces/product/product";

export interface ProductFormLookups {
    categories: Category[];
    brands: Brand[];
    units: Unit[];
    taxes: Tax[];
}

interface ProductFormProps {
    /** Product being edited, or null to create a new one. */
    product: Product | null;
    lookups: ProductFormLookups;
}

// UI sanity cap (₹ 9,999,999,999.99). The database allows more (18,2).
const MAX_PRICE = 9_999_999_999.99;

const FIELD_NAMES: (keyof SaveProductRequest)[] = [
    "name",
    "sku",
    "purchasePrice",
    "sellingPrice",
    "stock",
    "categoryId",
    "brandId",
    "unitId",
    "taxId",
    "hsnCode",
    "barcode",
    "warrantyDurationValue",
    "warrantyDurationUnit",
    "requiresSerialNumber",
];

interface SelectOption {
    id: number;
    label: string;
}

/** Active items, plus the currently selected one even if it is inactive. */
function buildOptions<T extends { id: number; isActive: boolean }>(
    items: T[],
    selectedId: number | null,
    getLabel: (item: T) => string
): SelectOption[] {
    return items
        .filter((item) => item.isActive || item.id === selectedId)
        .map((item) => ({
            id: item.id,
            label: item.isActive
                ? getLabel(item)
                : `${getLabel(item)} (inactive)`,
        }));
}

const toNullableNumber = (value: unknown): number | null =>
    value === "" || value === null || value === undefined
        ? null
        : Number(value);

function ProductForm({ product, lookups }: ProductFormProps) {
    const navigate = useNavigate();
    const isEdit = product !== null;

    const {
        register,
        handleSubmit,
        setError,
        control,
        formState: { errors, isSubmitting },
    } = useForm<SaveProductRequest>({
        defaultValues: {
            name: product?.name ?? "",
            sku: product?.sku ?? "",
            purchasePrice: product?.purchasePrice ?? 0,
            sellingPrice: product?.sellingPrice ?? 0,
            stock: product?.stock ?? 0,
            categoryId: product?.categoryId ?? null,
            brandId: product?.brandId ?? null,
            unitId: product?.unitId ?? null,
            taxId: product?.taxId ?? null,
            hsnCode: product?.hsnCode ?? "",
            barcode: product?.barcode ?? "",
            warrantyDurationValue: product?.warrantyDurationValue ?? null,
            warrantyDurationUnit: product?.warrantyDurationUnit ?? null,
            requiresSerialNumber: product?.requiresSerialNumber ?? false,
        },
    });

    const purchasePrice = useWatch({ control, name: "purchasePrice" });
    const sellingPrice = useWatch({ control, name: "sellingPrice" });

    const sellingBelowCost =
        Number.isFinite(purchasePrice) &&
        Number.isFinite(sellingPrice) &&
        sellingPrice < purchasePrice;

    const categoryOptions = buildOptions(
        lookups.categories,
        product?.categoryId ?? null,
        (c) => c.name
    );
    const brandOptions = buildOptions(
        lookups.brands,
        product?.brandId ?? null,
        (b) => b.name
    );
    const unitOptions = buildOptions(
        lookups.units,
        product?.unitId ?? null,
        (u) => (u.shortName ? `${u.name} (${u.shortName})` : u.name)
    );
    const taxOptions = buildOptions(
        lookups.taxes,
        product?.taxId ?? null,
        (t) => `${t.name} (${t.percentage}%)`
    );

    const onSubmit = async (values: SaveProductRequest) => {
        const payload: SaveProductRequest = {
            ...values,
            name: values.name.trim(),
            sku: values.sku.trim(),
            hsnCode: values.hsnCode?.trim() ? values.hsnCode.trim() : null,
            barcode: values.barcode?.trim() ? values.barcode.trim() : null,
            warrantyDurationValue: values.warrantyDurationValue || null,
            warrantyDurationUnit: values.warrantyDurationValue
                ? values.warrantyDurationUnit
                : null,
            stock: values.stock,
        };

        try {
            if (product) {
                await updateProduct(product.id, payload);
                toast.success("Product updated.");
            } else {
                await createProduct(payload);
                toast.success("Product created.");
            }

            navigate("/products");
        } catch (error) {
            let handled = false;

            for (const [field, message] of Object.entries(
                getFieldErrors(error)
            )) {
                const match = FIELD_NAMES.find(
                    (name) => name.toLowerCase() === field.toLowerCase()
                );

                if (match) {
                    setError(match, { type: "server", message });
                    handled = true;
                }
            }

            if (handled) {
                return;
            }

            const message = getApiErrorMessage(
                error,
                "Unable to save the product."
            );

            if (
                getApiStatus(error) === 409 &&
                message.toLowerCase().includes("sku")
            ) {
                setError("sku", { type: "server", message });
            } else {
                toast.error(message);
            }
        }
    };

    const fieldClass = (hasError: boolean) =>
        `form-control${hasError ? " is-invalid" : ""}`;

    const selectClass = (hasError: boolean) =>
        `form-select${hasError ? " is-invalid" : ""}`;

    return (
        <form onSubmit={handleSubmit(onSubmit)} noValidate>
            <div className="data-card form-section">
                <h5 className="form-section-title">Product Details</h5>

                <div className="row g-3">
                    <div className="col-md-8">
                        <label htmlFor="product-name" className="form-label">
                            Product name{" "}
                            <span className="text-danger">*</span>
                        </label>

                        <input
                            id="product-name"
                            type="text"
                            className={fieldClass(!!errors.name)}
                            autoFocus={!isEdit}
                            aria-invalid={errors.name ? "true" : "false"}
                            {...register("name", {
                                required: "Product name is required.",
                                validate: (value) =>
                                    value.trim().length > 0 ||
                                    "Product name is required.",
                                maxLength: {
                                    value: 200,
                                    message:
                                        "Product name cannot exceed 200 characters.",
                                },
                            })}
                        />

                        {errors.name && (
                            <div className="invalid-feedback" role="alert">
                                {errors.name.message}
                            </div>
                        )}
                    </div>

                    <div className="col-md-4">
                        <label htmlFor="product-sku" className="form-label">
                            SKU <span className="text-danger">*</span>
                        </label>

                        <input
                            id="product-sku"
                            type="text"
                            className={fieldClass(!!errors.sku)}
                            aria-invalid={errors.sku ? "true" : "false"}
                            {...register("sku", {
                                required: "SKU is required.",
                                validate: (value) =>
                                    value.trim().length > 0 ||
                                    "SKU is required.",
                                maxLength: {
                                    value: 200,
                                    message:
                                        "SKU cannot exceed 200 characters.",
                                },
                            })}
                        />

                        {errors.sku && (
                            <div className="invalid-feedback" role="alert">
                                {errors.sku.message}
                            </div>
                        )}
                    </div>

                    <div className="col-md-6 col-lg-3">
                        <label
                            htmlFor="product-category"
                            className="form-label"
                        >
                            Category
                        </label>

                        <select
                            id="product-category"
                            className={selectClass(!!errors.categoryId)}
                            {...register("categoryId", {
                                setValueAs: toNullableNumber,
                            })}
                        >
                            <option value="">— None —</option>
                            {categoryOptions.map((option) => (
                                <option key={option.id} value={option.id}>
                                    {option.label}
                                </option>
                            ))}
                        </select>

                        {errors.categoryId && (
                            <div className="invalid-feedback" role="alert">
                                {errors.categoryId.message}
                            </div>
                        )}
                    </div>

                    <div className="col-md-6 col-lg-3">
                        <label
                            htmlFor="product-brand"
                            className="form-label"
                        >
                            Brand
                        </label>

                        <select
                            id="product-brand"
                            className={selectClass(!!errors.brandId)}
                            {...register("brandId", {
                                setValueAs: toNullableNumber,
                            })}
                        >
                            <option value="">— None —</option>
                            {brandOptions.map((option) => (
                                <option key={option.id} value={option.id}>
                                    {option.label}
                                </option>
                            ))}
                        </select>

                        {errors.brandId && (
                            <div className="invalid-feedback" role="alert">
                                {errors.brandId.message}
                            </div>
                        )}
                    </div>

                    <div className="col-md-6 col-lg-3">
                        <label
                            htmlFor="product-unit"
                            className="form-label"
                        >
                            Unit
                        </label>

                        <select
                            id="product-unit"
                            className={selectClass(!!errors.unitId)}
                            {...register("unitId", {
                                setValueAs: toNullableNumber,
                            })}
                        >
                            <option value="">— None —</option>
                            {unitOptions.map((option) => (
                                <option key={option.id} value={option.id}>
                                    {option.label}
                                </option>
                            ))}
                        </select>

                        {errors.unitId && (
                            <div className="invalid-feedback" role="alert">
                                {errors.unitId.message}
                            </div>
                        )}
                    </div>

                    <div className="col-md-6 col-lg-3">
                        <label
                            htmlFor="product-tax"
                            className="form-label"
                        >
                            Tax
                        </label>

                        <select
                            id="product-tax"
                            className={selectClass(!!errors.taxId)}
                            {...register("taxId", {
                                setValueAs: toNullableNumber,
                            })}
                        >
                            <option value="">— None —</option>
                            {taxOptions.map((option) => (
                                <option key={option.id} value={option.id}>
                                    {option.label}
                                </option>
                            ))}
                        </select>

                        {errors.taxId && (
                            <div className="invalid-feedback" role="alert">
                                {errors.taxId.message}
                            </div>
                        )}
                    </div>

                    <div className="col-md-6 col-lg-3">
                        <label
                            htmlFor="product-hsn"
                            className="form-label"
                        >
                            HSN code
                        </label>

                        <input
                            id="product-hsn"
                            type="text"
                            inputMode="numeric"
                            maxLength={8}
                            autoComplete="off"
                            className={fieldClass(!!errors.hsnCode)}
                            aria-invalid={errors.hsnCode ? "true" : "false"}
                            {...register("hsnCode", {
                                validate: (value) =>
                                    !value ||
                                    value.trim().length === 0 ||
                                    /^(\d{4}|\d{6}|\d{8})$/.test(value.trim()) ||
                                    "HSN must be 4, 6 or 8 digits.",
                            })}
                        />

                        {errors.hsnCode && (
                            <div className="invalid-feedback" role="alert">
                                {errors.hsnCode.message}
                            </div>
                        )}
                    </div>
                </div>
            </div>

            <div className="data-card form-section">
                <h5 className="form-section-title">Pricing &amp; Stock</h5>

                <div className="row g-3">
                    <div className="col-md-4">
                        <label
                            htmlFor="product-purchase-price"
                            className="form-label"
                        >
                            Purchase price (₹){" "}
                            <span className="text-danger">*</span>
                        </label>

                        <input
                            id="product-purchase-price"
                            type="number"
                            step="0.01"
                            min="0"
                            inputMode="decimal"
                            className={fieldClass(!!errors.purchasePrice)}
                            aria-invalid={
                                errors.purchasePrice ? "true" : "false"
                            }
                            {...register("purchasePrice", {
                                valueAsNumber: true,
                                validate: (value) => {
                                    if (!Number.isFinite(value)) {
                                        return "Purchase price is required.";
                                    }
                                    if (value < 0) {
                                        return "Purchase price cannot be negative.";
                                    }
                                    if (value > MAX_PRICE) {
                                        return "Purchase price is too large.";
                                    }
                                    if (!hasMaxTwoDecimals(value)) {
                                        return "Use at most 2 decimal places.";
                                    }
                                    return true;
                                },
                            })}
                        />

                        {errors.purchasePrice && (
                            <div className="invalid-feedback" role="alert">
                                {errors.purchasePrice.message}
                            </div>
                        )}
                    </div>

                    <div className="col-md-4">
                        <label
                            htmlFor="product-selling-price"
                            className="form-label"
                        >
                            Selling price (₹){" "}
                            <span className="text-danger">*</span>
                        </label>

                        <input
                            id="product-selling-price"
                            type="number"
                            step="0.01"
                            min="0"
                            inputMode="decimal"
                            className={fieldClass(!!errors.sellingPrice)}
                            aria-invalid={
                                errors.sellingPrice ? "true" : "false"
                            }
                            {...register("sellingPrice", {
                                valueAsNumber: true,
                                validate: (value) => {
                                    if (!Number.isFinite(value)) {
                                        return "Selling price is required.";
                                    }
                                    if (value < 0) {
                                        return "Selling price cannot be negative.";
                                    }
                                    if (value > MAX_PRICE) {
                                        return "Selling price is too large.";
                                    }
                                    if (!hasMaxTwoDecimals(value)) {
                                        return "Use at most 2 decimal places.";
                                    }
                                    return true;
                                },
                            })}
                        />

                        {errors.sellingPrice && (
                            <div className="invalid-feedback" role="alert">
                                {errors.sellingPrice.message}
                            </div>
                        )}

                        {!errors.sellingPrice && sellingBelowCost && (
                            <div className="form-text text-warning-emphasis">
                                <i className="bi bi-exclamation-triangle me-1"></i>
                                Selling price is below the purchase price.
                            </div>
                        )}
                    </div>

                    <div className="col-md-4">
                        <label
                            htmlFor="product-stock"
                            className="form-label"
                        >
                            {isEdit ? "Current stock" : "Opening stock"}
                        </label>

                        <input
                            id="product-stock"
                            type="number"
                            step="1"
                            min="0"
                            inputMode="numeric"
                            className={fieldClass(!!errors.stock)}
                            aria-invalid={errors.stock ? "true" : "false"}
                            {...register("stock", {
                                valueAsNumber: true,
                                validate: (value) => {
                                    if (!Number.isFinite(value)) {
                                        return "Enter 0 or more.";
                                    }
                                    if (!Number.isInteger(value)) {
                                        return "Stock must be a whole number.";
                                    }
                                    if (value < 0) {
                                        return "Stock cannot be negative.";
                                    }
                                    if (value > 2_147_483_647) {
                                        return "Stock is too large.";
                                    }
                                    return true;
                                },
                            })}
                        />

                        {errors.stock && (
                            <div className="invalid-feedback" role="alert">
                                {errors.stock.message}
                            </div>
                        )}

                        <div className="form-text">
                            {isEdit
                                ? "A new quantity is saved as a stock adjustment. Stock cannot go below zero."
                                : "Recorded through the stock ledger as opening stock."}
                        </div>
                    </div>
                </div>
            </div>

            <div className="data-card form-section">
                <h5 className="form-section-title">Barcode &amp; warranty default</h5>
                <p className="text-muted small mb-3">
                    The default warranty is used on new sales. Changing it later
                    does not rewrite warranties already registered on invoices.
                    Serial numbers are stored on the warranty, not as
                    serial-level stock.
                </p>
                <div className="row g-3">
                    <div className="col-md-4">
                        <label htmlFor="product-barcode" className="form-label">
                            Barcode
                        </label>
                        <input
                            id="product-barcode"
                            type="text"
                            className={fieldClass(!!errors.barcode)}
                            {...register("barcode", {
                                maxLength: {
                                    value: 80,
                                    message: "Barcode cannot exceed 80 characters.",
                                },
                            })}
                        />
                        {errors.barcode && (
                            <div className="invalid-feedback" role="alert">
                                {errors.barcode.message}
                            </div>
                        )}
                    </div>
                    <div className="col-md-3">
                        <label htmlFor="product-warranty-value" className="form-label">
                            Default warranty
                        </label>
                        <input
                            id="product-warranty-value"
                            type="number"
                            min={1}
                            className={fieldClass(!!errors.warrantyDurationValue)}
                            {...register("warrantyDurationValue", {
                                setValueAs: toNullableNumber,
                            })}
                        />
                        {errors.warrantyDurationValue && (
                            <div className="invalid-feedback" role="alert">
                                {errors.warrantyDurationValue.message}
                            </div>
                        )}
                    </div>
                    <div className="col-md-3">
                        <label htmlFor="product-warranty-unit" className="form-label">
                            Unit
                        </label>
                        <select
                            id="product-warranty-unit"
                            className={selectClass(!!errors.warrantyDurationUnit)}
                            {...register("warrantyDurationUnit")}
                        >
                            <option value="">— None —</option>
                            <option value="Days">Days</option>
                            <option value="Months">Months</option>
                            <option value="Years">Years</option>
                        </select>
                    </div>
                    <div className="col-md-2 d-flex align-items-end">
                        <div className="form-check mb-2">
                            <input
                                id="product-serial"
                                type="checkbox"
                                className="form-check-input"
                                {...register("requiresSerialNumber")}
                            />
                            <label htmlFor="product-serial" className="form-check-label">
                                Serial required
                            </label>
                        </div>
                    </div>
                </div>
            </div>

            <div className="form-actions">
                <Link
                    to="/products"
                    className={`btn btn-outline-secondary${
                        isSubmitting ? " disabled" : ""
                    }`}
                    aria-disabled={isSubmitting}
                >
                    Cancel
                </Link>

                <button
                    type="submit"
                    className="btn btn-primary"
                    disabled={isSubmitting}
                >
                    {isSubmitting && (
                        <span
                            className="spinner-border spinner-border-sm me-2"
                            aria-hidden="true"
                        ></span>
                    )}
                    {isEdit ? "Save Changes" : "Create Product"}
                </button>
            </div>
        </form>
    );
}

export default ProductForm;
