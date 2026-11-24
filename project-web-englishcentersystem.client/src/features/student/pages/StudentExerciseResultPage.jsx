import { useEffect, useMemo, useState } from "react";
import { useNavigate } from "react-router-dom";

import "../../lesson/styles/lesson.css";
import "../styles/exercise.css";
import "../styles/studentExerciseResult.css";
import { isToeicReadingClozeQuestion }
    from "../../../shared/lib/readingFormat";
import ReadingPassageWithEvidence
    from "../../../shared/components/ReadingPassageWithEvidence";
import { formatSpeakingScore }
    from "../../../shared/lib/speakingScore";
import { formatVietnamDateTime } from "../../../utils/dateTimeUtils";
import { getListeningQuestionDisplayContent }
    from "../../listening/utils/listeningDisplay";

function StudentExerciseResultPage() {
    const navigate = useNavigate();
    const [result, setResult] = useState(null);

    useEffect(() => {
        const saved = localStorage.getItem("student-exercise-result");

        if (!saved) {
            setResult(null);
            return;
        }

        try {
            setResult(JSON.parse(saved));
        }
        catch {
            localStorage.removeItem("student-exercise-result");
            setResult(null);
        }
    }, []);

    const groupedAnswers = useMemo(() => {
        const answers = Array.isArray(result?.answers)
            ? result.answers.filter(Boolean)
            : [];

        return {
            grammar: answers.filter(answer =>
                answer?.questionId &&
                !answer?.listeningQuestionId &&
                !answer?.readingQuestionId &&
                !answer?.writingTaskId &&
                !answer?.speakingTaskId
            ),

            listening: answers.filter(
                answer => answer?.listeningQuestionId
            ),

            reading: answers.filter(
                answer => answer?.readingQuestionId
            ),

            writing: answers.filter(
                answer => answer?.writingTaskId
            ),

            speaking: answers.filter(
                answer => answer?.speakingTaskId
            )
        };
    }, [result]);

    const groupByTaskId = (answers, fieldName) =>
        (Array.isArray(answers) ? answers : [])
            .filter(Boolean)
            .reduce((groups, answer) => {
            const taskId =
                answer[fieldName] != null
                    ? String(answer[fieldName])
                    : "unknown";

            const existed = groups.find(group => group.taskId === taskId);

            if (existed) {
                existed.answers.push(answer);
                return groups;
            }

            groups.push({
                taskId,
                taskInfo: answer,
                answers: [answer]
            });

            return groups;
        }, []);

    const listeningGroups = useMemo(
        () => groupByTaskId(groupedAnswers.listening, "listeningTaskId"),
        [groupedAnswers.listening]
    );

    const readingGroups = useMemo(
        () => groupByTaskId(groupedAnswers.reading, "readingTaskId"),
        [groupedAnswers.reading]
    );

    if (!result) {
        return (
            <div className="exercise-result-page">
                <div className="exercise-result-empty">
                    <h2>Exercise result not found</h2>

                    <button
                        className="lesson-btn secondary"
                        onClick={() => navigate("/student/exercises")}
                    >
                        Back to Exercise List
                    </button>
                </div>
            </div>
        );
    }

    const scorePercent =
        result.totalPoints > 0
            ? ((result.score / result.totalPoints) * 100).toFixed(1)
            : "0.0";

    const renderAnswerOptions = answer => {
        const options = answer.options || [];

        if (options.length === 0) {
            return null;
        }

        return (
            <div className="exercise-result-option-list">
                {options.map((option, index) => {
                    const isSelected = option.isSelected === true;
                    const isCorrect = option.isCorrect === true;

                    let className = "exercise-result-option";

                    if (isCorrect) className += " correct";
                    if (isSelected && !isCorrect) className += " selected-wrong";
                    if (isSelected && isCorrect) className += " selected-correct";

                    return (
                        <div
                            key={
                                option.optionId ||
                                option.questionOptionId ||
                                option.listeningOptionId ||
                                option.readingOptionId ||
                                `${option.label}-${index}`
                            }
                            className={className}
                        >
                            <span className="exercise-result-option-label">
                                {option.label || String.fromCharCode(65 + index)}
                            </span>

                            <span className="exercise-result-option-content">
                                {option.content}
                            </span>

                            <div className="exercise-result-option-status">
                                {isSelected && (
                                    <span className="option-selected-badge">
                                        Your answer
                                    </span>
                                )}

                                {isCorrect && (
                                    <span className="option-correct-badge">
                                        Correct answer
                                    </span>
                                )}
                            </div>
                        </div>
                    );
                })}
            </div>
        );
    };

    const renderObjectiveGrid = (
        answers,
        emptyText,
        startIndex = 0,
        readingPassageText = null
    ) => {
        if (answers.length === 0) {
            return (
                <div className="exercise-result-section-empty">
                    {emptyText}
                </div>
            );
        }

        return (
            <div className="exercise-objective-answer-list">
                {answers.map((answer, index) => (
                    <article
                        key={answer.exerciseAnswerId || index}
                        className={`exercise-objective-answer-card ${answer.isCorrect ? "correct" : "wrong"
                            }`}
                    >
                        <div className="exercise-result-answer-head">
                            <div>
                                <span className="exercise-result-answer-number">
                                    Question {startIndex + index + 1}
                                </span>

                                {!isToeicReadingClozeQuestion({
                                    isReadingQuestion: Boolean(readingPassageText),
                                    content: answer.questionContent,
                                    readingPassageText
                                }) && (
                                    <p className="exercise-result-answer-question">
                                        {getListeningQuestionDisplayContent(
                                            answer.questionContent,
                                            startIndex + index + 1
                                        ) || "Question"}
                                    </p>
                                )}
                            </div>

                            {answer.pointEarned > 0 && (
                                <span className="exercise-point-earned">
                                    +{answer.pointEarned} points
                                </span>
                            )}
                        </div>

                        {answer.listeningPromptImageUrl &&
                            answer.listeningPromptImageUrl !==
                                answers[index - 1]?.listeningPromptImageUrl && (
                                <img
                                    src={answer.listeningPromptImageUrl}
                                    alt={`Listening question ${startIndex + index + 1}`}
                                    className="exercise-result-listening-prompt-image"
                                />
                            )}

                        {renderAnswerOptions(answer)}

                        {answer.explanation && (
                            <div className="exercise-result-explanation">
                                <strong>Explanation</strong>
                                <p>{answer.explanation}</p>
                            </div>
                        )}
                    </article>
                ))}
            </div>
        );
    };

    const renderGrammarSection = () => (
        <section className="exercise-result-skill-section">
            <div className="exercise-result-skill-header">
                <div>
                    
                    <p>Grammar question results</p>
                </div>

                <span>{groupedAnswers.grammar.length} questions</span>
            </div>

            {renderObjectiveGrid(
                groupedAnswers.grammar,
                "This exercise does not have a Grammar section."
            )}
        </section>
    );

    const renderListeningSection = () => (
        <section className="exercise-result-skill-section">
            <div className="exercise-result-skill-header">
                <div>
                    <h2>Listening</h2>
                    <p>Listening task and comprehension question results</p>
                </div>

                <span>{groupedAnswers.listening.length} questions</span>
            </div>

            {listeningGroups.length === 0 ? (
                <div className="exercise-result-section-empty">
                    This exercise does not have a Listening section.
                </div>
            ) : (
                <div className="exercise-result-task-group-list">
                    {listeningGroups.map((group, groupIndex) => {
                        const info = group.taskInfo;
                        const questionStartIndex = listeningGroups
                            .slice(0, groupIndex)
                            .reduce(
                                (total, currentGroup) =>
                                    total + currentGroup.answers.length,
                                0
                            );

                        return (
                            <div
                                key={group.taskId}
                                className="exercise-result-task-group"
                            >
                                <div className="exercise-result-source-box">
                                    <h3>
                                        {info.listeningPartTitle ||
                                            info.listeningTaskTitle ||
                                            `Listening Part ${groupIndex + 1}`}
                                    </h3>

                                    {(info.listeningDirection ||
                                        info.listeningInstruction) && (
                                            <div className="exercise-result-source-instruction">
                                                <strong>Instructions</strong>
                                                <p>
                                                    {info.listeningDirection ||
                                                        info.listeningInstruction}
                                                </p>
                                            </div>
                                        )}

                                    {info.listeningAudioUrl && (
                                        <div className="exercise-result-source-audio">
                                            <strong>Exercise Audio</strong>
                                            <audio
                                                controls
                                                src={info.listeningAudioUrl}
                                            />
                                        </div>
                                    )}

                                    {info.listeningTranscript && (
                                        <details className="exercise-result-source-details">
                                            <summary>View transcript</summary>
                                            <p>{info.listeningTranscript}</p>
                                        </details>
                                    )}
                                </div>

                                {renderObjectiveGrid(
                                    group.answers,
                                    "No Listening questions.",
                                    questionStartIndex
                                )}
                            </div>
                        );
                    })}
                </div>
            )}
        </section>
    );

    const renderReadingSection = () => (
        <section className="exercise-result-skill-section">
            <div className="exercise-result-skill-header">
                <div>
                    <h2>Reading</h2>
                    <p>Reading passages and comprehension question results</p>
                </div>

                <span>{groupedAnswers.reading.length} questions</span>
            </div>

            {readingGroups.length === 0 ? (
                <div className="exercise-result-section-empty">
                    This exercise does not have a Reading section.
                </div>
            ) : (
                <div className="exercise-result-task-group-list">
                    {readingGroups.map((group, groupIndex) => {
                        const info = group.taskInfo;
                        const questionStartIndex = readingGroups
                            .slice(0, groupIndex)
                            .reduce(
                                (total, currentGroup) =>
                                    total + currentGroup.answers.length,
                                0
                            );

                        return (
                            <div
                                key={group.taskId}
                                className="exercise-result-task-group"
                            >
                                <div className="exercise-result-source-box">
                                    <h3>
                                        {info.readingPassageTitle ||
                                            info.readingTaskTitle ||
                                            `Reading Passage ${groupIndex + 1}`}
                                    </h3>

                                    {info.readingInstruction && (
                                        <div className="exercise-result-source-instruction">
                                            <strong>Instructions</strong>
                                            <p>{info.readingInstruction}</p>
                                        </div>
                                    )}

                                    {info.readingPassageText && (
                                        <article className="exercise-result-reading-passage">
                                            <strong>Reading Passage</strong>
                                            <div>
                                                <ReadingPassageWithEvidence
                                                    passage={info.readingPassageText}
                                                    explanations={group.answers.map(
                                                        answer => answer.explanation
                                                    )}
                                                />
                                            </div>
                                        </article>
                                    )}
                                </div>

                                {renderObjectiveGrid(
                                    group.answers,
                                    "No Reading questions.",
                                    questionStartIndex,
                                    info.readingPassageText
                                )}
                            </div>
                        );
                    })}
                </div>
            )}
        </section>
    );

    const renderWritingAnswer = (answer, index) => (
        <div
            className="exercise-result-answer-card writing"
            key={answer.exerciseAnswerId || index}
        >
            <div className="exercise-result-answer-head">
                <div>
                    <span className="exercise-result-answer-number">
                        Writing Task {index + 1}
                    </span>

                    {(answer.writingTaskTitle || answer.questionContent) && (
                        <p className="exercise-result-answer-question">
                            {answer.writingTaskTitle || answer.questionContent}
                        </p>
                    )}
                </div>

                <span className="exercise-badge-ai">
                    {answer.manualScore != null ||
                        answer.writingOverallBand != null
                        ? "Graded"
                        : "Awaiting grading"}
                </span>
            </div>

            {answer.writingInstruction && (
                <div className="exercise-result-task-box">
                    <strong>Instructions</strong>
                    <p>{answer.writingInstruction}</p>
                </div>
            )}

            {answer.writingPromptText && (
                <div className="exercise-result-task-box">
                    <strong>Prompt</strong>
                    <p>{answer.writingPromptText}</p>
                </div>
            )}

            {answer.writingPromptImageUrl && (
                <div className="exercise-result-task-box">
                    <img
                        src={answer.writingPromptImageUrl}
                        alt={answer.writingTaskTitle || "Writing prompt"}
                        style={{
                            display: "block",
                            maxWidth: "100%",
                            maxHeight: "420px",
                            objectFit: "contain",
                            borderRadius: "10px",
                            border: "1px solid #e5e7eb"
                        }}
                    />
                </div>
            )}

            {answer.writingReferenceText && (
                <div className="exercise-result-task-box">
                    <strong>Reference Material</strong>
                    <p>{answer.writingReferenceText}</p>
                </div>
            )}

            {answer.writingSourceText && (
                <div className="exercise-result-task-box">
                    <strong>Source Text</strong>
                    <p>{answer.writingSourceText}</p>
                </div>
            )}

            {answer.writingRequirements && (
                <div className="exercise-result-task-box">
                    <strong>Requirements</strong>
                    <p>{answer.writingRequirements}</p>
                </div>
            )}

            <div className="exercise-result-score-grid">
                <div>
                    <label>Overall Band</label>
                    <strong>{answer.writingOverallBand ?? "-"}</strong>
                </div>
                <div>
                    <label>Task Achievement</label>
                    <strong>{answer.writingTaskAchievement ?? "-"}</strong>
                </div>
                <div>
                    <label>Coherence</label>
                    <strong>
                        {answer.writingCoherenceAndCohesion ?? "-"}
                    </strong>
                </div>
                <div>
                    <label>Lexical</label>
                    <strong>{answer.writingLexicalResource ?? "-"}</strong>
                </div>
                <div>
                    <label>Grammar</label>
                    <strong>
                        {answer.writingGrammaticalRangeAndAccuracy ?? "-"}
                    </strong>
                </div>
            </div>

            {answer.textAnswer && (
                <div className="exercise-result-written-answer">
                    <strong>Your Answer</strong>
                    <p>{answer.textAnswer}</p>
                </div>
            )}

            {(answer.writingFeedback || answer.teacherFeedback) && (
                <div className="exercise-result-feedback">
                    <strong>Feedback:</strong>{" "}
                    {answer.writingFeedback || answer.teacherFeedback}
                </div>
            )}
        </div>
    );

    const getValidSpeakingTitle = answer => {
        const title = answer.speakingTaskTitle || answer.questionContent;

        if (
            !title ||
            title.trim().toLowerCase() === "speaking task" ||
            /^part\s*\d+$/i.test(title.trim())
        ) {
            return null;
        }

        return title;
    };

    const renderSpeakingAnswer = (answer, index) => (
        <div
            className="exercise-result-answer-card speaking"
            key={answer.exerciseAnswerId || index}
        >
            <div className="exercise-result-answer-head">
                <div>
                    <span className="exercise-result-answer-number">
                        Speaking Task {index + 1}
                    </span>

                    {getValidSpeakingTitle(answer) && (
                        <p className="exercise-result-answer-question">
                            {getValidSpeakingTitle(answer)}
                        </p>
                    )}
                </div>

                <span className="exercise-badge-ai">
                    {answer.overallSpeakingScore != null
                        ? "Graded"
                        : "Awaiting grading"}
                </span>
            </div>

            {answer.speakingInstruction && (
                <div className="exercise-result-task-box">
                    <strong>Instructions</strong>
                    <p>{answer.speakingInstruction}</p>
                </div>
            )}

            {answer.speakingPromptText && (
                <div className="exercise-result-task-box">
                    <strong>Speaking Prompt</strong>
                    <p>{answer.speakingPromptText}</p>
                </div>
            )}

            {answer.speakingPromptImageUrl && (
                <img
                    src={answer.speakingPromptImageUrl}
                    alt={answer.speakingTaskTitle || "Speaking prompt"}
                    style={{
                        display: "block",
                        maxWidth: "100%",
                        maxHeight: "420px",
                        objectFit: "contain",
                        borderRadius: "10px",
                        border: "1px solid #e5e7eb",
                        margin: "14px 0"
                    }}
                />
            )}

            {answer.speakingReferenceText && (
                <div className="exercise-result-task-box">
                    <strong>Reference Material</strong>
                    <p>{answer.speakingReferenceText}</p>
                </div>
            )}

            {answer.audioAnswerUrl && (
                <div className="exercise-result-audio-box">
                    <audio controls src={answer.audioAnswerUrl} />
                </div>
            )}

            <div className="exercise-result-score-grid">
                <div>
                    <label>Overall</label>
                    <strong>{formatSpeakingScore(answer.overallSpeakingScore)}</strong>
                </div>
                <div>
                    <label>Pronunciation</label>
                    <strong>{formatSpeakingScore(answer.pronunciationScore)}</strong>
                </div>
                <div>
                    <label>Fluency</label>
                    <strong>{formatSpeakingScore(answer.fluencyScore)}</strong>
                </div>
                <div>
                    <label>Grammar</label>
                    <strong>{formatSpeakingScore(answer.grammarScore)}</strong>
                </div>
                <div>
                    <label>Vocabulary</label>
                    <strong>{formatSpeakingScore(answer.vocabularyScore)}</strong>
                </div>
            </div>

            {(answer.speakingTranscript || answer.textAnswer) && (
                <div className="exercise-result-written-answer">
                    <strong>Transcript</strong>
                    <p>{answer.speakingTranscript || answer.textAnswer}</p>
                </div>
            )}

            {(answer.aiSpeakingFeedback || answer.teacherFeedback) && (
                <div className="exercise-result-feedback">
                    <strong>Feedback:</strong>{" "}
                    {answer.aiSpeakingFeedback || answer.teacherFeedback}
                </div>
            )}
        </div>
    );

    const renderSubjectiveSection = (
        title,
        subtitle,
        answers,
        renderer,
        emptyText
    ) => (
        <section className="exercise-result-skill-section">
            <div className="exercise-result-skill-header">
                <div>
                    <h2>{title}</h2>
                    <p>{subtitle}</p>
                </div>

                <span>{answers.length} questions</span>
            </div>

            {answers.length > 0 ? (
                <div className="exercise-result-answer-list">
                    {answers.map(renderer)}
                </div>
            ) : (
                <div className="exercise-result-section-empty">
                    {emptyText}
                </div>
            )}
        </section>
    );

    return (
        <div className="exercise-result-page">
            <button
                className="lesson-btn secondary exercise-result-back-btn"
                onClick={() => navigate(-1)}
            >
                Back to History
            </button>

            <section className="exercise-result-hero">
                <div>
                    <p className="exercise-result-label">
                        Exercise Result
                    </p>

                    

                    <p className="exercise-result-date">
                        Submitted:{" "}
                        {result.submittedAt
                            ? formatVietnamDateTime(result.submittedAt)
                            : "Not submitted"}
                    </p>
                </div>

                <div className="exercise-result-score">
                    <strong>{scorePercent}%</strong>
                    <span>
                        {result.score ?? 0}/{result.totalPoints ?? 0} points
                    </span>
                </div>
            </section>

            {renderGrammarSection()}
            {renderListeningSection()}
            {renderReadingSection()}

            {renderSubjectiveSection(
                "Writing",
                "Written response and grading feedback",
                groupedAnswers.writing,
                renderWritingAnswer,
                "This exercise does not have a Writing section."
            )}

            {renderSubjectiveSection(
                "Speaking",
                "Spoken response, audio, and AI feedback",
                groupedAnswers.speaking,
                renderSpeakingAnswer,
                "This exercise does not have a Speaking section."
            )}
        </div>
    );
}

export default StudentExerciseResultPage;
