import { useEffect, useState } from "react";

import { getGstStates } from "../services/lookups/lookupService";

import type { GstState } from "../interfaces/lookups/lookups";

/**
 * GST state list for dropdowns. `ready` is true after the request finishes
 * (even if it failed), so a select can wait until options exist before
 * attaching a saved value.
 */
export function useGstStates(): { states: GstState[]; ready: boolean } {
    const [states, setStates] = useState<GstState[]>([]);
    const [ready, setReady] = useState(false);

    useEffect(() => {
        let cancelled = false;

        getGstStates()
            .then((list) => {
                if (!cancelled) {
                    setStates(list);
                }
            })
            .catch(() => {
                // The dropdown stays empty; saving still works because state
                // is optional and the server validates it.
            })
            .finally(() => {
                if (!cancelled) {
                    setReady(true);
                }
            });

        return () => {
            cancelled = true;
        };
    }, []);

    return { states, ready };
}
