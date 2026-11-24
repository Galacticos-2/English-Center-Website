import "../styles/questionPalette.css";

function QuestionPalette({
    questions = [],
    currentIndex = 0,
    selectedAnswers = {},
    markedQuestions = {},
    visitedQuestions = {},
    loading = false,
    onSelectQuestion,
    onMarkQuestion,
    onSubmit
}) {
    const getQuestionKey = (question) => {
        if (question.isListeningQuestion) {
            return `listening-${question.listeningQuestionId}`;
        }

       if (question.isSpeakingQuestion) {
    return `speaking-${question.speakingTaskId}`;
}

        if (question.readingQuestionId) {
            return `reading-${question.readingQuestionId}`;
        }

        if (question.writingTaskId) {
            return `writing-${question.writingTaskId}`;
        }

        return `question-${question.questionId}`;
    };
    const getQuestionStatus = (question) => {
        const questionKey = getQuestionKey(question);

        const answered =
            selectedAnswers[questionKey] !== undefined &&
            selectedAnswers[questionKey] !== null &&
            selectedAnswers[questionKey] !== "";

        const marked =
            markedQuestions[questionKey];

        const visited =
            visitedQuestions[questionKey];

        if (answered && marked) {
            return "answered-marked";
        }

        if (marked) {
            return "marked";
        }

        if (answered) {
            return "answered";
        }

        if (visited) {
            return "not-answered";
        }

        return "not-visited";
    };

    return (
        <aside className="question-palette">
            <h3>Question Palette</h3>

            <div className="question-palette-grid">
                {questions.map((question, index) => {
                    const status =
                        getQuestionStatus(question);

                    return (
                        <button
                            key={getQuestionKey(question)}
                            type="button"
                            className={
                                `question-palette-item ${status} ${currentIndex === index
                                    ? "current"
                                    : ""
                                }`
                            }
                            onClick={() =>
                                onSelectQuestion(index)
                            }
                        >
                            {index + 1}

                            {status === "answered-marked" && (
                                <span className="answered-marked-dot">
                                    ✓
                                </span>
                            )}
                        </button>
                    );
                })}
            </div>

            <button
                type="button"
                className="question-mark-btn"
                onClick={onMarkQuestion}
            >
                Mark for Review
            </button>

            <div className="question-palette-legend">
                <h4>Legend</h4>

                <div className="legend-row">
                    <span className="legend-box not-visited">
                        1
                    </span>
                    <span>Not Visited</span>
                </div>

                <div className="legend-row">
                    <span className="legend-box not-answered">
                        2
                    </span>
                    <span>Not Answered</span>
                </div>

                <div className="legend-row">
                    <span className="legend-box answered">
                        3
                    </span>
                    <span>Answered</span>
                </div>

                <div className="legend-row">
                    <span className="legend-box marked">
                        4
                    </span>
                    <span>Marked For Review</span>
                </div>

                <div className="legend-row">
                    <span className="legend-box answered-marked">
                        5
                    </span>
                    <span>Answered & Marked</span>
                </div>
            </div>

            {onSubmit && (
                <button
                    type="button"
                    className="question-submit-btn"
                    disabled={loading}
                    onClick={onSubmit}
                >
                    {loading ? "Submitting..." : "Submit"}
                </button>
            )}
        </aside>
    );
}

export default QuestionPalette;
