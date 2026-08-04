type StatCardProps = {
    title: string;
    value: string;
    icon: string;
    color: string;
};

function StatCard({
    title,
    value,
    icon,
    color,
}: StatCardProps) {
    return (
        <div className="col-xl-3 col-md-6 mb-4">

            <div className="stat-card">

                <div
                    className="stat-icon"
                    style={{ background: color }}
                >
                    <i className={`bi ${icon}`}></i>
                </div>

                <div className="stat-content">

                    <h6>{title}</h6>

                    <h3>{value}</h3>

                </div>

            </div>

        </div>
    );
}

export default StatCard;