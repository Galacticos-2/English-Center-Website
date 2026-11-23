import { useCallback, useEffect, useState } from "react";
import { useDropzone } from "react-dropzone";
import { Upload, X } from "lucide-react";
import {
    getMyInfoApi,
    uploadAvatarApi
} from "../api/profileApi";
import AvatarCropModal from "../components/AvatarCropModal";
import "../components/profile.css";

function MyInfoPage() {
    const [profile, setProfile] =
        useState(null);

    const [loading, setLoading] =
        useState(true);

    const [imageForCrop, setImageForCrop] =
        useState(null);

    const [showOriginalAvatar, setShowOriginalAvatar] = useState(false);

    const loadProfile = async () => {
        const response = await getMyInfoApi();
        setProfile(response.data);
        setLoading(false);
    };

    useEffect(() => {
        loadProfile();
    }, []);

    const onDrop = useCallback((acceptedFiles) => {
        const file = acceptedFiles[0];

        if (!file) return;

        const previewUrl =
            URL.createObjectURL(file);

        setImageForCrop({ previewUrl, originalFile: file });
    }, []);

    const { getRootProps, getInputProps } =
        useDropzone({
            onDrop,
            accept: {
                "image/jpeg": [],
                "image/png": [],
                "image/webp": []
            },
            maxFiles: 1,
            maxSize: 2 * 1024 * 1024
        });

    const closeCropModal = () => {
        if (imageForCrop?.previewUrl) {
            URL.revokeObjectURL(imageForCrop.previewUrl);
        }

        setImageForCrop(null);
    };

    const handleCropDone = async (file) => {
        if (!imageForCrop?.originalFile) return;

        await uploadAvatarApi(file, imageForCrop.originalFile);
        URL.revokeObjectURL(imageForCrop.previewUrl);
        setImageForCrop(null);
        await loadProfile();

        window.dispatchEvent(
            new Event("avatar-updated")
        );
    };

    if (loading) {
        return <h1>Loading...</h1>;
    }

    return (
        <div className="my-info-page">
            <div className="profile-card">
                <div className="profile-top">
                    <button
                        type="button"
                        className="profile-avatar-button"
                        onClick={() => profile.avatarUrl && setShowOriginalAvatar(true)}
                        disabled={!profile.avatarUrl}
                        aria-label="View original profile picture"
                    >
                        <img
                            className="profile-avatar"
                            src={
                                profile.avatarUrl ||
                                "/default-avatar.png"
                            }
                            alt="avatar"
                        />
                    </button>

                    <div className="avatar-upload-area">
                        <p>
                            Personalize your account with a profile picture.
                            Your profile picture will appear wherever your
                            account is used in the system.
                        </p>

                        <div
                            {...getRootProps()}
                            className="dropzone-box"
                        >
                            <input {...getInputProps()} />

                            <div className="upload-icon">
                                <Upload size={18} />
                            </div>

                            <strong>
                                Drag and drop an image here, or click to select one
                            </strong>

                            <span>
                                JPG, PNG, WEBP • maximum 2MB
                            </span>
                        </div>
                    </div>
                </div>

                <div className="info-row">
                    <span>Full name</span>
                    <strong>{profile.fullName}</strong>
                </div>

                <section className="info-section">
                    <h2>Profile Information</h2>

                    <div className="info-grid">
                        <div className="info-row">
                            <span>Date of birth</span>
                            <strong>
                                {profile.birthday
                                    ? new Date(profile.birthday)
                                        .toLocaleDateString("vi-VN")
                                    : "Not updated"}
                            </strong>
                        </div>

                        <div className="info-row">
                            <span>Role</span>
                            <strong>
                                {profile.role === "Student"
                                    ? "Student"
                                    : profile.role === "Teacher"
                                        ? "Teacher"
                                        : "Administrator"}
                            </strong>
                        </div>

                        <div className="info-row">
                            <span>Status</span>
                            <strong>
                                {profile.isActive
                                    ? "Active"
                                    : "Inactive"}
                            </strong>
                        </div>

                        <div className="info-row">
                            <span>Email</span>
                            <strong>{profile.email}</strong>
                        </div>

                        <div className="info-row">
                            <span>Phone number</span>
                            <strong>
                                {profile.phoneNumber || "Not updated"}
                            </strong>
                        </div>
                    </div>
                </section>

                {profile.role === "Student" && (
                    <section className="info-section">
                        <h2>Student Information</h2>

                        <div className="info-grid">
                            <div className="info-row">
                                <span>Course</span>
                                <strong>
                                    {profile.courseName || "Not assigned"}
                                </strong>
                            </div>

                            

                            <div className="info-row">
                                <span>Class</span>
                                <strong>
                                    {profile.className || "Not assigned"}
                                </strong>
                            </div>

                            <div className="info-row">
                                <span>Level</span>
                                <strong>
                                    {profile.level || "Not updated"}
                                </strong>
                            </div>
                        </div>
                    </section>
                )}

                {profile.role === "Teacher" && (
                    <section className="info-section">
                        <h2>Teacher Information</h2>

                        {profile.teacherClasses.length === 0 ? (
                            <p>No classes have been assigned.</p>
                        ) : (
                            profile.teacherClasses.map((item) => (
                                <div
                                    className="teacher-class-item"
                                    key={item.classRoomId}
                                >
                                    <strong>{item.className}</strong>
                                    <span>{item.courseName}</span>
                                </div>
                            ))
                        )}
                    </section>
                )}
            </div>

            {imageForCrop && (
                <AvatarCropModal
                    image={imageForCrop.previewUrl}
                    onClose={closeCropModal}
                    onCropDone={handleCropDone}
                />
            )}

            {showOriginalAvatar && profile.avatarUrl && (
                <div
                    className="avatar-preview-backdrop"
                    role="presentation"
                    onClick={() => setShowOriginalAvatar(false)}
                >
                    <div
                        className="avatar-preview-modal"
                        role="dialog"
                        aria-modal="true"
                        aria-label="Original profile picture"
                        onClick={event => event.stopPropagation()}
                    >
                        <button
                            type="button"
                            className="avatar-preview-close"
                            onClick={() => setShowOriginalAvatar(false)}
                            aria-label="Close image preview"
                        >
                            <X size={22} />
                        </button>
                        <img
                            src={profile.originalAvatarUrl || profile.avatarUrl}
                            alt="Original profile"
                        />
                    </div>
                </div>
            )}
        </div>
    );
}

export default MyInfoPage;
