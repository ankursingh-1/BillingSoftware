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
            const token = localStorage.getItem("token");

            if (token) {
                config.headers.Authorization = `Bearer ${token}`;
            }
        }

        // Let the browser set the multipart boundary. The instance default
        // (application/json) would break file uploads.
        if (typeof FormData !== "undefined" && config.data instanceof FormData) {
            delete config.headers["Content-Type"];
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