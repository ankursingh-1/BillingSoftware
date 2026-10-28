import { useRef, useState } from "react";
import { toast } from "react-toastify";

import Modal from "../common/Modal";

import {
    downloadProductImportErrors,
    downloadProductTemplate,
    importProducts,
    previewProductImport,
} from "../../services/products/productService";
import { getApiErrorMessage } from "../../utils/apiError";

import type {
    ProductImportPreview,
    ProductImportResult,
} from "../../interfaces/product/product";

type Step = "upload" | "preview" | "done";

interface ProductImportModalProps {
    onClose: () => void;
    onImported: () => void;
}

function ProductImportModal({ onClose, onImported }: ProductImportModalProps) {
    const inputRef = useRef<HTMLInputElement>(null);
    const abortRef = useRef<AbortController | null>(null);

    const [step, setStep] = useState<Step>("upload");
    const [file, setFile] = useState<File | null>(null);
    const [preview, setPreview] = useState<ProductImportPreview | null>(null);
    const [result, setResult] = useState<ProductImportResult | null>(null);
    const [busy, setBusy] = useState(false);
    const [busyLabel, setBusyLabel] = useState("");

    const cancelInFlight = () => {
        abortRef.current?.abort();
        abortRef.current = null;
    };

    const handleClose = () => {
        if (busy) {
            cancelInFlight();
        }

        onClose();
    };

    const run = async (
        label: string,
        work: (signal: AbortSignal) => Promise<void>
    ) => {
        cancelInFlight();
        const controller = new AbortController();
        abortRef.current = controller;
        setBusy(true);
        setBusyLabel(label);

        try {
            await work(controller.signal);
        } catch (error) {
            if ((error as { code?: string }).code === "ERR_CANCELED") {
                return;
            }

            toast.error(getApiErrorMessage(error, "Unable to process the file."));
        } finally {
            if (abortRef.current === controller) {
                abortRef.current = null;
                setBusy(false);
                setBusyLabel("");
            }
        }
    };

    const handleFile = (chosen: File | undefined) => {
        if (!chosen) {
            return;
        }

        if (!chosen.name.toLowerCase().endsWith(".xlsx")) {
            toast.error("Upload an .xlsx Excel file.");
            return;
        }

        setFile(chosen);
        setPreview(null);
        setResult(null);
        setStep("upload");
    };

    const handlePreview = () => {
        if (!file) {
            toast.error("Choose an .xlsx file first.");
            return;
        }

        void run("Checking the file...", async (signal) => {
            const data = await previewProductImport(file, signal);
            setPreview(data);
            setStep("preview");
        });
    };

    const handleImport = () => {
        if (!file || !preview?.canImport) {
            return;
        }

        void run("Importing products...", async (signal) => {
            const data = await importProducts(file, signal);
            setResult(data);
            setStep("done");
            onImported();
            toast.success(
                `${data.imported} product${data.imported === 1 ? "" : "s"} imported.`
            );
        });
    };

    const flagged =
        (result ?? preview)?.rows.filter(
            (row) => row.status !== "Valid" && row.status !== "Imported"
        ) ?? [];

    return (
        <Modal
            show
            size="xl"
            title="Import products from Excel"
            busy={busy}
            onClose={handleClose}
            footer={
                <>
                    {busy && (
                        <button
                            type="button"
                            className="btn btn-outline-secondary me-auto"
                            onClick={cancelInFlight}
                        >
                            Cancel
                        </button>
                    )}
                    <button
                        type="button"
                        className="btn btn-outline-secondary"
                        onClick={handleClose}
                        disabled={busy}
                    >
                        Close
                    </button>
                    {step === "upload" && (
                        <button
                            type="button"
                            className="btn btn-primary"
                            disabled={busy || !file}
                            onClick={handlePreview}
                        >
                            {busy ? busyLabel : "Check file"}
                        </button>
                    )}
                    {step === "preview" && (
                        <button
                            type="button"
                            className="btn btn-primary"
                            disabled={busy || !preview?.canImport}
                            onClick={handleImport}
                        >
                            {busy
                                ? busyLabel
                                : `Import ${preview?.valid ?? 0} product${
                                      preview?.valid === 1 ? "" : "s"
                                  }`}
                        </button>
                    )}
                </>
            }
        >
            <ol className="small text-muted">
                <li>Download the template and fill the Products sheet.</li>
                <li>Upload the .xlsx file.</li>
                <li>Review valid, duplicate and invalid rows.</li>
                <li>Confirm. Valid rows are saved together (all or none).</li>
            </ol>

            <div className="d-flex flex-wrap gap-2 mb-3">
                <button
                    type="button"
                    className="btn btn-outline-primary btn-sm"
                    disabled={busy}
                    onClick={() =>
                        void downloadProductTemplate().catch((error) =>
                            toast.error(
                                getApiErrorMessage(
                                    error,
                                    "Unable to download the template."
                                )
                            )
                        )
                    }
                >
                    Download template
                </button>
                {file && flagged.length > 0 && (
                    <button
                        type="button"
                        className="btn btn-outline-secondary btn-sm"
                        disabled={busy}
                        onClick={() =>
                            void downloadProductImportErrors(file).catch(
                                (error) =>
                                    toast.error(
                                        getApiErrorMessage(
                                            error,
                                            "Unable to download the error report."
                                        )
                                    )
                            )
                        }
                    >
                        Download error report
                    </button>
                )}
            </div>

            {step !== "done" && (
                <>
                    <input
                        ref={inputRef}
                        type="file"
                        accept=".xlsx,application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
                        className="d-none"
                        onChange={(e) => {
                            handleFile(e.target.files?.[0]);
                            e.target.value = "";
                        }}
                    />
                    <button
                        type="button"
                        className="btn btn-outline-secondary"
                        disabled={busy}
                        onClick={() => inputRef.current?.click()}
                    >
                        {file ? file.name : "Choose .xlsx file"}
                    </button>
                </>
            )}

            {busy && (
                <div className="d-flex align-items-center gap-2 mt-3">
                    <div
                        className="spinner-border spinner-border-sm text-primary"
                        role="status"
                    >
                        <span className="visually-hidden">{busyLabel}</span>
                    </div>
                    <span>{busyLabel}</span>
                </div>
            )}

            {(preview || result) && (
                <div className="mt-3">
                    <div className="row g-2 mb-3">
                        <Summary
                            label="Rows"
                            value={(result ?? preview)!.totalRows}
                        />
                        <Summary
                            label={result ? "Imported" : "Valid"}
                            value={result ? result.imported : preview!.valid}
                            tone="success"
                        />
                        <Summary
                            label="Duplicates skipped"
                            value={(result ?? preview)!.duplicate}
                            tone="warning"
                        />
                        <Summary
                            label="Invalid"
                            value={(result ?? preview)!.invalid}
                            tone="danger"
                        />
                    </div>

                    <p className="small text-muted">{(result ?? preview)!.policy}</p>

                    {flagged.length > 0 && (
                        <div className="table-responsive" style={{ maxHeight: 280 }}>
                            <table className="table table-sm align-middle mb-0">
                                <thead>
                                    <tr>
                                        <th>Row</th>
                                        <th>Name</th>
                                        <th>SKU</th>
                                        <th>Status</th>
                                        <th>Error</th>
                                    </tr>
                                </thead>
                                <tbody>
                                    {flagged.map((row) => (
                                        <tr key={row.excelRow}>
                                            <td>{row.excelRow}</td>
                                            <td>{row.name || "—"}</td>
                                            <td>{row.sku || "—"}</td>
                                            <td>{row.status}</td>
                                            <td>{row.error}</td>
                                        </tr>
                                    ))}
                                </tbody>
                            </table>
                        </div>
                    )}

                    {step === "preview" && !preview?.canImport && (
                        <div className="alert alert-warning mt-3 mb-0">
                            Nothing to import. Fix the rows above or skip the
                            duplicates; existing products are never overwritten.
                        </div>
                    )}

                    {step === "done" && result && (
                        <div className="alert alert-success mt-3 mb-0">
                            Imported {result.imported}. Skipped {result.skipped}{" "}
                            (duplicates and invalid rows). Retrying the same
                            file will skip SKUs that already exist, so stock is
                            not added twice.
                        </div>
                    )}
                </div>
            )}
        </Modal>
    );
}

function Summary({
    label,
    value,
    tone,
}: {
    label: string;
    value: number;
    tone?: "success" | "warning" | "danger";
}) {
    const cls =
        tone === "success"
            ? "text-bg-success"
            : tone === "warning"
              ? "text-bg-warning"
              : tone === "danger"
                ? "text-bg-danger"
                : "text-bg-light";

    return (
        <div className="col-6 col-md-3">
            <div className={`rounded p-2 ${cls}`}>
                <div className="small">{label}</div>
                <div className="fs-5 fw-semibold">{value}</div>
            </div>
        </div>
    );
}

export default ProductImportModal;
