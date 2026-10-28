import { useCallback, useEffect, useRef, useState } from "react";
import type { ChangeEvent, FormEvent } from "react";
import { toast } from "react-toastify";

import { API_URL } from "../../config/api";
import { useGstStates } from "../../hooks/useGstStates";
import {
    getCompany,
    removeCompanyLogo,
    saveCompany,
    uploadCompanyLogo,
} from "../../services/company/companyService";
import { getApiErrorMessage } from "../../utils/apiError";
import { GSTIN_PATTERN, emptyToNull } from "../../utils/validation";

import type {
    InvoiceFormat,
    SaveCompanyRequest,
} from "../../interfaces/company/company";

import "../../styles/company-settings.css";

const PREFIX_PATTERN = /^[A-Za-z0-9]{1,4}$/;
const PAN_PATTERN = /^[A-Z]{5}[0-9]{4}[A-Z]$/;
const MAX_LOGO_BYTES = 2 * 1024 * 1024;

interface FormState {
    companyName: string;
    gstNumber: string;
    panNumber: string;
    address: string;
    city: string;
    stateCode: string;
    country: string;
    pincode: string;
    phone: string;
    email: string;
    website: string;
    invoicePrefix: string;
    currency: string;
    bankName: string;
    accountNumber: string;
    ifscCode: string;
    upiId: string;
    termsAndConditions: string;
    footerMessage: string;
    defaultInvoiceFormat: InvoiceFormat;
    thermalPaperWidthMm: 58 | 80;
    showBankDetails: boolean;
    showLogo: boolean;
    showTerms: boolean;
    showFooter: boolean;
    isActive: boolean;
}

const emptyForm: FormState = {
    companyName: "",
    gstNumber: "",
    panNumber: "",
    address: "",
    city: "",
    stateCode: "",
    country: "India",
    pincode: "",
    phone: "",
    email: "",
    website: "",
    invoicePrefix: "INV",
    currency: "INR",
    bankName: "",
    accountNumber: "",
    ifscCode: "",
    upiId: "",
    termsAndConditions: "",
    footerMessage: "",
    defaultInvoiceFormat: "A4",
    thermalPaperWidthMm: 80,
    showBankDetails: false,
    showLogo: true,
    showTerms: true,
    showFooter: true,
    isActive: true,
};

function toPayload(form: FormState): SaveCompanyRequest {
    return {
        companyName: form.companyName.trim(),
        gstNumber: emptyToNull(form.gstNumber.toUpperCase()),
        panNumber: emptyToNull(form.panNumber.toUpperCase()),
        address: emptyToNull(form.address),
        city: emptyToNull(form.city),
        state: null,
        stateCode: emptyToNull(form.stateCode),
        country: emptyToNull(form.country),
        pincode: emptyToNull(form.pincode),
        phone: emptyToNull(form.phone),
        email: emptyToNull(form.email),
        website: emptyToNull(form.website),
        invoicePrefix: form.invoicePrefix.trim().toUpperCase(),
        currency: form.currency.trim().toUpperCase(),
        bankName: emptyToNull(form.bankName),
        accountNumber: emptyToNull(form.accountNumber),
        ifscCode: emptyToNull(form.ifscCode.toUpperCase()),
        upiId: emptyToNull(form.upiId),
        termsAndConditions: emptyToNull(form.termsAndConditions),
        footerMessage: emptyToNull(form.footerMessage),
        defaultInvoiceFormat: form.defaultInvoiceFormat,
        thermalPaperWidthMm: form.thermalPaperWidthMm,
        showBankDetails: form.showBankDetails,
        showLogo: form.showLogo,
        showTerms: form.showTerms,
        showFooter: form.showFooter,
        isActive: form.isActive,
    };
}

function CompanySettingsPage() {
    const { states, ready } = useGstStates();
    const fileRef = useRef<HTMLInputElement>(null);

    const [form, setForm] = useState<FormState>(emptyForm);
    const [hasLogo, setHasLogo] = useState(false);
    const [logoKey, setLogoKey] = useState(0);
    const [loading, setLoading] = useState(true);
    const [saving, setSaving] = useState(false);
    const [uploading, setUploading] = useState(false);

    const loadCompany = useCallback(async () => {
        try {
            setLoading(true);

            const company = await getCompany();

            if (company) {
                setForm({
                    companyName: company.companyName,
                    gstNumber: company.gstNumber ?? "",
                    panNumber: company.panNumber ?? "",
                    address: company.address ?? "",
                    city: company.city ?? "",
                    stateCode: company.stateCode ?? "",
                    country: company.country ?? "India",
                    pincode: company.pincode ?? "",
                    phone: company.phone ?? "",
                    email: company.email ?? "",
                    website: company.website ?? "",
                    invoicePrefix: company.invoicePrefix || "INV",
                    currency: company.currency || "INR",
                    bankName: company.bankName ?? "",
                    accountNumber: company.accountNumber ?? "",
                    ifscCode: company.ifscCode ?? "",
                    upiId: company.upiId ?? "",
                    termsAndConditions: company.termsAndConditions ?? "",
                    footerMessage: company.footerMessage ?? "",
                    defaultInvoiceFormat: company.defaultInvoiceFormat || "A4",
                    thermalPaperWidthMm:
                        company.thermalPaperWidthMm === 58 ? 58 : 80,
                    showBankDetails: company.showBankDetails,
                    showLogo: company.showLogo,
                    showTerms: company.showTerms,
                    showFooter: company.showFooter,
                    isActive: company.isActive,
                });
                setHasLogo(company.hasLogo);
            }
        } catch (error) {
            toast.error(
                getApiErrorMessage(error, "Unable to load company settings.")
            );
        } finally {
            setLoading(false);
        }
    }, []);

    useEffect(() => {
        // eslint-disable-next-line react-hooks/set-state-in-effect
        void loadCompany();
    }, [loadCompany]);

    const handleChange = (
        e: ChangeEvent<HTMLInputElement | HTMLTextAreaElement | HTMLSelectElement>
    ) => {
        const { name, value } = e.target;

        setForm((previous) => ({
            ...previous,
            [name]: value,
        }));
    };

    const handleToggle = (e: ChangeEvent<HTMLInputElement>) => {
        const { name, checked } = e.target;

        setForm((previous) => ({
            ...previous,
            [name]: checked,
        }));
    };

    const validate = (): string | null => {
        if (!form.companyName.trim()) {
            return "Company name is required.";
        }

        if (!PREFIX_PATTERN.test(form.invoicePrefix.trim())) {
            return "The invoice prefix must be 1 to 4 letters or digits.";
        }

        const gstin = form.gstNumber.trim().toUpperCase();

        if (gstin && !GSTIN_PATTERN.test(gstin)) {
            return "Enter a valid 15-character GSTIN.";
        }

        if (
            gstin &&
            form.stateCode &&
            gstin.slice(0, 2) !== form.stateCode
        ) {
            return `The GSTIN starts with ${gstin.slice(0, 2)} but the selected state is ${form.stateCode}.`;
        }

        const pan = form.panNumber.trim().toUpperCase();

        if (pan && !PAN_PATTERN.test(pan)) {
            return "Enter a valid 10-character PAN (for example ABCDE1234F).";
        }

        return null;
    };

    const handleSubmit = async (e: FormEvent<HTMLFormElement>) => {
        e.preventDefault();

        const problem = validate();

        if (problem) {
            toast.error(problem);
            return;
        }

        try {
            setSaving(true);
            await saveCompany(toPayload(form));
            toast.success("Company settings saved.");
            await loadCompany();
        } catch (error) {
            toast.error(
                getApiErrorMessage(error, "Unable to save company settings.")
            );
        } finally {
            setSaving(false);
        }
    };

    const handleLogo = async (e: ChangeEvent<HTMLInputElement>) => {
        const file = e.target.files?.[0];

        e.target.value = "";

        if (!file) {
            return;
        }

        if (!["image/png", "image/jpeg"].includes(file.type)) {
            toast.error("Upload a PNG or JPEG image.");
            return;
        }

        if (file.size > MAX_LOGO_BYTES) {
            toast.error("The logo must be 2 MB or smaller.");
            return;
        }

        try {
            setUploading(true);
            await uploadCompanyLogo(file);
            setHasLogo(true);
            setLogoKey((key) => key + 1);
            toast.success("Logo saved. Issued invoices keep the logo they were created with.");
        } catch (error) {
            toast.error(getApiErrorMessage(error, "Unable to upload the logo."));
        } finally {
            setUploading(false);
        }
    };

    const handleRemoveLogo = async () => {
        try {
            setUploading(true);
            await removeCompanyLogo();
            setHasLogo(false);
            setLogoKey((key) => key + 1);
            toast.success("Logo removed from new invoices. Issued invoices are unchanged.");
        } catch (error) {
            toast.error(getApiErrorMessage(error, "Unable to remove the logo."));
        } finally {
            setUploading(false);
        }
    };

    if (loading) {
        return (
            <div className="company-loading">
                <div className="spinner-border text-primary" role="status">
                    <span className="visually-hidden">Loading...</span>
                </div>
                <p>Loading company settings...</p>
            </div>
        );
    }

    return (
        <div className="company-settings-page">
            <div className="company-page-header">
                <div>
                    <h2>Company Settings</h2>
                    <p>
                        Business details printed on new invoices. Changing them
                        does not rewrite invoices already issued.
                    </p>
                </div>
            </div>

            <form onSubmit={handleSubmit}>
                <div className="company-card">
                    <div className="company-card-header">
                        <div className="section-icon">
                            <i className="bi bi-building"></i>
                        </div>
                        <div>
                            <h5>Company Information</h5>
                            <span>Name, tax IDs and contact</span>
                        </div>
                    </div>

                    <div className="row g-4">
                        <div className="col-md-8">
                            <label className="form-label" htmlFor="companyName">
                                Company Name
                                <span className="required">*</span>
                            </label>
                            <input
                                id="companyName"
                                type="text"
                                name="companyName"
                                className="form-control"
                                value={form.companyName}
                                onChange={handleChange}
                                maxLength={200}
                                required
                            />
                        </div>

                        <div className="col-md-4">
                            <label className="form-label" htmlFor="invoicePrefix">
                                Invoice Prefix
                                <span className="required">*</span>
                            </label>
                            <input
                                id="invoicePrefix"
                                type="text"
                                name="invoicePrefix"
                                className="form-control text-uppercase"
                                value={form.invoicePrefix}
                                onChange={handleChange}
                                maxLength={4}
                                required
                            />
                            <div className="form-text">
                                1–4 letters or digits. Invoice numbers are
                                limited to 16 characters.
                            </div>
                        </div>

                        <div className="col-md-6">
                            <label className="form-label" htmlFor="gstNumber">
                                GSTIN
                            </label>
                            <input
                                id="gstNumber"
                                type="text"
                                name="gstNumber"
                                className="form-control text-uppercase"
                                value={form.gstNumber}
                                onChange={handleChange}
                                maxLength={15}
                            />
                        </div>

                        <div className="col-md-6">
                            <label className="form-label" htmlFor="panNumber">
                                PAN
                            </label>
                            <input
                                id="panNumber"
                                type="text"
                                name="panNumber"
                                className="form-control text-uppercase"
                                value={form.panNumber}
                                onChange={handleChange}
                                maxLength={10}
                            />
                        </div>

                        <div className="col-md-4">
                            <label className="form-label" htmlFor="phone">
                                Phone
                            </label>
                            <input
                                id="phone"
                                type="tel"
                                name="phone"
                                className="form-control"
                                value={form.phone}
                                onChange={handleChange}
                            />
                        </div>

                        <div className="col-md-4">
                            <label className="form-label" htmlFor="email">
                                Email
                            </label>
                            <input
                                id="email"
                                type="email"
                                name="email"
                                className="form-control"
                                value={form.email}
                                onChange={handleChange}
                            />
                        </div>

                        <div className="col-md-4">
                            <label className="form-label" htmlFor="website">
                                Website
                            </label>
                            <input
                                id="website"
                                type="url"
                                name="website"
                                className="form-control"
                                value={form.website}
                                onChange={handleChange}
                                placeholder="https://"
                            />
                        </div>
                    </div>
                </div>

                <div className="company-card">
                    <div className="company-card-header">
                        <div className="section-icon">
                            <i className="bi bi-geo-alt"></i>
                        </div>
                        <div>
                            <h5>Business Address</h5>
                            <span>Address shown on invoices</span>
                        </div>
                    </div>

                    <div className="row g-4">
                        <div className="col-12">
                            <label className="form-label" htmlFor="address">
                                Address
                            </label>
                            <textarea
                                id="address"
                                name="address"
                                className="form-control"
                                rows={3}
                                value={form.address}
                                onChange={handleChange}
                                maxLength={500}
                            />
                        </div>

                        <div className="col-md-4">
                            <label className="form-label" htmlFor="city">
                                City
                            </label>
                            <input
                                id="city"
                                type="text"
                                name="city"
                                className="form-control"
                                value={form.city}
                                onChange={handleChange}
                            />
                        </div>

                        <div className="col-md-4">
                            <label className="form-label" htmlFor="stateCode">
                                State
                            </label>
                            <select
                                id="stateCode"
                                name="stateCode"
                                className="form-select"
                                value={form.stateCode}
                                onChange={handleChange}
                                disabled={!ready}
                            >
                                <option value="">Not specified</option>
                                {form.stateCode &&
                                    !states.some(
                                        (s) => s.code === form.stateCode
                                    ) && (
                                        <option value={form.stateCode}>
                                            {form.stateCode}
                                        </option>
                                    )}
                                {states.map((state) => (
                                    <option key={state.code} value={state.code}>
                                        {state.name} ({state.code})
                                    </option>
                                ))}
                            </select>
                            <div className="form-text">
                                Used to decide CGST + SGST or IGST. Must match
                                the GSTIN state code.
                            </div>
                        </div>

                        <div className="col-md-4">
                            <label className="form-label" htmlFor="country">
                                Country
                            </label>
                            <input
                                id="country"
                                type="text"
                                name="country"
                                className="form-control"
                                value={form.country}
                                onChange={handleChange}
                            />
                        </div>

                        <div className="col-md-4">
                            <label className="form-label" htmlFor="pincode">
                                Pincode
                            </label>
                            <input
                                id="pincode"
                                type="text"
                                name="pincode"
                                className="form-control"
                                value={form.pincode}
                                onChange={handleChange}
                                maxLength={10}
                            />
                        </div>
                    </div>
                </div>

                <div className="company-card">
                    <div className="company-card-header">
                        <div className="section-icon">
                            <i className="bi bi-printer"></i>
                        </div>
                        <div>
                            <h5>Invoice print</h5>
                            <span>
                                Default layout. You can still pick a format
                                when printing a single invoice.
                            </span>
                        </div>
                    </div>

                    <div className="row g-4">
                        <div className="col-md-6">
                            <label
                                className="form-label"
                                htmlFor="defaultInvoiceFormat"
                            >
                                Default format
                            </label>
                            <select
                                id="defaultInvoiceFormat"
                                name="defaultInvoiceFormat"
                                className="form-select"
                                value={form.defaultInvoiceFormat}
                                onChange={handleChange}
                            >
                                <option value="A4">A4 tax invoice</option>
                                <option value="Compact">
                                    Compact counter bill (A5)
                                </option>
                                <option value="Thermal">Thermal receipt</option>
                            </select>
                        </div>

                        <div className="col-md-6">
                            <label
                                className="form-label"
                                htmlFor="thermalPaperWidthMm"
                            >
                                Thermal paper width
                            </label>
                            <select
                                id="thermalPaperWidthMm"
                                name="thermalPaperWidthMm"
                                className="form-select"
                                value={form.thermalPaperWidthMm}
                                onChange={(e) =>
                                    setForm((previous) => ({
                                        ...previous,
                                        thermalPaperWidthMm: Number(
                                            e.target.value
                                        ) as 58 | 80,
                                    }))
                                }
                            >
                                <option value={80}>80 mm</option>
                                <option value={58}>58 mm</option>
                            </select>
                        </div>

                        <div className="col-md-6">
                            <label className="form-label" htmlFor="currency">
                                Currency
                                <span className="required">*</span>
                            </label>
                            <select
                                id="currency"
                                name="currency"
                                className="form-select"
                                value={form.currency}
                                onChange={handleChange}
                                required
                            >
                                <option value="INR">INR - Indian Rupee</option>
                            </select>
                        </div>

                        {(
                            [
                                ["showLogo", "Show logo on invoices"],
                                ["showBankDetails", "Show bank details (off by default)"],
                                ["showTerms", "Show terms & conditions"],
                                ["showFooter", "Show footer message"],
                            ] as const
                        ).map(([name, label]) => (
                            <div className="col-md-6" key={name}>
                                <div className="form-check form-switch">
                                    <input
                                        className="form-check-input"
                                        type="checkbox"
                                        role="switch"
                                        id={name}
                                        name={name}
                                        checked={form[name]}
                                        onChange={handleToggle}
                                    />
                                    <label
                                        className="form-check-label"
                                        htmlFor={name}
                                    >
                                        {label}
                                    </label>
                                </div>
                            </div>
                        ))}
                    </div>
                </div>

                <div className="company-card">
                    <div className="company-card-header">
                        <div className="section-icon">
                            <i className="bi bi-bank"></i>
                        </div>
                        <div>
                            <h5>Bank &amp; payment details</h5>
                            <span>
                                Only printed when “Show bank details” is on
                            </span>
                        </div>
                    </div>

                    <div className="row g-4">
                        <div className="col-md-6">
                            <label className="form-label" htmlFor="bankName">
                                Bank Name
                            </label>
                            <input
                                id="bankName"
                                type="text"
                                name="bankName"
                                className="form-control"
                                value={form.bankName}
                                onChange={handleChange}
                            />
                        </div>
                        <div className="col-md-6">
                            <label className="form-label" htmlFor="accountNumber">
                                Account Number
                            </label>
                            <input
                                id="accountNumber"
                                type="text"
                                name="accountNumber"
                                className="form-control"
                                value={form.accountNumber}
                                onChange={handleChange}
                            />
                        </div>
                        <div className="col-md-6">
                            <label className="form-label" htmlFor="ifscCode">
                                IFSC Code
                            </label>
                            <input
                                id="ifscCode"
                                type="text"
                                name="ifscCode"
                                className="form-control"
                                value={form.ifscCode}
                                onChange={handleChange}
                                maxLength={11}
                            />
                        </div>
                        <div className="col-md-6">
                            <label className="form-label" htmlFor="upiId">
                                UPI ID
                            </label>
                            <input
                                id="upiId"
                                type="text"
                                name="upiId"
                                className="form-control"
                                value={form.upiId}
                                onChange={handleChange}
                                placeholder="shop@bank"
                            />
                        </div>
                    </div>
                </div>

                <div className="company-card">
                    <div className="company-card-header">
                        <div className="section-icon">
                            <i className="bi bi-file-text"></i>
                        </div>
                        <div>
                            <h5>Invoice text</h5>
                            <span>Terms and footer</span>
                        </div>
                    </div>

                    <div className="row g-4">
                        <div className="col-12">
                            <label
                                className="form-label"
                                htmlFor="termsAndConditions"
                            >
                                Terms &amp; conditions
                            </label>
                            <textarea
                                id="termsAndConditions"
                                name="termsAndConditions"
                                className="form-control"
                                rows={5}
                                value={form.termsAndConditions}
                                onChange={handleChange}
                                maxLength={2000}
                            />
                        </div>
                        <div className="col-12">
                            <label className="form-label" htmlFor="footerMessage">
                                Footer message
                            </label>
                            <textarea
                                id="footerMessage"
                                name="footerMessage"
                                className="form-control"
                                rows={3}
                                value={form.footerMessage}
                                onChange={handleChange}
                                maxLength={1000}
                            />
                        </div>
                    </div>
                </div>

                <div className="company-card">
                    <div className="company-card-header">
                        <div className="section-icon">
                            <i className="bi bi-image"></i>
                        </div>
                        <div>
                            <h5>Company logo</h5>
                            <span>PNG or JPEG, up to 2 MB</span>
                        </div>
                    </div>

                    <div className="logo-row">
                        {hasLogo ? (
                            <img
                                className="logo-preview"
                                src={`${API_URL}/Company/logo?v=${logoKey}`}
                                alt="Company logo"
                            />
                        ) : (
                            <div className="logo-placeholder">No logo</div>
                        )}

                        <div>
                            <input
                                ref={fileRef}
                                type="file"
                                accept="image/png,image/jpeg"
                                className="d-none"
                                onChange={(e) => void handleLogo(e)}
                            />
                            <div className="d-flex flex-wrap gap-2">
                                <button
                                    type="button"
                                    className="btn btn-outline-primary"
                                    disabled={uploading}
                                    onClick={() => fileRef.current?.click()}
                                >
                                    {hasLogo ? "Replace logo" : "Upload logo"}
                                </button>
                                {hasLogo && (
                                    <button
                                        type="button"
                                        className="btn btn-outline-danger"
                                        disabled={uploading}
                                        onClick={() => void handleRemoveLogo()}
                                    >
                                        Remove
                                    </button>
                                )}
                            </div>
                            <p className="form-text mb-0 mt-2">
                                Replacing the logo does not change invoices
                                already issued. Those keep the file they were
                                created with.
                            </p>
                        </div>
                    </div>
                </div>

                <div className="company-card">
                    <div className="company-status-row">
                        <div>
                            <h5>Company status</h5>
                            <p>Inactive companies are hidden from new work.</p>
                        </div>
                        <div className="form-check form-switch">
                            <input
                                className="form-check-input"
                                type="checkbox"
                                role="switch"
                                id="isActive"
                                name="isActive"
                                checked={form.isActive}
                                onChange={handleToggle}
                            />
                            <label
                                className="form-check-label"
                                htmlFor="isActive"
                            >
                                {form.isActive ? "Active" : "Inactive"}
                            </label>
                        </div>
                    </div>
                </div>

                <div className="company-actions">
                    <button
                        type="submit"
                        className="btn btn-primary px-4"
                        disabled={saving}
                    >
                        {saving ? (
                            <>
                                <span className="spinner-border spinner-border-sm me-2"></span>
                                Saving...
                            </>
                        ) : (
                            <>
                                <i className="bi bi-check-lg me-2"></i>
                                Save company settings
                            </>
                        )}
                    </button>
                </div>
            </form>
        </div>
    );
}

export default CompanySettingsPage;
