import { useEffect, useMemo, useState } from "react";

import QuestionPalette
    from "../../../question-palette/components/QuestionPalette";
import { useNavigate } from "react-router-dom";
import SpeakingRecorder
    from "../../../speaking/student/components/SpeakingRecorder";
import SpeakingTaskContent, {
    getVstepDuration,
    getToeicSpeakingGroup,
    isVstepSpeakingTask
}
    from "../../../speaking/student/components/SpeakingTaskContent";
import {
    formatReadingPassageText,
    getVstepReadingDisplayContent,
    isToeicReadingClozeQuestion,
    isToeicReadingPartLabel,
    isToeicReadingRangeTitle,
    splitToeicReadingPassageTitle
} from "../../../../shared/lib/readingFormat";
import {
    getWritingTaskDisplayTitle,
    getToeicPictureWritingPromptTitle,
    getToeicWrittenRequestDisplay,
    isToeicPictureWritingQuestion,
    splitWritingEmailPromptText,
    shouldShowWritingInstruction
} from "../../../writing/utils/writingDisplay";
import {
    formatListeningPartTitle,
    getListeningQuestionDisplayContent,
    getToeicListeningPartContext
}
    from "../../../listening/utils/listeningDisplay";

import {
    getMyStudentTestsApi,
    startTestAttemptApi,
    submitTestAttemptApi,
    submitTestSpeakingAnswerApi

} from "../api/studentTestApi";

import { LoadingModal } from "../../../../shared/loading";
import "../../../lesson/styles/lesson.css";
import "../../../student/styles/exercise.css";
const ACTIVE_TEST_STORAGE_KEY = "student-active-test-state";
const DEFAULT_TEST_DURATION_MINUTES = 120;
const DEFAULT_TEST_DURATION_SECONDS =
    DEFAULT_TEST_DURATION_MINUTES * 60;
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
const getCurrentTimestamp = () => {
    return Date.now();
};
const getAttemptDurationSeconds = attempt => {
    const durationMinutes =
        Number(attempt?.durationMinutes);

    return durationMinutes > 0
        ? durationMinutes * 60
        : DEFAULT_TEST_DURATION_SECONDS;
};
const getListeningAudioKey = question =>
    question.listeningTaskId
        ? `listening-task-${question.listeningTaskId}`
        : question.audioUrl;
function StudentTestPage() {
    const SKILLS = [
        {
            id: 2,
            name: "Listening"
        },
        {
            id: 4,
            name: "Reading"
        },
        {
            id: 5,
            name: "Writing"
        },
        {
            id: 3,
            name: "Speaking"
        }
    ];

    const [tests, setTests] = useState([]);

    const [searchText, setSearchText] = useState("");
    const [activeAttempt, setActiveAttempt] = useState(null);
    const [statusFilter, setStatusFilter] = useState("all");
    const navigate = useNavigate();
    const [selectedAnswers, setSelectedAnswers] = useState({});
    const [markedQuestions, setMarkedQuestions] = useState({});
    const [visitedQuestions, setVisitedQuestions] = useState({});
    const [currentQuestionIndex, setCurrentQuestionIndex] = useState(0);
    const testProgressSummary = useMemo(() => {
        const totalTests = tests.length;

        const completedTests = tests.filter(test =>
            test.attemptHistory?.some(attempt =>
                attempt.submittedAt
            )
        ).length;

        const percent = totalTests > 0
            ? Math.round((completedTests / totalTests) * 100)
            : 0;

        const submittedAttempts = tests
            .flatMap(test => test.attemptHistory || [])
            .filter(attempt => attempt.submittedAt)
            .sort((a, b) =>
                new Date(b.submittedAt) - new Date(a.submittedAt)
            );

        const lastAttemptAt = submittedAttempts.length > 0
            ? submittedAttempts[0].submittedAt
            : null;

        return {
            totalTests,
            completedTests,
            percent,
            lastAttemptAt
        };
    }, [tests]);
    const [activeSkill, setActiveSkill] = useState(2);
    const [startingTestId, setStartingTestId] = useState(null);
    const [startProgress, setStartProgress] = useState(0);
    const [speakingAudios, setSpeakingAudios] = useState({});
    const [speakingResults, setSpeakingResults] = useState({});
    const [playedListeningAudios, setPlayedListeningAudios] =
        useState({});
    const [submittingTest, setSubmittingTest] = useState(false);
    const [submitProgress, setSubmitProgress] = useState(0);
    const [loading, setLoading] = useState(false);

    const [remainingSeconds, setRemainingSeconds] =
        useState(DEFAULT_TEST_DURATION_SECONDS);

    const [testEndAt, setTestEndAt] = useState(null);
    const testFilterCounts = useMemo(() => {
        return {
            all: tests.length,

            notStarted: tests.filter(test =>
                !test.attemptCount ||
                test.attemptCount <= 0
            ).length,

            completed: tests.filter(test =>
                test.attemptCount > 0
            ).length
        };
    }, [tests]);
    const filteredTests = useMemo(() => {
        const normalizedSearch =
            searchText.trim().toLowerCase();

        return tests
            .filter(test => {
                const matchesSearch =
                    (test.title || "")
                        .toLowerCase()
                        .includes(normalizedSearch);

                const attemptCount =
                    Number(test.attemptCount || 0);

                const matchesStatus =
                    statusFilter === "all" ||
                    (
                        statusFilter === "not-started" &&
                        attemptCount <= 0
                    ) ||
                    (
                        statusFilter === "completed" &&
                        attemptCount > 0
                    );

                return matchesSearch && matchesStatus;
            })
            .sort((a, b) =>
                (a.title || "").localeCompare(
                    b.title || "",
                    "vi",
                    {
                        sensitivity: "base"
                    }
                )
            );
    }, [
        tests,
        searchText,
        statusFilter
    ]);
    const loadTests = async () => {
        try {
            const data = await getMyStudentTestsApi();

            const items = Array.isArray(data)
                ? data
                : [];

            setTests(items);
            return items;
        }
        catch (error) {
            console.log("Load student tests error:", error.response?.data || error);
            setTests([]);
            return null;
        }
    };
    const handleRefreshTests = async () => {
        try {
            setLoading(true);

            await loadTests();
        }
        finally {
            setLoading(false);
        }
    };
    useEffect(() => {
        let isMounted = true;

        const loadAndRestoreActiveAttempt = async () => {
            const availableTests = await loadTests();

            if (!isMounted || !availableTests) {
                return;
            }

            const savedState = localStorage.getItem(
                ACTIVE_TEST_STORAGE_KEY
            );

            if (!savedState) {
                return;
            }

            try {
                const parsed = JSON.parse(savedState);

                const isValidSavedAttempt =
                    parsed.activeAttempt?.testAttemptId &&
                    Array.isArray(parsed.activeAttempt?.questions) &&
                    availableTests.some(test =>
                        test.testAssignmentId ===
                        parsed.activeAttempt.testAssignmentId
                    );

                if (!isValidSavedAttempt) {
                    localStorage.removeItem(
                        ACTIVE_TEST_STORAGE_KEY
                    );

                    return;
                }

                setActiveAttempt(parsed.activeAttempt);

                setSelectedAnswers(
                    parsed.selectedAnswers || {}
                );

                setMarkedQuestions(
                    parsed.markedQuestions || {}
                );

                setVisitedQuestions(
                    parsed.visitedQuestions || {}
                );

                setSpeakingResults(
                    parsed.speakingResults || {}
                );

                setPlayedListeningAudios(
                    parsed.playedListeningAudios || {}
                );

                setActiveSkill(
                    parsed.activeSkill || 2
                );

                setCurrentQuestionIndex(
                    Number.isInteger(parsed.currentQuestionIndex)
                        ? parsed.currentQuestionIndex
                        : 0
                );

                const restoredEndAt =
                    Number(parsed.testEndAt);

                const currentTimestamp =
                    getCurrentTimestamp();

                if (
                    Number.isFinite(restoredEndAt) &&
                    restoredEndAt > 0
                ) {
                    setTestEndAt(restoredEndAt);

                    setRemainingSeconds(
                        Math.max(
                            0,
                            Math.ceil(
                                (
                                    restoredEndAt -
                                    currentTimestamp
                                ) / 1000
                            )
                        )
                    );
                }
                else {
                    const durationSeconds =
                        getAttemptDurationSeconds(
                            parsed.activeAttempt
                        );

                    const fallbackEndAt =
                        currentTimestamp +
                        durationSeconds * 1000;

                    setTestEndAt(fallbackEndAt);

                    setRemainingSeconds(
                        durationSeconds
                    );
                }
            }
            catch (error) {
                console.error(
                    "Restore active test error:",
                    error
                );

                localStorage.removeItem(
                    ACTIVE_TEST_STORAGE_KEY
                );
            }
        };

        loadAndRestoreActiveAttempt();

        return () => {
            isMounted = false;
        };
    }, []);
    useEffect(() => {
        if (!activeAttempt?.testAttemptId) {
            return;
        }

        const stateToSave = {
            activeAttempt,
            selectedAnswers,
            markedQuestions,
            visitedQuestions,
            speakingResults,
            playedListeningAudios,
            activeSkill,
            currentQuestionIndex,
            testEndAt
        };

        try {
            localStorage.setItem(
                ACTIVE_TEST_STORAGE_KEY,
                JSON.stringify(stateToSave)
            );
        }
        catch (error) {
            console.error(
                "Save active test error:",
                error
            );
        }
    }, [
        activeAttempt,
        selectedAnswers,
        markedQuestions,
        visitedQuestions,
        speakingResults,
        playedListeningAudios,
        activeSkill,
        currentQuestionIndex,
        testEndAt
    ]);


    const getQuestionSkill = (question) => {
        if (question.isListeningQuestion) {
            return 2;
        }

        if (question.isSpeakingQuestion) {
            return 3;
        }

        if (question.isReadingQuestion) {
            return 4;
        }

        if (question.isWritingQuestion) {
            return 5;
        }

        return 0;
    };

    const availableSkills = useMemo(() => {
        if (!activeAttempt) {
            return [];
        }

        return SKILLS.filter(skill =>
            activeAttempt.questions.some(question =>
                getQuestionSkill(question) === skill.id
            )
        );
    }, [activeAttempt]);

    const filteredQuestions = useMemo(() => {
        if (!activeAttempt) {
            return [];
        }

        return activeAttempt.questions.filter(question =>
            getQuestionSkill(question) === activeSkill
        );
    }, [activeAttempt, activeSkill]);

    const currentQuestion = filteredQuestions[currentQuestionIndex];
    
    const getQuestionKey = (question) => {
        if (question.isListeningQuestion) {
            return `listening-${question.listeningQuestionId}`;
        }

        if (question.isSpeakingQuestion) {
            return `speaking-${question.speakingTaskId}`;
        }

        if (question.isReadingQuestion) {
            return `reading-${question.readingQuestionId}`;
        }

        if (question.isWritingQuestion) {
            return `writing-${question.writingTaskId}`;
        }

        return `question-${question.questionId}`;
    };

    const getOptionId = (question, option) => {
        if (question.isListeningQuestion) {
            return option.listeningOptionId;
        }

        if (question.isReadingQuestion) {
            return option.readingOptionId;
        }

        return option.questionOptionId;
    };

    const formatTime = (seconds) => {
        const h = Math.floor(seconds / 3600)
            .toString()
            .padStart(2, "0");

        const m = Math.floor((seconds % 3600) / 60)
            .toString()
            .padStart(2, "0");

        const s = (seconds % 60)
            .toString()
            .padStart(2, "0");

        return `${h}:${m}:${s}`;
    };

    const handleStart = async (testAssignmentId) => {
        try {
            setLoading(true);
            setStartingTestId(testAssignmentId);
            setStartProgress(10);

            const progressTimer = setInterval(() => {
                setStartProgress(prev => {
                    if (prev >= 90) {
                        return prev;
                    }

                    return prev + 10;
                });
            }, 300);

            const data = await startTestAttemptApi(
                testAssignmentId
            );

            clearInterval(progressTimer);

            setStartProgress(100);

            setActiveAttempt(data);

            setSelectedAnswers({});
            setMarkedQuestions({});
            setVisitedQuestions({});
            setSpeakingAudios({});
            setSpeakingResults({});
            setPlayedListeningAudios({});
            setCurrentQuestionIndex(0);

            const currentTimestamp =
                getCurrentTimestamp();

            const parsedStartedAt =
                data.startedAt
                    ? new Date(data.startedAt).getTime()
                    : Number.NaN;

            const validStartedAt =
                Number.isFinite(parsedStartedAt)
                    ? parsedStartedAt
                    : currentTimestamp;

            const endAt =
                validStartedAt +
                getAttemptDurationSeconds(data) * 1000;

            setTestEndAt(endAt);

            setRemainingSeconds(
                Math.max(
                    0,
                    Math.ceil(
                        (endAt - currentTimestamp) / 1000
                    )
                )
            );

            const firstSkill =
                SKILLS.find(skill =>
                    data.questions.some(question =>
                        getQuestionSkill(question) === skill.id
                    )
                )?.id || 2;

            setActiveSkill(firstSkill);

            if (data.questions.length > 0) {
                setVisitedQuestions({
                    [getQuestionKey(data.questions[0])]: true
                });
            }
        }
        catch (error) {
            alert(
                error.response?.data?.message ||
                "Unable to start the test"
            );
        }
        finally {
            setTimeout(() => {
                setLoading(false);
                setStartingTestId(null);
                setStartProgress(0);
            }, 300);
        }
    };

    const handleChangeSkill = (skillId) => {
        setActiveSkill(skillId);
        setCurrentQuestionIndex(0);

        const firstQuestion = activeAttempt.questions.find(question =>
            getQuestionSkill(question) === skillId
        );

        if (firstQuestion) {
            setVisitedQuestions(prev => ({
                ...prev,
                [getQuestionKey(firstQuestion)]: true
            }));
        }
    };

    const handleSelectQuestion = (index) => {
        const question = filteredQuestions[index];

        if (!question) {
            return;
        }

        setCurrentQuestionIndex(index);

        setVisitedQuestions(prev => ({
            ...prev,
            [getQuestionKey(question)]: true
        }));
    };

    const handleMarkQuestion = () => {
        if (!currentQuestion) {
            return;
        }

        const key = getQuestionKey(currentQuestion);

        setMarkedQuestions(prev => ({
            ...prev,
            [key]: !prev[key]
        }));
    };

    const handleUploadSpeakingAnswer = async (question) => {
        const key = getQuestionKey(question);
        const audioBlob = speakingAudios[key];

        if (!audioBlob) {
            alert("Please record your answer before saving the audio.");
            return;
        }

        const formData = new FormData();

        formData.append(
            "testAttemptId",
            activeAttempt.testAttemptId
        );

        formData.append(
            "speakingTaskId",
            question.speakingTaskId
        );

        formData.append(
            "audioFile",
            audioBlob,
            `test-speaking-${question.speakingTaskId}.webm`
        );

        try {
            setLoading(true);

            const data = await submitTestSpeakingAnswerApi(
                formData
            );

            setSpeakingResults(prev => ({
                ...prev,
                [key]: data
            }));

            setSelectedAnswers(prev => ({
                ...prev,
                [key]: data?.audioAnswerUrl ?? "uploaded"
            }));

            alert("Speaking audio saved.");
        }
        catch (error) {
            alert(
                error.response?.data?.message ||
                "Unable to save the Speaking audio"
            );
        }
        finally {
            setLoading(false);
        }
    };

    const buildSubmitAnswers = () => {
        return activeAttempt.questions.map(question => {
            const key = getQuestionKey(question);
            const value = selectedAnswers[key];

            if (question.isListeningQuestion) {
                return {
                    questionId: null,
                    listeningQuestionId:
                        question.listeningQuestionId,
                    readingQuestionId: null,
                    writingTaskId: null,
                    speakingTaskId: null,
                    selectedQuestionOptionId: null,
                    selectedListeningOptionId: value || null,
                    selectedReadingOptionId: null,
                    textAnswer: null,
                    audioAnswerUrl: null
                };
            }

            if (question.isReadingQuestion) {
                return {
                    questionId: null,
                    listeningQuestionId: null,
                    readingQuestionId:
                        question.readingQuestionId,
                    writingTaskId: null,
                    speakingTaskId: null,
                    selectedQuestionOptionId: null,
                    selectedListeningOptionId: null,
                    selectedReadingOptionId: value || null,
                    textAnswer: null,
                    audioAnswerUrl: null
                };
            }

            if (question.isWritingQuestion) {
                return {
                    questionId: null,
                    listeningQuestionId: null,
                    readingQuestionId: null,
                    writingTaskId:
                        question.writingTaskId,
                    speakingTaskId: null,
                    selectedQuestionOptionId: null,
                    selectedListeningOptionId: null,
                    selectedReadingOptionId: null,
                    textAnswer: value || "",
                    audioAnswerUrl: null
                };
            }

            if (question.isSpeakingQuestion) {
                return {
                    questionId: null,
                    listeningQuestionId: null,
                    readingQuestionId: null,
                    writingTaskId: null,
                    speakingTaskId:
                        question.speakingTaskId,
                    selectedQuestionOptionId: null,
                    selectedListeningOptionId: null,
                    selectedReadingOptionId: null,
                    textAnswer: null,
                    audioAnswerUrl: value || ""
                };
            }

            return {
                questionId: question.questionId,
                listeningQuestionId: null,
                readingQuestionId: null,
                writingTaskId: null,
                speakingTaskId: null,
                selectedQuestionOptionId: value || null,
                selectedListeningOptionId: null,
                selectedReadingOptionId: null,
                textAnswer: null,
                audioAnswerUrl: null
            };
        });
    };

    const handleSubmit = async (autoSubmit = false) => {
        if (!activeAttempt) {
            return;
        }

        const notAnswered = activeAttempt.questions.some(question =>
            !selectedAnswers[getQuestionKey(question)]
        );

        if (notAnswered && !autoSubmit) {
            const ok = window.confirm(
                "You have not answered all questions. Submit anyway?"
            );

            if (!ok) {
                return;
            }
        }

        try {
            setLoading(true);
            setSubmittingTest(true);
            setSubmitProgress(10);

            const progressTimer = setInterval(() => {
                setSubmitProgress(prev => {
                    if (prev >= 90) {
                        return prev;
                    }

                    return prev + 10;
                });
            }, 300);

            const answers = buildSubmitAnswers();

            const data = await submitTestAttemptApi(
                activeAttempt.testAttemptId,
                answers
            );
            clearInterval(progressTimer);
            setSubmitProgress(100);
            localStorage.setItem(
                "student-test-result",
                JSON.stringify(data)
            );
            localStorage.removeItem(
                ACTIVE_TEST_STORAGE_KEY
            );

            setActiveAttempt(null);
            setSelectedAnswers({});
            setMarkedQuestions({});
            setVisitedQuestions({});
            setSpeakingAudios({});
            setSpeakingResults({});
            setPlayedListeningAudios({});
            setCurrentQuestionIndex(0);
            setTestEndAt(null);
            setRemainingSeconds(
                DEFAULT_TEST_DURATION_SECONDS
            );

            await loadTests();

            navigate("/student/tests");
        }
        catch (error) {
            alert(
                error.response?.data?.message ||
                "Unable to submit the test"
            );
        }
        finally {
            setTimeout(() => {
                setLoading(false);
                setSubmittingTest(false);
                setSubmitProgress(0);
            }, 300);
        }
    };
    useEffect(() => {
        if (!activeAttempt || !testEndAt) {
            return;
        }

        let autoSubmitted = false;

        const updateTimer = () => {
            const currentTimestamp =
                getCurrentTimestamp();

            const nextSeconds = Math.max(
                0,
                Math.ceil(
                    (
                        testEndAt -
                        currentTimestamp
                    ) / 1000
                )
            );
            setRemainingSeconds(nextSeconds);

            if (
                nextSeconds <= 0 &&
                !autoSubmitted
            ) {
                autoSubmitted = true;
                handleSubmit(true);
            }
        };

        updateTimer();

        const timer = setInterval(
            updateTimer,
            1000
        );

        return () => {
            clearInterval(timer);
        };
    }, [
        activeAttempt,
        testEndAt,
        selectedAnswers,
        speakingAudios
    ]);

    const timerUrgencyClass =
        remainingSeconds <= 300
            ? " danger"
            : remainingSeconds <= 900
                ? " warning"
                : "";

    if (activeAttempt) {
        return (
            <div className="stt-page">
                <LoadingModal
                    show={submittingTest}
                    percent={submitProgress}
                    title="Loading..."
                    message="Submitting..."
                />

                <div className="stt-content stt-active">
                    <main>
                        <div className="stt-topbar">
                            <h1>{activeAttempt.testTitle}</h1>
                        </div>

                        <div className="stt-skill-tabs">
                            {availableSkills.map(skill => (
                                <button
                                    key={skill.id}
                                    type="button"
                                    className={
                                        activeSkill === skill.id
                                            ? "stt-skill-tab active"
                                            : "stt-skill-tab"
                                    }
                                    onClick={() =>
                                        handleChangeSkill(skill.id)
                                    }
                                >
                                    {skill.name}
                                </button>
                            ))}

                            <div className={`stt-timer${timerUrgencyClass}`}>
                                {formatTime(remainingSeconds)}
                            </div>
                        </div>
                       
                        {!currentQuestion ? (
                            <div className="stt-question-card">
                                <p className="stt-no-question">
                                    There are no questions for this skill.
                                </p>
                            </div>
                        ) : (
                            <QuestionBlock
                                displayQuestionNumber={currentQuestionIndex + 1}
                                question={currentQuestion}
                                answerValue={
                                    selectedAnswers[
                                    getQuestionKey(currentQuestion)
                                    ]
                                }
                                onAnswerChange={(value) => {
                                    const key =
                                        getQuestionKey(currentQuestion);

                                    setSelectedAnswers(prev => ({
                                        ...prev,
                                        [key]: value
                                    }));
                                }}
                                getOptionId={getOptionId}
                                answerKey={getQuestionKey(currentQuestion)}
                                activeAttempt={activeAttempt}
                                speakingAudios={speakingAudios}
                                setSpeakingAudios={setSpeakingAudios}
                                speakingResults={speakingResults}
                                loading={loading}
                                onUploadSpeaking={
                                    handleUploadSpeakingAnswer
                                }
                                hasPlayedListeningAudio={Boolean(
                                    playedListeningAudios[
                                    getListeningAudioKey(currentQuestion)
                                    ]
                                )}
                                onStartListeningAudio={() =>
                                    setPlayedListeningAudios(prev => ({
                                        ...prev,
                                        [getListeningAudioKey(currentQuestion)]: true
                                    }))
                                }
                            />
                        )}

                        <div className="stt-question-nav">
                            <button
                                className="stt-nav-btn"
                                disabled={currentQuestionIndex === 0}
                                onClick={() =>
                                    handleSelectQuestion(
                                        currentQuestionIndex - 1
                                    )
                                }
                            >
                                ← Previous question
                            </button>

                            <button
                                className="stt-nav-btn"
                                disabled={
                                    currentQuestionIndex >=
                                    filteredQuestions.length - 1
                                }
                                onClick={() =>
                                    handleSelectQuestion(
                                        currentQuestionIndex + 1
                                    )
                                }
                            >
                                Next question →
                            </button>
                        </div>
                    </main>

                    <QuestionPalette
                        questions={filteredQuestions}
                        currentIndex={currentQuestionIndex}
                        selectedAnswers={selectedAnswers}
                        markedQuestions={markedQuestions}
                        visitedQuestions={visitedQuestions}
                        loading={loading}
                        onSelectQuestion={handleSelectQuestion}
                        onMarkQuestion={handleMarkQuestion}
                        onSubmit={() => handleSubmit(false)}
                    />
                </div>
            </div>
        );
    }

    return (
        <div className="stt-page">
            <LoadingModal
                show={loading && startingTestId !== null}
                percent={startProgress}
                title="Loading..."
                message="Opening test..."
            />

            <div className="stt-content">
                <div className="stt-hero">
                    <div className="stt-hero-left">
                        <div className="stt-hero-icon">📝</div>
                        <div>
                            <h1>My Tests</h1>
                            <p>Take tests, track progress, and review your attempt history</p>
                        </div>
                    </div>

                    <div className="stt-hero-stats">
                        <div className="stt-hero-stat">
                            <strong>{testProgressSummary.totalTests}</strong>
                            <span>TESTS</span>
                        </div>
                        <div className="stt-hero-stat">
                            <strong>{testProgressSummary.completedTests}</strong>
                            <span>ATTEMPTED</span>
                        </div>
                        <div className="stt-hero-stat">
                            <strong>{testProgressSummary.percent}%</strong>
                            <span>COMPLETED</span>
                        </div>
                    </div>
                </div>

               

                <div className="stt-filter-section">
                    <div className="stt-filter-top">
                        <div className="stt-search-wrap">
                            <span className="stt-search-icon">
                                🔍
                            </span>

                            <input
                                type="text"
                                placeholder="Search tests by name..."
                                value={searchText}
                                onChange={e =>
                                    setSearchText(e.target.value)
                                }
                            />
                        </div>

                        <button
                            type="button"
                            className="stt-refresh-btn"
                            disabled={loading}
                            onClick={handleRefreshTests}
                        >
                            <span
                                className={
                                    loading
                                        ? "stt-refresh-icon spinning"
                                        : "stt-refresh-icon"
                                }
                            >
                                ↻
                            </span>

                            Refresh
                        </button>
                    </div>

                    <div className="stt-filter-tabs">
                        <button
                            type="button"
                            className={
                                statusFilter === "all"
                                    ? "stt-filter-tab active"
                                    : "stt-filter-tab"
                            }
                            onClick={() =>
                                setStatusFilter("all")
                            }
                        >
                            All

                            <span>
                                {testFilterCounts.all}
                            </span>
                        </button>

                        <button
                            type="button"
                            className={
                                statusFilter === "not-started"
                                    ? "stt-filter-tab active"
                                    : "stt-filter-tab"
                            }
                            onClick={() =>
                                setStatusFilter("not-started")
                            }
                        >
                            Not started

                            <span>
                                {testFilterCounts.notStarted}
                            </span>
                        </button>

                        <button
                            type="button"
                            className={
                                statusFilter === "completed"
                                    ? "stt-filter-tab active"
                                    : "stt-filter-tab"
                            }
                            onClick={() =>
                                setStatusFilter("completed")
                            }
                        >
                            Attempted

                            <span>
                                {testFilterCounts.completed}
                            </span>
                        </button>
                    </div>
                </div>

                {filteredTests.length === 0 ? (
                    <div className="stt-empty">
                        No matching tests found.
                    </div>
                ) : (
                    <div className="stt-grid">
                        {filteredTests.map(test => (
                            <div
                                key={test.testAssignmentId}
                                className="stt-card"
                            >
                                <div className="stt-card-head">
                                    <h3
                                        className="stt-card-title"
                                        title={test.title}
                                    >
                                        {test.title}
                                    </h3>

                                    <span className={
                                        test.attemptCount > 0
                                            ? "stt-badge done"
                                            : "stt-badge todo"
                                    }>
                                        {test.attemptCount > 0
                                            ? "Attempted"
                                            : "Not started"}
                                    </span>
                                </div>

                                <div className="stt-card-body">
                                    <p className="stt-card-desc">
                                        {test.description || "No description"}
                                    </p>

                                    <div className="stt-card-meta">
                                        <span>
                                            ⏱ {test.durationMinutes} minutes
                                        </span>
                                        <span>📄 {test.totalQuestions} questions</span>
                                        <span>⭐ {test.totalPoints} points</span>
                                        <span>
                                            🔁 {test.attemptCount}/{test.maxAttempts} attempts
                                        </span>
                                    </div>

                                    <div className="stt-card-actions">
                                        <button
                                            className="stt-btn-start"
                                            disabled={!test.canStart || loading}
                                            onClick={() =>
                                                handleStart(test.testAssignmentId)
                                            }
                                        >
                                            Start Test
                                        </button>

                                        <button
                                            type="button"
                                            className="stt-btn-history"
                                            onClick={() =>
                                                navigate(`/student/tests/${test.testAssignmentId}/history`)
                                            }
                                            title="Attempt history"
                                        >
                                            ↺
                                        </button>
                                    </div>
                                </div>
                            </div>
                        ))}
                    </div>
                )}
            </div>
        </div>
    );
}

function QuestionBlock({
    question,
    displayQuestionNumber,
    answerValue,
    onAnswerChange,
    getOptionId,
    answerKey,
    activeAttempt,

    setSpeakingAudios,
    speakingResults,
    loading,
    onUploadSpeaking,
    hasPlayedListeningAudio,
    onStartListeningAudio
}) {
    const shouldShowChoices =
        question.isListeningQuestion ||
        question.isReadingQuestion ||
        question.questionType === 1 ||
        question.questionType === 2;
    const hasReadableReadingPassage =
        Boolean(question.readingPassageText) &&
        !question.readingPassageText
            .toLowerCase()
            .includes("provided as images");
    const isToeicClozeQuestion =
        isToeicReadingClozeQuestion(question);
    const toeicReadingTitle = splitToeicReadingPassageTitle(
        question.readingPassageTitle
    );
    const vstepReadingDisplay = getVstepReadingDisplayContent({
        passageTitle: question.readingPassageTitle,
        passageText: question.readingPassageText,
        instruction: question.readingInstruction
    });
    const displayReadingPassageTitle =
        vstepReadingDisplay?.passageTitle ||
        question.readingPassageTitle;
    const displayReadingPassageText =
        vstepReadingDisplay?.passageText ||
        question.readingPassageText;
    const writingEmailPrompt = splitWritingEmailPromptText(
        question.writingPromptText
    );
    const toeicWrittenRequest = getToeicWrittenRequestDisplay(
        question,
        displayQuestionNumber
    );
    const isEmailReplyTask =
        question.isWritingQuestion &&
        !toeicWrittenRequest &&
        getWritingTaskDisplayTitle(
            question,
            displayQuestionNumber
        ) === "Email / Reply Task";
    const isVstepEssayTask =
        question.isWritingQuestion &&
        question.writingTaskType === "Essay" &&
        !/\btoeic\b/i.test(activeAttempt?.testTitle || "");
    const isStandaloneVstepWritingTask =
        isEmailReplyTask || isVstepEssayTask;
    const isToeicPictureWriting = isToeicPictureWritingQuestion(
        question,
        displayQuestionNumber
    );
    const isVstepSpeaking =
        question.isSpeakingQuestion &&
        isVstepSpeakingTask(question);
    const isToeicSpeaking =
        question.isSpeakingQuestion &&
        /\btoeic\b/i.test(activeAttempt?.testTitle || "");
    const toeicSpeakingGroup = isToeicSpeaking
        ? getToeicSpeakingGroup(question)
        : null;
    const toeicListeningFallback =
        question.isListeningQuestion &&
        (Number(question.listeningCourseType) === 1 ||
            /\btoeic\b/i.test(activeAttempt?.testTitle || ""))
            ? getToeicListeningPartContext(displayQuestionNumber)
            : null;
    const listeningPartTitle =
        toeicListeningFallback?.title || question.listeningPartTitle;
    const listeningDirection =
        toeicListeningFallback?.direction || question.listeningDirection;
    const listeningQuestionContent = getListeningQuestionDisplayContent(
        question.content,
        displayQuestionNumber
    );

    return (
        <div className="stt-question-card">
            {question.isListeningQuestion && (
                <div className="stt-context-box">
                    {listeningPartTitle && (
                        <h2 className="stt-context-title">
                            {formatListeningPartTitle(listeningPartTitle)}
                        </h2>
                    )}

                    {listeningDirection && (
                        <div className="stt-direction-box">
                            <strong>Directions:</strong>{" "}
                            {listeningDirection}
                        </div>
                    )}
                    {question.listeningLittlePart && (
                        <h3 className="stt-context-sub">
                            {question.listeningLittlePart}
                        </h3>
                    )}
                    {question.listeningPromptImageUrl && (
                        <>
                            <h3 className="stt-question-title">
                                Question {displayQuestionNumber}
                                {listeningQuestionContent
                                    ? `: ${listeningQuestionContent}`
                                    : ""}
                            </h3>
                            <img
                                src={question.listeningPromptImageUrl}
                                alt={`Listening question ${displayQuestionNumber}`}
                                className="stt-listening-prompt-image"
                            />
                        </>
                    )}
                    {question.audioUrl && (
                        <>
                            <h3
                                style={{
                                    marginTop: 0,
                                    marginBottom: "12px"
                                }}
                            >
                                Listening Audio
                            </h3>

                            <OneTimeAudioPlayer
                                src={question.audioUrl}
                                className="stt-audio"
                                hasPlayed={hasPlayedListeningAudio}
                                onStart={onStartListeningAudio}
                            />
                        </>
                    )}

                </div>
            )}
            {question.isReadingQuestion && (
                <>
                <div className="stt-context-box">
                    {toeicReadingTitle ? (
                        <>
                            <h2 className="stt-context-title stt-reading-passage-title">
                                {toeicReadingTitle.partTitle}
                            </h2>
                            {question.readingInstruction && (
                                <div className="stt-direction-box">
                                    <strong>Directions:</strong>{" "}
                                    {question.readingInstruction}
                                </div>
                            )}
                        </>
                    ) : (
                        <>
                    {!isToeicReadingRangeTitle(
                        question.readingPassageTitle
                    ) && (
                        <h2 className="stt-context-title stt-reading-passage-title">
                            {displayReadingPassageTitle ||
                                "Reading Passage"}
                        </h2>
                    )}

                    {vstepReadingDisplay ? (
                        <div className="stt-direction-box">
                            <strong>
                                {vstepReadingDisplay.passageLabel}
                            </strong>
                            <br />
                            <strong>Directions:</strong>{" "}
                            {vstepReadingDisplay.direction}
                        </div>
                    ) : question.readingInstruction && (
                        <div className="stt-direction-box">
                            <strong>Directions:</strong>{" "}
                            {question.readingInstruction}
                        </div>
                    )}
                        </>
                    )}

                    {!toeicReadingTitle && hasReadableReadingPassage &&
                    !isToeicReadingPartLabel(
                        displayReadingPassageText
                    ) ? (
                        <div className="stt-passage">
                            {formatReadingPassageText(displayReadingPassageText)}
                        </div>
                    ) : !toeicReadingTitle && Array.isArray(
                        question.readingPassageImages
                    ) && question.readingPassageImages.length > 0 ? (
                        <div className="stt-passage-images">
                            {question.readingPassageImages
                                .slice()
                                .sort(
                                    (a, b) =>
                                        Number(a.orderIndex || 0) -
                                        Number(b.orderIndex || 0)
                                )
                                .map((image, index) => (
                                    <div
                                        key={
                                            image.readingPassageImageId ||
                                            image.imageUrl ||
                                            `${image.pageNumber}-${index}`
                                        }
                                        className="stt-passage-image-card"
                                    >
                                        <img
                                            src={image.imageUrl}
                                            alt={
                                                `${question.readingPassageTitle ||
                                                "Reading Passage"} - phần ${index + 1
                                                }`
                                            }
                                            className="stt-passage-image"
                                            loading="eager"
                                        />
                                    </div>
                                ))}
                        </div>
                    ) : !toeicReadingTitle && displayReadingPassageText &&
                    !isToeicReadingPartLabel(
                        displayReadingPassageText
                    ) ? (
                        <div className="stt-passage">
                            {formatReadingPassageText(displayReadingPassageText)}
                        </div>
                    ) : null}
                </div>
                {toeicReadingTitle && (
                    <div className="stt-context-box">
                        <p
                            style={{
                                margin: "0 0 16px",
                                fontSize: "18px",
                                fontWeight: 700
                            }}
                        >
                            {toeicReadingTitle.groupTitle}
                        </p>
                        {hasReadableReadingPassage ? (
                            <div className="stt-passage">
                                {formatReadingPassageText(question.readingPassageText)}
                            </div>
                        ) : null}
                    </div>
                )}
                </>
            )}

            {question.isWritingQuestion && (
                <>
                    {isStandaloneVstepWritingTask ? (
                        <>
                            <h3 className="stt-question-title">
                                {getWritingTaskDisplayTitle(
                                    question,
                                    displayQuestionNumber
                                )}
                            </h3>
                        </>
                    ) : !isToeicPictureWriting &&
                        !toeicWrittenRequest ? (
                        <h3 className="stt-question-title">
                            Question {displayQuestionNumber}: {question.content}
                        </h3>
                    ) : null}
                    {isToeicPictureWriting &&
                        shouldShowWritingInstruction(
                            question,
                            displayQuestionNumber
                        ) && (
                        <div className="stt-context-box">
                            <h2 className="stt-context-title">
                                {getWritingTaskDisplayTitle(
                                    question,
                                    displayQuestionNumber
                                )}
                            </h2>
                            {question.writingInstruction && (
                                <div className="stt-direction-box">
                                    <strong>Directions:</strong>{" "}
                                    {question.writingInstruction}
                                </div>
                            )}
                        </div>
                    )}
                    {toeicWrittenRequest &&
                        toeicWrittenRequest.questionNumber === 6 && (
                        <div className="stt-context-box">
                            <h2 className="stt-context-title">
                                {toeicWrittenRequest.sectionTitle}
                            </h2>
                            <div className="stt-direction-box">
                                <strong>Directions:</strong>{" "}
                                {toeicWrittenRequest.direction}
                            </div>
                        </div>
                    )}
                    <div className="stt-context-box">
                    {!isStandaloneVstepWritingTask &&
                        !isToeicPictureWriting &&
                        !toeicWrittenRequest && (
                        <>
                            <h2 className="stt-context-title">
                                {getWritingTaskDisplayTitle(
                                    question,
                                    displayQuestionNumber
                                )}
                            </h2>
                            {question.writingInstruction &&
                                shouldShowWritingInstruction(
                                    question,
                                    displayQuestionNumber
                                ) && <p>{question.writingInstruction}</p>}
                        </>
                    )}

                    {isToeicPictureWriting && (
                        <h2 className="stt-context-title">
                            Question {displayQuestionNumber}: {" "}
                            {getToeicPictureWritingPromptTitle(
                                question,
                                displayQuestionNumber
                            )}
                        </h2>
                    )}

                    {toeicWrittenRequest && (
                        <>
                            <h2
                                className="stt-context-title"
                                style={{ fontSize: "18px", fontWeight: 700 }}
                            >
                                Question {toeicWrittenRequest.questionNumber}: {" "}
                                {toeicWrittenRequest.prompt}
                            </h2>
                            <div className="writing-email-box">
                                {toeicWrittenRequest.sourceText}
                            </div>
                        </>
                    )}

                    {!isStandaloneVstepWritingTask &&
                        !toeicWrittenRequest &&
                        question.writingReferenceText &&
                        !isRepeatedWritingDetail(
                            question.writingReferenceText,
                            question.writingPromptText
                        ) && (
                        <p style={{ whiteSpace: "pre-line" }}>
                            {question.writingReferenceText}
                        </p>
                    )}

                    {!toeicWrittenRequest &&
                        question.writingPromptImageUrl && (
                        <img
                            src={question.writingPromptImageUrl}
                            alt={question.content || "Writing prompt"}
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

                    {question.writingSourceText &&
                        !toeicWrittenRequest &&
                        !isRepeatedWritingDetail(
                            question.writingSourceText,
                            question.writingPromptText
                        ) && (
                        <div className="writing-source-box">
                            {question.writingSourceText}
                        </div>
                    )}

                    {!isToeicPictureWriting &&
                        !toeicWrittenRequest &&
                        question.writingPromptText && (
                        writingEmailPrompt ? (
                            <>
                                <p style={{ whiteSpace: "pre-line" }}>
                                    {writingEmailPrompt.beforeEmail}
                                </p>
                                <div className="writing-email-box">
                                    {writingEmailPrompt.emailContent}
                                </div>
                                <p style={{ whiteSpace: "pre-line" }}>
                                    {writingEmailPrompt.afterEmail}
                                </p>
                            </>
                        ) : (
                            <p style={{ whiteSpace: "pre-line" }}>
                                {question.writingPromptText}
                            </p>
                        )
                    )}

                    {!toeicWrittenRequest &&
                        question.writingRequirements &&
                        !isRepeatedWritingDetail(
                            question.writingRequirements,
                            question.writingPromptText
                        ) && (
                        <p style={{ whiteSpace: "pre-line" }}>
                            {question.writingRequirements}
                        </p>
                    )}

                    {!isToeicPictureWriting && !toeicWrittenRequest && (
                        <p className="stt-min-words">
                            Minimum words:{" "}
                            {question.writingMinimumWords || 120}
                        </p>
                    )}
                    </div>
                </>
            )}

            {question.isSpeakingQuestion && (
                <>
                    {toeicSpeakingGroup &&
                        displayQuestionNumber === toeicSpeakingGroup.startQuestion && (
                        <div className="stt-context-box">
                            <h2 className="stt-context-title">
                                {toeicSpeakingGroup.title}
                            </h2>
                            <div className="stt-direction-box">
                                <strong>Directions:</strong>{" "}
                                {toeicSpeakingGroup.direction}
                            </div>
                        </div>
                    )}
                    {isVstepSpeaking && (
                        <>
                            <h3 className="stt-question-title">Speaking Task</h3>
                            <p>
                                {question.speakingInstruction}
                                {getVstepDuration(question.speakingPromptText) &&
                                    ` ${getVstepDuration(
                                        question.speakingPromptText
                                    )}`}
                            </p>
                        </>
                    )}
                    <div className="stt-context-box">
                        {isToeicSpeaking && (
                            <h3 className="stt-question-title">
                                Question {displayQuestionNumber}
                            </h3>
                        )}
                        <SpeakingTaskContent
                            question={question}
                            hideVstepHeading={isVstepSpeaking}
                            hideToeicGroupInstruction={Boolean(toeicSpeakingGroup)}
                            showPromptBeforeImage={
                                isToeicSpeaking &&
                                [3, 4].includes(displayQuestionNumber)
                            }
                            promptTextOverride={toeicSpeakingGroup?.promptText}
                        />
                    </div>
                </>
            )}

            {!question.isWritingQuestion &&
                !question.isSpeakingQuestion &&
                !(question.isListeningQuestion &&
                    question.listeningPromptImageUrl) &&
                !isToeicClozeQuestion && (
                    <h3 className="stt-question-title">
                        Question {displayQuestionNumber}: {question.isListeningQuestion
                            ? listeningQuestionContent
                            : question.content}
                    </h3>
                )}

            {shouldShowChoices && (
                <div className="stt-options">
                    {question.options.map((option, index) => {
                        const optionLabel = String.fromCharCode(65 + index);
                        const isSelected =
                            answerValue === getOptionId(question, option);

                        return (
                            <label
                                key={getOptionId(question, option)}
                                className={
                                    isSelected
                                        ? "stt-option selected"
                                        : "stt-option"
                                }
                            >
                                <input
                                    type="radio"
                                    name={answerKey}
                                    checked={isSelected}
                                    onChange={() =>
                                        onAnswerChange(
                                            getOptionId(question, option)
                                        )
                                    }
                                />

                                <span>
                                    <span className="stt-option-label">{optionLabel}.</span>{" "}
                                    {option.content}
                                </span>
                            </label>
                        );
                    })}
                </div>
            )}

            {question.isWritingQuestion && (
                <textarea
                    className="stt-textarea"
                    placeholder="Write your answer here..."
                    value={answerValue || ""}
                    onChange={e =>
                        onAnswerChange(e.target.value)
                    }
                />
            )}

            {question.isSpeakingQuestion && (
                <div style={{ marginTop: "18px" }}>
                    <SpeakingRecorder
                        key={`${activeAttempt.testAttemptId}-${question.speakingTaskId}`}
                        maxSeconds={
                            question.speakingTimeSeconds || 120
                        }
                        storageKey={
                            `test-speaking-${activeAttempt.testAttemptId}-${question.speakingTaskId}`
                        }
                        onRecorded={(blob) => {
                            setSpeakingAudios(prev => ({
                                ...prev,
                                [answerKey]: blob
                            }));
                        }}
                    />

                    <button
                        type="button"
                        className="lesson-btn stt-save-audio-btn"
                        disabled={loading}
                        onClick={() => onUploadSpeaking(question)}
                    >
                        Save Speaking Audio
                    </button>

                    {speakingResults[answerKey] && (
                        <div className="stt-speaking-result">
                            Audio saved. The system will grade it after submission.
                        </div>
                    )}
                </div>
            )}
        </div>
    );
}

function OneTimeAudioPlayer({
    src,
    className,
    hasPlayed,
    onStart
}) {
    return (
        <audio
            controls
            src={src}
            className={hasPlayed ? `${className} is-locked` : className}
            onPlay={onStart}
        />
    );
}

export default StudentTestPage;
