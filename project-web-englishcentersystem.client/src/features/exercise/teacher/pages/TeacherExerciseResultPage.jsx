import {
    useEffect,
    useState
} from "react";

import TeacherSidebar
    from "../../../lesson/teacher/TeacherSidebar";
import { formatVietnamDateTime } from "../../../../utils/dateTimeUtils";
import {
    getExerciseAssignmentsApi
} from "../api/exerciseAssignmentApi";
import {
    getExerciseResultsApi
    
} from "../api/exerciseResultApi";


import "../../../lesson/styles/lesson.css";
import "../styles/questionBank.css";

function TeacherExerciseResultPage() {
    const [assignments, setAssignments] =
        useState([]);

    const [selectedAssignmentId, setSelectedAssignmentId] =
        useState("");
    
    const [analytics, setAnalytics] =
        useState(null);

    const loadAssignments = async () => {
        const data = await getExerciseAssignmentsApi();
        setAssignments(data);
    };
    
    
    const loadResults = async () => {
        const data = await getExerciseResultsApi(
            selectedAssignmentId
        );

        setAnalytics(data);
    };

    useEffect(() => {
        loadAssignments();
    }, []);

    useEffect(() => {
        loadResults();
    }, [
        selectedAssignmentId
    ]);

    return (
        <div className="lesson-layout">
            <TeacherSidebar />

            <div className="lesson-main">
                <div className="lesson-header">
                    <div className="lesson-title">
                        <h1>Kết quả bài tập</h1>
                        <p>
                            Xem kết quả và thống kê bài tập của học viên
                        </p>
                    </div>
                </div>

                <div className="question-bank-filters">
                    <select
                        value={selectedAssignmentId}
                        onChange={e =>
                            setSelectedAssignmentId(e.target.value)
                        }
                    >
                        <option value="">
                            Tất cả bài đã giao
                        </option>

                        {assignments.map(assignment => (
                            <option
                                key={assignment.exerciseAssignmentId}
                                value={assignment.exerciseAssignmentId}
                            >
                                {assignment.exerciseTitle}
                                {" - "}
                                {assignment.classRoomName}
                            </option>
                        ))}
                    </select>
                </div>

                {analytics && (
                    <>
                        <div className="question-category-list">
                            <div className="question-category-card">
                                <div>
                                    <h3>Tổng lượt làm</h3>
                                    <p>{analytics.totalAttempts}</p>
                                </div>
                            </div>

                            <div className="question-category-card">
                                <div>
                                    <h3>Đã nộp</h3>
                                    <p>{analytics.submittedAttempts}</p>
                                </div>
                            </div>

                            <div className="question-category-card">
                                <div>
                                    <h3>Điểm trung bình</h3>
                                    <p>{analytics.averageScore}</p>
                                </div>
                            </div>
                        </div>

                        <div className="question-list">
                            {analytics.results.map(result => (
                                <div
                                    key={result.exerciseAttemptId}
                                    className="question-card"
                                >
                                    <div className="question-card-header">
                                        <div>
                                            <span className="question-badge">
                                                {result.studentName}
                                            </span>

                                            <span className="question-type-badge">
                                                {result.classRoomName}
                                            </span>
                                        </div>
                                    </div>

                                    <h3>{result.exerciseTitle}</h3>

                                    <p className="question-explanation">
                                        Trạng thái: {result.status}
                                        <br />
                                        Điểm: {result.score}/
                                        {result.totalPoints}
                                        <br />
                                        Đúng: {result.correctAnswers}/
                                        {result.totalQuestions}
                                        <br />
                                        Bắt đầu:{" "}
                                        {formatVietnamDateTime(result.startedAt)}
                                        <br />
                                        Nộp bài:{" "}
                                        {result.submittedAt
                                            ? formatVietnamDateTime(result.submittedAt)
                                            : "Chưa nộp"}
                                    </p>
                                    
                                </div>
                            ))}
                        </div>
                    </>
                )}
                
            </div>
        </div>
    );
}

export default TeacherExerciseResultPage;