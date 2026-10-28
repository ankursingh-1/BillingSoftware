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

const dateFormatter = new Intl.DateTimeFormat("en-IN", {
    day: "2-digit",
    month: "short",
    year: "numeric",
});

const dateTimeFormatter = new Intl.DateTimeFormat("en-IN", {
    day: "2-digit",
    month: "short",
    year: "numeric",
    hour: "2-digit",
    minute: "2-digit",
});

/** Today as YYYY-MM-DD in the browser's local time (for date inputs). */
export const todayLocal = (): string => {
    const now = new Date();
    const month = String(now.getMonth() + 1).padStart(2, "0");
    const day = String(now.getDate()).padStart(2, "0");

    return `${now.getFullYear()}-${month}-${day}`;
};

export const formatDate = (value: string): string =>
    dateFormatter.format(new Date(value));

export const formatDateTime = (value: string): string =>
    dateTimeFormatter.format(new Date(value));

/** Money arithmetic helper: rounds to 2 decimals without float drift. */
export const roundMoney = (value: number): number =>
    Math.round((value + Number.EPSILON) * 100) / 100;
