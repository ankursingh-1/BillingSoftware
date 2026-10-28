import { useContext } from "react";

import { LayoutContext } from "../context/LayoutContext";
import type { LayoutContextValue } from "../context/LayoutContext";

export function useLayout(): LayoutContextValue {
    const context = useContext(LayoutContext);

    if (!context) {
        throw new Error("useLayout must be used inside LayoutProvider.");
    }

    return context;
}
