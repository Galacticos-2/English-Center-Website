import {
    useEffect,
    useMemo,
    useRef,
    useState
} from "react";

import { useNavigate } from "react-router-dom";
import {
    BarChart3,
    BookOpenCheck,
    CalendarDays,
    CheckCircle2,
    Clock3,
    FileQuestion,
    History,
    LoaderCircle,
    PlayCircle,
    RotateCcw,
  
    
    Trophy
} from "lucide-react";
import ExerciseSkillNavbar
    from "../../exercise/student/components/ExerciseSkillNavbar";
import ExerciseSidebar
    from "../../exercise/student/components/ExerciseSidebar";
import QuestionPalette
    from "../../question-palette/components/QuestionPalette";
import {
    formatReadingPassageText,
    getVstepReadingDisplayContent,
    isToeicReadingClozeQuestion,
    isToeicReadingPartLabel,
    isToeicReadingRangeTitle,
    splitToeicReadingPassageTitle
} from "../../../shared/lib/readingFormat";
import {
    getWritingTaskDisplayTitle,
    getToeicPictureWritingPromptTitle,
    getToeicWrittenRequestDisplay,
    isToeicPictureWritingQuestion,
    splitWritingEmailPromptText,
    shouldShowWritingInstruction
} from "../../writing/utils/writingDisplay";
import {
    formatListeningPartTitle,
    getListeningQuestionDisplayContent,
    getToeicListeningPartContext
}
    from "../../listening/utils/listeningDisplay";
import {
    formatVietnamDateOnly,
    formatVietnamHourMinute
} from "../../../utils/dateTimeUtils";
import "../../lesson/styles/lesson.css";
import "../styles/exercise.css";
import {

    startExerciseAttemptApi,
    submitExerciseAttemptApi,

    getMyStudentExerciseSectionsApi,
    getExerciseAttemptResultApi,

} from "../api/studentApi";

import SpeakingRecorder
    from "../../speaking/student/components/SpeakingRecorder";
import SpeakingTaskContent, {
    getVstepDuration,
    getToeicSpeakingGroup,
    isVstepSpeakingTask
}
    from "../../speaking/student/components/SpeakingTaskContent";

import {
    submitSpeakingAnswerApi
}
    from "../../speaking/api/speakingApi";

function ExercisePage() {

    const navigate = useNavigate();
    const [activeAttempt, setActiveAttempt] =
        useState(null);

    

    const [loading, setLoading] =
        useState(false);

    const [selectedAnswers, setSelectedAnswers] =
        useState({});

    const [activeSkill, setActiveSkill] =
        useState(1);
    const [studentSections, setStudentSections] =
        useState([]);

    const [activeExercise, setActiveExercise] =
        useState(null);
    const [currentQuestionIndex, setCurrentQuestionIndex] =
        useState(0);
    const sectionRequestRef = useRef(0);
    const [markedQuestions, setMarkedQuestions] =
        useState({});
    const [speakingAudios, setSpeakingAudios] =
        useState({});

    const [speakingResults, setSpeakingResults] =
        useState({});
    const [visitedQuestions, setVisitedQuestions] =
        useState({});
    const [isSidebarOpen, setIsSidebarOpen] =
        useState(true);
    const exerciseSections = studentSections.map(section => ({
        exerciseSectionId: section.exerciseSectionId,
        title: section.title,
        exercises: section.exercises || []
    }));

  

 

    const remainingAttempts = activeExercise
        ? Math.max(
            0,
            activeExercise.maxAttempts -
            activeExercise.attemptCount
        )
        : 0;

    

    
    const allExercises = useMemo(
        () =>
            exerciseSections.flatMap(
                section => section.exercises ?? []
            ),
        [exerciseSections]
    );

    const totalExercises =
        allExercises.length;

    const completedExercises = useMemo(
        () =>
            allExercises.filter(
                exercise =>
                    exercise.isCompleted === true ||
                    Number(exercise.attemptCount ?? 0) > 0
            ).length,
        [allExercises]
    );
    const loadStudentSections = async (skillValue) => {
        const requestId = sectionRequestRef.current + 1;
        sectionRequestRef.current = requestId;

        setStudentSections([]);
        setActiveExercise(null);


        const data = await getMyStudentExerciseSectionsApi(
            skillValue
        );

        if (requestId !== sectionRequestRef.current) {
            return;
        }

        setStudentSections(data);

        const firstExercise = data
            .flatMap(section => section.exercises || [])[0];

        setActiveExercise(firstExercise || null);
    };

    





    useEffect(() => {
        loadStudentSections(activeSkill);
    }, [activeSkill]);
    const handleChangeSkill = (skill) => {

        setActiveAttempt(null);
        setSelectedAnswers({});

        setActiveSkill(skill);
    };
    const handleStart = async (exerciseAssignmentId) => {
        try {
            setLoading(true);

            const data = await startExerciseAttemptApi(
                exerciseAssignmentId
            );

            setActiveAttempt(data);
            setSelectedAnswers({});

            setCurrentQuestionIndex(0);
            setMarkedQuestions({});
            setVisitedQuestions(
                data.questions?.length > 0
                    ? {
                        [getQuestionKey(data.questions[0])]: true
                    }
                    : {}
            );

        }
        catch (error) {
            alert(
                error.response?.data?.message ||
                "Unable to start the exercise"
            );
        }
        finally {
            setLoading(false);
        }
    };
    const handleUploadSpeakingAnswer = async (question) => {
        const key = getQuestionKey(question);
        const audioBlob = speakingAudios[key];

        if (!audioBlob) {
            alert("Please record your response before submitting the speaking answer.");
            return;
        }

        const formData = new FormData();

        formData.append(
            "exerciseAttemptId",
            activeAttempt.exerciseAttemptId
        );

        formData.append(
            "speakingTaskId",
            question.speakingTaskId
        );

        formData.append(
            "audioFile",
            audioBlob,
            `speaking-${question.speakingTaskId}.webm`
        );

        try {
            setLoading(true);

            const data = await submitSpeakingAnswerApi(formData);

            setSpeakingResults({
                ...speakingResults,
                [key]: data
            });

            setSelectedAnswers(previous => ({
                ...previous,
                [key]:
                    data?.audioAnswerUrl ??
                    data?.data?.audioAnswerUrl ??
                    "uploaded"
            }));

            alert("Speaking audio saved. The system will grade it when you submit.");
        }
        catch (error) {
            alert(
                error.response?.data?.message ||
                "Unable to submit the speaking audio"
            );
        }
        finally {
            setLoading(false);
        }
    };
    const handleSelectQuestionByIndex = (index) => {
        const question =
            activeAttempt.questions[index];

        if (!question) {
            return;
        }

        setCurrentQuestionIndex(index);

        window.setTimeout(() => {
            document
                .getElementById(`exercise-question-${index}`)
                ?.scrollIntoView({
                    behavior: "smooth",
                    block: "start"
                });
        }, 0);

        setVisitedQuestions({
            ...visitedQuestions,
            [getQuestionKey(question)]: true
        });
    };
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

    const getOptionId = (question, option) => {
        if (question.isListeningQuestion) {
            return option.listeningOptionId;
        }

        if (question.isReadingQuestion) {
            return option.readingOptionId;
        }

        return option.questionOptionId;
    };


    const handleMarkCurrentQuestion = () => {
        const question =
            activeAttempt.questions[currentQuestionIndex];

        if (!question) {
            return;
        }

        setMarkedQuestions({
            ...markedQuestions,
            [getQuestionKey(question)]:
                !markedQuestions[getQuestionKey(question)]
        });
    };


    const handleSubmit = async () => {
        if (!activeAttempt) {
            return;
        }

        const notAnswered =
            activeAttempt.questions.some(question =>
                !selectedAnswers[getQuestionKey(question)]
            );

        if (notAnswered) {
            if (!window.confirm(
                "You have not answered all questions. Submit anyway?"
            )) {
                return;
            }
        }

        try {
            setLoading(true);

            const answers = activeAttempt.questions.map(question => {
                const key = getQuestionKey(question);
                const value = selectedAnswers[key];

                // 1. Listening
                if (question.isListeningQuestion) {
                    return {
                        questionId: null,
                        listeningQuestionId: question.listeningQuestionId,
                        selectedQuestionOptionId: null,
                        selectedListeningOptionId: value || null,
                        readingQuestionId: null,
                        selectedReadingOptionId: null,
                        writingTaskId: null,
                        speakingTaskId: null,
                        textAnswer: null,
                        audioAnswerUrl: null
                    };
                }

                // 2. Reading
                if (question.isReadingQuestion) {
                    return {
                        questionId: null,
                        listeningQuestionId: null,
                        selectedQuestionOptionId: null,
                        selectedListeningOptionId: null,
                        readingQuestionId: question.readingQuestionId,
                        selectedReadingOptionId: value || null,
                        writingTaskId: null,
                        speakingTaskId: null,
                        textAnswer: null,
                        audioAnswerUrl: null
                    };
                }

                // 3. Writing
                if (question.isWritingQuestion) {
                    return {
                        questionId: null,
                        listeningQuestionId: null,
                        selectedQuestionOptionId: null,
                        selectedListeningOptionId: null,
                        readingQuestionId: null,
                        selectedReadingOptionId: null,
                        writingTaskId: question.writingTaskId,
                        speakingTaskId: null,
                        textAnswer: value || "",
                        audioAnswerUrl: null
                    };
                }

                // 4. Speaking
                if (question.isSpeakingQuestion) {
                    return {
                        questionId: null,
                        listeningQuestionId: null,
                        selectedQuestionOptionId: null,
                        selectedListeningOptionId: null,
                        readingQuestionId: null,
                        selectedReadingOptionId: null,
                        writingTaskId: null,
                        speakingTaskId: question.speakingTaskId,
                        textAnswer: null,
                        audioAnswerUrl: value || ""
                    };
                }

                // 5. Normal Question / Grammar
                return {
                    questionId: question.questionId,
                    listeningQuestionId: null,
                    selectedQuestionOptionId:
                        question.questionType === 1 ||
                            question.questionType === 2
                            ? value || null
                            : null,
                    selectedListeningOptionId: null,
                    readingQuestionId: null,
                    selectedReadingOptionId: null,
                    writingTaskId: null,
                    speakingTaskId: null,
                    textAnswer:
                        question.questionType === 3
                            ? value || ""
                            : null,
                    audioAnswerUrl:
                        question.questionType === 4
                            ? value || ""
                            : null
                };
            });

            await submitExerciseAttemptApi(
                activeAttempt.exerciseAttemptId,
                answers
            );

            const fullResult =
                await getExerciseAttemptResultApi(
                    activeAttempt.exerciseAttemptId
                );

            localStorage.setItem(
                "student-exercise-result",
                JSON.stringify(fullResult)
            );

            setActiveAttempt(null);
            setSelectedAnswers({});

            await loadStudentSections(activeSkill);
          

            navigate("/student/exercises/result");
        }
        catch (error) {
            alert(
                error.response?.data?.message ||
                "Unable to submit the exercise"
            );
        }
        finally {
            setLoading(false);
        }
    };


    if (activeAttempt) {
        const QUESTIONS_PER_PAGE = activeSkill === 4 ? 1 : 5;

        const currentPage =
            Math.floor(currentQuestionIndex / QUESTIONS_PER_PAGE);

        const startIndex =
            currentPage * QUESTIONS_PER_PAGE;

        const currentPageQuestions =
            activeAttempt.questions.slice(
                startIndex,
                startIndex + QUESTIONS_PER_PAGE
            );

        const totalPages =
            Math.ceil(
                activeAttempt.questions.length / QUESTIONS_PER_PAGE
            );

        return (
            <>
                <ExerciseSkillNavbar
                    activeSkill={activeSkill}
                    onChange={handleChangeSkill}
                    totalExercises={totalExercises}
                    completedExercises={completedExercises}
                />

                <div
                    style={{
                        display: "grid",
                        gridTemplateColumns: "1fr 330px",
                        gap: "24px",
                        padding: "40px"
                    }}
                >
                    <main>


                        {currentPageQuestions.map((question, index) => {
                            const realIndex = startIndex + index;

                            const previousQuestion =
                                activeAttempt.questions[realIndex - 1];

                            const shouldShowListeningContext =
                                question.isListeningQuestion &&
                                (index === 0 ||
                                    question.listeningTaskId !==
                                        previousQuestion?.listeningTaskId);
                            const shouldShowListeningImage =
                                question.isListeningQuestion &&
                                question.listeningPromptImageUrl &&
                                    question.listeningPromptImageUrl !==
                                    previousQuestion?.listeningPromptImageUrl;
                            const listeningQuestionNumber =
                                question.isListeningQuestion
                                    ? activeAttempt.questions
                                        .filter(item =>
                                            item.isListeningQuestion)
                                        .findIndex(item =>
                                            item.listeningQuestionId ===
                                            question.listeningQuestionId) + 1
                                    : null;
                            const toeicListeningContext =
                                (Number(question.listeningCourseType) === 1 ||
                                    /\btoeic\b/i.test(
                                        activeAttempt.exerciseTitle || ""))
                                    ? getToeicListeningPartContext(
                                        listeningQuestionNumber)
                                    : null;
                            const listeningPartTitle =
                                toeicListeningContext?.title ||
                                question.listeningPartTitle;
                            const listeningDirection =
                                toeicListeningContext?.direction ||
                                question.listeningDirection;
                            const shouldShowReadingPassage =
                                question.isReadingQuestion &&
                                question.readingPassageText;
                            const readingTaskQuestions =
                                question.isReadingQuestion
                                    ? activeAttempt.questions.filter(
                                        item =>
                                            item.isReadingQuestion &&
                                            item.readingTaskId ===
                                                question.readingTaskId
                                    )
                                    : [];
                            const readingQuestionNumbers =
                                readingTaskQuestions
                                    .map(item =>
                                        item.readingQuestionOrderIndex)
                                    .filter(Number.isFinite);
                            const readingQuestionRange =
                                readingQuestionNumbers.length > 0
                                    ? `${Math.min(...readingQuestionNumbers)}-${Math.max(...readingQuestionNumbers)}`
                                    : null;
                            const isToeicClozeQuestion =
                                isToeicReadingClozeQuestion(question);
                            const isToeicReadingTask =
                                /^TOEIC\s+Reading\s+Part\s+[5-7]\b/i.test(
                                    question.readingPassageTitle || ""
                                );
                            const toeicReadingTitle =
                                splitToeicReadingPassageTitle(
                                    question.readingPassageTitle
                                );
                            const vstepReadingDisplay =
                                getVstepReadingDisplayContent({
                                    passageTitle:
                                        question.readingPassageTitle,
                                    passageText:
                                        question.readingPassageText,
                                    instruction:
                                        question.readingInstruction
                                });
                            const displayReadingPassageTitle =
                                vstepReadingDisplay?.passageTitle ||
                                question.readingPassageTitle;
                            const displayReadingPassageText =
                                vstepReadingDisplay?.passageText ||
                                question.readingPassageText;
                            const writingEmailPrompt =
                                splitWritingEmailPromptText(
                                    question.writingPromptText
                                );
                            const toeicWrittenRequest =
                                getToeicWrittenRequestDisplay(
                                    question,
                                    realIndex + 1
                                );
                            const isEmailReplyTask =
                                question.isWritingQuestion &&
                                !toeicWrittenRequest &&
                                getWritingTaskDisplayTitle(
                                    question,
                                    realIndex + 1
                                ) === "Email / Reply Task";
                            const isVstepEssayTask =
                                question.isWritingQuestion &&
                                question.writingTaskType === "Essay" &&
                                !/\btoeic\b/i.test(
                                    activeAttempt.exerciseTitle || ""
                                );
                            const isStandaloneVstepWritingTask =
                                isEmailReplyTask || isVstepEssayTask;
                            const isToeicPictureWriting =
                                isToeicPictureWritingQuestion(
                                    question,
                                    realIndex + 1
                                );
                            const isVstepSpeaking =
                                question.isSpeakingQuestion &&
                                isVstepSpeakingTask(question);
                            const isToeicSpeaking =
                                question.isSpeakingQuestion &&
                                /\btoeic\b/i.test(
                                    activeAttempt.exerciseTitle || ""
                                );
                            const toeicSpeakingGroup = isToeicSpeaking
                                ? getToeicSpeakingGroup(question)
                                : null;
                            const answerKey = getQuestionKey(question);

                            return (
                                <div
                                    id={`exercise-question-${realIndex}`}
                                    key={getQuestionKey(question)}
                                >
                                    {shouldShowListeningContext && (
                                        <div className="stt-context-box">
                                            {listeningPartTitle && (
                                                <h2 className="stt-context-title">
                                                    {formatListeningPartTitle(
                                                        listeningPartTitle
                                                    )}
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

                                                    <audio
                                                        controls
                                                        src={question.audioUrl}
                                                        className="stt-audio"
                                                    />
                                                </>
                                            )}

                                            {question.transcript && (
                                                <details className="stt-transcript">
                                                    <summary>Transcript</summary>
                                                    <p>{question.transcript}</p>
                                                </details>
                                            )}
                                        </div>
                                    )}
                                    {shouldShowReadingPassage && (
                                        <div
                                            style={{
                                                background: "white",
                                                border: "1px solid #e5e7eb",
                                                borderRadius: "16px",
                                                padding: "24px",
                                                marginBottom: "20px"
                                            }}
                                        >
                                            {readingQuestionRange &&
                                                !isToeicReadingTask && (
                                                <p
                                                    style={{
                                                        margin: "0 0 12px",
                                                        fontWeight: 600,
                                                        color: "#374151"
                                                    }}
                                                >
                                                    Questions {readingQuestionRange}
                                                </p>
                                            )}

                                            {isToeicReadingTask &&
                                                !isToeicReadingRangeTitle(
                                                    question.readingPassageTitle
                                                ) && (
                                                <h2
                                                    className="student-reading-passage-title"
                                                    style={{
                                                        margin: "0 0 16px",
                                                        fontSize: "34px",
                                                        fontWeight: 700
                                                    }}
                                                >
                                                    {toeicReadingTitle?.partTitle ||
                                                        question.readingPassageTitle ||
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
                                                <p
                                                    style={{
                                                        color: "#6b7280",
                                                        marginBottom: "16px"
                                                    }}
                                                >
                                                    {question.readingInstruction}
                                                </p>
                                            )}

                                            {!isToeicReadingTask &&
                                                !isToeicReadingRangeTitle(
                                                question.readingPassageTitle
                                            ) && (
                                                <h2
                                                    className="student-reading-passage-title"
                                                    style={{
                                                        margin: "0 0 16px",
                                                        fontSize: "34px",
                                                        fontWeight: 700
                                                    }}
                                                >
                                                    {displayReadingPassageTitle ||
                                                        "Reading Passage"}
                                                </h2>
                                            )}

                                            {!toeicReadingTitle &&
                                                !isToeicReadingPartLabel(
                                                displayReadingPassageText
                                            ) && (
                                                <div
                                                    style={{
                                                        whiteSpace: "pre-line",
                                                        lineHeight: "1.8",
                                                        fontSize: "17px",
                                                        color: "#111827"
                                                    }}
                                                >
                                                    {formatReadingPassageText(
                                                        displayReadingPassageText
                                                    )}
                                                </div>
                                            )}
                                        </div>
                                    )}

                                    {shouldShowReadingPassage &&
                                        toeicReadingTitle && (
                                        <div
                                            style={{
                                                background: "white",
                                                border: "1px solid #e5e7eb",
                                                borderRadius: "16px",
                                                padding: "24px",
                                                marginBottom: "20px"
                                            }}
                                        >
                                            <p
                                                style={{
                                                    margin: "0 0 16px",
                                                    fontSize: "18px",
                                                    fontWeight: 700
                                                }}
                                            >
                                                {toeicReadingTitle.groupTitle}
                                            </p>
                                            <div
                                                style={{
                                                    whiteSpace: "pre-line",
                                                    lineHeight: "1.8",
                                                    fontSize: "17px",
                                                    color: "#111827"
                                                }}
                                            >
                                                {formatReadingPassageText(
                                                    question.readingPassageText
                                                )}
                                            </div>
                                        </div>
                                    )}

                                    {isToeicPictureWriting &&
                                        shouldShowWritingInstruction(
                                            question,
                                            realIndex + 1
                                        ) && (
                                        <div
                                            style={{
                                                background: "white",
                                                border: "1px solid #e5e7eb",
                                                borderRadius: "16px",
                                                padding: "24px",
                                                marginBottom: "18px"
                                            }}
                                        >
                                            <h3>
                                                {getWritingTaskDisplayTitle(
                                                    question,
                                                    realIndex + 1
                                                )}
                                            </h3>
                                            {question.writingInstruction && (
                                                <div className="stt-direction-box">
                                                    <strong>Directions:</strong>{" "}
                                                    {question.writingInstruction}
                                                </div>
                                            )}
                                        </div>
                                    )}

                                    <div

                                        style={{
                                            background: "white",
                                            border: "1px solid #e5e7eb",
                                            borderRadius: "16px",
                                            padding: "24px",
                                            marginBottom: "18px"
                                        }}
                                    >
                                        {isStandaloneVstepWritingTask ? (
                                            <>
                                                <h3>
                                                    {getWritingTaskDisplayTitle(
                                                        question,
                                                        realIndex + 1
                                                    )}
                                                </h3>
                                            </>
                                        ) : !isVstepSpeaking &&
                                            !isToeicClozeQuestion &&
                                            !isToeicPictureWriting &&
                                            !toeicWrittenRequest && (
                                            <h3>
                                                Question {realIndex + 1}: {question.isListeningQuestion
                                                    ? getListeningQuestionDisplayContent(
                                                        question.content,
                                                        realIndex + 1
                                                    )
                                                    : question.content}
                                            </h3>
                                        )}
                                        {shouldShowListeningImage && (
                                            <img
                                                src={question.listeningPromptImageUrl}
                                                alt={`Listening question ${realIndex + 1}`}
                                                style={{
                                                    display: "block",
                                                    width: "100%",
                                                    maxWidth: "760px",
                                                    maxHeight: "520px",
                                                    objectFit: "contain",
                                                    marginBottom: "18px",
                                                    borderRadius: "10px",
                                                    border: "1px solid #e5e7eb"
                                                }}
                                            />
                                        )}
                                        {question.isWritingQuestion && toeicWrittenRequest &&
                                            toeicWrittenRequest.questionNumber === 6 && (
                                            <div
                                                style={{
                                                    background: "#f8fafc",
                                                    border: "1px solid #e5e7eb",
                                                    borderRadius: "14px",
                                                    padding: "20px",
                                                    marginTop: "14px",
                                                    marginBottom: "18px"
                                                }}
                                            >
                                                <h3>
                                                    {toeicWrittenRequest.sectionTitle}
                                                </h3>
                                                <div className="stt-direction-box">
                                                    <strong>Directions:</strong>{" "}
                                                    {toeicWrittenRequest.direction}
                                                </div>
                                            </div>
                                        )}
                                        {question.isWritingQuestion && (
                                            <div
                                                style={{
                                                    background: "#f8fafc",
                                                    border: "1px solid #e5e7eb",
                                                    borderRadius: "14px",
                                                    padding: "20px",
                                                    marginTop: "14px",
                                                    marginBottom: "18px"
                                                }}
                                            >
                                                {!isStandaloneVstepWritingTask &&
                                                    !isToeicPictureWriting &&
                                                    !toeicWrittenRequest && (
                                                    <>
                                                        <h3>
                                                            {getWritingTaskDisplayTitle(
                                                                question,
                                                                realIndex + 1
                                                            )}
                                                        </h3>
                                                        {question.writingInstruction &&
                                                            shouldShowWritingInstruction(
                                                                question,
                                                                realIndex + 1
                                                            ) && (
                                                            <div style={{ marginTop: "14px" }}>
                                                                <strong>Instruction</strong>
                                                                <p
                                                                    style={{
                                                                        whiteSpace: "pre-line",
                                                                        lineHeight: "1.7",
                                                                        color: "#374151"
                                                                    }}
                                                                >
                                                                    {question.writingInstruction}
                                                                </p>
                                                            </div>
                                                        )}
                                                    </>
                                                )}

                                                {isToeicPictureWriting && (
                                                    <h3>
                                                        Question {realIndex + 1}:{" "}
                                                        {getToeicPictureWritingPromptTitle(
                                                            question,
                                                            realIndex + 1
                                                        )}
                                                    </h3>
                                                )}

                                                {toeicWrittenRequest && (
                                                    <>
                                                        <h3
                                                            style={{
                                                                fontSize: "18px",
                                                                fontWeight: 700
                                                            }}
                                                        >
                                                            Question {toeicWrittenRequest.questionNumber}:{" "}
                                                            {toeicWrittenRequest.prompt}
                                                        </h3>
                                                        <div className="writing-email-box">
                                                            {toeicWrittenRequest.sourceText}
                                                        </div>
                                                    </>
                                                )}

                                                {!isStandaloneVstepWritingTask &&
                                                    !toeicWrittenRequest &&
                                                    question.writingReferenceText && (
                                                    <div style={{ marginTop: "14px" }}>
                                                        <strong>Topic / Situation</strong>
                                                        <p
                                                            style={{
                                                                whiteSpace: "pre-line",
                                                                lineHeight: "1.7",
                                                                color: "#374151"
                                                            }}
                                                        >
                                                            {question.writingReferenceText}
                                                        </p>
                                                    </div>
                                                )}

                                                {!toeicWrittenRequest &&
                                                    question.writingPromptImageUrl && (
                                                    <div style={{ marginTop: "14px" }}>
                                                        <img
                                                            src={question.writingPromptImageUrl}
                                                            alt={question.content || "Writing prompt"}
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

                                                {!toeicWrittenRequest &&
                                                    question.writingSourceText && (
                                                    <div style={{ marginTop: "14px" }}>
                                                        <strong>
                                                            {question.writingTaskType === "Essay"
                                                                ? "Writing Issue"
                                                                : "Email from Alex"}
                                                        </strong>

                                                        <div
                                                            className={
                                                                question.writingTaskType === "Essay"
                                                                    ? "writing-source-box"
                                                                    : "writing-email-box"
                                                            }
                                                        >
                                                            {question.writingSourceText}
                                                        </div>
                                                    </div>
                                                )}

                                                {!isToeicPictureWriting &&
                                                    !toeicWrittenRequest &&
                                                    question.writingPromptText && (
                                                    <div style={{ marginTop: "14px" }}>
                                                        {writingEmailPrompt ? (
                                                            <>
                                                                <p
                                                                    style={{
                                                                        whiteSpace: "pre-line",
                                                                        lineHeight: "1.7",
                                                                        color: "#111827"
                                                                    }}
                                                                >
                                                                    {writingEmailPrompt.beforeEmail}
                                                                </p>
                                                                <div className="writing-email-box">
                                                                    {writingEmailPrompt.emailContent}
                                                                </div>
                                                                <p
                                                                    style={{
                                                                        whiteSpace: "pre-line",
                                                                        lineHeight: "1.7",
                                                                        color: "#111827"
                                                                    }}
                                                                >
                                                                    {writingEmailPrompt.afterEmail}
                                                                </p>
                                                            </>
                                                        ) : (
                                                            <p
                                                                style={{
                                                                    whiteSpace: "pre-line",
                                                                    lineHeight: "1.7",
                                                                    color: "#111827"
                                                                }}
                                                            >
                                                                {question.writingPromptText}
                                                            </p>
                                                        )}
                                                    </div>
                                                )}

                                                {!toeicWrittenRequest &&
                                                    question.writingRequirements && (
                                                    <div style={{ marginTop: "14px" }}>
                                                        <strong>Requirements</strong>
                                                        <p
                                                            style={{
                                                                whiteSpace: "pre-line",
                                                                lineHeight: "1.7",
                                                                color: "#374151"
                                                            }}
                                                        >
                                                            {question.writingRequirements}
                                                        </p>
                                                    </div>
                                                )}

                                                {!isToeicPictureWriting &&
                                                    !toeicWrittenRequest && (
                                                    <div style={{ marginTop: "14px" }}>
                                                        <strong>Minimum words:</strong>{" "}
                                                        {question.writingMinimumWords || 120}
                                                    </div>
                                                )}

                                                {question.writingScoringCriteria && (
                                                    <div style={{ marginTop: "8px" }}>
                                                        <strong>Scoring criteria:</strong>{" "}
                                                        {question.writingScoringCriteria}
                                                    </div>
                                                )}
                                            </div>
                                        )}
                                        {(question.questionType === 1 ||
                                            question.questionType === 2) &&
                                            question.options.map((option, optionIndex) => {
                                                const optionLabel = String.fromCharCode(65 + optionIndex);

                                                return (
                                                    <label
                                                        key={getOptionId(question, option)}
                                                        style={{
                                                            display: "block",
                                                            marginTop: "14px",
                                                            fontSize: "17px"
                                                        }}
                                                    >
                                                        <input
                                                            type="radio"
                                                            name={getQuestionKey(question)}
                                                            checked={
                                                                selectedAnswers[answerKey] ===
                                                                getOptionId(question, option)
                                                            }
                                                            onChange={() =>
                                                                setSelectedAnswers({
                                                                    ...selectedAnswers,
                                                                    [answerKey]: getOptionId(question, option)
                                                                })
                                                            }
                                                        />

                                                        {" "}
                                                        <strong>{optionLabel}.</strong>{" "}
                                                        {option.content}
                                                    </label>
                                                );
                                            })}
                                        {question.questionType === 3 && (
                                            <textarea
                                                placeholder={
                                                    question.isWritingQuestion
                                                        ? "Write your response here..."
                                                        : "Enter your answer"
                                                }
                                                value={selectedAnswers[answerKey] || ""}
                                                onChange={e =>
                                                    setSelectedAnswers({
                                                        ...selectedAnswers,
                                                        [answerKey]: e.target.value
                                                    })
                                                }
                                                style={{
                                                    width: "100%",
                                                    minHeight: question.isWritingQuestion ? "360px" : "160px",
                                                    marginTop: "14px",
                                                    padding: "12px",
                                                    borderRadius: "12px",
                                                    border: "1px solid #d1d5db"
                                                }}
                                            />
                                        )}

                                        {question.questionType === 4 &&
                                            question.isSpeakingQuestion && (
                                                <div style={{ marginTop: "18px" }}>
                                                    {toeicSpeakingGroup &&
                                                        realIndex + 1 ===
                                                            toeicSpeakingGroup.startQuestion && (
                                                        <div
                                                            style={{
                                                                background: "#f8fafc",
                                                                border: "1px solid #e5e7eb",
                                                                borderRadius: "14px",
                                                                padding: "20px",
                                                                marginBottom: "18px"
                                                            }}
                                                        >
                                                            <h3>
                                                                {toeicSpeakingGroup.title}
                                                            </h3>
                                                            <div className="stt-direction-box">
                                                                <strong>Directions:</strong>{" "}
                                                                {toeicSpeakingGroup.direction}
                                                            </div>
                                                        </div>
                                                    )}
                                                    {isVstepSpeaking && (
                                                        <>
                                                            <h3>Speaking Task</h3>
                                                            <p>
                                                                {question.speakingInstruction}
                                                                {getVstepDuration(
                                                                    question.speakingPromptText
                                                                ) &&
                                                                    ` ${getVstepDuration(
                                                                        question.speakingPromptText
                                                                    )}`}
                                                            </p>
                                                        </>
                                                    )}
                                                    <div
                                                        style={{
                                                            background: "#f8fafc",
                                                            border: "1px solid #e5e7eb",
                                                            borderRadius: "14px",
                                                            padding: "20px",
                                                            marginBottom: "18px"
                                                        }}
                                                    >
                                                        {isToeicSpeaking && (
                                                            <h3>
                                                                Question {realIndex + 1}
                                                            </h3>
                                                        )}
                                                        <SpeakingTaskContent
                                                            question={question}
                                                            hideVstepHeading={
                                                                isVstepSpeaking
                                                            }
                                                            hideToeicGroupInstruction={Boolean(
                                                                toeicSpeakingGroup
                                                            )}
                                                            showPromptBeforeImage={
                                                                isToeicSpeaking &&
                                                                [3, 4].includes(realIndex + 1)
                                                            }
                                                            promptTextOverride={
                                                                toeicSpeakingGroup?.promptText
                                                            }
                                                        />
                                                    </div>
                                                    {question.speakingPromptText && (
                                                        <details style={{ marginBottom: "18px" }}>
                                                            <summary>Extracted test content</summary>
                                                            <p style={{ whiteSpace: "pre-wrap" }}>
                                                                {question.speakingPromptText}
                                                            </p>
                                                        </details>
                                                    )}

                                                    <SpeakingRecorder
                                                        maxSeconds={
                                                            question.speakingTimeSeconds || 120
                                                        }
                                                        storageKey={
                                                            `speaking-${activeAttempt.exerciseAttemptId}-${question.speakingTaskId}`
                                                        }
                                                        onRecorded={(blob) => {
                                                            const key = getQuestionKey(question);

                                                            setSpeakingAudios({
                                                                ...speakingAudios,
                                                                [key]: blob
                                                            });
                                                        }}
                                                    />

                                                    <button
                                                        type="button"
                                                        className="lesson-btn"
                                                        disabled={loading}
                                                        onClick={() =>
                                                            handleUploadSpeakingAnswer(question)
                                                        }
                                                        style={{ marginTop: "16px" }}
                                                    >
                                                        {loading
                                                            ? "Grading..."
                                                            : "Submit audio"}
                                                    </button>

                                                    {speakingResults[answerKey] && (
                                                        <div
                                                            style={{
                                                                marginTop: "18px",
                                                                background: "#ecfdf5",
                                                                border: "1px solid #86efac",
                                                                borderRadius: "16px",
                                                                padding: "18px"
                                                            }}
                                                        >
                                                            <h3>Speaking Result</h3>

                                                            <p>
                                                                Score:{" "}
                                                                <strong>
                                                                    {speakingResults[answerKey].pointEarned}
                                                                </strong>
                                                            </p>

                                                            <p>
                                                                Transcript:
                                                            </p>

                                                            <p style={{ whiteSpace: "pre-wrap" }}>
                                                                {speakingResults[answerKey].textAnswer}
                                                            </p>

                                                            <p style={{ whiteSpace: "pre-wrap" }}>
                                                                {speakingResults[answerKey].teacherFeedback}
                                                            </p>
                                                        </div>
                                                    )}
                                                </div>
                                            )}

                                        {question.questionType === 4 &&
                                            !question.isSpeakingQuestion && (
                                                <input
                                                    placeholder="Paste the answer audio link"
                                                    value={selectedAnswers[answerKey] || ""}
                                                    onChange={e =>
                                                        setSelectedAnswers({
                                                            ...selectedAnswers,
                                                            [answerKey]: e.target.value
                                                        })
                                                    }
                                                    style={{
                                                        width: "100%",
                                                        marginTop: "14px",
                                                        padding: "12px",
                                                        borderRadius: "12px",
                                                        border: "1px solid #d1d5db"
                                                    }}
                                                />
                                            )}
                                    </div>
                                </div>
                            );
                        })}

                        <div
                            style={{
                                display: "flex",
                                gap: "12px",
                                marginTop: "20px"
                            }}
                        >
                            <button
                                onClick={() =>
                                    handleSelectQuestionByIndex(
                                        Math.max(
                                            0,
                                            startIndex - QUESTIONS_PER_PAGE
                                        )
                                    )
                                }
                                disabled={currentPage === 0}
                                className="lesson-btn secondary"
                            >
                                Previous Page
                            </button>

                            <button
                                onClick={() =>
                                    handleSelectQuestionByIndex(
                                        Math.min(
                                            activeAttempt.questions.length - 1,
                                            startIndex + QUESTIONS_PER_PAGE
                                        )
                                    )
                                }
                                disabled={currentPage >= totalPages - 1}
                                className="lesson-btn secondary"
                            >
                                Next Page
                            </button>


                        </div>
                    </main>

                    <QuestionPalette
                        questions={activeAttempt.questions}
                        currentIndex={currentQuestionIndex}
                        selectedAnswers={selectedAnswers}
                        markedQuestions={markedQuestions}
                        visitedQuestions={visitedQuestions}
                        loading={loading}
                        onSelectQuestion={handleSelectQuestionByIndex}
                        onMarkQuestion={handleMarkCurrentQuestion}
                        onSubmit={handleSubmit}
                    />
                </div>
            </>
        );
    }

    return (
        <div className="student-exercise-page">
            <ExerciseSkillNavbar
                activeSkill={activeSkill}
                onChange={handleChangeSkill}
                totalExercises={totalExercises}
                completedExercises={completedExercises}
            />

            <div
                className={
                    isSidebarOpen
                        ? "lesson-student-layout"
                        : "lesson-student-layout sidebar-collapsed"
                }
            >
                <ExerciseSidebar
                    sections={exerciseSections}
                    activeExercise={activeExercise}
                    setActiveExercise={setActiveExercise}
                    isSidebarOpen={isSidebarOpen}
                    setIsSidebarOpen={setIsSidebarOpen}
                />

                <main className="student-exercise-main">
                    {!activeExercise ? (
                        <div className="student-exercise-empty-state">
                            <div className="student-exercise-empty-icon">
                                <BookOpenCheck size={36} />
                            </div>

                            <span className="student-exercise-empty-eyebrow">
                                Practice Content
                            </span>

                            <h2>No Exercises Yet</h2>

                            <p>
                                There is no content for this skill yet.
                                Select another skill or exercise section
                                to continue practicing.
                            </p>
                        </div>
                    ) : (
                        <div className="student-exercise-content">
                            

                            <section className="student-exercise-overview-card">
                                <div className="student-exercise-overview-header">
                                    <div className="student-exercise-overview-title">
                                        <div className="student-exercise-overview-icon">
                                            <BarChart3 size={21} />
                                        </div>

                                        <div>
                                                <span>Exercise</span>
                                                <h2>{activeExercise.title}</h2>
                                        </div>
                                    </div>

                                    <span className="student-exercise-overview-badge">
                                        {remainingAttempts} attempts remaining
                                    </span>
                                </div>

                                

                                <div className="student-exercise-stat-grid">
                                    <article className="student-exercise-stat-card">
                                        <div className="student-exercise-stat-icon questions">
                                            <FileQuestion size={22} />
                                        </div>

                                        <div>
                                            <span>Questions</span>
                                            <strong>
                                                {activeExercise.totalQuestions}
                                            </strong>
                                            <small>questions</small>
                                        </div>
                                    </article>

                                    <article className="student-exercise-stat-card">
                                        <div className="student-exercise-stat-icon points">
                                            <Trophy size={22} />
                                        </div>

                                        <div>
                                            <span>Total points</span>
                                            <strong>
                                                {activeExercise.totalPoints}
                                            </strong>
                                            <small>points</small>
                                        </div>
                                    </article>

                                   

                                    <article className="student-exercise-stat-card">
                                        <div className="student-exercise-stat-icon attempts">
                                            <RotateCcw size={22} />
                                        </div>

                                        <div>
                                            <span>Attempts made</span>
                                            <strong>
                                                {activeExercise.attemptCount}
                                            </strong>
                                            <small>attempts</small>
                                        </div>
                                    </article>
                                </div>
                            </section>

                           

                            <section className="student-exercise-history-card">
                                <div className="student-exercise-card-heading">
                                    <div className="student-exercise-heading-with-icon">
                                        <div className="student-exercise-section-icon">
                                            <History size={21} />
                                        </div>

                                        <div>
                                            <span>Activity History</span>
                                            <h2>Recent Attempts</h2>
                                        </div>
                                    </div>

                                    <strong>
                                        {activeExercise.attemptHistory?.length || 0} attempts
                                    </strong>
                                </div>

                                <div className="student-exercise-history-table-wrap">
                                    <table className="exercise-history-table">
                                        <thead>
                                            <tr>
                                                <th>Attempt</th>
                                                <th>Date</th>
                                                <th>Time</th>
                                                <th>Score</th>
                                                <th>Details</th>
                                            </tr>
                                        </thead>

                                        <tbody>
                                            {activeExercise.attemptHistory?.length > 0 ? (
                                                activeExercise.attemptHistory.map(
                                                    (item, index) => (
                                                        <tr
                                                            key={
                                                                item.exerciseAttemptId ||
                                                                index
                                                            }
                                                        >
                                                            <td>
                                                                <span className="student-exercise-attempt-number">
                                                                    #{index + 1}
                                                                </span>
                                                            </td>

                                                            <td>
                                                                <span className="student-exercise-date-cell">
                                                                    <CalendarDays size={16} />

                                                                    {formatVietnamDateOnly(
                                                                        item.submittedAt
                                                                    )}
                                                                </span>
                                                            </td>

                                                            <td>
                                                                <span className="student-exercise-date-cell">
                                                                    <Clock3 size={16} />

                                                                    {formatVietnamHourMinute(
                                                                        item.submittedAt
                                                                    )}
                                                                </span>
                                                            </td>

                                                            <td className="score">
                                                                {Number.isInteger(
                                                                    item.score
                                                                )
                                                                    ? item.score
                                                                    : item.score.toFixed(
                                                                        1
                                                                    )}{" "}
                                                                points
                                                            </td>

                                                            <td>
                                                                <button
                                                                    type="button"
                                                                    className="student-exercise-detail-btn"
                                                                    onClick={async () => {
                                                                        try {
                                                                            const data =
                                                                                await getExerciseAttemptResultApi(
                                                                                    item.exerciseAttemptId
                                                                                );

                                                                            localStorage.setItem(
                                                                                "student-exercise-result",
                                                                                JSON.stringify(
                                                                                    data
                                                                                )
                                                                            );

                                                                            navigate(
                                                                                "/student/exercises/result"
                                                                            );
                                                                        }
                                                                        catch (error) {
                                                                            alert(
                                                                                error.response?.data?.message ||
                                                                                "Unable to load the attempt result"
                                                                            );
                                                                        }
                                                                    }}
                                                                >
                                                                    View details
                                                                </button>
                                                            </td>
                                                        </tr>
                                                    )
                                                )
                                            ) : (
                                                <tr>
                                                    <td
                                                        colSpan="5"
                                                        className="empty-history"
                                                    >
                                                        No attempt history yet
                                                    </td>
                                                </tr>
                                            )}
                                        </tbody>
                                    </table>
                                </div>
                            </section>

                            <footer className="student-exercise-footer">
                                <div className="student-exercise-footer-info">
                                    <div className="student-exercise-footer-icon">
                                        <CheckCircle2 size={21} />
                                    </div>

                                    <div>
                                        <strong>
                                            {activeExercise.canStart
                                                ? "Ready to begin?"
                                                : "This exercise is currently unavailable"}
                                        </strong>

                                        <span>
                                            {remainingAttempts} attempts remaining
                                        </span>
                                    </div>
                                </div>

                                <button
                                    type="button"
                                    disabled={
                                        !activeExercise.canStart ||
                                        loading
                                    }
                                    onClick={() =>
                                        handleStart(
                                            activeExercise.exerciseAssignmentId
                                        )
                                    }
                                    className="exercise-start-btn"
                                >
                                    {loading ? (
                                        <LoaderCircle
                                            size={20}
                                            className="student-exercise-spin"
                                        />
                                    ) : activeExercise.attemptCount > 0 ? (
                                        <RotateCcw size={20} />
                                    ) : (
                                        <PlayCircle size={20} />
                                    )}

                                    <span>
                                        {loading
                                            ? "Preparing..."
                                            : activeExercise.canStart
                                                ? activeExercise.attemptCount > 0
                                                    ? "Retry Exercise"
                                                    : "Start Exercise"
                                                : "Unavailable"}
                                    </span>

                                    {!loading &&
                                        activeExercise.canStart && (
                                            <span>→</span>
                                        )}
                                </button>
                            </footer>
                        </div>
                    )}
                </main>
            </div>
        </div>
    );

}

export default ExercisePage;
