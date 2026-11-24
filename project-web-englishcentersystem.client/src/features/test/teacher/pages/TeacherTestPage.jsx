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
   
    ChevronRight,
    FileText,
    Headphones,
    Pencil,
    Plus,
    Search,
    Trash2
} from "lucide-react";

import TeacherSidebar
    from "../../../lesson/teacher/TeacherSidebar";

import {
    getMyTeacherClassesApi
} from "../../../lesson/api/lessonApi";

import {
    deleteTeacherTestApi,
    getTeacherTestSourceFileApi,
    getTeacherTestsApi,
    updateTeacherTestApi
} from "../api/teacherTestApi";

import {
    getTestBackgroundJobApi,
    importAssignTestApi
} from "../api/testImportApi";

import {
    formatVietnamDateTime
} from "../../../../utils/dateTimeUtils";

import "../../../lesson/styles/lesson.css";
import "../styles/teacherTest.css";

function TeacherTestPage() {
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

    const [classes, setClasses] =
        useState([]);

    const [tests, setTests] =
        useState([]);

    const [title, setTitle] =
        useState("");

    const [description, setDescription] =
        useState("");

    const [classRoomId, setClassRoomId] =
        useState("");

    const [courseType, setCourseType] =
        useState("");

    const [testType, setTestType] =
        useState("");

    const [maxAttempts, setMaxAttempts] =
        useState(1);

    const [durationMinutes, setDurationMinutes] =
        useState(120);
    const [generateExplanations, setGenerateExplanations] =
        useState(false);

    const [showEditModal, setShowEditModal] =
        useState(false);

    const [editingTest, setEditingTest] =
        useState(null);

    const [editTitle, setEditTitle] =
        useState("");

    const [editDescription, setEditDescription] =
        useState("");

    const [editMaxAttempts, setEditMaxAttempts] =
        useState(1);

    const [editDurationMinutes, setEditDurationMinutes] =
        useState(120);
    const [editGenerateExplanations, setEditGenerateExplanations] =
        useState(false);

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

    const [importJobId, setImportJobId] =
        useState(null);

    const [importStatus, setImportStatus] =
        useState("");

    const [showImportModal, setShowImportModal] =
        useState(false);

    const [searchKeyword, setSearchKeyword] =
        useState("");

    const [activeFilter, setActiveFilter] =
        useState("all");

    const handleViewTestSource = async test => {
        const previewWindow = window.open("about:blank", "_blank");

        if (!previewWindow) {
            alert("Please allow pop-ups to view the test source.");
            return;
        }

        previewWindow.opener = null;
        previewWindow.document.title = "Opening test source...";
        previewWindow.document.body.innerHTML =
            "<p style='font-family:sans-serif;padding:24px;'>Loading test source file...</p>";

        try {
            const result = await getTeacherTestSourceFileApi(test.testId);

            if (!result?.fileUrl) {
                throw new Error("Test source file not found");
            }

            previewWindow.location.replace(result.fileUrl);
        } catch (error) {
            previewWindow.close();
            alert(error.response?.data?.message || "Unable to open the test source file.");
        }
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

    const loadClasses = async () => {
        const data =
            await getMyTeacherClassesApi();

        setClasses(data || []);
    };

    const loadTests = async () => {
        const data =
            await getTeacherTestsApi();

        setTests(data || []);
    };

    useEffect(() => {
        loadClasses();
        loadTests();
    }, []);

    useEffect(() => {
        if (!importJobId) {
            return;
        }

        const intervalId =
            setInterval(async () => {
                try {
                    const job =
                        await getTestBackgroundJobApi(
                            importJobId
                        );

                    setImportStatus(
                        String(job.status)
                    );

                    if (
                        Number(job.status) === 3
                    ) {
                        clearInterval(intervalId);

                        alert(
                            "Test created successfully."
                        );

                        setImportJobId(null);
                        setImportStatus("");

                        await loadTests();
                    }

                    if (
                        Number(job.status) === 4
                    ) {
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
                                "Import failed because Groq Whisper's daily audio quota has been exhausted. " +
                                "Please wait for the quota to reset or use a different API key/project."
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
                            normalizedError.includes("tpd") ||
                            normalizedError.includes("rpd")
                        ) {
                            alert(
                                "Import failed because Groq's daily quota has been exhausted. " +
                                "Please wait for the quota to reset or use a different API key, project, or model."
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
                            normalizedError.includes("tpm") ||
                            normalizedError.includes("rpm") ||
                            normalizedError.includes("rate limit") ||
                            normalizedError.includes(
                                "rate_limit_exceeded"
                            ) ||
                            normalizedError.includes("429")
                        ) {
                            alert(
                                "Import failed because Groq is temporarily rate-limited. " +
                                "Please wait a few minutes and import again."
                            );
                        }
                        else if (
                            normalizedError.includes(
                                "closed stream"
                            )
                        ) {
                            alert(
                                "Import failed because of a file stream processing error. " +
                                "Please check the backend core import service."
                            );
                        }
                        else {
                            alert(
                                errorMessage ||
                                "Failed to create the test."
                            );
                        }

                        setImportJobId(null);
                        setImportStatus("");
                    }
                }
                catch (error) {
                    console.log(
                        "Test import polling error:",
                        error
                    );
                }
            }, 5000);

        return () =>
            clearInterval(intervalId);
    }, [importJobId]);

    const normalizeCourseName =
        value =>
            String(value || "")
                .normalize("NFD")
                .replace(/[\u0300-\u036f]/g, "")
                .toLowerCase();

    const getClassCourseName =
        classRoom =>
            normalizeCourseName(
                classRoom?.courseName ||
                classRoom?.courseTitle ||
                classRoom?.course?.name ||
                ""
            );

    const isToeicFourSkillsCourseName =
        name =>
            name.includes("toeic") &&
            (
                name.includes("four skill") ||
                name.includes("4 skill") ||
                name.includes("4 ky nang") ||
                name.includes("4 ki nang") ||
                (
                    name.includes("speaking") &&
                    name.includes("writing")
                )
            );

    const getCourseTypeFromClass =
        classRoom => {
            const name =
                getClassCourseName(classRoom);

            if (name.includes("toeic")) {
                return COURSE_TYPES.TOEIC;
            }

            if (name.includes("vstep")) {
                return COURSE_TYPES.VSTEP;
            }

            if (
                name.includes("communication")
            ) {
                return COURSE_TYPES.Communication;
            }

            return "";
        };

    const getDefaultTestTypeFromClass =
        classRoom => {
            const name =
                getClassCourseName(classRoom);

            if (name.includes("vstep")) {
                return TEST_TYPES.VstepFourSkills;
            }

            if (isToeicFourSkillsCourseName(name)) {
                return TEST_TYPES.ToeicFourSkills;
            }

            if (name.includes("toeic")) {
                return TEST_TYPES.ToeicTwoSkills;
            }

            return "";
        };

    const openEditModal = test => {
        setEditingTest(test);
        setEditTitle(test.title || "");
        setEditDescription(
            test.description || ""
        );
        setEditMaxAttempts(
            test.maxAttempts || 1
        );
        setEditDurationMinutes(
            test.durationMinutes || 120
        );
        setEditGenerateExplanations(false);
        setShowEditModal(true);
    };

    const handleUpdateTest = async () => {
        if (!editingTest) {
            return;
        }

        if (!editTitle.trim()) {
            alert(
                "Please enter a test title."
            );
            return;
        }

        if (
            Number(editMaxAttempts) <= 0
        ) {
            alert(
                "The maximum number of attempts must be greater than 0."
            );
            return;
        }

        if (
            Number(editDurationMinutes) <= 0
        ) {
            alert(
                "The test duration must be greater than 0."
            );
            return;
        }

        try {
            await updateTeacherTestApi(
                editingTest.testId,
                {
                    title: editTitle,
                    description:
                        editDescription,
                    maxAttempts:
                        Number(editMaxAttempts),
                    durationMinutes:
                        Number(editDurationMinutes),
                    generateExplanations:
                        editGenerateExplanations
                }
            );

            alert(
                "Test updated successfully."
            );

            setShowEditModal(false);
            setEditingTest(null);

            await loadTests();
        }
        catch (error) {
            console.log(
                "Update test error:",
                error.response?.data
            );

            alert(
                error.response?.data?.message ||
                error.response?.data?.title ||
                "Failed to update the test."
            );
        }
    };

    const handleDeleteTest =
        async test => {
            const confirmed =
                window.confirm(
                    `Are you sure you want to delete "${test.title}"?\n\n` +
                    "This will permanently delete the test, assignments, attempts, and related results."
                );

            if (!confirmed) {
                return;
            }

            try {
                await deleteTeacherTestApi(
                    test.testId
                );

                alert(
                    "Test deleted successfully."
                );

                await loadTests();
            }
            catch (error) {
                console.log(
                    "Delete test error:",
                    error.response?.data
                );

                alert(
                    error.response?.data
                        ?.message ||
                    error.response?.data
                        ?.title ||
                    "Failed to delete the test."
                );
            }
        };

    const resetForm = () => {
        setTitle("");
        setDescription("");
        setClassRoomId("");
        setCourseType("");
        setTestType("");
        setMaxAttempts(1);
        setDurationMinutes(120);
        setTestFile(null);
        setAudioCount(1);
        setAudioFiles([null]);
        setQuestionCounts([""]);
        setTranscript("");
        setGenerateExplanations(false);
    };

    const testHasListening = () => {
        return (
            Number(testType) ===
            TEST_TYPES.VstepFourSkills ||
            Number(testType) ===
            TEST_TYPES.ToeicTwoSkills ||
            Number(testType) ===
            TEST_TYPES.ToeicFourSkills
        );
    };

    const isToeicTestType = value => {
        const numericValue = Number(value);

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

    const handleAudioCountChange =
        value => {
            const count =
                Math.max(1, Number(value));

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
        const next = [...audioFiles];

        next[index] = file;

        setAudioFiles(next);
    };

    const handleQuestionCountChange = (
        index,
        value
    ) => {
        const next = [
            ...questionCounts
        ];

        next[index] = value;

        setQuestionCounts(next);
    };

    const validateForm = () => {
        if (!title.trim()) {
            alert(
                "Please enter a test title."
            );
            return false;
        }

        if (!classRoomId) {
            alert("Please select a class.");
            return false;
        }

        if (!courseType) {
            alert(
                "Unable to identify the course type."
            );
            return false;
        }

        if (!testType) {
            alert(
                "Please select a test type."
            );
            return false;
        }

        if (!testFile) {
            alert(
                "Please select a test PDF, DOCX, or TXT file."
            );
            return false;
        }

        if (
            Number(maxAttempts) <= 0
        ) {
            alert(
                "The maximum number of attempts must be greater than 0."
            );
            return false;
        }

        if (
            Number(durationMinutes) <= 0
        ) {
            alert(
                "The test duration must be greater than 0."
            );
            return false;
        }

        if (testHasListening()) {
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
                    item =>
                        !item ||
                        Number(item) <= 0
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

    const handleImportTest =
        async () => {
            if (!validateForm()) {
                return;
            }

            const formData =
                new FormData();

            formData.append(
                "testFile",
                testFile
            );

            formData.append(
                "title",
                title
            );

            formData.append(
                "description",
                description || ""
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
                "classRoomId",
                Number(classRoomId)
            );

            formData.append(
                "maxAttempts",
                Number(maxAttempts)
            );

            formData.append(
                "durationMinutes",
                Number(durationMinutes)
            );

            formData.append(
                "generateExplanations",
                generateExplanations
            );

            if (testHasListening()) {
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
                    transcript || ""
                );
            }

            try {
                const data =
                    await importAssignTestApi(
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

                setImportJobId(
                    backgroundJobId
                );

                setImportStatus("1");

                alert(
                    "The test has been queued for processing."
                );

                setShowImportModal(false);
                resetForm();
            }
            catch (error) {
                console.log(
                    "Test import error:",
                    error.response?.data
                );

                alert(
                    error.response?.data
                        ?.message ||
                    error.response?.data
                        ?.title ||
                    "Failed to create the test."
                );
            }
        };

    const getTestTypeText =
        value => {
            if (
                Number(value) ===
                TEST_TYPES.VstepFourSkills
            ) {
                return "VSTEP 4 Skills";
            }

            if (
                Number(value) ===
                TEST_TYPES.ToeicTwoSkills
            ) {
                return "TOEIC Listening + Reading";
            }

            if (
                Number(value) ===
                TEST_TYPES.ToeicFourSkills
            ) {
                return "TOEIC 4 Skills";
            }

            return "Unknown";
        };

    const getCourseTypeText =
        value => {
            if (
                Number(value) ===
                COURSE_TYPES.TOEIC
            ) {
                return "TOEIC";
            }

            if (
                Number(value) ===
                COURSE_TYPES.VSTEP
            ) {
                return "VSTEP";
            }

            if (
                Number(value) ===
                COURSE_TYPES.Communication
            ) {
                return "Communication";
            }

            return "Other";
        };

    const statistics =
        useMemo(() => {
            const assigned =
                tests.filter(
                    test =>
                        Number(
                            test.assignmentCount
                        ) > 0
                ).length;

            return {
                total: tests.length,
                assigned,
                unassigned: tests.length - assigned,

                vstep: tests.filter(
                    test =>
                        Number(test.testType) ===
                        TEST_TYPES.VstepFourSkills
                ).length,

                toeicTwoSkills: tests.filter(
                    test =>
                        Number(test.testType) ===
                        TEST_TYPES.ToeicTwoSkills
                ).length,

                toeicFourSkills: tests.filter(
                    test =>
                        Number(test.testType) ===
                        TEST_TYPES.ToeicFourSkills
                ).length
            };
        }, [tests]);

    const filteredTests =
        useMemo(() => {
            const keyword =
                searchKeyword
                    .trim()
                    .toLowerCase();

            return [...tests]
                .filter(test => {
                    if (
                        activeFilter ===
                        "vstep"
                    ) {
                        return (
                            Number(
                                test.courseType
                            ) ===
                            COURSE_TYPES.VSTEP
                        );
                    }

                    if (activeFilter === "toeic-two-skills") {
                        return (
                            Number(test.testType) ===
                            TEST_TYPES.ToeicTwoSkills
                        );
                    }

                    if (activeFilter === "toeic-four-skills") {
                        return (
                            Number(test.testType) ===
                            TEST_TYPES.ToeicFourSkills
                        );
                    }

                    if (
                        activeFilter ===
                        "assigned"
                    ) {
                        return (
                            Number(
                                test.assignmentCount
                            ) > 0
                        );
                    }

                    if (
                        activeFilter ===
                        "unassigned"
                    ) {
                        return (
                            Number(
                                test.assignmentCount
                            ) === 0
                        );
                    }

                    return true;
                })
                .filter(test => {
                    if (!keyword) {
                        return true;
                    }

                    return [
                        test.title,
                        test.description,
                        getCourseTypeText(
                            test.courseType
                        ),
                        getTestTypeText(
                            test.testType
                        )
                    ]
                        .filter(Boolean)
                        .some(value =>
                            value
                                .toLowerCase()
                                .includes(keyword)
                        );
                })
                .sort((a, b) =>
                    (a.title || "")
                        .localeCompare(
                            b.title || "",
                            "vi",
                            {
                                sensitivity:
                                    "base"
                            }
                        )
                );
        }, [
            tests,
            searchKeyword,
            activeFilter
        ]);

    const filters = [
        {
            key: "all",
            label: "All",
            count: statistics.total
        },
        {
            key: "vstep",
            label: "VSTEP",
            count: statistics.vstep
        },
        {
            key: "toeic-four-skills",
            label: "TOEIC 4 Skills",
            count: statistics.toeicFourSkills
        },
        {
            key: "toeic-two-skills",
            label: "TOEIC Listening + Reading",
            count: statistics.toeicTwoSkills
        },
     
    ];

    return (
        <div className="lesson-layout">
            <TeacherSidebar />

            <main className="lesson-main teacher-test-main">
                <div className="teacher-test-page">
                    <div className="teacher-test-breadcrumb">
                        <span>Home</span>
                        <ChevronRight size={14} />
                        <strong>Tests</strong>
                    </div>

                    <section className="teacher-test-header">
                        <div>
                            <div className="teacher-test-title-row">
                                <div className="teacher-test-title-icon">
                                    <FileText size={24} />
                                </div>

                                <h1>Tests</h1>
                            </div>

                            <p>
                                Import VSTEP / TOEIC tests and assign them directly to your classes
                            </p>
                        </div>

                        <button
                            type="button"
                            className="teacher-test-import-btn"
                            onClick={() => {
                                resetForm();
                                setShowImportModal(
                                    true
                                );
                            }}
                        >
                            <Plus size={19} />
                            Create Test
                        </button>
                    </section>

                    {importJobId && (
                        <div className="teacher-test-job-banner">
                            <div className="teacher-test-job-spinner" />

                            <div className="teacher-test-job-text">
                                <strong>
                                    The test is being processed
                                </strong>

                                <span>
                                    Job ID {importJobId} · Status:{" "}
                                    {getImportStatusText(
                                        importStatus
                                    )}
                                </span>
                            </div>

                            <span className="teacher-test-job-status-pill">
                                {getImportStatusText(importStatus)}
                            </span>
                        </div>
                    )}

                  

                    <section className="teacher-test-toolbar">
                        <div className="teacher-test-tabs">
                            {filters.map(
                                filter => (
                                    <button
                                        type="button"
                                        key={
                                            filter.key
                                        }
                                        className={
                                            activeFilter ===
                                                filter.key
                                                ? "teacher-test-tab active"
                                                : "teacher-test-tab"
                                        }
                                        onClick={() =>
                                            setActiveFilter(
                                                filter.key
                                            )
                                        }
                                    >
                                        {filter.label}

                                        <span>
                                            {filter.count}
                                        </span>
                                    </button>
                                )
                            )}
                        </div>

                        <div className="teacher-test-actions">
                            <div className="teacher-test-search">
                                <Search size={18} />

                                <input
                                    value={
                                        searchKeyword
                                    }
                                    onChange={event =>
                                        setSearchKeyword(
                                            event.target
                                                .value
                                        )
                                    }
                                    placeholder="Search by title, description, or test type..."
                                />
                            </div>
                        </div>
                    </section>

                    {filteredTests.length ===
                        0 ? (
                        <div className="teacher-test-empty">
                            <FileText size={40} strokeWidth={1.5} />

                            <h2>
                                No tests found
                            </h2>

                            <p>
                                No tests match the current filters. Try a different keyword or import a new test.
                            </p>
                        </div>
                    ) : (
                        <section className="teacher-test-table-wrap">
                            <Table
                                className="teacher-test-table"
                                dataSource={filteredTests}
                                rowKey="testId"
                                tableLayout="fixed"
                                pagination={{
                                    pageSize: 10,
                                    showSizeChanger: true,
                                    showTotal: total => `${total} tests`
                                }}
                                columns={[
                                    {
                                        title: "Test",
                                        key: "test",
                                        width: 210,
                                        render: (_, test) => (
                                            <div className="teacher-test-table-title">
                                                <Tooltip title={test.title}>
                                                    <strong>{test.title}</strong>
                                                </Tooltip>
                                                <span>{test.description || "No description"}</span>
                                            </div>
                                        )
                                    },
                                    {
                                        title: "Type",
                                        dataIndex: "testType",
                                        key: "testType",
                                        width: 120,
                                        render: value => getTestTypeText(value)
                                    },
                                    {
                                        title: "Course",
                                        dataIndex: "courseType",
                                        key: "courseType",
                                        width: 130,
                                        render: value => (
                                            <Tag className={`teacher-test-course-badge ${getCourseTypeText(value).toLowerCase()}`}>
                                                {getCourseTypeText(value)}
                                            </Tag>
                                        )
                                    },
                                    {
                                        title: "Points",
                                        dataIndex: "totalPoints",
                                        key: "totalPoints",
                                        width: 100,
                                        render: value => `${value} points`
                                    },
                                    {
                                        title: "Duration",
                                        dataIndex: "durationMinutes",
                                        key: "durationMinutes",
                                        width: 120,
                                        render: value => `${value || 120} minutes`
                                    },
                                    {
                                        title: "Assignment",
                                        dataIndex: "assignmentCount",
                                        key: "assignmentCount",
                                        width: 120,
                                        render: value => (
                                            <Tag color={Number(value) > 0 ? "success" : "warning"}>
                                                {value} assignments
                                            </Tag>
                                        )
                                    },
                                    {
                                        title: "Created on",
                                        dataIndex: "createdAt",
                                        key: "createdAt",
                                        width: 175,
                                        render: value => value ? formatVietnamDateTime(value) : "-"
                                    },
                                    {
                                        title: "Actions",
                                        key: "actions",
                                        width: 150,
                                        render: (_, test) => (
                                            <Space size={4}>
                                                {test.hasSourceFile && (
                                                    <Tooltip title="View source">
                                                        <button
                                                            type="button"
                                                            className="teacher-test-table-action source"
                                                            onClick={() => handleViewTestSource(test)}
                                                            aria-label="View test source"
                                                        >
                                                            <FileText size={16} />
                                                        </button>
                                                    </Tooltip>
                                                )}
                                                <Tooltip title="Edit test">
                                                    <button
                                                        type="button"
                                                        className="teacher-test-table-action edit"
                                                        onClick={() => openEditModal(test)}
                                                        aria-label="Edit test"
                                                    >
                                                        <Pencil size={16} />
                                                    </button>
                                                </Tooltip>
                                                <Tooltip title="Delete test">
                                                    <button
                                                        type="button"
                                                        className="teacher-test-table-action delete"
                                                        onClick={() => handleDeleteTest(test)}
                                                        aria-label="Delete test"
                                                    >
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
                </div>
            </main>

            {showImportModal && (
                <div className="lesson-modal-overlay">
                    <div className="lesson-modal teacher-test-modal">
                        <div className="teacher-test-modal-header">
                            <div>
                                <span>
                                    Create New Test
                                </span>
                                <h2>
                                    Import Test
                                </h2>
                            </div>

                            <button
                                type="button"
                                className="teacher-test-modal-close"
                                onClick={() => {
                                    setShowImportModal(
                                        false
                                    );
                                    resetForm();
                                }}
                                aria-label="Close"
                            >
                                ×
                            </button>
                        </div>

                        <div className="lesson-form teacher-test-form">
                            <div className="teacher-test-form-section">
                                <h4 className="teacher-test-form-section-title">
                                    General Information
                                </h4>

                                <label>
                                    Test Title
                                </label>

                                <input
                                    placeholder="Enter test title"
                                    value={title}
                                    onChange={event =>
                                        setTitle(
                                            event.target
                                                .value
                                        )
                                    }
                                />

                                <label>Description</label>

                                <textarea
                                    placeholder="Enter test description"
                                    value={description}
                                    onChange={event =>
                                        setDescription(
                                            event.target
                                                .value
                                        )
                                    }
                                />

                                <div className="teacher-test-form-row">
                                    <div>
                                        <label>
                                            Class
                                        </label>

                                        <select
                                            value={classRoomId}
                                            onChange={event => {
                                                const selectedClassRoomId =
                                                    event.target
                                                        .value;

                                                setClassRoomId(
                                                    selectedClassRoomId
                                                );

                                                const selectedClass =
                                                    classes.find(
                                                        item =>
                                                            String(
                                                                item.classRoomId
                                                            ) ===
                                                            String(
                                                                selectedClassRoomId
                                                            )
                                                    );

                                                if (
                                                    selectedClass
                                                ) {
                                                    setCourseType(
                                                        getCourseTypeFromClass(
                                                            selectedClass
                                                        )
                                                    );

                                                    handleTestTypeChange(
                                                        getDefaultTestTypeFromClass(
                                                            selectedClass
                                                        )
                                                    );
                                                }
                                                else {
                                                    setCourseType(
                                                        ""
                                                    );
                                                    handleTestTypeChange("");
                                                }
                                            }}
                                        >
                                            <option value="">
                                                Select a class
                                            </option>

                                            {classes.map(
                                                classRoom => (
                                                    <option
                                                        key={
                                                            classRoom.classRoomId
                                                        }
                                                        value={
                                                            classRoom.classRoomId
                                                        }
                                                    >
                                                        {classRoom.className ||
                                                            classRoom.name ||
                                                            classRoom.title}
                                                        {" - "}
                                                        {classRoom.courseName ||
                                                            classRoom.courseTitle ||
                                                            classRoom.course
                                                                ?.name}
                                                    </option>
                                                )
                                            )}
                                        </select>
                                    </div>

                                    <div>
                                        <label>
                                            Test Type
                                        </label>

                                        <select
                                            value={testType}
                                            onChange={event =>
                                                handleTestTypeChange(
                                                    event.target
                                                        .value
                                                )
                                            }
                                        >
                                            <option value="">
                                                Select a test type
                                            </option>

                                            <option
                                                value={
                                                    TEST_TYPES.VstepFourSkills
                                                }
                                            >
                                                VSTEP 4 Skills
                                            </option>

                                            <option
                                                value={
                                                    TEST_TYPES.ToeicTwoSkills
                                                }
                                            >
                                                TOEIC Listening + Reading
                                            </option>

                                            <option
                                                value={
                                                    TEST_TYPES.ToeicFourSkills
                                                }
                                            >
                                                TOEIC 4 Skills
                                            </option>
                                        </select>
                                    </div>
                                </div>

                                <div className="teacher-test-form-row">
                                    <div>
                                        <label>
                                            Maximum Attempts
                                        </label>

                                        <input
                                            type="number"
                                            min="1"
                                            value={maxAttempts}
                                            onChange={event =>
                                                setMaxAttempts(
                                                    event.target
                                                        .value
                                                )
                                            }
                                        />
                                    </div>

                                    <div>
                                        <label>
                                            Test Duration (minutes)
                                        </label>

                                        <input
                                            type="number"
                                            min="1"
                                            value={durationMinutes}
                                            onChange={event =>
                                                setDurationMinutes(
                                                    event.target
                                                        .value
                                                )
                                            }
                                        />
                                    </div>

                                </div>

                                <div className="teacher-test-form-row">
                                    <div>
                                        <label>
                                            Test File
                                        </label>

                                        <input
                                            type="file"
                                            accept=".pdf,.docx,.txt"
                                            onChange={event =>
                                                setTestFile(
                                                    event.target
                                                        .files[0]
                                                )
                                            }
                                        />

                                        <span className="teacher-test-hint">
                                            Supports PDF, DOCX, and TXT
                                        </span>
                                    </div>
                                </div>
                            </div>

                            {testHasListening() && (
                                <div className="teacher-test-listening-box">
                                    <h3>
                                        <Headphones size={16} />
                                        Listening Audio Mapping
                                    </h3>

                                    <p>
                                        Enter the corresponding question count for each audio file.
                                        Example: [5, 5, 10, 10].
                                    </p>

                                    <label>
                                        Number of Audio Files
                                    </label>

                                    <input
                                        type="number"
                                        min="1"
                                        value={
                                            audioCount
                                        }
                                        onChange={event =>
                                            handleAudioCountChange(
                                                event.target
                                                    .value
                                            )
                                        }
                                    />

                                    {Array.from({
                                        length:
                                            audioCount
                                    }).map(
                                        (
                                            _,
                                            index
                                        ) => (
                                            <div
                                                key={
                                                    index
                                                }
                                                className="teacher-test-audio-card"
                                            >
                                                <strong>
                                                    Audio{" "}
                                                    {index +
                                                        1}
                                                </strong>

                                                <div className="teacher-test-form-row">
                                                    <div>
                                                        <label>
                                                            File audio
                                                        </label>

                                                        <input
                                                            type="file"
                                                            accept=".mp3,.wav,.m4a,.webm"
                                                            onChange={event =>
                                                                handleAudioFileChange(
                                                                    index,
                                                                    event
                                                                        .target
                                                                        .files[0]
                                                                )
                                                            }
                                                        />
                                                    </div>

                                                    <div>
                                                        <label>
                                                            Questions for audio{" "}
                                                            {index +
                                                                1}
                                                        </label>

                                                        <input
                                                            type="number"
                                                            min="1"
                                                            value={
                                                                questionCounts[
                                                                index
                                                                ] ||
                                                                ""
                                                            }
                                                            onChange={event =>
                                                                handleQuestionCountChange(
                                                                    index,
                                                                    event
                                                                        .target
                                                                        .value
                                                                )
                                                            }
                                                            placeholder="e.g. 5"
                                                        />
                                                    </div>
                                                </div>
                                            </div>
                                        )
                                    )}

                                    <label>
                                        Transcript (optional)
                                    </label>

                                    <textarea
                                        placeholder="Enter transcript"
                                        value={
                                            transcript
                                        }
                                        onChange={event =>
                                            setTranscript(
                                                event
                                                    .target
                                                    .value
                                            )
                                        }
                                    />
                                </div>
                            )}

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
                                    Generate explanations for this test
                                    <small>
                                        Clear this option to parse the test without generating AI explanations.
                                    </small>
                                </span>
                            </label>

                            <div className="teacher-test-modal-actions">
                                <button
                                    type="button"
                                    className="teacher-test-cancel-btn"
                                    onClick={() => {
                                        setShowImportModal(
                                            false
                                        );
                                        resetForm();
                                    }}
                                >
                                    Close
                                </button>

                                <button
                                    type="button"
                                    className="teacher-test-submit-btn"
                                    onClick={
                                        handleImportTest
                                    }
                                >
                                    <Plus
                                        size={17}
                                    />
                                    Create Test
                                </button>
                            </div>
                        </div>
                    </div>
                </div>
            )}

            {showEditModal && (
                <div className="lesson-modal-overlay">
                    <div className="lesson-modal teacher-test-modal teacher-test-edit-modal">
                        <div className="teacher-test-modal-header">
                            <div>
                                <span>
                                    Edit Information
                                </span>
                                <h2>
                                    Edit Test
                                </h2>
                            </div>

                            <button
                                type="button"
                                className="teacher-test-modal-close"
                                onClick={() => {
                                    setShowEditModal(
                                        false
                                    );
                                    setEditingTest(
                                        null
                                    );
                                }}
                                aria-label="Close"
                            >
                                ×
                            </button>
                        </div>

                        <div className="lesson-form teacher-test-form">
                            <label>
                                Test Title
                            </label>

                            <input
                                placeholder="Test title"
                                value={editTitle}
                                onChange={event =>
                                    setEditTitle(
                                        event.target
                                            .value
                                    )
                                }
                            />

                            <label>Description</label>

                            <textarea
                                placeholder="Description"
                                value={
                                    editDescription
                                }
                                onChange={event =>
                                    setEditDescription(
                                        event.target
                                            .value
                                    )
                                }
                            />

                            <label>
                                Maximum Attempts
                            </label>

                            <input
                                type="number"
                                min="1"
                                value={
                                    editMaxAttempts
                                }
                                onChange={event =>
                                    setEditMaxAttempts(
                                        event.target
                                            .value
                                    )
                                }
                            />

                            <label>
                                Test Duration (minutes)
                            </label>

                            <input
                                type="number"
                                min="1"
                                value={
                                    editDurationMinutes
                                }
                                onChange={event =>
                                    setEditDurationMinutes(
                                        event.target
                                            .value
                                    )
                                }
                            />

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
                                    Generate explanations for this test
                                    <small>
                                        Only questions without explanations will be updated.
                                    </small>
                                </span>
                            </label>

                            <div className="teacher-test-modal-actions">
                                <button
                                    type="button"
                                    className="teacher-test-cancel-btn"
                                    onClick={() => {
                                        setShowEditModal(
                                            false
                                        );
                                        setEditingTest(
                                            null
                                        );
                                    }}
                                >
                                    Close
                                </button>

                                <button
                                    type="button"
                                    className="teacher-test-submit-btn"
                                    onClick={
                                        handleUpdateTest
                                    }
                                >
                                    Save Changes
                                </button>
                            </div>
                        </div>
                    </div>
                </div>
            )}
        </div>
    );
}

export default TeacherTestPage;
