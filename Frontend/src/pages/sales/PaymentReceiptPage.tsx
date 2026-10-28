import { useEffect, useState } from "react";
import { Link, useParams } from "react-router-dom";
import { toast } from "react-toastify";

import ReceiptView from "../../components/invoices/ReceiptView";

import { usePrintPage } from "../../hooks/usePrintPage";
import {
    downloadReceiptPdf,
    getPaymentReceipt,
} from "../../services/sales/salesService";
import { getApiErrorMessage, getApiStatus } from "../../utils/apiError";

import type { InvoiceFormat } from "../../interfaces/company/company";
import type { PaymentReceipt } from "../../interfaces/sale/receipt";

import "../../styles/data-pages.css";
import "../../styles/sales.css";
import "../../styles/invoice-print.css";

function PaymentReceiptPage() {
    const { id, paymentId } = useParams();
    const saleId = Number(id);
    const payId = Number(paymentId);
    const valid =
        Number.isInteger(saleId) &&
        saleId > 0 &&
        Number.isInteger(payId) &&
        payId > 0;

    const [receipt, setReceipt] = useState<PaymentReceipt | null>(null);
    const [loading, setLoading] = useState(valid);
    const [error, setError] = useState<{
        message: string;
        notFound: boolean;
    } | null>(null);
    const [format, setFormat] = useState<InvoiceFormat>("A4");
    const [thermalWidth, setThermalWidth] = useState<58 | 80>(80);
    const [downloading, setDownloading] = useState(false);

    usePrintPage(format, thermalWidth);

    useEffect(() => {
        if (!valid) {
            return;
        }

        let cancelled = false;

        const load = async () => {
            setLoading(true);
            setError(null);

            try {
                const data = await getPaymentReceipt(saleId, payId);

                if (!cancelled) {
                    setReceipt(data);
                    setFormat(
                        data.settings.defaultFormat === "Thermal"
                            ? "Thermal"
                            : "A4"
                    );
                    setThermalWidth(
                        data.settings.thermalPaperWidthMm === 58 ? 58 : 80
                    );
                }
            } catch (err) {
                if (!cancelled) {
                    setError({
                        message: getApiErrorMessage(
                            err,
                            "Unable to load the receipt."
                        ),
                        notFound: getApiStatus(err) === 404,
                    });
                }
            } finally {
                if (!cancelled) {
                    setLoading(false);
                }
            }
        };

        void load();

        return () => {
            cancelled = true;
        };
    }, [saleId, payId, valid]);

    const handleDownload = async () => {
        if (!receipt) {
            return;
        }

        try {
            setDownloading(true);
            await downloadReceiptPdf(
                receipt.saleId,
                receipt.paymentId,
                receipt.receiptNumber,
                format,
                thermalWidth
            );
        } catch (err) {
            toast.error(
                getApiErrorMessage(err, "Unable to download the receipt.")
            );
        } finally {
            setDownloading(false);
        }
    };

    if (!valid || (error?.notFound ?? false)) {
        return (
            <div className="data-page">
                <div className="data-card">
                    <div className="state-block">
                        <i className="bi bi-receipt state-icon"></i>
                        <p>This receipt does not exist.</p>
                        <Link to="/sales" className="btn btn-primary btn-sm">
                            Back to sales
                        </Link>
                    </div>
                </div>
            </div>
        );
    }

    if (loading && !receipt) {
        return (
            <div className="data-page">
                <div className="data-card">
                    <div className="state-block">
                        <div
                            className="spinner-border text-primary"
                            role="status"
                        >
                            <span className="visually-hidden">
                                Loading receipt...
                            </span>
                        </div>
                        <p>Loading receipt...</p>
                    </div>
                </div>
            </div>
        );
    }

    if (error || !receipt) {
        return (
            <div className="data-page">
                <div className="data-card">
                    <div className="state-block" role="alert">
                        <i className="bi bi-exclamation-octagon state-icon text-danger"></i>
                        <p>{error?.message ?? "Unable to load the receipt."}</p>
                    </div>
                </div>
            </div>
        );
    }

    return (
        <div className="data-page data-page-narrow">
            <div className="invoice-toolbar no-print">
                <Link
                    to={`/sales/${receipt.saleId}`}
                    className="btn btn-outline-secondary"
                >
                    <i className="bi bi-arrow-left me-2"></i>
                    Invoice {receipt.invoiceNumber}
                </Link>

                <div className="d-flex flex-wrap gap-2 align-items-center">
                    <select
                        className="form-select"
                        style={{ width: "auto" }}
                        aria-label="Receipt format"
                        value={format}
                        onChange={(e) =>
                            setFormat(e.target.value as InvoiceFormat)
                        }
                    >
                        <option value="A4">A5 receipt</option>
                        <option value="Thermal">Thermal</option>
                    </select>

                    {format === "Thermal" && (
                        <select
                            className="form-select"
                            style={{ width: "auto" }}
                            aria-label="Thermal paper width"
                            value={thermalWidth}
                            onChange={(e) =>
                                setThermalWidth(
                                    Number(e.target.value) as 58 | 80
                                )
                            }
                        >
                            <option value={80}>80 mm</option>
                            <option value={58}>58 mm</option>
                        </select>
                    )}

                    <button
                        type="button"
                        className="btn btn-outline-primary"
                        onClick={() => window.print()}
                    >
                        <i className="bi bi-printer me-2"></i>
                        Print
                    </button>

                    <button
                        type="button"
                        className="btn btn-primary"
                        disabled={downloading}
                        onClick={() => void handleDownload()}
                    >
                        {downloading ? (
                            <span
                                className="spinner-border spinner-border-sm me-2"
                                aria-hidden="true"
                            ></span>
                        ) : (
                            <i className="bi bi-file-earmark-pdf me-2"></i>
                        )}
                        Download PDF
                    </button>
                </div>
            </div>

            {format === "Thermal" && (
                <p className="format-note no-print mb-3">
                    Print opens the browser print dialog. Choose {thermalWidth}{" "}
                    mm paper in the printer settings — the browser cannot pick
                    the printer for you.
                </p>
            )}

            <ReceiptView
                receipt={receipt}
                format={format}
                thermalWidthMm={thermalWidth}
            />
        </div>
    );
}

export default PaymentReceiptPage;
