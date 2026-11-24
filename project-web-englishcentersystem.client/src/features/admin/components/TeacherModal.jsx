import {
    useEffect,
    useState
} from "react";

import {
    createTeacherApi,
    updateTeacherApi
} from "../api/teacherApi";

const EMPTY_FORM = {
    userName: "",
    fullName: "",
    email: "",
    phoneNumber: "",
    password: "",
    confirmPassword: ""
};

function TeacherModal({
    open,
    onClose,
    onSuccess,
    editData
}) {
    const isEditMode = Boolean(editData?.userId);

    const [formData, setFormData] =
        useState(EMPTY_FORM);

    const [error, setError] = useState("");

    const [loading, setLoading] =
        useState(false);

    useEffect(() => {
        if (!open) {
            return;
        }

        setFormData(
            isEditMode
                ? {
                    userName: editData.userName || "",
                    fullName: editData.fullName || "",
                    email: editData.email || "",
                    phoneNumber: editData.phoneNumber || "",
                    password: "",
                    confirmPassword: ""
                }
                : EMPTY_FORM
        );

        setError("");
        setLoading(false);
    }, [open, isEditMode, editData]);

    if (!open) {
        return null;
    }

    const handleChange = event => {
        const {
            name,
            value
        } = event.target;

        setFormData(current => ({
            ...current,
            [name]: value
        }));
    };

    const validateForm = () => {
        if (!formData.userName.trim()) {
            return "Username is required";
        }

        if (!formData.fullName.trim()) {
            return "Full name is required";
        }

        if (!formData.email.trim()) {
            return "Email is required";
        }

        if (!formData.phoneNumber.trim()) {
            return "Phone number is required";
        }

        if (!isEditMode && !formData.password) {
            return "Password is required";
        }

        if (formData.password && formData.password.length < 6) {
            return "Password must be at least 6 characters";
        }

        if (formData.password !== formData.confirmPassword) {
            return "Passwords do not match";
        }

        return "";
    };

    const handleSubmit = async event => {
        event.preventDefault();

        const validationError = validateForm();

        if (validationError) {
            setError(validationError);
            return;
        }

        try {
            setLoading(true);
            setError("");

            const payload = {
                userName: formData.userName.trim(),
                fullName: formData.fullName.trim(),
                email: formData.email.trim(),
                phoneNumber: formData.phoneNumber.trim()
            };

            if (isEditMode) {
                if (formData.password.trim()) {
                    payload.password = formData.password;
                    payload.confirmPassword = formData.confirmPassword;
                }

                await updateTeacherApi(
                    editData.userId,
                    payload
                );
            }
            else {
                await createTeacherApi({
                    ...payload,
                    password: formData.password
                });
            }

            await onSuccess?.();
            onClose();
        }
        catch (submitError) {
            console.error(
                "Save teacher failed:",
                submitError
            );

            setError(
                submitError?.response?.data?.message ||
                submitError?.message ||
                (
                    isEditMode
                        ? "Failed to update teacher account"
                        : "Failed to create teacher account"
                )
            );
        }
        finally {
            setLoading(false);
        }
    };

    const handleClose = () => {
        if (loading) {
            return;
        }

        setError("");
        onClose();
    };

    return (
        <div
            className="modal-overlay"
            onMouseDown={handleClose}
        >
            <div
                className="student-modal"
                onMouseDown={event =>
                    event.stopPropagation()
                }
            >
                <div className="modal-header">
                    <div>
                        <h2>
                            {isEditMode
                                ? "Edit Teacher"
                                : "Create Teacher"}
                        </h2>

                        {isEditMode && (
                            <p>
                                Leave the password blank
                                if you do not want to change it.
                            </p>
                        )}
                    </div>

                    <button
                        type="button"
                        className="close-btn"
                        onClick={handleClose}
                        disabled={loading}
                    >
                        ×
                    </button>
                </div>

                <form
                    className="student-form"
                    onSubmit={handleSubmit}
                >
                    <input
                        type="text"
                        name="userName"
                        placeholder="Teacher username"
                        value={formData.userName}
                        onChange={handleChange}
                        disabled={loading}
                        required
                    />

                    <input
                        type="text"
                        name="fullName"
                        placeholder="Full name"
                        value={formData.fullName}
                        onChange={handleChange}
                        disabled={loading}
                        required
                    />

                    <input
                        type="email"
                        name="email"
                        placeholder="Email"
                        value={formData.email}
                        onChange={handleChange}
                        disabled={loading}
                        required
                    />

                    <input
                        type="text"
                        name="phoneNumber"
                        placeholder="Phone number"
                        value={formData.phoneNumber}
                        onChange={handleChange}
                        disabled={loading}
                        required
                    />

                    <input
                        type="password"
                        name="password"
                        placeholder={
                            isEditMode
                                ? "New Password (optional)"
                                : "Password"
                        }
                        value={formData.password}
                        onChange={handleChange}
                        disabled={loading}
                        required={!isEditMode}
                    />

                    <input
                        type="password"
                        name="confirmPassword"
                        placeholder={
                            isEditMode
                                ? "Confirm New Password"
                                : "Confirm password"
                        }
                        value={formData.confirmPassword}
                        onChange={handleChange}
                        disabled={loading}
                        required={!isEditMode}
                    />

                    {error && (
                        <p className="error-text">
                            {error}
                        </p>
                    )}

                    <button
                        className="create-btn"
                        type="submit"
                        disabled={loading}
                    >
                        {loading
                            ? (
                                isEditMode
                                    ? "Updating..."
                                    : "Creating..."
                            )
                            : (
                                isEditMode
                                    ? "Save Changes"
                                    : "Create Account"
                            )}
                    </button>
                </form>
            </div>
        </div>
    );
}

export default TeacherModal;
