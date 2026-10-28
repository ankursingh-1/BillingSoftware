export interface DashboardSummary {
    totalSales: number;
    todaySales: number;
    monthlySales: number;

    totalPurchase: number;
    todayPurchase: number;
    monthlyPurchase: number;

    grossProfit: number | null;
    grossProfitComplete: boolean;

    totalProducts: number;
    totalCustomers: number;
    totalSuppliers: number;

    lowStockProducts: number;
    outOfStockProducts: number;
}

export interface RecentSale {
    saleId: number;
    invoiceNumber: string;
    customerName: string;
    totalAmount: number;
    saleDate: string;
}

export interface RecentPurchase {
    purchaseId: number;
    purchaseNumber: string;
    supplierName: string;
    totalAmount: number;
    purchaseDate: string;
}

export interface LowStockProduct {
    productId: number;
    productName: string;
    currentStock: number;
}

export interface TopProduct {
    productId: number;
    productName: string;
    totalSold: number;
    salesAmount: number;
}

export interface ChartPoint {
    label: string;
    value: number;
}