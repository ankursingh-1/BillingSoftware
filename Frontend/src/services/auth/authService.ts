import apiClient from "../api/apiClient";

export interface LoginRequest {
    email: string;
    password: string;
}

export interface LoginResponse {
    success: boolean;
    token: string;
    message?: string;
}

export const login = async (
    request: LoginRequest
): Promise<LoginResponse> => {
    const response = await apiClient.post<LoginResponse>(
        "/Auth/login",
        request
    );

    return response.data;
};

export const logout = (): void => {
    localStorage.removeItem("token");

    window.location.replace("/login");
};

export const getToken = (): string | null => {
    return localStorage.getItem("token");
};

export default apiClient;