import {
    useEffect,
    useMemo,
    useState
} from "react";

import {
    BookOpen,
    ChevronRight,
    ClipboardList,
    FileText,
    LayoutDashboard,
    Users
} from "lucide-react";

import {
    Bar,
    BarChart,
    CartesianGrid,
    Cell,
    Legend,
    Line,
    LineChart,
    Pie,
    PieChart,
    ResponsiveContainer,
    Tooltip,
    XAxis,
    YAxis
} from "recharts";

import TeacherSidebar
    from "../../lesson/teacher/TeacherSidebar";

import {
    getTeacherDashboardApi
} from "../api/teacherDashboardApi";

import "../../lesson/styles/lesson.css";
import "../styles/teacherDashboard.css";

const EMPTY_SUMMARY = {
    totalLessons: 0,
    totalExercises: 0,
    totalTests: 0,
    totalStudents: 0
};

const EMPTY_ACTIVITY = [];
const EMPTY_CLASS_ACTIVITIES = [];

const CLASS_LINE_COLORS = [
    "#3b82f6",
    "#10b981",
    "#f97316",
    "#8b5cf6",
    "#ef4444",
    "#06b6d4",
    "#ec4899",
    "#84cc16"
];

const MIX_CHART_COLORS = [
    "#10b981",
    "#f97316"
];

function buildClassChartData(
    classActivities = []
) {
    const result = [];

    classActivities.forEach(classItem => {
        classItem.data.forEach(point => {
            let dateItem = result.find(
                item =>
                    item.dateLabel ===
                    point.dateLabel
            );

            if (!dateItem) {
                dateItem = {
                    dateLabel: point.dateLabel
                };

                result.push(dateItem);
            }

            dateItem[classItem.className] =
                point.totalAttempts;
        });
    });

    return result;
}

function buildDashboardInsights(
    activityData = [],
    classActivities = []
) {
    const totalExerciseAttempts =
        activityData.reduce(
            (total, item) =>
                total + Number(item.exerciseAttempts || 0),
            0
        );

    const totalTestAttempts =
        activityData.reduce(
            (total, item) =>
                total + Number(item.testAttempts || 0),
            0
        );

    const totalActivity =
        totalExerciseAttempts + totalTestAttempts;

    const peakDay =
        activityData.reduce(
            (currentPeak, item) => {
                const itemTotal =
                    Number(item.exerciseAttempts || 0) +
                    Number(item.testAttempts || 0);

                if (
                    !currentPeak ||
                    itemTotal > currentPeak.totalAttempts
                ) {
                    return {
                        dateLabel: item.dateLabel,
                        totalAttempts: itemTotal
                    };
                }

                return currentPeak;
            },
            null
        );

    const maxDailyActivity =
        activityData.reduce(
            (maxValue, item) => {
                const itemTotal =
                    Number(item.exerciseAttempts || 0) +
                    Number(item.testAttempts || 0);

                return Math.max(maxValue, itemTotal);
            },
            0
        );

    const classSummary =
        classActivities
            .map(classItem => {
                const exerciseAttempts =
                    classItem.data.reduce(
                        (total, point) =>
                            total +
                            Number(point.exerciseAttempts || 0),
                        0
                    );

                const testAttempts =
                    classItem.data.reduce(
                        (total, point) =>
                            total +
                            Number(point.testAttempts || 0),
                        0
                    );

                return {
                    classRoomId: classItem.classRoomId,
                    className: classItem.className,
                    exerciseAttempts,
                    testAttempts,
                    totalAttempts:
                        exerciseAttempts + testAttempts
                };
            })
            .sort(
                (first, second) =>
                    second.totalAttempts -
                    first.totalAttempts
            );

    const activeClass =
        classSummary.find(
            item => item.totalAttempts > 0
        ) || null;

    const quietClass =
        classSummary
            .slice()
            .reverse()
            .find(
                item => item.totalAttempts >= 0
            ) || null;

    return {
        totalExerciseAttempts,
        totalTestAttempts,
        totalActivity,
        peakDay,
        classSummary,
        activeClass,
        quietClass,
        maxDailyActivity,
        heatmapDays: activityData.map(item => ({
            dateLabel: item.dateLabel,
            exerciseAttempts:
                Number(item.exerciseAttempts || 0),
            testAttempts:
                Number(item.testAttempts || 0),
            totalAttempts:
                Number(item.exerciseAttempts || 0) +
                Number(item.testAttempts || 0)
        })),
        activityMix: [
            {
                name: "Exercises",
                value: totalExerciseAttempts
            },
            {
                name: "Tests",
                value: totalTestAttempts
            }
        ]
    };
}

function TeacherHomePage() {
    const [dashboard, setDashboard] =
        useState(null);

    const [activityDays, setActivityDays] = useState(30);
    const [classActivityDays, setClassActivityDays] = useState(30);
    const [mixDays, setMixDays] = useState(30);
    const [classSummaryDays, setClassSummaryDays] = useState(30);

    const [loading, setLoading] =
        useState(true);

    const [error, setError] =
        useState("");

    useEffect(() => {
        let cancelled = false;

        const loadDashboard = async () => {
            try {
                setLoading(true);
                setError("");

                const data = await getTeacherDashboardApi(30);

                if (!cancelled) {
                    setDashboard(data);
                }
            }
            catch (loadError) {
                console.error(
                    "Load teacher dashboard failed:",
                    loadError
                );

                if (!cancelled) {
                    setDashboard(null);

                    setError(
                        loadError?.response?.data?.message ||
                        loadError?.message ||
                        "Không thể tải dữ liệu dashboard."
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
    }, []);

    const summary =
        dashboard?.summary ??
        EMPTY_SUMMARY;

    const allActivityData =
        dashboard?.activity ??
        EMPTY_ACTIVITY;

    const allClassActivities =
        dashboard?.classActivities ??
        EMPTY_CLASS_ACTIVITIES;

    const activityData = useMemo(
        () => allActivityData.slice(-activityDays),
        [allActivityData, activityDays]
    );

    const classActivities = useMemo(
        () => allClassActivities.map(classItem => ({
            ...classItem,
            data: classItem.data.slice(-classActivityDays)
        })),
        [allClassActivities, classActivityDays]
    );

    const mixInsights = useMemo(
        () => buildDashboardInsights(
            allActivityData.slice(-mixDays),
            EMPTY_CLASS_ACTIVITIES
        ),
        [allActivityData, mixDays]
    );

    const classSummaryInsights = useMemo(
        () => buildDashboardInsights(
            EMPTY_ACTIVITY,
            allClassActivities.map(classItem => ({
                ...classItem,
                data: classItem.data.slice(-classSummaryDays)
            }))
        ),
        [allClassActivities, classSummaryDays]
    );

    const classChartData =
        useMemo(() => {
            return buildClassChartData(
                classActivities
            );
        }, [classActivities]);

    const insights =
        useMemo(() => {
            return buildDashboardInsights(
                activityData,
                classActivities
            );
        }, [activityData, classActivities]);

    return (
        <div className="lesson-layout">
            <TeacherSidebar />

            <div className="lesson-main">
                <div className="teacher-dashboard-page">
                    <div className="teacher-dashboard-breadcrumb">
                        <span>Home</span>
                        <ChevronRight size={16} />
                        <strong>Dashboard</strong>
                    </div>

                    <header className="teacher-dashboard-title">
                        <span className="teacher-dashboard-title-icon">
                            <LayoutDashboard />
                        </span>

                        <div>
                            <h1>Teacher Dashboard</h1>

                            <p>
                                An overview of your teaching activity
                            </p>
                        </div>
                    </header>

                    {error && (
                        <div className="teacher-dashboard-error">
                            {error}
                        </div>
                    )}

                    <div className="teacher-dashboard-summary">
                        <div className="teacher-dashboard-stat-card">
                            <div className="teacher-dashboard-stat-icon blue">
                                <BookOpen size={25} />
                            </div>

                            <div className="teacher-dashboard-stat-content">
                                <strong>
                                    {loading
                                        ? "..."
                                        : summary.totalLessons}
                                </strong>

                                <span>
                                    Total lessons
                                </span>
                            </div>
                        </div>

                        <div className="teacher-dashboard-stat-card">
                            <div className="teacher-dashboard-stat-icon green">
                                <ClipboardList size={25} />
                            </div>

                            <div className="teacher-dashboard-stat-content">
                                <strong>
                                    {loading
                                        ? "..."
                                        : summary.totalExercises}
                                </strong>

                                <span>
                                    Total exercises
                                </span>
                            </div>
                        </div>

                        <div className="teacher-dashboard-stat-card">
                            <div className="teacher-dashboard-stat-icon cyan">
                                <FileText size={25} />
                            </div>

                            <div className="teacher-dashboard-stat-content">
                                <strong>
                                    {loading
                                        ? "..."
                                        : summary.totalTests}
                                </strong>

                                <span>
                                    Total tests
                                </span>
                            </div>
                        </div>

                        <div className="teacher-dashboard-stat-card">
                            <div className="teacher-dashboard-stat-icon orange">
                                <Users size={25} />
                            </div>

                            <div className="teacher-dashboard-stat-content">
                                <strong>
                                    {loading
                                        ? "..."
                                        : summary.totalStudents}
                                </strong>

                                <span>
                                    Total students
                                </span>
                            </div>
                        </div>
                    </div>

                    <div className="teacher-dashboard-chart-grid">
                        <div className="teacher-dashboard-chart-card">
                            <div className="teacher-dashboard-card-header">
                                <div>
                                    <h2>
                                        Student Activity
                                    </h2>

                                    <div className="teacher-dashboard-chart-description">
                                        Exercise and test attempt statistics
                                    </div>
                                </div>

                                <select
                                    className="teacher-dashboard-range-select"
                                    value={activityDays}
                                    onChange={event =>
                                        setActivityDays(
                                            Number(
                                                event.target.value
                                            )
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
                                </select>
                            </div>

                            <div className="teacher-dashboard-top-legend">
                                <div className="teacher-dashboard-top-legend-item">
                                    <span
                                        className="teacher-dashboard-top-legend-line"
                                        style={{
                                            backgroundColor:
                                                "#10b981"
                                        }}
                                    />

                                    <span>
                                        Exercise attempts
                                    </span>
                                </div>

                                <div className="teacher-dashboard-top-legend-item">
                                    <span
                                        className="teacher-dashboard-top-legend-line"
                                        style={{
                                            backgroundColor:
                                                "#f97316"
                                        }}
                                    />

                                    <span>
                                        Test attempts
                                    </span>
                                </div>
                            </div>

                            <div className="teacher-dashboard-chart-wrapper">
                                {loading ? (
                                    <div className="teacher-dashboard-chart-state">
                                        Đang tải dữ liệu...
                                    </div>
                                ) : activityData.length === 0 ? (
                                    <div className="teacher-dashboard-chart-state">
                                        Chưa có dữ liệu hoạt động
                                    </div>
                                ) : (
                                    <ResponsiveContainer
                                        width="100%"
                                        height={300}
                                    >
                                        <LineChart
                                            data={activityData}
                                            margin={{
                                                top: 10,
                                                right: 15,
                                                left: -10,
                                                bottom: 5
                                            }}
                                        >
                                            <CartesianGrid
                                                strokeDasharray="4 4"
                                                vertical
                                            />

                                            <XAxis
                                                dataKey="dateLabel"
                                                tickLine={false}
                                            />

                                            <YAxis
                                                allowDecimals={false}
                                                tickLine={false}
                                            />

                                            <Tooltip
                                                formatter={(
                                                    value,
                                                    name
                                                ) => [
                                                        value,
                                                        name
                                                    ]}
                                                labelFormatter={
                                                    label =>
                                                        `Ngày ${label}`
                                                }
                                            />

                                            <Legend
                                                verticalAlign="bottom"
                                                align="center"
                                                iconType="line"
                                            />

                                            <Line
                                                type="monotone"
                                                dataKey="exerciseAttempts"
                                                name="Exercise attempts"
                                                stroke="#10b981"
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
                                                stroke="#f97316"
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

                        </div>

                        <div className="teacher-dashboard-chart-card">
                            <div className="teacher-dashboard-card-header">
                                <div>
                                    <h2>
                                        Activity by Class
                                    </h2>

                                    <div className="teacher-dashboard-chart-description">
                                        Total exercise and test attempts
                                        for each class
                                    </div>
                                </div>

                                <select
                                    className="teacher-dashboard-range-select"
                                    value={classActivityDays}
                                    onChange={event =>
                                        setClassActivityDays(
                                            Number(event.target.value)
                                        )
                                    }
                                >
                                    <option value={7}>Last 7 days</option>
                                    <option value={14}>Last 14 days</option>
                                    <option value={30}>Last 30 days</option>
                                </select>
                            </div>

                            <div className="teacher-dashboard-top-legend teacher-dashboard-top-class-legend">
                                {classActivities.length === 0 ? (
                                    <span className="teacher-dashboard-no-class-legend">
                                        Chưa có lớp
                                    </span>
                                ) : (
                                    classActivities.map(
                                        (
                                            classItem,
                                            index
                                        ) => (
                                            <div
                                                className="teacher-dashboard-top-legend-item"
                                                key={
                                                    classItem.classRoomId
                                                }
                                            >
                                                <span
                                                    className="teacher-dashboard-top-legend-line"
                                                    style={{
                                                        backgroundColor:
                                                            CLASS_LINE_COLORS[
                                                            index %
                                                            CLASS_LINE_COLORS.length
                                                            ]
                                                    }}
                                                />

                                                <span>
                                                    {
                                                        classItem.className
                                                    }
                                                </span>
                                            </div>
                                        )
                                    )
                                )}
                            </div>

                            <div className="teacher-dashboard-chart-wrapper">
                                {loading ? (
                                    <div className="teacher-dashboard-chart-state">
                                        Đang tải dữ liệu...
                                    </div>
                                ) : classActivities.length === 0 ? (
                                    <div className="teacher-dashboard-chart-state">
                                        Giáo viên chưa được phân công
                                        lớp hoặc chưa có dữ liệu hoạt động
                                    </div>
                                ) : (
                                    <ResponsiveContainer
                                        width="100%"
                                        height={300}
                                    >
                                        <LineChart
                                            data={classChartData}
                                            margin={{
                                                top: 10,
                                                right: 15,
                                                left: -10,
                                                bottom: 5
                                            }}
                                        >
                                            <CartesianGrid
                                                strokeDasharray="4 4"
                                                vertical
                                            />

                                            <XAxis
                                                dataKey="dateLabel"
                                                tickLine={false}
                                            />

                                            <YAxis
                                                allowDecimals={false}
                                                tickLine={false}
                                            />

                                            <Tooltip
                                                labelFormatter={
                                                    label =>
                                                        `Ngày ${label}`
                                                }
                                            />

                                            <Legend
                                                verticalAlign="bottom"
                                                align="center"
                                                iconType="line"
                                            />

                                            {classActivities.map(
                                                (
                                                    classItem,
                                                    index
                                                ) => (
                                                    <Line
                                                        key={
                                                            classItem.classRoomId
                                                        }
                                                        type="monotone"
                                                        dataKey={
                                                            classItem.className
                                                        }
                                                        name={
                                                            classItem.className
                                                        }
                                                        stroke={
                                                            CLASS_LINE_COLORS[
                                                            index %
                                                            CLASS_LINE_COLORS.length
                                                            ]
                                                        }
                                                        strokeWidth={3}
                                                        dot={{
                                                            r: 4
                                                        }}
                                                        activeDot={{
                                                            r: 6
                                                        }}
                                                        connectNulls
                                                    />
                                                )
                                            )}
                                        </LineChart>
                                    </ResponsiveContainer>
                                )}
                            </div>
                        </div>
                    </div>

                    <div className="teacher-dashboard-detail-grid">
                        <div className="teacher-dashboard-chart-card teacher-dashboard-compact-card">
                            <div className="teacher-dashboard-card-header">
                                <div>
                                    <h2>
                                        Activity Ratio
                                    </h2>

                                    <div className="teacher-dashboard-chart-description">
                                        Distribution of exercises and tests
                                        for the selected period
                                    </div>
                                </div>

                                <select
                                    className="teacher-dashboard-range-select"
                                    value={mixDays}
                                    onChange={event =>
                                        setMixDays(Number(event.target.value))
                                    }
                                >
                                    <option value={7}>7 ngày gần nhất</option>
                                    <option value={14}>Last 14 days</option>
                                    <option value={30}>Last 30 days</option>
                                </select>
                            </div>

                            <div className="teacher-dashboard-donut-layout">
                                <div className="teacher-dashboard-donut-wrapper">
                                    {loading ? (
                                        <div className="teacher-dashboard-chart-state compact">
                                            Đang tải dữ liệu...
                                        </div>
                                    ) : mixInsights.totalActivity === 0 ? (
                                        <div className="teacher-dashboard-chart-state compact">
                                            Chưa có dữ liệu
                                        </div>
                                    ) : (
                                        <ResponsiveContainer
                                            width="100%"
                                            height={220}
                                        >
                                            <PieChart>
                                                <Pie
                                                    data={mixInsights.activityMix}
                                                    dataKey="value"
                                                    nameKey="name"
                                                    innerRadius={58}
                                                    outerRadius={88}
                                                    paddingAngle={4}
                                                >
                                                    {mixInsights.activityMix.map(
                                                        (entry, index) => (
                                                            <Cell
                                                                key={entry.name}
                                                                fill={
                                                                    MIX_CHART_COLORS[
                                                                    index %
                                                                    MIX_CHART_COLORS.length
                                                                    ]
                                                                }
                                                            />
                                                        )
                                                    )}
                                                </Pie>

                                                <Tooltip />
                                            </PieChart>
                                        </ResponsiveContainer>
                                    )}
                                </div>

                                <div className="teacher-dashboard-donut-summary">
                                    {mixInsights.activityMix.map(
                                        (item, index) => (
                                            <div
                                                key={item.name}
                                                className="teacher-dashboard-donut-row"
                                            >
                                                <span
                                                    style={{
                                                        backgroundColor:
                                                            MIX_CHART_COLORS[
                                                            index %
                                                            MIX_CHART_COLORS.length
                                                            ]
                                                    }}
                                                />

                                                <div>
                                                    <strong>
                                                        {item.value}
                                                    </strong>

                                                    <small>
                                                        {item.name}
                                                    </small>
                                                </div>
                                            </div>
                                        )
                                    )}
                                </div>
                            </div>
                        </div>

                        <div className="teacher-dashboard-chart-card teacher-dashboard-compact-card">
                            <div className="teacher-dashboard-card-header">
                                <div>
                                    <h2>
                                        Total Activity by Class
                                    </h2>

                                    <div className="teacher-dashboard-chart-description">
                                        A quick comparison of attempts
                                        for each class
                                    </div>
                                </div>

                                <select
                                    className="teacher-dashboard-range-select"
                                    value={classSummaryDays}
                                    onChange={event =>
                                        setClassSummaryDays(
                                            Number(event.target.value)
                                        )
                                    }
                                >
                                    <option value={7}>7 ngày gần nhất</option>
                                    <option value={14}>Last 14 days</option>
                                    <option value={30}>Last 30 days</option>
                                </select>
                            </div>

                            <div className="teacher-dashboard-bar-wrapper">
                                {loading ? (
                                    <div className="teacher-dashboard-chart-state compact">
                                        Loading data...
                                    </div>
                                ) : classSummaryInsights.classSummary.length === 0 ? (
                                    <div className="teacher-dashboard-chart-state compact">
                                        Chưa có lớp
                                    </div>
                                ) : (
                                    <ResponsiveContainer
                                        width="100%"
                                        height={240}
                                    >
                                        <BarChart
                                            data={classSummaryInsights.classSummary}
                                            margin={{
                                                top: 12,
                                                right: 12,
                                                left: -10,
                                                bottom: 4
                                            }}
                                        >
                                            <CartesianGrid
                                                strokeDasharray="4 4"
                                                vertical={false}
                                            />

                                            <XAxis
                                                dataKey="className"
                                                tickLine={false}
                                            />

                                            <YAxis
                                                allowDecimals={false}
                                                tickLine={false}
                                            />

                                            <Tooltip />

                                            <Bar
                                                dataKey="exerciseAttempts"
                                                name="Exercises"
                                                fill="#10b981"
                                                stackId="activity"
                                                radius={[0, 0, 8, 8]}
                                            />

                                            <Bar
                                                dataKey="testAttempts"
                                                name="Tests"
                                                fill="#f97316"
                                                stackId="activity"
                                                radius={[8, 8, 0, 0]}
                                            />
                                        </BarChart>
                                    </ResponsiveContainer>
                                )}
                            </div>
                        </div>

                        <div className="teacher-dashboard-chart-card teacher-dashboard-compact-card teacher-dashboard-class-table-card">
                            <div className="teacher-dashboard-card-header">
                                <div>
                                    <h2>
                                        Classes to Monitor
                                    </h2>

                                    <div className="teacher-dashboard-chart-description">
                                        Ranked by total attempts
                                    </div>
                                </div>
                            </div>

                            {loading ? (
                                <div className="teacher-dashboard-table-state">
                                    Đang tải dữ liệu...
                                </div>
                            ) : classSummaryInsights.classSummary.length === 0 ? (
                                <div className="teacher-dashboard-table-state">
                                    Chưa có lớp để thống kê
                                </div>
                            ) : (
                                <div className="teacher-dashboard-class-table">
                                    {classSummaryInsights.classSummary
                                        .slice(0, 5)
                                        .map((classItem, index) => (
                                            <div
                                                key={classItem.classRoomId}
                                                className="teacher-dashboard-class-row"
                                            >
                                                <span className="teacher-dashboard-class-rank">
                                                    {index + 1}
                                                </span>

                                                <div className="teacher-dashboard-class-name">
                                                    <strong>
                                                        {classItem.className}
                                                    </strong>

                                                    <small>
                                                        {classItem.exerciseAttempts} exercises
                                                        {" · "}
                                                        {classItem.testAttempts} tests
                                                    </small>
                                                </div>

                                                <span className="teacher-dashboard-class-total">
                                                    {classItem.totalAttempts}
                                                </span>
                                            </div>
                                        ))}
                                </div>
                            )}
                        </div>
                    </div>
                </div>
            </div>
        </div>
    );
}

export default TeacherHomePage;
