import { useEffect } from "react";

import type { InvoiceFormat } from "../interfaces/company/company";

/**
 * Sets the CSS @page size for the browser print dialog.
 * The user still has to pick the matching paper in the printer settings;
 * the browser cannot choose the printer or paper tray by itself.
 */
export function usePrintPage(
    format: InvoiceFormat,
    thermalWidthMm: 58 | 80 = 80
): void {
    useEffect(() => {
        const style = document.createElement("style");

        style.setAttribute("data-invoice-page", "true");

        const page =
            format === "Thermal"
                ? `size: ${thermalWidthMm}mm auto; margin: 3mm;`
                : format === "Compact"
                  ? "size: A5; margin: 8mm;"
                  : "size: A4; margin: 10mm;";

        style.textContent = `@page { ${page} }`;
        document.head.appendChild(style);

        return () => {
            style.remove();
        };
    }, [format, thermalWidthMm]);
}
