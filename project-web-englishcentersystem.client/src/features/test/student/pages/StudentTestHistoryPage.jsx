import { useEffect, useState } from "react";
import { useNavigate, useParams } from "react-router-dom";
import {
    getMyStudentTestsApi,
    getTestAttemptResultApi
} from "../api/studentTestApi";
import {
    formatVietnamDateTimeWithSeconds,
    formatVietnamDateOnly
} from "../../../../utils/dateTimeUtils";
import "../../../lesson/styles/lesson.css";
import "../../../student/styles/exercise.css";

/* =========================================================
   Small inline icons (no extra dependency needed)
========================================================= */

const IconBack = () => (
    <svg width="16" height="16" viewBox="0 0 24 24" fill="none">
        <path d="M15 18l-6-6 6-6" stroke="currentColor" strokeWidth="2.3"
            strokeLinecap="round" strokeLinejoin="round" />
    </svg>
);

const IconHistory = () => (
    <svg width="22" height="22" viewBox="0 0 24 24" fill="none">
        <path d="M3 12a9 9 0 1 0 3-6.7" stroke="currentColor" strokeWidth="2"
            strokeLinecap="round" strokeLinejoin="round" />
        <path d="M3 4v5h5" stroke="currentColor" strokeWidth="2"
            strokeLinecap="round" strokeLinejoin="round" />
        <path d="M12 7v5l3.5 2" stroke="currentColor" strokeWidth="2"
            strokeLinecap="round" strokeLinejoin="round" />
    </svg>
);

const IconCalendar = () => (
    <svg width="15" height="15" viewBox="0 0 24 24" fill="none">
        <rect x="3" y="5" width="18" height="16" rx="3" stroke="currentColor" strokeWidth="2" />
        <path d="M3 10h18M8 3v4M16 3v4" stroke="currentColor" strokeWidth="2"
            strokeLinecap="round" />
    </svg>
);

const IconTrophy = () => (
    <svg width="20" height="20" viewBox="0 0 24 24" fill="none">
        <path d="M8 21h8M12 17v4M7 4h10v4a5 5 0 0 1-10 0V4z" stroke="currentColor"
            strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" />
        <path d="M7 5H4a1 1 0 0 0-1 1c0 2.5 1.5 4.3 4 4.7M17 5h3a1 1 0 0 1 1 1c0 2.5-1.5 4.3-4 4.7"
            stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" />
    </svg>
);

const IconRepeat = () => (
    <svg width="20" height="20" viewBox="0 0 24 24" fill="none">
        <path d="M17 2l4 4-4 4" stroke="currentColor" strokeWidth="2"
            strokeLinecap="round" strokeLinejoin="round" />
        <path d="M3 11V9a4 4 0 0 1 4-4h14" stroke="currentColor" strokeWidth="2"
            strokeLinecap="round" strokeLinejoin="round" />
        <path d="M7 22l-4-4 4-4" stroke="currentColor" strokeWidth="2"
            strokeLinecap="round" strokeLinejoin="round" />
        <path d="M21 13v2a4 4 0 0 1-4 4H3" stroke="currentColor" strokeWidth="2"
            strokeLinecap="round" strokeLinejoin="round" />
    </svg>
);

const IconClock = () => (
    <svg width="20" height="20" viewBox="0 0 24 24" fill="none">
        <circle cx="12" cy="12" r="9" stroke="currentColor" strokeWidth="2" />
        <path d="M12 7v5l3.5 2" stroke="currentColor" strokeWidth="2"
            strokeLinecap="round" strokeLinejoin="round" />
    </svg>
);

const IconEye = () => (
    <svg width="15" height="15" viewBox="0 0 24 24" fill="none">
        <path d="M1 12s4-7 11-7 11 7 11 7-4 7-11 7-11-7-11-7z" stroke="currentColor"
            strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" />
        <circle cx="12" cy="12" r="3" stroke="currentColor" strokeWidth="2" />
    </svg>
);

const IconEmpty = () => (
    <svg width="34" height="34" viewBox="0 0 24 24" fill="none">
        <path d="M3 12a9 9 0 1 0 3-6.7" stroke="currentColor" strokeWidth="2"
            strokeLinecap="round" strokeLinejoin="round" />
        <path d="M3 4v5h5" stroke="currentColor" strokeWidth="2"
            strokeLinecap="round" strokeLinejoin="round" />
        <path d="M9.5 14.5l5-5M9.5 9.5l5 5" stroke="currentColor" strokeWidth="2"
            strokeLinecap="round" />
    </svg>
);

/* =========================================================
   Helpers
========================================================= */

// Map trạng thái backend trả về -> style hiển thị.
// Không đổi giá trị status, chỉ đổi cách hiển thị.
function getStatusMeta(status) {
    const raw = String(status || "").toLowerCase();

    if (/(complete|done|finish|hoan\s*thanh|hoàn\s*thành|submitted|graded|cham\s*xong|chấm\s*xong)/.test(raw)) {
        return { className: "stth-badge done", label: status || "Completed" };
    }

    if (/(progress|pending|grading|dang\s*cham|đang\s*chấm|processing)/.test(raw)) {
        return { className: "stth-badge pending", label: status || "Processing" };
    }

    if (/(fail|missed|expired|late|huy|hủy|error)/.test(raw)) {
        return { className: "stth-badge failed", label: status || "Invalid" };
    }

    return { className: "stth-badge neutral", label: status || "-" };
}

function getScoreClass(score) {
    if (score === null || score === undefined || score === "") return "stth-score-empty";
    const n = Number(score);
    if (Number.isNaN(n)) return "stth-score-empty";
    if (n >= 7) return "stth-score-high";
    if (n >= 5) return "stth-score-mid";
    return "stth-score-low";
}

function averageOf(item) {
    const values = [item.listeningScore, item.readingScore, item.writingScore, item.speakingScore]
        .map(Number)
        .filter((v) => !Number.isNaN(v));

    if (values.length === 0) return null;
    return values.reduce((a, b) => a + b, 0) / values.length;
}

function StudentTestHistoryPage() {
    const { testAssignmentId } = useParams();
    const navigate = useNavigate();

    const [test, setTest] = useState(null);
    const [loading, setLoading] = useState(true);
    const [openingId, setOpeningId] = useState(null);

    useEffect(() => {
        const load = async () => {
            const data = await getMyStudentTestsApi();

            const found = data.find(x =>
                String(x.testAssignmentId) === String(testAssignmentId)
            );

            setTest(found || null);
            setLoading(false);
        };

        load();
    }, [testAssignmentId]);

    const openResult = async (testAttemptId) => {
        try {
            setOpeningId(testAttemptId);
            const result = await getTestAttemptResultApi(testAttemptId);

            localStorage.setItem(
                "student-test-result",
                JSON.stringify(result)
            );

            navigate("/student/tests/result");
        } finally {
            setOpeningId(null);
        }
    };

    if (loading) {
        return (
            <div className="stth-page">
                <div className="stth-loading">
                    <span className="stth-spin"><IconClock /></span>
                    Loading attempt history...
                </div>
            </div>
        );
    }

    if (!test) {
        return (
            <div className="stth-page">
                <div className="stth-not-found">
                    <div className="stth-not-found-icon"><IconEmpty /></div>
                    <h2>Test not found</h2>
                    <p>This test does not exist or has not been assigned to you.</p>
                    <button
                        className="stth-back-btn"
                        onClick={() => navigate("/student/tests")}
                    >
                        <IconBack /> Back to Test List
                    </button>
                </div>
            </div>
        );
    }

    const attempts = test.attemptHistory || [];
    const attemptCount = attempts.length;

    const averages = attempts.map(averageOf).filter((v) => v !== null);
    const bestScore = averages.length > 0 ? Math.max(...averages) : null;
    const lastAttempt = attempts[0];

    return (
        <div className="stth-page">
            <div className="stth-content">

                <button
                    className="stth-back-btn"
                    onClick={() => navigate("/student/tests")}
                >
                    <IconBack /> Back to Test List
                </button>

                {/* Hero */}
                <div className="stth-hero">
                    <div className="stth-hero-left">
                        <div className="stth-hero-icon"><IconHistory /></div>
                        <div>
                            <span className="stth-hero-eyebrow">Attempt History</span>
                            <h1>{test.title}</h1>
                            <p>Review all your attempts and scores for this test.</p>
                        </div>
                    </div>

                    <div className="stth-hero-stats">
                        <div className="stth-hero-stat">
                            <IconRepeat />
                            <div>
                                <strong>{attemptCount}</strong>
                                <span>Attempts</span>
                            </div>
                        </div>
                        <div className="stth-hero-stat">
                            <IconTrophy />
                            <div>
                                <strong>{bestScore !== null ? bestScore.toFixed(1) : "-"}</strong>
                                <span>Highest Score</span>
                            </div>
                        </div>
                        <div className="stth-hero-stat">
                            <IconCalendar />
                            <div>
                                <strong>
                                    {lastAttempt?.submittedAt
                                        ? formatVietnamDateOnly(
                                            lastAttempt.submittedAt
                                        )
                                        : "-"}
                                </strong>
                                <span>Latest Attempt</span>
                            </div>
                        </div>
                    </div>
                </div>

                {/* History */}
                <div className="stth-card">
                    <div className="stth-card-header">
                        <div className="stth-card-heading">
                          
                            <h2>Attempts</h2>
                        </div>
                        <span className="stth-card-badge">{attemptCount} attempts</span>
                    </div>

                    {attempts.length > 0 ? (
                        <div className="stth-table-wrap">
                            <table className="stth-table">
                                <thead>
                                    <tr>
                                        <th>Attempt</th>
                                        <th>Submitted</th>
                                        <th>Listening</th>
                                        <th>Reading</th>
                                        <th>Writing</th>
                                        <th>Speaking</th>
                                        <th>Status</th>
                                        <th className="stth-th-action">Details</th>
                                    </tr>
                                </thead>

                                <tbody>
                                    {attempts.map((item, index) => {
                                        const status = getStatusMeta(item.status);

                                        return (
                                            <tr key={item.testAttemptId}>
                                                <td>
                                                    <span className="stth-attempt-number">
                                                        #{index + 1}
                                                    </span>
                                                </td>

                                                <td>
                                                    {item.submittedAt
                                                        ? formatVietnamDateTimeWithSeconds(
                                                            item.submittedAt
                                                        )
                                                        : "Not submitted"}
                                                </td>

                                                <td>
                                                    <span className={`stth-score-pill ${getScoreClass(item.listeningScore)}`}>
                                                        {item.listeningScore ?? "-"}
                                                    </span>
                                                </td>
                                                <td>
                                                    <span className={`stth-score-pill ${getScoreClass(item.readingScore)}`}>
                                                        {item.readingScore ?? "-"}
                                                    </span>
                                                </td>
                                                <td>
                                                    <span className={`stth-score-pill ${getScoreClass(item.writingScore)}`}>
                                                        {item.writingScore ?? "-"}
                                                    </span>
                                                </td>
                                                <td>
                                                    <span className={`stth-score-pill ${getScoreClass(item.speakingScore)}`}>
                                                        {item.speakingScore ?? "-"}
                                                    </span>
                                                </td>

                                                <td>
                                                    <span className={status.className}>
                                                        {status.label}
                                                    </span>
                                                </td>

                                                <td>
                                                    <button
                                                        className="stth-detail-btn"
                                                        disabled={openingId === item.testAttemptId}
                                                        onClick={() => openResult(item.testAttemptId)}
                                                    >
                                                        {openingId === item.testAttemptId ? (
                                                            <span className="stth-spin"><IconClock /></span>
                                                        ) : (
                                                            <IconEye />
                                                        )}
                                                        View
                                                    </button>
                                                </td>
                                            </tr>
                                        );
                                    })}
                                </tbody>
                            </table>
                        </div>
                    ) : (
                        <div className="stth-empty">
                            <div className="stth-empty-icon"><IconEmpty /></div>
                            <h3>No attempt history yet</h3>
                            <p>You have not completed any attempts for this test.</p>
                        </div>
                    )}
                </div>
            </div>
        </div>
    );
}

export default StudentTestHistoryPage;
