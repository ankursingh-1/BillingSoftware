import { useRef, useState } from "react";
import { Link, useNavigate } from "react-router-dom";
import { toast } from "react-toastify";

import EntityPicker from "../../components/common/EntityPicker";

import { searchProducts } from "../../services/products/productService";
import { createPurchase } from "../../services/purchases/purchaseService";
import { searchSuppliers } from "../../services/suppliers/supplierService";
import { getApiErrorMessage, getApiStatus } from "../../utils/apiError";
import {
    formatCurrency,
    hasMaxTwoDecimals,
    roundMoney,
    todayLocal,
} from "../../utils/format";

import type { Product } from "../../interfaces/product/product";
import type { Supplier } from "../../interfaces/supplier/supplier";

import "../../styles/data-pages.css";
import "../../styles/sales.css";

interface PurchaseLine {
    product: Product;
    quantity: string;
    price: string;
}

const findSuppliers = async (query: string): Promise<Supplier[]> =>
    (
        await searchSuppliers({
            search: query || undefined,
            pageNumber: 1,
            pageSize: 8,
        })
    ).data;

const findProducts = async (query: string): Promise<Product[]> =>
    (
        await searchProducts({
            search: query || undefined,
            pageNumber: 1,
            pageSize: 8,
        })
    ).data;

function NewPurchasePage() {
    const navigate = useNavigate();

    const [supplier, setSupplier] = useState<Supplier | null>(null);
    const [purchaseDate, setPurchaseDate] = useState(todayLocal());
    const [supplierInvoice, setSupplierInvoice] = useState("");
    const [remarks, setRemarks] = useState("");
    const [updateCost, setUpdateCost] = useState(false);
    const [lines, setLines] = useState<PurchaseLine[]>([]);

    const [submitting, setSubmitting] = useState(false);
    const [formError, setFormError] = useState<string | null>(null);

    const submittingRef = useRef(false);
    // Reused only while the purchase content is unchanged, so a retry after a
    // lost response cannot book the same purchase twice.
    const requestRef = useRef<{ signature: string; id: string } | null>(null);

    // ---- Line validation and estimates (the server calculates the real totals) ----
    const lineChecks = lines.map((line) => {
        const quantity = Number(line.quantity);
        const price = Number(line.price);

        const quantityError =
            !Number.isInteger(quantity) || quantity < 1 || quantity > 1_000_000
                ? "Whole number, 1 to 1,000,000."
                : null;
        const priceError =
            !Number.isFinite(price) || price <= 0
                ? "Enter a price above 0."
                : !hasMaxTwoDecimals(price)
                  ? "At most 2 decimals."
                  : null;

        const taxable =
            quantityError || priceError ? 0 : roundMoney(quantity * price);
        const rate = line.product.taxPercentage ?? 0;

        return {
            quantityError,
            priceError,
            taxable,
            tax: roundMoney((taxable * rate) / 100),
        };
    });

    const subtotal = roundMoney(
        lineChecks.reduce((sum, line) => sum + line.taxable, 0)
    );
    const tax = roundMoney(lineChecks.reduce((sum, line) => sum + line.tax, 0));
    const exactTotal = roundMoney(subtotal + tax);
    const grandTotal = Math.round(exactTotal);
    const roundOff = roundMoney(grandTotal - exactTotal);

    const hasLineError = lineChecks.some(
        (line) => line.quantityError || line.priceError
    );
    const dateError =
        purchaseDate === ""
            ? "Choose the purchase date."
            : purchaseDate > todayLocal()
              ? "The purchase date cannot be in the future."
              : null;

    const canSubmit =
        supplier !== null &&
        lines.length > 0 &&
        !hasLineError &&
        !dateError &&
        !submitting;

    const addProduct = (product: Product) => {
        setFormError(null);

        setLines((current) => {
            if (current.some((line) => line.product.id === product.id)) {
                toast.info(`"${product.name}" is already on this purchase.`);
                return current;
            }

            return [
                ...current,
                {
                    product,
                    quantity: "1",
                    price:
                        product.purchasePrice > 0
                            ? String(product.purchasePrice)
                            : "",
                },
            ];
        });
    };

    const updateLine = (
        productId: number,
        change: Partial<Pick<PurchaseLine, "quantity" | "price">>
    ) => {
        setFormError(null);
        setLines((current) =>
            current.map((line) =>
                line.product.id === productId ? { ...line, ...change } : line
            )
        );
    };

    const handleSubmit = async () => {
        if (submittingRef.current || !supplier || !canSubmit) {
            return;
        }

        const payload = {
            supplierId: supplier.id,
            purchaseDate,
            supplierInvoiceNumber: supplierInvoice.trim() || null,
            remarks: remarks.trim() || null,
            items: lines.map((line) => ({
                productId: line.product.id,
                quantity: Number(line.quantity),
                purchasePrice: Number(line.price),
            })),
            updateCatalogueCost: updateCost,
        };

        const signature = JSON.stringify(payload);

        if (requestRef.current?.signature !== signature) {
            requestRef.current = { signature, id: crypto.randomUUID() };
        }

        submittingRef.current = true;
        setSubmitting(true);
        setFormError(null);

        try {
            const response = await createPurchase({
                ...payload,
                clientRequestId: requestRef.current.id,
            });

            toast[response.isDuplicate ? "info" : "success"](
                response.isDuplicate
                    ? `This purchase was already saved as ${response.purchaseNumber}.`
                    : `Purchase ${response.purchaseNumber} saved. Stock updated.`
            );

            navigate(`/purchases/${response.purchaseId}`, { replace: true });
        } catch (err) {
            if (getApiStatus(err) === undefined) {
                setFormError(
                    "The server could not be reached, so it is not certain the purchase was saved. It is safe to press Save again: the same purchase will not be booked twice."
                );
            } else {
                setFormError(
                    getApiErrorMessage(err, "Unable to save the purchase.")
                );
            }
        } finally {
            submittingRef.current = false;
            setSubmitting(false);
        }
    };

    return (
        <div className="data-page">
            <div className="data-page-header">
                <div>
                    <h2 className="page-title">New Purchase</h2>
                    <p className="data-page-subtitle">
                        Prices are before tax. Tax and totals are calculated
                        by the server when you save.
                    </p>
                </div>
            </div>

            <div className="pos-layout">
                <div>
                    <section className="data-card pos-panel">
                        <h5 className="pos-panel-title">1. Supplier</h5>

                        {supplier ? (
                            <div className="d-flex justify-content-between align-items-start gap-3">
                                <div>
                                    <strong>{supplier.companyName}</strong>
                                    <div className="text-muted">
                                        {supplier.name} · {supplier.mobile}
                                        {supplier.gstin &&
                                            ` · GSTIN ${supplier.gstin}`}
                                    </div>
                                    <div className="small text-muted">
                                        {supplier.state
                                            ? `Supplier state: ${supplier.state}`
                                            : "No state saved. Your own state is assumed (CGST + SGST)."}
                                    </div>
                                </div>

                                <button
                                    type="button"
                                    className="btn btn-sm btn-outline-secondary"
                                    disabled={submitting}
                                    onClick={() => setSupplier(null)}
                                >
                                    Change
                                </button>
                            </div>
                        ) : (
                            <EntityPicker
                                id="purchase-supplier"
                                label="Search supplier"
                                placeholder="Search supplier by name, company or mobile"
                                icon="bi-truck"
                                search={findSuppliers}
                                getKey={(item) => item.id}
                                onSelect={setSupplier}
                                emptyHint="No suppliers found. Add the supplier on the Suppliers page first."
                                renderOption={(item) => (
                                    <>
                                        <span>
                                            <strong>{item.companyName}</strong>
                                            <small className="d-block">
                                                {item.name}
                                            </small>
                                        </span>
                                        <small>{item.mobile}</small>
                                    </>
                                )}
                            />
                        )}
                    </section>

                    <section className="data-card pos-panel">
                        <h5 className="pos-panel-title">2. Items</h5>

                        <div className="mb-3">
                            <EntityPicker
                                id="purchase-product"
                                label="Search product"
                                placeholder="Search product by name or SKU"
                                icon="bi-box-seam"
                                search={findProducts}
                                getKey={(item) => item.id}
                                onSelect={addProduct}
                                emptyHint="No products found."
                                renderOption={(item) => (
                                    <>
                                        <span>
                                            <strong>{item.name}</strong>
                                            <small className="d-block">
                                                {item.sku} · in stock{" "}
                                                {item.stock}
                                            </small>
                                        </span>
                                        <small>
                                            Cost{" "}
                                            {item.purchasePrice > 0
                                                ? formatCurrency(
                                                      item.purchasePrice
                                                  )
                                                : "not set"}
                                        </small>
                                    </>
                                )}
                            />
                        </div>

                        {lines.length === 0 ? (
                            <div className="text-center text-muted py-4">
                                <i className="bi bi-box-seam fs-3 d-block mb-2"></i>
                                Search and select the products you received.
                            </div>
                        ) : (
                            <div className="table-responsive">
                                <table className="table align-middle mb-0">
                                    <thead>
                                        <tr>
                                            <th>Product</th>
                                            <th style={{ width: 120 }}>Qty</th>
                                            <th style={{ width: 150 }}>
                                                Price (excl. tax)
                                            </th>
                                            <th className="text-end">Amount</th>
                                            <th></th>
                                        </tr>
                                    </thead>
                                    <tbody>
                                        {lines.map((line, index) => {
                                            const check = lineChecks[index];

                                            return (
                                                <tr key={line.product.id}>
                                                    <td>
                                                        <strong>
                                                            {line.product.name}
                                                        </strong>
                                                        <div className="small text-muted">
                                                            {line.product.sku}
                                                            {line.product
                                                                .taxPercentage
                                                                ? ` · ${line.product.taxPercentage}% tax`
                                                                : ""}
                                                        </div>
                                                    </td>
                                                    <td>
                                                        <input
                                                            type="number"
                                                            min={1}
                                                            step={1}
                                                            className={`form-control form-control-sm${
                                                                check.quantityError
                                                                    ? " is-invalid"
                                                                    : ""
                                                            }`}
                                                            aria-label={`Quantity of ${line.product.name}`}
                                                            value={line.quantity}
                                                            disabled={submitting}
                                                            onChange={(e) =>
                                                                updateLine(
                                                                    line.product
                                                                        .id,
                                                                    {
                                                                        quantity:
                                                                            e
                                                                                .target
                                                                                .value,
                                                                    }
                                                                )
                                                            }
                                                        />
                                                        {check.quantityError && (
                                                            <div
                                                                className="invalid-feedback d-block"
                                                                role="alert"
                                                            >
                                                                {
                                                                    check.quantityError
                                                                }
                                                            </div>
                                                        )}
                                                    </td>
                                                    <td>
                                                        <input
                                                            type="number"
                                                            min={0.01}
                                                            step="0.01"
                                                            className={`form-control form-control-sm${
                                                                check.priceError
                                                                    ? " is-invalid"
                                                                    : ""
                                                            }`}
                                                            aria-label={`Purchase price of ${line.product.name}`}
                                                            value={line.price}
                                                            disabled={submitting}
                                                            onChange={(e) =>
                                                                updateLine(
                                                                    line.product
                                                                        .id,
                                                                    {
                                                                        price: e
                                                                            .target
                                                                            .value,
                                                                    }
                                                                )
                                                            }
                                                        />
                                                        {check.priceError && (
                                                            <div
                                                                className="invalid-feedback d-block"
                                                                role="alert"
                                                            >
                                                                {check.priceError}
                                                            </div>
                                                        )}
                                                    </td>
                                                    <td className="text-end text-nowrap">
                                                        {formatCurrency(
                                                            check.taxable
                                                        )}
                                                    </td>
                                                    <td className="text-end">
                                                        <button
                                                            type="button"
                                                            className="btn btn-sm btn-outline-danger"
                                                            aria-label={`Remove ${line.product.name}`}
                                                            disabled={submitting}
                                                            onClick={() =>
                                                                setLines(
                                                                    (current) =>
                                                                        current.filter(
                                                                            (
                                                                                item
                                                                            ) =>
                                                                                item
                                                                                    .product
                                                                                    .id !==
                                                                                line
                                                                                    .product
                                                                                    .id
                                                                        )
                                                                )
                                                            }
                                                        >
                                                            <i className="bi bi-x-lg"></i>
                                                        </button>
                                                    </td>
                                                </tr>
                                            );
                                        })}
                                    </tbody>
                                </table>
                            </div>
                        )}
                    </section>
                </div>

                <aside className="pos-sticky">
                    <section className="data-card pos-panel">
                        <h5 className="pos-panel-title">3. Details</h5>

                        <div className="mb-3">
                            <label htmlFor="purchase-date" className="form-label">
                                Purchase date
                            </label>
                            <input
                                id="purchase-date"
                                type="date"
                                className={`form-control${
                                    dateError ? " is-invalid" : ""
                                }`}
                                max={todayLocal()}
                                value={purchaseDate}
                                disabled={submitting}
                                onChange={(e) => setPurchaseDate(e.target.value)}
                            />
                            {dateError && (
                                <div className="invalid-feedback" role="alert">
                                    {dateError}
                                </div>
                            )}
                        </div>

                        <div className="mb-3">
                            <label
                                htmlFor="purchase-supplier-invoice"
                                className="form-label"
                            >
                                Supplier invoice number
                            </label>
                            <input
                                id="purchase-supplier-invoice"
                                type="text"
                                className="form-control"
                                maxLength={50}
                                value={supplierInvoice}
                                disabled={submitting}
                                onChange={(e) =>
                                    setSupplierInvoice(e.target.value)
                                }
                            />
                            <div className="form-text">
                                Their bill number. The same number cannot be
                                entered twice for one supplier.
                            </div>
                        </div>

                        <div className="mb-3">
                            <label htmlFor="purchase-remarks" className="form-label">
                                Remarks
                            </label>
                            <textarea
                                id="purchase-remarks"
                                className="form-control"
                                rows={2}
                                maxLength={500}
                                value={remarks}
                                disabled={submitting}
                                onChange={(e) => setRemarks(e.target.value)}
                            />
                        </div>

                        <div className="form-check mb-3">
                            <input
                                id="purchase-update-cost"
                                type="checkbox"
                                className="form-check-input"
                                checked={updateCost}
                                disabled={submitting}
                                onChange={(e) => setUpdateCost(e.target.checked)}
                            />
                            <label
                                htmlFor="purchase-update-cost"
                                className="form-check-label"
                            >
                                Set each product&apos;s catalogue cost to the
                                price on this purchase
                            </label>
                            <div className="form-text">
                                Profit on future sales uses the catalogue cost.
                                Leave off unless these prices are your new cost.
                            </div>
                        </div>

                        <dl className="pos-totals row mb-0">
                            <dt className="col-6">Subtotal</dt>
                            <dd className="col-6">{formatCurrency(subtotal)}</dd>

                            <dt className="col-6">Tax (estimate)</dt>
                            <dd className="col-6">{formatCurrency(tax)}</dd>

                            {roundOff !== 0 && (
                                <>
                                    <dt className="col-6">Round off</dt>
                                    <dd className="col-6">
                                        {formatCurrency(roundOff)}
                                    </dd>
                                </>
                            )}

                            <dt className="col-6 pos-grand">Total</dt>
                            <dd className="col-6 pos-grand">
                                {formatCurrency(grandTotal)}
                            </dd>
                        </dl>

                        {formError && (
                            <div className="alert alert-danger mt-3" role="alert">
                                {formError}
                            </div>
                        )}

                        <div className="d-grid gap-2 mt-3">
                            <button
                                type="button"
                                className="btn btn-primary"
                                disabled={!canSubmit}
                                onClick={() => void handleSubmit()}
                            >
                                {submitting && (
                                    <span
                                        className="spinner-border spinner-border-sm me-2"
                                        aria-hidden="true"
                                    ></span>
                                )}
                                Save Purchase
                            </button>

                            <Link
                                to="/purchases"
                                className="btn btn-outline-secondary"
                            >
                                Cancel
                            </Link>
                        </div>
                    </section>
                </aside>
            </div>
        </div>
    );
}

export default NewPurchasePage;