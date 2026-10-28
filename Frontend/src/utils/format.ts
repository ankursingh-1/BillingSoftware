const currencyFormatter = new Intl.NumberFormat("en-IN", {
    style: "currency",
    currency: "INR",
    minimumFractionDigits: 2,
    maximumFractionDigits: 2,
});

export const formatCurrency = (value: number): string =>
    currencyFormatter.format(value);

/** True when the number has at most two decimal places (money). */
export const hasMaxTwoDecimals = (value: number): boolean =>
    Math.abs(value * 100 - Math.round(value * 100)) < 1e-6;
