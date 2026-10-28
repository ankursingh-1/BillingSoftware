import { useCallback, useEffect, useMemo, useState } from "react";

import { getApiErrorMessage } from "../utils/apiError";
import { useDebounce } from "./useDebounce";

import type { PagedResult } from "../interfaces/common/pagedResult";

export const PAGE_SIZE_OPTIONS = [10, 25, 50];

/** What usePagedSearch returns; list components take this as one prop. */
export interface PagedList<T> {
    search: string;
    setSearch: (value: string) => void;
    debouncedSearch: string;
    page: number;
    pageSize: number;
    result: PagedResult<T> | null;
    loading: boolean;
    error: string | null;
    goToPage: (page: number) => void;
    changePageSize: (size: number) => void;
    reload: () => void;
}

interface PagedRequest {
    search?: string;
    pageNumber: number;
    pageSize: number;
}

/**
 * Server-side search + pagination for list pages.
 *
 * `fetcher` must be a stable reference (e.g. a module-level service
 * function) because it is a dependency of the loading effect.
 *
 * `filters` are extra request fields (date range, status, ...). Changing
 * them sends the user back to page 1.
 */
export function usePagedSearch<T, F extends object = object>(
    fetcher: (request: PagedRequest & F) => Promise<PagedResult<T>>,
    errorFallback: string,
    filters?: F
) {
    const [search, setSearch] = useState("");
    const debouncedSearch = useDebounce(search.trim(), 400);

    // Filters are compared by value so callers can pass an inline object.
    const filtersKey = JSON.stringify(filters ?? {});
    // eslint-disable-next-line react-hooks/exhaustive-deps
    const stableFilters = useMemo(() => filters, [filtersKey]);

    // The page belongs to the search term + filters it was chosen for, so a
    // new search or filter always starts on page 1 without an extra render.
    const queryKey = `${debouncedSearch}|${filtersKey}`;
    const [pageState, setPageState] = useState({ page: 1, forQuery: "" });
    const page = pageState.forQuery === queryKey ? pageState.page : 1;

    const [pageSize, setPageSize] = useState(PAGE_SIZE_OPTIONS[0]);
    const [reloadKey, setReloadKey] = useState(0);

    const [result, setResult] = useState<PagedResult<T> | null>(null);
    const [loading, setLoading] = useState(true);
    const [error, setError] = useState<string | null>(null);

    useEffect(() => {
        let cancelled = false;

        const load = async () => {
            setLoading(true);
            setError(null);

            try {
                const data = await fetcher({
                    ...(stableFilters as F),
                    search: debouncedSearch || undefined,
                    pageNumber: page,
                    pageSize,
                });

                if (cancelled) {
                    return;
                }

                // Page no longer exists (e.g. last item on it was deleted).
                if (data.data.length === 0 && page > 1 && data.totalPages > 0) {
                    setPageState({
                        page: data.totalPages,
                        forQuery: queryKey,
                    });
                    return;
                }

                setResult(data);
            } catch (err) {
                if (!cancelled) {
                    setError(getApiErrorMessage(err, errorFallback));
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
    }, [
        fetcher,
        errorFallback,
        debouncedSearch,
        stableFilters,
        queryKey,
        page,
        pageSize,
        reloadKey,
    ]);

    const goToPage = useCallback(
        (nextPage: number) =>
            setPageState({ page: nextPage, forQuery: queryKey }),
        [queryKey]
    );

    const changePageSize = useCallback(
        (size: number) => {
            setPageSize(size);
            setPageState({ page: 1, forQuery: queryKey });
        },
        [queryKey]
    );

    const reload = useCallback(() => setReloadKey((key) => key + 1), []);

    return {
        search,
        setSearch,
        debouncedSearch,
        page,
        pageSize,
        result,
        loading,
        error,
        goToPage,
        changePageSize,
        reload,
    };
}