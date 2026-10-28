import { useCallback, useEffect, useState } from "react";

import { getApiErrorMessage, getApiStatus } from "../utils/apiError";

/**
 * Loads one record by id for a detail page.
 *
 * `fetcher` must be a stable reference (a module-level service function).
 */
export function useLoadedDetail<T>(
    fetcher: (id: number) => Promise<T>,
    rawId: string | undefined,
    errorFallback: string
) {
    const id = Number(rawId);
    const validId = Number.isInteger(id) && id > 0;

    const [data, setData] = useState<T | null>(null);
    const [loading, setLoading] = useState(validId);
    const [error, setError] = useState<string | null>(null);
    const [notFound, setNotFound] = useState(false);
    const [reloadKey, setReloadKey] = useState(0);

    useEffect(() => {
        if (!validId) {
            return;
        }

        let cancelled = false;

        const load = async () => {
            setLoading(true);
            setError(null);
            setNotFound(false);

            try {
                const result = await fetcher(id);

                if (!cancelled) {
                    setData(result);
                }
            } catch (err) {
                if (!cancelled) {
                    setNotFound(getApiStatus(err) === 404);
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
    }, [fetcher, id, validId, errorFallback, reloadKey]);

    const reload = useCallback(() => setReloadKey((key) => key + 1), []);

    return {
        data,
        loading,
        error,
        notFound: !validId || notFound,
        reload,
    };
}