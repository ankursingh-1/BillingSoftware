import { useEffect, useState } from "react";

import { getGstStates } from "../services/lookups/lookupService";

import type { GstState } from "../interfaces/lookups/lookups";

/** GST state list for dropdowns. Empty until loaded (or if loading fails). */
export function useGstStates(): GstState[] {
    const [states, setStates] = useState<GstState[]>([]);

    useEffect(() => {
        let cancelled = false;

        getGstStates()
            .then((list) => {
                if (!cancelled) {
                    setStates(list);
                }
            })
            .catch(() => {
                // The dropdown simply stays empty; saving still works because
                // state is optional and the server validates it.
            });

        return () => {
            cancelled = true;
        };
    }, []);

    return states;
}