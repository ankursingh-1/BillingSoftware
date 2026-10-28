import apiClient from "../api/apiClient";

import type { Brand, Tax, Unit } from "../../interfaces/lookups/lookups";

export interface SaveBrandRequest {
    name: string;
    description: string | null;
    isActive: boolean;
}

export interface SaveUnitRequest {
    name: string;
    shortName: string | null;
    description: string | null;
    isActive: boolean;
}

export interface SaveTaxRequest {
    name: string;
    percentage: number;
    description: string | null;
    isActive: boolean;
}

export const createBrand = async (request: SaveBrandRequest) =>
    (await apiClient.post<Brand>("/Brand", request)).data;

export const updateBrand = async (id: number, request: SaveBrandRequest) =>
    (await apiClient.put<Brand>(`/Brand/${id}`, request)).data;

export const deleteBrand = async (id: number) => {
    await apiClient.delete(`/Brand/${id}`);
};

export const createUnit = async (request: SaveUnitRequest) =>
    (await apiClient.post<Unit>("/Unit", request)).data;

export const updateUnit = async (id: number, request: SaveUnitRequest) =>
    (await apiClient.put<Unit>(`/Unit/${id}`, request)).data;

export const deleteUnit = async (id: number) => {
    await apiClient.delete(`/Unit/${id}`);
};

export const createTax = async (request: SaveTaxRequest) =>
    (await apiClient.post<Tax>("/Tax", request)).data;

export const updateTax = async (id: number, request: SaveTaxRequest) =>
    (await apiClient.put<Tax>(`/Tax/${id}`, request)).data;

export const deleteTax = async (id: number) => {
    await apiClient.delete(`/Tax/${id}`);
};
