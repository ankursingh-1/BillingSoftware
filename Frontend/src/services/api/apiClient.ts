import axios from "axios";

import { API_URL } from "../../config/api";

const apiClient = axios.create({
    baseURL: API_URL,
    headers: {
        "Content-Type": "application/json",
    },
});

// Attach JWT token to every API request
apiClient.interceptors.request.use(
    (config) => {

        const isLoginRequest =
            config.url?.toLowerCase().includes("/auth/login");

        if (!isLoginRequest) {

            const token =
                localStorage.getItem("token");

            if (token) {
                config.headers.Authorization =
                    `Bearer ${token}`;
            }
        }
        return config;
    },
    (error) => {
        return Promise.reject(error);
    }
);

// Handle unauthorized responses
apiClient.interceptors.response.use(
    (response) => {
        return response;
    },
    (error) => {

        const status =
            error.response?.status;

        const requestUrl =
            error.config?.url ?? "";

        const isLoginRequest =
            requestUrl
                .toLowerCase()
                .includes("/auth/login");

        if (
            status === 401 &&
            !isLoginRequest
        ) {
            localStorage.removeItem("token");

            window.location.href = "/login";
        }

        return Promise.reject(error);
    }
);

export default apiClient;