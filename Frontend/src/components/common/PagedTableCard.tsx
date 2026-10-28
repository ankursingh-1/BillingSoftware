import type { ReactNode } from "react";

import { PAGE_SIZE_OPTIONS } from "../../hooks/usePagedSearch";
import Pagination from "./Pagination";

import type { PagedList } from "../../hooks/usePagedSearch";

interface PagedTableCardProps<T> {
    list: PagedList<T>;
    /** Plural noun used in messages, e.g. "purchases". */
    noun: string;
    searchPlaceholder: string;
    /** Extra filter controls shown beside the search box. */
    filters?: ReactNode;
    /** Content of the table header row (the <tr>). */
    head: ReactNode;
    /** Renders one <tr>; it must set its own key. */
    renderRow: (item: T) => ReactNode;
    emptyIcon: string;
    emptyText: string;
    isFiltering: boolean;
    onClearFilters: () => void;
    emptyAction?: ReactNode;
    /** Shown above the table, e.g. a validation warning. */
    banner?: ReactNode;
}

/**
 * The shared "toolbar + table + states + pagination" card used by the list
 * pages. It only presents state; the caller owns the data (usePagedSearch).
 */
function PagedTableCard<T>({
    list,
    noun,
    searchPlaceholder,
    filters,
    head,
    renderRow,
    emptyIcon,
    emptyText,
    isFiltering,
    onClearFilters,
    emptyAction,
    banner,
}: PagedTableCardProps<T>) {
    const {
        search,
        setSearch,
        page,
        pageSize,
        result,
        loading,
        error,
        goToPage,
        changePageSize,
        reload,
    } = list;

    const rows = result?.data ?? [];

    return (
        <div className="data-card">
            <div className="data-toolbar">
                <div className="input-group data-search">
                    <span className="input-group-text">
                        <i className="bi bi-search"></i>
                    </span>

                    <input
                        type="search"
                        className="form-control"
                        placeholder={searchPlaceholder}
                        aria-label={`Search ${noun}`}
                        value={search}
                        onChange={(e) => setSearch(e.target.value)}
                    />
                </div>

                <div className="data-toolbar-right d-flex flex-wrap gap-2 align-items-center">
                    {filters}

                    <select
                        className="form-select form-select-sm"
                        style={{ width: "auto" }}
                        aria-label="Rows per page"
                        value={pageSize}
                        onChange={(e) =>
                            changePageSize(Number(e.target.value))
                        }
                    >
                        {PAGE_SIZE_OPTIONS.map((size) => (
                            <option key={size} value={size}>
                                {size} / page
                            </option>
                        ))}
                    </select>
                </div>
            </div>

            {banner}

            {error && !result ? (
                <div className="state-block" role="alert">
                    <i className="bi bi-exclamation-octagon state-icon text-danger"></i>
                    <p>{error}</p>
                    <button
                        type="button"
                        className="btn btn-outline-primary btn-sm"
                        onClick={reload}
                    >
                        <i className="bi bi-arrow-clockwise me-1"></i>
                        Retry
                    </button>
                </div>
            ) : loading && !result ? (
                <div className="state-block">
                    <div className="spinner-border text-primary" role="status">
                        <span className="visually-hidden">
                            Loading {noun}...
                        </span>
                    </div>
                    <p>Loading {noun}...</p>
                </div>
            ) : (
                <>
                    {error && (
                        <div
                            className="alert alert-danger m-3 mb-0"
                            role="alert"
                        >
                            {error}{" "}
                            <button
                                type="button"
                                className="btn btn-link p-0 align-baseline"
                                onClick={reload}
                            >
                                Retry
                            </button>
                        </div>
                    )}

                    {rows.length === 0 ? (
                        <div className="state-block">
                            <i className={`bi ${emptyIcon} state-icon`}></i>
                            <p>
                                {isFiltering
                                    ? `No ${noun} match these filters.`
                                    : emptyText}
                            </p>
                            {isFiltering ? (
                                <button
                                    type="button"
                                    className="btn btn-outline-primary btn-sm"
                                    onClick={onClearFilters}
                                >
                                    Clear filters
                                </button>
                            ) : (
                                emptyAction
                            )}
                        </div>
                    ) : (
                        <div
                            className={`table-wrapper${
                                loading ? " is-loading" : ""
                            }`}
                            aria-busy={loading}
                        >
                            <div className="table-responsive">
                                <table className="table table-hover align-middle mb-0">
                                    <thead>
                                        <tr>{head}</tr>
                                    </thead>
                                    <tbody>{rows.map(renderRow)}</tbody>
                                </table>
                            </div>

                            <Pagination
                                page={result?.pageNumber ?? page}
                                totalPages={result?.totalPages ?? 1}
                                totalRecords={result?.totalRecords ?? 0}
                                pageSize={result?.pageSize ?? pageSize}
                                disabled={loading}
                                onPageChange={goToPage}
                            />
                        </div>
                    )}
                </>
            )}
        </div>
    );
}

export default PagedTableCard;