import { useEffect, useRef, useState } from "react";
import "../../styles/speakingRecorder.css";
import {
    saveSpeakingAudio,
    getSpeakingAudio,
    deleteSpeakingAudio
} from "../utils/speakingAudioStore";

function SpeakingRecorder({
    maxSeconds = 120,
    storageKey,
    onRecorded
}) {
    const mediaRecorderRef = useRef(null);
    const streamRef = useRef(null);
    const chunksRef = useRef([]);
    const timerRef = useRef(null);
    const canvasRef = useRef(null);
    const audioContextRef = useRef(null);
    const analyserRef = useRef(null);
    const animationFrameRef = useRef(null);
    const audioUrlRef = useRef("");

    const [recording, setRecording] = useState(false);
    const [seconds, setSeconds] = useState(0);
    const [audioUrl, setAudioUrl] = useState("");

    const stopStream = () => {
        if (animationFrameRef.current) {
            cancelAnimationFrame(animationFrameRef.current);
            animationFrameRef.current = null;
        }

        if (streamRef.current) {
            streamRef.current
                .getTracks()
                .forEach(track => track.stop());

            streamRef.current = null;
        }

        if (
            audioContextRef.current &&
            audioContextRef.current.state !== "closed"
        ) {
            audioContextRef.current.close();
        }

        audioContextRef.current = null;
        analyserRef.current = null;
    };

    const revokeAudioUrl = () => {
        if (audioUrlRef.current) {
            URL.revokeObjectURL(audioUrlRef.current);
            audioUrlRef.current = "";
        }
    };

    useEffect(() => {
        let cancelled = false;

        stopStream();
        clearInterval(timerRef.current);
        revokeAudioUrl();

        chunksRef.current = [];
        mediaRecorderRef.current = null;

        setRecording(false);
        setSeconds(0);
        setAudioUrl("");

        getSpeakingAudio(storageKey).then(savedBlob => {
            if (cancelled) {
                return;
            }

            if (savedBlob) {
                const url = URL.createObjectURL(savedBlob);

                audioUrlRef.current = url;
                setAudioUrl(url);
                onRecorded(savedBlob);
            }
            else {
                onRecorded(null);
            }
        });

        return () => {
            cancelled = true;
            stopStream();
            clearInterval(timerRef.current);
            revokeAudioUrl();
        };
    }, [storageKey]);

    const formatTime = (value) => {
        const m = Math.floor(value / 60)
            .toString()
            .padStart(2, "0");

        const s = (value % 60)
            .toString()
            .padStart(2, "0");

        return `${m}:${s}`;
    };

    const drawWaveform = () => {
        const canvas = canvasRef.current;

        if (!canvas || !analyserRef.current) {
            return;
        }

        const ctx = canvas.getContext("2d");
        const analyser = analyserRef.current;
        const data = new Uint8Array(analyser.frequencyBinCount);

        const draw = () => {
            if (!analyserRef.current) {
                return;
            }

            analyser.getByteTimeDomainData(data);

            ctx.clearRect(0, 0, canvas.width, canvas.height);
            ctx.beginPath();

            const slice = canvas.width / data.length;

            for (let i = 0; i < data.length; i++) {
                const y = (data[i] / 128) * (canvas.height / 2);
                const x = i * slice;

                if (i === 0) {
                    ctx.moveTo(x, y);
                }
                else {
                    ctx.lineTo(x, y);
                }
            }

            ctx.stroke();

            animationFrameRef.current =
                requestAnimationFrame(draw);
        };

        draw();
    };

    const stopRecording = () => {
        if (
            mediaRecorderRef.current &&
            mediaRecorderRef.current.state !== "inactive"
        ) {
            mediaRecorderRef.current.stop();
        }

        clearInterval(timerRef.current);
        setRecording(false);
    };

    const startRecording = async () => {
        stopStream();
        clearInterval(timerRef.current);
        revokeAudioUrl();

        setAudioUrl("");
        setSeconds(0);
        chunksRef.current = [];

        const stream = await navigator.mediaDevices.getUserMedia({
            audio: true
        });

        streamRef.current = stream;

        const mediaRecorder = new MediaRecorder(stream, {
            mimeType: "audio/webm"
        });

        mediaRecorderRef.current = mediaRecorder;

        mediaRecorder.ondataavailable = (event) => {
            if (event.data.size > 0) {
                chunksRef.current.push(event.data);
            }
        };

        mediaRecorder.onstop = () => {
            const blob = new Blob(chunksRef.current, {
                type: "audio/webm"
            });

            const url = URL.createObjectURL(blob);

            audioUrlRef.current = url;
            setAudioUrl(url);
            onRecorded(blob);
            saveSpeakingAudio(storageKey, blob);

            stopStream();
        };

        audioContextRef.current = new AudioContext();

        const source =
            audioContextRef.current.createMediaStreamSource(stream);

        analyserRef.current =
            audioContextRef.current.createAnalyser();

        source.connect(analyserRef.current);

        mediaRecorder.start();

        setRecording(true);

        timerRef.current = setInterval(() => {
            setSeconds(prev => {
                const next = prev + 1;

                if (next >= maxSeconds) {
                    stopRecording();
                }

                return next;
            });
        }, 1000);

        setTimeout(drawWaveform, 100);
    };

    const retryRecording = async () => {
        stopStream();
        clearInterval(timerRef.current);
        revokeAudioUrl();

        setRecording(false);
        setAudioUrl("");
        setSeconds(0);

        chunksRef.current = [];
        mediaRecorderRef.current = null;

        await deleteSpeakingAudio(storageKey);
        onRecorded(null);
    };

    return (
        <div className="speaking-recorder">
            <div className="speaking-timer">
                {formatTime(seconds)} / {formatTime(maxSeconds)}
            </div>

            <canvas
                ref={canvasRef}
                width="600"
                height="90"
                className="speaking-waveform"
            />

            <div className="speaking-recorder-actions">
                {!recording && !audioUrl && (
                    <button
                        type="button"
                        className="speaking-record-btn"
                        onClick={startRecording}
                    >
                        🎙 Start Recording
                    </button>
                )}

                {recording && (
                    <button
                        type="button"
                        className="speaking-stop-btn"
                        onClick={stopRecording}
                    >
                        ⏹ Stop Recording
                    </button>
                )}

                {audioUrl && !recording && (
                    <>
                        <audio controls src={audioUrl} />

                        <button
                            type="button"
                            className="speaking-retry-btn"
                            onClick={retryRecording}
                        >
                            Record Again
                        </button>
                    </>
                )}
            </div>
        </div>
    );
}

export default SpeakingRecorder;
