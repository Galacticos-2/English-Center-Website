import { useState } from "react";
import Cropper from "react-easy-crop";
import "./profile.css";

function AvatarCropModal({
    image,
    onClose,
    onCropDone
}) {
    const [crop, setCrop] =
        useState({ x: 0, y: 0 });

    const [zoom, setZoom] =
        useState(1);

    const [croppedAreaPixels, setCroppedAreaPixels] =
        useState(null);

    const createImage = (url) =>
        new Promise((resolve, reject) => {
            const img = new Image();
            img.addEventListener("load", () => resolve(img));
            img.addEventListener("error", reject);
            img.setAttribute("crossOrigin", "anonymous");
            img.src = url;
        });

    const getCroppedImage = async () => {
        const img = await createImage(image);
        const canvas = document.createElement("canvas");
        const ctx = canvas.getContext("2d");

        canvas.width = croppedAreaPixels.width;
        canvas.height = croppedAreaPixels.height;

        ctx.drawImage(
            img,
            croppedAreaPixels.x,
            croppedAreaPixels.y,
            croppedAreaPixels.width,
            croppedAreaPixels.height,
            0,
            0,
            croppedAreaPixels.width,
            croppedAreaPixels.height
        );

        canvas.toBlob((blob) => {
            const file = new File(
                [blob],
                "avatar.png",
                { type: "image/png" }
            );

            onCropDone(file);
        }, "image/png");
    };

    return (
        <div className="crop-backdrop">
            <div className="crop-modal">
                <h2>Crop Profile Picture</h2>

                <div className="crop-area">
                    <Cropper
                        image={image}
                        crop={crop}
                        zoom={zoom}
                        aspect={1}
                        cropShape="round"
                        showGrid={false}
                        onCropChange={setCrop}
                        onZoomChange={setZoom}
                        onCropComplete={(_, pixels) =>
                            setCroppedAreaPixels(pixels)
                        }
                    />
                </div>

                <input
                    type="range"
                    min={1}
                    max={3}
                    step={0.1}
                    value={zoom}
                    onChange={(e) =>
                        setZoom(Number(e.target.value))
                    }
                />

                <div className="crop-actions">
                    <button onClick={onClose}>
                        Cancel
                    </button>

                    <button
                        className="primary-btn"
                        onClick={getCroppedImage}
                    >
                        Save Picture
                    </button>
                </div>
            </div>
        </div>
    );
}

export default AvatarCropModal;
