import LoadingCircle from "./LoadingCircle";
import "./loading.css";

function LoadingButton({
    loading = false,
    percent = 0,
    loadingText = "Processing...",
    children,
    disabled,
    className = "",
    ...props
}) {
    return (
        <button
            {...props}
            disabled={disabled || loading}
            className={`${className} app-loading-button`}
        >
            {loading ? (
                <span className="app-loading-button-content">
                    <span className="app-loading-button-circle">
                        <LoadingCircle
                            percent={percent}
                            text=""
                        />
                    </span>

                    <span>{loadingText}</span>
                </span>
            ) : (
                children
            )}
        </button>
    );
}

export default LoadingButton;
