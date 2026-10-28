interface PaginationProps {
    page: number;
    totalPages: number;
    totalRecords: number;
    pageSize: number;
    disabled?: boolean;
    onPageChange: (page: number) => void;
}

/** Page numbers to show: first, last, and a window around the current page. */
function getPageItems(
    page: number,
    totalPages: number
): (number | "gap-start" | "gap-end")[] {
    const items: (number | "gap-start" | "gap-end")[] = [];

    for (let p = 1; p <= totalPages; p++) {
        if (
            p === 1 ||
            p === totalPages ||
            Math.abs(p - page) <= 1
        ) {
            items.push(p);
        } else if (p < page && items[items.length - 1] !== "gap-start") {
            items.push("gap-start");
        } else if (p > page && items[items.length - 1] !== "gap-end") {
            items.push("gap-end");
        }
    }

    return items;
}

function Pagination({
    page,
    totalPages,
    totalRecords,
    pageSize,
    disabled = false,
    onPageChange,
}: PaginationProps) {
    if (totalRecords === 0) {
        return null;
    }

    const first = (page - 1) * pageSize + 1;
    const last = Math.min(page * pageSize, totalRecords);

    return (
        <div className="pagination-bar">
            <span className="pagination-summary">
                Showing {first}–{last} of {totalRecords}
            </span>

            {totalPages > 1 && (
                <nav aria-label="Pagination">
                    <ul className="pagination pagination-sm mb-0">
                        <li
                            className={`page-item${
                                page <= 1 || disabled ? " disabled" : ""
                            }`}
                        >
                            <button
                                type="button"
                                className="page-link"
                                onClick={() => onPageChange(page - 1)}
                                disabled={page <= 1 || disabled}
                                aria-label="Previous page"
                            >
                                &laquo;
                            </button>
                        </li>

                        {getPageItems(page, totalPages).map((item) =>
                            typeof item === "number" ? (
                                <li
                                    key={item}
                                    className={`page-item${
                                        item === page ? " active" : ""
                                    }`}
                                >
                                    <button
                                        type="button"
                                        className="page-link"
                                        onClick={() => onPageChange(item)}
                                        disabled={disabled}
                                        aria-current={
                                            item === page
                                                ? "page"
                                                : undefined
                                        }
                                    >
                                        {item}
                                    </button>
                                </li>
                            ) : (
                                <li
                                    key={item}
                                    className="page-item disabled"
                                >
                                    <span className="page-link">…</span>
                                </li>
                            )
                        )}

                        <li
                            className={`page-item${
                                page >= totalPages || disabled
                                    ? " disabled"
                                    : ""
                            }`}
                        >
                            <button
                                type="button"
                                className="page-link"
                                onClick={() => onPageChange(page + 1)}
                                disabled={page >= totalPages || disabled}
                                aria-label="Next page"
                            >
                                &raquo;
                            </button>
                        </li>
                    </ul>
                </nav>
            )}
        </div>
    );
}

export default Pagination;
