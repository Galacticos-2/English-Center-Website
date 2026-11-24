import {
    useEffect,
    useMemo,
    useState
} from "react";

import {
    Table,
    Tooltip
} from "antd";

import {
    BookOpen,
    CheckSquare,
    ChevronDown,
    ChevronRight,
    Search,
    Star,
    Trophy,
    UserRound,
    Users
} from "lucide-react";

import TeacherSidebar
    from "../../lesson/teacher/TeacherSidebar";

import {
    getTeacherStudentDashboardApi
} from "../api/teacherStudentDashboardApi";

import "../../lesson/styles/lesson.css";
import "../styles/teacherStudentDashboard.css";

const EMPTY_SUMMARY = {
    selectedClassName: "All classes",
    selectedCourseName: "All courses",
    totalStudents: 0,
    averageLessonProgressPercent: 0,
    averageExerciseCompletionPercent: 0,
    averageTestScore: 0
};

const SORT_TABS = [
    {
        key: "all",
        label: "All"
    },
    {
        key: "lesson-progress",
        label: "Most Lesson Progress"
    },
    {
        key: "exercise-completion",
        label: "Most Exercise Completion"
    },
    {
        key: "test-score",
        label: "Highest Average Score"
    }
];

function getProgressClass(value) {
    const percent = Number(value || 0);

    if (percent >= 75) {
        return "high";
    }

    if (percent >= 40) {
        return "medium";
    }

    return "low";
}

function formatPercent(value) {
    const number = Number(value || 0);

    return Number.isInteger(number)
        ? `${number}%`
        : `${number.toFixed(1)}%`;
}

function formatScore(value) {
    if (
        value === null ||
        value === undefined
    ) {
        return "–";
    }

    const number = Number(value);

    return Number.isInteger(number)
        ? number.toFixed(1)
        : number.toFixed(2);
}

function TeacherStudentDashboardPage() {
    const [dashboard, setDashboard] =
        useState(null);

    const [classRoomId, setClassRoomId] =
        useState("");

    const [courseId, setCourseId] =
        useState("");

    const [searchInput, setSearchInput] =
        useState("");

    const [searchKeyword, setSearchKeyword] =
        useState("");

    const [sortBy, setSortBy] =
        useState("all");

    const [loading, setLoading] =
        useState(true);

    const [error, setError] =
        useState("");

   
    const loadDashboard = async ({
        nextClassRoomId =
        classRoomId,

        nextCourseId =
        courseId,

        nextSearch =
        searchKeyword,

        nextSortBy =
        sortBy
    } = {}) => {
        try {
            setLoading(true);
            setError("");

            const data =
                await getTeacherStudentDashboardApi({
                    classRoomId:
                        nextClassRoomId,

                    courseId:
                        nextCourseId,

                    search:
                        nextSearch,

                    sortBy:
                        nextSortBy === "all"
                            ? null
                            : nextSortBy
                });

            setDashboard(data);
        }
        catch (loadError) {
            console.error(
                "Load teacher students failed:",
                loadError
            );

            setError(
                loadError?.response?.data
                    ?.message ||
                loadError?.message ||
                "Unable to load the student list."
            );
        }
        finally {
            setLoading(false);
        }
    };
    useEffect(() => {
        loadDashboard({
            nextClassRoomId: "",
            nextCourseId: "",
            nextSearch: "",
            nextSortBy: "all"
        });
    }, []);

    useEffect(() => {
        const timeoutId =
            window.setTimeout(() => {
                setSearchKeyword(
                    searchInput.trim()
                );
            }, 350);

        return () =>
            window.clearTimeout(timeoutId);
    }, [searchInput]);

    useEffect(() => {
        loadDashboard();
    }, [
        classRoomId,
        courseId,
        searchKeyword,
        sortBy
    ]);

    const courses =
        dashboard?.courses ?? [];

    const classes =
        dashboard?.classes ?? [];

    const summary =
        dashboard?.summary ??
        EMPTY_SUMMARY;

    const students =
        dashboard?.students ?? [];

    const filteredClasses =
        useMemo(() => {
            if (!courseId) {
                return classes;
            }

            return classes.filter(item =>
                String(item.courseId) ===
                String(courseId)
            );
        }, [
            classes,
            courseId
        ]);

    const selectedClass =
        classes.find(item =>
            String(item.classRoomId) ===
            String(classRoomId)
        );

    return (
        <div className="lesson-layout">
            <TeacherSidebar />

            <main className="lesson-main teacher-student-main">
                <div className="teacher-student-page">
                    <div className="teacher-student-breadcrumb">
                        <span>Home</span>
                        <ChevronRight size={16} />
                        <strong>Students</strong>
                    </div>

                    <section className="teacher-student-header">
                        <div className="teacher-student-heading-row">
                            <div className="teacher-student-heading-icon">
                                <UserRound size={29} />
                            </div>

                            <div>
                                <h1>Students</h1>

                                <p>
                                    Manage and track student learning
                                    progress in the classes you
                                    are responsible for.
                                </p>
                            </div>
                        </div>
                    </section>

                    {error && (
                        <div className="teacher-student-error">
                            {error}
                        </div>
                    )}

                    <section className="teacher-student-filters">
                        <label className="teacher-student-filter-field">
                            <span>Course</span>

                            <div>
                                <select
                                    value={courseId}
                                    onChange={event => {
                                        const nextCourseId =
                                            event.target.value;

                                        /*
                                         * Khóa học là bộ lọc cha.
                                         */
                                        setCourseId(
                                            nextCourseId
                                        );

                                        /*
                                         * Khi đổi khóa học,
                                         * lớp đang chọn có thể không còn
                                         * thuộc khóa học mới.
                                         *
                                         * Vì vậy luôn đưa lớp về
                                         * " cả lớp".
                                         */
                                        setClassRoomId("");
                                    }}
                                >
                                    <option value="">
                                        All courses
                                    </option>

                                    {courses.map(course => (
                                        <option
                                            key={
                                                course.courseId
                                            }
                                            value={
                                                course.courseId
                                            }
                                        >
                                            {
                                                course.courseName
                                            }
                                        </option>
                                    ))}
                                </select>

                                <ChevronDown
                                    size={17}
                                />
                            </div>
                        </label>

                        <label className="teacher-student-filter-field">
                            <span>Class</span>

                            <div>
                                <select
                                    value={classRoomId}
                                    onChange={event => {
                                        /*
                                         * Lớp học là bộ lọc con.
                                         *
                                         * Chọn lớp không được thay đổi
                                         * khóa học nữa.
                                         */
                                        setClassRoomId(
                                            event.target.value
                                        );
                                    }}
                                >
                                    <option value="">
                                        All classes
                                    </option>

                                    {filteredClasses.map(
                                        classRoom => (
                                            <option
                                                key={
                                                    classRoom
                                                        .classRoomId
                                                }
                                                value={
                                                    classRoom
                                                        .classRoomId
                                                }
                                            >
                                                {
                                                    classRoom
                                                        .className
                                                }
                                            </option>
                                        )
                                    )}
                                </select>

                                <ChevronDown
                                    size={17}
                                />
                            </div>
                        </label>

                        <label className="teacher-student-search-field">
                            <span>
                                Search Students
                            </span>

                            <div>
                                <input
                                    value={searchInput}
                                    onChange={event =>
                                        setSearchInput(
                                            event.target.value
                                        )
                                    }
                                    placeholder="Search by name or email..."
                                />

                                <Search size={19} />
                            </div>
                        </label>
                    </section>

                    <section className="teacher-student-summary-grid">
                        <article className="teacher-student-summary-card students">
                            <div className="teacher-student-summary-icon">
                                <Users size={28} />
                            </div>

                            <div>
                                <strong>
                                    {loading
                                        ? "..."
                                        : summary
                                            .totalStudents}
                                </strong>

                                <span>
                                    Total Students
                                </span>

                                <small>
                                    {selectedClass
                                        ?.className ||
                                        "All classes"}
                                </small>
                            </div>
                        </article>

                        <article className="teacher-student-summary-card lessons">
                            <div className="teacher-student-summary-icon">
                                <BookOpen size={28} />
                            </div>

                            <div>
                                <strong>
                                    {loading
                                        ? "..."
                                        : formatPercent(
                                            summary
                                                .averageLessonProgressPercent
                                        )}
                                </strong>

                                <span>
                                    Avg. Lesson Progress
                                </span>

                                <small>
                                    Class average
                                </small>
                            </div>
                        </article>

                        <article className="teacher-student-summary-card exercises">
                            <div className="teacher-student-summary-icon">
                                <CheckSquare
                                    size={28}
                                />
                            </div>

                            <div>
                                <strong>
                                    {loading
                                        ? "..."
                                        : formatPercent(
                                            summary
                                                .averageExerciseCompletionPercent
                                        )}
                                </strong>

                                <span>
                                    Exercise Completion
                                </span>

                                <small>
                                    Class average
                                </small>
                            </div>
                        </article>

                        <article className="teacher-student-summary-card scores">
                            <div className="teacher-student-summary-icon">
                                <Trophy size={28} />
                            </div>

                            <div>
                                <strong>
                                    {loading
                                        ? "..."
                                        : formatScore(
                                            summary
                                                .averageTestScore
                                        )}
                                </strong>

                                <span>
                                    Average Test Score
                                </span>

                                <small>
                                    Based on the best attempt
                                </small>
                            </div>
                        </article>
                    </section>

                    <section className="teacher-student-table-card">
                        <div className="teacher-student-tabs">
                            {SORT_TABS.map(tab => (
                                <button
                                    type="button"
                                    key={tab.key}
                                    className={
                                        sortBy === tab.key
                                            ? "teacher-student-tab active"
                                            : "teacher-student-tab"
                                    }
                                    onClick={() =>
                                        setSortBy(
                                            tab.key
                                        )
                                    }
                                >
                                    {tab.label}

                                    {tab.key === "all" && (
                                        <span>
                                            {
                                                summary
                                                    .totalStudents
                                            }
                                        </span>
                                    )}
                                </button>
                            ))}
                        </div>

                        <Table
                            className="teacher-student-ant-table"
                            dataSource={students}
                            rowKey="studentRoleId"
                            tableLayout="fixed"
                            loading={loading}
                            locale={{ emptyText: "No matching students found." }}
                            pagination={{
                                pageSize: 10,
                                showSizeChanger: true,
                                showTotal: total => `${total} students`
                            }}
                            columns={[
                                {
                                    title: "Student",
                                    key: "student",
                                    width: 280,
                                    render: (_, student) => (
                                        <div className="teacher-student-profile">
                                            {student.avatarUrl ? <img src={student.avatarUrl} alt={student.fullName} /> : <div className="teacher-student-avatar-fallback">{student.fullName?.trim()?.charAt(0)?.toUpperCase() || "H"}</div>}
                                            <div>
                                                <Tooltip title={student.fullName}><strong>{student.fullName}</strong></Tooltip>
                                                <span>{student.email}</span>
                                            </div>
                                        </div>
                                    )
                                },
                                {
                                    title: "Class",
                                    dataIndex: "classRoomName",
                                    key: "classRoomName",
                                    width: 120,
                                    render: value => <span className="teacher-student-class-badge">{value || "No class assigned"}</span>
                                },
                                {
                                    title: "Course",
                                    dataIndex: "courseName",
                                    key: "courseName",
                                    width: 160,
                                    render: value => <span className="teacher-student-course-badge">{value || "No course assigned"}</span>
                                },
                                {
                                    title: "Lesson Progress",
                                    key: "lessonProgress",
                                    width: 200,
                                    render: (_, student) => {
                                        const progressClass = getProgressClass(student.lessonProgressPercent);
                                        return <div className="teacher-student-progress-cell"><div><strong>{formatPercent(student.lessonProgressPercent)}</strong><span>{student.completedLessons}/{student.totalLessons}</span></div><div className="teacher-student-progress-track"><span className={progressClass} style={{ width: `${Math.min(100, Number(student.lessonProgressPercent || 0))}%` }} /></div></div>;
                                    }
                                },
                                {
                                    title: "Exercises",
                                    key: "exercises",
                                    width: 125,
                                    render: (_, student) => <div className="teacher-student-exercise-cell"><strong>{student.completedExercises}/{student.totalAssignedExercises}</strong><span>{formatPercent(student.exerciseCompletionPercent)}</span></div>
                                },
                                {
                                    title: "Tests",
                                    key: "tests",
                                    width: 110,
                                    render: (_, student) => <div className="teacher-student-test-cell"><strong>{student.completedTests}</strong><span>/ {student.totalAssignedTests} tests</span></div>
                                },
                                {
                                    title: "Avg. Score",
                                    key: "averageTestScore",
                                    width: 115,
                                    render: (_, student) => <div className={student.averageTestScore === null ? "teacher-student-score empty" : Number(student.averageTestScore) >= 5 ? "teacher-student-score good" : "teacher-student-score weak"}><strong>{formatScore(student.averageTestScore)}</strong>{student.averageTestScore !== null && <Star size={18} fill="currentColor" />}</div>
                                }
                            ]}
                        />
                    </section>
                </div>
            </main>
        </div>
    );
}

export default TeacherStudentDashboardPage;
