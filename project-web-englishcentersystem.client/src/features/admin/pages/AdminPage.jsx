import {
    useCallback,
    useEffect,
    useMemo,
    useState
} from "react";

import {
    FiChevronRight,
    FiEdit2,
    FiFilter,
    FiPlus,
    FiSearch,
    FiTrash2,
    FiUserPlus,
   
    FiX
} from "react-icons/fi";

import {
    getStudentsApi,
    deleteStudentApi
} from "../api/userApi";

import StudentModal
    from "../components/StudentModal";

import AssignClassModal
    from "../components/AssignClassModal";

import AssignCourseModal
    from "../components/AssignCourseModal";

import Sidebar
    from "../components/Sidebar";

import "../styles/admin.css";
import "../styles/adminStudentPage.css";
import {
    formatVietnamDate
} from "../../../utils/dateTimeUtils";

const normalizeText = value =>
    String(value || "")
        .trim()
        .toLocaleLowerCase("vi");

const getInitials = student => {
    const source =
        student?.fullName ||
        student?.userName ||
        "ST";

    const words =
        source
            .trim()
            .split(/\s+/)
            .filter(Boolean);

    if (words.length === 1) {
        return words[0]
            .slice(0, 2)
            .toUpperCase();
    }

    return words
        .slice(-2)
        .map(word =>
            word.charAt(0)
        )
        .join("")
        .toUpperCase();
};

const formatJoinDate = value => {
    if (!value) {
        return "—";
    }

    return formatVietnamDate(value) || "—";
};

const getStudentStatus = student => {
    if (
        student.isActive === false ||
        student.status === "Inactive" ||
        student.status === "Stopped"
    ) {
        return {
            key: "inactive",
            label: "Inactive"
        };
    }

    if (
        student.status === "Paused" ||
        student.status === "Suspended"
    ) {
        return {
            key: "paused",
            label: "Paused"
        };
    }

    if (
        !student.className &&
        !student.classRoomName
    ) {
        return {
            key: "unassigned",
            label: "Unassigned"
        };
    }

    return {
        key: "active",
        label: "Active"
    };
};

function AdminPage() {
    const [students, setStudents] =
        useState([]);

    const [search, setSearch] =
        useState("");

    const [selectedCourse, setSelectedCourse] =
        useState("");

    const [selectedClass, setSelectedClass] =
        useState("");

    const [selectedStatus, setSelectedStatus] =
        useState("");

    const [openModal, setOpenModal] =
        useState(false);
    const [
        editingStudent,
        setEditingStudent
    ] = useState(null);
    const [
        selectedStudent,
        setSelectedStudent
    ] = useState(null);

    const [
        openAssignModal,
        setOpenAssignModal
    ] = useState(false);

    const [
        openCourseAssignModal,
        setOpenCourseAssignModal
    ] = useState(false);

    const [loading, setLoading] =
        useState(true);

    const [error, setError] =
        useState("");

    const loadStudents =
        useCallback(async () => {
            try {
                setLoading(true);
                setError("");

                const response =
                    await getStudentsApi();

                const data =
                    Array.isArray(response)
                        ? response
                        : Array.isArray(
                            response?.data
                        )
                            ? response.data
                            : [];

                setStudents(data);
            }
            catch (loadError) {
                console.error(
                    "Load students failed:",
                    loadError
                );

                setStudents([]);

                setError(
                    loadError?.response
                        ?.data?.message ||
                    loadError?.message ||
                    "Unable to load the student list."
                );
            }
            finally {
                setLoading(false);
            }
        }, []);

    useEffect(() => {
        loadStudents();
    }, [loadStudents]);

    const courseOptions =
        useMemo(() => {
            return [
                ...new Set(
                    students
                        .map(student =>
                            student.courseName
                        )
                        .filter(Boolean)
                )
            ].sort((first, second) =>
                first.localeCompare(
                    second,
                    "vi"
                )
            );
        }, [students]);

    const classOptions =
        useMemo(() => {
            return [
                ...new Set(
                    students
                        .filter(student =>
                            !selectedCourse ||
                            student.courseName ===
                            selectedCourse
                        )
                        .map(student =>
                            student.className ||
                            student.classRoomName
                        )
                        .filter(Boolean)
                )
            ].sort((first, second) =>
                first.localeCompare(
                    second,
                    "vi"
                )
            );
        }, [
            students,
            selectedCourse
        ]);

    const filteredStudents =
        useMemo(() => {
            const keyword =
                normalizeText(search);

            return students.filter(
                student => {
                    const status =
                        getStudentStatus(
                            student
                        );

                    const className =
                        student.className ||
                        student.classRoomName ||
                        "";

                    const matchesSearch =
                        !keyword ||
                        [
                            student.fullName,
                            student.userName,
                            student.email,
                            student.phoneNumber,
                            className,
                            student.courseName
                        ].some(value =>
                            normalizeText(
                                value
                            ).includes(
                                keyword
                            )
                        );

                    const matchesCourse =
                        !selectedCourse ||
                        student.courseName ===
                        selectedCourse;

                    const matchesClass =
                        !selectedClass ||
                        className ===
                        selectedClass;

                    const matchesStatus =
                        !selectedStatus ||
                        status.key ===
                        selectedStatus;

                    return (
                        matchesSearch &&
                        matchesCourse &&
                        matchesClass &&
                        matchesStatus
                    );
                }
            );
        }, [
            students,
            search,
            selectedCourse,
            selectedClass,
            selectedStatus
        ]);

    const hasActiveFilters =
        Boolean(
            search ||
            selectedCourse ||
            selectedClass ||
            selectedStatus
        );

    const clearFilters = () => {
        setSearch("");
        setSelectedCourse("");
        setSelectedClass("");
        setSelectedStatus("");
    };

    const openCreateStudentModal =
        () => {
            setEditingStudent(null);
            setOpenModal(true);
        };
    const openEditStudentModal =
        student => {
            setEditingStudent(student);
            setOpenModal(true);
        };
    const openStudentCourseAssignment =
        student => {
            setSelectedStudent(
                student
            );

            setOpenCourseAssignModal(true);
        };

    const openStudentClassAssignment =
        student => {
            if (!student.courseId) {
                window.alert(
                    "Please assign a course before assigning a class."
                );

                return;
            }

            setSelectedStudent(student);

            setOpenAssignModal(
                true
            );
        };

    const handleCourseChange =
        event => {
            setSelectedCourse(
                event.target.value
            );

            setSelectedClass("");
        };

    const handleDeleteStudent =
        async student => {
            const confirmed =
                window.confirm(
                    `Are you sure you want to delete the student "${student.fullName}"?`
                );

            if (!confirmed) {
                return;
            }

            try {
                await deleteStudentApi(
                    student.userId
                );

                await loadStudents();
            }
            catch (deleteError) {
                console.error(
                    "Delete student failed:",
                    deleteError
                );

                window.alert(
                    deleteError?.response
                        ?.data?.message ||
                    deleteError?.message ||
                    "Unable to delete the student."
                );
            }
        };

    return (
        <div className="admin-layout">
            <Sidebar />

            <main className="admin-content admin-student-page">
                <nav className="admin-student-breadcrumb" aria-label="Breadcrumb">
                    <span>Home</span>
                    <FiChevronRight />
                    <strong>Students</strong>
                </nav>

                <header className="admin-student-header">
                    <span className="admin-student-title-icon">
                        <FiUserPlus />
                    </span>

                    <div>
                        <h1>
                            Student Management
                        </h1>

                        <p>
                            Manage and track all students
                            in the system
                        </p>
                    </div>

                    <button
                        type="button"
                        className="admin-student-create-btn"
                        onClick={
                            openCreateStudentModal
                        }
                    >
                        <FiPlus />

                        <span>
                            Add Student
                        </span>
                    </button>
                </header>

                {error && (
                    <div className="admin-student-error">
                        {error}
                    </div>
                )}

                <section className="admin-student-toolbar">
                    <label className="admin-student-search">
                        <FiSearch />

                        <input
                            type="text"
                            value={search}
                            onChange={event =>
                                setSearch(
                                    event.target.value
                                )
                            }
                            placeholder="Search by name, email, phone number, or class..."
                        />

                        {search && (
                            <button
                                type="button"
                                aria-label="Clear search"
                                onClick={() =>
                                    setSearch("")
                                }
                            >
                                <FiX />
                            </button>
                        )}
                    </label>

                    <label className="admin-student-select">
                        <select
                            value={
                                selectedCourse
                            }
                            onChange={
                                handleCourseChange
                            }
                        >
                            <option value="">
                                All courses
                            </option>

                            {courseOptions.map(
                                courseName => (
                                    <option
                                        key={
                                            courseName
                                        }
                                        value={
                                            courseName
                                        }
                                    >
                                        {
                                            courseName
                                        }
                                    </option>
                                )
                            )}
                        </select>
                    </label>

                    <label className="admin-student-select">
                        <select
                            value={
                                selectedClass
                            }
                            onChange={event =>
                                setSelectedClass(
                                    event.target
                                        .value
                                )
                            }
                        >
                            <option value="">
                                All classes
                            </option>

                            {classOptions.map(
                                className => (
                                    <option
                                        key={
                                            className
                                        }
                                        value={
                                            className
                                        }
                                    >
                                        {
                                            className
                                        }
                                    </option>
                                )
                            )}
                        </select>
                    </label>

                    <label className="admin-student-select">
                        <select
                            value={
                                selectedStatus
                            }
                            onChange={event =>
                                setSelectedStatus(
                                    event.target
                                        .value
                                )
                            }
                        >
                            <option value="">
                                All statuses
                            </option>

                            <option value="active">
                                Active
                            </option>

                            <option value="unassigned">
                                Unassigned
                            </option>

                            <option value="paused">
                                Paused
                            </option>

                            <option value="inactive">
                                Inactive
                            </option>
                        </select>
                    </label>

                    <button
                        type="button"
                        className={
                            hasActiveFilters
                                ? "admin-student-clear-filter active"
                                : "admin-student-clear-filter"
                        }
                        onClick={clearFilters}
                        disabled={
                            !hasActiveFilters
                        }
                    >
                        <FiFilter />

                        <span>
                            Clear Filters
                        </span>
                    </button>
                </section>

                <section className="admin-student-table-card">
                    <div className="admin-student-table-wrapper">
                        <table className="admin-student-table">
                            <thead>
                                <tr>
                                    <th>
                                        #
                                    </th>

                                    <th>
                                        Student
                                    </th>

                                    <th>
                                        Class
                                    </th>

                                    <th>
                                        Course
                                    </th>

                                    <th>
                                        Email
                                    </th>

                                    <th>
                                        Status
                                    </th>

                                    <th>
                                        Join Date
                                    </th>

                                    <th>
                                        Actions
                                    </th>
                                </tr>
                            </thead>

                            <tbody>
                                {loading ? (
                                    <tr>
                                        <td
                                            colSpan={8}
                                            className="admin-student-table-state"
                                        >
                                            Loading students...
                                        </td>
                                    </tr>
                                ) : filteredStudents
                                    .length === 0 ? (
                                    <tr>
                                        <td
                                            colSpan={8}
                                            className="admin-student-table-state"
                                        >
                                            No matching students found.
                                        </td>
                                    </tr>
                                ) : (
                                    filteredStudents.map(
                                        (
                                            student,
                                            index
                                        ) => {
                                            const status =
                                                getStudentStatus(
                                                    student
                                                );

                                            const className =
                                                student.className ||
                                                student.classRoomName;

                                            return (
                                                <tr
                                                    key={
                                                        student.userId
                                                    }
                                                >
                                                    <td
                                                        data-label="STT"
                                                        className="admin-student-index"
                                                    >
                                                        {
                                                            index +
                                                            1
                                                        }
                                                    </td>

                                                    <td data-label="Student">
                                                        <div className="admin-student-profile">
                                                            {student.avatarUrl ? (
                                                                <img
                                                                    src={
                                                                        student.avatarUrl
                                                                    }
                                                                    alt={
                                                                        student.fullName
                                                                    }
                                                                />
                                                            ) : (
                                                                <div
                                                                    className={`admin-student-avatar avatar-${index % 5}`}
                                                                >
                                                                    {getInitials(
                                                                        student
                                                                    )}
                                                                </div>
                                                            )}

                                                            <div>
                                                                <strong>
                                                                    {
                                                                        student.fullName
                                                                    }
                                                                </strong>

                                                                <span>
                                                                    {student.phoneNumber ||
                                                                        student.userName ||
                                                                        "Not updated"}
                                                                </span>
                                                            </div>
                                                        </div>
                                                    </td>

                                                    <td data-label="Class">
                                                        {className ? (
                                                            <span className="admin-student-class-name">
                                                                {
                                                                    className
                                                                }
                                                            </span>
                                                        ) : (
                                                            <button
                                                                type="button"
                                                                className="admin-student-assign-inline"
                                                                onClick={() =>
                                                                    openStudentClassAssignment(
                                                                        student
                                                                    )
                                                                }
                                                            >
                                                                <FiUserPlus />

                                                                Assign Class
                                                            </button>
                                                        )}
                                                    </td>

                                                    <td data-label="Course">
                                                        {student.courseName ? (
                                                            <span className="admin-student-course-badge">
                                                                {
                                                                    student.courseName
                                                                }
                                                            </span>
                                                        ) : (
                                                            <button
                                                                type="button"
                                                                className="admin-student-assign-inline course"
                                                                onClick={() =>
                                                                    openStudentCourseAssignment(
                                                                        student
                                                                    )
                                                                }
                                                            >
                                                                <FiUserPlus />

                                                                Assign Course
                                                            </button>
                                                        )}
                                                    </td>

                                                    <td data-label="Email">
                                                        <span className="admin-student-email">
                                                            {student.email ||
                                                                "Not updated"}
                                                        </span>
                                                    </td>

                                                    <td data-label="Status">
                                                        <span
                                                            className={`admin-student-status ${status.key}`}
                                                        >
                                                            <i />

                                                            {
                                                                status.label
                                                            }
                                                        </span>
                                                    </td>

                                                    <td data-label="Join Date">
                                                        <span className="admin-student-date">
                                                            {formatJoinDate(
                                                                student.createdAt ||
                                                                student.joinedAt
                                                            )}
                                                        </span>
                                                    </td>

                                                    <td data-label="Actions">
                                                        <div className="admin-student-actions">
                                                            <button
                                                                type="button"
                                                                className="edit"
                                                                title="Edit student information"
                                                                onClick={() =>
                                                                    openEditStudentModal(
                                                                        student
                                                                    )
                                                                }
                                                            >
                                                                <FiEdit2 />
                                                            </button>

                                                            <button
                                                                type="button"
                                                                className="delete"
                                                                title="Delete student"
                                                                onClick={() =>
                                                                    handleDeleteStudent(
                                                                        student
                                                                    )
                                                                }
                                                            >
                                                                <FiTrash2 />
                                                            </button>
                                                        </div>
                                                    </td>
                                                </tr>
                                            );
                                        }
                                    )
                                )}
                            </tbody>
                        </table>
                    </div>
                </section>

                {!loading && (
                    <footer className="admin-student-footer">
                        Showing{" "}
                        <strong>
                            {
                                filteredStudents.length
                            }
                        </strong>{" "}
                        students out of{" "}
                        <strong>
                            {students.length}
                        </strong>{" "}
                        students
                    </footer>
                )}
            </main>

            <AssignClassModal
                open={openAssignModal}
                onClose={() => {
                    setOpenAssignModal(
                        false
                    );

                    setSelectedStudent(
                        null
                    );
                }}
                student={
                    selectedStudent
                }
                onSuccess={async () => {
                    setOpenAssignModal(
                        false
                    );

                    setSelectedStudent(
                        null
                    );

                    await loadStudents();
                }}
            />

            <AssignCourseModal
                open={openCourseAssignModal}
                onClose={() => {
                    setOpenCourseAssignModal(false);
                    setSelectedStudent(null);
                }}
                student={selectedStudent}
                onSuccess={async () => {
                    setOpenCourseAssignModal(false);
                    setSelectedStudent(null);
                    await loadStudents();
                }}
            />

            <StudentModal
                open={openModal}
                editData={
                    editingStudent
                }
                existingStudents={students}
                onClose={() => {
                    setOpenModal(false);
                    setEditingStudent(null);
                }}
                onSuccess={async () => {
                    setOpenModal(false);
                    setEditingStudent(null);

                    await loadStudents();
                }}
            />
        </div>
    );
}

export default AdminPage;
