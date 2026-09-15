import { useEffect, useState } from "react";
import type { ChangeEvent, FormEvent } from "react";
import { toast } from "react-toastify";

import {
    getCompany,
    saveCompany,
} from "../../services/company/companyService";

import type {
    SaveCompanyRequest,
} from "../../interfaces/company/company";

import "../../styles/company-settings.css";

const initialForm: SaveCompanyRequest = {
    companyName: "",
    gstNumber: "",
    panNumber: "",
    address: "",
    city: "",
    state: "",
    country: "India",
    pincode: "",
    phone: "",
    email: "",
    website: "",
    logoPath: "",
    invoicePrefix: "INV",
    currency: "INR",
    bankName: "",
    accountNumber: "",
    ifscCode: "",
    upiId: "",
    termsAndConditions: "",
    footerMessage: "",
    isActive: true,
};

function CompanySettingsPage() {
    const [form, setForm] =
        useState<SaveCompanyRequest>(initialForm);

    const [loading, setLoading] = useState(true);
    const [saving, setSaving] = useState(false);

    useEffect(() => {
        loadCompany();
    }, []);

    const loadCompany = async () => {
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
                    state: company.state ?? "",
                    country: company.country ?? "India",
                    pincode: company.pincode ?? "",
                    phone: company.phone ?? "",
                    email: company.email ?? "",
                    website: company.website ?? "",
                    logoPath: company.logoPath ?? "",
                    invoicePrefix: company.invoicePrefix || "INV",
                    currency: company.currency || "INR",
                    bankName: company.bankName ?? "",
                    accountNumber: company.accountNumber ?? "",
                    ifscCode: company.ifscCode ?? "",
                    upiId: company.upiId ?? "",
                    termsAndConditions:
                        company.termsAndConditions ?? "",
                    footerMessage:
                        company.footerMessage ?? "",
                    isActive: company.isActive,
                });
            }
        } catch {
            toast.error("Unable to load company settings.");
        } finally {
            setLoading(false);
        }
    };

    const handleChange = (
        e: ChangeEvent<
            HTMLInputElement | HTMLTextAreaElement | HTMLSelectElement
        >
    ) => {
        const { name, value } = e.target;

        setForm((previous) => ({
            ...previous,
            [name]: value,
        }));
    };

    const handleActiveChange = (
        e: ChangeEvent<HTMLInputElement>
    ) => {
        setForm((previous) => ({
            ...previous,
            isActive: e.target.checked,
        }));
    };

    const handleSubmit = async (
        e: FormEvent<HTMLFormElement>
    ) => {
        e.preventDefault();

        if (!form.companyName.trim()) {
            toast.error("Company name is required.");
            return;
        }

        if (!form.invoicePrefix.trim()) {
            toast.error("Invoice prefix is required.");
            return;
        }

        if (!form.currency.trim()) {
            toast.error("Currency is required.");
            return;
        }

        try {
            setSaving(true);
            await saveCompany(form);
            toast.success(
                "Company settings saved successfully."
            );

            await loadCompany();
        } catch (error: any) {
            if (error.response?.data?.errors) {
                const errors =
                    error.response.data.errors;

                Object.values(errors)
                    .flat()
                    .forEach((message) => {
                        toast.error(String(message));
                    });
            } else {
                toast.error(
                    "Unable to save company settings."
                );
            }
        } finally {
            setSaving(false);
        }
    };

    if (loading) {
        return (
            <div className="company-loading">
                <div
                    className="spinner-border text-primary"
                    role="status"
                >
                    <span className="visually-hidden">
                        Loading...
                    </span>
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
                        Manage your business information,
                        invoice and payment details.
                    </p>
                </div>
            </div>

            <form onSubmit={handleSubmit}>

                {/* Company Information */}

                <div className="company-card">

                    <div className="company-card-header">
                        <div className="section-icon">
                            <i className="bi bi-building"></i>
                        </div>

                        <div>
                            <h5>Company Information</h5>
                            <span>
                                Basic business information
                            </span>
                        </div>
                    </div>

                    <div className="row g-4">

                        <div className="col-md-8">
                            <label className="form-label">
                                Company Name
                                <span className="required">*</span>
                            </label>

                            <input
                                type="text"
                                name="companyName"
                                className="form-control"
                                value={form.companyName}
                                onChange={handleChange}
                                maxLength={200}
                                placeholder="Enter company name"
                                required
                            />
                        </div>

                        <div className="col-md-4">
                            <label className="form-label">
                                Invoice Prefix
                                <span className="required">*</span>
                            </label>

                            <input
                                type="text"
                                name="invoicePrefix"
                                className="form-control"
                                value={form.invoicePrefix}
                                onChange={handleChange}
                                maxLength={10}
                                placeholder="INV"
                                required
                            />
                        </div>

                        <div className="col-md-6">
                            <label className="form-label">
                                GST Number
                            </label>

                            <input
                                type="text"
                                name="gstNumber"
                                className="form-control"
                                value={form.gstNumber}
                                onChange={handleChange}
                                maxLength={15}
                                placeholder="Enter GST number"
                            />
                        </div>

                        <div className="col-md-6">
                            <label className="form-label">
                                PAN Number
                            </label>

                            <input
                                type="text"
                                name="panNumber"
                                className="form-control"
                                value={form.panNumber}
                                onChange={handleChange}
                                maxLength={10}
                                placeholder="Enter PAN number"
                            />
                        </div>

                        <div className="col-md-4">
                            <label className="form-label">
                                Phone
                            </label>

                            <input
                                type="tel"
                                name="phone"
                                className="form-control"
                                value={form.phone}
                                onChange={handleChange}
                                placeholder="Enter phone number"
                            />
                        </div>

                        <div className="col-md-4">
                            <label className="form-label">
                                Email
                            </label>

                            <input
                                type="email"
                                name="email"
                                className="form-control"
                                value={form.email}
                                onChange={handleChange}
                                placeholder="company@example.com"
                            />
                        </div>

                        <div className="col-md-4">
                            <label className="form-label">
                                Website
                            </label>

                            <input
                                type="url"
                                name="website"
                                className="form-control"
                                value={form.website}
                                onChange={handleChange}
                                placeholder="https://example.com"
                            />
                        </div>

                    </div>
                </div>

                {/* Address */}

                <div className="company-card">

                    <div className="company-card-header">
                        <div className="section-icon">
                            <i className="bi bi-geo-alt"></i>
                        </div>

                        <div>
                            <h5>Business Address</h5>
                            <span>
                                Address shown on invoices
                            </span>
                        </div>
                    </div>

                    <div className="row g-4">

                        <div className="col-12">
                            <label className="form-label">
                                Address
                            </label>

                            <textarea
                                name="address"
                                className="form-control"
                                rows={3}
                                value={form.address}
                                onChange={handleChange}
                                maxLength={500}
                                placeholder="Enter complete business address"
                            />
                        </div>

                        <div className="col-md-4">
                            <label className="form-label">
                                City
                            </label>

                            <input
                                type="text"
                                name="city"
                                className="form-control"
                                value={form.city}
                                onChange={handleChange}
                                placeholder="City"
                            />
                        </div>

                        <div className="col-md-4">
                            <label className="form-label">
                                State
                            </label>

                            <input
                                type="text"
                                name="state"
                                className="form-control"
                                value={form.state}
                                onChange={handleChange}
                                placeholder="State"
                            />
                        </div>

                        <div className="col-md-4">
                            <label className="form-label">
                                Country
                            </label>

                            <input
                                type="text"
                                name="country"
                                className="form-control"
                                value={form.country}
                                onChange={handleChange}
                                placeholder="Country"
                            />
                        </div>

                        <div className="col-md-4">
                            <label className="form-label">
                                Pincode
                            </label>

                            <input
                                type="text"
                                name="pincode"
                                className="form-control"
                                value={form.pincode}
                                onChange={handleChange}
                                maxLength={10}
                                placeholder="Pincode"
                            />
                        </div>

                    </div>
                </div>

                {/* Invoice & Currency */}

                <div className="company-card">

                    <div className="company-card-header">
                        <div className="section-icon">
                            <i className="bi bi-receipt"></i>
                        </div>

                        <div>
                            <h5>Invoice Settings</h5>
                            <span>
                                Invoice and currency configuration
                            </span>
                        </div>
                    </div>

                    <div className="row g-4">

                        <div className="col-md-6">
                            <label className="form-label">
                                Invoice Prefix
                                <span className="required">*</span>
                            </label>

                            <input
                                type="text"
                                name="invoicePrefix"
                                className="form-control"
                                value={form.invoicePrefix}
                                onChange={handleChange}
                                maxLength={10}
                                required
                            />
                        </div>

                        <div className="col-md-6">
                            <label className="form-label">
                                Currency
                                <span className="required">*</span>
                            </label>

                            <select
                                name="currency"
                                className="form-select"
                                value={form.currency}
                                onChange={handleChange}
                                required
                            >
                                <option value="INR">
                                    INR - Indian Rupee
                                </option>
                                <option value="USD">
                                    USD - US Dollar
                                </option>
                                <option value="EUR">
                                    EUR - Euro
                                </option>
                                <option value="GBP">
                                    GBP - British Pound
                                </option>
                            </select>
                        </div>

                    </div>
                </div>

                {/* Bank Details */}

                <div className="company-card">

                    <div className="company-card-header">
                        <div className="section-icon">
                            <i className="bi bi-bank"></i>
                        </div>

                        <div>
                            <h5>Bank & Payment Details</h5>
                            <span>
                                Payment information for invoices
                            </span>
                        </div>
                    </div>

                    <div className="row g-4">

                        <div className="col-md-6">
                            <label className="form-label">
                                Bank Name
                            </label>

                            <input
                                type="text"
                                name="bankName"
                                className="form-control"
                                value={form.bankName}
                                onChange={handleChange}
                                placeholder="Bank name"
                            />
                        </div>

                        <div className="col-md-6">
                            <label className="form-label">
                                Account Number
                            </label>

                            <input
                                type="text"
                                name="accountNumber"
                                className="form-control"
                                value={form.accountNumber}
                                onChange={handleChange}
                                placeholder="Account number"
                            />
                        </div>

                        <div className="col-md-6">
                            <label className="form-label">
                                IFSC Code
                            </label>

                            <input
                                type="text"
                                name="ifscCode"
                                className="form-control"
                                value={form.ifscCode}
                                onChange={handleChange}
                                placeholder="IFSC code"
                            />
                        </div>

                        <div className="col-md-6">
                            <label className="form-label">
                                UPI ID
                            </label>

                            <input
                                type="text"
                                name="upiId"
                                className="form-control"
                                value={form.upiId}
                                onChange={handleChange}
                                placeholder="example@upi"
                            />
                        </div>

                    </div>
                </div>

                {/* Invoice Text */}

                <div className="company-card">

                    <div className="company-card-header">
                        <div className="section-icon">
                            <i className="bi bi-file-text"></i>
                        </div>

                        <div>
                            <h5>Invoice Text</h5>
                            <span>
                                Terms and footer information
                            </span>
                        </div>
                    </div>

                    <div className="row g-4">

                        <div className="col-12">
                            <label className="form-label">
                                Terms & Conditions
                            </label>

                            <textarea
                                name="termsAndConditions"
                                className="form-control"
                                rows={5}
                                value={form.termsAndConditions}
                                onChange={handleChange}
                                maxLength={1000}
                                placeholder="Enter invoice terms and conditions"
                            />
                        </div>

                        <div className="col-12">
                            <label className="form-label">
                                Footer Message
                            </label>

                            <textarea
                                name="footerMessage"
                                className="form-control"
                                rows={3}
                                value={form.footerMessage}
                                onChange={handleChange}
                                maxLength={500}
                                placeholder="Thank you for your business!"
                            />
                        </div>

                    </div>
                </div>

                {/* Logo */}

                <div className="company-card">

                    <div className="company-card-header">
                        <div className="section-icon">
                            <i className="bi bi-image"></i>
                        </div>

                        <div>
                            <h5>Company Logo</h5>
                            <span>
                                Logo configuration
                            </span>
                        </div>
                    </div>

                    <div className="row g-4">

                        <div className="col-md-8">
                            <label className="form-label">
                                Logo Path
                            </label>

                            <input
                                type="text"
                                name="logoPath"
                                className="form-control"
                                value={form.logoPath}
                                onChange={handleChange}
                                placeholder="Logo path / URL"
                            />

                            <small className="text-muted">
                                Actual image upload will be
                                implemented with a dedicated
                                backend upload API.
                            </small>
                        </div>

                    </div>
                </div>

                {/* Status */}

                <div className="company-card">

                    <div className="company-status-row">

                        <div>
                            <h5>Company Status</h5>
                            <p>
                                Enable or disable this company
                                configuration.
                            </p>
                        </div>

                        <div className="form-check form-switch">
                            <input
                                className="form-check-input"
                                type="checkbox"
                                role="switch"
                                checked={form.isActive}
                                onChange={handleActiveChange}
                            />

                            <label className="form-check-label">
                                {form.isActive
                                    ? "Active"
                                    : "Inactive"}
                            </label>
                        </div>

                    </div>

                </div>

                {/* Save */}

                <div className="company-actions">

                    <button
                        type="submit"
                        className="btn btn-primary px-4"
                        disabled={saving}
                    >
                        {saving ? (
                            <>
                                <span
                                    className="spinner-border spinner-border-sm me-2"
                                ></span>

                                Saving...
                            </>
                        ) : (
                            <>
                                <i className="bi bi-check-lg me-2"></i>
                                Save Company Settings
                            </>
                        )}
                    </button>

                </div>

            </form>

        </div>
    );
}

export default CompanySettingsPage;