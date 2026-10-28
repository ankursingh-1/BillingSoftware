import apiClient from "../api/apiClient";

import type { Brand, Tax, Unit } from "../../interfaces/lookups/lookups";

export const getBrands = async (): Promise<Brand[]> => {
    const response = await apiClient.get<Brand[]>("/Brand");

    return response.data;
};

export const getUnits = async (): Promise<Unit[]> => {
    const response = await apiClient.get<Unit[]>("/Unit");

    return response.data;
};

export const getTaxes = async (): Promise<Tax[]> => {
    const response = await apiClient.get<Tax[]>("/Tax");

    return response.data;
};
