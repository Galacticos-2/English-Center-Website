import {
    useCallback,
    useEffect,
    useMemo,
    useState
} from "react";

import {
    BookOpen,
    ChevronDown,
    ChevronRight,
    Edit3,
    Filter,
    Mail,
    Pencil,
    Plus,
    Search,
    Trash2,
    Users,
    X
} from "lucide-react";

import {
    getTeachersApi,
    deleteTeacherApi
} from "../api/teacherApi";

import {
    deleteTeacherClassApi
} from "../api/courseApi";

import TeacherModal
    from "../components/TeacherModal";

import AssignTeacherModal
    from "../components/AssignTeacherModal";

import Sidebar
    from "../components/Sidebar";

import "../styles/admin.css";
import "../styles/adminTeacherPage.css";

const normalizeText = value =>
    String(value || "")
        .trim()
        .toLocaleLowerCase("vi");

const getInitials = teacher => {
    const source =
        teacher?.fullName ||
        teacher?.userName ||
        "TC";

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

function TeacherPage() {
    const [teachers, setTeachers] =
        useState([]);

    const [search, setSearch] =
        useState("");

    const [selectedClassId, setSelectedClassId] =
        useState("");

    const [sortBy, setSortBy] =
        useState("name-asc");

    const [loading, setLoading] =
        useState(true);

    const [error, setError] =
        useState("");

    const [assignMode, setAssignMode] =
        useState("add");

    const [editingClass, setEditingClass] =
        useState(null);

    const [openModal, setOpenModal] =
        useState(false);

    const [editingTeacher, setEditingTeacher] =
        useState(null);

    const [
        selectedTeacher,
        setSelectedTeacher
    ] = useState(null);

    const [
        openAssignModal,
        setOpenAssignModal
    ] = useState(false);

    const loadTeachers =
        useCallback(async () => {
            try {
                setLoading(true);
                setError("");

                const response =
                    await getTeachersApi();

                const data =
                    Array.isArray(response)
                        ? response
                        : Array.isArray(
                            response?.data
                        )
                            ? response.data
                            : [];

                setTeachers(data);
            }
            catch (loadError) {
                console.error(
                    "Load teachers failed:",
                    loadError
                );

                setTeachers([]);

                setError(
                    loadError?.response
                        ?.data?.message ||
                    loadError?.message ||
                    "Unable to load the teacher list."
                );
            }
            finally {
                setLoading(false);
            }
        }, []);

    useEffect(() => {
        loadTeachers();
    }, [loadTeachers]);

    const classOptions =
        useMemo(() => {
            const classMap =
                new Map();

            teachers.forEach(teacher => {
                const teacherClasses =
                    teacher.teacherClasses ||
                    [];

                teacherClasses.forEach(
                    classItem => {
                        const key =
                            String(
                                classItem
                                    .classRoomId
                            );

                        if (!classMap.has(key)) {
                            classMap.set(
                                key,
                                {
                                    classRoomId:
                                        classItem
                                            .classRoomId,

                                    className:
                                        classItem
                                            .className,

                                    courseName:
                                        classItem
                                            .courseName
                                }
                            );
                        }
                    }
                );
            });

            return Array
                .from(
                    classMap.values()
                )
                .sort((first, second) => {
                    const firstLabel =
                        `${first.courseName || ""} ${first.className || ""}`;

                    const secondLabel =
                        `${second.courseName || ""} ${second.className || ""}`;

                    return firstLabel.localeCompare(
                        secondLabel,
                        "vi"
                    );
                });
        }, [teachers]);

    const filteredTeachers =
        useMemo(() => {
            const keyword =
                normalizeText(search);

            const filtered =
                teachers.filter(teacher => {
                    const teacherClasses =
                        teacher.teacherClasses ||
                        [];

                    const matchesSearch =
                        !keyword ||
                        [
                            teacher.fullName,
                            teacher.userName,
                            teacher.email
                        ].some(value =>
                            normalizeText(
                                value
                            ).includes(
                                keyword
                            )
                        ) ||
                        teacherClasses.some(
                            classItem =>
                                normalizeText(
                                    classItem
                                        .className
                                ).includes(
                                    keyword
                                ) ||
                                normalizeText(
                                    classItem
                                        .courseName
                                ).includes(
                                    keyword
                                )
                        );

                    const matchesClass =
                        !selectedClassId ||
                        teacherClasses.some(
                            classItem =>
                                String(
                                    classItem
                                        .classRoomId
                                ) ===
                                String(
                                    selectedClassId
                                )
                        );

                    return (
                        matchesSearch &&
                        matchesClass
                    );
                });

            return [...filtered].sort(
                (first, second) => {
                    const firstClassCount =
                        first.teacherClasses
                            ?.length || 0;

                    const secondClassCount =
                        second.teacherClasses
                            ?.length || 0;

                    if (
                        sortBy ===
                        "name-desc"
                    ) {
                        return String(
                            second.fullName ||
                            ""
                        ).localeCompare(
                            String(
                                first.fullName ||
                                ""
                            ),
                            "vi"
                        );
                    }

                    if (
                        sortBy ===
                        "classes-desc"
                    ) {
                        return (
                            secondClassCount -
                            firstClassCount
                        );
                    }

                    if (
                        sortBy ===
                        "classes-asc"
                    ) {
                        return (
                            firstClassCount -
                            secondClassCount
                        );
                    }

                    return String(
                        first.fullName ||
                        ""
                    ).localeCompare(
                        String(
                            second.fullName ||
                            ""
                        ),
                        "vi"
                    );
                }
            );
        }, [
            teachers,
            search,
            selectedClassId,
            sortBy
        ]);

    const hasActiveFilters =
        Boolean(
            search ||
            selectedClassId ||
            sortBy !== "name-asc"
        );

    const clearFilters = () => {
        setSearch("");
        setSelectedClassId("");
        setSortBy("name-asc");
    };

    const handleDeleteTeacher =
        async teacher => {
            const confirmed =
                window.confirm(
                    `Are you sure you want to delete the teacher "${teacher.fullName}"?`
                );

            if (!confirmed) {
                return;
            }

            try {
                await deleteTeacherApi(
                    teacher.userId
                );

                await loadTeachers();
            }
            catch (deleteError) {
                console.error(
                    "Delete teacher failed:",
                    deleteError
                );

                window.alert(
                    deleteError?.response
                        ?.data?.message ||
                    deleteError?.message ||
                    "Unable to delete the teacher."
                );
            }
        };

    const openAddClass =
        teacher => {
            setSelectedTeacher(
                teacher
            );

            setEditingClass(null);

            setAssignMode("add");

            setOpenAssignModal(true);
        };

    const openEditClass =
        (
            teacher,
            classItem
        ) => {
            setSelectedTeacher(
                teacher
            );

            setEditingClass(
                classItem
            );

            setAssignMode("edit");

            setOpenAssignModal(true);
        };

    const handleDeleteTeachingClass =
        async (teacher, classItem) => {
            const confirmed = window.confirm(
                `Remove "${classItem.className}" from ${teacher.fullName}'s assigned classes?`
            );

            if (!confirmed) {
                return;
            }

            try {
                await deleteTeacherClassApi(
                    teacher.userId,
                    classItem.classRoomId
                );

                await loadTeachers();
            }
            catch (deleteError) {
                window.alert(
                    deleteError?.response?.data?.message ||
                    deleteError?.message ||
                    "Unable to remove the assigned class."
                );
            }
        };

    const openCreateTeacherModal =
        () => {
            setEditingTeacher(null);

            setOpenModal(true);
        };

    const openEditTeacherModal =
        teacher => {
            setEditingTeacher(teacher);

            setOpenModal(true);
        };

    return (
        <div className="admin-layout">
            <Sidebar />

            <main className="admin-content admin-teacher-page">
                <nav className="admin-teacher-breadcrumb" aria-label="Breadcrumb">
                    <span>Home</span>
                    <ChevronRight />
                    <strong>Teachers</strong>
                </nav>

                <header className="admin-teacher-header">
                    <span className="admin-teacher-title-icon">
                        <Users />
                    </span>

                    <div>
                        <h1>
                            Teacher Management
                        </h1>

                        <p>
                            Manage teacher accounts and
                            their assigned classes
                        </p>
                    </div>

                    <button
                        type="button"
                        className="admin-teacher-create-btn"
                        onClick={
                            openCreateTeacherModal
                        }
                    >
                        <Plus size={18} />

                        <span>
                            Add Teacher
                        </span>
                    </button>
                </header>

                {error && (
                    <div className="admin-teacher-error">
                        {error}
                    </div>
                )}

                <section className="admin-teacher-toolbar">
                    <label className="admin-teacher-search">
                        <Search size={19} />

                        <input
                            type="text"
                            value={search}
                            onChange={event =>
                                setSearch(
                                    event.target.value
                                )
                            }
                            placeholder="Search by name, email, course, or assigned class..."
                        />

                        {search && (
                            <button
                                type="button"
                                aria-label="Clear search"
                                onClick={() =>
                                    setSearch("")
                                }
                            >
                                <X size={16} />
                            </button>
                        )}
                    </label>

                    <label className="admin-teacher-select">
                        <select
                            value={
                                selectedClassId
                            }
                            onChange={event =>
                                setSelectedClassId(
                                    event.target
                                        .value
                                )
                            }
                        >
                            <option value="">
                                All classes
                            </option>

                            {classOptions.map(
                                classItem => (
                                    <option
                                        key={
                                            classItem
                                                .classRoomId
                                        }
                                        value={
                                            classItem
                                                .classRoomId
                                        }
                                    >
                                        {
                                            classItem
                                                .courseName
                                        }
                                        {" - "}
                                        {
                                            classItem
                                                .className
                                        }
                                    </option>
                                )
                            )}
                        </select>

                        <ChevronDown
                            size={17}
                        />
                    </label>

                    <label className="admin-teacher-select">
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

                            <option value="classes-asc">
                                Fewest Classes
                            </option>
                        </select>

                        <ChevronDown
                            size={17}
                        />
                    </label>

                    <button
                        type="button"
                        className={
                            hasActiveFilters
                                ? "admin-teacher-clear-filter active"
                                : "admin-teacher-clear-filter"
                        }
                        disabled={
                            !hasActiveFilters
                        }
                        onClick={
                            clearFilters
                        }
                    >
                        <Filter size={17} />

                        <span>
                            Clear Filters
                        </span>
                    </button>
                </section>

                <section className="admin-teacher-card-list">
                    {loading ? (
                        <div className="admin-teacher-state">
                            Loading teachers...
                        </div>
                    ) : filteredTeachers
                        .length === 0 ? (
                        <div className="admin-teacher-state">
                            No matching teachers found.
                        </div>
                    ) : (
                        filteredTeachers.map(
                            (
                                teacher,
                                teacherIndex
                            ) => {
                                const teacherClasses =
                                    teacher
                                        .teacherClasses ||
                                    [];

                                const classCount =
                                    teacherClasses
                                        .length;

                                return (
                                    <article
                                        className="admin-teacher-card"
                                        key={
                                            teacher.userId
                                        }
                                    >
                                        <div className="admin-teacher-identity">
                                            {teacher.avatarUrl ? (
                                                <img
                                                    src={
                                                        teacher
                                                            .avatarUrl
                                                    }
                                                    alt={
                                                        teacher
                                                            .fullName
                                                    }
                                                />
                                            ) : (
                                                <div
                                                    className={`admin-teacher-avatar avatar-${teacherIndex % 5}`}
                                                >
                                                    {getInitials(
                                                        teacher
                                                    )}
                                                </div>
                                            )}

                                            <div>
                                                <h2>
                                                    {
                                                        teacher
                                                            .fullName
                                                    }
                                                </h2>

                                                <p>
                                                    <Mail
                                                        size={15}
                                                    />

                                                    <span>
                                                        {teacher
                                                            .email ||
                                                            "Email not updated"}
                                                    </span>
                                                </p>
                                            </div>
                                        </div>

                                        <div className="admin-teacher-class-content">
                                            <div className="admin-teacher-section-title">
                                                <BookOpen
                                                    size={16}
                                                />

                                                <span>
                                                    Assigned Classes
                                                </span>
                                            </div>

                                            {classCount > 0 ? (
                                                <div className="admin-teacher-class-tags">
                                                    {teacherClasses.map(
                                                        (
                                                            classItem,
                                                            classIndex
                                                        ) => (
                                                            <div
                                                                className={`admin-teacher-class-tag color-${classIndex % 4}`}
                                                                key={
                                                                    `${teacher.userId}-${classItem.classRoomId}`
                                                                }
                                                            >
                                                                <span>
                                                                    {
                                                                        classItem
                                                                            .courseName
                                                                    }
                                                                </span>

                                                                <b>
                                                                    •
                                                                </b>

                                                                <span>
                                                                    {
                                                                        classItem
                                                                            .className
                                                                    }
                                                                </span>

                                                                <button
                                                                    type="button"
                                                                    title="Edit assigned class"
                                                                    onClick={() =>
                                                                        openEditClass(
                                                                            teacher,
                                                                            classItem
                                                                        )
                                                                    }
                                                                >
                                                                    <Pencil
                                                                        size={14}
                                                                    />
                                                                </button>

                                                                <button
                                                                    type="button"
                                                                    className="admin-teacher-remove-class"
                                                                    title="Remove assigned class"
                                                                    onClick={() =>
                                                                        handleDeleteTeachingClass(
                                                                            teacher,
                                                                            classItem
                                                                        )
                                                                    }
                                                                >
                                                                    ×
                                                                </button>
                                                            </div>
                                                        )
                                                    )}
                                                </div>
                                            ) : (
                                                <p className="admin-teacher-empty-class">
                                                    This teacher has not been
                                                    assigned to a class yet.
                                                </p>
                                            )}

                                            <button
                                                type="button"
                                                className="admin-teacher-add-class-btn"
                                                onClick={() =>
                                                    openAddClass(
                                                        teacher
                                                    )
                                                }
                                            >
                                                <Plus
                                                    size={16}
                                                />

                                                <span>
                                                    Add Assigned Class
                                                </span>
                                            </button>
                                        </div>

                                        <div className="admin-teacher-card-summary">
                                            <span>
                                                {classCount} classes
                                            </span>
                                        </div>

                                        <div className="admin-teacher-card-actions">
                                            <button
                                                type="button"
                                                className="edit"
                                                title="Edit teacher"
                                                onClick={() =>
                                                    openEditTeacherModal(
                                                        teacher
                                                    )
                                                }
                                            >
                                                <Edit3
                                                    size={18}
                                                />
                                            </button>

                                            <button
                                                type="button"
                                                className="delete"
                                                title="Delete teacher"
                                                onClick={() =>
                                                    handleDeleteTeacher(
                                                        teacher
                                                    )
                                                }
                                            >
                                                <Trash2
                                                    size={18}
                                                />
                                            </button>
                                        </div>
                                    </article>
                                );
                            }
                        )
                    )}
                </section>

                {!loading && (
                    <footer className="admin-teacher-footer">
                        Showing{" "}
                        <strong>
                            {
                                filteredTeachers
                                    .length
                            }
                        </strong>{" "}
                        teachers out of{" "}
                        <strong>
                            {teachers.length}
                        </strong>{" "}
                        teachers
                    </footer>
                )}
            </main>

            <AssignTeacherModal
                open={openAssignModal}
                onClose={() => {
                    setOpenAssignModal(
                        false
                    );

                    setSelectedTeacher(
                        null
                    );

                    setEditingClass(
                        null
                    );

                    setAssignMode("add");
                }}
                teacher={
                    selectedTeacher
                }
                mode={assignMode}
                editingClass={
                    editingClass
                }
                onSuccess={async () => {
                    setOpenAssignModal(
                        false
                    );

                    setSelectedTeacher(
                        null
                    );

                    setEditingClass(
                        null
                    );

                    setAssignMode("add");

                    await loadTeachers();
                }}
            />

            <TeacherModal
                open={openModal}
                onClose={() =>
                    {
                        setOpenModal(false);
                        setEditingTeacher(null);
                    }
                }
                editData={editingTeacher}
                onSuccess={async () => {
                    setOpenModal(false);

                    setEditingTeacher(null);

                    await loadTeachers();
                }}
            />
        </div>
    );
}

export default TeacherPage;
