import apiClient from "../api/apiClient";

import type {
    ChartPoint,
    DashboardSummary,
    LowStockProduct,
    RecentPurchase,
    RecentSale,
    TopProduct,
} from "../../interfaces/dashboard/dashboard";

export const getDashboardSummary =
    async (): Promise<DashboardSummary> => {
        const response =
            await apiClient.get<DashboardSummary>(
                "/Dashboard/summary"
            );

        return response.data;
    };

export const getRecentSales =
    async (): Promise<RecentSale[]> => {
        const response =
            await apiClient.get<RecentSale[]>(
                "/Dashboard/recent-sales"
            );

        return response.data;
    };

export const getRecentPurchases =
    async (): Promise<RecentPurchase[]> => {
        const response =
            await apiClient.get<RecentPurchase[]>(
                "/Dashboard/recent-purchases"
            );

        return response.data;
    };

export const getLowStockProducts =
    async (): Promise<LowStockProduct[]> => {
        const response =
            await apiClient.get<LowStockProduct[]>(
                "/Dashboard/low-stock"
            );

        return response.data;
    };

export const getTopProducts =
    async (): Promise<TopProduct[]> => {
        const response =
            await apiClient.get<TopProduct[]>(
                "/Dashboard/top-products"
            );

        return response.data;
    };

export const getSalesChart =
    async (days = 7): Promise<ChartPoint[]> => {
        const response =
            await apiClient.get<ChartPoint[]>(
                `/Dashboard/sales-chart?days=${days}`
            );

        return response.data;
    };

export const getPurchaseChart =
    async (days = 7): Promise<ChartPoint[]> => {
        const response =
            await apiClient.get<ChartPoint[]>(
                `/Dashboard/purchase-chart?days=${days}`
            );

        return response.data;
    };