import LoadingCircle from "./LoadingCircle";
import "./loading.css";

function LoadingModal({
    show = false,
    percent = 0,
    title = "Loading...",
    message = "Processing..."
}) {
    if (!show) {
        return null;
    }

    return (
        <div className="app-loading-modal-overlay">
            <div className="app-loading-modal">
                <LoadingCircle percent={percent} />

                <h3>{title}</h3>

                <p>{message}</p>
            </div>
        </div>
    );
}

export default LoadingModal;
