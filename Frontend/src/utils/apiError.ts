import { isAxiosError } from "axios";

/**
 * Error bodies the API can return:
 *  - ASP.NET validation (400):  { errors: { Field: ["message"] } }
 *  - Global middleware:         { success: false, message, errors: ["message"] }
 */
interface ApiErrorBody {
    message?: string;
    errors?: Record<string, string[]> | string[];
}

const NETWORK_ERROR =
    "Cannot reach the server. Check your connection and try again.";

export function getApiStatus(error: unknown): number | undefined {
    return isAxiosError(error) ? error.response?.status : undefined;
}

function firstValidationMessage(
    errors: ApiErrorBody["errors"]
): string | undefined {
    if (!errors) {
        return undefined;
    }

    const messages = Array.isArray(errors)
        ? errors
        : Object.values(errors).flat();

    return messages.find((message) => message.trim().length > 0);
}

/** Best human-readable message for an API failure. */
export function getApiErrorMessage(
    error: unknown,
    fallback: string
): string {
    if (!isAxiosError<ApiErrorBody>(error)) {
        return fallback;
    }

    if (!error.response) {
        return NETWORK_ERROR;
    }

    const body = error.response.data;

    if (body && typeof body === "object") {
        // Business and validation errors carry a specific message.
        // 5xx bodies only carry a generic message, so prefer the fallback.
        if (error.response.status < 500) {
            const specific =
                firstValidationMessage(body.errors) ?? body.message;

            if (specific) {
                return specific;
            }
        }
    }

    if (error.response.status === 403) {
        return "You do not have permission to do this.";
    }

    return fallback;
}

/**
 * Field-level validation errors (ASP.NET ModelState) keyed by the
 * property name the server used, e.g. { SKU: "SKU is required." }.
 */
export function getFieldErrors(error: unknown): Record<string, string> {
    const result: Record<string, string> = {};

    if (!isAxiosError<ApiErrorBody>(error)) {
        return result;
    }

    const errors = error.response?.data?.errors;

    if (errors && !Array.isArray(errors)) {
        for (const [field, messages] of Object.entries(errors)) {
            if (messages.length > 0) {
                result[field] = messages[0];
            }
        }
    }

    return result;
}
