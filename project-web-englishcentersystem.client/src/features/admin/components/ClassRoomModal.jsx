import {
    useEffect,
    useState
} from "react";

import {
    createClassRoomApi,
    updateClassRoomApi
} from "../api/courseApi";

const getInitialFormData = classRoom => ({
    className: classRoom?.className || "",
    schedule: classRoom?.schedule || "T2,4,6",
    capacity: classRoom?.capacity || 30,
    room: classRoom?.room || "Online"
});

function ClassRoomModal({
    open,
    course,
    classRoom,
    onClose,
    onSuccess
}) {
    const [formData, setFormData] = useState(
        getInitialFormData()
    );
    const [submitting, setSubmitting] = useState(false);
    const [error, setError] = useState("");

    useEffect(() => {
        if (open) {
            setFormData(getInitialFormData(classRoom));
            setError("");
        }
    }, [classRoom, open]);

    if (!open || !course) {
        return null;
    }

    const handleSubmit = async event => {
        event.preventDefault();
        setSubmitting(true);
        setError("");

        try {
            const payload = {
                ...formData,
                capacity: Number(formData.capacity)
            };

            if (classRoom) {
                await updateClassRoomApi(
                    course.courseId,
                    classRoom.classRoomId,
                    payload
                );
            } else {
                await createClassRoomApi(course.courseId, payload);
            }

            await onSuccess();
        } catch (submitError) {
            setError(
                submitError?.response?.data?.message ||
                submitError?.message ||
                "Unable to save the class."
            );
        } finally {
            setSubmitting(false);
        }
    };

    return (
        <div className="modal-overlay">
            <div className="student-modal">
                <div className="modal-header">
                    <h2>
                        {classRoom ? "Edit Class" : "Create Class"}
                    </h2>

                    <button className="close-btn" onClick={onClose}>
                        ×
                    </button>
                </div>

                <form className="student-form" onSubmit={handleSubmit}>
                    <input
                        required
                        name="className"
                        value={formData.className}
                        placeholder="Class name"
                        onChange={event => setFormData({
                            ...formData,
                            className: event.target.value
                        })}
                    />

                    <input
                        required
                        name="schedule"
                        value={formData.schedule}
                        placeholder="Class days, e.g. Mon, Wed, Fri"
                        onChange={event => setFormData({
                            ...formData,
                            schedule: event.target.value
                        })}
                    />

                    <input
                        required
                        min="1"
                        type="number"
                        name="capacity"
                        value={formData.capacity}
                        placeholder="Maximum number of students"
                        onChange={event => setFormData({
                            ...formData,
                            capacity: event.target.value
                        })}
                    />

                    <select
                        name="room"
                        value={formData.room}
                        onChange={event => setFormData({
                            ...formData,
                            room: event.target.value
                        })}
                    >
                        <option value="Online">Online</option>
                        <option value="Offline">Offline</option>
                    </select>

                    {error && <p className="course-modal-error">{error}</p>}

                    <button type="submit" className="create-btn" disabled={submitting}>
                        {submitting
                            ? "Saving..."
                            : classRoom
                                ? "Update Class"
                                : "Create Class"}
                    </button>
                </form>
            </div>
        </div>
    );
}

export default ClassRoomModal;
