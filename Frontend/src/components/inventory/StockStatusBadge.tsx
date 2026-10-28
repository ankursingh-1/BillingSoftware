import type { StockStatus } from "../../interfaces/inventory/inventory";

const STYLES: Record<StockStatus, { className: string; label: string }> = {
    InStock: { className: "text-bg-success", label: "In stock" },
    Low: { className: "text-bg-warning", label: "Low stock" },
    Out: { className: "text-bg-danger", label: "Out of stock" },
};

function StockStatusBadge({ status }: { status: StockStatus }) {
    const style = STYLES[status] ?? STYLES.InStock;

    return <span className={`badge ${style.className}`}>{style.label}</span>;
}

export default StockStatusBadge;