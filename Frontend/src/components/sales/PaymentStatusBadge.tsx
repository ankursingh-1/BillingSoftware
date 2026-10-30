import type { PaymentStatus } from "../../interfaces/sale/sale";

const BADGES: Record<PaymentStatus, { label: string; className: string }> = {
    Paid: { label: "Paid", className: "text-bg-success" },
    Partial: { label: "Partially Paid", className: "text-bg-warning" },
    Unpaid: { label: "Unpaid", className: "text-bg-danger" },
    Settled: { label: "Settled", className: "text-bg-info" },
    NotRecorded: {
        label: "Not recorded",
        className: "text-bg-secondary",
    },
};

function PaymentStatusBadge({ status }: { status: PaymentStatus }) {
    const badge = BADGES[status] ?? BADGES.NotRecorded;

    return (
        <span
            className={`badge ${badge.className}`}
            title={
                status === "NotRecorded"
                    ? "Payments were not tracked for this older invoice."
                    : undefined
            }
        >
            {badge.label}
        </span>
    );
}

export default PaymentStatusBadge;