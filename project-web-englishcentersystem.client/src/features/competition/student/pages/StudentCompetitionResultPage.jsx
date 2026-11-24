import {
    useMemo
} from "react";

import {
    Trophy
} from "lucide-react";

import {
    useNavigate
} from "react-router-dom";

import { isToeicReadingClozeQuestion }
    from "../../../../shared/lib/readingFormat";
import ReadingPassageWithEvidence
    from "../../../../shared/components/ReadingPassageWithEvidence";
import { formatSpeakingScore }
    from "../../../../shared/lib/speakingScore";

import {
    formatVietnamDateTime
} from "../../../../utils/dateTimeUtils";

import {
    getSavedCompetitionResult
} from "../utils/competitionStorage";

import {
    getListeningQuestionDisplayContent
} from "../../../listening/utils/listeningDisplay";

import "../../../test/student/styles/studentTestResult.css";
import "../styles/studentCompetition.css";

const isRepeatedWritingDetail = (detail, promptText) => {
    const normalizedDetail = (detail || "")
        .replace(/\s+/g, " ")
        .trim();
    const normalizedPrompt = (promptText || "")
        .replace(/\s+/g, " ")
        .trim();

    return normalizedDetail.length > 0 &&
        normalizedPrompt.includes(normalizedDetail);
};

function StudentCompetitionResultPage() {
    const navigate =
        useNavigate();

    const result =
        getSavedCompetitionResult();

    const groupedAnswers =
        useMemo(() => {
            const answers =
                result?.answers || [];

            return {
                listening:
                    answers.filter(x =>
                        x.listeningQuestionId
                    ),

                reading:
                    answers.filter(x =>
                        x.readingQuestionId
                    ),

                writing:
                    answers.filter(x =>
                        x.writingTaskId
                    ),

                speaking:
                    answers.filter(x =>
                        x.speakingTaskId
                    )
            };
        }, [result]);

    const groupByTaskId = (
        answers,
        fieldName
    ) => {
        return answers.reduce(
            (groups, answer) => {
                const rawTaskId =
                    answer[fieldName];

                const taskId =
                    rawTaskId != null
                        ? String(rawTaskId)
                        : "unknown";

                const existed =
                    groups.find(group =>
                        group.taskId === taskId
                    );

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
            },
            []
        );
    };

    const listeningGroups =
        useMemo(
            () =>
                groupByTaskId(
                    groupedAnswers.listening,
                    "listeningTaskId"
                ),
            [groupedAnswers.listening]
        );

    const readingGroups =
        useMemo(
            () =>
                groupByTaskId(
                    groupedAnswers.reading,
                    "readingTaskId"
                ),
            [groupedAnswers.reading]
        );

    if (!result) {
        return (
            <div className="competition-result-empty">
                <h2>
                    Result not found
                </h2>

                <button
                    type="button"
                    className="competition-primary-btn"
                    onClick={() =>
                        navigate(
                            "/student/competitions"
                        )
                    }
                >
                    Back to Competitions
                </button>
            </div>
        );
    }

    const scorePercent =
        result.totalPoints > 0
            ? (
                result.score /
                result.totalPoints *
                100
            ).toFixed(1)
            : 0;

    const isToeicResult =
        result.toeicTotalScore != null ||
        result.toeicListeningScore != null ||
        result.toeicReadingScore != null;

    const primaryScoreLabel =
        isToeicResult
            ? "TOEIC"
            : "Completed";

    const primaryScoreValue =
        isToeicResult
            ? result.toeicTotalScore ?? result.score
            : `${scorePercent}%`;

    const getValidWritingTitle =
        answer => {
            const title =
                answer.writingTaskTitle ||
                answer.questionContent;

            if (!title) {
                return null;
            }

            return title.trim().toLowerCase() ===
                "writing task"
                ? null
                : title;
        };

    const getValidSpeakingTitle =
        answer => {
            const title =
                answer.speakingTaskTitle ||
                answer.questionContent;

            if (!title) {
                return null;
            }

            return title.trim().toLowerCase() ===
                "speaking task" ||
                /^part\s*\d+$/i.test(title.trim())
                ? null
                : title;
        };

    const renderAnswerOptions =
        answer => {
            const options =
                answer.options || [];

            if (options.length === 0) {
                return null;
            }

            return (
                <div className="test-result-option-list">
                    {options.map(
                        (option, index) => {
                            const isSelected =
                                option.isSelected === true;

                            const isCorrect =
                                option.isCorrect === true;

                            let className =
                                "test-result-option";

                            if (isCorrect) {
                                className += " correct";
                            }

                            if (
                                isSelected &&
                                !isCorrect
                            ) {
                                className +=
                                    " selected-wrong";
                            }

                            if (
                                isSelected &&
                                isCorrect
                            ) {
                                className +=
                                    " selected-correct";
                            }

                            return (
                                <div
                                    key={
                                        option.optionId ||
                                        `${option.label}-${index}`
                                    }
                                    className={className}
                                >
                                    <span className="test-result-option-label">
                                        {option.label ||
                                            String.fromCharCode(
                                                65 + index
                                            )}
                                    </span>

                                    <span className="test-result-option-content">
                                        {option.content}
                                    </span>

                                    <div className="test-result-option-status">
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
                        }
                    )}
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
                <div className="test-section-empty">
                    {emptyText}
                </div>
            );
        }

        return (
            <div className="test-objective-answer-list">
                {answers.map(
                    (answer, index) => (
                        <article
                            key={
                                answer.competitionTestAnswerId ||
                                index
                            }
                            className={`test-objective-answer-card ${answer.isCorrect
                                    ? "correct"
                                    : "wrong"
                                }`}
                        >
                            <div className="test-answer-head">
                                <div>
                                    <span className="test-answer-number">
                                        Question {startIndex + index + 1}
                                    </span>

                                    {!isToeicReadingClozeQuestion({
                                        isReadingQuestion: Boolean(readingPassageText),
                                        content: answer.questionContent,
                                        readingPassageText
                                    }) && (
                                        <p className="test-answer-question">
                                            {getListeningQuestionDisplayContent(
                                                answer.questionContent,
                                                startIndex + index + 1
                                            )}
                                        </p>
                                    )}
                                </div>

                                <span
                                    className={
                                        answer.isCorrect
                                            ? "badge-correct"
                                            : "badge-wrong"
                                    }
                                >
                                    {answer.isCorrect
                                        ? "Correct"
                                        : "Incorrect"}
                                </span>
                            </div>

                            {answer.listeningPromptImageUrl &&
                                answer.listeningPromptImageUrl !==
                                    answers[index - 1]?.listeningPromptImageUrl && (
                                    <img
                                        src={answer.listeningPromptImageUrl}
                                        alt={`Listening question ${startIndex + index + 1}`}
                                        className="test-listening-prompt-image"
                                    />
                                )}

                            {renderAnswerOptions(answer)}

                            <div className="test-objective-choice-row">
                                <div>
                                    <label>Your answer</label>

                                    <strong>
                                        {answer.selectedAnswerLabel ||
                                            "Not selected"}
                                    </strong>
                                </div>

                                <div>
                                    <label>Correct answer</label>

                                    <strong>
                                        {answer.correctAnswerLabel ||
                                            "-"}
                                    </strong>
                                </div>
                            </div>

                            {answer.explanation && (
                                <div className="test-answer-explanation">
                                    <strong>Explanation</strong>
                                    <p>{answer.explanation}</p>
                                </div>
                            )}
                        </article>
                    )
                )}
            </div>
        );
    };

    const renderListeningSection = () => (
        <section className="test-skill-section">
            <div className="test-skill-header">
                <div>
                    <h2>Listening</h2>
                    <p>Listening task and comprehension question results</p>
                </div>

                <span>
                    {groupedAnswers.listening.length} questions
                </span>
            </div>

            {listeningGroups.length === 0 ? (
                <div className="test-section-empty">
                    This competition test does not have a Listening section.
                </div>
            ) : (
                <div className="test-task-group-list">
                    {listeningGroups.map(
                        (group, groupIndex) => {
                            const info =
                                group.taskInfo;

                            const questionStartIndex =
                                listeningGroups
                                    .slice(0, groupIndex)
                                    .reduce(
                                        (total, currentGroup) =>
                                            total +
                                            currentGroup.answers.length,
                                        0
                                    );

                            return (
                                <div
                                    key={group.taskId}
                                    className="test-task-group"
                                >
                                    <div className="test-source-box">
                                        <h3>
                                            {info.listeningPartTitle ||
                                                info.listeningTaskTitle ||
                                                `Listening Part ${groupIndex + 1}`}
                                        </h3>

                                        {info.listeningLittlePart && (
                                            <h4>
                                                {info.listeningLittlePart}
                                            </h4>
                                        )}

                                        {(info.listeningDirection ||
                                            info.listeningInstruction) && (
                                                <div className="test-source-instruction">
                                                    <strong>Instructions</strong>

                                                    <p>
                                                        {info.listeningDirection ||
                                                            info.listeningInstruction}
                                                    </p>
                                                </div>
                                            )}

                                        {info.listeningAudioUrl && (
                                            <div className="test-source-audio">
                                                <strong>Test Audio</strong>

                                                <audio
                                                    controls
                                                    src={
                                                        info.listeningAudioUrl
                                                    }
                                                />
                                            </div>
                                        )}

                                        {info.listeningTranscript && (
                                            <details className="test-source-details">
                                                <summary>
                                                    View listening transcript
                                                </summary>

                                                <p>
                                                    {info.listeningTranscript}
                                                </p>
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
                        }
                    )}
                </div>
            )}
        </section>
    );

    const renderReadingSection = () => (
        <section className="test-skill-section">
            <div className="test-skill-header">
                <div>
                    <h2>Reading</h2>

                    <p>
                        Reading passages and comprehension question results
                    </p>
                </div>

                <span>
                    {groupedAnswers.reading.length} questions
                </span>
            </div>

            {readingGroups.length === 0 ? (
                <div className="test-section-empty">
                    This competition test does not have a Reading section.
                </div>
            ) : (
                <div className="test-task-group-list">
                    {readingGroups.map(
                        (group, groupIndex) => {
                            const info =
                                group.taskInfo;

                            const questionStartIndex =
                                readingGroups
                                    .slice(0, groupIndex)
                                    .reduce(
                                        (
                                            total,
                                            previousGroup
                                        ) =>
                                            total +
                                            previousGroup.answers.length,
                                        0
                                    );

                            return (
                                <div
                                    key={group.taskId}
                                    className="test-task-group"
                                >
                                    <div className="test-source-box">
                                        <h3>
                                            {info.readingPassageTitle ||
                                                info.readingTaskTitle ||
                                                `Reading Passage ${groupIndex + 1}`}
                                        </h3>

                                        {info.readingInstruction && (
                                            <div className="test-source-instruction">
                                                <strong>
                                                    Instructions
                                                </strong>

                                                <p>
                                                    {info.readingInstruction}
                                                </p>
                                            </div>
                                        )}

                                        {info.readingPassageText && (
                                            <article className="test-reading-passage">
                                                <div className="test-reading-passage-heading">
                                                    <strong>Reading Passage</strong>
                                                </div>

                                                <div className="test-reading-passage-content">
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
                        }
                    )}
                </div>
            )}
        </section>
    );

    const renderWritingAnswer =
        (answer, index) => (
            <div
                className="test-answer-card writing"
                key={
                    answer.competitionTestAnswerId ||
                    index
                }
            >
                <div className="test-answer-head">
                    <div>
                        <span className="test-answer-number">
                            Writing Task {index + 1}
                        </span>

                        {getValidWritingTitle(answer) && (
                            <p className="test-answer-question">
                                {getValidWritingTitle(answer)}
                            </p>
                        )}
                    </div>

                    <span className="badge-ai">
                        {answer.manualScore != null ||
                            answer.writingOverallBand != null
                            ? "Graded"
                            : "Awaiting grading"}
                    </span>
                </div>

                {answer.writingInstruction && (
                    <div className="test-task-instruction">
                        <strong>Instructions</strong>
                        <p>{answer.writingInstruction}</p>
                    </div>
                )}

                {answer.writingPromptText && (
                    <div className="test-task-prompt">
                        <strong>Prompt</strong>
                        <p>{answer.writingPromptText}</p>
                    </div>
                )}

                {answer.writingPromptImageUrl && (
                    <div className="test-task-prompt">
                        <img
                            src={answer.writingPromptImageUrl}
                            alt={getValidWritingTitle(answer)}
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
                    <div className="test-task-reference">
                        <strong>Reference Material</strong>
                        <p>{answer.writingReferenceText}</p>
                    </div>
                )}

                {answer.writingSourceText && (
                    <div className="test-task-reference">
                        <strong>Source Text</strong>
                        <p>{answer.writingSourceText}</p>
                    </div>
                )}

                {answer.writingRequirements &&
                    !isRepeatedWritingDetail(
                        answer.writingRequirements,
                        answer.writingPromptText
                    ) && (
                    <div className="test-task-requirements">
                        <strong>Requirements</strong>
                        <p>{answer.writingRequirements}</p>
                    </div>
                )}

                {answer.writingMinimumWords != null && (
                    <p className="test-minimum-words">
                        Minimum word count:{" "}
                        <strong>
                            {answer.writingMinimumWords}
                        </strong>
                    </p>
                )}

                <div className="test-score-grid">
                    <div>
                        <label>Overall Band</label>
                        <strong>
                            {answer.writingOverallBand ?? "-"}
                        </strong>
                    </div>

                    <div>
                        <label>Task Achievement</label>
                        <strong>
                            {answer.writingTaskAchievement ?? "-"}
                        </strong>
                    </div>

                    <div>
                        <label>Coherence</label>
                        <strong>
                            {answer.writingCoherenceAndCohesion ?? "-"}
                        </strong>
                    </div>

                    <div>
                        <label>Lexical</label>
                        <strong>
                            {answer.writingLexicalResource ?? "-"}
                        </strong>
                    </div>

                    <div>
                        <label>Grammar</label>
                        <strong>
                            {answer.writingGrammaticalRangeAndAccuracy ?? "-"}
                        </strong>
                    </div>
                </div>

                {answer.textAnswer && (
                    <div className="test-written-answer">
                        <strong>Your Answer</strong>
                        <p>{answer.textAnswer}</p>
                    </div>
                )}

                {(answer.writingFeedback ||
                    answer.teacherFeedback) && (
                        <div className="test-feedback">
                            <strong>Feedback:</strong>{" "}
                            {answer.writingFeedback ||
                                answer.teacherFeedback}
                        </div>
                    )}
            </div>
        );

    const renderSpeakingAnswer =
        (answer, index) => (
            <div
                className="test-answer-card speaking"
                key={
                    answer.competitionTestAnswerId ||
                    index
                }
            >
                <div className="test-answer-head">
                    <div>
                        <span className="test-answer-number">
                            Speaking Task {index + 1}
                        </span>

                        {getValidSpeakingTitle(answer) && (
                            <p className="test-answer-question">
                                {getValidSpeakingTitle(answer)}
                            </p>
                        )}
                    </div>

                    <span className="badge-ai">
                        {answer.overallSpeakingScore != null
                            ? "Graded"
                            : "Not submitted"}
                    </span>
                </div>

                {answer.speakingInstruction && (
                    <div className="test-task-instruction">
                        <strong>Instructions</strong>
                        <p>{answer.speakingInstruction}</p>
                    </div>
                )}

                {answer.speakingPromptText && (
                    <div className="test-task-prompt">
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
                    <div className="test-task-reference">
                        <strong>Reference Material</strong>
                        <p>{answer.speakingReferenceText}</p>
                    </div>
                )}

                {(answer.preparationTimeSeconds != null ||
                    answer.speakingTimeSeconds != null) && (
                        <div className="test-speaking-time">
                            {answer.preparationTimeSeconds != null && (
                                <span>
                                    Preparation:{" "}
                                    <strong>
                                        {answer.preparationTimeSeconds} seconds
                                    </strong>
                                </span>
                            )}

                            {answer.speakingTimeSeconds != null && (
                                <span>
                                    Speaking time:{" "}
                                    <strong>
                                        {answer.speakingTimeSeconds} seconds
                                    </strong>
                                </span>
                            )}
                        </div>
                    )}

                {answer.audioAnswerUrl && (
                    <div className="test-audio-box">
                        <audio
                            controls
                            src={answer.audioAnswerUrl}
                        />
                    </div>
                )}

                <div className="test-score-grid">
                    <div>
                        <label>Overall</label>
                        <strong>
                            {formatSpeakingScore(answer.overallSpeakingScore)}
                        </strong>
                    </div>

                    <div>
                        <label>Pronunciation</label>
                        <strong>
                            {formatSpeakingScore(answer.pronunciationScore)}
                        </strong>
                    </div>

                    <div>
                        <label>Fluency</label>
                        <strong>
                            {formatSpeakingScore(answer.fluencyScore)}
                        </strong>
                    </div>

                    <div>
                        <label>Grammar</label>
                        <strong>
                            {formatSpeakingScore(answer.grammarScore)}
                        </strong>
                    </div>

                    <div>
                        <label>Vocabulary</label>
                        <strong>
                            {formatSpeakingScore(answer.vocabularyScore)}
                        </strong>
                    </div>
                </div>

                {answer.speakingTranscript && (
                    <div className="test-written-answer">
                        <strong>Transcript</strong>
                        <p>{answer.speakingTranscript}</p>
                    </div>
                )}

                {(answer.aiSpeakingFeedback ||
                    answer.teacherFeedback) && (
                        <div className="test-feedback">
                            <strong>Feedback:</strong>{" "}
                            {answer.aiSpeakingFeedback ||
                                answer.teacherFeedback}
                        </div>
                    )}
            </div>
        );

    const renderSection = (
        title,
        subtitle,
        answers,
        renderer,
        emptyText
    ) => (
        <section className="test-skill-section">
            <div className="test-skill-header">
                <div>
                    <h2>{title}</h2>
                    <p>{subtitle}</p>
                </div>

                <span>{answers.length} questions</span>
            </div>

            {answers.length > 0 ? (
                <div className="test-answer-list">
                    {answers.map(renderer)}
                </div>
            ) : (
                <div className="test-section-empty">
                    {emptyText}
                </div>
            )}
        </section>
    );

    return (
        <div className="competition-result-page test-result-page">
            <button
                type="button"
                className="lesson-btn secondary test-back-btn"
                onClick={() =>
                    navigate(
                        "/student/competitions"
                    )
                }
            >
                Back to List
            </button>

            <section className="test-result-hero">
                <div>
                    <p className="test-result-label">
                        Competition Result
                    </p>

                    <h1>
                        {result.competitionTitle ||
                            result.competitionTestTitle}
                    </h1>

                    <p className="test-result-date">
                        Submitted:{" "}
                        {result.submittedAt
                            ? formatVietnamDateTime(
                                result.submittedAt
                            )
                            : "Not submitted"}
                    </p>
                </div>

                {!isToeicResult && (
                    <div className="test-result-score">
                        <label>{primaryScoreLabel}</label>
                        <strong>{primaryScoreValue}</strong>
                    </div>
                )}
            </section>

            <section className="test-summary-grid competition-result-summary">
                {isToeicResult && (
                    <>
                        <div>
                            <label>
                                TOEIC Listening Estimated Score (5–495)
                            </label>
                            <strong>{result.toeicListeningScore ?? "-"}</strong>
                        </div>

                        <div>
                            <label>
                                TOEIC Reading Estimated Score (5–495)
                            </label>
                            <strong>{result.toeicReadingScore ?? "-"}</strong>
                        </div>

                        {result.toeicSpeakingScore != null && (
                            <div>
                                <label>
                                    TOEIC Speaking Estimated Score (0–200)
                                </label>
                                <strong>{result.toeicSpeakingScore}</strong>
                            </div>
                        )}

                        {result.toeicWritingScore != null && (
                            <div>
                                <label>
                                    TOEIC Writing Estimated Score (0–200)
                                </label>
                                <strong>{result.toeicWritingScore}</strong>
                            </div>
                        )}

                    </>
                )}
                <div>
                    <label>Rank</label>

                    <strong>
                        {result.rank
                            ? `#${result.rank}`
                            : "Not announced"}
                    </strong>
                </div>
            </section>

            {renderListeningSection()}

            {renderReadingSection()}

            {renderSection(
                "Writing",
                "Written response and grading feedback",
                groupedAnswers.writing,
                renderWritingAnswer,
                "This competition test does not have a Writing section."
            )}

            {renderSection(
                "Speaking",
                "Spoken response, audio, and AI feedback",
                groupedAnswers.speaking,
                renderSpeakingAnswer,
                "This competition test does not have a Speaking section."
            )}

            <div className="competition-result-actions">
                <button
                    type="button"
                    className="competition-secondary-btn"
                    onClick={() =>
                        navigate(
                            "/student/competitions"
                        )
                    }
                >
                    Back to List
                </button>

                <button
                    type="button"
                    className="competition-primary-btn"
                    onClick={() =>
                        navigate(
                            `/student/competitions/${result.competitionTestAssignmentId}/leaderboard`
                        )
                    }
                >
                    <Trophy size={17} />
                    Leaderboard
                </button>
            </div>
        </div>
    );
}

export default StudentCompetitionResultPage;
