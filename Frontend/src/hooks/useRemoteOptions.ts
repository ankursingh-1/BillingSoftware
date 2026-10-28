import { useEffect, useState } from "react";

import { useDebounce } from "./useDebounce";

interface OptionsState<T> {
    items: T[];
    loading: boolean;
    failed: boolean;
}

/**
 * Debounced "search as you type" options for pickers.
 *
 * `search` must be a stable reference (module-level function). Nothing is
 * requested while `enabled` is false.
 */
export function useRemoteOptions<T>(
    search: (query: string) => Promise<T[]>,
    query: string,
    enabled: boolean
): OptionsState<T> {
    const debounced = useDebounce(query.trim(), 300);
    const [state, setState] = useState<OptionsState<T>>({
        items: [],
        loading: false,
        failed: false,
    });

    useEffect(() => {
        if (!enabled) {
            return;
        }

        let cancelled = false;

        const run = async () => {
            setState((current) => ({
                ...current,
                loading: true,
                failed: false,
            }));

            try {
                const items = await search(debounced);

                if (!cancelled) {
                    setState({ items, loading: false, failed: false });
                }
            } catch {
                if (!cancelled) {
                    setState({ items: [], loading: false, failed: true });
                }
            }
        };

        void run();

        return () => {
            cancelled = true;
        };
    }, [search, debounced, enabled]);

    return state;
}