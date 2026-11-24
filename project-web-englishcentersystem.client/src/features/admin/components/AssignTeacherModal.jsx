import {
    useEffect,
    useState
} from "react";

import {
    getCoursesApi,
    getClassesApi,
    addTeacherClassApi,
    updateTeacherClassApi
} from "../api/courseApi";

function AssignTeacherModal({
    open,
    onClose,
    teacher,
    mode,
    editingClass,
    onSuccess
}) {
    const [courses, setCourses] =
        useState([]);

    const [classes, setClasses] =
        useState([]);

    const [selectedCourseId, setSelectedCourseId] =
        useState("");

    

    const [selectedNewClassId, setSelectedNewClassId] =
        useState("");

    const loadCourses = async () => {
        const response =
            await getCoursesApi();

        setCourses(response.data);
    };

    useEffect(() => {
        if (!open) return;

        loadCourses();

        setClasses([]);
        setSelectedCourseId("");
        
        setSelectedNewClassId("");

    }, [open, teacher]);

    const handleSelectCourse = async (courseId) => {
        setSelectedCourseId(courseId);
        setSelectedNewClassId("");

        if (!courseId) {
            setClasses([]);
            return;
        }

        const response =
            await getClassesApi(courseId);

        setClasses(response.data);
    };

    const handleAssign = async () => {
        if (!selectedNewClassId) {
            alert("Please select a class.");
            return;
        }

        if (mode === "add") {
            await addTeacherClassApi({
                userId: teacher.userId,
                classRoomId: Number(selectedNewClassId)
            });
        }

        if (mode === "edit") {
            await updateTeacherClassApi({
                userId: teacher.userId,
                oldClassRoomId: editingClass.classRoomId,
                newClassRoomId: Number(selectedNewClassId)
            });
        }

        onSuccess();
        onClose();
    };

    if (!open) return null;


    return (
        <div className="modal-overlay">

            <div className="assign-modal assign-dropdown-modal">

                <div className="modal-header">

                    <h2>
                        {mode === "edit"
                            ? "Edit Teaching Class"
                            : "Add Teaching Class"}
                    </h2>

                    <button
                        className="close-btn"
                        onClick={onClose}
                    >
                        ×
                    </button>

                </div>

                

                {
                    (
                        <>
                            <div className="assign-form-group">
                                <label>
                                    Course Name
                                </label>

                                <select
                                    value={selectedCourseId}
                                    onChange={(e) =>
                                        handleSelectCourse(
                                            e.target.value
                                        )
                                    }
                                >
                                    <option value="">
                                        VD: Vstep, Toeic...
                                    </option>

                                    {
                                        courses.map(course => (
                                            <option
                                                key={course.courseId}
                                                value={course.courseId}
                                            >
                                                {course.name}
                                            </option>
                                        ))
                                    }
                                </select>
                            </div>

                            <div className="assign-form-group">
                                <label>
                                    Class Name
                                </label>

                                <select
                                    value={selectedNewClassId}
                                    disabled={!selectedCourseId}
                                    onChange={(e) =>
                                        setSelectedNewClassId(
                                            e.target.value
                                        )
                                    }
                                >
                                    <option value="">
                                        e.g. Class A, Class B...
                                    </option>

                                    {
                                        classes.map(item => (
                                            <option
                                                key={item.classRoomId}
                                                value={item.classRoomId}
                                            >
                                                {item.className}
                                            </option>
                                        ))
                                    }
                                </select>
                            </div>
                        </>
                    )
                }

                <div className="assign-actions">

                    <button onClick={onClose}>
                        Cancel
                    </button>

                    <button
                        className="confirm-btn"
                        onClick={handleAssign}
                    >
                        {mode === "edit"
                            ? "Confirm"
                            : "Add Class"}
                    </button>

                </div>

            </div>

        </div>
    );
}

export default AssignTeacherModal;
