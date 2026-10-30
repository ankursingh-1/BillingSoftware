import { useEffect, useRef, useState } from "react";
import { useNavigate } from "react-router-dom";
import { toast } from "react-toastify";

import { PAYMENT_METHODS } from "../../constants/payments";
import { useRemoteOptions } from "../../hooks/useRemoteOptions";
import {
    createCustomer,
    searchCustomers,
} from "../../services/customers/customerService";
import { getCompany } from "../../services/company/companyService";
import {
    getProduct,
    scanProduct,
    searchProducts,
} from "../../services/products/productService";
import { createSale } from "../../services/sales/salesService";
import {
    getApiErrorMessage,
    getApiStatus,
} from "../../utils/apiError";
import {
    formatCurrency,
    hasMaxTwoDecimals,
    roundMoney,
} from "../../utils/format";

import type { ShopType } from "../../interfaces/company/company";
import type { Customer } from "../../interfaces/customer/customer";
import type { PaymentMethod } from "../../interfaces/lookups/lookups";
import type { Product } from "../../interfaces/product/product";

import "../../styles/data-pages.css";
import "../../styles/sales.css";

interface CartLine {
    product: Product;
    quantity: number;
    registerWarranty: boolean;
    warrantyDurationValue: string;
    warrantyDurationUnit: "Days" | "Months" | "Years";
    serialText: string;
}

const WALK_IN_MOBILE = "9000000000";
const WALK_IN_NAME = "Walk-in Customer";

const findCustomers = async (query: string): Promise<Customer[]> =>
    (
        await searchCustomers({
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

function newRequestId(): string {
    return crypto.randomUUID();
}

function NewSalePage() {
    const navigate = useNavigate();

    const [customer, setCustomer] = useState<Customer | null>(null);
    const [customerQuery, setCustomerQuery] = useState("");
    const [customerOpen, setCustomerOpen] = useState(false);

    const [productQuery, setProductQuery] = useState("");
    const [productOpen, setProductOpen] = useState(false);

    const [cart, setCart] = useState<CartLine[]>([]);
    const [remarks, setRemarks] = useState("");

    const [payAmount, setPayAmount] = useState("");
    const [payMethod, setPayMethod] = useState<PaymentMethod>("Cash");
    const [payReference, setPayReference] = useState("");

    const [submitting, setSubmitting] = useState(false);
    const [refreshing, setRefreshing] = useState(false);
    const [formError, setFormError] = useState<string | null>(null);
    const [walkInBusy, setWalkInBusy] = useState(false);
    const [shopType, setShopType] = useState<ShopType>("GeneralRetail");
    const [barcode, setBarcode] = useState("");
    const [scanBusy, setScanBusy] = useState(false);
    const barcodeRef = useRef<HTMLInputElement>(null);

    // Guards against a double click before React re-renders the disabled button.
    const submittingRef = useRef(false);
    // The same request id is reused only while the sale content is unchanged,
    // so a retry after a lost response cannot create a duplicate invoice, and
    // an edited cart is never mistaken for the earlier attempt.
    const requestRef = useRef<{ signature: string; id: string } | null>(null);

    const customerOptions = useRemoteOptions(
        findCustomers,
        customerQuery,
        customerOpen
    );
    const productOptions = useRemoteOptions(
        findProducts,
        productQuery,
        productOpen
    );

    // ---- Estimated totals (the server calculates the real ones) ----
    const lineEstimates = cart.map((line) => {
        const taxable = roundMoney(line.product.sellingPrice * line.quantity);
        const rate = line.product.taxPercentage ?? 0;

        return { taxable, tax: roundMoney((taxable * rate) / 100) };
    });
    const subtotal = roundMoney(
        lineEstimates.reduce((sum, line) => sum + line.taxable, 0)
    );
    const tax = roundMoney(
        lineEstimates.reduce((sum, line) => sum + line.tax, 0)
    );
    const exactTotal = roundMoney(subtotal + tax);
    const grandTotal = Math.round(exactTotal);
    const roundOff = roundMoney(grandTotal - exactTotal);

    const lineProblems = cart.map((line) => {
        if (line.product.stock <= 0) {
            return "Out of stock.";
        }

        if (line.quantity > line.product.stock) {
            return `Only ${line.product.stock} in stock.`;
        }

        if (line.registerWarranty) {
            const value = Number(line.warrantyDurationValue);
            if (!Number.isInteger(value) || value < 1) {
                return "Enter a warranty duration of at least 1.";
            }

            const serials = line.serialText
                .split(/[\n,]/)
                .map((item) => item.trim())
                .filter(Boolean);

            if (
                line.product.requiresSerialNumber &&
                serials.length !== line.quantity
            ) {
                return `Enter ${line.quantity} serial number(s), one per unit.`;
            }

            if (serials.length > 0 && serials.length !== line.quantity) {
                return `Enter one serial per unit (${line.quantity}), or none.`;
            }
        }

        return null;
    });
    const hasLineProblem = lineProblems.some((problem) => problem !== null);

    const paying = Number(payAmount) > 0;
    const payAmountNumber = Number(payAmount);
    const payError = !paying
        ? null
        : !hasMaxTwoDecimals(payAmountNumber)
          ? "Use at most 2 decimal places."
          : payAmountNumber > grandTotal + 0.005
            ? `Cannot exceed the invoice total of ${formatCurrency(
                  grandTotal
              )}.`
            : null;

    const canSubmit =
        customer !== null &&
        cart.length > 0 &&
        !hasLineProblem &&
        !payError &&
        !submitting;

    const selectWalkIn = async () => {
        setWalkInBusy(true);
        setFormError(null);

        try {
            const existing = await searchCustomers({
                search: WALK_IN_MOBILE,
                pageNumber: 1,
                pageSize: 8,
            });
            let found = existing.data.find(
                (row) => row.mobile === WALK_IN_MOBILE
            );

            if (!found) {
                await createCustomer({
                    name: WALK_IN_NAME,
                    mobile: WALK_IN_MOBILE,
                    email: null,
                    address: null,
                    stateCode: null,
                    gstin: null,
                });
                const created = await searchCustomers({
                    search: WALK_IN_MOBILE,
                    pageNumber: 1,
                    pageSize: 8,
                });
                found = created.data.find(
                    (row) => row.mobile === WALK_IN_MOBILE
                );
            }

            if (!found) {
                toast.error(
                    "Walk-in customer could not be created. Add one on the Customers page."
                );
                return;
            }

            setCustomer(found);
            setCustomerQuery("");
            setCustomerOpen(false);
        } catch (err) {
            toast.error(
                getApiErrorMessage(err, "Unable to select the walk-in customer.")
            );
        } finally {
            setWalkInBusy(false);
        }
    };

    // ---- Cart actions ----
    useEffect(() => {
        let cancelled = false;

        const load = async () => {
            try {
                const company = await getCompany();
                if (cancelled || !company) {
                    return;
                }

                setShopType(company.shopType);
                if (company.shopType === "Grocery") {
                    await selectWalkIn();
                }
            } catch {
                // Shop type is optional; billing still works.
            }
        };

        void load();
        return () => {
            cancelled = true;
        };
    }, []);

    const addProduct = (product: Product) => {
        setFormError(null);

        if (product.stock <= 0) {
            toast.warning(`"${product.name}" is out of stock.`);
            return;
        }

        setCart((current) => {
            const existing = current.find(
                (line) => line.product.id === product.id
            );

            if (!existing) {
                const register =
                    shopType !== "Grocery" &&
                    product.warrantyDurationValue != null;

                return [
                    ...current,
                    {
                        product,
                        quantity: 1,
                        registerWarranty: register,
                        warrantyDurationValue:
                            product.warrantyDurationValue?.toString() ?? "1",
                        warrantyDurationUnit:
                            product.warrantyDurationUnit ?? "Years",
                        serialText: "",
                    },
                ];
            }

            if (existing.quantity >= product.stock) {
                toast.warning(
                    `Only ${product.stock} of "${product.name}" in stock.`
                );
                return current;
            }

            return current.map((line) =>
                line.product.id === product.id
                    ? { ...line, quantity: line.quantity + 1 }
                    : line
            );
        });

        setProductQuery("");
        setProductOpen(false);
    };

    const handleScan = async () => {
        const code = barcode.trim();
        if (!code || scanBusy) {
            return;
        }

        setScanBusy(true);
        try {
            const product = await scanProduct(code);
            addProduct(product);
            setBarcode("");
            barcodeRef.current?.focus();
        } catch (err) {
            if (getApiStatus(err) === 404) {
                toast.warning(
                    `No product matches barcode or SKU "${code}".`
                );
            } else {
                toast.error(getApiErrorMessage(err, "Unable to scan."));
            }
        } finally {
            setScanBusy(false);
        }
    };

    const setQuantity = (productId: number, value: string) => {
        setFormError(null);

        const parsed = Math.floor(Number(value));
        const quantity = Number.isFinite(parsed) && parsed >= 1 ? parsed : 1;

        setCart((current) =>
            current.map((line) =>
                line.product.id === productId ? { ...line, quantity } : line
            )
        );
    };

    const removeLine = (productId: number) => {
        setFormError(null);
        setCart((current) =>
            current.filter((line) => line.product.id !== productId)
        );
    };

    /** Reloads current prices and stock for every product in the cart. */
    const refreshCart = async () => {
        if (cart.length === 0) {
            return;
        }

        setRefreshing(true);

        try {
            const fresh = await Promise.all(
                cart.map(async (line) => {
                    try {
                        return await getProduct(line.product.id);
                    } catch (err) {
                        if (getApiStatus(err) === 404) {
                            return null;
                        }

                        throw err;
                    }
                })
            );

            setCart((current) =>
                current.flatMap((line) => {
                    const product = fresh.find(
                        (item) => item?.id === line.product.id
                    );

                    return product ? [{ ...line, product }] : [];
                })
            );
            toast.info("Prices and stock refreshed.");
        } catch (err) {
            toast.error(
                getApiErrorMessage(err, "Unable to refresh the products.")
            );
        } finally {
            setRefreshing(false);
        }
    };

    // ---- Submit ----
    const handleSubmit = async () => {
        if (submittingRef.current || !customer || !canSubmit) {
            return;
        }

        const payload = {
            customerId: customer.id,
            items: cart.map((line) => ({
                productId: line.product.id,
                quantity: line.quantity,
                unitPrice: line.product.sellingPrice,
                registerWarranty: line.registerWarranty,
                warrantyDurationValue: line.registerWarranty
                    ? Number(line.warrantyDurationValue)
                    : null,
                warrantyDurationUnit: line.registerWarranty
                    ? line.warrantyDurationUnit
                    : null,
                serialNumbers: line.serialText
                    .split(/[\n,]/)
                    .map((item) => item.trim())
                    .filter(Boolean),
            })),
            payments: paying
                ? [
                      {
                          method: payMethod,
                          amount: payAmountNumber,
                          reference: payReference.trim() || null,
                      },
                  ]
                : undefined,
            remarks: remarks.trim() || null,
        };

        const signature = JSON.stringify(payload);

        if (requestRef.current?.signature !== signature) {
            requestRef.current = { signature, id: newRequestId() };
        }

        submittingRef.current = true;
        setSubmitting(true);
        setFormError(null);

        try {
            const response = await createSale({
                ...payload,
                clientRequestId: requestRef.current.id,
            });

            if (response.isDuplicate) {
                toast.info(
                    `This sale was already saved as ${response.invoiceNumber}.`
                );
            } else {
                toast.success(`Invoice ${response.invoiceNumber} created.`);
            }

            navigate(`/sales/${response.saleId}`, { replace: true });
        } catch (err) {
            const status = getApiStatus(err);
            const message = getApiErrorMessage(
                err,
                "Unable to create the sale."
            );

            if (status === undefined) {
                setFormError(
                    "The server could not be reached, so it is not certain the sale was saved. It is safe to press Create Invoice again: the same sale will not be created twice."
                );
            } else {
                setFormError(message);
            }

            if (status === 409) {
                // Stock or price changed under us. Show the new values.
                void refreshCart();
            }
        } finally {
            submittingRef.current = false;
            setSubmitting(false);
        }
    };

    const customerHint = customer
        ? customer.state
            ? `Place of supply: ${customer.state}`
            : "No state saved. Your own state is assumed (CGST + SGST)."
        : null;

    return (
        <div className="data-page">
            <div className="data-page-header">
                <div>
                    <h2 className="page-title">New Sale</h2>
                    <p className="data-page-subtitle">
                        Prices come from the product catalogue. Tax and totals
                        are calculated by the server when you create the
                        invoice.
                    </p>
                </div>
            </div>

            <div className="pos-layout">
                <div>
                    <section className="data-card pos-panel">
                        <h5 className="pos-panel-title">1. Customer</h5>

                        {customer ? (
                            <div className="d-flex justify-content-between align-items-start gap-3">
                                <div>
                                    <strong>{customer.name}</strong>
                                    <div className="text-muted">
                                        {customer.mobile}
                                        {customer.gstin &&
                                            ` · GSTIN ${customer.gstin}`}
                                    </div>
                                    <div className="small text-muted">
                                        {customerHint}
                                    </div>
                                </div>

                                <button
                                    type="button"
                                    className="btn btn-sm btn-outline-secondary"
                                    onClick={() => {
                                        setCustomer(null);
                                        setCustomerQuery("");
                                    }}
                                    disabled={submitting}
                                >
                                    Change
                                </button>
                            </div>
                        ) : (
                            <div className="pos-results">
                                <label
                                    htmlFor="pos-customer"
                                    className="visually-hidden"
                                >
                                    Search customer
                                </label>
                                <div className="input-group">
                                    <span className="input-group-text">
                                        <i className="bi bi-person"></i>
                                    </span>
                                    <input
                                        id="pos-customer"
                                        type="search"
                                        className="form-control"
                                        placeholder="Search customer by name or mobile"
                                        autoComplete="off"
                                        value={customerQuery}
                                        onChange={(e) => {
                                            setCustomerQuery(e.target.value);
                                            setCustomerOpen(true);
                                        }}
                                        onFocus={() => setCustomerOpen(true)}
                                        onBlur={() => setCustomerOpen(false)}
                                        disabled={submitting || walkInBusy}
                                    />
                                    <button
                                        type="button"
                                        className="btn btn-outline-secondary"
                                        onClick={() => void selectWalkIn()}
                                        disabled={submitting || walkInBusy}
                                    >
                                        {walkInBusy ? "…" : "Walk-in"}
                                    </button>
                                </div>

                                {customerOpen && (
                                    <div
                                        className="pos-dropdown"
                                        onMouseDown={(e) => e.preventDefault()}
                                    >
                                        {customerOptions.failed ? (
                                            <div className="p-3 text-danger">
                                                Unable to search customers.
                                            </div>
                                        ) : customerOptions.items.length ===
                                          0 ? (
                                            <div className="p-3 text-muted">
                                                {customerOptions.loading
                                                    ? "Searching..."
                                                    : "No customers found. Add the customer on the Customers page first."}
                                            </div>
                                        ) : (
                                            customerOptions.items.map((item) => (
                                                <button
                                                    key={item.id}
                                                    type="button"
                                                    className="pos-option"
                                                    onClick={() => {
                                                        setCustomer(item);
                                                        setCustomerOpen(false);
                                                        setFormError(null);
                                                    }}
                                                >
                                                    <span>
                                                        <strong>
                                                            {item.name}
                                                        </strong>
                                                        <br />
                                                        <small>
                                                            {item.mobile}
                                                        </small>
                                                    </span>
                                                    <small>
                                                        {item.state ?? ""}
                                                    </small>
                                                </button>
                                            ))
                                        )}
                                    </div>
                                )}
                            </div>
                        )}
                    </section>

                    <section className="data-card pos-panel">
                        <div className="d-flex justify-content-between align-items-center mb-3">
                            <h5 className="pos-panel-title mb-0">
                                2. Products
                                {shopType === "Grocery" && (
                                    <span className="badge text-bg-success ms-2">
                                        Grocery POS
                                    </span>
                                )}
                            </h5>

                            {cart.length > 0 && (
                                <button
                                    type="button"
                                    className="btn btn-sm btn-outline-secondary"
                                    onClick={() => void refreshCart()}
                                    disabled={refreshing || submitting}
                                >
                                    <i className="bi bi-arrow-clockwise me-1"></i>
                                    Refresh prices
                                </button>
                            )}
                        </div>

                        <div className="input-group mb-2">
                            <span className="input-group-text">
                                <i className="bi bi-upc"></i>
                            </span>
                            <input
                                ref={barcodeRef}
                                id="pos-barcode"
                                type="text"
                                className="form-control"
                                placeholder="Scan barcode or SKU, then Enter"
                                autoComplete="off"
                                value={barcode}
                                onChange={(e) => setBarcode(e.target.value)}
                                onKeyDown={(e) => {
                                    if (e.key === "Enter") {
                                        e.preventDefault();
                                        void handleScan();
                                    }
                                }}
                                disabled={submitting || scanBusy}
                            />
                            <button
                                type="button"
                                className="btn btn-outline-primary"
                                onClick={() => void handleScan()}
                                disabled={submitting || scanBusy || !barcode.trim()}
                            >
                                {scanBusy ? "…" : "Add"}
                            </button>
                        </div>
                        <div className="form-text mb-3">
                            Scanning the same barcode again increases quantity by 1, up to available stock.
                            Quantity is a whole number (kg/litre packed items are billed as packets or pieces).
                        </div>

                        <div className="pos-results mb-3">
                            <label
                                htmlFor="pos-product"
                                className="visually-hidden"
                            >
                                Search product
                            </label>
                            <div className="input-group">
                                <span className="input-group-text">
                                    <i className="bi bi-upc-scan"></i>
                                </span>
                                <input
                                    id="pos-product"
                                    type="search"
                                    className="form-control"
                                    placeholder="Search product by name or SKU, press Enter to add"
                                    autoComplete="off"
                                    value={productQuery}
                                    onChange={(e) => {
                                        setProductQuery(e.target.value);
                                        setProductOpen(true);
                                    }}
                                    onFocus={() => setProductOpen(true)}
                                    onBlur={() => setProductOpen(false)}
                                    onKeyDown={(e) => {
                                        if (e.key !== "Enter") {
                                            return;
                                        }

                                        e.preventDefault();

                                        const query = productQuery
                                            .trim()
                                            .toLowerCase();
                                        const items = productOptions.items;
                                        const exact = items.find(
                                            (item) =>
                                                item.sku.toLowerCase() ===
                                                query
                                        );
                                        const pick =
                                            exact ??
                                            (items.length === 1
                                                ? items[0]
                                                : undefined);

                                        if (pick && !productOptions.loading) {
                                            addProduct(pick);
                                        }
                                    }}
                                />
                            </div>

                            {productOpen && (
                                <div
                                    className="pos-dropdown"
                                    onMouseDown={(e) => e.preventDefault()}
                                >
                                    {productOptions.failed ? (
                                        <div className="p-3 text-danger">
                                            Unable to search products.
                                        </div>
                                    ) : productOptions.items.length === 0 ? (
                                        <div className="p-3 text-muted">
                                            {productOptions.loading
                                                ? "Searching..."
                                                : "No products found."}
                                        </div>
                                    ) : (
                                        productOptions.items.map((item) => (
                                            <button
                                                key={item.id}
                                                type="button"
                                                className="pos-option"
                                                disabled={item.stock <= 0}
                                                onClick={() =>
                                                    addProduct(item)
                                                }
                                            >
                                                <span>
                                                    <strong>
                                                        {item.name}
                                                    </strong>
                                                    <br />
                                                    <small>
                                                        SKU {item.sku} ·{" "}
                                                        {item.stock > 0
                                                            ? `${item.stock} in stock`
                                                            : "Out of stock"}
                                                        {item.taxPercentage
                                                            ? ` · ${item.taxPercentage}% tax`
                                                            : ""}
                                                    </small>
                                                </span>
                                                <strong>
                                                    {formatCurrency(
                                                        item.sellingPrice
                                                    )}
                                                </strong>
                                            </button>
                                        ))
                                    )}
                                </div>
                            )}
                        </div>

                        {cart.length === 0 ? (
                            <div className="text-center text-muted py-4">
                                <i className="bi bi-cart3 fs-3 d-block mb-2"></i>
                                Search and select products to add them to the
                                invoice.
                            </div>
                        ) : (
                            <div className="table-responsive">
                                <table className="table align-middle mb-0">
                                    <thead>
                                        <tr>
                                            <th>Product</th>
                                            <th className="text-end">Price</th>
                                            <th>Qty</th>
                                            <th className="text-end">
                                                Amount
                                            </th>
                                            <th></th>
                                        </tr>
                                    </thead>
                                    <tbody>
                                        {cart.map((line, index) => (
                                            <tr key={line.product.id}>
                                                <td>
                                                    <strong>
                                                        {line.product.name}
                                                    </strong>
                                                    <div className="small text-muted">
                                                        {line.product.sku}
                                                        {line.product.barcode
                                                            ? ` · ${line.product.barcode}`
                                                            : ""}
                                                        {line.product.unitName
                                                            ? ` · ${line.product.unitName}`
                                                            : ""}
                                                        {` · stock ${line.product.stock}`}
                                                        {line.product
                                                            .taxPercentage
                                                            ? ` · ${line.product.taxPercentage}% tax`
                                                            : ""}
                                                    </div>
                                                    <div className="form-check mt-2">
                                                        <input
                                                            id={`wrn-${line.product.id}`}
                                                            type="checkbox"
                                                            className="form-check-input"
                                                            checked={line.registerWarranty}
                                                            onChange={(e) =>
                                                                setCart((current) =>
                                                                    current.map((item) =>
                                                                        item.product.id ===
                                                                        line.product.id
                                                                            ? {
                                                                                  ...item,
                                                                                  registerWarranty:
                                                                                      e.target.checked,
                                                                              }
                                                                            : item
                                                                    )
                                                                )
                                                            }
                                                        />
                                                        <label
                                                            htmlFor={`wrn-${line.product.id}`}
                                                            className="form-check-label small"
                                                        >
                                                            Register warranty
                                                        </label>
                                                    </div>
                                                    {line.registerWarranty && (
                                                        <div className="d-flex flex-wrap gap-2 mt-2">
                                                            <input
                                                                type="number"
                                                                min={1}
                                                                className="form-control form-control-sm"
                                                                style={{ width: 80 }}
                                                                aria-label="Warranty duration"
                                                                value={line.warrantyDurationValue}
                                                                onChange={(e) =>
                                                                    setCart((current) =>
                                                                        current.map((item) =>
                                                                            item.product.id ===
                                                                            line.product.id
                                                                                ? {
                                                                                      ...item,
                                                                                      warrantyDurationValue:
                                                                                          e.target.value,
                                                                                  }
                                                                                : item
                                                                        )
                                                                    )
                                                                }
                                                            />
                                                            <select
                                                                className="form-select form-select-sm"
                                                                style={{ width: 110 }}
                                                                aria-label="Warranty unit"
                                                                value={line.warrantyDurationUnit}
                                                                onChange={(e) =>
                                                                    setCart((current) =>
                                                                        current.map((item) =>
                                                                            item.product.id ===
                                                                            line.product.id
                                                                                ? {
                                                                                      ...item,
                                                                                      warrantyDurationUnit:
                                                                                          e.target.value as
                                                                                              | "Days"
                                                                                              | "Months"
                                                                                              | "Years",
                                                                                  }
                                                                                : item
                                                                        )
                                                                    )
                                                                }
                                                            >
                                                                <option value="Days">Days</option>
                                                                <option value="Months">Months</option>
                                                                <option value="Years">Years</option>
                                                            </select>
                                                            <input
                                                                type="text"
                                                                className="form-control form-control-sm"
                                                                placeholder="Serials (one per unit)"
                                                                aria-label="Serial numbers"
                                                                value={line.serialText}
                                                                onChange={(e) =>
                                                                    setCart((current) =>
                                                                        current.map((item) =>
                                                                            item.product.id ===
                                                                            line.product.id
                                                                                ? {
                                                                                      ...item,
                                                                                      serialText:
                                                                                          e.target.value,
                                                                                  }
                                                                                : item
                                                                        )
                                                                    )
                                                                }
                                                            />
                                                        </div>
                                                    )}
                                                    {lineProblems[index] && (
                                                        <div
                                                            className="small text-danger"
                                                            role="alert"
                                                        >
                                                            {lineProblems[index]}
                                                        </div>
                                                    )}
                                                </td>
                                                <td className="text-end text-nowrap">
                                                    {formatCurrency(
                                                        line.product
                                                            .sellingPrice
                                                    )}
                                                </td>
                                                <td>
                                                    <input
                                                        type="number"
                                                        min={1}
                                                        max={Math.max(
                                                            line.product.stock,
                                                            1
                                                        )}
                                                        step={1}
                                                        className={`form-control form-control-sm pos-qty${
                                                            lineProblems[index]
                                                                ? " is-invalid"
                                                                : ""
                                                        }`}
                                                        aria-label={`Quantity of ${line.product.name}`}
                                                        value={line.quantity}
                                                        onChange={(e) =>
                                                            setQuantity(
                                                                line.product
                                                                    .id,
                                                                e.target.value
                                                            )
                                                        }
                                                    />
                                                </td>
                                                <td className="text-end text-nowrap">
                                                    {formatCurrency(
                                                        lineEstimates[index]
                                                            .taxable
                                                    )}
                                                </td>
                                                <td className="text-end">
                                                    <button
                                                        type="button"
                                                        className="btn btn-sm btn-outline-danger"
                                                        aria-label={`Remove ${line.product.name}`}
                                                        onClick={() =>
                                                            removeLine(
                                                                line.product.id
                                                            )
                                                        }
                                                    >
                                                        <i className="bi bi-x-lg"></i>
                                                    </button>
                                                </td>
                                            </tr>
                                        ))}
                                    </tbody>
                                </table>
                            </div>
                        )}
                    </section>
                </div>

                <aside className="pos-sticky">
                    <section className="data-card pos-panel">
                        <h5 className="pos-panel-title">Summary</h5>

                        <dl className="pos-totals row mb-0">
                            <dt className="col-6">Subtotal</dt>
                            <dd className="col-6">
                                {formatCurrency(subtotal)}
                            </dd>

                            <dt className="col-6">Tax (estimate)</dt>
                            <dd className="col-6">{formatCurrency(tax)}</dd>

                            {roundOff !== 0 && (
                                <>
                                    <dt className="col-6">Round off</dt>
                                    <dd className="col-6">
                                        {roundOff > 0 ? "+" : "−"}
                                        {formatCurrency(Math.abs(roundOff))}
                                    </dd>
                                </>
                            )}

                            <dt className="col-6 pos-grand">Total</dt>
                            <dd className="col-6 pos-grand">
                                {formatCurrency(grandTotal)}
                            </dd>
                        </dl>

                        <p className="small text-muted mt-2 mb-0">
                            The final tax split (CGST + SGST or IGST) and
                            total are fixed by the server on the invoice.
                        </p>
                    </section>

                    <section className="data-card pos-panel">
                        <h5 className="pos-panel-title">
                            3. Payment received now
                        </h5>

                        <div className="row g-2">
                            <div className="col-12">
                                <label
                                    htmlFor="pos-pay-amount"
                                    className="form-label"
                                >
                                    Amount
                                </label>
                                <div className="input-group">
                                    <input
                                        id="pos-pay-amount"
                                        type="number"
                                        inputMode="decimal"
                                        min="0"
                                        step="0.01"
                                        placeholder="0.00 (leave empty if unpaid)"
                                        className={`form-control${
                                            payError ? " is-invalid" : ""
                                        }`}
                                        value={payAmount}
                                        onChange={(e) =>
                                            setPayAmount(e.target.value)
                                        }
                                    />
                                    <button
                                        type="button"
                                        className="btn btn-outline-secondary"
                                        disabled={grandTotal <= 0}
                                        onClick={() =>
                                            setPayAmount(grandTotal.toFixed(2))
                                        }
                                    >
                                        Full
                                    </button>
                                    {payError && (
                                        <div
                                            className="invalid-feedback"
                                            role="alert"
                                        >
                                            {payError}
                                        </div>
                                    )}
                                </div>
                            </div>

                            <div className="col-6">
                                <label
                                    htmlFor="pos-pay-method"
                                    className="form-label"
                                >
                                    Method
                                </label>
                                <select
                                    id="pos-pay-method"
                                    className="form-select"
                                    value={payMethod}
                                    onChange={(e) =>
                                        setPayMethod(
                                            e.target.value as PaymentMethod
                                        )
                                    }
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

                            <div className="col-6">
                                <label
                                    htmlFor="pos-pay-ref"
                                    className="form-label"
                                >
                                    Reference
                                </label>
                                <input
                                    id="pos-pay-ref"
                                    type="text"
                                    maxLength={100}
                                    autoComplete="off"
                                    className="form-control"
                                    value={payReference}
                                    onChange={(e) =>
                                        setPayReference(e.target.value)
                                    }
                                />
                            </div>

                            <div className="col-12">
                                <label
                                    htmlFor="pos-remarks"
                                    className="form-label"
                                >
                                    Remarks
                                </label>
                                <input
                                    id="pos-remarks"
                                    type="text"
                                    maxLength={500}
                                    autoComplete="off"
                                    className="form-control"
                                    value={remarks}
                                    onChange={(e) =>
                                        setRemarks(e.target.value)
                                    }
                                />
                            </div>
                        </div>

                        {formError && (
                            <div
                                className="alert alert-danger mt-3 mb-0"
                                role="alert"
                            >
                                {formError}
                            </div>
                        )}

                        <button
                            type="button"
                            className="btn btn-primary w-100 mt-3"
                            disabled={!canSubmit}
                            onClick={() => void handleSubmit()}
                        >
                            {submitting && (
                                <span
                                    className="spinner-border spinner-border-sm me-2"
                                    aria-hidden="true"
                                ></span>
                            )}
                            Create Invoice
                        </button>

                        {!customer && (
                            <p className="small text-muted text-center mt-2 mb-0">
                                Select a customer to continue.
                            </p>
                        )}
                    </section>
                </aside>
            </div>
        </div>
    );
}

export default NewSalePage;