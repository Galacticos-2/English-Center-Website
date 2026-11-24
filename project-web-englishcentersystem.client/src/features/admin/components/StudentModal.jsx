import {
    useEffect,
    useState
} from "react";

import {
    createStudentApi,
    updateStudentApi
} from "../api/userApi";

const EMPTY_FORM = {
    userName: "",
    fullName: "",
    email: "",
    phoneNumber: "",
    password: "",
    confirmPassword: ""
};

function StudentModal({
    open,
    onClose,
    onSuccess,
    editData,
    existingStudents = []
}) {
    const isEditMode =
        Boolean(editData?.userId);

    const [formData, setFormData] =
        useState(EMPTY_FORM);

    const [loading, setLoading] =
        useState(false);

    const [error, setError] =
        useState("");

    useEffect(() => {
        if (!open) {
            return;
        }

        if (isEditMode) {
            setFormData({
                userName:
                    editData.userName || "",

                fullName:
                    editData.fullName || "",

                email:
                    editData.email || "",

                phoneNumber:
                    editData.phoneNumber || "",

                password: "",

                confirmPassword: ""
            });
        }
        else {
            setFormData(EMPTY_FORM);
        }

        setError("");
        setLoading(false);
    }, [
        open,
        isEditMode,
        editData
    ]);

    if (!open) {
        return null;
    }

    const handleChange =
        event => {
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
        if (
            !formData.userName.trim()
        ) {
            return "Username is required";
        }

        if (
            !formData.fullName.trim()
        ) {
            return "Full name is required";
        }

        if (
            !formData.email.trim()
        ) {
            return "Email is required";
        }

        if (
            !formData.phoneNumber.trim()
        ) {
            return "Phone number is required";
        }

        const normalizedEmail =
            formData.email.trim().toLowerCase();

        const normalizedUserName =
            formData.userName.trim().toLowerCase();

        const isAnotherStudent = student =>
            student.userId !== editData?.userId;

        if (existingStudents.some(student =>
            isAnotherStudent(student) &&
            student.email?.trim().toLowerCase() === normalizedEmail
        )) {
            return "An account with this email already exists";
        }

        if (existingStudents.some(student =>
            isAnotherStudent(student) &&
            student.userName?.trim().toLowerCase() === normalizedUserName
        )) {
            return "This username is already in use";
        }

        if (
            !isEditMode &&
            !formData.password
        ) {
            return "Password is required";
        }

        if (
            formData.password &&
            formData.password.length < 6
        ) {
            return "Password must be at least 6 characters";
        }

        if (
            formData.password !==
            formData.confirmPassword
        ) {
            return "Passwords do not match";
        }

        return "";
    };

    const handleSubmit =
        async event => {
            event.preventDefault();

            const validationError =
                validateForm();

            if (validationError) {
                setError(
                    validationError
                );

                return;
            }

            try {
                setLoading(true);
                setError("");

                const commonPayload = {
                    userName:
                        formData.userName
                            .trim(),

                    fullName:
                        formData.fullName
                            .trim(),

                    email:
                        formData.email
                            .trim(),

                    phoneNumber:
                        formData.phoneNumber
                            .trim()
                };

                if (isEditMode) {
                    const updatePayload = {
                        ...commonPayload
                    };

                    if (
                        formData.password
                            .trim()
                    ) {
                        updatePayload.password =
                            formData.password;

                        updatePayload.confirmPassword =
                            formData.confirmPassword;
                    }

                    await updateStudentApi(
                        editData.userId,
                        updatePayload
                    );
                }
                else {
                    await createStudentApi({
                        ...commonPayload,
                        password:
                            formData.password
                    });
                }
                

                await onSuccess?.();

                onClose();
            }
            catch (submitError) {
                setError(
                    submitError?.response
                        ?.data?.message ||
                    submitError?.response
                        ?.data?.Message ||
                    submitError?.message ||
                    (
                        isEditMode
                            ? "Failed to update student account"
                            : "Failed to create student account"
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
            onMouseDown={
                handleClose
            }
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
                                ? "Edit Student"
                                : "Create Student Account"}
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
                        onClick={
                            handleClose
                        }
                        disabled={
                            loading
                        }
                    >
                        ×
                    </button>
                </div>

                <form
                    onSubmit={
                        handleSubmit
                    }
                    className="student-form"
                >
                    <input
                        type="text"
                        name="userName"
                        placeholder="Username"
                        value={
                            formData.userName
                        }
                        onChange={
                            handleChange
                        }
                        disabled={
                            loading
                        }
                        autoComplete="username"
                        required
                    />

                    <input
                        type="text"
                        name="fullName"
                        placeholder="Full Name"
                        value={
                            formData.fullName
                        }
                        onChange={
                            handleChange
                        }
                        disabled={
                            loading
                        }
                        autoComplete="name"
                        required
                    />

                    <input
                        type="email"
                        name="email"
                        placeholder="Email"
                        value={
                            formData.email
                        }
                        onChange={
                            handleChange
                        }
                        disabled={
                            loading
                        }
                        autoComplete="email"
                        required
                    />

                    <input
                        type="text"
                        name="phoneNumber"
                        placeholder="Phone Number"
                        value={
                            formData.phoneNumber
                        }
                        onChange={
                            handleChange
                        }
                        disabled={
                            loading
                        }
                        autoComplete="tel"
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
                        value={
                            formData.password
                        }
                        onChange={
                            handleChange
                        }
                        disabled={
                            loading
                        }
                        autoComplete="new-password"
                        required={
                            !isEditMode
                        }
                    />

                    <input
                        type="password"
                        name="confirmPassword"
                        placeholder={
                            isEditMode
                                ? "Confirm New Password"
                                : "Confirm Password"
                        }
                        value={
                            formData
                                .confirmPassword
                        }
                        onChange={
                            handleChange
                        }
                        disabled={
                            loading
                        }
                        autoComplete="new-password"
                        required={
                            !isEditMode
                        }
                    />

                    {error && (
                        <p className="error-text">
                            {error}
                        </p>
                    )}

                    <button
                        type="submit"
                        className="create-btn"
                        disabled={
                            loading
                        }
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

export default StudentModal;
