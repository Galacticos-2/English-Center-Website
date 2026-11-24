import {
    useEffect,
    useMemo,
    useState
} from "react";

import {
    FiBookOpen,
    FiChevronDown,
    FiChevronRight,
    FiChevronUp,
    FiEdit2,
    FiFilter,
    FiPlus,
    FiSearch,
    FiTrash2,
    
    FiUsers
} from "react-icons/fi";

import {
    getCoursesApi,
    deleteClassRoomApi,
    deleteCourseApi
} from "../api/courseApi";

import CourseModal
    from "../components/CourseModal";
import ClassRoomModal
    from "../components/ClassRoomModal";

import Sidebar
    from "../components/Sidebar";

import "../styles/admin.css";
import "../styles/coursePage.css";

const getCourseTypeLabel = courseType => {
    if (
        courseType === null ||
        courseType === undefined
    ) {
        return "Other";
    }

    if (typeof courseType === "string") {
        const normalized =
            courseType
                .trim()
                .toLowerCase();

        if (
            normalized === "1" ||
            normalized.includes("toeic")
        ) {
            return "TOEIC";
        }

        if (
            normalized === "2" ||
            normalized.includes("vstep")
        ) {
            return "VSTEP";
        }

        if (
            normalized === "3" ||
            normalized.includes("communication") ||
            normalized.includes("giao tiếp")
        ) {
            return "Communication";
        }

        return courseType;
    }

    switch (Number(courseType)) {
        case 1:
            return "TOEIC";

        case 2:
            return "VSTEP";

        case 3:
            return "Communication";

        default:
            return "Other";
    }
};

const getCourseInitials = name => {
    if (!name?.trim()) {
        return "CO";
    }

    const words =
        name
            .trim()
            .split(/\s+/)
            .filter(Boolean);

    if (words.length === 1) {
        return words[0]
            .slice(0, 2)
            .toUpperCase();
    }

    return words
        .slice(0, 2)
        .map(word => word.charAt(0))
        .join("")
        .toUpperCase();
};

const normalizeText = value =>
    String(value || "")
        .trim()
        .toLocaleLowerCase("vi");

const formatSchedule = schedule => {
    if (!schedule?.trim()) {
        return "Not updated";
    }

    return schedule
        .replace(/\bT2\b/gi, "Monday")
        .replace(/\bT3\b/gi, "Tuesday")
        .replace(/\bT4\b/gi, "Wednesday")
        .replace(/\bT5\b/gi, "Thursday")
        .replace(/\bT6\b/gi, "Friday")
        .replace(/\bT7\b/gi, "Saturday")
        .replace(/\bCN\b/gi, "Sunday");
};

const getScheduleTags = schedule => {
    if (!schedule?.trim()) {
        return [];
    }

    return schedule
        .split(/[,;+|]/)
        .map(item => item.trim())
        .filter(Boolean);
};

function CoursePage() {
    const [courses, setCourses] =
        useState([]);

    const [openModal, setOpenModal] =
        useState(false);

    const [editData, setEditData] =
        useState(null);

    const [classRoomModalData, setClassRoomModalData] =
        useState(null);

    const [expandedCourseIds, setExpandedCourseIds] =
        useState([]);

    const [searchKeyword, setSearchKeyword] =
        useState("");

    const [selectedCourseType, setSelectedCourseType] =
        useState("");

    const [sortBy, setSortBy] =
        useState("name-asc");

    const [loading, setLoading] =
        useState(true);

    const [error, setError] =
        useState("");

    const loadCourses = async () => {
        try {
            setLoading(true);
            setError("");

            const response =
                await getCoursesApi();

            const courseData =
                Array.isArray(response)
                    ? response
                    : Array.isArray(response?.data)
                        ? response.data
                        : [];

            setCourses(courseData);

            setExpandedCourseIds(
                currentIds => {
                    const validIds =
                        currentIds.filter(id =>
                            courseData.some(
                                course =>
                                    course.courseId === id
                            )
                        );

                    if (
                        validIds.length === 0 &&
                        courseData.length > 0
                    ) {
                        return [
                            courseData[0].courseId
                        ];
                    }

                    return validIds;
                }
            );
        }
        catch (loadError) {
            console.error(
                "Load courses failed:",
                loadError
            );

            setCourses([]);

            setError(
                loadError?.response?.data?.message ||
                loadError?.message ||
                "Unable to load the course list."
            );
        }
        finally {
            setLoading(false);
        }
    };

    useEffect(() => {
        loadCourses();
    }, []);

    const courseTypeOptions =
        useMemo(() => {
            return [
                ...new Set(
                    courses.map(course =>
                        getCourseTypeLabel(
                            course.courseType
                        )
                    )
                )
            ]
                .filter(Boolean)
                .sort((first, second) =>
                    first.localeCompare(
                        second,
                        "vi"
                    )
                );
        }, [courses]);

    const displayedCourses =
        useMemo(() => {
            const keyword =
                normalizeText(searchKeyword);

            const filtered =
                courses.filter(course => {
                    const courseType =
                        getCourseTypeLabel(
                            course.courseType
                        );

                    const matchesKeyword =
                        !keyword ||
                        normalizeText(
                            course.name
                        ).includes(keyword) ||
                        normalizeText(
                            course.description
                        ).includes(keyword) ||
                        normalizeText(
                            courseType
                        ).includes(keyword) ||
                        course.classes?.some(
                            classRoom =>
                                normalizeText(
                                    classRoom.className
                                ).includes(keyword) ||
                                normalizeText(
                                    classRoom.teacherFullName
                                ).includes(keyword)
                        );

                    const matchesType =
                        !selectedCourseType ||
                        courseType ===
                        selectedCourseType;

                    return (
                        matchesKeyword &&
                        matchesType
                    );
                });

            return [...filtered].sort(
                (first, second) => {
                    if (sortBy === "name-desc") {
                        return second.name.localeCompare(
                            first.name,
                            "vi"
                        );
                    }

                    if (sortBy === "classes-desc") {
                        return (
                            (second.classes?.length || 0) -
                            (first.classes?.length || 0)
                        );
                    }

                    if (sortBy === "students-desc") {
                        const firstStudents =
                            first.classes?.reduce(
                                (total, classRoom) =>
                                    total +
                                    Number(
                                        classRoom.studentCount ||
                                        0
                                    ),
                                0
                            ) || 0;

                        const secondStudents =
                            second.classes?.reduce(
                                (total, classRoom) =>
                                    total +
                                    Number(
                                        classRoom.studentCount ||
                                        0
                                    ),
                                0
                            ) || 0;

                        return (
                            secondStudents -
                            firstStudents
                        );
                    }

                    return first.name.localeCompare(
                        second.name,
                        "vi"
                    );
                }
            );
        }, [
            courses,
            searchKeyword,
            selectedCourseType,
            sortBy
        ]);

    const summary =
        useMemo(() => {
            const allClasses =
                courses.flatMap(
                    course =>
                        course.classes || []
                );

            const totalStudents =
                allClasses.reduce(
                    (total, classRoom) =>
                        total +
                        Number(
                            classRoom.studentCount ||
                            0
                        ),
                    0
                );

            return {
                totalCourses:
                    courses.length,

                totalClasses:
                    allClasses.length,

                totalStudents
            };
        }, [courses]);

    const toggleCourse = courseId => {
        setExpandedCourseIds(
            currentIds =>
                currentIds.includes(courseId)
                    ? currentIds.filter(
                        id => id !== courseId
                    )
                    : [
                        ...currentIds,
                        courseId
                    ]
        );
    };

    const openCreateModal = () => {
        setEditData(null);
        setOpenModal(true);
    };

    const openEditModal = course => {
        setEditData(course);
        setOpenModal(true);
    };

    const openCreateClassRoomModal = course => {
        setClassRoomModalData({
            course,
            classRoom: null
        });
    };

    const openEditClassRoomModal = (
        course,
        classRoom
    ) => {
        setClassRoomModalData({
            course,
            classRoom
        });
    };

    const handleDeleteCourse =
        async course => {
            const confirmDelete =
                window.confirm(
                    `Are you sure you want to delete the course "${course.name}"?`
                );

            if (!confirmDelete) {
                return;
            }

            try {
                await deleteCourseApi(
                    course.courseId
                );

                await loadCourses();
            }
            catch (deleteError) {
                console.error(
                    "Delete course failed:",
                    deleteError
                );

                window.alert(
                    deleteError?.response?.data
                        ?.message ||
                    deleteError?.message ||
                    "Unable to delete the course."
                );
            }
        };

    const handleDeleteClassRoom = async (
        course,
        classRoom
    ) => {
        const confirmed = window.confirm(
            `Are you sure you want to delete the class "${classRoom.className}"?`
        );

        if (!confirmed) {
            return;
        }

        try {
            await deleteClassRoomApi(
                course.courseId,
                classRoom.classRoomId
            );

            window.alert("Class deleted successfully.");

            await loadCourses();
        }
        catch (deleteError) {
            console.error(
                "Delete class room failed:",
                deleteError
            );

            window.alert(
                deleteError?.response?.data?.message ||
                deleteError?.message ||
                "Unable to delete the class."
            );
        }
    };

    return (
        <div className="admin-layout">
            <Sidebar />

            <main className="admin-content course-management-page">
                <nav className="course-management-breadcrumb" aria-label="Breadcrumb">
                    <span>Home</span>
                    <FiChevronRight />
                    <strong>Courses</strong>
                </nav>

                <header className="course-management-header">
                    <div className="course-management-title-card">
                        <span className="course-management-title-icon">
                            <FiBookOpen />
                        </span>

                        <div>
                            <h1>
                                Course Management
                            </h1>

                            <p>
                                Manage and track all courses
                                in the system
                            </p>
                        </div>

                        <button
                            type="button"
                            className="course-management-create-btn"
                            onClick={openCreateModal}
                        >
                            <FiPlus />

                            <span>
                                Add Course
                            </span>
                        </button>
                    </div>
                </header>

                {error && (
                    <div className="course-management-error">
                        {error}
                    </div>
                )}

                <section className="course-management-summary">
                    <article className="course-summary-card courses">
                        <div className="course-summary-icon">
                            <FiBookOpen />
                        </div>

                        <div>
                            <span>
                                Total Courses
                            </span>

                            <strong>
                                {loading
                                    ? "..."
                                    : summary
                                        .totalCourses}
                            </strong>

                            <small>
                                courses
                            </small>
                        </div>
                    </article>

                    <article className="course-summary-card classes">
                        <div className="course-summary-icon">
                            <FiBookOpen />
                        </div>

                        <div>
                            <span>
                                Total Classes
                            </span>

                            <strong>
                                {loading
                                    ? "..."
                                    : summary
                                        .totalClasses}
                            </strong>

                            <small>
                                classes
                            </small>
                        </div>
                    </article>

                    <article className="course-summary-card students">
                        <div className="course-summary-icon">
                            <FiUsers />
                        </div>

                        <div>
                            <span>
                                Total Students
                            </span>

                            <strong>
                                {loading
                                    ? "..."
                                    : summary
                                        .totalStudents}
                            </strong>

                            <small>
                                enrolled students
                            </small>
                        </div>
                    </article>
                </section>

                <section className="course-management-toolbar">
                    <div className="course-search-control">
                        <FiSearch />

                        <input
                            type="text"
                            value={searchKeyword}
                            onChange={event =>
                                setSearchKeyword(
                                    event.target.value
                                )
                            }
                            placeholder="Search courses, classes, or teachers..."
                        />
                    </div>

                    <div className="course-select-control">
                        <FiFilter />

                        <select
                            value={selectedCourseType}
                            onChange={event =>
                                setSelectedCourseType(
                                    event.target.value
                                )
                            }
                        >
                            <option value="">
                                All course types
                            </option>

                            {courseTypeOptions.map(
                                courseType => (
                                    <option
                                        key={courseType}
                                        value={courseType}
                                    >
                                        {courseType}
                                    </option>
                                )
                            )}
                        </select>

                        <FiChevronDown />
                    </div>

                    <div className="course-select-control sort">
                        <select
                            value={sortBy}
                            onChange={event =>
                                setSortBy(
                                    event.target.value
                                )
                            }
                        >
                            <option value="name-asc">
                                Sort: Name A–Z
                            </option>

                            <option value="name-desc">
                                Sort: Name Z–A
                            </option>

                            <option value="classes-desc">
                                Most Classes
                            </option>

                            <option value="students-desc">
                                Most Students
                            </option>
                        </select>

                        <FiChevronDown />
                    </div>
                </section>

                <section className="course-accordion-list">
                    {loading ? (
                        <div className="course-page-state">
                            Loading courses...
                        </div>
                    ) : displayedCourses.length === 0 ? (
                        <div className="course-page-state">
                            No matching courses found.
                        </div>
                    ) : (
                        displayedCourses.map(
                            (
                                course,
                                courseIndex
                            ) => {
                                const isExpanded =
                                    expandedCourseIds
                                        .includes(
                                            course.courseId
                                        );

                                const classes =
                                    course.classes || [];

                                const totalStudents =
                                    classes.reduce(
                                        (
                                            total,
                                            classRoom
                                        ) =>
                                            total +
                                            Number(
                                                classRoom
                                                    .studentCount ||
                                                0
                                            ),
                                        0
                                    );

                                const totalCapacity =
                                    classes.reduce(
                                        (
                                            total,
                                            classRoom
                                        ) =>
                                            total +
                                            Number(
                                                classRoom
                                                    .capacity ||
                                                0
                                            ),
                                        0
                                    );

                                return (
                                    <article
                                        className={
                                            isExpanded
                                                ? `course-accordion-item expanded theme-${courseIndex % 3}`
                                                : `course-accordion-item theme-${courseIndex % 3}`
                                        }
                                        key={
                                            course.courseId
                                        }
                                    >
                                        <div className="course-accordion-header">
                                            <button
                                                type="button"
                                                className="course-accordion-main-button"
                                                onClick={() =>
                                                    toggleCourse(
                                                        course.courseId
                                                    )
                                                }
                                            >
                                                <div className="course-accordion-icon">
                                                    {getCourseInitials(
                                                        course.name
                                                    )}
                                                </div>

                                                <div className="course-accordion-title">
                                                    <div>
                                                        <h2>
                                                            {
                                                                course.name
                                                            }
                                                        </h2>

                                                        <span className="course-type-pill">
                                                            {getCourseTypeLabel(
                                                                course.courseType
                                                            )}
                                                        </span>
                                                    </div>

                                                    <p>
                                                        {course.description ||
                                                            "No description for this course."}
                                                    </p>
                                                </div>

                                                <div className="course-accordion-summary">
                                                    <span>
                                                        {
                                                            classes.length
                                                        }{" "}
                                                            classes
                                                    </span>

                                                    <i />

                                                    <span>
                                                        {
                                                            totalStudents
                                                        }
                                                        /
                                                        {
                                                            totalCapacity
                                                        }{" "}
                                                            students
                                                    </span>

                                                    {isExpanded ? (
                                                        <FiChevronUp />
                                                    ) : (
                                                        <FiChevronDown />
                                                    )}
                                                </div>
                                            </button>

                                            <div className="course-accordion-actions">
                                                <button
                                                    type="button"
                                                    title="Add class"
                                                    onClick={() =>
                                                        openCreateClassRoomModal(
                                                            course
                                                        )
                                                    }
                                                >
                                                    <FiPlus />
                                                </button>

                                                <button
                                                    type="button"
                                                    title="Edit course"
                                                    onClick={() =>
                                                        openEditModal(
                                                            course
                                                        )
                                                    }
                                                >
                                                    <FiEdit2 />
                                                </button>

                                                <button
                                                    type="button"
                                                    className="delete"
                                                    title="Delete course"
                                                    onClick={() =>
                                                        handleDeleteCourse(
                                                            course
                                                        )
                                                    }
                                                >
                                                    <FiTrash2 />
                                                </button>
                                            </div>
                                        </div>

                                        {isExpanded && (
                                            <div className="course-class-table-wrapper">
                                                {classes.length ===
                                                    0 ? (
                                                    <div className="course-empty-classes">
                                            This course has
                                            no classes yet.
                                                    </div>
                                                ) : (
                                                    <table className="course-class-table">
                                                        <thead>
                                                            <tr>
                                                                                <th>
                                                                                    Class
                                                                                </th>

                                                                                <th>
                                                                                    Students
                                                                                </th>

                                                                                <th>
                                                                                    Assigned Teacher
                                                                                </th>

                                                                                <th>
                                                                                    Schedule
                                                                                </th>

                                                                                <th>
                                                                                    Actions
                                                                                </th>
                                                            </tr>
                                                        </thead>

                                                        <tbody>
                                                            {classes.map(
                                                                (
                                                                    classRoom,
                                                                    classIndex
                                                                ) => {
                                                                    const scheduleTags =
                                                                        getScheduleTags(
                                                                            classRoom.schedule
                                                                        );

                                                                    return (
                                                                        <tr
                                                                            key={
                                                                                classRoom.classRoomId
                                                                            }
                                                                        >
                                                                            <td>
                                                                                <div className="course-class-name">
                                                                                    <FiBookOpen />

                                                                                    <strong>
                                                                                        {
                                                                                            classRoom.className
                                                                                        }
                                                                                    </strong>
                                                                                </div>
                                                                            </td>

                                                                            <td>
                                                                                <div className="course-class-students">
                                                                                    <FiUsers />

                                                                                    <strong>
                                                                                        {Number(
                                                                                            classRoom.studentCount ||
                                                                                            0
                                                                                        )}
                                                                                        /
                                                                                        {Number(
                                                                                            classRoom.capacity ||
                                                                                            0
                                                                                        )}
                                                                                    </strong>
                                                                                </div>
                                                                            </td>

                                                                            <td>
                                                                                <div className="course-class-teacher">
                                                                                    <span
                                                                                        className={`course-teacher-avatar color-${classIndex % 4}`}
                                                                                    >
                                                                                        {getCourseInitials(
                                                                                            classRoom.teacherFullName ||
                                                                                            "GV"
                                                                                        )}
                                                                                    </span>

                                                                                    <span
                                                                                        className={
                                                                                            classRoom.teacherFullName
                                                                                                ? ""
                                                                                                : "unassigned"
                                                                                        }
                                                                                    >
                                                                                        {classRoom.teacherFullName ||
                                                                                            "Unassigned"}
                                                                                    </span>
                                                                                </div>
                                                                            </td>

                                                                            <td>
                                                                                {scheduleTags.length >
                                                                                    0 ? (
                                                                                    <div className="course-schedule-tags">
                                                                                        {scheduleTags.map(
                                                                                            schedule => (
                                                                                                <span
                                                                                                    key={`${classRoom.classRoomId}-${schedule}`}
                                                                                                >
                                                                                                    {formatSchedule(
                                                                                                        schedule
                                                                                                    )}
                                                                                                </span>
                                                                                            )
                                                                                        )}
                                                                                    </div>
                                                                                ) : (
                                                                                    <span className="course-no-schedule">
                                                                                        Not updated
                                                                                    </span>
                                                                                )}
                                                                            </td>

                                                                            <td>
                                                                                <div className="course-class-actions">
                                                                                    <button
                                                                                        type="button"
                                                                                        title="Edit class"
                                                                                        onClick={() =>
                                                                                            openEditClassRoomModal(
                                                                                                course,
                                                                                                classRoom
                                                                                            )
                                                                                        }
                                                                                    >
                                                                                        <FiEdit2 />
                                                                                    </button>

                                                                                    <button
                                                                                        type="button"
                                                                                        className="delete"
                                                                                        title="Delete class"
                                                                                        onClick={() =>
                                                                                            handleDeleteClassRoom(
                                                                                                course,
                                                                                                classRoom
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
                                                            )}
                                                        </tbody>
                                                    </table>
                                                )}
                                            </div>
                                        )}
                                    </article>
                                );
                            }
                        )
                    )}
                </section>

                {!loading && (
                    <div className="course-management-footer">
                        Showing{" "}
                        <strong>
                            {displayedCourses.length}
                        </strong>{" "}
                        out of{" "}
                        <strong>
                            {courses.length}
                        </strong>{" "}
                        courses
                    </div>
                )}

                <CourseModal
                    open={openModal}
                    onClose={() => {
                        setOpenModal(false);
                        setEditData(null);
                    }}
                    onSuccess={async () => {
                        setOpenModal(false);
                        setEditData(null);

                        await loadCourses();
                    }}
                    editData={editData}
                />

                <ClassRoomModal
                    open={Boolean(classRoomModalData)}
                    course={classRoomModalData?.course}
                    classRoom={classRoomModalData?.classRoom}
                    onClose={() => setClassRoomModalData(null)}
                    onSuccess={async () => {
                        setClassRoomModalData(null);
                        await loadCourses();
                    }}
                />
            </main>
        </div>
    );
}

export default CoursePage;
