import {
    useEffect,
    useState
} from "react";

import {
    getMyExerciseHistoryApi,
    getExerciseAttemptResultApi
} from "../api/studentApi";
import {
    formatVietnamDateTime
} from "../../../utils/dateTimeUtils";

function StudentExerciseHistoryPage() {
    const [history, setHistory] =
        useState([]);
    const [selectedResult, setSelectedResult] =
        useState(null);
    const loadHistory = async () => {
        const data = await getMyExerciseHistoryApi();
        setHistory(data);
    };

    useEffect(() => {
        loadHistory();
    }, []);

    return (
        <div style={{ padding: "40px" }}>
            <h1>Lịch sử làm bài</h1>
            {selectedResult && (
                <div
                    style={{
                        background: "white",
                        border: "1px solid #e5e7eb",
                        borderRadius: "18px",
                        padding: "24px",
                        marginTop: "24px",
                        marginBottom: "24px"
                    }}
                >
                    <h2>{selectedResult.exerciseTitle}</h2>

                    <p>
                        <strong>Trạng thái:</strong> {selectedResult.status}
                    </p>

                    <p>
                        <strong>Điểm:</strong>{" "}
                        {selectedResult.score}/{selectedResult.totalPoints}
                    </p>

                    {selectedResult.answers
                        ?.filter(answer => answer.writingTaskId)
                        .map(answer => (
                            <div
                                key={answer.exerciseAnswerId}
                                style={{
                                    marginTop: "22px",
                                    padding: "20px",
                                    background: "#f8fafc",
                                    border: "1px solid #e5e7eb",
                                    borderRadius: "14px"
                                }}
                            >
                                <h3>{answer.writingTaskTitle}</h3>

                                <p>
                                    <strong>AI Score:</strong>{" "}
                                    {answer.writingOverallBand ??
                                        answer.manualScore ??
                                        0}/10
                                </p>

                                <p>
                                    <strong>Điểm quy đổi:</strong>{" "}
                                    {answer.pointEarned}
                                </p>

                                <p>
                                    <strong>Task Achievement:</strong>{" "}
                                    {answer.writingTaskAchievement ?? "-"} / 2.5
                                </p>

                                <p>
                                    <strong>Coherence & Cohesion:</strong>{" "}
                                    {answer.writingCoherenceAndCohesion ?? "-"} / 2.5
                                </p>

                                <p>
                                    <strong>Lexical Resource:</strong>{" "}
                                    {answer.writingLexicalResource ?? "-"} / 2.5
                                </p>

                                <p>
                                    <strong>Grammar:</strong>{" "}
                                    {answer.writingGrammaticalRangeAndAccuracy ?? "-"} / 2.5
                                </p>

                                <h4>AI Feedback</h4>

                                <div
                                    style={{
                                        whiteSpace: "pre-line",
                                        lineHeight: "1.7",
                                        color: "#374151",
                                        background: "white",
                                        padding: "16px",
                                        borderRadius: "12px",
                                        border: "1px solid #e5e7eb"
                                    }}
                                >
                                    {answer.writingFeedback ||
                                        answer.teacherFeedback ||
                                        "Chưa có feedback"}
                                </div>

                                <h4 style={{ marginTop: "18px" }}>
                                    Bài làm của bạn
                                </h4>

                                <div
                                    style={{
                                        whiteSpace: "pre-wrap",
                                        lineHeight: "1.7",
                                        background: "white",
                                        padding: "16px",
                                        borderRadius: "12px",
                                        border: "1px solid #e5e7eb"
                                    }}
                                >
                                    {answer.textAnswer}
                                </div>
                            </div>
                        ))}

                    <button
                        onClick={() => setSelectedResult(null)}
                        style={{
                            marginTop: "20px",
                            padding: "10px 16px",
                            borderRadius: "10px",
                            border: "none",
                            background: "#ef4444",
                            color: "white",
                            cursor: "pointer"
                        }}
                    >
                        Đóng
                    </button>
                </div>
            )}
            <div
                style={{
                    display: "flex",
                    flexDirection: "column",
                    gap: "18px",
                    marginTop: "24px"
                }}
            >
                {history.map(item => (
                    <div
                        key={item.exerciseAttemptId}
                        style={{
                            background: "white",
                            border: "1px solid #e5e7eb",
                            borderRadius: "18px",
                            padding: "22px"
                        }}
                    >
                        <h2>{item.exerciseTitle}</h2>

                        <p>
                            Trạng thái: {item.status}
                        </p>

                        <p>
                            Số câu đúng: {item.correctAnswers}/
                            {item.totalQuestions}
                        </p>

                        <p>
                            Điểm: {item.score}/{item.totalPoints}
                        </p>
                        <button
                            onClick={async () => {
                                const data = await getExerciseAttemptResultApi(
                                    item.exerciseAttemptId
                                );

                                setSelectedResult(data);
                            }}
                        >
                            Xem bài + đánh giá
                        </button>
                        <p>
                            Bắt đầu:{" "}
                            {formatVietnamDateTime(
                                item.startedAt
                            )}
                            <br />
                            Nộp bài:{" "}
                            {item.submittedAt
                                ? formatVietnamDateTime(
                                    item.submittedAt
                                )
                                : "Chưa nộp"}
                        </p>
                    </div>
                ))}
            </div>
        </div>
    );
}

export default StudentExerciseHistoryPage;
