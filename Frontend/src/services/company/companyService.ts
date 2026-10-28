import { isAxiosError } from "axios";

import apiClient from "../api/apiClient";

import type {
    Company,
    SaveCompanyRequest,
} from "../../interfaces/company/company";

export const getCompany = async (): Promise<Company | null> => {
    try {
        const response = await apiClient.get<Company>("/Company");

        return response.data;
    } catch (error: unknown) {
        if (isAxiosError(error) && error.response?.status === 404) {
            return null;
        }

        throw error;
    }
};

export const saveCompany = async (
    request: SaveCompanyRequest
): Promise<Company> => {
    const response = await apiClient.post<Company>("/Company", request);

    return response.data;
};

export const uploadCompanyLogo = async (file: File): Promise<void> => {
    const body = new FormData();

    body.append("file", file);

    await apiClient.post("/Company/logo", body, {
        headers: { "Content-Type": "multipart/form-data" },
    });
};

export const removeCompanyLogo = async (): Promise<void> => {
    await apiClient.delete("/Company/logo");
};
