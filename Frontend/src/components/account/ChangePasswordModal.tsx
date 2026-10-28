import { useState } from "react";
import { toast } from "react-toastify";

import Modal from "../common/Modal";

import { changePassword } from "../../services/auth/authService";
import { getApiErrorMessage } from "../../utils/apiError";

interface ChangePasswordModalProps {
    onClose: () => void;
}

function ChangePasswordModal({ onClose }: ChangePasswordModalProps) {
    const [currentPassword, setCurrentPassword] = useState("");
    const [newPassword, setNewPassword] = useState("");
    const [confirm, setConfirm] = useState("");
    const [saving, setSaving] = useState(false);

    const save = async () => {
        if (newPassword.length < 8) {
            toast.error("The new password must be at least 8 characters.");
            return;
        }

        if (newPassword !== confirm) {
            toast.error("The new password and confirmation do not match.");
            return;
        }

        try {
            setSaving(true);
            await changePassword(currentPassword, newPassword);
            toast.success("Password changed.");
            onClose();
        } catch (error) {
            toast.error(getApiErrorMessage(error, "Unable to change the password."));
        } finally {
            setSaving(false);
        }
    };

    return (
        <Modal
            show
            title="Change password"
            busy={saving}
            onClose={onClose}
            footer={
                <>
                    <button
                        type="button"
                        className="btn btn-outline-secondary"
                        disabled={saving}
                        onClick={onClose}
                    >
                        Cancel
                    </button>
                    <button
                        type="button"
                        className="btn btn-primary"
                        disabled={saving}
                        onClick={() => void save()}
                    >
                        {saving ? "Saving..." : "Change password"}
                    </button>
                </>
            }
        >
            <div className="mb-3">
                <label className="form-label" htmlFor="current-password">
                    Current password
                </label>
                <input
                    id="current-password"
                    type="password"
                    autoComplete="current-password"
                    className="form-control"
                    value={currentPassword}
                    onChange={(e) => setCurrentPassword(e.target.value)}
                />
            </div>
            <div className="mb-3">
                <label className="form-label" htmlFor="new-password">
                    New password
                </label>
                <input
                    id="new-password"
                    type="password"
                    autoComplete="new-password"
                    className="form-control"
                    value={newPassword}
                    onChange={(e) => setNewPassword(e.target.value)}
                />
            </div>
            <div>
                <label className="form-label" htmlFor="confirm-password">
                    Confirm new password
                </label>
                <input
                    id="confirm-password"
                    type="password"
                    autoComplete="new-password"
                    className="form-control"
                    value={confirm}
                    onChange={(e) => setConfirm(e.target.value)}
                />
            </div>
        </Modal>
    );
}

export default ChangePasswordModal;
