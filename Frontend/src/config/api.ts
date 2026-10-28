// Override per environment with VITE_API_URL (see .env.example).
export const API_URL: string =
    import.meta.env.VITE_API_URL ?? "https://localhost:7179/api";
