import axios from "axios";
import { API_URL } from "../../config/api";

export interface LoginRequest {
    email: string;
    password: string;
}

export interface LoginResponse {
    success: boolean;
    token: string;
    message?: string;
}

const api = axios.create({
    baseURL: API_URL,
    headers: {
        "Content-Type": "application/json",
    },
});

export const login = async (
    request: LoginRequest
): Promise<LoginResponse> => {

    const response = await api.post<LoginResponse>(
        "/Auth/login",
        request
    );

    return response.data;
};

export default api;