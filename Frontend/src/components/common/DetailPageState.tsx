import { Link } from "react-router-dom";

interface DetailPageStateProps {
    loading: boolean;
    error: string | null;
    notFound: boolean;
    /** What is being loaded, e.g. "purchase". */
    noun: string;
    backTo: string;
    backLabel: string;
    onRetry: () => void;
}

/** Loading / not-found / error block shown instead of a detail page. */
function DetailPageState({
    loading,
    error,
    notFound,
    noun,
    backTo,
    backLabel,
    onRetry,
}: DetailPageStateProps) {
    return (
        <div className="data-page">
            <div className="data-card">
                {notFound ? (
                    <div className="state-block">
                        <i className="bi bi-search state-icon"></i>
                        <p>This {noun} does not exist.</p>
                        <Link to={backTo} className="btn btn-primary btn-sm">
                            {backLabel}
                        </Link>
                    </div>
                ) : loading ? (
                    <div className="state-block">
                        <div className="spinner-border text-primary" role="status">
                            <span className="visually-hidden">
                                Loading {noun}...
                            </span>
                        </div>
                        <p>Loading {noun}...</p>
                    </div>
                ) : (
                    <div className="state-block" role="alert">
                        <i className="bi bi-exclamation-octagon state-icon text-danger"></i>
                        <p>{error ?? `Unable to load the ${noun}.`}</p>
                        <button
                            type="button"
                            className="btn btn-outline-primary btn-sm"
                            onClick={onRetry}
                        >
                            <i className="bi bi-arrow-clockwise me-1"></i>
                            Retry
                        </button>
                    </div>
                )}
            </div>
        </div>
    );
}

export default DetailPageState;