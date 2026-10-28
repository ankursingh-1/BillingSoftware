import { Link } from "react-router-dom";

type StatCardProps = {
    title: string;
    value: string;
    icon: string;
    color: string;
    to?: string;
};

function StatCard({ title, value, icon, color, to }: StatCardProps) {
    const inner = (
        <div className="stat-card">
            <div className="stat-icon" style={{ background: color }}>
                <i className={`bi ${icon}`}></i>
            </div>

            <div className="stat-content">
                <h6>{title}</h6>
                <h3>{value}</h3>
            </div>
        </div>
    );

    return (
        <div className="col-xl-3 col-md-6 mb-4">
            {to ? (
                <Link to={to} className="stat-card-link">
                    {inner}
                </Link>
            ) : (
                inner
            )}
        </div>
    );
}

export default StatCard;
