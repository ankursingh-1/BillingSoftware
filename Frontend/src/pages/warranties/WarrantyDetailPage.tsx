import { useCallback, useEffect, useState } from "react";
import { Link, useParams } from "react-router-dom";
import { toast } from "react-toastify";

import { getProduct, searchProducts } from "../../services/products/productService";
import {
    getWarranty,
    submitWarrantyClaim,
    updateWarrantyClaim,
} from "../../services/warranty/warrantyService";
import { getApiErrorMessage } from "../../utils/apiError";
import { formatDate } from "../../utils/format";

import type { Product } from "../../interfaces/product/product";
import type {
    WarrantyClaimStatus,
    WarrantyDetail,
} from "../../interfaces/warranty/warranty";

import "../../styles/data-pages.css";

const NEXT: Record<WarrantyClaimStatus, WarrantyClaimStatus[]> = {
    Submitted: ["UnderInspection", "Rejected", "Closed"],
    UnderInspection: ["Approved", "Rejected", "Closed"],
    Approved: ["Repaired", "ReplacementIssued", "Closed"],
    Rejected: ["Closed"],
    Repaired: ["Closed"],
    ReplacementIssued: ["Closed"],
    Closed: [],
};

function WarrantyDetailPage() {
    const { id } = useParams();
    const warrantyId = Number(id);
    const [warranty, setWarranty] = useState<WarrantyDetail | null>(null);
    const [loading, setLoading] = useState(true);
    const [problem, setProblem] = useState("");
    const [notes, setNotes] = useState("");
    const [reason, setReason] = useState("");
    const [inspection, setInspection] = useState("");
    const [replacementSku, setReplacementSku] = useState("");
    const [replacementSerial, setReplacementSerial] = useState("");
    const [busy, setBusy] = useState(false);

    const load = useCallback(async () => {
        if (!Number.isInteger(warrantyId) || warrantyId < 1) {
            return;
        }

        setLoading(true);
        try {
            setWarranty(await getWarranty(warrantyId));
        } catch (error) {
            toast.error(getApiErrorMessage(error, "Unable to load the warranty."));
            setWarranty(null);
        } finally {
            setLoading(false);
        }
    }, [warrantyId]);

    useEffect(() => {
        // eslint-disable-next-line react-hooks/set-state-in-effect
        void load();
    }, [load]);

    const submitClaim = async () => {
        if (!warranty) return;
        setBusy(true);
        try {
            await submitWarrantyClaim(warranty.id, problem, notes || undefined);
            toast.success("Claim submitted.");
            setProblem("");
            setNotes("");
            await load();
        } catch (error) {
            toast.error(getApiErrorMessage(error, "Unable to submit the claim."));
        } finally {
            setBusy(false);
        }
    };

    const moveClaim = async (claimId: number, status: WarrantyClaimStatus) => {
        if (!warranty) return;
        setBusy(true);
        try {
            let replacementProductId: number | undefined;
            if (status === "ReplacementIssued") {
                const sku = replacementSku.trim();
                if (sku) {
                    const found = await searchProducts({
                        search: sku,
                        pageNumber: 1,
                        pageSize: 5,
                    });
                    const match =
                        found.data.find((p) => p.sku.toLowerCase() === sku.toLowerCase()) ??
                        found.data[0];
                    if (!match) {
                        toast.error("Replacement product not found.");
                        setBusy(false);
                        return;
                    }
                    replacementProductId = match.id;
                } else {
                    replacementProductId = warranty.productId;
                }
                const product: Product = await getProduct(replacementProductId);
                if (product.stock < 1) {
                    toast.error(`Insufficient stock for '${product.name}'.`);
                    setBusy(false);
                    return;
                }
            }

            await updateWarrantyClaim(claimId, {
                status,
                inspectionNotes: inspection || undefined,
                decisionReason: reason || undefined,
                notes: notes || undefined,
                replacementProductId,
                replacementSerial: replacementSerial || undefined,
            });
            toast.success(`Claim moved to ${status}.`);
            await load();
        } catch (error) {
            toast.error(getApiErrorMessage(error, "Unable to update the claim."));
        } finally {
            setBusy(false);
        }
    };

    if (loading) {
        return <p className="text-muted">Loading warranty…</p>;
    }

    if (!warranty) {
        return <p className="text-muted">Warranty not found.</p>;
    }

    const openClaim = warranty.claims.find(
        (c) => c.status !== "Closed" && c.status !== "Rejected"
    );

    return (
        <>
            <div className="page-header">
                <div>
                    <h2 className="page-title">{warranty.warrantyNumber}</h2>
                    <p className="text-muted mb-0">
                        Invoice{" "}
                        <Link to={`/sales/${warranty.saleId}`}>
                            {warranty.invoiceNumber}
                        </Link>
                    </p>
                </div>
                <Link to="/warranties" className="btn btn-outline-secondary">
                    Back to list
                </Link>
            </div>

            <div className="row g-3">
                <div className="col-lg-6">
                    <section className="data-card">
                        <h5>Customer &amp; product</h5>
                        <dl className="row mb-0">
                            <dt className="col-sm-4">Customer</dt>
                            <dd className="col-sm-8">
                                {warranty.customerName}
                                <div className="small text-muted">
                                    {warranty.customerMobile}
                                </div>
                            </dd>
                            <dt className="col-sm-4">Product</dt>
                            <dd className="col-sm-8">
                                {warranty.productName}
                                <div className="small text-muted">
                                    {warranty.productSku}
                                    {warranty.serialNumber
                                        ? ` · SN ${warranty.serialNumber}`
                                        : ""}
                                </div>
                            </dd>
                            <dt className="col-sm-4">Duration</dt>
                            <dd className="col-sm-8">{warranty.duration}</dd>
                            <dt className="col-sm-4">Start</dt>
                            <dd className="col-sm-8">{formatDate(warranty.startDate)}</dd>
                            <dt className="col-sm-4">End</dt>
                            <dd className="col-sm-8">{formatDate(warranty.endDate)}</dd>
                            <dt className="col-sm-4">Days left</dt>
                            <dd className="col-sm-8">{warranty.daysRemaining}</dd>
                            <dt className="col-sm-4">Status</dt>
                            <dd className="col-sm-8">{warranty.status}</dd>
                        </dl>
                    </section>
                </div>
                <div className="col-lg-6">
                    <section className="data-card">
                        <h5>Eligibility</h5>
                        <p>
                            {warranty.eligible ? (
                                <span className="badge text-bg-success">Eligible by date</span>
                            ) : (
                                <span className="badge text-bg-secondary">Not eligible</span>
                            )}
                        </p>
                        <p className="mb-0">{warranty.eligibilityNote}</p>
                    </section>
                </div>
            </div>

            <section className="data-card mt-3">
                <h5>Claims</h5>
                {warranty.claims.length === 0 ? (
                    <p className="text-muted">No claims yet.</p>
                ) : (
                    warranty.claims.map((claim) => (
                        <div key={claim.id} className="border rounded p-3 mb-3">
                            <div className="d-flex justify-content-between">
                                <strong>{claim.claimNumber}</strong>
                                <span className="badge text-bg-primary">{claim.status}</span>
                            </div>
                            <p className="mb-1">{claim.reportedProblem}</p>
                            {claim.inspectionNotes && (
                                <p className="small mb-1">Inspection: {claim.inspectionNotes}</p>
                            )}
                            {claim.decisionReason && (
                                <p className="small mb-1">Decision: {claim.decisionReason}</p>
                            )}
                            {claim.replacementProductName && (
                                <p className="small mb-1">
                                    Replacement: {claim.replacementProductName}
                                    {claim.replacementSerial
                                        ? ` · SN ${claim.replacementSerial}`
                                        : ""}
                                    {claim.replacementStockMoved
                                        ? " · stock deducted"
                                        : ""}
                                </p>
                            )}
                            <ul className="small text-muted mb-2">
                                {claim.events.map((event, index) => (
                                    <li key={`${claim.id}-${index}`}>
                                        {event.status}
                                        {event.notes ? ` — ${event.notes}` : ""}
                                        {event.by ? ` (${event.by})` : ""}
                                    </li>
                                ))}
                            </ul>
                            {NEXT[claim.status].length > 0 && (
                                <div className="d-flex flex-wrap gap-2">
                                    {NEXT[claim.status].map((next) => (
                                        <button
                                            key={next}
                                            type="button"
                                            className="btn btn-sm btn-outline-primary"
                                            disabled={busy}
                                            onClick={() => void moveClaim(claim.id, next)}
                                        >
                                            {next}
                                        </button>
                                    ))}
                                </div>
                            )}
                        </div>
                    ))
                )}

                {warranty.eligible && !openClaim && (
                    <div className="mt-3">
                        <h6>Submit a claim</h6>
                        <textarea
                            className="form-control mb-2"
                            rows={3}
                            placeholder="Reported problem"
                            value={problem}
                            onChange={(e) => setProblem(e.target.value)}
                        />
                        <button
                            type="button"
                            className="btn btn-primary"
                            disabled={busy || problem.trim().length === 0}
                            onClick={() => void submitClaim()}
                        >
                            Submit claim
                        </button>
                    </div>
                )}

                {openClaim && (
                    <div className="row g-2 mt-2">
                        <div className="col-md-6">
                            <label className="form-label">Inspection notes</label>
                            <textarea
                                className="form-control"
                                rows={2}
                                value={inspection}
                                onChange={(e) => setInspection(e.target.value)}
                            />
                        </div>
                        <div className="col-md-6">
                            <label className="form-label">Decision reason</label>
                            <textarea
                                className="form-control"
                                rows={2}
                                value={reason}
                                onChange={(e) => setReason(e.target.value)}
                            />
                        </div>
                        <div className="col-md-6">
                            <label className="form-label">
                                Replacement SKU (blank = original product)
                            </label>
                            <input
                                className="form-control"
                                value={replacementSku}
                                onChange={(e) => setReplacementSku(e.target.value)}
                            />
                        </div>
                        <div className="col-md-6">
                            <label className="form-label">Replacement serial</label>
                            <input
                                className="form-control"
                                value={replacementSerial}
                                onChange={(e) => setReplacementSerial(e.target.value)}
                            />
                        </div>
                    </div>
                )}
            </section>
        </>
    );
}

export default WarrantyDetailPage;
