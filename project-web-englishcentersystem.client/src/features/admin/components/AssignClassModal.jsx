import {
    useEffect,
    useState
} from "react";

import {
    getClassesApi,
    assignStudentApi
} from "../api/courseApi";

function AssignClassModal({
    open,
    onClose,
    student,
    onSuccess
}) {

    const [classes, setClasses] =
        useState([]);

    const [selectedClass,
        setSelectedClass] =
        useState(null);

    useEffect(() => {
        if (!open || !student?.courseId) {
            setClasses([]);
            setSelectedClass(null);
            return;
        }

        const loadClasses = async () => {
            try {
                const response = await getClassesApi(student.courseId);

                setClasses(response.data || []);
            }
            catch (loadError) {
                window.alert(
                    loadError?.response?.data?.message ||
                    loadError?.message ||
                    "Unable to load classes."
                );
            }
        };

        loadClasses();
    }, [open, student]);

    const handleAssign =
        async () => {
            try {
                await assignStudentApi({
                    userId: student.userId,
                    classRoomId: selectedClass
                });

                await onSuccess();
            }
            catch (assignError) {
                window.alert(
                    assignError?.response?.data?.message ||
                    assignError?.message ||
                    "Unable to assign the class."
                );
            }
        };

    if (!open) return null;

    return (

        <div className="modal-overlay">

            <div className="assign-modal student-assignment-modal">

                <div className="modal-header">

                    <h2>
                        Assign Class
                    </h2>

                    <button
                        className="close-btn"
                        onClick={onClose}
                    >
                        ×
                    </button>

                </div>

                <p className="assign-desc">

                    Assign a class in <b>{student?.courseName}</b> to
                    <b>
                        {" "}
                        {student.userName}
                    </b>

                </p>

                <h3>Select a class</h3>

                <div className="class-select-list">
                    {classes.map(item => (
                        <button
                            type="button"
                            key={item.classRoomId}
                            className={
                                selectedClass === item.classRoomId
                                    ? "class-card active-class"
                                    : "class-card"
                            }
                            onClick={() =>
                                setSelectedClass(item.classRoomId)
                            }
                        >
                            <h4>{item.className}</h4>

                            <p>{item.schedule}</p>
                        </button>
                    ))}
                </div>

                <div className="assign-actions">

                    <button
                        onClick={onClose}
                    >
                        Cancel
                    </button>

                    <button
                        className="confirm-btn"
                        onClick={handleAssign}
                        disabled={!selectedClass}
                    >
                        Confirm
                    </button>

                </div>

            </div>

        </div>
    );
}

export default AssignClassModal;
