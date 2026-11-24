import {
    useEffect,
    useMemo,
    useState
} from "react";

import {
    ArrowLeft,
    Eye,
    Medal,
    Trophy
} from "lucide-react";

import {
    useNavigate,
    useParams
} from "react-router-dom";
import {
    formatVietnamDateTime
} from "../../../../utils/dateTimeUtils";
import TeacherSidebar
    from "../../../lesson/teacher/TeacherSidebar";

import {
    getTeacherCompetitionAttemptDetailApi,
    getTeacherCompetitionLeaderboardApi
} from "../api/competitionAssignmentApi";

import "../../../lesson/styles/lesson.css";
import "../../../exercise/teacher/styles/questionBank.css";
import "../../student/styles/studentCompetition.css";
import "../styles/teacherCompetition.css";

function TeacherCompetitionLeaderboardPage() {
    const {
        competitionTestAssignmentId
    } = useParams();

    const navigate =
        useNavigate();

    const [leaderboard, setLeaderboard] =
        useState(null);

    const [selectedAttempt, setSelectedAttempt] =
        useState(null);

    const [loading, setLoading] =
        useState(true);

    useEffect(() => {
        if (!competitionTestAssignmentId) {
            setLeaderboard(null);
            setLoading(false);
            return;
        }

        const loadLeaderboard = async () => {
            setLoading(true);

            try {
                const data =
                    await getTeacherCompetitionLeaderboardApi(
                        competitionTestAssignmentId
                    );

                setLeaderboard(data);
                setSelectedAttempt(null);
            }
            catch (error) {
                console.error(
                    "Load teacher competition leaderboard error:",
                    error.response?.data ||
                    error
                );

                alert(
                    error.response?.data?.message ||
                    error.response?.data?.title ||
                    "Unable to load the leaderboard."
                );

                navigate(
                    "/teacher/competition"
                );
            }
            finally {
                setLoading(false);
            }
        };

        loadLeaderboard();
    }, [
        competitionTestAssignmentId,
        navigate
    ]);
    const groupedSelectedAnswers =
        useMemo(() => {
            const answers =
                selectedAttempt?.result?.answers || [];

            return {
                listening:
                    answers.filter(
                        answer =>
                            answer.listeningQuestionId
                    ),

                reading:
                    answers.filter(
                        answer =>
                            answer.readingQuestionId
                    ),

                writing:
                    answers.filter(
                        answer =>
                            answer.writingTaskId
                    ),

                speaking:
                    answers.filter(
                        answer =>
                            answer.speakingTaskId
                    )
            };
        }, [selectedAttempt]);
    const statusMap = {
        Submitted: "Submitted",
        PendingAiGrading:
            "Waiting for AI grading",
        AiGrading:
            "AI grading in progress",
        AiGraded:
            "Graded",
        AiGradingFailed:
            "AI grading failed",
        Reviewed:
            "Reviewed",
        Ranked:
            "Ranked"
    };
    const handleViewAttempt =
        async competitionTestAttemptId => {
            try {
                const data =
                    await getTeacherCompetitionAttemptDetailApi(
                        competitionTestAttemptId
                    );

                setSelectedAttempt(data);
            }
            catch (error) {
                alert(
                    error.response?.data?.message ||
                    "Unable to load the attempt."
                );
            }
        };

    const formatSeconds = seconds => {
        if (
            seconds == null ||
            seconds < 0
        ) {
            return "-";
        }

        const hours =
            Math.floor(
                seconds / 3600
            );

        const minutes =
            Math.floor(
                (seconds % 3600) / 60
            );

        const remainingSeconds =
            seconds % 60;

        return [
            hours > 0
                ? `${hours} hours`
                : null,

            minutes > 0
                ? `${minutes} minutes`
                : null,

            `${remainingSeconds} seconds`
        ]
            .filter(Boolean)
            .join(" ");
    };

    const renderRank = rank => {
        if (rank === 1) {
            return (
                <Trophy size={25} />
            );
        }

        if (
            rank === 2 ||
            rank === 3
        ) {
            return (
                <Medal size={24} />
            );
        }

        return `#${rank}`;
    };

    const getStatusText = status => {
        const map = {
            Submitted: "Submitted",
            PendingAiGrading: "Grading in progress",
            AiGraded: "Graded",
            Ranked: "Ranked",
            Open: "Open",
            Closed: "Closed"
        };

        return map[status] ||
            status ||
            "-";
    };

    if (loading) {
        return (
            <div className="lesson-layout">
                <TeacherSidebar />

                <main className="lesson-main">
                    <div className="competition-empty">
                        Loading leaderboard...
                    </div>
                </main>
            </div>
        );
    }

    return (
        <div className="lesson-layout">
            <TeacherSidebar />

            <main className="lesson-main teacher-leaderboard-main">
                <div className="teacher-leaderboard-page">
                    <button
                        type="button"
                        className="competition-secondary-btn"
                        onClick={() =>
                            navigate(
                                "/teacher/competition"
                            )
                        }
                    >
                        <ArrowLeft size={18} />
                        Back
                    </button>

                    {!leaderboard && (
                        <div className="competition-empty">
                            No leaderboard data available.
                        </div>
                    )}

                    {leaderboard && (
                        <>
                            <section className="student-leaderboard-hero">
                                <Trophy size={56} />

                                <div>
                                    <span>
                                        Leaderboard
                                    </span>

                                    <h1>
                                        {
                                            leaderboard
                                                .competitionTitle
                                        }
                                    </h1>

                                    <p>
                                        {
                                            leaderboard
                                                .classRoomName
                                        }
                                    </p>
                                </div>
                            </section>

                            <div className="teacher-leaderboard-summary">
                                <div>
                                    <span>
                                        Total Students
                                    </span>

                                    <strong>
                                        {
                                            leaderboard
                                                .totalStudentsInClass
                                        }
                                    </strong>
                                </div>

                                <div>
                                    <span>
                                        Participated
                                    </span>

                                    <strong>
                                        {
                                            leaderboard
                                                .participantCount
                                        }
                                    </strong>
                                </div>

                                <div>
                                    <span>
                                        Submitted
                                    </span>

                                    <strong>
                                        {
                                            leaderboard
                                                .submittedCount
                                        }
                                    </strong>
                                </div>

                                <div>
                                    <span>
                                        Status
                                    </span>

                                    <strong>
                                        {getStatusText(
                                            leaderboard.status
                                        )}
                                    </strong>
                                </div>
                            </div>

                            {!leaderboard
                                .isLeaderboardAvailable ? (
                                <div className="competition-empty">
                                    <h2>
                                        The leaderboard has not been published yet
                                    </h2>

                                    <p>
                                        The leaderboard will be available after
                                        the deadline and after the system
                                        finishes grading all tests.
                                    </p>
                                </div>
                            ) : (
                                <div className="student-leaderboard-list">
                                    {leaderboard.rankings?.map(
                                        item => (
                                            <article
                                                key={
                                                    item
                                                        .competitionResultId ||
                                                    item
                                                        .studentRoleId
                                                }
                                                className="student-leaderboard-row teacher-leaderboard-row"
                                            >
                                                <div className="student-rank-position">
                                                    {renderRank(
                                                        item.rank
                                                    )}
                                                </div>

                                                <img
                                                    src={
                                                        item.avatarUrl ||
                                                        "/default-avatar.png"
                                                    }
                                                    alt=""
                                                />

                                                <div className="student-rank-name">
                                                    <strong>
                                                        {
                                                            item
                                                                .studentName
                                                        }
                                                    </strong>

                                                    <span>
                                                        {getStatusText(
                                                            item
                                                                .attemptStatus
                                                        )}
                                                    </span>
                                                </div>

                                                <div>
                                                    <label>
                                                        Score
                                                    </label>

                                                    <strong>
                                                        {
                                                            item
                                                                .finalScore
                                                        }
                                                    </strong>
                                                </div>

                                                <div>
                                                    <label>
                                                        Correct
                                                    </label>

                                                    <strong>
                                                        {
                                                            item
                                                                .correctAnswers
                                                        }
                                                        /
                                                        {
                                                            item
                                                                .totalQuestions
                                                        }
                                                    </strong>
                                                </div>

                                                <div>
                                                    <label>
                                                        Time
                                                    </label>

                                                    <strong>
                                                        {formatSeconds(
                                                            item
                                                                .completionSeconds
                                                        )}
                                                    </strong>
                                                </div>

                                                <button
                                                    type="button"
                                                    className="teacher-leaderboard-view-btn"
                                                    onClick={() =>
                                                        handleViewAttempt(
                                                            item
                                                                .competitionTestAttemptId
                                                        )
                                                    }
                                                >
                                                    <Eye size={17} />
                                                    View
                                                </button>
                                            </article>
                                        )
                                    )}
                                </div>
                            )}
                        </>
                    )}
                </div>
            </main>

            {selectedAttempt && (
                <div className="lesson-modal-overlay">
                    <div className="lesson-modal teacher-attempt-result-modal">
                        <div className="teacher-attempt-result-header">
                            <div>
                                <span>
                                    Attempt Details
                                </span>

                                <h2>
                                    {
                                        selectedAttempt
                                            .studentName
                                    }
                                </h2>

                                <p>
                                    Rank:{" "}
                                    {selectedAttempt.rank
                                        ? `#${selectedAttempt.rank}`
                                        : "Not ranked"}
                                </p>
                            </div>

                            <button
                                type="button"
                                className="competition-secondary-btn"
                                onClick={() =>
                                    setSelectedAttempt(null)
                                }
                            >
                                Close
                            </button>
                        </div>

                        <div className="teacher-attempt-result-content">
                            <section className="competition-result-hero">
                                <div>
                                    <span>
                                        Competition Result
                                    </span>

                                    <h1>
                                        {
                                            selectedAttempt
                                                .competitionTitle ||
                                            leaderboard
                                                ?.competitionTitle ||
                                            "Competition Test"
                                        }
                                    </h1>

                                    <p>
                                        Student:{" "}
                                        <strong>
                                            {
                                                selectedAttempt
                                                    .studentName
                                            }
                                        </strong>
                                    </p>

                                    <p>
                                        Submitted:{" "}
                                        {selectedAttempt
                                            .result
                                            .submittedAt
                                            ? formatVietnamDateTime(
                                                selectedAttempt
                                                    .result
                                                    .submittedAt
                                            )
                                            : "Not submitted"}
                                    </p>
                                </div>

                                <div className="competition-result-score">
                                    <strong>
                                        {
                                            selectedAttempt
                                                .result
                                                .score
                                        }
                                    </strong>

                                    <span>
                                        /{" "}
                                        {
                                            selectedAttempt
                                                .result
                                                .totalPoints
                                        }
                                    </span>
                                </div>
                            </section>

                            <section className="competition-result-summary">
                                <div>
                                    <label>
                                        Correct Answers
                                    </label>

                                    <strong>
                                        {
                                            selectedAttempt
                                                .result
                                                .correctAnswers
                                        }
                                        {" / "}
                                        {
                                            selectedAttempt
                                                .result
                                                .totalQuestions
                                        }
                                    </strong>
                                </div>

                                <div>
                                    <label>
                                        Status
                                    </label>

                                    <strong>
                                        {statusMap[
                                            selectedAttempt
                                                .result
                                                .status
                                        ] ||
                                            selectedAttempt
                                                .result
                                                .status}
                                    </strong>
                                </div>

                                <div>
                                    <label>
                                        Rank
                                    </label>

                                    <strong>
                                        {selectedAttempt.rank
                                            ? `#${selectedAttempt.rank}`
                                            : "Not published"}
                                    </strong>
                                </div>
                            </section>

                            {[
                                [
                                    "Listening",
                                    groupedSelectedAnswers
                                        .listening
                                ],
                                [
                                    "Reading",
                                    groupedSelectedAnswers
                                        .reading
                                ]
                            ].map(
                                ([skillTitle, answers]) =>
                                    answers.length > 0 && (
                                        <section
                                            key={skillTitle}
                                            className="competition-result-section"
                                        >
                                            <h2>
                                                {skillTitle}
                                            </h2>

                                            <div className="competition-objective-grid">
                                                {answers.map(
                                                    (
                                                        answer,
                                                        index
                                                    ) => (
                                                        <div
                                                            key={
                                                                answer
                                                                    .competitionTestAnswerId ||
                                                                index
                                                            }
                                                            className={
                                                                answer
                                                                    .isCorrect
                                                                    ? "competition-objective-item correct"
                                                                    : "competition-objective-item wrong"
                                                            }
                                                        >
                                                            <strong>
                                                                Question{" "}
                                                                {index +
                                                                    1}
                                                            </strong>

                                                            <span>
                                                                Student's answer:{" "}
                                                                {answer
                                                                    .selectedAnswerLabel ||
                                                                    "-"}
                                                            </span>

                                                            <span>
                                                                Correct answer:{" "}
                                                                {answer
                                                                    .correctAnswerLabel ||
                                                                    "-"}
                                                            </span>

                                                            <span>
                                                                Points:{" "}
                                                                {answer
                                                                    .pointEarned ??
                                                                    0}
                                                            </span>
                                                        </div>
                                                    )
                                                )}
                                            </div>
                                        </section>
                                    )
                            )}

                            {groupedSelectedAnswers
                                .writing.length > 0 && (
                                    <section className="competition-result-section">
                                        <h2>
                                            Writing
                                        </h2>

                                        {groupedSelectedAnswers
                                            .writing.map(
                                                (
                                                    answer,
                                                    index
                                                ) => (
                                                    <article
                                                        key={
                                                            answer
                                                                .competitionTestAnswerId ||
                                                            index
                                                        }
                                                        className="competition-result-answer"
                                                    >
                                                        <h3>
                                                            {answer
                                                                .writingTaskTitle ||
                                                                answer
                                                                    .questionContent ||
                                                                `Writing ${index + 1}`}
                                                        </h3>

                                                        <p>
                                                            <strong>
                                                                Points:
                                                            </strong>{" "}
                                                            {answer
                                                                .pointEarned ??
                                                                0}
                                                        </p>

                                                        <div className="competition-result-text">
                                                            {answer
                                                                .textAnswer ||
                                                                "The student did not submit a written response"}
                                                        </div>

                                                        {(answer
                                                            .writingFeedback ||
                                                            answer
                                                                .teacherFeedback) && (
                                                                <div className="competition-result-feedback">
                                                                    <strong>
                                                                        Feedback:
                                                                    </strong>

                                                                    <div>
                                                                        {answer
                                                                            .writingFeedback ||
                                                                            answer
                                                                                .teacherFeedback}
                                                                    </div>
                                                                </div>
                                                            )}
                                                    </article>
                                                )
                                            )}
                                    </section>
                                )}

                            {groupedSelectedAnswers
                                .speaking.length > 0 && (
                                    <section className="competition-result-section">
                                        <h2>
                                            Speaking
                                        </h2>

                                        {groupedSelectedAnswers
                                            .speaking.map(
                                                (
                                                    answer,
                                                    index
                                                ) => (
                                                    <article
                                                        key={
                                                            answer
                                                                .competitionTestAnswerId ||
                                                            index
                                                        }
                                                        className="competition-result-answer"
                                                    >
                                                        <h3>
                                                            {answer
                                                                .speakingTaskTitle ||
                                                                answer
                                                                    .questionContent ||
                                                                `Speaking ${index + 1}`}
                                                        </h3>

                                                        <p>
                                                            <strong>
                                                                Points:
                                                            </strong>{" "}
                                                            {answer
                                                                .pointEarned ??
                                                                0}
                                                        </p>

                                                        {answer
                                                            .audioAnswerUrl && (
                                                                <audio
                                                                    controls
                                                                    src={
                                                                        answer
                                                                            .audioAnswerUrl
                                                                    }
                                                                />
                                                            )}

                                                        {answer
                                                            .speakingTranscript && (
                                                                <div className="competition-result-text">
                                                                    <strong>
                                                                        Transcript:
                                                                    </strong>

                                                                    <div>
                                                                        {
                                                                            answer
                                                                                .speakingTranscript
                                                                        }
                                                                    </div>
                                                                </div>
                                                            )}

                                                        {(answer
                                                            .aiSpeakingFeedback ||
                                                            answer
                                                                .teacherFeedback) && (
                                                                <div className="competition-result-feedback">
                                                                    <strong>
                                                                        Feedback:
                                                                    </strong>

                                                                    <div>
                                                                        {answer
                                                                            .aiSpeakingFeedback ||
                                                                            answer
                                                                                .teacherFeedback}
                                                                    </div>
                                                                </div>
                                                            )}
                                                    </article>
                                                )
                                            )}
                                    </section>
                                )}

                            {selectedAttempt
                                .result
                                .answers?.length === 0 && (
                                    <div className="competition-empty">
                                        No answer data available.
                                    </div>
                                )}
                        </div>
                    </div>
                </div>
            )}
        </div>
    );
}

export default TeacherCompetitionLeaderboardPage;
