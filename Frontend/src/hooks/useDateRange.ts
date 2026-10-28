import { useState } from "react";

/** From/to date filter state with inverted-range detection. */
export function useDateRange() {
    const [from, setFrom] = useState("");
    const [to, setTo] = useState("");

    const invalid = from !== "" && to !== "" && from > to;

    return {
        from,
        to,
        setFrom,
        setTo,
        invalid,
        active: from !== "" || to !== "",
        reset: () => {
            setFrom("");
            setTo("");
        },
        // An inverted range would return nothing; wait until it is fixed.
        filters: {
            fromDate: invalid ? undefined : from || undefined,
            toDate: invalid ? undefined : to || undefined,
        },
    };
}

export type DateRange = ReturnType<typeof useDateRange>;