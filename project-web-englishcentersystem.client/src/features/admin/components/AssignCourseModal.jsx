import {
    useEffect,
    useState
} from "react";

import {
    assignStudentCourseApi,
    getCoursesApi
} from "../api/courseApi";

function AssignCourseModal({
    open,
    onClose,
    student,
    onSuccess
}) {
    const [courses, setCourses] =
        useState([]);

    const [selectedCourse, setSelectedCourse] =
        useState(null);

    const [submitting, setSubmitting] =
        useState(false);

    useEffect(() => {
        if (!open) {
            setSelectedCourse(null);
            return;
        }

        const loadCourses = async () => {
            try {
                const response = await getCoursesApi();

                setCourses(response.data || []);
            }
            catch (loadError) {
                window.alert(
                    loadError?.response?.data?.message ||
                    loadError?.message ||
                    "Unable to load courses."
                );
            }
        };

        loadCourses();
    }, [open]);

    const handleAssign = async () => {
        if (!selectedCourse || !student) {
            return;
        }

        try {
            setSubmitting(true);

            await assignStudentCourseApi({
                userId: student.userId,
                courseId: selectedCourse.courseId
            });

            await onSuccess();
        }
        catch (assignError) {
            window.alert(
                assignError?.response?.data?.message ||
                assignError?.message ||
                "Unable to assign the course."
            );
        }
        finally {
            setSubmitting(false);
        }
    };

    if (!open) {
        return null;
    }

    return (
        <div className="modal-overlay">
            <div className="assign-modal student-assignment-modal">
                <div className="modal-header">
                    <h2>Assign Course</h2>

                    <button
                        type="button"
                        className="close-btn"
                        onClick={onClose}
                    >
                        ×
                    </button>
                </div>

                <p className="assign-desc">
                    Assign a course to <b>{student?.userName}</b>
                </p>

                <h3>Select a course</h3>

                <div className="course-select-list">
                    {courses.map(course => (
                        <button
                            type="button"
                            key={course.courseId}
                            className={
                                selectedCourse?.courseId ===
                                course.courseId
                                    ? "course-select-card active-course"
                                    : "course-select-card"
                            }
                            onClick={() =>
                                setSelectedCourse(course)
                            }
                        >
                            <h4>{course.name}</h4>

                            <p>{course.description}</p>
                        </button>
                    ))}
                </div>

                <div className="assign-actions">
                    <button
                        type="button"
                        onClick={onClose}
                        disabled={submitting}
                    >
                        Cancel
                    </button>

                    <button
                        type="button"
                        className="confirm-btn"
                        onClick={handleAssign}
                        disabled={!selectedCourse || submitting}
                    >
                        {submitting ? "Assigning..." : "Confirm"}
                    </button>
                </div>
            </div>
        </div>
    );
}

export default AssignCourseModal;
