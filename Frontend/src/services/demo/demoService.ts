import apiClient from "../api/apiClient";

export interface DemoSeedResult {
    success: boolean;
    message: string;
}

export const seedDemoData = async (): Promise<DemoSeedResult> => {
    const response = await apiClient.post<DemoSeedResult>("/Demo/seed");

    return response.data;
};
