import type { ReactNode } from "react";

import Modal from "./Modal";

interface ConfirmDialogProps {
    show: boolean;
    title: string;
    message: ReactNode;
    confirmLabel?: string;
    busy?: boolean;
    onConfirm: () => void;
    onCancel: () => void;
}

function ConfirmDialog({
    show,
    title,
    message,
    confirmLabel = "Delete",
    busy = false,
    onConfirm,
    onCancel,
}: ConfirmDialogProps) {
    return (
        <Modal
            show={show}
            title={title}
            onClose={onCancel}
            busy={busy}
            footer={
                <>
                    <button
                        type="button"
                        className="btn btn-outline-secondary"
                        onClick={onCancel}
                        disabled={busy}
                    >
                        Cancel
                    </button>

                    <button
                        type="button"
                        className="btn btn-danger"
                        onClick={onConfirm}
                        disabled={busy}
                    >
                        {busy && (
                            <span
                                className="spinner-border spinner-border-sm me-2"
                                aria-hidden="true"
                            ></span>
                        )}
                        {confirmLabel}
                    </button>
                </>
            }
        >
            {message}
        </Modal>
    );
}

export default ConfirmDialog;
