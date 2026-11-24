import {
    useEffect,
    useMemo,
    useState
} from "react";
import {
    Space,
    Table,
    Tag,
    Tooltip
} from "antd";
import {
    formatVietnamDateOnly,
    formatVietnamHourMinute,
    parseUtcDate
} from "../../../../utils/dateTimeUtils";
import {
    FileText,
    Medal,
    Pencil,
    Plus,
    Search,
    Trash2,
    Trophy,
    XCircle
} from "lucide-react";
import {
    useNavigate
} from "react-router-dom";
import TeacherSidebar
    from "../../../lesson/teacher/TeacherSidebar";

import {
    getTeacherCompetitionTestsApi,
    importCompetitionTestApi,
    replaceCompetitionTestApi,
    getCompetitionImportJobApi,
    getCompetitionSourceFileApi
} from "../api/competitionTestApi";

import {
    cancelTeacherCompetitionApi,
    createCompetitionAssignmentApi,
    getCompetitionAssignmentOptionsApi,
    updateTeacherCompetitionApi,
    hardDeleteTeacherCompetitionApi,
    getTeacherCompetitionsApi
} from "../api/competitionAssignmentApi";

import "../../../lesson/styles/lesson.css";
import "../../../exercise/teacher/styles/questionBank.css";
import "../styles/teacherCompetition.css";

function TeacherCompetitionPage() {
    const COURSE_TYPES = {
        TOEIC: 1,
        VSTEP: 2,
        Communication: 3
    };

    const TEST_TYPES = {
        VstepFourSkills: 1,
        ToeicTwoSkills: 2,
        ToeicFourSkills: 3
    };

    const TOEIC_LISTENING_DEFAULT_QUESTION_COUNTS =
        ["6", "25", "39", "30"];
    const navigate =
        useNavigate();
    const [competitions, setCompetitions] =
        useState([]);

    const [competitionTests, setCompetitionTests] =
        useState([]);

    const [options, setOptions] =
        useState({
            competitionTests: [],
            classes: [],
            serverTime: null
        });

    const [loading, setLoading] =
        useState(true);

    const [submitting, setSubmitting] =
        useState(false);

    const [searchText, setSearchText] =
        useState("");

    const [activeStatus, setActiveStatus] =
        useState("all");

    const [showCreateModal, setShowCreateModal] =
        useState(false);


    const [showEditModal, setShowEditModal] =
        useState(false);

    const [editingCompetition, setEditingCompetition] =
        useState(null);

    const [editTitle, setEditTitle] =
        useState("");

    const [editDescription, setEditDescription] =
        useState("");

    const [editStartsAt, setEditStartsAt] =
        useState("");

    const [editDeadlineAt, setEditDeadlineAt] =
        useState("");

    const [editDurationMinutes, setEditDurationMinutes] =
        useState(120);
    const [editGenerateExplanations, setEditGenerateExplanations] =
        useState(false);

    const [replacementTestFile, setReplacementTestFile] =
        useState(null);

    const [replacementAudioCount, setReplacementAudioCount] =
        useState(1);

    const [replacementAudioFiles, setReplacementAudioFiles] =
        useState([null]);

    const [replacementQuestionCounts, setReplacementQuestionCounts] =
        useState([""]);
    const [importJobId, setImportJobId] =
        useState(null);

    const [importStatus, setImportStatus] =
        useState("");

    const [pendingCompetitionCreation,
        setPendingCompetitionCreation] =
        useState(null);

    const [title, setTitle] =
        useState("");

    const [description, setDescription] =
        useState("");

    const [courseType, setCourseType] =
        useState("");

    const [testType, setTestType] =
        useState("");

    const [testFile, setTestFile] =
        useState(null);

    const [audioCount, setAudioCount] =
        useState(1);

    const [audioFiles, setAudioFiles] =
        useState([null]);

    const [questionCounts, setQuestionCounts] =
        useState([""]);

    const [transcript, setTranscript] =
        useState("");
    const [generateExplanations, setGenerateExplanations] =
        useState(false);

    const [form, setForm] =
        useState({
            competitionTestId: "",
            classRoomId: "",
            title: "",
            description: "",
            startsAt: "",
            deadlineAt: "",
            durationMinutes: 120
        });

    const availableClasses =
        useMemo(() => {
            if (!courseType) {
                return options.classes;
            }

            return options.classes.filter(
                classRoom =>
                    Number(classRoom.courseType) ===
                    Number(courseType)
            );
        }, [
            options.classes,
            courseType
        ]);

    const normalizeStatus = status => {
        const value =
            typeof status === "string"
                ? status
                : Number(status);

        const map = {
            1: "scheduled",
            2: "open",
            3: "closed",
            4: "waiting",
            5: "ranked",
            6: "cancelled",
            7: "failed",
            Scheduled: "scheduled",
            Open: "open",
            Closed: "closed",
            WaitingForGrading: "waiting",
            Ranked: "ranked",
            Cancelled: "cancelled",
            Failed: "failed"
        };

        return map[value] ||
            map[status] ||
            "unknown";
    };

    const filteredCompetitions =
        useMemo(() => {
            const keyword =
                searchText.trim().toLowerCase();

            return competitions.filter(
                competition => {
                    const normalizedStatus =
                        normalizeStatus(
                            competition.status
                        );

                    const matchStatus =
                        activeStatus === "all" ||
                        normalizedStatus ===
                        activeStatus;

                    const searchableText = [
                        competition.title,
                        competition.description,
                        competition.competitionTestTitle,
                        competition.classRoomName
                    ]
                        .filter(Boolean)
                        .join(" ")
                        .toLowerCase();

                    const matchSearch =
                        !keyword ||
                        searchableText.includes(
                            keyword
                        );

                    return (
                        matchStatus &&
                        matchSearch
                    );
                }
            );
        }, [
            competitions,
            activeStatus,
            searchText
        ]);

    const statistics =
        useMemo(() => {
            return {
                open: competitions.filter(
                    item =>
                        normalizeStatus(
                            item.status
                        ) === "open"
                ).length,

                ranked: competitions.filter(
                    item =>
                        normalizeStatus(
                            item.status
                        ) === "ranked"
                ).length
            };
        }, [competitions]);

    const loadPage = async () => {
        setLoading(true);

        try {
            const [
                competitionData,
                optionData,
                testData
            ] = await Promise.all([
                getTeacherCompetitionsApi(),
                getCompetitionAssignmentOptionsApi(),
                getTeacherCompetitionTestsApi()
            ]);

            const safeCompetitions =
                Array.isArray(
                    competitionData
                )
                    ? competitionData
                    : [];

            const safeTests =
                Array.isArray(testData)
                    ? testData
                    : [];

            setCompetitions(
                safeCompetitions
            );

            setCompetitionTests(
                safeTests
            );

            setOptions(
                optionData || {
                    competitionTests:
                        safeTests,
                    classes: [],
                    serverTime: null
                }
            );
        }
        catch (error) {
            console.error(
                "Load competition page error:",
                error.response?.data || error
            );

            alert(
                error.response?.data?.message ||
                error.response?.data?.title ||
                "Unable to load competition test data."
            );
        }
        finally {
            setLoading(false);
        }
    };

    useEffect(() => {
        loadPage();
    }, []);

    useEffect(() => {
        if (!importJobId) {
            return;
        }

        const intervalId = setInterval(
            async () => {
                try {
                    const job =
                        await getCompetitionImportJobApi(
                            importJobId
                        );

                    setImportStatus(
                        String(job.status)
                    );

                    if (Number(job.status) === 3) {
                        clearInterval(intervalId);

                        if (!pendingCompetitionCreation) {
                            alert(
                                "Competition test imported successfully."
                            );

                            setImportJobId(null);
                            setImportStatus("");
                            await loadPage();
                            return;
                        }

                        const importedTests =
                            await getTeacherCompetitionTestsApi();

                        const existingIds =
                            new Set(
                                pendingCompetitionCreation
                                    .existingCompetitionTestIds
                            );

                        const importedTest =
                            (Array.isArray(importedTests)
                                ? importedTests
                                : [])
                                .filter(test =>
                                    !existingIds.has(
                                        Number(
                                            test.competitionTestId
                                        )
                                    ) &&
                                    test.title ===
                                    pendingCompetitionCreation
                                        .testTitle &&
                                    Number(test.courseType) ===
                                    Number(
                                        pendingCompetitionCreation
                                            .courseType
                                    ) &&
                                    Number(test.testType) ===
                                    Number(
                                        pendingCompetitionCreation
                                            .testType
                                    )
                                )
                                .sort((first, second) =>
                                    Number(
                                        second.competitionTestId
                                    ) -
                                    Number(
                                        first.competitionTestId
                                    )
                                )[0];

                        if (!importedTest) {
                            throw new Error(
                                "The test was imported, but the newly created test could not be identified for class assignment."
                            );
                        }

                        await createCompetitionAssignmentApi({
                            competitionTestId:
                                Number(
                                    importedTest
                                        .competitionTestId
                                ),
                            classRoomId:
                                Number(
                                    pendingCompetitionCreation
                                        .classRoomId
                                ),
                            title:
                                pendingCompetitionCreation
                                    .competitionTitle,
                            description:
                                pendingCompetitionCreation
                                    .description,
                            startsAt:
                                pendingCompetitionCreation
                                    .startsAt,
                            deadlineAt:
                                pendingCompetitionCreation
                                    .deadlineAt,
                            durationMinutes:
                                pendingCompetitionCreation
                                    .durationMinutes
                        });

                        alert(
                            "Competition created successfully."
                        );

                        setImportJobId(null);
                        setImportStatus("");
                        setPendingCompetitionCreation(null);
                        setShowCreateModal(false);
                        resetImportForm();
                        resetForm();

                        await loadPage();
                    }

                    if (Number(job.status) === 4) {
                        clearInterval(intervalId);

                        const errorMessage =
                            job.errorMessage || "";

                        const normalizedError =
                            errorMessage.toLowerCase();

                        if (
                            normalizedError.includes(
                                "groq_audio_daily_limit"
                            ) ||
                            normalizedError.includes(
                                "audio seconds per day"
                            ) ||
                            normalizedError.includes(
                                "asd"
                            )
                        ) {
                            alert(
                                "Import failed because Groq Whisper's daily audio quota has been exhausted. Please wait for the quota to reset or use a different API key/project."
                            );
                        }
                        else if (
                            normalizedError.includes(
                                "groq_daily_limit"
                            ) ||
                            normalizedError.includes(
                                "tokens per day"
                            ) ||
                            normalizedError.includes(
                                "requests per day"
                            ) ||
                            normalizedError.includes(
                                "tpd"
                            ) ||
                            normalizedError.includes(
                                "rpd"
                            )
                        ) {
                            alert(
                                "Import failed because Groq's daily quota has been exhausted. Please wait for the quota to reset or use a different API key, project, or model."
                            );
                        }
                        else if (
                            normalizedError.includes(
                                "groq_minute_limit"
                            ) ||
                            normalizedError.includes(
                                "tokens per minute"
                            ) ||
                            normalizedError.includes(
                                "requests per minute"
                            ) ||
                            normalizedError.includes(
                                "tpm"
                            ) ||
                            normalizedError.includes(
                                "rpm"
                            ) ||
                            normalizedError.includes(
                                "rate limit"
                            ) ||
                            normalizedError.includes(
                                "rate_limit_exceeded"
                            ) ||
                            normalizedError.includes(
                                "429"
                            )
                        ) {
                            alert(
                                "Import failed because Groq is temporarily rate-limited. Please wait a few minutes and import again."
                            );
                        }
                        else if (
                            normalizedError.includes(
                                "closed stream"
                            )
                        ) {
                            alert(
                                "Import failed because of a file stream processing error. Please check the Competition Core Import backend."
                            );
                        }
                        else {
                            alert(
                                errorMessage ||
                                "Failed to import the competition test."
                            );
                        }

                        setImportJobId(null);
                        setImportStatus("");
                        setPendingCompetitionCreation(null);
                    }
                }
                catch (error) {
                    clearInterval(intervalId);

                    console.error(
                        "Competition import/create error:",
                        error.response?.data || error
                    );

                    alert(
                        error.response?.data?.message ||
                        error.response?.data?.title ||
                        error.message ||
                        "Unable to complete competition creation."
                    );

                    setImportJobId(null);
                    setImportStatus("");
                    setPendingCompetitionCreation(null);
                }
            },
            5000
        );

        return () => {
            clearInterval(intervalId);
        };
    }, [
        importJobId,
        pendingCompetitionCreation
    ]);

    function resetForm() {
        setForm({
            competitionTestId: "",
            classRoomId: "",
            title: "",
            description: "",
            startsAt: "",
            deadlineAt: "",
            durationMinutes: 120
        });
    }

    function resetImportForm() {
        setTitle("");
        setDescription("");
        setCourseType("");
        setTestType("");
        setTestFile(null);
        setAudioCount(1);
        setAudioFiles([null]);
        setQuestionCounts([""]);
        setTranscript("");
        setGenerateExplanations(false);
    }
    const toDateTimeLocalValue = value => {
        const date = parseUtcDate(value);

        if (!date) {
            return "";
        }

        const parts =
            new Intl.DateTimeFormat(
                "en-CA",
                {
                    timeZone:
                        "Asia/Ho_Chi_Minh",

                    year: "numeric",
                    month: "2-digit",
                    day: "2-digit",
                    hour: "2-digit",
                    minute: "2-digit",
                    hourCycle: "h23"
                }
            ).formatToParts(date);

        const values = Object.fromEntries(
            parts.map(part => [
                part.type,
                part.value
            ])
        );

        return (
            `${values.year}-` +
            `${values.month}-` +
            `${values.day}T` +
            `${values.hour}:` +
            `${values.minute}`
        );
    };
    const vietnamDateTimeToUtcIso =
        localValue => {
            if (!localValue) {
                return null;
            }

            const normalizedValue =
                localValue.length === 16
                    ? `${localValue}:00`
                    : localValue;

            const date = new Date(
                `${normalizedValue}+07:00`
            );

            if (Number.isNaN(
                date.getTime()
            )) {
                return null;
            }

            return date.toISOString();
        };

    const openEditCompetitionModal =
        competition => {
            setEditingCompetition(
                competition
            );

            setEditTitle(
                competition.title || ""
            );

            setEditDescription(
                competition.description || ""
            );

            setEditStartsAt(
                toDateTimeLocalValue(
                    competition.startsAt
                )
            );

            setEditDeadlineAt(
                toDateTimeLocalValue(
                    competition.deadlineAt
                )
            );

            setEditDurationMinutes(
                competition.durationMinutes || 120
            );

            setReplacementTestFile(null);
            setReplacementAudioCount(1);
            setReplacementAudioFiles([null]);
            setReplacementQuestionCounts([""]);
            setEditGenerateExplanations(false);

            setShowEditModal(true);
        };
    const getImportStatusText = status => {
        const value = Number(status);

        if (value === 1) {
            return "Waiting to be processed";
        }

        if (value === 2) {
            return "Processing";
        }

        if (value === 3) {
            return "Completed";
        }

        if (value === 4) {
            return "Failed";
        }

        return status || "";
    };
    const handleUpdateCompetition =
        async () => {
            if (!editingCompetition) {
                return;
            }
            if (replacementTestFile) {
                const missingAudio =
                    replacementAudioFiles.length !==
                    replacementAudioCount ||
                    replacementAudioFiles.some(
                        file => !file
                    );

                if (missingAudio) {
                    alert(
                "Please select all audio files for the replacement test."
                    );

                    return;
                }

                const invalidQuestionCounts =
                    replacementQuestionCounts.length !==
                    replacementAudioCount ||
                    replacementQuestionCounts.some(
                        value =>
                            !value ||
                            Number(value) <= 0
                    );

                if (invalidQuestionCounts) {
                    alert(
                "Please enter a valid question count for each audio file."
                    );

                    return;
                }
            }
            if (!editTitle.trim()) {
                alert(
                "Please enter a competition title."
                );

                return;
            }

            if (!editStartsAt ||
                !editDeadlineAt) {
                alert(
                "Please select both start and end times."
                );

                return;
            }

            const startsAtIso =
                vietnamDateTimeToUtcIso(
                    editStartsAt
                );

            const deadlineAtIso =
                vietnamDateTimeToUtcIso(
                    editDeadlineAt
                );

            const startsAt =
                new Date(startsAtIso);

            const deadlineAt =
                new Date(deadlineAtIso);

            if (deadlineAt <= startsAt) {
                alert(
                "The end time must be after the start time."
                );

                return;
            }

            setSubmitting(true);

            try {
                await updateTeacherCompetitionApi(
                    editingCompetition
                        .competitionTestAssignmentId,
                    {
                        title:
                            editTitle.trim(),

                        description:
                            editDescription.trim(),

                        startsAt:
                            startsAtIso,

                        deadlineAt:
                            deadlineAtIso,

                        durationMinutes:
                            Number(
                                editDurationMinutes
                            ),
                        generateExplanations:
                            editGenerateExplanations &&
                            !replacementTestFile
                    }
                );

                if (replacementTestFile) {
                    const formData =
                        new FormData();

                    formData.append(
                        "testFile",
                        replacementTestFile
                    );

                    formData.append(
                        "title",
                        editTitle.trim()
                    );

                    formData.append(
                        "description",
                        editDescription.trim()
                    );

                    formData.append(
                        "courseType",
                        Number(
                            editingCompetition
                                .courseType
                        )
                    );

                    formData.append(
                        "testType",
                        Number(
                            editingCompetition
                                .testType
                        )
                    );

                    replacementAudioFiles
                        .filter(Boolean)
                        .forEach(file => {
                            formData.append(
                                "audioFiles",
                                file
                            );
                        });

                    formData.append(
                        "questionCountsJson",
                        JSON.stringify(
                            replacementQuestionCounts
                                .map(Number)
                        )
                    );

                    formData.append(
                        "transcript",
                        ""
                    );

                    formData.append(
                        "generateExplanations",
                        editGenerateExplanations
                    );

                    await replaceCompetitionTestApi(
                        editingCompetition
                            .competitionTestId,
                        formData
                    );
                }

                alert(
                    replacementTestFile
                                            ? "Competition updated and the replacement test has been queued for processing."
                                            : "Competition updated successfully."
                );

                setShowEditModal(false);
                setEditingCompetition(null);

                await loadPage();
            }
            catch (error) {
                console.error(
                    "Update competition error:",
                    error.response?.data || error
                );

                alert(
                    error.response?.data?.message ||
                    error.response?.data?.title ||
                "Failed to update the competition."
                );
            }
            finally {
                setSubmitting(false);
            }
        };
    const handleViewCompetitionSource =
        async competition => {
            const competitionTestId =
                competition.competitionTestId;

            if (!competitionTestId) {
                alert(
                "Unable to identify the competition test."
                );

                return;
            }

            /*
             * Mở cửa sổ trước khi await để tránh
             * trình duyệt chặn popup.
             */
            const previewWindow =
                window.open(
                    "about:blank",
                    "_blank"
                );

            if (!previewWindow) {
                alert(
                "The browser blocked the new window. " +
                "Please allow pop-ups to view the test."
                );

                return;
            }

            previewWindow.opener =
                null;

            previewWindow.document.title =
            "Opening test source...";

            previewWindow.document.body.innerHTML =
                "<p style='font-family: sans-serif; " +
            "padding: 24px;'>Loading test source file...</p>";

            try {
                const result =
                    await getCompetitionSourceFileApi(
                        competitionTestId
                    );

                const fileUrl =
                    result?.fileUrl;

                if (!fileUrl) {
                    previewWindow.close();

                    alert(
                "Original test source file not found."
                    );

                    return;
                }

                previewWindow.location.replace(
                    fileUrl
                );
            }
            catch (error) {
                previewWindow.close();

                console.error(
                    "View competition source error:",
                    error.response?.data || error
                );

                alert(
                    error.response?.data?.message ||
                    error.response?.data?.title ||
            "Unable to open the test source file."
                );
            }
        };
    const handleHardDeleteCompetition =
        async competition => {
            const firstConfirmed =
                window.confirm(
                    `Are you sure you want to permanently delete "${competition.competitionTestTitle || competition.title}"?\n\n` +
                    "This will delete every competition using this test, along with attempts, answers, results, leaderboards, and related notifications. This action cannot be undone."
                );

            if (!firstConfirmed) {
                return;
            }

            const finalConfirmed =
                window.confirm(
                    `Final confirmation: permanently delete the competition "${competition.title}"?\n\n` +
                    "This will delete the assignment, test, and all related Listening, Reading, Writing, and Speaking content."
                );

            if (!finalConfirmed) {
                return;
            }

            try {
                await hardDeleteTeacherCompetitionApi(
                    competition
                        .competitionTestAssignmentId
                );

                alert(
                "Competition and test permanently deleted successfully."
                );

                await loadPage();
            }
            catch (error) {
                console.error(
                    "Hard delete competition error:",
                    error.response?.data || error
                );

                alert(
                    error.response?.data?.message ||
                    error.response?.data?.title ||
                "Unable to delete the competition."
                );
            }
        };
    const getDefaultTestTypeFromCourseType =
        selectedCourseType => {
            const value =
                Number(selectedCourseType);

            if (value === COURSE_TYPES.VSTEP) {
                return TEST_TYPES.VstepFourSkills;
            }

            if (value === COURSE_TYPES.TOEIC) {
                return TEST_TYPES.ToeicTwoSkills;
            }

            return "";
        };

    const handleCourseTypeChange = value => {
        setCourseType(value);

        handleTestTypeChange(
            getDefaultTestTypeFromCourseType(
                value
            )
        );
    };

    const isToeicTestType = value => {
        const numericValue =
            Number(value);

        return (
            numericValue === TEST_TYPES.ToeicTwoSkills ||
            numericValue === TEST_TYPES.ToeicFourSkills
        );
    };

    const applyListeningQuestionCountDefaults =
        counts => {
            setAudioCount(counts.length);
            setAudioFiles(Array(counts.length).fill(null));
            setQuestionCounts(counts);
        };

    const handleTestTypeChange = value => {
        setTestType(value);

        if (isToeicTestType(value)) {
            applyListeningQuestionCountDefaults(
                TOEIC_LISTENING_DEFAULT_QUESTION_COUNTS
            );
        }
    };

    const competitionTestHasListening = () => {
        const value =
            Number(testType);

        return (
            value === TEST_TYPES.VstepFourSkills ||
            value === TEST_TYPES.ToeicTwoSkills ||
            value === TEST_TYPES.ToeicFourSkills
        );
    };

    const handleAudioCountChange = value => {
        const parsedValue =
            Number(value);

        const count =
            Number.isFinite(parsedValue) &&
                parsedValue > 0
                ? parsedValue
                : 1;

        setAudioCount(count);
        setAudioFiles(
            Array(count).fill(null)
        );
        setQuestionCounts(
            Array(count).fill("")
        );
    };

    const handleAudioFileChange = (
        index,
        file
    ) => {
        const next =
            [...audioFiles];

        next[index] =
            file || null;

        setAudioFiles(next);
    };

    const handleQuestionCountChange = (
        index,
        value
    ) => {
        const next =
            [...questionCounts];

        next[index] = value;

        setQuestionCounts(next);
    };

    const validateImportForm = () => {
        if (!title.trim()) {
            alert(
                "Please enter a competition test title."
            );

            return false;
        }

        if (!courseType) {
            alert(
                "Please select a course type."
            );

            return false;
        }

        if (!testType) {
            alert(
                "Please select a competition test type."
            );

            return false;
        }

        if (!testFile) {
            alert(
                "Please select a PDF, DOCX, or TXT file."
            );

            return false;
        }

        const extension =
            testFile.name
                .split(".")
                .pop()
                ?.toLowerCase();

        if (
            !["pdf", "docx", "txt"].includes(
                extension
            )
        ) {
            alert(
                "Only PDF, DOCX, and TXT files are accepted."
            );

            return false;
        }

        if (competitionTestHasListening()) {
            if (
                audioFiles.length !==
                audioCount ||
                audioFiles.some(
                    file => !file
                )
            ) {
                alert(
                "Please select all audio files for Listening."
                );

                return false;
            }

            if (
                questionCounts.length !==
                audioCount ||
                questionCounts.some(
                    value =>
                        !value ||
                        Number(value) <= 0
                )
            ) {
                alert(
                "Please enter a valid question count for each audio file."
                );

                return false;
            }
        }

        return true;
    };

    const handleCreateCompetition =
        async () => {
            if (!validateImportForm() ||
                !validateCreateForm()) {
                return;
            }

            setSubmitting(true);

            try {
                const formData =
                    new FormData();

                formData.append(
                    "testFile",
                    testFile
                );

                formData.append(
                    "title",
                    title.trim()
                );

                formData.append(
                    "description",
                    description.trim()
                );

                formData.append(
                    "testType",
                    Number(testType)
                );

                formData.append(
                    "courseType",
                    Number(courseType)
                );

                formData.append(
                    "generateExplanations",
                    generateExplanations
                );

                if (competitionTestHasListening()) {
                    audioFiles.forEach(
                        file => {
                            formData.append(
                                "audioFiles",
                                file
                            );
                        }
                    );

                    formData.append(
                        "questionCountsJson",
                        JSON.stringify(
                            questionCounts.map(
                                Number
                            )
                        )
                    );

                    formData.append(
                        "transcript",
                        transcript.trim()
                    );
                }
                else {
                    formData.append(
                        "questionCountsJson",
                        "[]"
                    );

                    formData.append(
                        "transcript",
                        ""
                    );
                }

                const data =
                    await importCompetitionTestApi(
                        formData
                    );

                const backgroundJobId =
                    data?.backgroundJobId;

                if (!backgroundJobId) {
                    alert(
                "Unable to retrieve the background job ID."
                    );

                    return;
                }

                setPendingCompetitionCreation({
                    existingCompetitionTestIds:
                        competitionTests.map(test =>
                            Number(
                                test.competitionTestId
                            )
                        ),
                    testTitle: title.trim(),
                    competitionTitle: title.trim(),
                    description: description.trim(),
                    courseType: Number(courseType),
                    testType: Number(testType),
                    classRoomId:
                        Number(form.classRoomId),
                    startsAt:
                        vietnamDateTimeToUtcIso(
                            form.startsAt
                        ),

                    deadlineAt:
                        vietnamDateTimeToUtcIso(
                            form.deadlineAt
                        ),
                    durationMinutes:
                        Number(
                            form.durationMinutes
                        )
                });

                setImportJobId(
                    backgroundJobId
                );

                setImportStatus("1");

                alert(
                "The competition test is being imported. The competition will be created automatically after processing finishes."
                );
            }
            catch (error) {
                console.error(
                    "Create competition error:",
                    error.response?.data || error
                );

                alert(
                    error.response?.data?.message ||
                    error.response?.data?.title ||
                "Unable to start competition creation."
                );
            }
            finally {
                setSubmitting(false);
            }
        };










    const validateCreateForm = () => {
        if (!form.classRoomId) {
            alert(
                "Please select a class."
            );

            return false;
        }

        if (!form.startsAt) {
            alert(
                "Please select a start time."
            );

            return false;
        }

        if (!form.deadlineAt) {
            alert(
                "Please select a deadline."
            );

            return false;
        }

        if (
            !form.durationMinutes ||
            Number(
                form.durationMinutes
            ) <= 0
        ) {
            alert(
                "The competition duration must be greater than 0."
            );

            return false;
        }

        const startsAtIso =
            vietnamDateTimeToUtcIso(
                form.startsAt
            );

        const deadlineAtIso =
            vietnamDateTimeToUtcIso(
                form.deadlineAt
            );

        if (!startsAtIso ||
            !deadlineAtIso) {
            alert(
                "Invalid time."
            );

            return false;
        }

        const startsAt =
            new Date(startsAtIso);

        const deadlineAt =
            new Date(deadlineAtIso);

        if (deadlineAt <= startsAt) {
            alert(
                "The deadline must be after the start time."
            );

            return false;
        }

        const availableMinutes =
            (deadlineAt - startsAt) /
            1000 /
            60;

        if (
            Number(
                form.durationMinutes
            ) > availableMinutes
        ) {
            alert(
                "The competition duration cannot exceed the open competition period."
            );

            return false;
        }

        return true;
    };

    const handleCancel =
        async competition => {
            const confirmed =
                window.confirm(
            `Are you sure you want to cancel the competition "${competition.title}"?`
                );

            if (!confirmed) {
                return;
            }

            try {
                await cancelTeacherCompetitionApi(
                    competition
                        .competitionTestAssignmentId
                );

                alert(
                "Competition cancelled successfully."
                );



                await loadPage();
            }
            catch (error) {
                alert(
                    error.response?.data?.message ||
                "Unable to cancel the competition."
                );
            }
        };



    const getStatusText = status => {
        const normalized =
            normalizeStatus(status);

        const map = {
        scheduled: "Scheduled",
        open: "Open",
        closed: "Closed",
        waiting: "Waiting for grading",
        ranked: "Ranked",
        cancelled: "Cancelled",
        failed: "Failed",
        unknown: "Unknown"
        };

        return map[normalized];
    };




    const statusTabs = [
        {
            key: "all",
            label: "All",
            count: competitions.length
        },
        {
            key: "open",
            label: "Open",
            count:
                statistics.open
        },
        {
            key: "ranked",
            label: "Ranked",
            count:
                statistics.ranked
        }
    ];

    return (
        <div className="lesson-layout">
            <TeacherSidebar />

            <main className="lesson-main competition-page">
                <div className="competition-breadcrumb">
                    Home
                    <span>›</span>
                    Competition Tests
                </div>

                <section className="competition-page-header">
                    <div>
                        <div className="competition-title-row">
                            <Trophy size={30} />

                            <div>
                                <h1>
                            Competition Tests
                                </h1>

                                <p>
                            Assign competition tests to classes,
                            manage schedules, and track
                            participation progress
                                </p>
                            </div>
                        </div>

                        {importJobId && (
                            <div className="competition-job-status">
                            The test is being processed.
                                {" "}
                            Job ID: {importJobId}
                                {" - "}
                                {getImportStatusText(
                                    importStatus
                                )}
                            </div>
                        )}
                    </div>

                    <div className="competition-header-actions">
                        <button
                            type="button"
                            className="competition-btn competition-btn-primary"
                            disabled={
                                Boolean(importJobId)
                            }
                            onClick={() => {
                                resetImportForm();
                                resetForm();
                                setShowCreateModal(
                                    true
                                );
                            }}
                        >
                            <Plus size={20} />
                            Create Competition Test
                        </button>
                    </div>
                </section>



                <section className="competition-controls">
                    <div className="competition-tabs">
                        {statusTabs.map(tab => (
                            <button
                                key={tab.key}
                                type="button"
                                className={
                                    activeStatus === tab.key
                                        ? "active"
                                        : ""
                                }
                                onClick={() =>
                                    setActiveStatus(tab.key)
                                }
                            >
                                {tab.label}

                                <span>
                                    {tab.count}
                                </span>
                            </button>
                        ))}
                    </div>

                    <div className="competition-search-area">
                        <label className="competition-search-box">
                            <Search size={18} />

                            <input
                                type="text"
                                value={searchText}
                            placeholder="Search competitions..."
                                onChange={event =>
                                    setSearchText(
                                        event.target.value
                                    )
                                }
                            />
                        </label>
                    </div>
                </section>

                {loading && (
                    <div className="competition-empty">
                            Loading competition tests...
                    </div>
                )}

                {!loading &&
                    filteredCompetitions.length ===
                    0 && (
                        <div className="competition-empty">
                            <Trophy size={22} />
                            No matching competitions found.
                        </div>
                    )}

                {!loading && filteredCompetitions.length > 0 && (
                    <section className="competition-table-wrap">
                        <Table
                            className="competition-table"
                            dataSource={filteredCompetitions}
                            rowKey="competitionTestAssignmentId"
                            tableLayout="fixed"
                            pagination={{
                                pageSize: 10,
                                showSizeChanger: true,
                                showTotal: total => `${total} competitions`
                            }}
                            columns={[
                                {
                                    title: "Competition",
                                    key: "competition",
                                    width: 200,
                                    render: (_, competition) => (
                                        <div className="competition-table-title">
                                            <Tooltip title={competition.title}>
                                                <strong>{competition.title}</strong>
                                            </Tooltip>
                                            <span>{competition.description || "No description"}</span>
                                        </div>
                                    )
                                },
                                {
                                    title: "Class",
                                    dataIndex: "classRoomName",
                                    key: "classRoomName",
                                    width: 80,
                                    render: value => value || "-"
                                },
                                {
                                    title: "Status",
                                    dataIndex: "status",
                                    key: "status",
                                    render: value => (
                                        <Tag className={`competition-status-tag status-${normalizeStatus(value)}`}>
                                            {getStatusText(value)}
                                        </Tag>
                                    )
                                },
                                {
                                    title: "Duration",
                                    dataIndex: "durationMinutes",
                                    key: "durationMinutes",
                                    render: value => `${value} minutes`
                                },
                                {
                                    title: "Starts",
                                    dataIndex: "startsAt",
                                    key: "startsAt",
                                    render: value => (
                                        <div className="competition-table-date">
                                            <span>{formatVietnamDateOnly(value)}</span>
                                            <span>{formatVietnamHourMinute(value)}</span>
                                        </div>
                                    )
                                },
                                {
                                    title: "Deadline",
                                    dataIndex: "deadlineAt",
                                    key: "deadlineAt",
                                    render: value => (
                                        <div className="competition-table-date">
                                            <span>{formatVietnamDateOnly(value)}</span>
                                            <span>{formatVietnamHourMinute(value)}</span>
                                        </div>
                                    )
                                },
                                {
                                    title: "Participation",
                                    key: "participation",
                                    width: 200,
                                    render: (_, competition) => `${competition.participantCount} joined · ${competition.submittedCount} submitted`
                                },
                                {
                                    title: "Actions",
                                    key: "actions",
                                    width: 190,
                                    render: (_, competition) => (
                                        <Space size={4}>
                                            <Tooltip title="View source">
                                                <button type="button" className="competition-table-action source" onClick={() => handleViewCompetitionSource(competition)} aria-label="View competition source">
                                                    <FileText size={16} />
                                                </button>
                                            </Tooltip>
                                            <Tooltip title="Leaderboard">
                                                <button type="button" className="competition-table-action leaderboard" onClick={() => navigate(`/teacher/competitions/${competition.competitionTestAssignmentId}/leaderboard`)} aria-label="View leaderboard">
                                                    <Medal size={16} />
                                                </button>
                                            </Tooltip>
                                            {competition.canCancel && (
                                                <Tooltip title="Cancel competition">
                                                    <button type="button" className="competition-table-action cancel" onClick={() => handleCancel(competition)} aria-label="Cancel competition">
                                                        <XCircle size={16} />
                                                    </button>
                                                </Tooltip>
                                            )}
                                            <Tooltip title="Edit competition">
                                                <button type="button" className="competition-table-action edit" onClick={() => openEditCompetitionModal(competition)} aria-label="Edit competition">
                                                    <Pencil size={16} />
                                                </button>
                                            </Tooltip>
                                            <Tooltip title="Delete competition and test">
                                                <button type="button" className="competition-table-action delete" onClick={() => handleHardDeleteCompetition(competition)} aria-label="Delete competition and test">
                                                    <Trash2 size={16} />
                                                </button>
                                            </Tooltip>
                                        </Space>
                                    )
                                }
                            ]}
                        />
                    </section>
                )}

                {/*
                    {filteredCompetitions.map(
                        competition => {
                            const normalizedStatus =
                                normalizeStatus(
                                    competition.status
                                );

                            return (
                                <article
                                    key={
                                        competition
                                            .competitionTestAssignmentId
                                    }
                                    className={`competition-card status-${normalizedStatus}`}
                                >
                                    <div className="competition-card-topline" />

                                    <div className="competition-card-badges">
                                        <button
                                            type="button"
                                            className="competition-file-badge"
                                            title="Open original test source"
                                            onClick={() =>
                                                handleViewCompetitionSource(
                                                    competition
                                                )
                                            }
                                        >
                                            <FileText size={13} />
                                                View Source
                                        </button>
                                    </div>
                                    <div className="competition-card-manage-actions">
                                        <button
                                            type="button"
                                            title="Edit competition"
                                            onClick={() =>
                                                openEditCompetitionModal(
                                                    competition
                                                )
                                            }
                                        >
                                            <Pencil size={17} />
                                        </button>

                                        <button
                                            type="button"
                                            title="Delete competition and test"
                                            className="danger"
                                            onClick={() =>
                                                handleHardDeleteCompetition(
                                                    competition
                                                )
                                            }
                                        >
                                            <Trash2 size={17} />
                                        </button>
                                    </div>
                                    <div className="competition-card-title">
                                        <div className="competition-card-title-icon">
                                            <Trophy size={18} />
                                        </div>

                                        <div>
                                            <h3 title={competition.title}>
                                                {competition.title}
                                            </h3>

                                            <p>
                                                {competition.description ||
                                                "No description"}
                                            </p>

                                            <span
                                                className={`competition-status-inline status-${normalizedStatus}`}
                                            >
                                                <i />
                                                {getStatusText(competition.status)}
                                            </span>
                                        </div>
                                    </div>

                                    <div className="competition-info">
                                        <span>
                                            <strong>
                                                Duration:
                                            </strong>

                                            {
                                                competition
                                                    .durationMinutes
                                            }{" "}
                                                minutes
                                        </span>

                                        <span>
                                            <strong>
                                                Class:
                                            </strong>

                                            {
                                                competition
                                                    .classRoomName
                                            }
                                        </span>



                                        <span>
                                            <strong>
                                                Participated:
                                            </strong>

                                            {
                                                competition
                                                    .participantCount
                                            }
                                        </span>

                                        <span>
                                            <strong>
                                                Submitted:
                                            </strong>

                                            {
                                                competition
                                                    .submittedCount
                                            }
                                        </span>



                                        <div className="competition-date-block">
                                            <div className="competition-date-item">
                                                    <strong>Starts</strong>

                                                <span>
                                                    📅 {formatVietnamDateOnly(competition.startsAt)}
                                                </span>

                                                <span>
                                                    🕒 {formatVietnamHourMinute(competition.startsAt)}
                                                </span>
                                            </div>

                                            <div className="competition-date-item">
                                                <strong>Deadline</strong>

                                                <span>
                                                    📅 {formatVietnamDateOnly(competition.deadlineAt)}
                                                </span>

                                                <span>
                                                    🕒 {formatVietnamHourMinute(competition.deadlineAt)}
                                                </span>
                                            </div>
                                        </div>
                                    </div>

                                    <div className="competition-actions">
                                        <button
                                            type="button"
                                            className="competition-card-btn"
                                            onClick={() =>
                                                navigate(
                                                    `/teacher/competitions/${competition.competitionTestAssignmentId}/leaderboard`
                                                )
                                            }
                                        >
                                            <Medal size={16} />
                                                Leaderboard
                                        </button>

                                        {competition.canCancel && (
                                            <button
                                                type="button"
                                                className="competition-card-btn danger"
                                                onClick={() =>
                                                    handleCancel(
                                                        competition
                                                    )
                                                }
                                            >
                                                <XCircle size={16} />
                                                Cancel
                                            </button>
                                        )}
                                    </div>
                                </article>
                            );
                        }
                    )}
                </section>
                */}
            </main>

            {showCreateModal && (
                <div className="lesson-modal-overlay">
                    <div className="lesson-modal competition-create-modal">
                        <div className="competition-modal-heading">
                            <div>
                                <div className="competition-modal-icon">
                                    <Trophy size={20} />
                                </div>

                                <div>
                                    <h2>
                                        Create Competition Test
                                    </h2>

                                    <p>
                                        Import a new test and assign it directly to the teacher's assigned class.
                                    </p>
                                </div>
                            </div>

                            <button
                                type="button"
                                className="competition-modal-close"
                                disabled={
                                    Boolean(importJobId)
                                }
                                onClick={() => {
                                    setShowCreateModal(false);
                                    resetImportForm();
                                    resetForm();
                                }}
                            >
                                ×
                            </button>
                        </div>

                        <div className="competition-create-layout">
                            <div className="lesson-form">
                                <label>
                                    Competition Test Title
                                </label>

                                <input
                                    type="text"
                                    placeholder="e.g. July TOEIC Competition"
                                    value={title}
                                    onChange={event =>
                                        setTitle(
                                            event.target.value
                                        )
                                    }
                                />

                                <label>
                                    Description
                                </label>

                                <textarea
                                    placeholder="Competition test description"
                                    value={description}
                                    onChange={event =>
                                        setDescription(
                                            event.target.value
                                        )
                                    }
                                />

                                <div className="competition-form-row">
                                    <div className="competition-form-field">
                                        <label>
                                            Course Type
                                        </label>

                                        <select
                                            value={courseType}
                                            onChange={event => {
                                                handleCourseTypeChange(
                                                    event.target.value
                                                );

                                                setForm(current => ({
                                                    ...current,
                                                    classRoomId: ""
                                                }));
                                            }}
                                        >
                                            <option value="">
                                                Select a course type
                                            </option>

                                            <option value={COURSE_TYPES.TOEIC}>
                                                TOEIC
                                            </option>

                                            <option value={COURSE_TYPES.VSTEP}>
                                                VSTEP
                                            </option>

                                            <option value={COURSE_TYPES.Communication}>
                                                Communication
                                            </option>
                                        </select>
                                    </div>

                                    <div className="competition-form-field">
                                        <label>
                                            Competition Test Type
                                        </label>

                                        <select
                                            value={testType}
                                            onChange={event =>
                                                handleTestTypeChange(
                                                    event.target.value
                                                )
                                            }
                                        >
                                            <option value="">
                                                Select a competition test type
                                            </option>

                                            <option value={TEST_TYPES.VstepFourSkills}>
                                                VSTEP 4 Skills
                                            </option>

                                            <option value={TEST_TYPES.ToeicTwoSkills}>
                                                TOEIC Listening + Reading
                                            </option>

                                            <option value={TEST_TYPES.ToeicFourSkills}>
                                                TOEIC 4 Skills
                                            </option>
                                        </select>
                                    </div>
                                </div>

                                <label>
                                    Test File (PDF/DOCX/TXT)
                                </label>

                                <input
                                    key={
                                        testFile?.name ||
                                        "competition-file"
                                    }
                                    type="file"
                                    accept=".pdf,.docx,.txt"
                                    onChange={event =>
                                        setTestFile(
                                            event.target.files?.[0] ||
                                            null
                                        )
                                    }
                                />

                                {competitionTestHasListening() && (
                                    <div className="question-form-options">
                                        <h3>
                                            Listening Audio Mapping
                                        </h3>

                                        <p className="question-explanation">
                                            Enter the corresponding question count for each audio file.
                                        </p>

                                        <label>
                                            Number of Audio Files
                                        </label>

                                        <input
                                            type="number"
                                            min="1"
                                            value={audioCount}
                                            onChange={event =>
                                                handleAudioCountChange(
                                                    event.target.value
                                                )
                                            }
                                        />

                                        {Array.from({
                                            length: audioCount
                                        }).map(
                                            (_, index) => (
                                                <div
                                                    key={index}
                                                    className="competition-audio-block"
                                                >
                                                    <label>
                                                        Audio {index + 1}
                                                    </label>

                                                    <input
                                                        type="file"
                                                        accept=".mp3,.wav,.m4a,.webm"
                                                        onChange={event =>
                                                            handleAudioFileChange(
                                                                index,
                                                                event.target
                                                                    .files?.[0] ||
                                                                null
                                                            )
                                                        }
                                                    />

                                                    <label>
                                                        Questions for audio {index + 1}
                                                    </label>

                                                    <input
                                                        type="number"
                                                        min="1"
                                                        value={
                                                            questionCounts[index] ||
                                                            ""
                                                        }
                                                        placeholder="e.g. 5"
                                                        onChange={event =>
                                                            handleQuestionCountChange(
                                                                index,
                                                                event.target.value
                                                            )
                                                        }
                                                    />
                                                </div>
                                            )
                                        )}

                                        <textarea
                                            placeholder="Transcript (optional)"
                                            value={transcript}
                                            onChange={event =>
                                                setTranscript(
                                                    event.target.value
                                                )
                                            }
                                        />
                                    </div>
                                )}

                                <label>
                                    Class
                                </label>

                                <select
                                    value={form.classRoomId}
                                    disabled={!courseType}
                                    onChange={event =>
                                        setForm(current => ({
                                            ...current,
                                            classRoomId:
                                                event.target.value
                                        }))
                                    }
                                >
                                    <option value="">
                                        Select a class
                                    </option>

                                    {availableClasses.map(
                                        classRoom => (
                                            <option
                                                key={
                                                    classRoom.classRoomId
                                                }
                                                value={
                                                    classRoom.classRoomId
                                                }
                                            >
                                                {classRoom.className}
                                                {" - "}
                                                {classRoom.courseName}
                                            </option>
                                        )
                                    )}
                                </select>

                                <div className="competition-form-row">
                                    <div className="competition-form-field">
                                        <label>
                                            Start Time
                                        </label>

                                        <input
                                            type="datetime-local"
                                            value={form.startsAt}
                                            onChange={event =>
                                                setForm(current => ({
                                                    ...current,
                                                    startsAt:
                                                        event.target.value
                                                }))
                                            }
                                        />
                                    </div>

                                    <div className="competition-form-field">
                                        <label>
                                            End Time
                                        </label>

                                        <input
                                            type="datetime-local"
                                            value={form.deadlineAt}
                                            onChange={event =>
                                                setForm(current => ({
                                                    ...current,
                                                    deadlineAt:
                                                        event.target.value
                                                }))
                                            }
                                        />
                                    </div>
                                </div>

                                <label>
                                    Competition Duration (minutes)
                                </label>

                                <input
                                    type="number"
                                    min="1"
                                    max="1440"
                                    value={form.durationMinutes}
                                    onChange={event =>
                                        setForm(current => ({
                                            ...current,
                                            durationMinutes:
                                                event.target.value
                                        }))
                                    }
                                />

                                <label className="teacher-explanation-option">
                                    <input
                                        type="checkbox"
                                        checked={generateExplanations}
                                        onChange={event =>
                                            setGenerateExplanations(
                                                event.target.checked
                                            )
                                        }
                                    />
                                    <span>
                                        Generate explanations for this competition test
                                        <small>
                                            Clear this option to parse the test without generating AI explanations.
                                        </small>
                                    </span>
                                </label>

                                <button
                                    type="button"
                                    className="lesson-btn"
                                    disabled={
                                        submitting ||
                                        Boolean(importJobId)
                                    }
                                    onClick={
                                        handleCreateCompetition
                                    }
                                >
                                    {submitting
                                        ? "Submitting..."
                                        : importJobId
                                            ? "Processing test..."
                                            : "Create Competition"}
                                </button>
                            </div>


                        </div>
                    </div>
                </div>
            )}
            {showEditModal &&
                editingCompetition && (
                    <div className="lesson-modal-overlay">
                        <div className="lesson-modal competition-edit-modal">
                            <div className="competition-modal-heading">
                                <div>
                                    <div className="competition-modal-icon">
                                        <Pencil size={20} />
                                    </div>

                                    <div>
                                        <h2>
                                            Edit Competition
                                        </h2>

                                        <p>
                                            Edit competition information
                                            or upload a new test to replace the current one.
                                        </p>
                                    </div>
                                </div>

                                <button
                                    type="button"
                                    className="competition-modal-close"
                                    disabled={submitting}
                                    onClick={() => {
                                        setShowEditModal(false);
                                        setEditingCompetition(null);
                                    }}
                                >
                                    ×
                                </button>
                            </div>

                            <div className="competition-edit-layout">
                                <div className="lesson-form">
                                    <label>
                                    Competition Title
                                    </label>

                                    <input
                                        type="text"
                                    placeholder="Enter competition title"
                                        value={editTitle}
                                        onChange={event =>
                                            setEditTitle(
                                                event.target.value
                                            )
                                        }
                                    />

                                    <label>
                                    Description
                                    </label>

                                    <textarea
                                    placeholder="Enter competition description"
                                        value={editDescription}
                                        onChange={event =>
                                            setEditDescription(
                                                event.target.value
                                            )
                                        }
                                    />

                                    <div className="competition-form-row">
                                        <div className="competition-form-field">
                                            <label>
                                    Start Time
                                            </label>

                                            <input
                                                type="datetime-local"
                                                value={editStartsAt}
                                                onChange={event =>
                                                    setEditStartsAt(
                                                        event.target.value
                                                    )
                                                }
                                            />
                                        </div>

                                        <div className="competition-form-field">
                                            <label>
                                    End Time
                                            </label>

                                            <input
                                                type="datetime-local"
                                                value={editDeadlineAt}
                                                onChange={event =>
                                                    setEditDeadlineAt(
                                                        event.target.value
                                                    )
                                                }
                                            />
                                        </div>
                                    </div>

                                    <label>
                                    Competition Duration (minutes)
                                    </label>

                                    <input
                                        type="number"
                                        min="1"
                                        max="1440"
                                    placeholder="e.g. 120"
                                        value={editDurationMinutes}
                                        onChange={event =>
                                            setEditDurationMinutes(
                                                event.target.value
                                            )
                                        }
                                    />

                                    <div className="competition-replacement-section">
                                        <div className="competition-replacement-heading">
                                            <div>
                                                <h3>
                                    Replacement Test
                                                </h3>

                                                <p>
                                    Optional. Select a file only when you want
                                    to replace the current test content.
                                                </p>
                                            </div>
                                        </div>

                                        <label>
                                    Test File (PDF/DOCX/TXT)
                                        </label>

                                        <input
                                            type="file"
                                            accept=".pdf,.docx,.txt"
                                            onChange={event =>
                                                setReplacementTestFile(
                                                    event.target.files?.[0] ||
                                                    null
                                                )
                                            }
                                        />

                                        {replacementTestFile && (
                                            <div className="competition-replacement-audio">
                                                <label>
                                                    Number of Audio Files
                                                </label>

                                                <input
                                                    type="number"
                                                    min="1"
                                                    value={
                                                        replacementAudioCount
                                                    }
                                                    onChange={event => {
                                                        const count =
                                                            Math.max(
                                                                1,
                                                                Number(
                                                                    event.target.value
                                                                ) || 1
                                                            );

                                                        setReplacementAudioCount(
                                                            count
                                                        );

                                                        setReplacementAudioFiles(
                                                            Array(count)
                                                                .fill(null)
                                                        );

                                                        setReplacementQuestionCounts(
                                                            Array(count)
                                                                .fill("")
                                                        );
                                                    }}
                                                />

                                                {Array.from({
                                                    length:
                                                        replacementAudioCount
                                                }).map((_, index) => (
                                                    <div
                                                        key={index}
                                                        className="competition-audio-block"
                                                    >
                                                        <div className="competition-audio-title">
                                                            Audio {index + 1}
                                                        </div>

                                                        <label>
                                                            File audio
                                                        </label>

                                                        <input
                                                            type="file"
                                                            accept=".mp3,.wav,.m4a,.webm"
                                                            onChange={event => {
                                                                const next = [
                                                                    ...replacementAudioFiles
                                                                ];

                                                                next[index] =
                                                                    event.target.files?.[0] ||
                                                                    null;

                                                                setReplacementAudioFiles(
                                                                    next
                                                                );
                                                            }}
                                                        />

                                                        <label>
                                                            Questions for audio {index + 1}
                                                        </label>

                                                        <input
                                                            type="number"
                                                            min="1"
                                                            placeholder="e.g. 5"
                                                            value={
                                                                replacementQuestionCounts[
                                                                index
                                                                ] || ""
                                                            }
                                                            onChange={event => {
                                                                const next = [
                                                                    ...replacementQuestionCounts
                                                                ];

                                                                next[index] =
                                                                    event.target.value;

                                                                setReplacementQuestionCounts(
                                                                    next
                                                                );
                                                            }}
                                                        />
                                                    </div>
                                                ))}
                                            </div>
                                        )}
                                    </div>

                                    <label className="teacher-explanation-option">
                                        <input
                                            type="checkbox"
                                            checked={editGenerateExplanations}
                                            onChange={event =>
                                                setEditGenerateExplanations(
                                                    event.target.checked
                                                )
                                            }
                                        />
                                        <span>
                                            Generate explanations for this competition test
                                            <small>
                                                Only questions without explanations will be updated.
                                            </small>
                                        </span>
                                    </label>

                                    <button
                                        type="button"
                                        className="lesson-btn"
                                        disabled={submitting}
                                        onClick={
                                            handleUpdateCompetition
                                        }
                                    >
                                        {submitting
                                            ? "Updating..."
                                            : "Save Changes"}
                                    </button>
                                </div>
                            </div>
                        </div>
                    </div>
                )}


        </div>
    );
}

export default TeacherCompetitionPage;
