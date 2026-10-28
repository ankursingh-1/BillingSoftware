import { Link } from "react-router-dom";

import Modal from "../common/Modal";
import PaymentStatusBadge from "./PaymentStatusBadge";

import { paymentMethodLabel } from "../../constants/payments";
import { formatCurrency } from "../../utils/format";

import type { SaleDetail, SalePayment } from "../../interfaces/sale/sale";

interface PaymentRecordedModalProps {
    sale: SaleDetail;
    payment: SalePayment;
    onClose: () => void;
}

function PaymentRecordedModal({
    sale,
    payment,
    onClose,
}: PaymentRecordedModalProps) {
    return (
        <Modal
            show
            title="Payment recorded"
            onClose={onClose}
            footer={
                <>
                    <button
                        type="button"
                        className="btn btn-outline-secondary"
                        onClick={onClose}
                    >
                        Close
                    </button>
                    {payment.receiptNumber && (
                        <Link
                            className="btn btn-primary"
                            to={`/sales/${sale.id}/payments/${payment.id}/receipt`}
                        >
                            Print receipt
                        </Link>
                    )}
                </>
            }
        >
            <p className="mb-3">
                Received{" "}
                <strong>{formatCurrency(payment.amount)}</strong> by{" "}
                {paymentMethodLabel(payment.method)}.
            </p>
            <dl className="row mb-0">
                <dt className="col-6">Invoice</dt>
                <dd className="col-6 text-end">{sale.invoiceNumber}</dd>
                <dt className="col-6">Remaining balance</dt>
                <dd className="col-6 text-end">
                    <strong>{formatCurrency(sale.outstanding)}</strong>
                </dd>
                <dt className="col-6">Status</dt>
                <dd className="col-6 text-end">
                    <PaymentStatusBadge status={sale.paymentStatus} />
                </dd>
            </dl>
        </Modal>
    );
}

export default PaymentRecordedModal;
