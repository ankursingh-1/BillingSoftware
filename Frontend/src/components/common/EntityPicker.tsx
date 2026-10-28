import { useState } from "react";
import type { ReactNode } from "react";

import { useRemoteOptions } from "../../hooks/useRemoteOptions";

interface EntityPickerProps<T> {
    id: string;
    label: string;
    placeholder: string;
    icon: string;
    /** Must be a stable (module-level) function. */
    search: (query: string) => Promise<T[]>;
    getKey: (item: T) => number | string;
    renderOption: (item: T) => ReactNode;
    isDisabled?: (item: T) => boolean;
    onSelect: (item: T) => void;
    /** Clear the typed text after picking (for "add to cart" pickers). */
    clearOnSelect?: boolean;
    disabled?: boolean;
    emptyHint?: string;
}

/** Type-ahead search box with a dropdown of server-side results. */
function EntityPicker<T>({
    id,
    label,
    placeholder,
    icon,
    search,
    getKey,
    renderOption,
    isDisabled,
    onSelect,
    clearOnSelect = true,
    disabled,
    emptyHint = "Nothing found.",
}: EntityPickerProps<T>) {
    const [query, setQuery] = useState("");
    const [open, setOpen] = useState(false);

    const options = useRemoteOptions(search, query, open);

    return (
        <div className="pos-results">
            <label htmlFor={id} className="visually-hidden">
                {label}
            </label>

            <div className="input-group">
                <span className="input-group-text">
                    <i className={`bi ${icon}`}></i>
                </span>

                <input
                    id={id}
                    type="search"
                    className="form-control"
                    placeholder={placeholder}
                    autoComplete="off"
                    disabled={disabled}
                    value={query}
                    onChange={(e) => {
                        setQuery(e.target.value);
                        setOpen(true);
                    }}
                    onFocus={() => setOpen(true)}
                    onBlur={() => setOpen(false)}
                />
            </div>

            {open && (
                <div
                    className="pos-dropdown"
                    onMouseDown={(e) => e.preventDefault()}
                >
                    {options.failed ? (
                        <div className="p-3 text-danger">
                            Unable to search. Please try again.
                        </div>
                    ) : options.items.length === 0 ? (
                        <div className="p-3 text-muted">
                            {options.loading ? "Searching..." : emptyHint}
                        </div>
                    ) : (
                        options.items.map((item) => (
                            <button
                                type="button"
                                key={getKey(item)}
                                className="pos-option"
                                disabled={isDisabled?.(item) ?? false}
                                onClick={() => {
                                    onSelect(item);
                                    setOpen(false);
                                    setQuery(clearOnSelect ? "" : query);
                                }}
                            >
                                {renderOption(item)}
                            </button>
                        ))
                    )}
                </div>
            )}
        </div>
    );
}

export default EntityPicker;