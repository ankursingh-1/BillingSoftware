import { useEffect, useState } from "react";
import { Link, useParams } from "react-router-dom";

import ProductForm from "../../components/forms/ProductForm";
import type { ProductFormLookups } from "../../components/forms/ProductForm";

import { getCategories } from "../../services/categories/categoryService";
import {
    getBrands,
    getTaxes,
    getUnits,
} from "../../services/lookups/lookupService";
import { getProduct } from "../../services/products/productService";
import { getApiErrorMessage, getApiStatus } from "../../utils/apiError";

import type { Product } from "../../interfaces/product/product";

import "../../styles/data-pages.css";

type LoadState =
    | { status: "loading" }
    | { status: "notFound" }
    | { status: "error"; message: string }
    | {
          status: "ready";
          product: Product | null;
          lookups: ProductFormLookups;
      };

function ProductFormPage() {
    const { id } = useParams();
    const productId = id === undefined ? null : Number(id);
    const isEdit = productId !== null;
    const invalidId =
        isEdit && (!Number.isInteger(productId) || productId <= 0);

    const [state, setState] = useState<LoadState>({ status: "loading" });
    const [reloadKey, setReloadKey] = useState(0);

    useEffect(() => {
        if (invalidId) {
            return;
        }

        let cancelled = false;

        const load = async () => {
            setState({ status: "loading" });

            try {
                const [categories, brands, units, taxes, product] =
                    await Promise.all([
                        getCategories(),
                        getBrands(),
                        getUnits(),
                        getTaxes(),
                        productId === null
                            ? Promise.resolve(null)
                            : getProduct(productId),
                    ]);

                if (!cancelled) {
                    setState({
                        status: "ready",
                        product,
                        lookups: { categories, brands, units, taxes },
                    });
                }
            } catch (err) {
                if (cancelled) {
                    return;
                }

                if (getApiStatus(err) === 404) {
                    setState({ status: "notFound" });
                } else {
                    setState({
                        status: "error",
                        message: getApiErrorMessage(
                            err,
                            "Unable to load the product form."
                        ),
                    });
                }
            }
        };

        void load();

        return () => {
            cancelled = true;
        };
    }, [productId, invalidId, reloadKey]);

    const title = isEdit ? "Edit Product" : "Add Product";

    return (
        <div className="data-page data-page-narrow">
            <div className="data-page-header">
                <div>
                    <h2 className="page-title">{title}</h2>
                    <p className="data-page-subtitle">
                        {isEdit
                            ? "Update product details and pricing"
                            : "Create a new product"}
                    </p>
                </div>

                <Link to="/products" className="btn btn-outline-secondary">
                    <i className="bi bi-arrow-left me-2"></i>
                    Back to Products
                </Link>
            </div>

            {invalidId || state.status === "notFound" ? (
                <div className="data-card state-block" role="alert">
                    <i className="bi bi-question-circle state-icon"></i>
                    <p>This product was not found. It may have been deleted.</p>
                    <Link to="/products" className="btn btn-primary btn-sm">
                        Back to Products
                    </Link>
                </div>
            ) : state.status === "loading" ? (
                <div className="data-card state-block">
                    <div className="spinner-border text-primary" role="status">
                        <span className="visually-hidden">Loading...</span>
                    </div>
                    <p>Loading...</p>
                </div>
            ) : state.status === "error" ? (
                <div className="data-card state-block" role="alert">
                    <i className="bi bi-exclamation-octagon state-icon text-danger"></i>
                    <p>{state.message}</p>
                    <button
                        type="button"
                        className="btn btn-outline-primary btn-sm"
                        onClick={() => setReloadKey((key) => key + 1)}
                    >
                        <i className="bi bi-arrow-clockwise me-1"></i>
                        Retry
                    </button>
                </div>
            ) : (
                <ProductForm
                    key={state.product?.id ?? "new"}
                    product={state.product}
                    lookups={state.lookups}
                />
            )}
        </div>
    );
}

export default ProductFormPage;
