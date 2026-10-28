import { useForm } from "react-hook-form";
import { toast } from "react-toastify";

import Modal from "../common/Modal";

import {
    createCategory,
    updateCategory,
} from "../../services/categories/categoryService";
import {
    getApiErrorMessage,
    getApiStatus,
    getFieldErrors,
} from "../../utils/apiError";

import type {
    Category,
    SaveCategoryRequest,
} from "../../interfaces/category/category";

interface CategoryFormModalProps {
    /** Category being edited, or null to create a new one. */
    category: Category | null;
    onClose: () => void;
    onSaved: () => void;
}

const FORM_ID = "category-form";

function CategoryFormModal({
    category,
    onClose,
    onSaved,
}: CategoryFormModalProps) {
    const isEdit = category !== null;

    const {
        register,
        handleSubmit,
        setError,
        formState: { errors, isSubmitting },
    } = useForm<SaveCategoryRequest>({
        defaultValues: {
            name: category?.name ?? "",
            description: category?.description ?? "",
            isActive: category?.isActive ?? true,
        },
    });

    const onSubmit = async (values: SaveCategoryRequest) => {
        const payload: SaveCategoryRequest = {
            ...values,
            name: values.name.trim(),
            description: values.description.trim(),
        };

        try {
            if (category) {
                await updateCategory(category.id, payload);
                toast.success("Category updated.");
            } else {
                await createCategory(payload);
                toast.success("Category created.");
            }

            onSaved();
        } catch (error) {
            const fieldErrors = getFieldErrors(error);
            let handled = false;

            for (const [field, message] of Object.entries(fieldErrors)) {
                const key = field.toLowerCase();

                if (key === "name" || key === "description") {
                    setError(key, { type: "server", message });
                    handled = true;
                }
            }

            if (handled) {
                return;
            }

            const message = getApiErrorMessage(
                error,
                "Unable to save the category."
            );

            if (getApiStatus(error) === 409) {
                // e.g. duplicate name
                setError("name", { type: "server", message });
            } else {
                toast.error(message);
            }
        }
    };

    return (
        <Modal
            show
            title={isEdit ? "Edit Category" : "Add Category"}
            onClose={onClose}
            busy={isSubmitting}
            footer={
                <>
                    <button
                        type="button"
                        className="btn btn-outline-secondary"
                        onClick={onClose}
                        disabled={isSubmitting}
                    >
                        Cancel
                    </button>

                    <button
                        type="submit"
                        form={FORM_ID}
                        className="btn btn-primary"
                        disabled={isSubmitting}
                    >
                        {isSubmitting && (
                            <span
                                className="spinner-border spinner-border-sm me-2"
                                aria-hidden="true"
                            ></span>
                        )}
                        {isEdit ? "Save Changes" : "Create Category"}
                    </button>
                </>
            }
        >
            <form
                id={FORM_ID}
                onSubmit={handleSubmit(onSubmit)}
                noValidate
            >
                <div className="mb-3">
                    <label htmlFor="category-name" className="form-label">
                        Name <span className="text-danger">*</span>
                    </label>

                    <input
                        id="category-name"
                        type="text"
                        className={`form-control${
                            errors.name ? " is-invalid" : ""
                        }`}
                        autoFocus
                        aria-invalid={errors.name ? "true" : "false"}
                        {...register("name", {
                            required: "Category name is required.",
                            validate: (value) =>
                                value.trim().length > 0 ||
                                "Category name is required.",
                            maxLength: {
                                value: 100,
                                message:
                                    "Category name cannot exceed 100 characters.",
                            },
                        })}
                    />

                    {errors.name && (
                        <div className="invalid-feedback" role="alert">
                            {errors.name.message}
                        </div>
                    )}
                </div>

                <div className="mb-3">
                    <label
                        htmlFor="category-description"
                        className="form-label"
                    >
                        Description
                    </label>

                    <textarea
                        id="category-description"
                        rows={3}
                        className={`form-control${
                            errors.description ? " is-invalid" : ""
                        }`}
                        aria-invalid={
                            errors.description ? "true" : "false"
                        }
                        {...register("description", {
                            maxLength: {
                                value: 500,
                                message:
                                    "Description cannot exceed 500 characters.",
                            },
                        })}
                    />

                    {errors.description && (
                        <div className="invalid-feedback" role="alert">
                            {errors.description.message}
                        </div>
                    )}
                </div>

                <div className="form-check form-switch">
                    <input
                        id="category-active"
                        type="checkbox"
                        role="switch"
                        className="form-check-input"
                        {...register("isActive")}
                    />

                    <label
                        htmlFor="category-active"
                        className="form-check-label"
                    >
                        Active
                    </label>

                    <div className="form-text">
                        Inactive categories stay on existing products but
                        are not offered for new ones.
                    </div>
                </div>
            </form>
        </Modal>
    );
}

export default CategoryFormModal;
