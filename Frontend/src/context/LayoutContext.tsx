/* eslint-disable react-refresh/only-export-components */
import { createContext, useMemo, useState } from "react";
import type { PropsWithChildren } from "react";

export interface LayoutContextValue {
    navOpen: boolean;
    openNav: () => void;
    closeNav: () => void;
    toggleNav: () => void;
}

export const LayoutContext = createContext<LayoutContextValue | null>(null);

export function LayoutProvider({ children }: PropsWithChildren) {
    const [navOpen, setNavOpen] = useState(false);

    const value = useMemo(
        () => ({
            navOpen,
            openNav: () => setNavOpen(true),
            closeNav: () => setNavOpen(false),
            toggleNav: () => setNavOpen((open) => !open),
        }),
        [navOpen]
    );

    return (
        <LayoutContext.Provider value={value}>{children}</LayoutContext.Provider>
    );
}
