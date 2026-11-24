import {
    useEffect,
    useState
} from "react";
import {
    useNotification
} from "../../notification/hooks/useNotification";
import {
    Bell,
    BookOpen,
    ChevronRight,
    GraduationCap,
    LibraryBig,
    LoaderCircle,
    Presentation,
    School,
    UserPlus,
    Users
} from "lucide-react";

import {
    CartesianGrid,
    Legend,
    Line,
    LineChart,
    ResponsiveContainer,
    Tooltip,
    XAxis,
    YAxis
} from "recharts";
import {
    useNavigate
} from "react-router-dom";
import Sidebar
    from "../components/Sidebar";

import {
    getAdminDashboardApi
} from "../api/adminDashboardApi";

import "../styles/admin.css";
import "../styles/adminDashboard.css";

const EMPTY_SUMMARY = {
    totalStudents: 0,
    totalTeachers: 0,
    totalCourses: 0,
    totalClasses: 0
};








const normalizeAdminNotificationType =
    value => {
        if (
            value === 5 ||
            value === "5" ||
            value ===
            "AdminTeacherUnassigned"
        ) {
            return 5;
        }

        if (
            value === 6 ||
            value === "6" ||
            value ===
            "AdminClassWithoutTeacher"
        ) {
            return 6;
        }

        if (
            value === 7 ||
            value === "7" ||
            value ===
            "AdminStudentUnassigned"
        ) {
            return 7;
        }

        if (
            value === 8 ||
            value === "8" ||
            value ===
            "AdminClassAtCapacity"
        ) {
            return 8;
        }

        return null;
    };

const getAdminNotificationContent = notification => {
    const type = normalizeAdminNotificationType(
        notification.typeName ?? notification.type
    );
    const message = String(notification.message || "");

    if (type === 5) {
        const name = message
            .replace(/^Giáo viên\s+/i, "")
            .replace(/\s+hiện chưa được phân lớp\.?$/i, "")
            .trim();

        return {
            title: "Teacher has no assigned class",
            message: name
                ? `Teacher ${name} has not been assigned to any class yet.`
                : "This teacher has not been assigned to any class yet."
        };
    }

    if (type === 6) {
        const name = message
            .replace(/^Lớp\s+/i, "")
            .replace(/\s+hiện chưa có giáo viên phụ trách\.?$/i, "")
            .trim();

        return {
            title: "Class has no assigned teacher",
            message: name
                ? `Class ${name} does not have an assigned teacher yet.`
                : "This class does not have an assigned teacher yet."
        };
    }

    if (type === 7) {
        const name = message
            .replace(/^Học viên\s+/i, "")
            .replace(/\s+hiện chưa được xếp vào lớp học\.?$/i, "")
            .trim();

        return {
            title: "Student has no assigned class",
            message: name
                ? `Student ${name} has not been assigned to a class yet.`
                : "This student has not been assigned to a class yet."
        };
    }

    return {
        title: notification.title,
        message
    };
};

function WarningIcon({
    type
}) {
    const normalizedType =
        normalizeAdminNotificationType(
            type
        );

    switch (normalizedType) {
        case 5:
            return (
                <UserPlus size={20} />
            );

        case 6:
            return (
                <School size={20} />
            );

        case 7:
            return (
                <GraduationCap
                    size={20}
                />
            );

        case 8:
            return (
                <LibraryBig size={20} />
            );

        default:
            return (
                <Bell size={20} />
            );
    }
}



function AdminDashboardPage() {
    const [dashboard, setDashboard] =
        useState(null);
    const navigate = useNavigate();
    const [loading, setLoading] =
        useState(true);
    const {
        notifications,
        loading: notificationLoading,
        markAsRead
    } = useNotification();
    const [error, setError] =
        useState("");
    const [activityDays, setActivityDays] =
        useState(7);

    useEffect(() => {
        let cancelled = false;

        const loadDashboard =
            async () => {
                try {
                    setLoading(true);
                    setError("");

                    const data =
                        await getAdminDashboardApi(
                            activityDays
                        );

                    if (!cancelled) {
                        setDashboard(data);
                    }
                }
                catch (loadError) {
                    console.error(
                        "Load admin dashboard failed:",
                        loadError
                    );

                    if (!cancelled) {
                        setDashboard(null);

                        setError(
                            loadError?.response
                                ?.data?.message ||
                            loadError?.message ||
                            "Unable to load dashboard data."
                        );
                    }
                }
                finally {
                    if (!cancelled) {
                        setLoading(false);
                    }
                }
            };

        loadDashboard();

        return () => {
            cancelled = true;
        };
    }, [activityDays]);

    const summary =
        dashboard?.summary ??
        EMPTY_SUMMARY;

    const activity =
        dashboard?.activity ?? [];
    const courses =
        Array.isArray(
            dashboard?.courses
        )
            ? dashboard.courses
            : [];

    const classes =
        Array.isArray(
            dashboard?.classes
        )
            ? dashboard.classes
            : [];
    const adminWarnings =
        (Array.isArray(notifications)
            ? notifications
            : []
        )
            .filter(notification => {
                const type =
                    normalizeAdminNotificationType(
                        notification.typeName ??
                        notification.type
                    );

                return type !== null;
            })
            .slice(0, 5);
    const handleWarningClick =
        async notification => {
            try {
                if (
                    !notification.isRead &&
                    notification
                        .userNotificationId
                ) {
                    await markAsRead(
                        notification
                            .userNotificationId
                    );
                }
            }
            catch (readError) {
                console.error(
                    "Unable to mark alert as read:",
                    readError
                );
            }

            if (
                notification.actionUrl
            ) {
                navigate(
                    notification.actionUrl
                );

                return;
            }

            navigate(
                "/notifications"
            );
        };
    return (
        <div className="admin-layout">
            <Sidebar />

            <main className="admin-content admin-dashboard-content">
                <nav className="admin-dashboard-breadcrumb" aria-label="Breadcrumb">
                    <span>Home</span>
                    <ChevronRight />
                    <strong>Dashboard</strong>
                </nav>

                <header className="admin-dashboard-header">
                    <span className="admin-dashboard-title-icon">
                        <School />
                    </span>

                    <div>
                        <h1>Dashboard</h1>

                        <p>
                            An overview of the center's activity
                        </p>
                    </div>
                </header>

                {error && (
                    <div className="admin-dashboard-error">
                        {error}
                    </div>
                )}

                <section className="admin-dashboard-summary">
                    <article className="admin-dashboard-summary-card students">
                        <div className="admin-dashboard-summary-icon">
                            <Users size={28} />
                        </div>

                        <div>
                            <span>
                                Total students
                            </span>

                            <strong>
                                {loading
                                    ? "..."
                                    : summary
                                        .totalStudents}
                            </strong>
                        </div>
                    </article>

                    <article className="admin-dashboard-summary-card teachers">
                        <div className="admin-dashboard-summary-icon">
                            <UserPlus size={28} />
                        </div>

                        <div>
                            <span>
                                Total teachers
                            </span>

                            <strong>
                                {loading
                                    ? "..."
                                    : summary
                                        .totalTeachers}
                            </strong>
                        </div>
                    </article>

                    <article className="admin-dashboard-summary-card courses">
                        <div className="admin-dashboard-summary-icon">
                            <BookOpen size={28} />
                        </div>

                        <div>
                            <span>
                                Total courses
                            </span>

                            <strong>
                                {loading
                                    ? "..."
                                    : summary
                                        .totalCourses}
                            </strong>
                        </div>
                    </article>

                    <article className="admin-dashboard-summary-card classes">
                        <div className="admin-dashboard-summary-icon">
                            <Presentation
                                size={28}
                            />
                        </div>

                        <div>
                            <span>
                                Total classes
                            </span>

                            <strong>
                                {loading
                                    ? "..."
                                    : summary
                                        .totalClasses}
                            </strong>
                        </div>
                    </article>
                </section>

                <section className="admin-dashboard-middle-grid">
                    <article className="admin-dashboard-panel admin-dashboard-chart-panel">
                        <div className="admin-dashboard-panel-header">
                            <h2>
                                Learning Activity
                                <small>
                                    (last {activityDays} days)
                                </small>
                            </h2>

                            <select
                                className="admin-dashboard-range-select"
                                value={activityDays}
                                onChange={event =>
                                    setActivityDays(
                                        Number(event.target.value)
                                    )
                                }
                            >
                                <option value={7}>
                                    Last 7 days
                                </option>

                                <option value={14}>
                                    Last 14 days
                                </option>

                                <option value={30}>
                                    Last 30 days
                                </option>

                                <option value={60}>
                                    Last 60 days
                                </option>
                            </select>
                        </div>

                        <div className="admin-dashboard-chart">
                            {loading ? (
                                <div className="admin-dashboard-state">
                                    Loading data...
                                </div>
                            ) : activity.length === 0 ? (
                                <div className="admin-dashboard-state">
                                    No activity data yet
                                </div>
                            ) : (
                                <ResponsiveContainer
                                    width="100%"
                                    height={300}
                                >
                                    <LineChart
                                        data={activity}
                                        margin={{
                                            top: 12,
                                            right: 18,
                                            left: -12,
                                            bottom: 0
                                        }}
                                    >
                                        <CartesianGrid
                                            strokeDasharray="4 4"
                                            vertical={false}
                                        />

                                        <XAxis
                                            dataKey="dateLabel"
                                            tickLine={false}
                                            axisLine={false}
                                        />

                                        <YAxis
                                            allowDecimals={
                                                false
                                            }
                                            tickLine={false}
                                            axisLine={false}
                                        />

                                        <Tooltip />

                                        <Legend
                                            verticalAlign="top"
                                            align="left"
                                            iconType="line"
                                            wrapperStyle={{
                                                paddingBottom:
                                                    "18px"
                                            }}
                                        />

                                        <Line
                                            type="monotone"
                                            dataKey="lessonCompletions"
                                            name="Lesson views"
                                            stroke="#2563eb"
                                            strokeWidth={3}
                                            dot={{
                                                r: 4
                                            }}
                                            activeDot={{
                                                r: 6
                                            }}
                                        />

                                        <Line
                                            type="monotone"
                                            dataKey="exerciseAttempts"
                                            name="Exercise attempts"
                                            stroke="#16a34a"
                                            strokeWidth={3}
                                            dot={{
                                                r: 4
                                            }}
                                            activeDot={{
                                                r: 6
                                            }}
                                        />

                                        <Line
                                            type="monotone"
                                            dataKey="testAttempts"
                                            name="Test attempts"
                                            stroke="#9333ea"
                                            strokeWidth={3}
                                            dot={{
                                                r: 4
                                            }}
                                            activeDot={{
                                                r: 6
                                            }}
                                        />
                                    </LineChart>
                                </ResponsiveContainer>
                            )}
                        </div>
                    </article>

                    <article className="admin-dashboard-panel admin-dashboard-warning-panel">
                        <div className="admin-dashboard-panel-header">
                            <div className="admin-dashboard-warning-heading">
                                <h2>
                                    Alerts Requiring Attention
                                </h2>

                                {adminWarnings.length > 0 && (
                                    <span>
                                        {adminWarnings.length}
                                    </span>
                                )}
                            </div>

                            <button
                                type="button"
                                onClick={() =>
                                    navigate(
                                        "/notifications"
                                    )
                                }
                            >
                                View all
                            </button>
                        </div>

                        <div className="admin-dashboard-warning-list">
                            {notificationLoading ? (
                                <div className="admin-dashboard-warning-state">
                                    <LoaderCircle
                                        size={21}
                                        className="admin-dashboard-warning-loader"
                                    />

                                    <span>
                                        Loading alerts...
                                    </span>
                                </div>
                            ) : adminWarnings.length === 0 ? (
                                <div className="admin-dashboard-warning-empty">
                                    <div>
                                        <Bell size={24} />
                                    </div>

                                    <strong>
                                        No alerts
                                    </strong>

                                    <span>
                                        There are currently no
                                        administrative issues to address.
                                    </span>
                                </div>
                            ) : (
                                adminWarnings.map(
                                    notification => {
                                        const type =
                                            notification
                                                .typeName ??
                                            notification
                                                .type;

                                        const normalizedType =
                                            normalizeAdminNotificationType(
                                                type
                                            );
                                        const content =
                                            getAdminNotificationContent(
                                                notification
                                            );

                                        return (
                                            <button
                                                type="button"
                                                className={
                                                    notification
                                                        .isRead
                                                        ? "admin-dashboard-warning-item"
                                                        : "admin-dashboard-warning-item unread"
                                                }
                                                key={
                                                    notification
                                                        .userNotificationId
                                                }
                                                onClick={() =>
                                                    handleWarningClick(
                                                        notification
                                                    )
                                                }
                                            >
                                                <div
                                                    className={
                                                        `admin-dashboard-warning-icon ` +
                                                        `notification-type-${normalizedType}`
                                                    }
                                                >
                                                    <WarningIcon
                                                        type={type}
                                                    />
                                                </div>

                                                <div className="admin-dashboard-warning-content">
                                                    <strong>
                                                        {content.title}
                                                    </strong>

                                                    <span>
                                                        {content.message}
                                                    </span>
                                                </div>

                                                {!notification
                                                    .isRead && (
                                                        <span className="admin-dashboard-warning-unread-dot" />
                                                    )}

                                                <ChevronRight
                                                    size={17}
                                                    className="admin-dashboard-warning-arrow"
                                                />
                                            </button>
                                        );
                                    }
                                )
                            )}
                        </div>
                    </article>

                    <article className="admin-dashboard-panel admin-dashboard-quick-panel">
                        <div className="admin-dashboard-panel-header">
                            <h2>
                                Quick Actions
                            </h2>
                        </div>

                        <div className="admin-dashboard-quick-grid">
                            <button
                                type="button"
                                className="blue"
                                onClick={() =>
                                    navigate("/admin/students")
                                }
                            >
                                <UserPlus size={25} />

                                <span>
                                    + Create Student
                                </span>
                            </button>

                            <button
                                type="button"
                                className="green"
                                onClick={() =>
                                    navigate("/admin/teachers")
                                }
                            >
                                <UserPlus size={25} />

                                <span>
                                    + Create Teacher
                                </span>
                            </button>

                            <button
                                type="button"
                                className="purple"
                                onClick={() =>
                                    navigate("/admin/courses")
                                }
                            >
                                <BookOpen size={25} />

                                <span>
                                    + Create Course
                                </span>
                            </button>
                        </div>
                    </article>
                </section>

                <section className="admin-dashboard-bottom-grid">
                    <article className="admin-dashboard-panel admin-dashboard-table-panel">
                        <div className="admin-dashboard-panel-header">
                            <h2>
                                Course Statistics
                            </h2>

                            <button
                                type="button"
                                onClick={() =>
                                    navigate("/admin/courses")
                                }
                            >
                                View all
                            </button>
                        </div>

                        <div className="admin-dashboard-table-wrapper">
                            <table className="admin-dashboard-table">
                                <thead>
                                    <tr>
                                        <th>
                                            Course
                                        </th>

                                        <th>
                                            Classes
                                        </th>

                                        <th>
                                            Students
                                        </th>

                                        <th>
                                            Teachers
                                        </th>
                                    </tr>
                                </thead>

                                <tbody>
                                    {loading ? (
                                        <tr>
                                            <td
                                                colSpan={4}
                                                className="admin-dashboard-table-state"
                                            >
                                                Loading data...
                                            </td>
                                        </tr>
                                    ) : courses.length === 0 ? (
                                        <tr>
                                            <td
                                                colSpan={4}
                                                className="admin-dashboard-table-state"
                                            >
                                                No courses yet
                                            </td>
                                        </tr>
                                    ) : (
                                                courses
                                                    .slice(0, 5)
                                                    .map(
                                            course => (
                                                <tr
                                                    key={
                                                        course.courseId
                                                    }
                                                >
                                                                <td className="admin-dashboard-course-name">
                                                                    {course.name}
                                                                </td>

                                                    <td>
                                                        {
                                                            course
                                                                .classCount
                                                        }
                                                    </td>

                                                    <td>
                                                        {
                                                            course
                                                                .studentCount
                                                        }
                                                    </td>

                                                    <td>
                                                        {
                                                            course
                                                                .teacherCount
                                                        }
                                                    </td>
                                                </tr>
                                            )
                                        )
                                    )}
                                </tbody>
                            </table>
                        </div>
                    </article>

                    <article className="admin-dashboard-panel admin-dashboard-table-panel">
                        <div className="admin-dashboard-panel-header">
                            <h2>
                                Class Status
                            </h2>

                            <button
                                type="button"
                                onClick={() =>
                                    navigate("/admin/courses")
                                }
                            >
                                View all
                            </button>
                        </div>

                        <div className="admin-dashboard-table-wrapper">
                            <table className="admin-dashboard-table">
                                <thead>
                                    <tr>
                                        <th>Class</th>
                                        <th>Course</th>
                                        <th>Teacher</th>
                                        <th>Students</th>
                                    </tr>
                                </thead>

                                <tbody>
                                    {loading ? (
                                        <tr>
                                            <td
                                                colSpan={4}
                                                className="admin-dashboard-table-state"
                                            >
                                                Loading data...
                                            </td>
                                        </tr>
                                    ) : classes.length === 0 ? (
                                        <tr>
                                            <td
                                                colSpan={4}
                                                className="admin-dashboard-table-state"
                                            >
                                                No classes yet
                                            </td>
                                        </tr>
                                    ) : (
                                                classes
                                                    .slice(0, 5)
                                                    .map(
                                            classItem => (
                                                <tr
                                                    key={
                                                        classItem
                                                            .classRoomId
                                                    }
                                                >
                                                    <td>
                                                        {
                                                            classItem
                                                                .className
                                                        }
                                                    </td>

                                                    <td>
                                                        {
                                                            classItem
                                                                .courseName
                                                        }
                                                    </td>

                                                    <td
                                                        className={
                                                            classItem
                                                                .teacherName ===
                                                                "Chưa phân công"
                                                                ? "admin-dashboard-danger-text"
                                                                : ""
                                                        }
                                                    >
                                                        {
                                                            classItem.teacherName ===
                                                                "Chưa phân công"
                                                                ? "Unassigned"
                                                                : classItem
                                                                    .teacherName
                                                        }
                                                    </td>

                                                    <td>
                                                        <span
                                                            className={
                                                                classItem
                                                                    .capacity > 0 &&
                                                                    classItem
                                                                        .studentCount >=
                                                                    classItem
                                                                        .capacity
                                                                    ? "admin-dashboard-capacity-full"
                                                                    : ""
                                                            }
                                                        >
                                                            {
                                                                classItem
                                                                    .studentCount
                                                            }
                                                            /
                                                            {
                                                                classItem
                                                                    .capacity
                                                            }
                                                        </span>
                                                    </td>
                                                </tr>
                                            )
                                        )
                                    )}
                                </tbody>
                            </table>
                        </div>
                    </article>

              
                </section>
            </main>
        </div>
    );
}

export default AdminDashboardPage;
