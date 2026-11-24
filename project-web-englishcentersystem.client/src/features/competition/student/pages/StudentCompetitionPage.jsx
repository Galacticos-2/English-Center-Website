import {
    useEffect,
    useMemo,
    useState
} from "react";

import {
    Clock3,
    Eye,
    Play,
    RotateCcw,
    Search,
    RefreshCw,
    Timer,
    Trophy
} from "lucide-react";

import {
    useNavigate
} from "react-router-dom";

import {
    getMyCompetitionsApi,
    getCompetitionAttemptResultApi,
    startCompetitionAttemptApi
} from "../api/studentCompetitionApi";

import {
    saveCompetitionAttempt,
    saveCompetitionResult
} from "../utils/competitionStorage";

import {
    formatVietnamDateTime,
    parseUtcDate
} from "../../../../utils/dateTimeUtils";

import "../styles/studentCompetition.css";

// Status code / string -> { label, category }
// category drives the accent color + filter pill grouping.
// (Same Vietnamese copy as before, only reorganized.)
const STATUS_META = {
    1: { label: "Not Open", category: "upcoming" },
    2: { label: "Ongoing", category: "ongoing" },
    3: { label: "Closed", category: "closed" },
    4: { label: "Awaiting Grading", category: "waiting" },
    5: { label: "Ranked", category: "ranked" },
    6: { label: "Cancelled", category: "cancelled" },
    7: { label: "Error", category: "error" },

    Scheduled: { label: "Not Open", category: "upcoming" },
    Open: { label: "Ongoing", category: "ongoing" },
    Closed: { label: "Closed", category: "closed" },
    WaitingForGrading: { label: "Awaiting Grading", category: "waiting" },
    Ranked: { label: "Ranked", category: "ranked" },
    Cancelled: { label: "Cancelled", category: "cancelled" },
    Failed: { label: "Error", category: "error" }
};

const FILTER_ORDER = [
    "ongoing",
    "upcoming",
    "waiting",
    "ranked",
    "closed",
    "cancelled",
    "error"
];

const FILTER_LABEL = {
    ongoing: "Ongoing",
    upcoming: "Upcoming",
    waiting: "Awaiting Grading",
    ranked: "Ranked",
    closed: "Closed",
    cancelled: "Cancelled",
    error: "Error"
};

const getStatusMeta = status =>
    STATUS_META[status] ||
    STATUS_META[Number(status)] || {
        label: String(status ?? ""),
        category: "upcoming"
    };

const pad2 = n => String(n).padStart(2, "0");

// Pure presentational helper – computes a live "time left" label
// for ongoing competitions. Does not touch attempt/timer logic.
const getCountdown = (
    deadlineAt,
    now
) => {
    const deadlineDate =
        parseUtcDate(deadlineAt);

    if (!deadlineDate) {
        return null;
    }

    const diffMs =
        deadlineDate.getTime() - now;

    if (diffMs <= 0) {
        return {
            text: "Expired",
            urgent: true
        };
    }

    const totalSeconds =
        Math.floor(diffMs / 1000);

    const days =
        Math.floor(
            totalSeconds / 86400
        );

    const hours =
        Math.floor(
            (totalSeconds % 86400) / 3600
        );

    const minutes =
        Math.floor(
            (totalSeconds % 3600) / 60
        );

    const seconds =
        totalSeconds % 60;

    if (days > 0) {
        return {
            text:
                `${days} days remaining ` +
                `${pad2(hours)}h`,
            urgent: false
        };
    }

    return {
        text:
            `${pad2(hours)}:` +
            `${pad2(minutes)}:` +
            `${pad2(seconds)}`,

        urgent:
            totalSeconds < 3600
    };
};



function StudentCompetitionPage() {
    const navigate =
        useNavigate();

    const [
        competitions,
        setCompetitions
    ] = useState([]);

    const [loading, setLoading] =
        useState(true);

    const [startingId, setStartingId] =
        useState(null);

    const [searchText, setSearchText] =
        useState("");

    const [statusFilter, setStatusFilter] =
        useState("all");

    // Ticks once a second so ongoing-competition countdown chips
    // stay live. Purely presentational – no data is refetched.
    const [now, setNow] =
        useState(() => Date.now());

    useEffect(() => {
        const timer = setInterval(
            () => setNow(Date.now()),
            1000
        );

        return () => clearInterval(timer);
    }, []);

    const loadCompetitions = async () => {
        setLoading(true);

        try {
            const data =
                await getMyCompetitionsApi();

            setCompetitions(
                Array.isArray(data)
                    ? data
                    : []
            );
        }
        catch (error) {
            console.error(
                "Load competitions error:",
                error.response?.data ||
                error
            );

            alert(
                error.response?.data?.message ||
                "Unable to load competitions"
            );
        }
        finally {
            setLoading(false);
        }
    };

    useEffect(() => {
        loadCompetitions();
    }, []);

    const competitionsWithMeta =
        useMemo(() =>
            competitions.map(item => ({
                ...item,
                __meta: getStatusMeta(item.status)
            })),
            [competitions]);

    const filterCounts =
        useMemo(() => {
            const counts = {};

            competitionsWithMeta.forEach(item => {
                const category =
                    item.__meta.category;

                counts[category] =
                    (counts[category] || 0) + 1;
            });

            return counts;
        }, [competitionsWithMeta]);

    const filteredCompetitions =
        useMemo(() => {
            const keyword =
                searchText
                    .trim()
                    .toLowerCase();

            return [...competitionsWithMeta]
                .filter(item =>
                    (statusFilter === "all" ||
                        item.__meta.category ===
                        statusFilter) &&
                    (!keyword ||
                        item.title
                            ?.toLowerCase()
                            .includes(keyword) ||
                        item.competitionTestTitle
                            ?.toLowerCase()
                            .includes(keyword))
                )
                .sort((a, b) => {
                    const bStart =
                        parseUtcDate(b.startsAt);

                    const aStart =
                        parseUtcDate(a.startsAt);

                    return (
                        (bStart?.getTime() ?? 0) -
                        (aStart?.getTime() ?? 0)
                    );
                });
        }, [
            competitionsWithMeta,
            searchText,
            statusFilter
        ]);

    const heroStats =
        useMemo(() => ({
            total: competitions.length,
            ongoing: filterCounts.ongoing || 0,
            ranked: filterCounts.ranked || 0
        }), [
            competitions.length,
            filterCounts
        ]);

    const handleStart =
        async competition => {
            setStartingId(
                competition
                    .competitionTestAssignmentId
            );

            try {
                const attempt =
                    await startCompetitionAttemptApi(
                        competition
                            .competitionTestAssignmentId
                    );

                saveCompetitionAttempt(
                    attempt
                );

                navigate(
                    `/student/competitions/${competition.competitionTestAssignmentId}/attempt`
                );
            }
            catch (error) {
                alert(
                    error.response?.data?.message ||
                    error.response?.data?.title ||
                    "Unable to start the competition"
                );

                await loadCompetitions();
            }
            finally {
                setStartingId(null);
            }
        };

    const handleViewResult =
        async competition => {
            if (
                !competition
                    .competitionTestAttemptId
            ) {
                return;
            }

            try {
                const result =
                    await getCompetitionAttemptResultApi(
                        competition
                            .competitionTestAttemptId
                    );

                saveCompetitionResult(
                    result
                );

                navigate(
                    "/student/competitions/result"
                );
            }
            catch (error) {
                alert(
                    error.response?.data?.message ||
                    "Unable to load results"
                );
            }
        };

    const getAction = competition => {
        if (competition.canContinue) {
            return {
                text: "Continue Competition",
                icon: <RotateCcw size={17} />,
                variant: "continue",
                handler: () =>
                    handleStart(competition)
            };
        }

        if (competition.canStart) {
            return {
                text: "Start Competition",
                icon: <Play size={17} />,
                variant: "start",
                handler: () =>
                    handleStart(competition)
            };
        }

        if (competition.canViewResult) {
            return {
                text: "View Results",
                icon: <Eye size={17} />,
                variant: "result",
                handler: () =>
                    handleViewResult(
                        competition
                    )
            };
        }

        return null;
    };

    return (
        <div className="sccomp-page">
            <section className="sccomp-hero">
                <div className="sccomp-hero-pattern" aria-hidden="true" />

                <div className="sccomp-hero-main">
                    <div className="sccomp-hero-icon">📝</div>

                    <div>
                        <h1 className="sccomp-hero-title">
                            Compete with Your Class
                        </h1>

                        <p className="sccomp-hero-desc">
                            Complete tests on time and reach
                            the top of the leaderboard.
                        </p>
                    </div>
                </div>

                <div className="sccomp-hero-divider" aria-hidden="true" />

                <div className="sccomp-hero-metrics">
                    <div className="sccomp-metric sccomp-metric--primary">
                        <strong>{heroStats.total}</strong>
                        <span>Total Competitions</span>
                    </div>

                    <div className="sccomp-metric sccomp-metric--secondary">
                        <strong>{heroStats.ongoing}</strong>
                        <span>Ongoing</span>
                    </div>

                    <div className="sccomp-metric sccomp-metric--success">
                        <strong>{heroStats.ranked}</strong>
                        <span>Ranked</span>
                    </div>
                </div>

                <Trophy
                    className="sccomp-hero-emblem"
                    size={60}
                    strokeWidth={1.4}
                />
            </section>

            <div className="sccomp-toolbar">
                <div className="sccomp-search">
                    <Search size={18} />

                    <input
                        type="text"
                        placeholder="Search competitions by name..."
                        value={searchText}
                        onChange={event =>
                            setSearchText(
                                event.target.value
                            )
                        }
                    />
                </div>

                <button
                    type="button"
                    className="sccomp-refresh-btn"
                    onClick={loadCompetitions}
                    disabled={loading}
                >
                    <RefreshCw
                        size={16}
                        className={
                            loading
                                ? "sccomp-spin"
                                : ""
                        }
                    />
                    Refresh
                </button>
            </div>

            <div className="sccomp-filters">
                <button
                    type="button"
                    className={
                        "sccomp-filter-pill" +
                        (statusFilter === "all"
                            ? " active"
                            : "")
                    }
                    onClick={() =>
                        setStatusFilter("all")
                    }
                >
                    All
                    <span className="sccomp-filter-count">
                        {competitions.length}
                    </span>
                </button>

                {FILTER_ORDER
                    .filter(category =>
                        filterCounts[category])
                    .map(category => (
                        <button
                            key={category}
                            type="button"
                            className={
                                "sccomp-filter-pill" +
                                (statusFilter ===
                                    category
                                    ? " active"
                                    : "")
                            }
                            onClick={() =>
                                setStatusFilter(
                                    category
                                )
                            }
                        >
                            {FILTER_LABEL[category]}
                            <span className="sccomp-filter-count">
                                {filterCounts[category]}
                            </span>
                        </button>
                    ))}
            </div>

            {loading && (
                <div className="sccomp-grid">
                    {Array.from({ length: 6 }).map(
                        (_, index) => (
                            <div
                                key={index}
                                className="sccomp-skeleton-card"
                            />
                        )
                    )}
                </div>
            )}

            {!loading &&
                filteredCompetitions.length ===
                0 && (
                    <div className="sccomp-empty">
                        <Trophy
                            size={40}
                            strokeWidth={1.4}
                        />
                        <h3>
                            No matching competitions
                        </h3>
                        <p>
                            There are no competitions for
                            your class, or none match the
                            current filters.
                        </p>
                    </div>
                )}

            {!loading &&
                filteredCompetitions.length > 0 && (
                    <div className="sccomp-grid">
                        {filteredCompetitions.map(
                            (competition, index) => {
                                const action =
                                    getAction(
                                        competition
                                    );

                                const category =
                                    competition
                                        .__meta
                                        .category;

                                const countdown =
                                    category ===
                                        "ongoing"
                                        ? getCountdown(
                                            competition.deadlineAt,
                                            now
                                        )
                                        : null;

                                const isStarting =
                                    startingId ===
                                    competition
                                        .competitionTestAssignmentId;

                                return (
                                    <article
                                        key={
                                            competition
                                                .competitionTestAssignmentId
                                        }
                                        className={
                                            `sccomp-card sccomp-card--${category}`
                                        }
                                        style={{
                                            animationDelay:
                                                `${Math.min(index, 8) * 45}ms`
                                        }}
                                    >
                                        <div className="sccomp-card-top">
                                            <span
                                                className={
                                                    `sccomp-status-pill sccomp-status-pill--${category}`
                                                }
                                            >
                                                <i className="sccomp-status-dot" />
                                                {
                                                    competition
                                                        .__meta
                                                        .label
                                                }
                                            </span>

                                            {countdown && (
                                                <span
                                                    className={
                                                        "sccomp-countdown-chip" +
                                                        (countdown.urgent
                                                            ? " urgent"
                                                            : "")
                                                    }
                                                >
                                                    <Timer
                                                        size={13}
                                                    />
                                                    {
                                                        countdown.text
                                                    }
                                                </span>
                                            )}
                                        </div>

                                        <h2 className="sccomp-card-title">
                                            {competition.title}
                                        </h2>

                                        <p className="sccomp-card-desc">
                                            {competition.description ||
                                                "No description"}
                                        </p>

                                        <div className="sccomp-schedule">
                                            <div className="sccomp-schedule-row">
                                                <Clock3
                                                    size={15}
                                                />
                                                <span>
                                                    Starts:{" "}
                                                    <strong>
                                                        {formatVietnamDateTime(
                                                            competition
                                                                .startsAt
                                                        )}
                                                    </strong>
                                                </span>
                                            </div>

                                            <div className="sccomp-schedule-row">
                                                <Clock3
                                                    size={15}
                                                />
                                                <span>
                                                    Deadline:{" "}
                                                    <strong>
                                                        {formatVietnamDateTime(
                                                            competition
                                                                .deadlineAt
                                                        )}
                                                    </strong>
                                                </span>
                                            </div>

                                            <div className="sccomp-schedule-row">
                                                <Clock3
                                                    size={15}
                                                />
                                                <span>
                                                    Duration:{" "}
                                                    <strong>
                                                        {
                                                            competition
                                                                .durationMinutes
                                                        }{" "}
                                                        minutes
                                                    </strong>
                                                </span>
                                            </div>
                                        </div>

                                        <div className="sccomp-stats-row">
                                            <div>
                                                <strong>
                                                    {
                                                        competition
                                                            .totalQuestions
                                                    }
                                                </strong>
                                                <span>
                                                    Questions
                                                </span>
                                            </div>

                                            <div className="sccomp-stats-divider" />

                                            <div>
                                                <strong>
                                                    {
                                                        competition
                                                            .totalPoints
                                                    }
                                                </strong>
                                                <span>
                                                    Points
                                                </span>
                                            </div>
                                        </div>

                                        <div className="sccomp-card-actions">
                                            {action && (
                                                <button
                                                    type="button"
                                                    className={
                                                        `sccomp-btn sccomp-btn--${action.variant}`
                                                    }
                                                    disabled={
                                                        isStarting
                                                    }
                                                    onClick={
                                                        action.handler
                                                    }
                                                >
                                                    {isStarting ? (
                                                        <RotateCcw
                                                            size={17}
                                                            className="sccomp-spin"
                                                        />
                                                    ) : (
                                                        action.icon
                                                    )}
                                                    {isStarting
                                                        ? "Opening..."
                                                        : action.text}
                                                </button>
                                            )}

                                            <button
                                                type="button"
                                                className="sccomp-btn sccomp-btn--leaderboard"
                                                onClick={() =>
                                                    navigate(
                                                        `/student/competitions/${competition.competitionTestAssignmentId}/leaderboard`
                                                    )
                                                }
                                            >
                                                <Trophy
                                                    size={16}
                                                />
                                                Leaderboard
                                            </button>
                                        </div>
                                    </article>
                                );
                            }
                        )}
                    </div>
                )}
        </div>
    );
}

export default StudentCompetitionPage;
