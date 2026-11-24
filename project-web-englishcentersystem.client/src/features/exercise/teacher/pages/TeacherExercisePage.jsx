import {
    useEffect,
    useMemo,
    useState
} from "react";

import {
    BookOpen,
    ChevronDown,
    ChevronRight,
    ChevronUp,
    ClipboardList,
    FileText,
    Headphones,
    Mic2,
    Pencil,
    PenLine,
    Plus,
    Search,
    Trash2,
    Upload,
    X
} from "lucide-react";
import TeacherSidebar
    from "../../../lesson/teacher/TeacherSidebar";
import {
    getMyTeacherClassesApi
} from "../../../lesson/api/lessonApi";
import {
    importAssignExerciseApi,
    importAssignListeningExerciseApi,
    getBackgroundJobApi,
    importAssignReadingExerciseApi,
    importAssignWritingExerciseApi,
    importAssignSpeakingExerciseApi
} from "../api/exerciseImportApi";
import {
    createExerciseAssignmentApi

} from "../api/exerciseAssignmentApi";

import {
    getExerciseCategoriesApi,
    getExerciseSectionsApi,
    createExerciseSectionApi,
    updateExerciseSectionApi,
    deleteExerciseSectionApi
} from "../api/exerciseStructureApi";
import {
    deleteExerciseApi,
    getExerciseSourceFileApi,
    getExercisesApi,
    updateExerciseMetadataApi
} from "../api/exerciseApi";

import "../../../lesson/styles/lesson.css";
import "../styles/questionBank.css";
import "../styles/teacherExercise.css";

function formatGroqImportError(
    errorMessage,
    fallbackMessage
) {
    const normalizedError =
        String(errorMessage || "").toLowerCase();

    if (
        normalizedError.includes(
            "groq_audio_daily_limit"
        ) ||
        normalizedError.includes(
            "audio seconds per day"
        ) ||
        normalizedError.includes("asd")
    ) {
        return "Failed to create the exercise because Groq Whisper's daily audio quota has been exhausted. Please wait for the quota to reset or use a different API key/project.";
    }

    if (
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
        return "Failed to create the exercise because Groq's daily quota has been exhausted. Please wait for the quota to reset or use a different API key, project, or model.";
    }

    if (
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
        return "Failed to create the exercise because Groq is temporarily rate-limited. Please wait a few minutes and import again.";
    }

    return errorMessage || fallbackMessage;
}

function getApiErrorMessage(error) {
    return (
        error.response?.data?.Message ||
        error.response?.data?.message ||
        error.response?.data?.detail ||
        error.response?.data?.title ||
        ""
    );
}

function TeacherExercisePage() {
    const COURSE_TYPES = {
        TOEIC: 1,
        VSTEP: 2,
        Communication: 3
    };



    const [exercises, setExercises] =
        useState([]);

    const handleViewExerciseSource = async exercise => {
        const previewWindow = window.open("about:blank", "_blank");

        if (!previewWindow) {
            alert("Please allow pop-ups to view the exercise source.");
            return;
        }

        previewWindow.opener = null;
        previewWindow.document.title = "Opening exercise source...";
        previewWindow.document.body.innerHTML =
            "<p style='font-family:sans-serif;padding:24px;'>Loading exercise source file...</p>";

        try {
            const result = await getExerciseSourceFileApi(exercise.exerciseId);

            if (!result?.fileUrl) {
                throw new Error("Exercise source file not found");
            }

            previewWindow.location.replace(result.fileUrl);
        } catch (error) {
            previewWindow.close();
            alert(error.response?.data?.message || "Unable to open the exercise source file.");
        }
    };
    const [classes, setClasses] =
        useState([]);
    const [exerciseCategories, setExerciseCategories] =
        useState([]);

    const [activeExerciseCategoryId, setActiveExerciseCategoryId] =
        useState("");

    const [activeClassRoomId, setActiveClassRoomId] =
        useState("");

    const [activeCourseId, setActiveCourseId] =
        useState("");

    const [exerciseSections, setExerciseSections] =
        useState([]);
    const [collapsedSectionIds, setCollapsedSectionIds] =
        useState([]);
    const [listeningAudioCount, setListeningAudioCount] =
        useState(1);
    const [listeningImportJobId, setListeningImportJobId] =
        useState(null);

    const [listeningImportStatus, setListeningImportStatus] =
        useState("");
    const [listeningAudioFiles, setListeningAudioFiles] =
        useState([]);
    const [
        showEditExerciseModal,
        setShowEditExerciseModal
    ] = useState(false);

    const [
        editingExercise,
        setEditingExercise
    ] = useState(null);

    const [
        editExerciseTitle,
        setEditExerciseTitle
    ] = useState("");

    const [
        editExerciseDescription,
        setEditExerciseDescription
    ] = useState("");

    const [
        editExerciseMaxAttempts,
        setEditExerciseMaxAttempts
    ] = useState(1);
    const [
        editGenerateExplanations,
        setEditGenerateExplanations
    ] = useState(false);
    const [questionCounts, setQuestionCounts] =
        useState([""]);
    const [editingSectionId, setEditingSectionId] =
        useState(null);

    const [editingSectionHasExercises, setEditingSectionHasExercises] =
        useState(false);
    const [listeningQuestionFile, setListeningQuestionFile] =
        useState(null);

    const [transcript, setTranscript] =
        useState("");


    const [showSectionModal, setShowSectionModal] =
        useState(false);


    const [importFile, setImportFile] =
        useState(null);
    const [sectionTitle, setSectionTitle] =
        useState("");
    const [sectionClassRoomId, setSectionClassRoomId] =
        useState("");

    const [sectionCourseId, setSectionCourseId] =
        useState("");

    const [sectionCategoryId, setSectionCategoryId] =
        useState("");

    const [showAssignModal, setShowAssignModal] =
        useState(false);

    const [assignExerciseId, setAssignExerciseId] =
        useState("");

    const [assignClassRoomId, setAssignClassRoomId] =
        useState("");



    const [maxAttempts, setMaxAttempts] =
        useState(1);




    const [showModal, setShowModal] =
        useState(false);
    const [searchKeyword, setSearchKeyword] =
        useState("");

    const [activeSkillFilter, setActiveSkillFilter] =
        useState("all");


    const [title, setTitle] =
        useState("");

    const [description, setDescription] =
        useState("");

    const [generateExplanations, setGenerateExplanations] =
        useState(false);

    const [courseType, setCourseType] =
        useState("");

    const [skill] =
        useState("");



    const [createClassRoomId, setCreateClassRoomId] =
        useState("");

    const [createCourseId, setCreateCourseId] =
        useState("");

    const [createCourseType, setCreateCourseType] =
        useState("");

    const [createSkill, setCreateSkill] =
        useState("");

    const [createExerciseSectionId, setCreateExerciseSectionId] =
        useState("");

    const [createSections, setCreateSections] =
        useState([]);

    const getCourseTypeFromClass = (classRoom) => {
        const name = (
            classRoom?.courseName ||
            classRoom?.courseTitle ||
            classRoom?.course?.name ||
            ""
        ).toLowerCase();

        if (name.includes("toeic")) {
            return COURSE_TYPES.TOEIC;
        }

        if (name.includes("vstep")) {
            return COURSE_TYPES.VSTEP;
        }

        if (name.includes("communication")) {
            return COURSE_TYPES.Communication;
        }

        return COURSE_TYPES.TOEIC;
    };
    const loadCreateSections = async (
        courseId,
        classRoomId,
        categoryId
    ) => {
        if (!courseId || !classRoomId || !categoryId) {
            setCreateSections([]);
            return;
        }

        const data = await getExerciseSectionsApi(
            courseId,
            classRoomId,
            categoryId
        );

        setCreateSections(data);
    };
    const exerciseSectionsWithSets = exerciseSections.map(section => ({
        ...section,
        exercises: exercises.filter(exercise =>
            Number(exercise.exerciseSectionId) ===
            Number(section.exerciseSectionId)
        )
    }));

    const exercisesWithoutSection = exercises.filter(exercise =>
        !exercise.exerciseSectionId
    );


    const loadExercises = async () => {
        const data = await getExercisesApi(
            courseType,
            skill

        );

        setExercises(data);
    };





    const loadExerciseCategories = async () => {
        const data = await getExerciseCategoriesApi();

        setExerciseCategories(data);


    };

    const loadExerciseSections = async (
        courseId = activeCourseId,
        classRoomId = activeClassRoomId,
        categoryId =
            activeExerciseCategoryId
    ) => {
        const data =
            await getExerciseSectionsApi(
                courseId || null,
                classRoomId || null,
                categoryId || null
            );

        setExerciseSections(data);
    };


    const loadClasses = async () => {
        const data = await getMyTeacherClassesApi();
        setClasses(data);
    };
    const handleUploadDraft = async () => {
        if (!title?.trim()) {
            alert("Please enter an exercise title.");
            return;
        }
        if (!importFile) {
            alert("Please select a file.");
            return;
        }

        if (!createClassRoomId) {
            alert("Please select a class to assign.");
            return;
        }

        if (!createExerciseSectionId) {
            alert("Please select a section before importing.");
            return;
        }

        if (!createSkill) {
            alert("Please select a skill.");
            return;
        }





        const formData = new FormData();

        formData.append("file", importFile);
        formData.append("title", title);
        formData.append("description", description || "");

        formData.append("courseType", Number(createCourseType));
        formData.append(
            "skill",
            Number(createSkill)
        );

        formData.append(
            "exerciseSectionId",
            Number(createExerciseSectionId)
        );

        formData.append("classRoomId", Number(createClassRoomId));

        formData.append("maxAttempts", Number(maxAttempts));
        formData.append("generateExplanations", generateExplanations);

        try {
            await importAssignExerciseApi(formData);

            alert("Exercise created and assigned successfully.");

            setImportFile(null);
            setShowModal(false);
            resetForm();

            await loadExercises();

        }
        catch (error) {
            console.log("Import assign error:", error.response?.data);

            alert(
                formatGroqImportError(
                    getApiErrorMessage(error),
                "Failed to import and assign the exercise."
                )
            );
        }
    };
    const handleDeleteSection = async sectionId => {
        if (!window.confirm(
            "Delete this exercise section? All exercises, attempts, and results within it will be permanently deleted."
        )) {
            return;
        }

        try {
            await deleteExerciseSectionApi(sectionId);

            await loadExerciseSections();
            await loadExercises();

            alert("Exercise section deleted successfully.");
        }
        catch (error) {
            console.error(
                "Delete section error:",
                error.response?.data || error
            );

            alert(
                error.response?.data?.message ||
                error.response?.data?.detail ||
                error.response?.data?.title ||
                "Unable to delete the exercise section."
            );
        }
    };
    const handleUploadSpeaking = async () => {
        if (!title.trim()) {
            alert("Please enter a speaking exercise title.");
            return;
        }

        if (!createClassRoomId) {
            alert("Please select a class to assign.");
            return;
        }

        if (!createExerciseSectionId) {
            alert("Please select a section.");
            return;
        }

        if (!importFile) {
            alert("Please select a Speaking PDF, Word, or TXT file.");
            return;
        }

        const formData = new FormData();

        formData.append("questionFile", importFile);
        formData.append("title", title);
        formData.append("description", description || "");
        formData.append("courseType", Number(createCourseType));
        formData.append(
            "exerciseSectionId",
            Number(createExerciseSectionId)
        );
        formData.append("classRoomId", Number(createClassRoomId));
        formData.append("maxAttempts", Number(maxAttempts));

        formData.append("preparationTimeSeconds", 60);
        formData.append("speakingTimeSeconds", 720);

        try {
            await importAssignSpeakingExerciseApi(formData);

            alert("Speaking exercise created and assigned successfully.");

            setShowModal(false);
            resetForm();

            await loadExercises();

        }
        catch (error) {
            console.log("Speaking import error:", error.response?.data);

            alert(
                formatGroqImportError(
                    getApiErrorMessage(error),
                    "Failed to create the Speaking exercise."
                )
            );
        }
    };
    const handleUploadWriting = async () => {
        if (!title.trim()) {
            alert("Please enter a writing exercise title.");
            return;
        }

        if (!createClassRoomId) {
            alert("Please select a class to assign.");
            return;
        }

        if (!createExerciseSectionId) {
            alert("Please select a section.");
            return;
        }

        if (!importFile) {
            alert("Please select a Writing PDF, Word, or TXT file.");
            return;
        }


        const formData = new FormData();

        formData.append("questionFile", importFile);
        formData.append("title", title);
        formData.append("description", description || "");
        formData.append("courseType", Number(createCourseType));
        formData.append(
            "exerciseSectionId",
            Number(createExerciseSectionId)
        );
        formData.append("classRoomId", Number(createClassRoomId));

        formData.append("maxAttempts", Number(maxAttempts));

        try {
            await importAssignWritingExerciseApi(formData);

            alert("Writing exercise created and assigned successfully.");

            setShowModal(false);
            resetForm();

            await loadExercises();

        }
        catch (error) {
            console.log("Writing import error:", error.response?.data);

            alert(
                formatGroqImportError(
                    getApiErrorMessage(error),
                    "Failed to create the Writing exercise."
                )
            );
        }
    };
    const openCreateSectionModal = () => {
        setEditingSectionId(null);
        setEditingSectionHasExercises(false);

        setSectionTitle("");
        setSectionClassRoomId("");
        setSectionCourseId("");
        setSectionCategoryId("");

        setShowSectionModal(true);
    };
    const closeCreateSectionModal = () => {
        setEditingSectionId(null);
        setEditingSectionHasExercises(false);

        setSectionTitle("");
        setSectionClassRoomId("");
        setSectionCourseId("");
        setSectionCategoryId("");

        setShowSectionModal(false);
    };
    const handleCreateSection = async () => {
        if (!sectionTitle.trim()) {
            alert("Please enter a section title.");
            return;
        }

        if (!sectionClassRoomId) {
            alert("Please select a class.");
            return;
        }

        if (!sectionCourseId) {
            alert(
                "Unable to identify the class course."
            );
            return;
        }

        if (!sectionCategoryId) {
            alert("Please select a skill.");
            return;
        }

        try {
            await createExerciseSectionApi({
                title: sectionTitle.trim(),
                courseId:
                    Number(sectionCourseId),
                classRoomId:
                    Number(sectionClassRoomId),
                exerciseCategoryId:
                    Number(sectionCategoryId)
            });

            alert(
                "Exercise section created successfully."
            );
            const createdClassRoomId =
                sectionClassRoomId;

            const createdCourseId =
                sectionCourseId;

            const createdCategoryId =
                sectionCategoryId;
            closeCreateSectionModal();

            await loadExerciseSections(
                createdCourseId,
                createdClassRoomId,
                createdCategoryId
            );
        }
        catch (error) {
            console.log(
                "Create exercise section error:",
                error.response?.data
            );

            alert(
                error.response?.data?.message ||
                error.response?.data?.title ||
                "Failed to create the exercise section."
            );
        }
    };
    useEffect(() => {
        loadExercises();

        loadExerciseCategories();


        loadClasses();

    }, [
        courseType,
        skill

    ]);
    useEffect(() => {
        loadExerciseSections();
    }, [
        activeCourseId,
        activeClassRoomId,
        activeExerciseCategoryId
    ]);

    const openAssignModal = (exerciseId) => {
        setAssignExerciseId(exerciseId);
        setAssignClassRoomId("");

        setMaxAttempts(100);
        setShowAssignModal(true);
    };
    useEffect(() => {
        if (!listeningImportJobId) {
            return;
        }

        const intervalId = setInterval(async () => {
            try {
                const response = await getBackgroundJobApi(
                    listeningImportJobId
                );

                const job = response.data;

                setListeningImportStatus(String(job.status));

                if (Number(job.status) === 3) {
                    clearInterval(intervalId);

                    alert("Listening exercise created and assigned successfully.");

                    setListeningImportJobId(null);
                    setListeningImportStatus("");

                    await loadExercises();

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
                        normalizedError.includes("asd")
                    ) {
                        alert(
                            "Failed to create the exercise because Groq Whisper's daily audio quota has been exhausted. Please wait for the quota to reset or use a different API key/project."
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
                            "Failed to create the exercise because Groq's daily quota has been exhausted. Please wait for the quota to reset or use a different API key, project, or model."
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
                            "Failed to create the exercise because Groq is temporarily rate-limited. Please wait a few minutes and import again."
                        );
                    }
                    else {
                        alert(
                            errorMessage ||
                            "Failed to create the Listening exercise."
                        );
                    }

                    setListeningImportJobId(null);
                    setListeningImportStatus("");
                }
            }
            catch (error) {
                console.log("Background job polling error:", error);
            }
        }, 5000);

        return () => clearInterval(intervalId);
    }, [listeningImportJobId]);
    const handleAssign = async () => {
        if (!assignExerciseId || !assignClassRoomId) {
            alert("Please select an exercise and a class.");
            return;
        }


        try {
            await createExerciseAssignmentApi({
                exerciseId: Number(assignExerciseId),
                classRoomId: Number(assignClassRoomId),

                maxAttempts: Number(maxAttempts)
            });

            setShowAssignModal(false);

        }
        catch (error) {
            console.log("Assign error:", error.response?.data);

            alert(
                error.response?.data?.message ||
                error.response?.data?.title ||
                "Failed to assign the exercise."
            );
        }
    };



    const resetForm = () => {
        setTitle("");
        setDescription("");


        setImportFile(null);

        setCreateClassRoomId("");
        setCreateCourseId("");
        setCreateCourseType("");
        setListeningAudioCount(1);
        setListeningAudioFiles([null]);
        setQuestionCounts([""]);
        setListeningQuestionFile(null);
        setTranscript("");
        setCreateSkill("");
        setCreateExerciseSectionId("");
        setCreateSections([]);
        setGenerateExplanations(false);
    };

    const closeEditExerciseModal =
        () => {
            setShowEditExerciseModal(
                false
            );

            setEditingExercise(null);

            setEditExerciseTitle("");

            setEditExerciseDescription("");

            setEditExerciseMaxAttempts(1);
            setEditGenerateExplanations(false);
        };
    const handleUpdateExercise =
        async () => {
            if (!editingExercise) {
                return;
            }

            if (!editExerciseTitle.trim()) {
                alert(
                    "Please enter an exercise title."
                );
                return;
            }

            if (
                Number(
                    editExerciseMaxAttempts
                ) <= 0
            ) {
                alert(
                    "The maximum number of attempts must be greater than 0."
                );
                return;
            }

            try {
                await updateExerciseMetadataApi(
                    editingExercise.exerciseId,
                    {
                        title:
                            editExerciseTitle
                                .trim(),

                        description:
                            editExerciseDescription
                                .trim(),

                        maxAttempts:
                            Number(
                                editExerciseMaxAttempts
                            ),

                        generateExplanations:
                            editGenerateExplanations
                    }
                );

                closeEditExerciseModal();

                await loadExercises();

                alert(
                    "Exercise updated successfully."
                );
            }
            catch (error) {
                console.error(
                    "Update Exercise Set error:",
                    error.response?.data ||
                    error
                );

                alert(
                    error.response?.data
                        ?.message ||
                    error.response?.data
                        ?.detail ||
                    error.response?.data
                        ?.title ||
                    "Failed to update the exercise."
                );
            }
        };
    const openEditExerciseModal =
        exercise => {
            setEditingExercise(
                exercise
            );

            setEditExerciseTitle(
                exercise.title || ""
            );

            setEditExerciseDescription(
                exercise.description || ""
            );

            setEditExerciseMaxAttempts(
                exercise.maxAttempts || 1
            );

            setEditGenerateExplanations(false);

            setShowEditExerciseModal(
                true
            );
        };
    const handleUploadListening = async () => {
        if (!title.trim()) {
            alert("Please enter a listening exercise title.");
            return;
        }

        if (!createClassRoomId) {
            alert("Please select a class to assign.");
            return;
        }

        if (!createExerciseSectionId) {
            alert("Please select a section.");
            return;
        }

        if (
            listeningAudioFiles.length !== listeningAudioCount ||
            listeningAudioFiles.some(file => !file)
        ) {
            alert("Please select all audio files.");
            return;
        }

        if (
            questionCounts.length !== listeningAudioCount ||
            questionCounts.some(x => !x || Number(x) <= 0)
        ) {
            alert("Please enter the question count for each audio file.");
            return;
        }

        if (!listeningQuestionFile) {
            alert("Please select a question file.");
            return;
        }

        
       
        const formData = new FormData();

        listeningAudioFiles.forEach(file => {
            formData.append("audioFiles", file);
        });

        formData.append(
            "questionCountsJson",
            JSON.stringify(questionCounts.map(Number))
        );
        formData.append(
            "questionCounts",
            JSON.stringify(questionCounts)
        );
        formData.append("questionFile", listeningQuestionFile);
        formData.append("title", title);
        formData.append("description", description || "");
        formData.append("transcript", transcript || "");
        formData.append("courseType", Number(createCourseType));

        formData.append(
            "exerciseSectionId",
            Number(createExerciseSectionId)
        );
        formData.append("classRoomId", Number(createClassRoomId));

        formData.append("maxAttempts", Number(maxAttempts));
        formData.append("generateExplanations", generateExplanations);

        try {
            const response = await importAssignListeningExerciseApi(formData);

            const backgroundJobId = response.data?.backgroundJobId;

            if (!backgroundJobId) {
                alert("Unable to retrieve the background job ID.");
                return;
            }

            setListeningImportJobId(backgroundJobId);
            setListeningImportStatus("1");

            alert("The listening exercise has been queued for processing.");

            setShowModal(false);
            resetForm();
        }
        catch (error) {
            console.log("Listening import error:", error.response?.data);

            alert(
                formatGroqImportError(
                    getApiErrorMessage(error),
                    "Failed to create the Listening exercise."
                )
            );
        }
    };
    const openEditSectionModal = section => {
        const hasExercises =
            exercises.some(exercise =>
                Number(
                    exercise.exerciseSectionId
                ) ===
                Number(
                    section.exerciseSectionId
                )
            );

        setEditingSectionId(
            section.exerciseSectionId
        );

        setEditingSectionHasExercises(
            hasExercises
        );

        setSectionTitle(
            section.title || ""
        );

        setSectionClassRoomId(
            String(section.classRoomId || "")
        );

        setSectionCourseId(
            String(section.courseId || "")
        );

        setSectionCategoryId(
            String(
                section.exerciseCategoryId || ""
            )
        );

        setShowSectionModal(true);
    };
    const handleUploadReading = async () => {
        if (!title.trim()) {
            alert("Please enter a reading exercise title.");
            return;
        }

        if (!createClassRoomId) {
            alert("Please select a class to assign.");
            return;
        }

        if (!createExerciseSectionId) {
            alert("Please select a section.");
            return;
        }

        if (!importFile) {
            alert("Please select a Reading PDF, Word, or TXT file.");
            return;
        }



        const formData = new FormData();

        formData.append("questionFile", importFile);
        formData.append("title", title);
        formData.append("description", description || "");
        formData.append("courseType", Number(createCourseType));
        formData.append(
            "exerciseSectionId",
            Number(createExerciseSectionId)
        );
        formData.append("classRoomId", Number(createClassRoomId));

        formData.append("maxAttempts", Number(maxAttempts));
        formData.append("generateExplanations", generateExplanations);

        try {
            await importAssignReadingExerciseApi(formData);

            alert("Reading exercise created and assigned successfully.");

            setShowModal(false);
            resetForm();

            await loadExercises();

        }
        catch (error) {
            console.log("Reading import error:", error.response?.data);

            alert(
                formatGroqImportError(
                    getApiErrorMessage(error),
                    "Failed to create the Reading exercise."
                )
            );
        }
    };
    const handleDelete = async (exerciseId) => {
        if (!window.confirm("Delete this exercise?")) {
            return;
        }

        await deleteExerciseApi(exerciseId);
        loadExercises();
    };

    const normalizeSkillName = value =>
        String(value || "")
            .trim()
            .toLowerCase();

    const getSkillConfig = value => {
        const normalized =
            normalizeSkillName(value);

        if (
            normalized.includes("grammar") ||
            normalized.includes("ngữ pháp")
        ) {
            return {
                key: "grammar",
                label: "Grammar",
                icon: BookOpen
            };
        }

        if (
            normalized.includes("listening") ||
            normalized.includes("nghe")
        ) {
            return {
                key: "listening",
                label: "Listening",
                icon: Headphones
            };
        }

        if (
            normalized.includes("speaking") ||
            normalized.includes("nói")
        ) {
            return {
                key: "speaking",
                label: "Speaking",
                icon: Mic2
            };
        }

        if (
            normalized.includes("reading") ||
            normalized.includes("đọc")
        ) {
            return {
                key: "reading",
                label: "Reading",
                icon: FileText
            };
        }

        if (
            normalized.includes("writing") ||
            normalized.includes("viết")
        ) {
            return {
                key: "writing",
                label: "Writing",
                icon: PenLine
            };
        }

        return {
            key: "other",
            label: value || "Skill",
            icon: ClipboardList
        };
    };

    const getCategoryById = categoryId =>
        exerciseCategories.find(category =>
            Number(category.exerciseCategoryId) ===
            Number(categoryId)
        );

    const getSectionSkillConfig = section => {
        const category =
            getCategoryById(
                section.exerciseCategoryId
            );

        return getSkillConfig(
            category?.name ||
            section.exerciseCategoryName ||
            section.categoryName
        );
    };

    const getExerciseSkillConfig = exercise => {
        const categoryId =
            exercise.exerciseCategoryId ||
            exercise.categoryId ||
            exercise.exerciseCategory
                ?.exerciseCategoryId;

        const category =
            getCategoryById(categoryId);

        return getSkillConfig(
            category?.name ||
            exercise.skillName ||
            exercise.exerciseCategoryName ||
            exercise.categoryName
        );
    };

    const getClassRoomName = section => {
        return (
            section.classRoomName ||
            section.className ||
            classes.find(classRoom =>
                Number(classRoom.classRoomId) ===
                Number(section.classRoomId)
            )?.className ||
            "Class"
        );
    };
    const exerciseStatistics =
        useMemo(() => {
            const counts = {
                total: exercises.length,
                grammar: 0,
                listening: 0,
                speaking: 0,
                reading: 0,
                writing: 0
            };

            exercises.forEach(exercise => {
                const key =
                    getExerciseSkillConfig(
                        exercise
                    ).key;

                if (
                    Object.prototype
                        .hasOwnProperty.call(
                            counts,
                            key
                        )
                ) {
                    counts[key] += 1;
                }
            });

            return counts;
        }, [
            exercises,
            exerciseCategories
        ]);
    const handleUpdateSection = async () => {
        if (!editingSectionId) {
            return;
        }

        if (!sectionTitle.trim()) {
            alert("Please enter a section title.");
            return;
        }

        if (!sectionClassRoomId) {
            alert("Please select a class.");
            return;
        }

        if (!sectionCourseId) {
            alert(
                "Unable to identify the class course."
            );
            return;
        }

        if (!sectionCategoryId) {
            alert("Please select a skill.");
            return;
        }

        try {
            await updateExerciseSectionApi(
                editingSectionId,
                {
                    title:
                        sectionTitle.trim(),

                    courseId:
                        Number(sectionCourseId),

                    classRoomId:
                        Number(sectionClassRoomId),

                    exerciseCategoryId:
                        Number(sectionCategoryId)
                }
            );

            closeCreateSectionModal();

            await loadExerciseSections();
            await loadExercises();

            alert(
                "Exercise section updated successfully."
            );
        }
        catch (error) {
            console.error(
                "Update exercise section error:",
                error.response?.data || error
            );

            alert(
                error.response?.data?.message ||
                error.response?.data?.detail ||
                error.response?.data?.title ||
                "Failed to update the exercise section."
            );
        }
    };
    const skillTabs =
        useMemo(() => [
            {
                key: "all",
            label: "All",
                count: exerciseStatistics.total
            },
            {
                key: "grammar",
            label: "Grammar",
                count: exerciseStatistics.grammar
            },
            {
                key: "listening",
            label: "Listening",
                count: exerciseStatistics.listening
            },
            {
                key: "speaking",
            label: "Speaking",
                count: exerciseStatistics.speaking
            },
            {
                key: "reading",
            label: "Reading",
                count: exerciseStatistics.reading
            },
            {
                key: "writing",
            label: "Writing",
                count: exerciseStatistics.writing
            }
        ], [exerciseStatistics]);

    const visibleExerciseSections =
        useMemo(() => {
            const keyword =
                searchKeyword
                    .trim()
                    .toLowerCase();

            return exerciseSectionsWithSets
                .map(section => {
                    const sectionSkill =
                        getSectionSkillConfig(
                            section
                        );

                    const matchingExercises =
                        section.exercises.filter(
                            exercise => {
                                const exerciseSkill =
                                    getExerciseSkillConfig(
                                        exercise
                                    );

                                const matchesSkill =
                                    activeSkillFilter ===
                                    "all" ||
                                    exerciseSkill.key ===
                                    activeSkillFilter ||
                                    sectionSkill.key ===
                                    activeSkillFilter;

                                const matchesKeyword =
                                    !keyword ||
                                    [
                                        exercise.title,
                                        exercise.description,
                                        section.title,
                                        sectionSkill.label,
                                        getClassRoomName(
                                            section
                                        )
                                    ]
                                        .filter(Boolean)
                                        .some(value =>
                                            String(value)
                                                .toLowerCase()
                                                .includes(
                                                    keyword
                                                )
                                        );

                                return (
                                    matchesSkill &&
                                    matchesKeyword
                                );
                            }
                        );

                    return {
                        ...section,
                        exercises:
                            matchingExercises,
                        skillConfig:
                            sectionSkill
                    };
                })
                .filter(section => {
                    const matchesSkill =
                        activeSkillFilter ===
                        "all" ||
                        section.skillConfig.key ===
                        activeSkillFilter;

                    const sectionMatchesKeyword =
                        !keyword ||
                        [
                            section.title,
                            section.skillConfig
                                .label,
                            getClassRoomName(
                                section
                            )
                        ]
                            .filter(Boolean)
                            .some(value =>
                                String(value)
                                    .toLowerCase()
                                    .includes(keyword)
                            );



                    return (
                        section.exercises.length > 0 ||
                        (
                            matchesSkill &&
                            sectionMatchesKeyword
                        )
                    );
                });
        }, [
            exerciseSectionsWithSets,
            exerciseCategories,
            classes,
            searchKeyword,
            activeSkillFilter,

        ]);

    return (
        <div className="lesson-layout">
            <TeacherSidebar />

            <main className="lesson-main teacher-exercise-main">
                <div className="teacher-exercise-page">
                    <div className="teacher-exercise-breadcrumb">
                        <span>Home</span>
                        <ChevronRight size={16} />
                        <strong>Exercises</strong>
                    </div>

                    <section className="teacher-exercise-header">
                        <div>
                            <div className="teacher-exercise-title-row">
                                <div className="teacher-exercise-title-icon">
                                    <ClipboardList size={28} />
                                </div>

                                <h1>Exercises</h1>
                            </div>

                            <p>
                                Create exercise sets for Grammar,
                                Listening, Speaking, Reading, or Writing
                                and assign them to classes
                            </p>
                        </div>

                        <button
                            type="button"
                            className="teacher-exercise-create-section-btn"
                            onClick={openCreateSectionModal}
                        >
                            <Plus size={19} />
                            Create Exercise Section
                        </button>
                    </section>

                    <section className="teacher-exercise-selectors">
                        <select
                            value={activeClassRoomId}
                            onChange={event => {
                                const classRoomId =
                                    event.target.value;

                                setActiveClassRoomId(
                                    classRoomId
                                );

                                const selectedClass =
                                    classes.find(item =>
                                        String(
                                            item.classRoomId
                                        ) ===
                                        String(classRoomId)
                                    );

                                setActiveCourseId(
                                    selectedClass?.courseId ||
                                    ""
                                );

                                if (selectedClass) {
                                    setCourseType(
                                        getCourseTypeFromClass(
                                            selectedClass
                                        )
                                    );
                                }
                                else {
                                    setCourseType("");
                                }
                            }}
                        >
                            <option value="">
                                All classes
                            </option>

                            {classes.map(classRoom => (
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
                                        classRoom.course?.name}
                                </option>
                            ))}
                        </select>

                        <select
                            value={
                                activeExerciseCategoryId
                            }
                            onChange={event => {
                                const categoryId =
                                    event.target.value;

                                setActiveExerciseCategoryId(
                                    categoryId
                                );

                                const category =
                                    exerciseCategories.find(
                                        item =>
                                            String(
                                                item
                                                    .exerciseCategoryId
                                            ) ===
                                            String(
                                                categoryId
                                            )
                                    );

                                setActiveSkillFilter(
                                    category
                                        ? getSkillConfig(
                                            getSkillConfig(category.name).label
                                        ).key
                                        : "all"
                                );
                            }}
                        >
                            <option value="">
                                All skills
                            </option>

                            {exerciseCategories.map(
                                category => (
                                    <option
                                        key={
                                            category
                                                .exerciseCategoryId
                                        }
                                        value={
                                            category
                                                .exerciseCategoryId
                                        }
                                    >
                                        {getSkillConfig(category.name).label}
                                    </option>
                                )
                            )}
                        </select>
                    </section>



                    {listeningImportJobId && (
                        <div className="teacher-exercise-job-banner">
                            <div className="teacher-exercise-job-spinner" />

                            <div>
                                <strong>
                                    Listening import is being processed
                                </strong>

                                <span>
                                    Job ID:{" "}
                                    {listeningImportJobId}
                                    {" • "}
                                    Status:{" "}
                                    {listeningImportStatus}
                                </span>
                            </div>
                        </div>
                    )}

                    <section className="teacher-exercise-toolbar">
                        <div className="teacher-exercise-tabs">
                            {skillTabs.map(tab => (
                                <button
                                    type="button"
                                    key={tab.key}
                                    className={
                                        activeSkillFilter ===
                                            tab.key
                                            ? "teacher-exercise-tab active"
                                            : "teacher-exercise-tab"
                                    }
                                    onClick={() => {
                                        setActiveSkillFilter(
                                            tab.key
                                        );

                                        if (
                                            tab.key === "all"
                                        ) {
                                            setActiveExerciseCategoryId(
                                                ""
                                            );
                                            return;
                                        }

                                        const category =
                                            exerciseCategories.find(
                                                item =>
                                                    getSkillConfig(
                                                        item.name
                                                    ).key ===
                                                    tab.key
                                            );

                                        if (category) {
                                            setActiveExerciseCategoryId(
                                                String(
                                                    category
                                                        .exerciseCategoryId
                                                )
                                            );
                                        }
                                    }}
                                >
                                    {tab.label}
                                    <span>{tab.count}</span>
                                </button>
                            ))}
                        </div>

                        <div className="teacher-exercise-toolbar-actions">
                            <div className="teacher-exercise-search">
                                <Search size={19} />

                                <input
                                    value={searchKeyword}
                                    onChange={event =>
                                        setSearchKeyword(
                                            event.target.value
                                        )
                                    }
                                    placeholder="Search exercises..."
                                />
                            </div>


                        </div>
                    </section>

                    {visibleExerciseSections.length ===
                        0 &&
                        exercisesWithoutSection.length ===
                        0 ? (
                        <div className="teacher-exercise-empty">
                            <ClipboardList size={42} />

                            <h2>
                                No exercises found
                            </h2>

                            <p>
                                No Exercise Sections match the
                                current filters.
                            </p>
                        </div>
                    ) : (
                        <section className="teacher-exercise-section-list">
                            {visibleExerciseSections.map(
                                section => {
                                    const SkillIcon =
                                        section.skillConfig
                                            .icon;
                                    const isCollapsed =
                                        collapsedSectionIds.includes(
                                            section.exerciseSectionId
                                        );

                                    return (
                                        <article
                                            key={
                                                section
                                                    .exerciseSectionId
                                            }
                                            className={`teacher-exercise-section-card ${section.skillConfig.key}`}
                                        >
                                            <header className="teacher-exercise-section-header">
                                                <div className="teacher-exercise-section-info">
                                                    <div className="teacher-exercise-section-badges">
                                                        <span className="teacher-exercise-skill-badge">
                                                            <SkillIcon size={14} />
                                                            {section.skillConfig.label}
                                                        </span>

                                                        <span className="teacher-exercise-class-badge">
                                                            {getClassRoomName(section)}
                                                        </span>

                                                        <span className="teacher-exercise-count-badge">
                                                            {section.exercises.length} exercise set
                                                        </span>
                                                    </div>
                                                </div>

                                                <div className="teacher-exercise-section-title-box">
                                                    <h2 className="teacher-exercise-section-title">
                                                        {section.title}
                                                    </h2>

                                                    <button
                                                        type="button"
                                                        className="teacher-exercise-section-toggle"
                                                        onClick={() =>
                                                            setCollapsedSectionIds(
                                                                currentIds =>
                                                                    currentIds.includes(
                                                                        section.exerciseSectionId
                                                                    )
                                                                        ? currentIds.filter(
                                                                            id =>
                                                                                id !== section.exerciseSectionId
                                                                        )
                                                                        : [
                                                                            ...currentIds,
                                                                            section.exerciseSectionId
                                                                        ]
                                                            )
                                                        }
                                                        aria-label={
                                                            isCollapsed
                                                            ? "Expand exercise section"
                                                            : "Collapse exercise section"
                                                        }
                                                        title={
                                                            isCollapsed
                                                            ? "Expand section"
                                                            : "Collapse section"
                                                        }
                                                    >
                                                        {isCollapsed ? (
                                                            <ChevronDown size={18} />
                                                        ) : (
                                                            <ChevronUp size={18} />
                                                        )}
                                                    </button>
                                                </div>

                                                <div className="teacher-exercise-section-actions">
                                                    <button
                                                        type="button"
                                                        className="teacher-exercise-add-set-btn"
                                                        onClick={() => {
                                                            resetForm();

                                                            setCreateClassRoomId(
                                                                section.classRoomId
                                                            );

                                                            setCreateCourseId(
                                                                section.courseId
                                                            );

                                                            setCreateCourseType(
                                                                getCourseTypeFromClass({
                                                                    courseName:
                                                                        section.courseName
                                                                })
                                                            );

                                                            setCreateSkill(
                                                                section.exerciseCategoryId
                                                            );

                                                            setCreateExerciseSectionId(
                                                                section.exerciseSectionId
                                                            );

                                                            setCreateSections([section]);

                                                            setShowModal(true);
                                                        }}
                                                    >
                                                        <Plus size={17} />
                                                        Add Exercise
                                                    </button>

                                                    <button
                                                        type="button"
                                                        className="teacher-exercise-icon-btn edit"
                                                        onClick={() =>
                                                            openEditSectionModal(section)
                                                        }
                                                        title="Edit exercise section"
                                                    >
                                                        <Pencil size={17} />
                                                    </button>

                                                    <button
                                                        type="button"
                                                        className="teacher-exercise-icon-btn delete"
                                                        onClick={() =>
                                                            handleDeleteSection(
                                                                section.exerciseSectionId
                                                            )
                                                        }
                                                    >
                                                        <Trash2 size={17} />
                                                    </button>
                                                </div>
                                            </header>

                                            {!isCollapsed && (
                                            <div className="teacher-exercise-set-grid">
                                                <div className="teacher-exercise-set-table-head" aria-hidden="true">
                                                    <span>STT</span>
                                                        <span>Exercise Title</span>
                                                        <span>Questions</span>
                                                        <span>Actions</span>
                                                </div>
                                                {section
                                                    .exercises
                                                    .length ===
                                                    0 ? (
                                                    <div className="teacher-exercise-section-empty">
                                                            No exercises in
                                                            this section yet.
                                                    </div>
                                                ) : (
                                                    section.exercises.map(
                                                        (exercise, index) => (
                                                            <div
                                                                key={
                                                                    exercise
                                                                        .exerciseId
                                                                }
                                                                className="teacher-exercise-set-card"
                                                            >
                                                                <span className="teacher-exercise-set-index">
                                                                    {index + 1}
                                                                </span>

                                                                <div className="teacher-exercise-set-title-row">
                                                                    <FileText
                                                                        size={20}
                                                                    />

                                                                    <h3>
                                                                        {exercise.title}
                                                                    </h3>
                                                                </div>

                                                                <div className="teacher-exercise-set-meta">
                                                                    <FileText size={15} />
                                                                    <span>
                                                                        {exercise.totalQuestions} questions
                                                                    </span>
                                                                </div>

                                                                <div className="teacher-exercise-set-actions">
                                                                    {exercise.hasSourceFile && (
                                                                        <button
                                                                            type="button"
                                                                            className="teacher-exercise-file-badge"
                                                                            onClick={() => handleViewExerciseSource(exercise)}
                                                                        >
                                                                            <FileText size={13} />
                                                                            View Source
                                                                        </button>
                                                                    )}
                                                                    <button
                                                                        type="button"
                                                                        className="teacher-exercise-icon-btn edit"
                                                                        onClick={() =>
                                                                            openEditExerciseModal(
                                                                                exercise
                                                                            )
                                                                        }
                                                                        title="Edit exercise"
                                                                    >
                                                                        <Pencil size={17} />
                                                                    </button>

                                                                    <button
                                                                        type="button"
                                                                        className="teacher-exercise-icon-btn delete"
                                                                        onClick={() =>
                                                                            handleDelete(
                                                                                exercise.exerciseId
                                                                            )
                                                                        }
                                                                        title="Delete exercise"
                                                                    >
                                                                        <Trash2 size={17} />
                                                                    </button>
                                                                </div>
                                                            </div>
                                                        )
                                                    )
                                                )}
                                            </div>
                                            )}
                                        </article>
                                    );
                                }
                            )}
                            {exercisesWithoutSection.length > 0 && (
                                <article className="teacher-exercise-section-card ungrouped">
                                    <header className="teacher-exercise-section-header">
                                        <div className="teacher-exercise-section-badges">
                                            <span className="teacher-exercise-skill-badge">
                                                <ClipboardList size={14} />
                                    No section
                                            </span>

                                            <span className="teacher-exercise-count-badge">
                                                {exercisesWithoutSection.length} exercise set
                                            </span>
                                        </div>
                                    </header>

                                    <div className="teacher-exercise-set-grid">
                                        <div className="teacher-exercise-set-table-head" aria-hidden="true">
                                            <span>STT</span>
                                            <span>Exercise Title</span>
                                            <span>Questions</span>
                                            <span>Actions</span>
                                        </div>
                                        {exercisesWithoutSection.map((exercise, index) => (
                                            <div
                                                key={exercise.exerciseId}
                                                className="teacher-exercise-set-card"
                                            >
                                                <span className="teacher-exercise-set-index">
                                                    {index + 1}
                                                </span>

                                                <div className="teacher-exercise-set-title-row">
                                                    <FileText size={20} />

                                                    <h3>{exercise.title}</h3>
                                                </div>

                                                <div className="teacher-exercise-set-meta">
                                                    <FileText size={15} />
                                                    <span>{exercise.totalQuestions} questions</span>
                                                </div>

                                                <div className="teacher-exercise-set-actions">
                                                    {exercise.hasSourceFile && (
                                                        <button
                                                            type="button"
                                                            className="teacher-exercise-file-badge"
                                                            onClick={() => handleViewExerciseSource(exercise)}
                                                        >
                                                            <FileText size={13} />
                                                            View Source
                                                        </button>
                                                    )}
                                                    <button
                                                        type="button"
                                                        className="teacher-exercise-assign-btn"
                                                        onClick={() =>
                                                            openAssignModal(exercise.exerciseId)
                                                        }
                                                    >
                                                        Assign to Class
                                                    </button>

                                                    <button
                                                        type="button"
                                                        className="teacher-exercise-icon-btn delete"
                                                        onClick={() =>
                                                            handleDelete(exercise.exerciseId)
                                                        }
                                                        title="Delete exercise"
                                                    >
                                                        <Trash2 size={17} />
                                                    </button>
                                                </div>
                                            </div>
                                        ))}
                                    </div>
                                </article>
                            )}
                        </section>
                    )}


                </div>
            </main>

            {showModal && (
                <div className="lesson-modal-overlay">
                    <div className="lesson-modal lesson-modal-large teacher-exercise-set-modal">
                        <div className="lesson-modal-header">
                            <div>
                                <span className="lesson-modal-eyebrow">
                                    Exercise Set
                                </span>

                                <h2>Create Exercise</h2>
                            </div>

                            <button
                                type="button"
                                className="lesson-modal-close"
                                onClick={() => {
                                    setShowModal(false);
                                    resetForm();
                                }}
                            >
                                <X size={20} />
                            </button>
                        </div>

                        <div className="lesson-form">
                            <label className="lesson-form-group">
                                <span>Exercise Title</span>

                                <input
                                    placeholder="Enter exercise title"
                                    value={title}
                                    onChange={e => setTitle(e.target.value)}
                                    autoFocus
                                />
                            </label>

                            <label className="lesson-form-group">
                                <span>Description</span>

                                <textarea
                                    placeholder="Description"
                                    value={description}
                                    onChange={e =>
                                        setDescription(e.target.value)
                                    }
                                />
                            </label>

                            <div className="lesson-section-create-grid">
                                <label className="lesson-form-group">
                                    <span>Class</span>

                                    <select
                                        value={createClassRoomId}
                                        onChange={e => {
                                            const classRoomId = e.target.value;

                                            setCreateClassRoomId(classRoomId);
                                            setCreateExerciseSectionId("");

                                            const selectedClass = classes.find(x =>
                                                String(x.classRoomId) === String(classRoomId)
                                            );

                                            const nextCourseId = selectedClass?.courseId || "";
                                            const nextCourseType = selectedClass
                                                ? getCourseTypeFromClass(selectedClass)
                                                : "";

                                            setCreateCourseId(nextCourseId);
                                            setCreateCourseType(nextCourseType);

                                            loadCreateSections(
                                                nextCourseId,
                                                classRoomId,
                                                createSkill
                                            );
                                        }}
                                    >
                                        <option value="">Select a class to assign</option>

                                        {classes.map(classRoom => (
                                            <option
                                                key={classRoom.classRoomId}
                                                value={classRoom.classRoomId}
                                            >
                                                {classRoom.className || classRoom.name || classRoom.title}
                                                {" - "}
                                                {classRoom.courseName || classRoom.courseTitle || classRoom.course?.name}
                                            </option>
                                        ))}
                                    </select>
                                </label>

                                <label className="lesson-form-group">
                                    <span>Skill</span>

                                    <select
                                        value={createSkill}
                                        onChange={e => {
                                            const categoryId = e.target.value;

                                            setCreateSkill(categoryId);
                                            setCreateExerciseSectionId("");

                                            loadCreateSections(
                                                createCourseId,
                                                createClassRoomId,
                                                categoryId
                                            );
                                        }}
                                    >
                                        <option value="">Select a skill</option>

                                        {exerciseCategories.map(category => (
                                            <option
                                                key={category.exerciseCategoryId}
                                                value={category.exerciseCategoryId}
                                            >
                                                {getSkillConfig(category.name).label}
                                            </option>
                                        ))}
                                    </select>
                                </label>
                            </div>

                            <label className="lesson-form-group">
                                <span>Exercise Section</span>

                                <select
                                    value={createExerciseSectionId}
                                    onChange={e =>
                                        setCreateExerciseSectionId(e.target.value)
                                    }
                                >
                                    <option value="">Select a section</option>

                                    {createSections.map(section => (
                                        <option
                                            key={section.exerciseSectionId}
                                            value={section.exerciseSectionId}
                                        >
                                            {section.title}
                                        </option>
                                    ))}
                                </select>
                            </label>

                            {Number(createSkill) !== 2 && (
                                <div className="teacher-exercise-import-panel">
                                    <label className="lesson-upload-box teacher-exercise-upload-box">
                                        <input
                                            type="file"
                                            accept=".pdf,.docx,.txt"
                                            onChange={e =>
                                                setImportFile(e.target.files[0])
                                            }
                                        />

                                        <div className="lesson-upload-icon pdf">
                                            <FileText size={25} />
                                        </div>

                                        <div>
                                            <strong>
                                                {Number(createSkill) === 3
                                                    ? "Upload Speaking Source"
                                                    : Number(createSkill) === 4
                                                        ? "Upload Reading Source"
                                                        : Number(createSkill) === 5
                                                            ? "Upload Writing Source"
                                                            : "Upload Exercise Source"}
                                            </strong>

                                            <span>
                                                {importFile
                                                    ? importFile.name
                                                    : "Select a PDF, DOCX, or TXT file"}
                                            </span>
                                        </div>

                                        <Upload size={19} />
                                    </label>

                                    <label className="lesson-form-group">
                                        <span>Maximum Attempts</span>

                                        <input
                                            type="number"
                                            min="1"
                                            value={maxAttempts}
                                            onChange={e => setMaxAttempts(e.target.value)}
                                            placeholder="Maximum attempts"
                                        />
                                    </label>

                                </div>
                            )}
                            {Number(createSkill) === 2 && (
                                <div className="teacher-exercise-import-panel">
                                    <div className="teacher-exercise-import-title">
                                        Import Listening Exercise
                                    </div>

                                    <label className="lesson-form-group">
                                        <span>Number of Audio Files / Conversations</span>

                                        <input
                                            type="number"
                                            min="1"
                                            value={listeningAudioCount}
                                            onChange={e => {
                                                const count = Number(e.target.value);

                                                setListeningAudioCount(count);
                                                setListeningAudioFiles(Array(count).fill(null));
                                                setQuestionCounts(Array(count).fill(""));
                                            }}
                                        />
                                    </label>

                                    {Array.from({ length: listeningAudioCount }).map((_, index) => (
                                        <div
                                            key={index}
                                            className="teacher-exercise-listening-card"
                                        >
                                            <label className="lesson-upload-box teacher-exercise-upload-box">
                                                <input
                                                    type="file"
                                                    accept=".mp3,.wav,.m4a"
                                                    onChange={e => {
                                                        const next = [...listeningAudioFiles];
                                                        next[index] = e.target.files[0];
                                                        setListeningAudioFiles(next);
                                                    }}
                                                />

                                                <div className="lesson-upload-icon video">
                                                    <Headphones size={25} />
                                                </div>

                                                <div>
                                                    <strong>Audio {index + 1}</strong>

                                                    <span>
                                                        {listeningAudioFiles[index]
                                                            ? listeningAudioFiles[index].name
                                                            : "Select an MP3, WAV, or M4A file"}
                                                    </span>
                                                </div>

                                                <Upload size={19} />
                                            </label>

                                            <label className="lesson-form-group">
                                                <span>Questions for audio {index + 1}</span>

                                                <input
                                                    type="number"
                                                    min="1"
                                                    value={questionCounts[index] || ""}
                                                    onChange={e => {
                                                        const next = [...questionCounts];
                                                        next[index] = e.target.value;
                                                        setQuestionCounts(next);
                                                    }}
                                                    placeholder="e.g. 4"
                                                />
                                            </label>
                                        </div>
                                    ))}

                                    <label className="lesson-upload-box teacher-exercise-upload-box">
                                        <input
                                            type="file"
                                            accept=".pdf,.docx,.txt"
                                            onChange={e =>
                                                setListeningQuestionFile(e.target.files[0])
                                            }
                                        />

                                        <div className="lesson-upload-icon pdf">
                                            <FileText size={25} />
                                        </div>

                                        <div>
                                            <strong>Question file</strong>

                                            <span>
                                                {listeningQuestionFile
                                                    ? listeningQuestionFile.name
                                                    : "Select a PDF, DOCX, or TXT file"}
                                            </span>
                                        </div>

                                        <Upload size={19} />
                                    </label>

                                    <label className="lesson-form-group">
                                        <span>Transcript (optional)</span>

                                        <textarea
                                            placeholder="Transcript (optional)"
                                            value={transcript}
                                            onChange={e => setTranscript(e.target.value)}
                                        />
                                    </label>

                                    <label className="lesson-form-group">
                                        <span>Maximum Attempts</span>

                                        <input
                                            type="number"
                                            min="1"
                                            value={maxAttempts}
                                            onChange={e => setMaxAttempts(e.target.value)}
                                            placeholder="Maximum attempts"
                                        />
                                    </label>

                                    <p className="teacher-exercise-import-note">
                                        Audio files will be stored in MinIO. The question file will be parsed by AI into listening questions.
                                    </p>
                                </div>
                            )}

                            {[1, 2, 4].includes(Number(createSkill)) && (
                                <label className="lesson-form-group teacher-explanation-option">
                                    <span>
                                        <input
                                            type="checkbox"
                                            checked={generateExplanations}
                                            onChange={event =>
                                                setGenerateExplanations(
                                                    event.target.checked
                                                )
                                            }
                                        />
                                        Generate explanations for this exercise set
                                    </span>
                                </label>
                            )}

                            <div className="lesson-modal-actions">
                                <button
                                    type="button"
                                    className="lesson-btn lesson-btn-cancel"
                                    onClick={() => {
                                        setShowModal(false);
                                        resetForm();
                                    }}
                                >
                                    Cancel
                                </button>

                                <button
                                    type="button"
                                    className="lesson-btn"
                                    onClick={() => {
                                        if (Number(createSkill) === 2) {
                                            handleUploadListening();
                                            return;
                                        }
                                        if (Number(createSkill) === 3) {
                                            handleUploadSpeaking();
                                            return;
                                        }
                                        if (Number(createSkill) === 4) {
                                            handleUploadReading();
                                            return;
                                        }
                                        if (Number(createSkill) === 5) {
                                            handleUploadWriting();
                                            return;
                                        }
                                        handleUploadDraft();
                                    }}
                                >
                                    <Plus size={18} />

                                    {Number(createSkill) === 2
                                        ? "Create Exercise"
                                        : Number(createSkill) === 3
                                            ? "Create Exercise"
                                            : Number(createSkill) === 4
                                                ? "Create Exercise"
                                                : Number(createSkill) === 5
                                                    ? "Create Exercise"
                                                    : "Create Exercise"}
                                </button>
                            </div>
                        </div>
                    </div>
                </div>
            )}

            {showSectionModal && (
                <div className="lesson-modal-overlay">
                    <div className="lesson-modal teacher-exercise-section-modal">
                        <div className="lesson-modal-header">
                            <div>
                                <span className="lesson-modal-eyebrow">
                                    Exercise Section
                                </span>

                                <h2>
                                    {editingSectionId
                                        ? "Edit Exercise Section"
                                        : "Create Exercise Section"}
                                </h2>
                            </div>

                            <button
                                type="button"
                                className="lesson-modal-close"
                                onClick={closeCreateSectionModal}
                            >
                                <X size={20} />
                            </button>
                        </div>

                        <div className="lesson-form teacher-exercise-section-form">
                            <label className="lesson-form-group">
                                <span>Section Title</span>

                                <input
                                    placeholder="e.g. Unit 1, Writing Task 1..."
                                    value={sectionTitle}
                                    onChange={event =>
                                        setSectionTitle(
                                            event.target.value
                                        )
                                    }
                                    autoFocus
                                />
                            </label>

                            <div className="lesson-section-create-grid">
                                <label className="lesson-form-group">
                                    <span>Class</span>

                                    <select
                                        value={sectionClassRoomId}
                                        disabled={
                                            Boolean(editingSectionId) &&
                                            editingSectionHasExercises
                                        }
                                        onChange={event => {
                                            const classRoomId =
                                                event.target.value;

                                            setSectionClassRoomId(
                                                classRoomId
                                            );

                                            const selectedClass =
                                                classes.find(
                                                    classRoom =>
                                                        String(
                                                            classRoom.classRoomId
                                                        ) ===
                                                        String(classRoomId)
                                                );

                                            setSectionCourseId(
                                                selectedClass?.courseId ||
                                                ""
                                            );
                                        }}
                                    >
                                        <option value="">
                                            Select a class
                                        </option>

                                        {classes.map(classRoom => (
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
                                        ))}
                                    </select>
                                </label>

                                <label className="lesson-form-group">
                                    <span>Skill</span>

                                    <select
                                        value={sectionCategoryId}
                                        disabled={
                                            Boolean(editingSectionId) &&
                                            editingSectionHasExercises
                                        }
                                        onChange={event =>
                                            setSectionCategoryId(
                                                event.target.value
                                            )
                                        }
                                    >
                                        <option value="">
                                            Select a skill
                                        </option>

                                        {exerciseCategories.map(
                                            category => (
                                                <option
                                                    key={
                                                        category
                                                            .exerciseCategoryId
                                                    }
                                                    value={
                                                        category
                                                            .exerciseCategoryId
                                                    }
                                                >
                                                    {getSkillConfig(category.name).label}
                                                </option>
                                            )
                                        )}
                                    </select>
                                </label>
                            </div>
                            {editingSectionId &&
                                editingSectionHasExercises && (
                                    <div className="teacher-exercise-section-edit-warning">
                                        This section already contains exercises.
                                        You can only rename it. To change its
                                        class or skill, delete the exercises
                                        in this section first.
                                    </div>
                                )}


                            <div className="lesson-modal-actions">
                                <button
                                    type="button"
                                    className="lesson-btn lesson-btn-cancel"
                                    onClick={closeCreateSectionModal}
                                >
                                    Cancel
                                </button>

                                <button
                                    type="button"
                                    className="lesson-btn"
                                    onClick={
                                        editingSectionId
                                            ? handleUpdateSection
                                            : handleCreateSection
                                    }
                                >
                                    {editingSectionId
                                        ? <Pencil size={17} />
                                        : <Plus size={17} />}

                                    {editingSectionId
                                        ? "Save Changes"
                                        : "Create Section"}
                                </button>
                            </div>
                        </div>
                    </div>
                </div>
            )}
            {showEditExerciseModal &&
                editingExercise && (
                    <div className="lesson-modal-overlay">
                        <div className="lesson-modal teacher-exercise-edit-set-modal">
                            <div className="lesson-modal-header">
                                <div>
                                    <span className="lesson-modal-eyebrow">
                                        Exercise Set
                                    </span>

                                    <h2>
                                        Edit Exercise
                                    </h2>
                                </div>

                                <button
                                    type="button"
                                    className="lesson-modal-close"
                                    onClick={
                                        closeEditExerciseModal
                                    }
                                >
                                    <X size={20} />
                                </button>
                            </div>

                            <div className="lesson-form teacher-exercise-edit-set-form">
                                <label className="lesson-form-group">
                                    <span>Exercise Title</span>

                                    <input
                                        value={
                                            editExerciseTitle
                                        }
                                        onChange={event =>
                                            setEditExerciseTitle(
                                                event.target
                                                    .value
                                            )
                                        }
                                        placeholder="Exercise title"
                                        autoFocus
                                    />
                                </label>

                                <label className="lesson-form-group">
                                    <span>Description</span>

                                    <textarea
                                        value={
                                            editExerciseDescription
                                        }
                                        onChange={event =>
                                            setEditExerciseDescription(
                                                event.target
                                                    .value
                                            )
                                        }
                                        placeholder="Exercise description"
                                    />
                                </label>

                                <label className="lesson-form-group">
                                    <span>Maximum Attempts</span>

                                    <input
                                        type="number"
                                        min="1"
                                        value={
                                            editExerciseMaxAttempts
                                        }
                                        onChange={event =>
                                            setEditExerciseMaxAttempts(
                                                event.target
                                                    .value
                                            )
                                        }
                                    />
                                </label>

                                {[1, 2, 4].includes(
                                    Number(editingExercise.skill)
                                ) && (
                                    <label className="lesson-form-group teacher-explanation-option">
                                        <span>
                                            <input
                                                type="checkbox"
                                                checked={editGenerateExplanations}
                                                onChange={event =>
                                                    setEditGenerateExplanations(
                                                        event.target.checked
                                                    )
                                                }
                                            />
                                            Generate explanations for this exercise
                                        </span>
                                        <small>
                                            Only questions without explanations will be updated.
                                        </small>
                                    </label>
                                )}

                                <div className="lesson-modal-actions">
                                    <button
                                        type="button"
                                        className="lesson-btn lesson-btn-cancel"
                                        onClick={
                                            closeEditExerciseModal
                                        }
                                    >
                                    Cancel
                                    </button>

                                    <button
                                        type="button"
                                        className="lesson-btn"
                                        onClick={
                                            handleUpdateExercise
                                        }
                                    >
                                        <Pencil size={17} />
                                        Save Exercise
                                    </button>
                                </div>
                            </div>
                        </div>
                    </div>
                )}
            {showAssignModal && (
                <div className="lesson-modal-overlay">
                    <div className="lesson-modal">
                        <h2>Assign Exercise to a Class</h2>

                        <div className="lesson-form">
                            <select
                                value={assignClassRoomId}
                                onChange={e =>
                                    setAssignClassRoomId(e.target.value)
                                }
                            >
                                <option value="">Select a class</option>

                                {classes.map(classRoom => (
                                    <option
                                        key={classRoom.classRoomId}
                                        value={classRoom.classRoomId}
                                    >
                                        {classRoom.className || classRoom.name || classRoom.title}
                                        {" - "}
                                        {classRoom.courseName || classRoom.courseTitle || classRoom.course?.name}
                                    </option>
                                ))}
                            </select>



                            <input
                                type="number"
                                min="1"
                                value={maxAttempts}
                                onChange={e => setMaxAttempts(e.target.value)}
                                placeholder="Maximum attempts"
                            />

                            <button
                                className="lesson-btn"
                                onClick={handleAssign}
                            >
                                Assign Exercise
                            </button>

                            <button
                                className="lesson-btn secondary"
                                onClick={() => setShowAssignModal(false)}
                            >
                                Close
                            </button>
                        </div>
                    </div>
                </div>
            )}
        </div>
    );
}

export default TeacherExercisePage;
