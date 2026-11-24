import {
    useEffect,
    useState
} from "react";

import {
    Medal,
    Trophy
} from "lucide-react";

import {
    useNavigate,
    useParams
} from "react-router-dom";

import {
    getStudentCompetitionLeaderboardApi
} from "../api/studentCompetitionApi";

import "../styles/studentCompetition.css";

function StudentCompetitionLeaderboardPage() {
    const {
        competitionTestAssignmentId
    } = useParams();

    const navigate =
        useNavigate();

    const [leaderboard,
        setLeaderboard] =
        useState(null);

    const [loading, setLoading] =
        useState(true);

    useEffect(() => {
        const load = async () => {
            try {
                const data =
                    await getStudentCompetitionLeaderboardApi(
                        competitionTestAssignmentId
                    );

                setLeaderboard(data);
            }
            catch (error) {
                alert(
                    error.response?.data?.message ||
                    "Unable to load the leaderboard."
                );

                navigate(
                    "/student/competitions"
                );
            }
            finally {
                setLoading(false);
            }
        };

        load();
    }, [
        competitionTestAssignmentId,
        navigate
    ]);

    const formatSeconds = seconds => {
        if (seconds == null) {
            return "-";
        }

        const hours =
            Math.floor(
                seconds / 3600
            );

        const minutes =
            Math.floor(
                (
                    seconds % 3600
                ) / 60
            );

        const remaining =
            seconds % 60;

        return [
            hours > 0
                ? `${hours} hours`
                : null,

            minutes > 0
                ? `${minutes} minutes`
                : null,

            `${remaining} seconds`
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

    if (loading) {
        return (
            <div className="competition-empty">
                Loading leaderboard...
            </div>
        );
    }

    return (
        <div className="student-leaderboard-page">
            <button
                type="button"
                className="competition-secondary-btn"
                onClick={() =>
                    navigate(
                        "/student/competitions"
                    )
                }
            >
                Back
            </button>

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

            {!leaderboard
                .isLeaderboardAvailable ? (
                <div className="competition-empty">
                    <h2>
                            The leaderboard has not been published yet.
                    </h2>

                    <p>
                            The leaderboard will be available after the deadline and once all submissions have been graded.
                    </p>
                </div>
            ) : (
                <>
                    <div className="student-my-rank">
                        <span>
                                Your Rank
                        </span>

                        <strong>
                            {leaderboard.myRank
                                ? `#${leaderboard.myRank}`
                                    : "Not Ranked"}
                        </strong>

                        <span>
                            Score:{" "}
                            {leaderboard.myScore ??
                                "-"}
                        </span>
                    </div>

                    <div className="student-leaderboard-list">
                        {leaderboard.rankings.map(
                            item => (
                                <article
                                    key={
                                        item
                                            .studentRoleId
                                    }
                                    className={
                                        item
                                            .isCurrentStudent
                                            ? "student-leaderboard-row current"
                                            : "student-leaderboard-row"
                                    }
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

                                        {item
                                            .isCurrentStudent && (
                                                <span>
                                                    Your result
                                                </span>
                                            )}
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
                                            Corrected
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
                                </article>
                            )
                        )}
                    </div>
                </>
            )}
        </div>
    );
}

export default StudentCompetitionLeaderboardPage;