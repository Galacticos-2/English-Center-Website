import { useEffect, useRef } from "react";
import Hls from "hls.js";

function HlsPlayer({ src }) {
    const videoRef = useRef(null);

    useEffect(() => {
        const video = videoRef.current;

        if (!video || !src) return;

        let hls;

        if (Hls.isSupported()) {
            hls = new Hls();
            hls.loadSource(src);
            hls.attachMedia(video);
        } else {
            video.src = src;
        }

        return () => {
            if (hls) {
                hls.destroy();
            }
        };
    }, [src]);

    return (
        <video
            ref={videoRef}
            controls
            controlsList="nodownload"
            playsInline
            className="lesson-video-player"
        >
            Your browser does not support video playback.
        </video>
    );
}

export default HlsPlayer;
