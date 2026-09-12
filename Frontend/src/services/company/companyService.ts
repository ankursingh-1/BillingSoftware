import api from "../auth/authService";

import type {
    Company,
    SaveCompanyRequest,
} from "../../interfaces/company/company";

export const getCompany = async (): Promise<Company | null> => {
    try {
        const response = await api.get<Company>("/Company");

        return response.data;
    } catch (error: any) {
        if (error.response?.status === 404) {
            return null;
        }

        throw error;
    }
};

export const saveCompany = async (
    request: SaveCompanyRequest
): Promise<Company> => {
    const response = await api.post<Company>(
        "/Company",
        request
    );

    return response.data;
};