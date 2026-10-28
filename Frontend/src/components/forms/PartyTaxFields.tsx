import { useGstStates } from "../../hooks/useGstStates";

import type { UseFormRegisterReturn } from "react-hook-form";

interface PartyTaxFieldsProps {
    idPrefix: string;
    stateProps: UseFormRegisterReturn;
    gstinProps: UseFormRegisterReturn;
    stateError?: string;
    gstinError?: string;
    /** Saved code, shown if the lookup has not loaded that option yet. */
    savedCode?: string | null;
}

/** State + GSTIN inputs shared by the customer and supplier forms. */
function PartyTaxFields({
    idPrefix,
    stateProps,
    gstinProps,
    stateError,
    gstinError,
    savedCode,
}: PartyTaxFieldsProps) {
    const { states, ready } = useGstStates();
    const extra =
        savedCode && !states.some((state) => state.code === savedCode)
            ? savedCode
            : null;

    return (
        <div className="row g-3 mb-3">
            <div className="col-md-6">
                <label htmlFor={`${idPrefix}-state`} className="form-label">
                    State
                </label>

                {!ready ? (
                    <select
                        id={`${idPrefix}-state`}
                        className="form-select"
                        disabled
                        aria-busy="true"
                    >
                        <option>Loading states...</option>
                    </select>
                ) : (
                    <select
                        id={`${idPrefix}-state`}
                        className={`form-select${stateError ? " is-invalid" : ""}`}
                        aria-invalid={stateError ? "true" : "false"}
                        {...stateProps}
                    >
                        <option value="">Not specified</option>
                        {extra && <option value={extra}>{extra}</option>}
                        {states.map((state) => (
                            <option key={state.code} value={state.code}>
                                {state.name} ({state.code})
                            </option>
                        ))}
                    </select>
                )}

                {stateError && (
                    <div className="invalid-feedback" role="alert">
                        {stateError}
                    </div>
                )}

                <div className="form-text">
                    Decides CGST + SGST or IGST on invoices. If left blank,
                    your own state is assumed.
                </div>
            </div>

            <div className="col-md-6">
                <label htmlFor={`${idPrefix}-gstin`} className="form-label">
                    GSTIN
                </label>

                <input
                    id={`${idPrefix}-gstin`}
                    type="text"
                    maxLength={15}
                    autoComplete="off"
                    className={`form-control text-uppercase${
                        gstinError ? " is-invalid" : ""
                    }`}
                    aria-invalid={gstinError ? "true" : "false"}
                    {...gstinProps}
                />

                {gstinError && (
                    <div className="invalid-feedback" role="alert">
                        {gstinError}
                    </div>
                )}

                <div className="form-text">Optional. 15 characters.</div>
            </div>
        </div>
    );
}

export default PartyTaxFields;
