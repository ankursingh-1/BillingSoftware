// UI-only helpers. The API enforces every permission; hiding a button here
// is a convenience, never the security boundary.

const ROLE_CLAIMS = [
    "http://schemas.microsoft.com/ws/2008/06/identity/claims/role",
    "role",
];

/** JWT segments are base64url and often omit the '=' padding atob requires. */
export function decodeJwtPayload(token: string): Record<string, unknown> | null {
    const part = token.split(".")[1];

    if (!part) {
        return null;
    }

    try {
        const base64 = part.replace(/-/g, "+").replace(/_/g, "/");
        const pad = (4 - (base64.length % 4)) % 4;
        return JSON.parse(atob(base64 + "=".repeat(pad))) as Record<string, unknown>;
    } catch {
        return null;
    }
}

function readPayload(): Record<string, unknown> | null {
    const token = localStorage.getItem("token");

    if (!token) {
        return null;
    }

    return decodeJwtPayload(token);
}

/** True when there is no token, it cannot be read, or its exp is in the past. */
export function isStoredTokenExpired(): boolean {
    const payload = readPayload();

    if (!payload || typeof payload.exp !== "number") {
        return true;
    }

    return payload.exp <= Math.floor(Date.now() / 1000);
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

const EMAIL_CLAIMS = [
    "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress",
    "email",
];

export function getCurrentEmail(): string | null {
    const payload = readPayload();

    if (!payload) {
        return null;
    }

    for (const claim of EMAIL_CLAIMS) {
        const value = payload[claim];

        if (typeof value === "string" && value.length > 0) {
            return value;
        }
    }

    return null;
}

export const isAdmin = (): boolean => getCurrentRoles().includes("Admin");

export function getRoleLabel(): string {
    const roles = getCurrentRoles();

    if (roles.includes("Admin")) {
        return "Admin";
    }

    if (roles.includes("Staff")) {
        return "Staff";
    }

    return roles[0] ?? "Signed in";
}