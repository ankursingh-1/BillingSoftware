import type { ReactNode } from "react";

import Modal from "./Modal";

export interface DetailItem {
    label: string;
    value: string | null | undefined;
}

interface DetailsModalProps {
    show: boolean;
    title: string;
    items: DetailItem[];
    /** Optional extra heading shown above the details. */
    heading?: ReactNode;
    onClose: () => void;
    onEdit?: () => void;
}

/** Read-only view of a record. Blank values show a dash. */
function DetailsModal({
    show,
    title,
    items,
    heading,
    onClose,
    onEdit,
}: DetailsModalProps) {
    return (
        <Modal
            show={show}
            title={title}
            onClose={onClose}
            footer={
                <>
                    <button
                        type="button"
                        className="btn btn-outline-secondary"
                        onClick={onClose}
                    >
                        Close
                    </button>

                    {onEdit && (
                        <button
                            type="button"
                            className="btn btn-primary"
                            onClick={onEdit}
                        >
                            <i className="bi bi-pencil me-2"></i>
                            Edit
                        </button>
                    )}
                </>
            }
        >
            {heading}

            <dl className="details-list mb-0">
                {items.map((item) => (
                    <div className="details-row" key={item.label}>
                        <dt>{item.label}</dt>
                        <dd>
                            {item.value ? (
                                item.value
                            ) : (
                                <span className="text-muted">—</span>
                            )}
                        </dd>
                    </div>
                ))}
            </dl>
        </Modal>
    );
}

export default DetailsModal;
