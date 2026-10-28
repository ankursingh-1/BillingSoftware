import apiClient from "../api/apiClient";

import type {
    Brand,
    GstState,
    PaymentMethodOption,
    Tax,
    Unit,
} from "../../interfaces/lookups/lookups";

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

let gstStatesRequest: Promise<GstState[]> | null = null;

/** The state list never changes at runtime, so it is fetched once. */
export const getGstStates = (): Promise<GstState[]> => {
    if (!gstStatesRequest) {
        gstStatesRequest = apiClient
            .get<GstState[]>("/Lookups/gst-states")
            .then((response) => response.data)
            .catch((error: unknown) => {
                gstStatesRequest = null;
                throw error;
            });
    }

    return gstStatesRequest;
};

export const getPaymentMethods = async (): Promise<PaymentMethodOption[]> => {
    const response = await apiClient.get<PaymentMethodOption[]>(
        "/Lookups/payment-methods"
    );

    return response.data;
};