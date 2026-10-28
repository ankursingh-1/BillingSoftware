import { useEffect, useMemo, useState } from "react";
import {
    Bar,
    BarChart,
    CartesianGrid,
    Legend,
    ResponsiveContainer,
    Tooltip,
    XAxis,
    YAxis,
} from "recharts";

import {
    getPurchaseChart,
    getSalesChart,
} from "../../services/dashboard/dashboardService";

import type { ChartPoint } from "../../interfaces/dashboard/dashboard";

const RANGE_OPTIONS = [7, 15, 30] as const;

interface ChartRow {
    label: string;
    sales: number;
    purchases: number;
}

const formatCurrency = (value: number): string =>
    `₹${value.toLocaleString("en-IN", {
        maximumFractionDigits: 2,
    })}`;

function mergeSeries(
    sales: ChartPoint[],
    purchases: ChartPoint[]
): ChartRow[] {
    // Both endpoints return one point per day for the same range,
    // so the labels line up; merge by label to be safe.
    const purchaseByLabel = new Map(
        purchases.map((point) => [point.label, point.value])
    );

    return sales.map((point) => ({
        label: point.label,
        sales: point.value,
        purchases: purchaseByLabel.get(point.label) ?? 0,
    }));
}

interface SalesPurchaseChartProps {
    /** Change this value to force the chart to reload (e.g. dashboard Refresh). */
    refreshToken?: number;
}

function SalesPurchaseChart({ refreshToken = 0 }: SalesPurchaseChartProps) {
    const [days, setDays] = useState<number>(7);
    const [data, setData] = useState<ChartRow[]>([]);
    const [loading, setLoading] = useState(true);
    const [error, setError] = useState(false);
    const [reloadKey, setReloadKey] = useState(0);

    useEffect(() => {
        let cancelled = false;

        const load = async () => {
            setLoading(true);
            setError(false);

            try {
                const [sales, purchases] = await Promise.all([
                    getSalesChart(days),
                    getPurchaseChart(days),
                ]);

                if (!cancelled) {
                    setData(mergeSeries(sales, purchases));
                }
            } catch {
                if (!cancelled) {
                    setError(true);
                }
            } finally {
                if (!cancelled) {
                    setLoading(false);
                }
            }
        };

        void load();

        return () => {
            cancelled = true;
        };
    }, [days, reloadKey, refreshToken]);

    const hasActivity = useMemo(
        () =>
            data.some(
                (row) => row.sales > 0 || row.purchases > 0
            ),
        [data]
    );

    return (
        <div className="dashboard-widget">
            <div className="widget-header chart-header">
                <h5>Sales &amp; Purchases</h5>

                <div
                    className="btn-group btn-group-sm"
                    role="group"
                    aria-label="Chart date range"
                >
                    {RANGE_OPTIONS.map((option) => (
                        <button
                            key={option}
                            type="button"
                            className={
                                days === option
                                    ? "btn btn-primary"
                                    : "btn btn-outline-primary"
                            }
                            aria-pressed={days === option}
                            onClick={() => setDays(option)}
                        >
                            {option} days
                        </button>
                    ))}
                </div>
            </div>

            <div className="chart-body">
                {loading ? (
                    <div className="chart-state">
                        <div
                            className="spinner-border spinner-border-sm text-primary"
                            role="status"
                        >
                            <span className="visually-hidden">
                                Loading chart...
                            </span>
                        </div>
                    </div>
                ) : error ? (
                    <div className="chart-state text-muted">
                        <p className="mb-2">
                            Unable to load chart data.
                        </p>
                        <button
                            type="button"
                            className="btn btn-sm btn-outline-primary"
                            onClick={() =>
                                setReloadKey((key) => key + 1)
                            }
                        >
                            Retry
                        </button>
                    </div>
                ) : !hasActivity ? (
                    <div className="chart-state text-muted">
                        No sales or purchases in the last {days}{" "}
                        days.
                    </div>
                ) : (
                    <ResponsiveContainer width="100%" height={300}>
                        <BarChart
                            data={data}
                            margin={{
                                top: 8,
                                right: 12,
                                left: 0,
                                bottom: 0,
                            }}
                        >
                            <CartesianGrid
                                strokeDasharray="3 3"
                                vertical={false}
                            />
                            <XAxis
                                dataKey="label"
                                tick={{ fontSize: 12 }}
                                interval="preserveStartEnd"
                            />
                            <YAxis
                                tick={{ fontSize: 12 }}
                                width={64}
                                tickFormatter={(value: number) =>
                                    value.toLocaleString("en-IN")
                                }
                            />
                            <Tooltip
                                formatter={(value) =>
                                    formatCurrency(Number(value))
                                }
                            />
                            <Legend />
                            <Bar
                                dataKey="sales"
                                name="Sales"
                                fill="#2563eb"
                                radius={[4, 4, 0, 0]}
                            />
                            <Bar
                                dataKey="purchases"
                                name="Purchases"
                                fill="#16a34a"
                                radius={[4, 4, 0, 0]}
                            />
                        </BarChart>
                    </ResponsiveContainer>
                )}
            </div>
        </div>
    );
}

export default SalesPurchaseChart;
