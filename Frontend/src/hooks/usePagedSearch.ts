import { useCallback, useEffect, useState } from "react";

import { getApiErrorMessage } from "../utils/apiError";
import { useDebounce } from "./useDebounce";

import type { PagedResult } from "../interfaces/common/pagedResult";

export const PAGE_SIZE_OPTIONS = [10, 25, 50];

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
 */
export function usePagedSearch<T>(
    fetcher: (request: PagedRequest) => Promise<PagedResult<T>>,
    errorFallback: string
) {
    const [search, setSearch] = useState("");
    const debouncedSearch = useDebounce(search.trim(), 400);

    // The page belongs to the search term it was chosen for, so a new
    // search always starts on page 1 without an extra render/request.
    const [pageState, setPageState] = useState({ page: 1, forSearch: "" });
    const page =
        pageState.forSearch === debouncedSearch ? pageState.page : 1;

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
                        forSearch: debouncedSearch,
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
    }, [fetcher, errorFallback, debouncedSearch, page, pageSize, reloadKey]);

    const goToPage = useCallback(
        (nextPage: number) =>
            setPageState({ page: nextPage, forSearch: debouncedSearch }),
        [debouncedSearch]
    );

    const changePageSize = useCallback(
        (size: number) => {
            setPageSize(size);
            setPageState({ page: 1, forSearch: debouncedSearch });
        },
        [debouncedSearch]
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
