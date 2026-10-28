import { useEffect, useId, useRef } from "react";
import type { MouseEvent, ReactNode } from "react";

interface ModalProps {
    show: boolean;
    title: string;
    onClose: () => void;
    children: ReactNode;
    footer?: ReactNode;
    /** When true the modal cannot be dismissed (e.g. while saving). */
    busy?: boolean;
    size?: "md" | "lg" | "xl";
}

/** Bootstrap-styled modal controlled by React (no Bootstrap JS needed). */
function Modal({
    show,
    title,
    onClose,
    children,
    footer,
    busy = false,
    size = "md",
}: ModalProps) {
    const titleId = useId();
    const dialogRef = useRef<HTMLDivElement>(null);

    // Keep the latest callbacks in a ref so the effect below only runs when
    // the modal opens/closes. Re-running it on every parent render would
    // move focus back to the dialog while the user is typing.
    const latest = useRef({ onClose, busy });

    useEffect(() => {
        latest.current = { onClose, busy };
    });

    useEffect(() => {
        if (!show) {
            return;
        }

        const previouslyFocused = document.activeElement as
            | HTMLElement
            | null;

        document.body.classList.add("modal-open");

        // Respect an autoFocus field inside the modal; otherwise focus the dialog.
        if (!dialogRef.current?.contains(document.activeElement)) {
            dialogRef.current?.focus();
        }

        const onKeyDown = (event: KeyboardEvent) => {
            if (event.key === "Escape" && !latest.current.busy) {
                latest.current.onClose();
            }
        };

        document.addEventListener("keydown", onKeyDown);

        return () => {
            document.removeEventListener("keydown", onKeyDown);
            document.body.classList.remove("modal-open");
            previouslyFocused?.focus?.();
        };
    }, [show]);

    if (!show) {
        return null;
    }

    const handleBackdropClick = (event: MouseEvent<HTMLDivElement>) => {
        if (event.target === event.currentTarget && !busy) {
            onClose();
        }
    };

    return (
        <>
            <div className="modal-backdrop fade show"></div>

            <div
                className="modal fade show d-block"
                role="dialog"
                aria-modal="true"
                aria-labelledby={titleId}
                tabIndex={-1}
                onMouseDown={handleBackdropClick}
            >
                <div
                    className={`modal-dialog modal-dialog-centered modal-dialog-scrollable${
                        size === "lg"
                            ? " modal-lg"
                            : size === "xl"
                              ? " modal-xl"
                              : ""
                    }`}
                >
                    <div
                        className="modal-content"
                        ref={dialogRef}
                        tabIndex={-1}
                    >
                        <div className="modal-header">
                            <h5 className="modal-title" id={titleId}>
                                {title}
                            </h5>

                            <button
                                type="button"
                                className="btn-close"
                                aria-label="Close"
                                onClick={onClose}
                                disabled={busy}
                            ></button>
                        </div>

                        <div className="modal-body">{children}</div>

                        {footer && (
                            <div className="modal-footer">{footer}</div>
                        )}
                    </div>
                </div>
            </div>
        </>
    );
}

export default Modal;
