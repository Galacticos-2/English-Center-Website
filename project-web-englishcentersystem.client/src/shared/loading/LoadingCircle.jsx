import {
    CircularProgressbar,
    buildStyles
} from "react-circular-progressbar";

import "react-circular-progressbar/dist/styles.css";
import "./loading.css";

function LoadingCircle({
    percent = 0
}) {
    return (
        <div className="app-loading-circle">
            <CircularProgressbar
                value={percent}
                text={`${percent}%`}
                styles={buildStyles({
                    textSize: "18px",
                    pathTransitionDuration: 0.4,
                    pathColor: "#2563eb",
                    textColor: "#2563eb",
                    trailColor: "#dbeafe"
                })}
            />
        </div>
    );
}

export default LoadingCircle;