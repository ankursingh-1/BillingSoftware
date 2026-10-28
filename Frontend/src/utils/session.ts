// UI-only helpers. The API enforces every permission; hiding a button here
// is a convenience, never the security boundary.

const ROLE_CLAIMS = [
    "http://schemas.microsoft.com/ws/2008/06/identity/claims/role",
    "role",
];

function readPayload(): Record<string, unknown> | null {
    const token = localStorage.getItem("token");
    const part = token?.split(".")[1];

    if (!part) {
        return null;
    }

    try {
        return JSON.parse(atob(part.replace(/-/g, "+").replace(/_/g, "/")));
    } catch {
        return null;
    }
}

export function getCurrentRoles(): string[] {
    const payload = readPayload();

    if (!payload) {
        return [];
    }

    for (const claim of ROLE_CLAIMS) {
        const value = payload[claim];

        if (typeof value === "string") {
            return [value];
        }

        if (Array.isArray(value)) {
            return value.filter((v): v is string => typeof v === "string");
        }
    }

    return [];
}

export const isAdmin = (): boolean => getCurrentRoles().includes("Admin");