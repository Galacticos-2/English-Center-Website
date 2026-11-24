import {
    useEffect,
    useMemo,
    useRef,
    useState
} from "react";
import SpeakingRecorder
    from "../../../speaking/student/components/SpeakingRecorder";
import SpeakingTaskContent, {
    getVstepDuration,
    getToeicSpeakingGroup,
    isVstepSpeakingTask
}
    from "../../../speaking/student/components/SpeakingTaskContent";


import {
    useNavigate,
    useParams
} from "react-router-dom";

import QuestionPalette
    from "../../../question-palette/components/QuestionPalette";
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
    parseUtcDate
} from "../../../../utils/dateTimeUtils";

import {
    startCompetitionAttemptApi,
    submitCompetitionAttemptApi,
    submitCompetitionSpeakingAnswerApi
} from "../api/studentCompetitionApi";

import {
    clearCompetitionAttempt,
    getSavedCompetitionAttempt,
    saveCompetitionAttempt,
    saveCompetitionResult
} from "../utils/competitionStorage";

import "../../../lesson/styles/lesson.css";
import "../../../student/styles/exercise.css";
import "../styles/studentCompetition.css";

const COMPETITION_SKILLS = [
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
const getListeningAudioKey = question =>
    question.listeningTaskId
        ? `listening-task-${question.listeningTaskId}`
        : question.audioUrl;
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

function StudentCompetitionAttemptPage() {
    const {
        competitionTestAssignmentId
    } = useParams();

    const navigate = useNavigate();

    const [attempt, setAttempt] =
        useState(null);

    const [answers, setAnswers] =
        useState({});

    const [answersLoaded, setAnswersLoaded] =
        useState(false);

    const [markedQuestions, setMarkedQuestions] =
        useState({});

    const [visitedQuestions, setVisitedQuestions] =
        useState({});
    const [speakingAudios, setSpeakingAudios] =
        useState({});
    const [activeSkill, setActiveSkill] =
        useState(2);

    const [activeQuestionIndex, setActiveQuestionIndex] =
        useState(0);

    const [remainingSeconds, setRemainingSeconds] =
        useState(0);

    const [submitting, setSubmitting] =
        useState(false);

   
    const [isTimerReady, setIsTimerReady] =
        useState(false);

    const [uploadingTaskId, setUploadingTaskId] =
        useState(null);

    const [competitionEndAt, setCompetitionEndAt] =
        useState(null);

    const [uploadedSpeaking, setUploadedSpeaking] =
        useState({});
    const [playedListeningAudios, setPlayedListeningAudios] =
        useState({});

    

    const autoSubmitRef =
        useRef(false);

    const handleSubmitRef =
        useRef(null);

    const getAnswersStorageKey =
        competitionTestAttemptId =>
            `competition-answers-${competitionTestAttemptId}`;

    const getPaletteStorageKey =
        competitionTestAttemptId =>
            `competition-palette-${competitionTestAttemptId}`;

    const getQuestionSkill = question => {
        if (question?.isListeningQuestion) {
            return 2;
        }

        if (question?.isSpeakingQuestion) {
            return 3;
        }

        if (question?.isReadingQuestion) {
            return 4;
        }

        if (question?.isWritingQuestion) {
            return 5;
        }

        return 0;
    };

    const getQuestionKey = question => {
        if (question?.listeningQuestionId) {
            return `listening-${question.listeningQuestionId}`;
        }

        if (question?.readingQuestionId) {
            return `reading-${question.readingQuestionId}`;
        }

        if (question?.writingTaskId) {
            return `writing-${question.writingTaskId}`;
        }

        return `speaking-${question?.speakingTaskId}`;
    };

    const loadAttempt = async () => {
        const saved =
            getSavedCompetitionAttempt();

        try {
            setIsTimerReady(false);
            autoSubmitRef.current = false;

            /*
             * Luôn hỏi backend để xác nhận:
             * - attempt còn InProgress hay không
             * - attempt có đúng student hay không
             * - attempt đã hết giờ hay chưa
             *
             * Backend phải trả lại attempt cũ nếu đang làm dở,
             * không tạo attempt mới và không reset ExpiresAt.
             */
            const data =
                await startCompetitionAttemptApi(
                    competitionTestAssignmentId
                );

            const expiresAtDate =
                parseUtcDate(data.expiresAt);

            if (!expiresAtDate) {
                throw new Error(
                    "The test end time is invalid."
                );
            }

            const serverTimeDate =
                parseUtcDate(data.serverTime);

            const backendEndAt =
                expiresAtDate.getTime();

            const backendNow =
                serverTimeDate
                    ? serverTimeDate.getTime()
                    : Date.now();

            if (!Number.isFinite(backendEndAt)) {
                throw new Error(
                    "Backend returned an invalid ExpiresAt"
                );
            }

            const remainingMilliseconds =
                Math.max(
                    0,
                    backendEndAt - backendNow
                );

            /*
             * Luôn lấy thời gian còn lại theo clock của backend.
             * Không dùng competitionEndAt cũ trong localStorage,
             * vì dữ liệu cũ có thể đã được lưu sai múi giờ.
             * Không trừ thẳng Date.now() với ExpiresAt backend,
             * vì đồng hồ máy học viên có thể lệch server.
             */
            const validEndAt =
                Date.now() + remainingMilliseconds;

            const attemptToSave = {
                ...data,
                competitionEndAt: validEndAt
            };

            saveCompetitionAttempt(
                attemptToSave
            );

            setAttempt(
                attemptToSave
            );

            setCompetitionEndAt(
                validEndAt
            );

            const secondsLeft =
                Math.max(
                    0,
                    Math.ceil(
                        remainingMilliseconds / 1000
                    )
                );

            setRemainingSeconds(
                secondsLeft
            );

            setIsTimerReady(true);

            console.log(
                "COMPETITION TIMER DEBUG:",
                {
                    backendExpiresAt:
                        data.expiresAt,

                    parsedExpiresAt:
                        expiresAtDate.toISOString(),

                    backendEndAt,

                    backendServerTime:
                        serverTimeDate
                            ?.toISOString(),

                    browserNow:
                        new Date().toISOString(),

                    secondsLeft
                }
            );
        }
        catch (error) {
            setIsTimerReady(false);

            const backendMessage =
                error.response?.data?.message ||
                error.response?.data?.Message ||
                error.message ||
                "Unable to open the test";

            if (saved?.competitionTestAttemptId) {
                localStorage.removeItem(
                    getAnswersStorageKey(
                        saved.competitionTestAttemptId
                    )
                );

                localStorage.removeItem(
                    getPaletteStorageKey(
                        saved.competitionTestAttemptId
                    )
                );
            }

            clearCompetitionAttempt();

            setAttempt(null);
            setCompetitionEndAt(null);
            setRemainingSeconds(0);

            alert(backendMessage);

            navigate(
                "/student/competitions",
                {
                    replace: true
                }
            );
        }
    };

    useEffect(() => {
        loadAttempt();
    }, [competitionTestAssignmentId]);

    useEffect(() => {
        if (!attempt?.competitionTestAttemptId) {
            return;
        }

        setAnswersLoaded(false);

        const answersStorageKey =
            getAnswersStorageKey(
                attempt.competitionTestAttemptId
            );

        const paletteStorageKey =
            getPaletteStorageKey(
                attempt.competitionTestAttemptId
            );

        const savedAnswers =
            localStorage.getItem(
                answersStorageKey
            );

        const savedPalette =
            localStorage.getItem(
                paletteStorageKey
            );

        if (!savedAnswers) {
            setAnswers({});
        }
        else {
            try {
                const parsedAnswers =
                    JSON.parse(savedAnswers);

                if (
                    parsedAnswers &&
                    typeof parsedAnswers === "object" &&
                    !Array.isArray(parsedAnswers)
                ) {
                    setAnswers(parsedAnswers);
                }
                else {
                    setAnswers({});
                }
            }
            catch (error) {
                console.error(
                    "Không thể đọc đáp án Competition từ localStorage:",
                    error
                );

                localStorage.removeItem(
                    answersStorageKey
                );

                setAnswers({});
            }
        }

        if (savedPalette) {
            try {
                const parsedPalette =
                    JSON.parse(savedPalette);

                setMarkedQuestions(
                    parsedPalette.markedQuestions || {}
                );

                setVisitedQuestions(
                    parsedPalette.visitedQuestions || {}
                );

                setActiveSkill(
                    parsedPalette.activeSkill || 2
                );

                setActiveQuestionIndex(
                    Number.isInteger(
                        parsedPalette.activeQuestionIndex
                    )
                        ? parsedPalette.activeQuestionIndex
                        : 0
                );

                setPlayedListeningAudios(
                    parsedPalette.playedListeningAudios || {}
                );
            }
            catch (error) {
                console.error(
                    "Không thể đọc trạng thái Question Palette từ localStorage:",
                    error
                );

                localStorage.removeItem(
                    paletteStorageKey
                );

                setMarkedQuestions({});
                setVisitedQuestions({});
                setPlayedListeningAudios({});
                setActiveQuestionIndex(0);
            }
        }
        else {
            setMarkedQuestions({});
            setVisitedQuestions({});
            setPlayedListeningAudios({});
            setActiveQuestionIndex(0);
        }

        setAnswersLoaded(true);
    }, [attempt?.competitionTestAttemptId]);

    useEffect(() => {
        if (
            !attempt?.competitionTestAttemptId ||
            !answersLoaded
        ) {
            return;
        }

        try {
            localStorage.setItem(
                getAnswersStorageKey(
                    attempt.competitionTestAttemptId
                ),
                JSON.stringify(answers)
            );

            localStorage.setItem(
                getPaletteStorageKey(
                    attempt.competitionTestAttemptId
                ),
                JSON.stringify({
                    markedQuestions,
                    visitedQuestions,
                    playedListeningAudios,
                    activeSkill,
                    activeQuestionIndex
                })
            );
        }
        catch (error) {
            console.error(
                "Không thể lưu tạm đáp án Competition vào localStorage:",
                error
            );
        }
    }, [
        attempt?.competitionTestAttemptId,
        answers,
        answersLoaded,
        markedQuestions,
        visitedQuestions,
        playedListeningAudios,
        activeSkill,
        activeQuestionIndex
    ]);

    const questions =
        useMemo(
            () => attempt?.questions || [],
            [attempt]
        );

    const availableSkills =
        useMemo(
            () =>
                COMPETITION_SKILLS.filter(skill =>
                    questions.some(question =>
                        getQuestionSkill(question) === skill.id
                    )
                ),
            [questions]
        );

    const filteredQuestions =
        useMemo(
            () =>
                questions.filter(question =>
                    getQuestionSkill(question) === activeSkill
                ),
            [questions, activeSkill]
        );

    const activeQuestion =
        filteredQuestions[activeQuestionIndex];

    useEffect(() => {
        if (availableSkills.length === 0) {
            return;
        }

        const activeSkillExists =
            availableSkills.some(skill =>
                skill.id === activeSkill
            );

        if (!activeSkillExists) {
            setActiveSkill(
                availableSkills[0].id
            );

            setActiveQuestionIndex(0);
        }
    }, [availableSkills, activeSkill]);

    useEffect(() => {
        if (filteredQuestions.length === 0) {
            setActiveQuestionIndex(0);
            return;
        }

        if (
            activeQuestionIndex >=
            filteredQuestions.length
        ) {
            setActiveQuestionIndex(0);
        }
    }, [filteredQuestions, activeQuestionIndex]);

    useEffect(() => {
        if (!activeQuestion) {
            return;
        }

        const key =
            getQuestionKey(activeQuestion);

        setVisitedQuestions(current => {
            if (current[key]) {
                return current;
            }

            return {
                ...current,
                [key]: true
            };
        });
    }, [activeQuestion]);

    const setAnswerValue = (
        question,
        value
    ) => {
        const key =
            getQuestionKey(question);

        setAnswers(current => ({
            ...current,
            [key]: value
        }));
    };

    const handleChangeSkill = skillId => {
        setActiveSkill(skillId);
        setActiveQuestionIndex(0);

        const firstQuestion =
            questions.find(question =>
                getQuestionSkill(question) === skillId
            );

        if (firstQuestion) {
            const key =
                getQuestionKey(firstQuestion);

            setVisitedQuestions(current => ({
                ...current,
                [key]: true
            }));
        }
    };

    const handleSelectQuestion = index => {
        const question =
            filteredQuestions[index];

        if (!question) {
            return;
        }

        setActiveQuestionIndex(index);

        setVisitedQuestions(current => ({
            ...current,
            [getQuestionKey(question)]: true
        }));
    };

    const handleMarkQuestion = () => {
        if (!activeQuestion) {
            return;
        }

        const key =
            getQuestionKey(activeQuestion);

        setMarkedQuestions(current => ({
            ...current,
            [key]: !current[key]
        }));
    };

    const buildSubmitAnswers = () => {
        return questions
            .map(question => {
                const value =
                    answers[
                    getQuestionKey(question)
                    ];

                if (question.isListeningQuestion) {
                    return {
                        listeningQuestionId:
                            question.listeningQuestionId,

                        selectedListeningOptionId:
                            value
                                ? Number(value)
                                : null
                    };
                }

                if (question.isReadingQuestion) {
                    return {
                        readingQuestionId:
                            question.readingQuestionId,

                        selectedReadingOptionId:
                            value
                                ? Number(value)
                                : null
                    };
                }

                if (question.isWritingQuestion) {
                    return {
                        writingTaskId:
                            question.writingTaskId,

                        textAnswer:
                            value || ""
                    };
                }

                if (question.isSpeakingQuestion) {
                    return {
                        speakingTaskId:
                            question.speakingTaskId
                    };
                }

                return null;
            })
            .filter(Boolean);
    };

    const handleSubmit = async isAutoSubmit => {
        if (
            submitting ||
            !attempt
        ) {
            return;
        }

        if (!isAutoSubmit) {
            const confirmed =
                window.confirm(
                    "Are you sure you want to submit?"
                );

            if (!confirmed) {
                return;
            }
        }

        setSubmitting(true);

        try {
            const result =
                await submitCompetitionAttemptApi(
                    attempt.competitionTestAttemptId,
                    buildSubmitAnswers()
                );

            localStorage.removeItem(
                getAnswersStorageKey(
                    attempt.competitionTestAttemptId
                )
            );

            localStorage.removeItem(
                getPaletteStorageKey(
                    attempt.competitionTestAttemptId
                )
            );

            clearCompetitionAttempt();

            setIsTimerReady(false);
            setCompetitionEndAt(null);
            setRemainingSeconds(0);

            saveCompetitionResult(result);

            navigate(
                "/student/competitions/result",
                {
                    replace: true
                }
            );
        }
        catch (error) {
            const responseData =
                error.response?.data;

            const backendMessage =
                responseData?.message ||
                responseData?.Message ||
                responseData?.errorMessage ||
                responseData?.title ||
                "";

            console.error(
                "COMPETITION SUBMIT ERROR:",
                {
                    status:
                        error.response?.status,

                    responseData,

                    competitionTestAttemptId:
                        attempt?.competitionTestAttemptId,

                    answers:
                        buildSubmitAnswers(),

                    error
                }
            );

            const validationMessage =
                responseData?.errors
                    ? Object.entries(
                        responseData.errors
                    )
                        .flatMap(
                            ([field, messages]) =>
                                messages.map(
                                    message =>
                                        `${field}: ${message}`
                                )
                        )
                        .join("\n")
                    : null;

            const attemptIsClosed =
                backendMessage.includes(
                    "Only an in-progress competition attempt can be modified"
                ) ||
                backendMessage.includes(
                    "already been submitted"
                ) ||
                backendMessage.includes(
                    "already completed"
                ) ||
                backendMessage.includes(
                    "no longer in progress"
                ) ||
                backendMessage.includes(
                    "time has expired"
                ) ||
                backendMessage.includes(
                    "deadline has expired"
                );

            if (attemptIsClosed) {
                localStorage.removeItem(
                    getAnswersStorageKey(
                        attempt.competitionTestAttemptId
                    )
                );

                localStorage.removeItem(
                    getPaletteStorageKey(
                        attempt.competitionTestAttemptId
                    )
                );

                clearCompetitionAttempt();

                setIsTimerReady(false);
                setAttempt(null);
                setCompetitionEndAt(null);
                setRemainingSeconds(0);

                alert(
                    backendMessage ||
                    "This test is no longer in progress."
                );

                navigate(
                    "/student/competitions",
                    {
                        replace: true
                    }
                );

                return;
            }

            alert(
                backendMessage ||
                validationMessage ||
                (
                    isAutoSubmit
                        ? "Time is up"
                        : "Unable to submit"
                )
            );

            if (isAutoSubmit) {
                navigate(
                    "/student/competitions",
                    {
                        replace: true
                    }
                );
            }
        }
        finally {
            setSubmitting(false);
        }
    };

    useEffect(() => {
        handleSubmitRef.current =
            handleSubmit;
    });

    useEffect(() => {
        if (
            !isTimerReady ||
            !attempt ||
            !competitionEndAt
        ) {
            return;
        }

        const updateTimer = () => {
            const nextSeconds =
                Math.max(
                    0,
                    Math.ceil(
                        (
                            competitionEndAt -
                            Date.now()
                        ) / 1000
                    )
                );

            setRemainingSeconds(
                nextSeconds
            );

            if (
                nextSeconds <= 0 &&
                !autoSubmitRef.current
            ) {
                autoSubmitRef.current = true;

                handleSubmitRef.current?.(
                    true
                );
            }
        };

        updateTimer();

        const timer =
            window.setInterval(
                updateTimer,
                1000
            );

        return () => {
            window.clearInterval(timer);
        };
    }, [
        isTimerReady,
        attempt,
        competitionEndAt
    ]);

   
    const handleSaveSpeakingAudio =
        async speakingTaskId => {
            const blob =
                speakingAudios[
                speakingTaskId
                ];

            if (!blob) {
                alert(
                    "Please record your response before saving the audio."
                );

                return;
            }

            await uploadSpeakingAudio(
                speakingTaskId,
                blob
            );
        };


    const uploadSpeakingAudio =
        async (
            speakingTaskId,
            blob
        ) => {
            setUploadingTaskId(
                speakingTaskId
            );

            try {
                const formData =
                    new FormData();

                formData.append(
                    "competitionTestAttemptId",
                    attempt.competitionTestAttemptId
                );

                formData.append(
                    "speakingTaskId",
                    speakingTaskId
                );

                formData.append(
                    "audioFile",
                    blob,
                    `speaking-${speakingTaskId}.webm`
                );

                const result =
                    await submitCompetitionSpeakingAnswerApi(
                        formData
                    );

                const savedValue =
                    result.audioAnswerUrl ||
                    "uploaded";

                setUploadedSpeaking(
                    current => ({
                        ...current,
                        [speakingTaskId]:
                            savedValue
                    })
                );
               
                /*
                 * Chỉ dùng để QuestionPalette nhận biết câu Speaking
                 * đã có câu trả lời. Payload submit Competition vẫn giữ
                 * nguyên: chỉ gửi speakingTaskId như logic gốc.
                 */
                setAnswers(current => ({
                    ...current,
                    [`speaking-${speakingTaskId}`]:
                        savedValue
                }));

                alert("Speaking audio saved.");
            }
            catch (error) {
                alert(
                    error.response?.data?.message ||
                    "Unable to upload the speaking audio"
                );
            }
            finally {
                setUploadingTaskId(null);
            }
        };

    const formatTimer = seconds => {
        const hours =
            Math.floor(seconds / 3600);

        const minutes =
            Math.floor(
                (seconds % 3600) / 60
            );

        const remaining =
            seconds % 60;

        return [
            hours,
            minutes,
            remaining
        ]
            .map(value =>
                String(value).padStart(2, "0")
            )
            .join(":");
    };

    if (!attempt) {
        return (
            <div className="competition-attempt-loading">
                Opening test...
            </div>
        );
    }

    return (
        <div className="competition-attempt-test-layout">
            <main className="competition-attempt-test-main">
                <div className="competition-attempt-test-header">
                    <h1>
                        {attempt.competitionTestTitle}
                    </h1>
                </div>

                <div className="competition-attempt-skill-tabs">
                    {availableSkills.map(skill => (
                        <button
                            key={skill.id}
                            type="button"
                            className={
                                activeSkill === skill.id
                                    ? "lesson-btn"
                                    : "lesson-btn secondary"
                            }
                            onClick={() =>
                                handleChangeSkill(
                                    skill.id
                                )
                            }
                        >
                            {skill.name}
                        </button>
                    ))}

                    <div
                        className={
                            remainingSeconds <= 300
                                ? "competition-attempt-test-timer danger"
                                : "competition-attempt-test-timer"
                        }
                    >
                        {formatTimer(
                            remainingSeconds
                        )}
                    </div>
                </div>

                {!activeQuestion ? (
                    <div className="competition-attempt-no-question">
                        There are no questions for this skill.
                    </div>
                ) : (
                    <CompetitionQuestionBlock
                        question={activeQuestion}
                        isToeicCompetition={
                            /\btoeic\b/i.test(
                                attempt.competitionTestTitle || "")
                        }
                        displayQuestionNumber={
                            activeQuestionIndex + 1
                        }
                        answerValue={
                            answers[
                            getQuestionKey(
                                activeQuestion
                            )
                            ]
                        }
                        answerKey={
                            getQuestionKey(
                                activeQuestion
                            )
                        }
                        onAnswerChange={value =>
                            setAnswerValue(
                                activeQuestion,
                                value
                            )
                        }
                            competitionTestAttemptId={
                                attempt.competitionTestAttemptId
                            }
                            uploadingTaskId={
                                uploadingTaskId
                            }
                            uploadedSpeaking={
                                uploadedSpeaking
                            }
                            speakingAudios={
                                speakingAudios
                            }
                            setSpeakingAudios={
                                setSpeakingAudios
                            }
                            onSaveSpeakingAudio={
                                handleSaveSpeakingAudio
                            }
                            hasPlayedListeningAudio={Boolean(
                                playedListeningAudios[
                                getListeningAudioKey(activeQuestion)
                                ]
                            )}
                            onStartListeningAudio={() =>
                                setPlayedListeningAudios(current => ({
                                    ...current,
                                    [getListeningAudioKey(activeQuestion)]: true
                                }))
                            }
                    />
                )}

                <div className="competition-attempt-navigation">
                    <button
                        type="button"
                        className="lesson-btn secondary"
                        disabled={
                            activeQuestionIndex === 0
                        }
                        onClick={() =>
                            handleSelectQuestion(
                                activeQuestionIndex - 1
                            )
                        }
                    >
                        Previous Question
                    </button>

                    <button
                        type="button"
                        className="lesson-btn secondary"
                        disabled={
                            activeQuestionIndex >=
                            filteredQuestions.length - 1
                        }
                        onClick={() =>
                            handleSelectQuestion(
                                activeQuestionIndex + 1
                            )
                        }
                    >
                        Next Question
                    </button>
                </div>
            </main>

            <QuestionPalette
                questions={filteredQuestions}
                currentIndex={activeQuestionIndex}
                selectedAnswers={answers}
                markedQuestions={markedQuestions}
                visitedQuestions={visitedQuestions}
                loading={submitting}
                onSelectQuestion={handleSelectQuestion}
                onMarkQuestion={handleMarkQuestion}
                onSubmit={() =>
                    handleSubmit(false)
                }
            />
        </div>
    );
}

function CompetitionQuestionBlock({
    question,
    isToeicCompetition,
    displayQuestionNumber,
    answerValue,
    answerKey,
    onAnswerChange,
    competitionTestAttemptId,
    uploadingTaskId,
    uploadedSpeaking,
    speakingAudios,
    setSpeakingAudios,
    onSaveSpeakingAudio,
    hasPlayedListeningAudio,
    onStartListeningAudio
}) {
    const hasReadableReadingPassage =
        Boolean(question.readingPassageText) &&
        !question.readingPassageText
            .toLowerCase()
            .includes("provided as images");
    const getOptionId = option => {
        if (question.isListeningQuestion) {
            return option.listeningOptionId;
        }

        if (question.isReadingQuestion) {
            return option.readingOptionId;
        }

        return option.questionOptionId;
    };

    const shouldShowChoices =
        question.isListeningQuestion ||
        question.isReadingQuestion;
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
        !isToeicCompetition;
    const isStandaloneVstepWritingTask =
        isEmailReplyTask || isVstepEssayTask;
    const isToeicPictureWriting = isToeicPictureWritingQuestion(
        question,
        displayQuestionNumber
    );
    const isVstepSpeaking =
        question.isSpeakingQuestion &&
        !isToeicCompetition &&
        isVstepSpeakingTask(question);
    const isToeicSpeaking =
        question.isSpeakingQuestion && isToeicCompetition;
    const toeicSpeakingGroup = isToeicSpeaking
        ? getToeicSpeakingGroup(question)
        : null;
    const toeicListeningContext =
        question.isListeningQuestion &&
        (Number(question.listeningCourseType) === 1 ||
            isToeicCompetition)
            ? getToeicListeningPartContext(displayQuestionNumber)
            : null;
    const listeningPartTitle =
        toeicListeningContext?.title || question.listeningPartTitle;
    const listeningDirection =
        toeicListeningContext?.direction || question.listeningDirection;

    const speakingWasSaved =
        Boolean(
            uploadedSpeaking[
            question.speakingTaskId
            ] || answerValue
        );
    const speakingAudioBlob =
        speakingAudios[
        question.speakingTaskId
        ];

    const hasRecordedAudio =
        Boolean(speakingAudioBlob);

   

    const isCurrentUploading =
        uploadingTaskId ===
        question.speakingTaskId;
    return (
        <div className="competition-attempt-question-card">
            {question.isListeningQuestion && (
                <div className="competition-attempt-source-box">
                    {listeningPartTitle && (
                        <h2>
                            {formatListeningPartTitle(listeningPartTitle)}
                        </h2>
                    )}

                    {listeningDirection && (
                        <div className="competition-attempt-direction-box">
                            <strong>Directions:</strong>{" "}
                            {listeningDirection}
                        </div>
                    )}

                    {question.listeningLittlePart && (
                        <h3>
                            {question.listeningLittlePart}
                        </h3>
                    )}

                    {question.listeningPromptImageUrl && (
                        <img
                            src={question.listeningPromptImageUrl}
                            alt={`Listening question ${displayQuestionNumber}`}
                            className="competition-attempt-listening-prompt-image"
                        />
                    )}

                    {question.audioUrl && (
                        <>
                            <h3 className="competition-attempt-audio-title">
                                Listening Audio
                            </h3>

                            <CompetitionOneTimeAudioPlayer
                                src={question.audioUrl}
                                hasPlayed={hasPlayedListeningAudio}
                                onStart={onStartListeningAudio}
                            />
                        </>
                    )}

                </div>
            )}

            {question.isReadingQuestion &&
                (question.readingPassageText ||
                    (
                        Array.isArray(question.readingPassageImages) &&
                        question.readingPassageImages.length > 0
                    )) && (
                    <>
                    <div className="competition-attempt-source-box">
                        {toeicReadingTitle ? (
                            <>
                                <h2 className="competition-attempt-reading-passage-title">
                                    {toeicReadingTitle.partTitle}
                                </h2>
                                {question.readingInstruction && (
                                    <p>{question.readingInstruction}</p>
                                )}
                            </>
                        ) : (
                            <>
                        {!isToeicReadingRangeTitle(
                            question.readingPassageTitle
                        ) && (
                            <h2 className="competition-attempt-reading-passage-title">
                                {displayReadingPassageTitle ||
                                    "Reading Passage"}
                            </h2>
                        )}

                        {vstepReadingDisplay ? (
                            <div className="competition-attempt-direction-box">
                                <strong>
                                    {vstepReadingDisplay.passageLabel}
                                </strong>
                                <br />
                                <strong>Directions:</strong>{" "}
                                {vstepReadingDisplay.direction}
                            </div>
                        ) : question.readingInstruction && (
                            <p>{question.readingInstruction}</p>
                        )}
                            </>
                        )}

                        {!toeicReadingTitle && hasReadableReadingPassage &&
                        !isToeicReadingPartLabel(
                        displayReadingPassageText
                        ) ? (
                            <div className="competition-attempt-reading-passage">
                                {formatReadingPassageText(displayReadingPassageText)}
                            </div>
                        ) : !toeicReadingTitle && Array.isArray(question.readingPassageImages) &&
                            question.readingPassageImages.length > 0 ? (
                            <div className="competition-attempt-passage-images">
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
                                            className="competition-attempt-passage-image-card"
                                        >
                                            <img
                                                src={image.imageUrl}
                                                alt={`${question.readingPassageTitle ||
                                                    "Reading Passage"} - part ${index + 1}`}
                                                className="competition-attempt-passage-image"
                                                loading="eager"
                                            />
                                        </div>
                                    ))}
                            </div>
                        ) : !toeicReadingTitle && displayReadingPassageText &&
                        !isToeicReadingPartLabel(
                            displayReadingPassageText
                        ) ? (
                            <div className="competition-attempt-reading-passage">
                                {formatReadingPassageText(displayReadingPassageText)}
                            </div>
                        ) : null}
                    </div>
                    {toeicReadingTitle && (
                        <div className="competition-attempt-source-box">
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
                                <div className="competition-attempt-reading-passage">
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
                            <h3 className="competition-attempt-question-title">
                                {getWritingTaskDisplayTitle(
                                    question,
                                    displayQuestionNumber
                                )}
                            </h3>
                        </>
                    ) : !isToeicPictureWriting &&
                        !toeicWrittenRequest ? (
                        <h3 className="competition-attempt-question-title">
                            Question {displayQuestionNumber}: {question.content}
                        </h3>
                    ) : null}
                    {isToeicPictureWriting &&
                        shouldShowWritingInstruction(
                            question,
                            displayQuestionNumber
                        ) && (
                        <div className="competition-attempt-source-box">
                            <h2>
                                {getWritingTaskDisplayTitle(
                                    question,
                                    displayQuestionNumber
                                )}
                            </h2>
                            {question.writingInstruction && (
                                <div className="competition-attempt-direction-box">
                                    <strong>Directions:</strong>{" "}
                                    {question.writingInstruction}
                                </div>
                            )}
                        </div>
                    )}
                    {toeicWrittenRequest &&
                        toeicWrittenRequest.questionNumber === 6 && (
                        <div className="competition-attempt-source-box">
                            <h2>{toeicWrittenRequest.sectionTitle}</h2>
                            <div className="competition-attempt-direction-box">
                                <strong>Directions:</strong>{" "}
                                {toeicWrittenRequest.direction}
                            </div>
                        </div>
                    )}
                    <div className="competition-attempt-source-box">
                    {!isStandaloneVstepWritingTask &&
                        !isToeicPictureWriting &&
                        !toeicWrittenRequest && (
                        <>
                            <h2>
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
                        <h2>
                            Question {displayQuestionNumber}: {" "}
                            {getToeicPictureWritingPromptTitle(
                                question,
                                displayQuestionNumber
                            )}
                        </h2>
                    )}

                    {toeicWrittenRequest && (
                        <>
                            <h2 style={{ fontSize: "18px", fontWeight: 700 }}>
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
                        <p className="competition-attempt-pre-line">
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
                        <div className="competition-attempt-inner-box">
                            {question.writingSourceText}
                        </div>
                    )}

                    {!isToeicPictureWriting &&
                        !toeicWrittenRequest &&
                        question.writingPromptText && (
                        writingEmailPrompt ? (
                            <>
                                <p className="competition-attempt-pre-line">
                                    {writingEmailPrompt.beforeEmail}
                                </p>
                                <div className="writing-email-box">
                                    {writingEmailPrompt.emailContent}
                                </div>
                                <p className="competition-attempt-pre-line">
                                    {writingEmailPrompt.afterEmail}
                                </p>
                            </>
                        ) : (
                            <p className="competition-attempt-pre-line">
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
                        <p className="competition-attempt-pre-line">
                            {question.writingRequirements}
                        </p>
                    )}

                    {!isToeicPictureWriting &&
                        !toeicWrittenRequest &&
                        question.writingMinimumWords && (
                        <p>
                            Minimum words:{" "}
                            {question.writingMinimumWords}
                        </p>
                    )}
                    </div>
                </>
            )}

            {question.isSpeakingQuestion && (
                <>
                    {toeicSpeakingGroup &&
                        displayQuestionNumber === toeicSpeakingGroup.startQuestion && (
                        <div className="competition-attempt-source-box">
                            <h2>{toeicSpeakingGroup.title}</h2>
                            <div className="competition-attempt-direction-box">
                                <strong>Directions:</strong>{" "}
                                {toeicSpeakingGroup.direction}
                            </div>
                        </div>
                    )}
                    {isVstepSpeaking && (
                        <>
                            <h3 className="competition-attempt-question-title">
                                Speaking Task
                            </h3>
                            <p>
                                {question.speakingInstruction}
                                {getVstepDuration(question.speakingPromptText) &&
                                    ` ${getVstepDuration(
                                        question.speakingPromptText
                                    )}`}
                            </p>
                        </>
                    )}
                    <div className="competition-attempt-source-box">
                        {isToeicSpeaking && (
                            <h3 className="competition-attempt-question-title">
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

                <div className="competition-attempt-record-box">
                    <SpeakingRecorder
                        key={
                            `${competitionTestAttemptId}-${question.speakingTaskId}`
                        }
                        maxSeconds={
                            question.speakingTimeSeconds ||
                            120
                        }
                        storageKey={
                            `competition-speaking-${competitionTestAttemptId}-${question.speakingTaskId}`
                        }
                        onRecorded={blob => {
                            setSpeakingAudios(
                                current => ({
                                    ...current,
                                    [answerKey]:
                                        blob,
                                    [question.speakingTaskId]:
                                        blob
                                })
                            );
                        }}
                    />

                    <button
                        type="button"
                        className="competition-save-speaking-btn"
                        disabled={
                            !hasRecordedAudio ||
                            isCurrentUploading
                        }
                        onClick={() =>
                            onSaveSpeakingAudio(
                                question.speakingTaskId
                            )
                        }
                    >
                        {isCurrentUploading
                            ? "Saving..."
                            : "Save Speaking Audio"}
                    </button>

                    {speakingWasSaved && (
                        <div className="competition-attempt-upload-success">
                            Audio saved. The system will grade it
                            after submission.
                        </div>
                    )}
                </div>
                </>
            )}

            {!question.isWritingQuestion &&
                !question.isSpeakingQuestion &&
                !isToeicClozeQuestion && (
                    <h3 className="competition-attempt-question-title">
                        Question {displayQuestionNumber}:{" "}
                        {question.isListeningQuestion
                            ? getListeningQuestionDisplayContent(
                                question.content,
                                displayQuestionNumber
                            )
                            : question.content}
                    </h3>
                )}

            {shouldShowChoices && (
                <div className="competition-attempt-choice-list">
                    {question.options?.map(
                        (option, index) => {
                            const optionId =
                                getOptionId(option);

                            const optionLabel =
                                String.fromCharCode(
                                    65 + index
                                );

                            const selected =
                                String(answerValue || "") ===
                                String(optionId);

                            return (
                                <label
                                    key={optionId}
                                    className={
                                        selected
                                            ? "competition-attempt-choice selected"
                                            : "competition-attempt-choice"
                                    }
                                >
                                    <input
                                        type="radio"
                                        name={answerKey}
                                        checked={selected}
                                        onChange={() =>
                                            onAnswerChange(
                                                optionId
                                            )
                                        }
                                    />

                                    <span className="competition-attempt-choice-label">
                                        {optionLabel}
                                    </span>

                                    <span>
                                        {option.content}
                                    </span>
                                </label>
                            );
                        }
                    )}
                </div>
            )}

            {question.isWritingQuestion && (
                <textarea
                    className="competition-attempt-writing-editor"
                    placeholder="Write your response here..."
                    value={answerValue || ""}
                    onChange={event =>
                        onAnswerChange(
                            event.target.value
                        )
                    }
                />
            )}

          
        </div>
    );
}

function CompetitionOneTimeAudioPlayer({
    src,
    hasPlayed,
    onStart
}) {
    return (
        <audio
            controls
            src={src}
            className={
                hasPlayed
                    ? "competition-attempt-audio is-locked"
                    : "competition-attempt-audio"
            }
            onPlay={onStart}
        />
    );
}

export default StudentCompetitionAttemptPage;
