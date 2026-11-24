import { useEffect, useState } from "react";
import TeacherSidebar from "../../../lesson/teacher/TeacherSidebar";

import {
    getTeacherTestsApi,
    getTeacherTestAttemptsApi,
    getTeacherTestAttemptDetailApi,
    exportTeacherTestResultsApi,
    getTeacherTestAnalyticsApi
} from "../api/teacherTestApi";
import {
    formatVietnamDateTime
} from "../../../../utils/dateTimeUtils";

import "../../../lesson/styles/lesson.css";
import "../../../exercise/teacher/styles/questionBank.css";

function TeacherTestResultPage() {
    const [tests, setTests] = useState([]);
    const [selectedTestId, setSelectedTestId] = useState("");
    const [attempts, setAttempts] = useState([]);
    const [analytics, setAnalytics] = useState(null);
    const [selectedDetail, setSelectedDetail] = useState(null);
    const [loading, setLoading] = useState(false);

    const loadTests = async () => {
        const data = await getTeacherTestsApi();

        setTests(data);

        if (data.length > 0) {
            setSelectedTestId(String(data[0].testId));
        }
    };
    const handleExportExcel = async () => {
    if (!selectedTestId) {
        alert("Vui lòng chọn đề thi");
        return;
    }

    const blob = await exportTeacherTestResultsApi(selectedTestId);

    const url = window.URL.createObjectURL(blob);

    const link = document.createElement("a");
    link.href = url;
    link.download = "test-results.xlsx";

    document.body.appendChild(link);
    link.click();

    link.remove();
    window.URL.revokeObjectURL(url);
};
    const loadResultData = async () => {
        if (!selectedTestId) {
            setAttempts([]);
            setAnalytics(null);
            return;
        }

        setLoading(true);

        try {
            const [attemptData, analyticsData] =
                await Promise.all([
                    getTeacherTestAttemptsApi(selectedTestId),
                    getTeacherTestAnalyticsApi(selectedTestId)
                ]);

            setAttempts(attemptData);
            setAnalytics(analyticsData);
        }
        finally {
            setLoading(false);
        }
    };

    const handleViewDetail = async (testAttemptId) => {
        const data = await getTeacherTestAttemptDetailApi(
            testAttemptId
        );

        setSelectedDetail(data);
    };

    useEffect(() => {
        loadTests();
    }, []);

    useEffect(() => {
        loadResultData();
    }, [selectedTestId]);

    const formatDate = (value) => {
        if (!value) {
            return "Chưa nộp";
        }

        return formatVietnamDateTime(value);
    };

    return (
        <div className="lesson-layout">
            <TeacherSidebar />

            <div className="lesson-main">
                <div className="lesson-header">
                    <div className="lesson-title">
                        <h1>Kết quả đề thi</h1>
                        <p>
                            Xem điểm, trạng thái AI grading và thống kê bài thi của học viên
                        </p>
                    </div>
                    <button
    className="lesson-btn secondary"
    onClick={handleExportExcel}
    disabled={!selectedTestId}
>
    Export Excel
</button>
                </div>

                <div className="question-bank-filters">
                    <select
                        value={selectedTestId}
                        onChange={e => {
                            setSelectedTestId(e.target.value);
                            setSelectedDetail(null);
                        }}
                    >
                        <option value="">Chọn đề thi</option>

                        {tests.map(test => (
                            <option
                                key={test.testId}
                                value={test.testId}
                            >
                                {test.title}
                            </option>
                        ))}
                    </select>
                </div>

                {loading && (
                    <p className="question-explanation">
                        Đang tải dữ liệu...
                    </p>
                )}

                {analytics && (
                    <div className="question-category-list">
                        <div className="question-category-card">
                            <div>
                                <h3>Tổng học viên</h3>
                                <p>{analytics.totalStudents}</p>
                            </div>
                        </div>

                        <div className="question-category-card">
                            <div>
                                <h3>Đã nộp</h3>
                                <p>{analytics.submittedStudents}</p>
                            </div>
                        </div>

                        <div className="question-category-card">
                            <div>
                                <h3>Chưa nộp</h3>
                                <p>{analytics.pendingStudents}</p>
                            </div>
                        </div>

                        <div className="question-category-card">
                            <div>
                                <h3>Điểm TB</h3>
                                <p>{analytics.averageScore}</p>
                            </div>
                        </div>

                        <div className="question-category-card">
                            <div>
                                <h3>Cao nhất</h3>
                                <p>{analytics.highestScore}</p>
                            </div>
                        </div>

                        <div className="question-category-card">
                            <div>
                                <h3>Tỉ lệ hoàn thành</h3>
                                <p>{analytics.completionRate}%</p>
                            </div>
                        </div>
                    </div>
                )}

                {selectedDetail && (
                    <div className="question-card">
                        <div className="question-card-header">
                            <div>
                                <span className="question-badge">
                                    {selectedDetail.attempt.studentName}
                                </span>

                                <span className="question-type-badge">
                                    {selectedDetail.attempt.status}
                                </span>
                            </div>

                            <button
                                className="lesson-btn secondary"
                                onClick={() => setSelectedDetail(null)}
                            >
                                Đóng chi tiết
                            </button>
                        </div>

                        <h2>{selectedDetail.detail.testTitle}</h2>

                        <p className="question-explanation">
                            Điểm: {selectedDetail.detail.score}/
                            {selectedDetail.detail.totalPoints}
                            <br />
                            Đúng: {selectedDetail.detail.correctAnswers}/
                            {selectedDetail.detail.totalQuestions}
                            <br />
                            Bắt đầu: {formatDate(selectedDetail.detail.startedAt)}
                            <br />
                            Nộp bài: {formatDate(selectedDetail.detail.submittedAt)}
                        </p>

                        <div className="question-form-options">
                            {selectedDetail.detail.answers?.map(answer => (
                                <div
                                    key={answer.testAnswerId}
                                    className="question-card"
                                >
                                    <h3>
                                        {answer.questionContent ||
                                            answer.writingTaskTitle ||
                                            answer.speakingTaskTitle ||
                                            "Câu trả lời"}
                                    </h3>

                                    <p>
                                        <strong>Điểm:</strong>{" "}
                                        {answer.pointEarned}
                                    </p>

                                    <p>
                                        <strong>Đúng/Sai:</strong>{" "}
                                        {answer.isCorrect ? "Đúng" : "Sai / Chưa chấm đúng sai"}
                                    </p>

                                    {answer.writingTaskId && (
                                        <div>
                                            <p>
                                                <strong>Writing Band:</strong>{" "}
                                                {answer.writingOverallBand ??
                                                    answer.manualScore ??
                                                    0}/10
                                            </p>

                                            <p>
                                                Task Achievement:{" "}
                                                {answer.writingTaskAchievement ?? "-"} / 2.5
                                            </p>

                                            <p>
                                                Coherence & Cohesion:{" "}
                                                {answer.writingCoherenceAndCohesion ?? "-"} / 2.5
                                            </p>

                                            <p>
                                                Lexical Resource:{" "}
                                                {answer.writingLexicalResource ?? "-"} / 2.5
                                            </p>

                                            <p>
                                                Grammar:{" "}
                                                {answer.writingGrammaticalRangeAndAccuracy ?? "-"} / 2.5
                                            </p>

                                            <p>
                                                <strong>Feedback:</strong>
                                            </p>

                                            <div
                                                style={{
                                                    whiteSpace: "pre-line",
                                                    background: "#f8fafc",
                                                    padding: "14px",
                                                    borderRadius: "12px"
                                                }}
                                            >
                                                {answer.writingFeedback ||
                                                    answer.teacherFeedback ||
                                                    "Chưa có feedback"}
                                            </div>

                                            <p>
                                                <strong>Bài viết:</strong>
                                            </p>

                                            <div
                                                style={{
                                                    whiteSpace: "pre-wrap",
                                                    background: "white",
                                                    padding: "14px",
                                                    border: "1px solid #e5e7eb",
                                                    borderRadius: "12px"
                                                }}
                                            >
                                                {answer.textAnswer}
                                            </div>
                                        </div>
                                    )}

                                    {answer.speakingTaskId && (
                                        <div>
                                            <p>
                                                <strong>Speaking Score:</strong>{" "}
                                                {answer.overallSpeakingScore ??
                                                    answer.manualScore ??
                                                    0}
                                            </p>

                                            <p>
                                                Pronunciation: {answer.pronunciationScore ?? "-"}
                                            </p>

                                            <p>
                                                Accuracy: {answer.accuracyScore ?? "-"}
                                            </p>

                                            <p>
                                                Fluency: {answer.fluencyScore ?? "-"}
                                            </p>

                                            <p>
                                                Completeness: {answer.completenessScore ?? "-"}
                                            </p>

                                            <p>
                                                <strong>Transcript:</strong>
                                            </p>

                                            <div
                                                style={{
                                                    whiteSpace: "pre-wrap",
                                                    background: "#f8fafc",
                                                    padding: "14px",
                                                    borderRadius: "12px"
                                                }}
                                            >
                                                {answer.speakingTranscript ||
                                                    "Chưa có transcript"}
                                            </div>

                                            {answer.audioAnswerUrl && (
                                                <audio
                                                    controls
                                                    src={answer.audioAnswerUrl}
                                                    style={{
                                                        width: "100%",
                                                        marginTop: "12px"
                                                    }}
                                                />
                                            )}

                                            <p>
                                                <strong>AI Feedback:</strong>
                                            </p>

                                            <div
                                                style={{
                                                    whiteSpace: "pre-line",
                                                    background: "#f8fafc",
                                                    padding: "14px",
                                                    borderRadius: "12px"
                                                }}
                                            >
                                                {answer.aiSpeakingFeedback ||
                                                    answer.teacherFeedback ||
                                                    "Chưa có feedback"}
                                            </div>
                                        </div>
                                    )}

                                    {!answer.writingTaskId &&
                                        !answer.speakingTaskId && (
                                            <p className="question-explanation">
                                                Selected option:{" "}
                                                {answer.selectedListeningOptionId ||
                                                    answer.selectedReadingOptionId ||
                                                    answer.selectedQuestionOptionId ||
                                                    "Không chọn"}
                                            </p>
                                        )}
                                </div>
                            ))}
                        </div>
                    </div>
                )}

                <div className="question-list">
                    {attempts.length === 0 && (
                        <p className="question-explanation">
                            Chưa có học viên làm bài thi này.
                        </p>
                    )}

                    {attempts.map(attempt => (
                        <div
                            key={attempt.testAttemptId}
                            className="question-card"
                        >
                            <div className="question-card-header">
                                <div>
                                    <span className="question-badge">
                                        {attempt.studentName}
                                    </span>

                                    <span className="question-type-badge">
                                        {attempt.classRoomName}
                                    </span>
                                </div>

                                <button
                                    className="lesson-btn secondary"
                                    onClick={() =>
                                        handleViewDetail(
                                            attempt.testAttemptId
                                        )
                                    }
                                >
                                    Xem chi tiết
                                </button>
                            </div>

                            <h3>
                                Điểm: {attempt.score}
                            </h3>

                            <p className="question-explanation">
                                Trạng thái: {attempt.status}
                                <br />
                                Đúng: {attempt.correctAnswers}/
                                {attempt.totalQuestions}
                                <br />
                                Bắt đầu: {formatDate(attempt.startedAt)}
                                <br />
                                Nộp bài: {formatDate(attempt.submittedAt)}
                            </p>
                        </div>
                    ))}
                </div>
            </div>
        </div>
    );
}

export default TeacherTestResultPage;
