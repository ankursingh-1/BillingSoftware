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
    ProductImportRow,
    ProductImportStatus,
} from "../../interfaces/product/product";

type Step = "upload" | "preview" | "done";

interface ProductImportModalProps {
    onClose: () => void;
    onImported: () => void;
}

const ERROR_STATUSES: ProductImportStatus[] = [
    "Invalid",
    "Duplicate",
    "Conflict",
    "Failed",
];

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
            const applied = data.added + data.updated + data.stockIncreased;
            toast.success(
                data.replayed
                    ? "This file was imported before. Stock was not added again."
                    : `${applied} change${applied === 1 ? "" : "s"} applied.`
            );
        });
    };

    const summary = result ?? preview;
    const flagged =
        summary?.rows.filter((row) => ERROR_STATUSES.includes(row.status)) ?? [];
    const reviewRows = summary?.rows ?? [];

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
                                : `Import ${preview?.valid ?? 0} row${
                                      preview?.valid === 1 ? "" : "s"
                                  }`}
                        </button>
                    )}
                </>
            }
        >
            <ol className="small text-muted">
                <li>Download the template and fill the Products sheet.</li>
                <li>
                    An existing barcode or SKU is a match: that product is
                    updated. Duplicate means the same product appears twice in
                    this file, not that it already exists in the catalogue.
                </li>
                <li>
                    Use <code>OpeningStock</code> as the desired on-hand for
                    existing products. Only the difference is added. Equal
                    quantities skip stock. Blank is not zero. Excel never
                    reduces stock — use Inventory → Adjustments.
                </li>
                <li>Upload the .xlsx file and review the preview.</li>
                <li>Confirm. Applicable rows are saved together (all or none).</li>
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

            {summary && (
                <div className="mt-3">
                    <div className="row g-2 mb-3">
                        <Summary label="Rows" value={summary.totalRows} />
                        <Summary
                            label={result ? "Added" : "Will apply"}
                            value={result ? result.added : summary.valid}
                            tone="success"
                        />
                        {result && (
                            <>
                                <Summary
                                    label="Updated"
                                    value={result.updated}
                                    tone="success"
                                />
                                <Summary
                                    label="Stock increases"
                                    value={result.stockIncreased}
                                    tone="success"
                                />
                            </>
                        )}
                        <Summary
                            label="Unchanged"
                            value={summary.unchanged ?? 0}
                        />
                        <Summary
                            label="Duplicate rows in file"
                            value={summary.duplicate}
                            tone="warning"
                        />
                        <Summary
                            label="Conflicts"
                            value={summary.conflict ?? 0}
                            tone="warning"
                        />
                        <Summary
                            label="Invalid"
                            value={summary.invalid}
                            tone="danger"
                        />
                        {result && (
                            <Summary
                                label="Failed"
                                value={
                                    result.rows.filter((row) => row.status === "Failed")
                                        .length
                                }
                                tone="danger"
                            />
                        )}
                    </div>

                    <p className="small text-muted">{summary.policy}</p>

                    {flagged.length > 0 && (
                        <div className="alert alert-warning py-2">
                            <div className="fw-semibold mb-1">
                                {flagged.length} row
                                {flagged.length === 1 ? "" : "s"} need review
                            </div>
                            <ul className="mb-0 small">
                                {flagged.map((row) => (
                                    <li key={row.excelRow}>
                                        Row {row.excelRow}{" "}
                                        {row.name || "—"} ({row.sku || "no SKU"}):{" "}
                                        {row.error || displayStatus(row)}
                                    </li>
                                ))}
                            </ul>
                        </div>
                    )}

                    {reviewRows.length > 0 && (
                        <div className="table-responsive" style={{ maxHeight: 320 }}>
                            <table className="table table-sm align-middle mb-0">
                                <thead>
                                    <tr>
                                        <th>Row</th>
                                        <th>Name</th>
                                        <th>SKU</th>
                                        <th>On hand</th>
                                        <th>Excel qty</th>
                                        <th>Change</th>
                                        <th>Action</th>
                                        <th>Status</th>
                                        <th>Detail</th>
                                    </tr>
                                </thead>
                                <tbody>
                                    {reviewRows.map((row) => (
                                        <tr
                                            key={row.excelRow}
                                            className={
                                                ERROR_STATUSES.includes(row.status)
                                                    ? "table-warning"
                                                    : undefined
                                            }
                                        >
                                            <td>{row.excelRow}</td>
                                            <td>{row.name || "—"}</td>
                                            <td>{row.sku || "—"}</td>
                                            <td>{formatStock(row.currentStock)}</td>
                                            <td>{excelQty(row)}</td>
                                            <td>
                                                {row.proposedStockChange
                                                    ? `+${row.proposedStockChange}`
                                                    : "—"}
                                            </td>
                                            <td>{row.action || row.fieldChanges || "—"}</td>
                                            <td>{displayStatus(row)}</td>
                                            <td>{row.error || "—"}</td>
                                        </tr>
                                    ))}
                                </tbody>
                            </table>
                        </div>
                    )}

                    {step === "preview" && !preview?.canImport && (
                        <div className="alert alert-warning mt-3 mb-0">
                            Nothing to import. Unchanged rows are skipped, not
                            errors. Fix the rows listed above, or there is no
                            product/stock change in this file.
                        </div>
                    )}

                    {step === "done" && result && (
                        <div className="alert alert-success mt-3 mb-0">
                            {result.replayed
                                ? "This exact file was imported before. The original result is shown; stock was not added twice."
                                : `Added ${result.added}, updated ${result.updated}, stock increases ${result.stockIncreased}. Skipped ${result.skipped} (unchanged, duplicates, conflicts, invalid).`}
                        </div>
                    )}
                </div>
            )}
        </Modal>
    );
}

function displayStatus(row: ProductImportRow): string {
    switch (row.status) {
        case "Valid":
            return "Ready to import";
        case "Unchanged":
            return "Unchanged / skipped";
        case "Duplicate":
            return "Duplicate row in file";
        case "Conflict":
            return "Conflict / needs review";
        case "StockIncreased":
            return "Stock increased";
        case "Added":
            return "Added";
        case "Updated":
            return "Updated";
        default:
            return row.status;
    }
}

function excelQty(row: ProductImportRow): string {
    if (row.addStockQuantity != null) {
        return `add ${row.addStockQuantity}`;
    }

    if (row.stockQuantity != null) {
        return String(row.stockQuantity);
    }

    return "blank";
}

function formatStock(value: number | null | undefined): string {
    return value == null ? "—" : String(value);
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
