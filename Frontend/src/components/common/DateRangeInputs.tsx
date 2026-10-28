import type { DateRange } from "../../hooks/useDateRange";

interface DateRangeInputsProps {
    range: DateRange;
    idPrefix: string;
}

function DateRangeInputs({ range, idPrefix }: DateRangeInputsProps) {
    const className = `form-control form-control-sm${
        range.invalid ? " is-invalid" : ""
    }`;

    return (
        <>
            <label htmlFor={`${idPrefix}-from`} className="visually-hidden">
                From date
            </label>
            <input
                id={`${idPrefix}-from`}
                type="date"
                className={className}
                style={{ width: "auto" }}
                value={range.from}
                max={range.to || undefined}
                onChange={(e) => range.setFrom(e.target.value)}
            />

            <span className="text-muted">to</span>

            <label htmlFor={`${idPrefix}-to`} className="visually-hidden">
                To date
            </label>
            <input
                id={`${idPrefix}-to`}
                type="date"
                className={className}
                style={{ width: "auto" }}
                value={range.to}
                min={range.from || undefined}
                onChange={(e) => range.setTo(e.target.value)}
            />
        </>
    );
}

export function DateRangeWarning({ range }: { range: DateRange }) {
    return range.invalid ? (
        <div className="alert alert-warning m-3 mb-0" role="alert">
            The &quot;from&quot; date is after the &quot;to&quot; date.
        </div>
    ) : null;
}

export default DateRangeInputs;