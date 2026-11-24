import { useEffect, useState } from "react";
import { ArrowLeft, LoaderCircle, Trophy } from "lucide-react";
import { useNavigate } from "react-router-dom";
import { getStudentClassLeaderboardApi } from "../api/studentApi";
import "../styles/studentClassLeaderboard.css";

function StudentClassLeaderboardPage() {
    const navigate = useNavigate();
    const [leaderboard, setLeaderboard] = useState(null);
    const [error, setError] = useState("");

    useEffect(() => {
        const loadLeaderboard = async () => {
            try {
                const response = await getStudentClassLeaderboardApi();
                setLeaderboard(response?.data || response);
            } catch (loadError) {
                console.error("Unable to load class leaderboard:", loadError);
                setError("Unable to load the class leaderboard. Please try again.");
            }
        };

        loadLeaderboard();
    }, []);

    if (error) {
        return (
            <main className="class-leaderboard-page">
                <div className="class-leaderboard-error">
                    <h1>Class leaderboard unavailable</h1>
                    <p>{error}</p>
                    <button type="button" onClick={() => navigate("/student")}>Back to dashboard</button>
                </div>
            </main>
        );
    }

    if (!leaderboard) {
        return (
            <main className="class-leaderboard-page class-leaderboard-loading">
                <LoaderCircle size={32} /> Loading class leaderboard...
            </main>
        );
    }

    return (
        <main className="class-leaderboard-page">
            <div className="class-leaderboard-container">
                <button type="button" className="class-leaderboard-back" onClick={() => navigate("/student")}>
                    <ArrowLeft size={18} /> Back to dashboard
                </button>

                <section className="class-leaderboard-hero">
                    <div className="class-leaderboard-hero-icon"><Trophy size={30} /></div>
                    <div>
                        <span>Class EXP leaderboard</span>
                        <h1>{leaderboard.className || "Your class"}</h1>
                        <p>Keep learning to move up the ranks and unlock your next title.</p>
                    </div>
                </section>

                <section className="class-leaderboard-table-card">
                    <div className="class-leaderboard-table-heading">
                        <h2>Class rankings</h2>
                        <span>{leaderboard.students?.length || 0} students</span>
                    </div>
                    <div className="class-leaderboard-table-scroll">
                        <table>
                            <thead>
                                <tr>
                                    <th>Rank</th>
                                    <th>Student</th>
                                    <th>EXP</th>
                                    <th>Level</th>
                                    <th>Title</th>
                                </tr>
                            </thead>
                            <tbody>
                                {(leaderboard.students || []).map((student) => (
                                    <tr key={student.studentRoleId} className={student.isCurrentStudent ? "is-current-student" : ""}>
                                        <td>
                                            <span className={`class-leaderboard-rank-badge rank-${Math.min(student.rank, 4)}`}>
                                                {student.rank}
                                            </span>
                                        </td>
                                        <td>
                                            <div className="class-leaderboard-student">
                                                <div className="class-leaderboard-avatar">
                                                    {student.avatarUrl ? <img src={student.avatarUrl} alt="" /> : <span>{student.fullName?.charAt(0)?.toUpperCase()}</span>}
                                                </div>
                                                <strong>{student.fullName}{student.isCurrentStudent ? " (You)" : ""}</strong>
                                            </div>
                                        </td>
                                        <td><strong className="class-leaderboard-exp">{student.totalExp.toLocaleString()} EXP</strong></td>
                                        <td>Level {student.level}</td>
                                        <td><span className="class-leaderboard-title">{student.title}</span></td>
                                    </tr>
                                ))}
                            </tbody>
                        </table>
                    </div>
                </section>
            </div>
        </main>
    );
}

export default StudentClassLeaderboardPage;
