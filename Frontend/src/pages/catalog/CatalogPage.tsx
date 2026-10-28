import { useEffect, useMemo, useState } from "react";
import { toast } from "react-toastify";

import ConfirmDialog from "../../components/common/ConfirmDialog";
import Modal from "../../components/common/Modal";

import {
    getBrands,
    getTaxes,
    getUnits,
} from "../../services/lookups/lookupService";
import {
    createBrand,
    createTax,
    createUnit,
    deleteBrand,
    deleteTax,
    deleteUnit,
    updateBrand,
    updateTax,
    updateUnit,
} from "../../services/catalog/catalogService";
import { getApiErrorMessage } from "../../utils/apiError";
import { emptyToNull } from "../../utils/validation";

import type { Brand, Tax, Unit } from "../../interfaces/lookups/lookups";

import "../../styles/data-pages.css";

type Tab = "brands" | "units" | "taxes";

function CatalogPage() {
    const [tab, setTab] = useState<Tab>("brands");
    const [brands, setBrands] = useState<Brand[]>([]);
    const [units, setUnits] = useState<Unit[]>([]);
    const [taxes, setTaxes] = useState<Tax[]>([]);
    const [loading, setLoading] = useState(true);
    const [reload, setReload] = useState(0);
    const [editing, setEditing] = useState<
        | { kind: "brand"; item: Brand | null }
        | { kind: "unit"; item: Unit | null }
        | { kind: "tax"; item: Tax | null }
        | null
    >(null);
    const [name, setName] = useState("");
    const [shortName, setShortName] = useState("");
    const [description, setDescription] = useState("");
    const [percentage, setPercentage] = useState("18");
    const [active, setActive] = useState(true);
    const [saving, setSaving] = useState(false);
    const [deleteTarget, setDeleteTarget] = useState<
        | { kind: Tab; id: number; name: string }
        | null
    >(null);
    const [deleting, setDeleting] = useState(false);

    useEffect(() => {
        let cancelled = false;

        const load = async () => {
            setLoading(true);

            try {
                const [brandRows, unitRows, taxRows] = await Promise.all([
                    getBrands(),
                    getUnits(),
                    getTaxes(),
                ]);

                if (!cancelled) {
                    setBrands(brandRows);
                    setUnits(unitRows);
                    setTaxes(taxRows);
                }
            } catch (error) {
                if (!cancelled) {
                    toast.error(
                        getApiErrorMessage(error, "Unable to load catalog data.")
                    );
                }
            } finally {
                if (!cancelled) setLoading(false);
            }
        };

        void load();

        return () => {
            cancelled = true;
        };
    }, [reload]);

    const openCreate = (kind: Tab) => {
        setName("");
        setShortName("");
        setDescription("");
        setPercentage("18");
        setActive(true);
        if (kind === "brands") setEditing({ kind: "brand", item: null });
        if (kind === "units") setEditing({ kind: "unit", item: null });
        if (kind === "taxes") setEditing({ kind: "tax", item: null });
    };

    const openEdit = (kind: Tab, id: number) => {
        if (kind === "brands") {
            const item = brands.find((row) => row.id === id);
            if (!item) return;
            setName(item.name);
            setDescription(item.description ?? "");
            setActive(item.isActive);
            setEditing({ kind: "brand", item });
        } else if (kind === "units") {
            const item = units.find((row) => row.id === id);
            if (!item) return;
            setName(item.name);
            setShortName(item.shortName ?? "");
            setDescription(item.description ?? "");
            setActive(item.isActive);
            setEditing({ kind: "unit", item });
        } else {
            const item = taxes.find((row) => row.id === id);
            if (!item) return;
            setName(item.name);
            setDescription(item.description ?? "");
            setPercentage(String(item.percentage));
            setActive(item.isActive);
            setEditing({ kind: "tax", item });
        }
    };

    const save = async () => {
        if (!editing) return;

        if (!name.trim()) {
            toast.error("Name is required.");
            return;
        }

        try {
            setSaving(true);

            if (editing.kind === "brand") {
                const payload = {
                    name: name.trim(),
                    description: emptyToNull(description),
                    isActive: active,
                };
                if (editing.item) await updateBrand(editing.item.id, payload);
                else await createBrand(payload);
            } else if (editing.kind === "unit") {
                const payload = {
                    name: name.trim(),
                    shortName: emptyToNull(shortName),
                    description: emptyToNull(description),
                    isActive: active,
                };
                if (editing.item) await updateUnit(editing.item.id, payload);
                else await createUnit(payload);
            } else {
                const rate = Number(percentage);
                if (!(rate >= 0 && rate <= 100)) {
                    toast.error("Tax percentage must be between 0 and 100.");
                    return;
                }
                const payload = {
                    name: name.trim(),
                    percentage: rate,
                    description: emptyToNull(description),
                    isActive: active,
                };
                if (editing.item) await updateTax(editing.item.id, payload);
                else await createTax(payload);
            }

            toast.success("Saved.");
            setEditing(null);
            setReload((key) => key + 1);
        } catch (error) {
            toast.error(getApiErrorMessage(error, "Unable to save."));
        } finally {
            setSaving(false);
        }
    };

    const rows = useMemo(() => {
        if (tab === "brands") return brands;
        if (tab === "units") return units;
        return taxes;
    }, [tab, brands, units, taxes]);

    return (
        <div className="data-page">
            <div className="data-page-header">
                <div>
                    <h2 className="page-title">Brands, units and tax</h2>
                    <p className="data-page-subtitle">
                        These lists feed product forms and Excel import Lookups.
                        Inactive items stay on existing products but cannot be
                        chosen for new ones.
                    </p>
                </div>
                <button
                    type="button"
                    className="btn btn-primary"
                    onClick={() => openCreate(tab)}
                >
                    Add {tab === "brands" ? "brand" : tab === "units" ? "unit" : "tax"}
                </button>
            </div>

            <div className="data-card">
                <div className="data-toolbar">
                    <div className="d-flex flex-wrap gap-2">
                        {(
                            [
                                ["brands", "Brands"],
                                ["units", "Units"],
                                ["taxes", "Tax rates"],
                            ] as const
                        ).map(([id, label]) => (
                            <button
                                key={id}
                                type="button"
                                className={`btn btn-sm ${
                                    tab === id ? "btn-primary" : "btn-outline-secondary"
                                }`}
                                onClick={() => setTab(id)}
                            >
                                {label}
                            </button>
                        ))}
                    </div>
                </div>

                {loading ? (
                    <div className="state-block">
                        <div className="spinner-border text-primary" role="status">
                            <span className="visually-hidden">Loading...</span>
                        </div>
                    </div>
                ) : (
                    <div className="table-responsive">
                        <table className="table align-middle mb-0">
                            <thead>
                                <tr>
                                    <th>Name</th>
                                    {tab === "units" && <th>Short name</th>}
                                    {tab === "taxes" && <th>Rate %</th>}
                                    <th>Status</th>
                                    <th />
                                </tr>
                            </thead>
                            <tbody>
                                {rows.length === 0 ? (
                                    <tr>
                                        <td colSpan={4} className="text-muted p-4">
                                            Nothing here yet.
                                        </td>
                                    </tr>
                                ) : (
                                    rows.map((row) => (
                                        <tr key={row.id}>
                                            <td>{row.name}</td>
                                            {tab === "units" && (
                                                <td>
                                                    {(row as Unit).shortName ??
                                                        "—"}
                                                </td>
                                            )}
                                            {tab === "taxes" && (
                                                <td>
                                                    {(row as Tax).percentage}
                                                </td>
                                            )}
                                            <td>
                                                {row.isActive ? "Active" : "Inactive"}
                                            </td>
                                            <td className="text-end">
                                                <button
                                                    type="button"
                                                    className="btn btn-sm btn-outline-primary me-2"
                                                    onClick={() => openEdit(tab, row.id)}
                                                >
                                                    Edit
                                                </button>
                                                <button
                                                    type="button"
                                                    className="btn btn-sm btn-outline-danger"
                                                    onClick={() =>
                                                        setDeleteTarget({
                                                            kind: tab,
                                                            id: row.id,
                                                            name: row.name,
                                                        })
                                                    }
                                                >
                                                    Delete
                                                </button>
                                            </td>
                                        </tr>
                                    ))
                                )}
                            </tbody>
                        </table>
                    </div>
                )}
            </div>

            {editing && (
                <Modal
                    show
                    title={editing.item ? "Edit" : "Add"}
                    busy={saving}
                    onClose={() => setEditing(null)}
                    footer={
                        <>
                            <button
                                type="button"
                                className="btn btn-outline-secondary"
                                disabled={saving}
                                onClick={() => setEditing(null)}
                            >
                                Cancel
                            </button>
                            <button
                                type="button"
                                className="btn btn-primary"
                                disabled={saving}
                                onClick={() => void save()}
                            >
                                {saving ? "Saving..." : "Save"}
                            </button>
                        </>
                    }
                >
                    <div className="mb-3">
                        <label className="form-label">Name</label>
                        <input
                            className="form-control"
                            value={name}
                            onChange={(e) => setName(e.target.value)}
                        />
                    </div>
                    {editing.kind === "unit" && (
                        <div className="mb-3">
                            <label className="form-label">Short name</label>
                            <input
                                className="form-control"
                                value={shortName}
                                onChange={(e) => setShortName(e.target.value)}
                            />
                        </div>
                    )}
                    {editing.kind === "tax" && (
                        <div className="mb-3">
                            <label className="form-label">Percentage</label>
                            <input
                                className="form-control"
                                type="number"
                                min={0}
                                max={100}
                                step="0.01"
                                value={percentage}
                                onChange={(e) => setPercentage(e.target.value)}
                            />
                        </div>
                    )}
                    <div className="mb-3">
                        <label className="form-label">Description</label>
                        <input
                            className="form-control"
                            value={description}
                            onChange={(e) => setDescription(e.target.value)}
                        />
                    </div>
                    <div className="form-check">
                        <input
                            id="catalog-active"
                            className="form-check-input"
                            type="checkbox"
                            checked={active}
                            onChange={(e) => setActive(e.target.checked)}
                        />
                        <label className="form-check-label" htmlFor="catalog-active">
                            Active
                        </label>
                    </div>
                </Modal>
            )}

            <ConfirmDialog
                show={deleteTarget !== null}
                title="Delete this item?"
                message={
                    deleteTarget
                        ? `${deleteTarget.name} will be hidden from new products.`
                        : ""
                }
                confirmLabel="Delete"
                busy={deleting}
                onCancel={() => setDeleteTarget(null)}
                onConfirm={() => {
                    if (!deleteTarget) return;
                    setDeleting(true);
                    const work =
                        deleteTarget.kind === "brands"
                            ? deleteBrand(deleteTarget.id)
                            : deleteTarget.kind === "units"
                              ? deleteUnit(deleteTarget.id)
                              : deleteTax(deleteTarget.id);
                    void work
                        .then(() => {
                            toast.success("Deleted.");
                            setDeleteTarget(null);
                            setReload((key) => key + 1);
                        })
                        .catch((error) =>
                            toast.error(
                                getApiErrorMessage(error, "Unable to delete.")
                            )
                        )
                        .finally(() => setDeleting(false));
                }}
            />
        </div>
    );
}

export default CatalogPage;
