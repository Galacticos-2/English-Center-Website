import {
    useEffect,
    useMemo,
    useState
} from "react";

import {
    BookOpen,
    ChevronDown,
    ChevronRight,
    FileText,

    Headphones,

    LibraryBig,
    Mic2,
    Pencil,
    Plus,
    Search,
    Trash2,
    Upload,
    Video,
    X
} from "lucide-react";

import {
    getLessonCategoriesApi,
    getLessonSectionsApi,
    createLessonSectionApi,
    createLessonApi,
    uploadLessonPdfApi,
    uploadLessonVideoApi,
    getMyTeacherClassesApi,
    updateLessonApi,
    getLessonStatisticsApi,
    deleteLessonSectionApi,
    deleteLessonApi,
    updateLessonSectionApi
} from "../api/lessonApi";

import TeacherSidebar from "./TeacherSidebar";
import "../styles/lesson.css";

function TeacherLessonPage() {
    const [categories, setCategories] = useState([]);
    const [activeCategory, setActiveCategory] = useState(null);

    const [sections, setSections] = useState([]);

    const [teacherClasses, setTeacherClasses] = useState([]);
    const [activeClass, setActiveClass] = useState(null);

    const [editingLesson, setEditingLesson] = useState(null);
    const [editingSection, setEditingSection] = useState(null);

    const [showSectionModal, setShowSectionModal] = useState(false);
    const [showLessonModal, setShowLessonModal] = useState(false);

    const [selectedSectionId, setSelectedSectionId] = useState(null);
    const [collapsedSections, setCollapsedSections] =
        useState({});
    const [sectionTitle, setSectionTitle] = useState("");
    const [lessonTitle, setLessonTitle] = useState("");
    const [lessonOrder, setLessonOrder] = useState(1);

    const [pdfFile, setPdfFile] = useState(null);
    const [videoFile, setVideoFile] = useState(null);
    const [lessonStatistics, setLessonStatistics] =
        useState([]);
    const [isSavingLesson, setIsSavingLesson] = useState(false);
    const [isSavingSection, setIsSavingSection] = useState(false);


    const [sectionClassId, setSectionClassId] =
        useState("");

    const [sectionCategoryId, setSectionCategoryId] =
        useState("");
    const [searchText, setSearchText] = useState("");


    const delay = ms =>
        new Promise(resolve => setTimeout(resolve, ms));

    const normalizeText = value =>
        String(value || "")
            .trim()
            .toLowerCase();

    const formatSkillName = name => {
        const normalizedName = normalizeText(name);

        if (normalizedName.includes("ngữ pháp") || normalizedName.includes("ngu phap")) return "Grammar";
        if (normalizedName.includes("nghe") || normalizedName.includes("listening")) return "Listening";
        if (normalizedName.includes("nói") || normalizedName.includes("noi")) return "Speaking";
        if (normalizedName.includes("đọc") || normalizedName.includes("doc")) return "Reading";
        if (normalizedName.includes("viết") || normalizedName.includes("viet")) return "Writing";

        return name;
    };

    const loadTeacherClasses = async () => {
        try {
            const data = await getMyTeacherClassesApi();

            setTeacherClasses(data || []);

            if (data?.length > 0) {
                setActiveClass(currentClass => {
                    if (currentClass) {
                        const existingClass = data.find(
                            item =>
                                item.classRoomId ===
                                currentClass.classRoomId
                        );

                        if (existingClass) {
                            return existingClass;
                        }
                    }

                    return null;
                });
            }
        }
        catch (error) {
            console.error("Load teacher classes error:", error);
        }
    };

    const loadCategories = async () => {
        try {
            const data = await getLessonCategoriesApi();

            setCategories(data || []);

            if (data?.length > 0) {
                setActiveCategory(currentCategory => {
                    if (currentCategory) {
                        const existingCategory = data.find(
                            item =>
                                item.lessonCategoryId ===
                                currentCategory.lessonCategoryId
                        );

                        if (existingCategory) {
                            return existingCategory;
                        }
                    }

                    return null;
                });
            }
        }
        catch (error) {
            console.error("Load lesson categories error:", error);
        }
    };
    const toggleSection = sectionId => {
        setCollapsedSections(previous => ({
            ...previous,
            [sectionId]:
                !previous[sectionId]
        }));
    };
    const loadLessonStatistics = async () => {
        if (
            teacherClasses.length === 0 ||
            categories.length === 0
        ) {
            setLessonStatistics([]);
            return;
        }

        try {
            const classesToLoad =
                activeClass
                    ? [activeClass]
                    : teacherClasses;

            const sectionResults =
                await Promise.all(
                    classesToLoad.map(async item => {
                        const classSections =
                            await getLessonStatisticsApi(
                                item.courseId,
                                item.classRoomId
                            );

                        const safeClassSections =
                            Array.isArray(classSections)
                                ? classSections
                                : [];

                        return safeClassSections.map(
                            section => ({
                                ...section,
                                courseId:
                                    item.courseId,
                                courseName:
                                    item.courseName,
                                classRoomId:
                                    item.classRoomId,
                                className:
                                    item.className
                            })
                        );
                    })
                );

            const allSections =
                sectionResults.flat();

            const statistics =
                categories.map(category => {
                    const categorySections =
                        allSections.filter(
                            section =>
                                Number(
                                    section.lessonCategoryId
                                ) ===
                                Number(
                                    category.lessonCategoryId
                                )
                        );

                    const lessonCount =
                        categorySections.reduce(
                            (total, section) =>
                                total +
                                (
                                    Array.isArray(
                                        section.lessons
                                    )
                                        ? section.lessons.length
                                        : 0
                                ),
                            0
                        );

                    return {
                        lessonCategoryId:
                            category.lessonCategoryId,

                        categoryName:
                            category.name,

                        sectionCount:
                            categorySections.length,

                        lessonCount
                    };
                });

            setLessonStatistics(statistics);
        }
        catch (error) {
            console.error(
                "Load lesson statistics error:",
                error.response?.data || error
            );

            setLessonStatistics([]);
        }
    };
    const loadSections = async () => {
        if (teacherClasses.length === 0) {
            setSections([]);
            return;
        }

        try {
            const classesToLoad =
                activeClass
                    ? [activeClass]
                    : teacherClasses;

            const sectionResults =
                await Promise.all(
                    classesToLoad.map(async item => {
                        const classSections =
                            await getLessonSectionsApi(
                                item.courseId,
                                item.classRoomId,
                                activeCategory
                                    ?.lessonCategoryId
                            );

                        const safeClassSections =
                            Array.isArray(classSections)
                                ? classSections
                                : [];

                        return safeClassSections.map(
                            section => ({
                                ...section,
                                courseId:
                                    item.courseId,
                                courseName:
                                    item.courseName,
                                classRoomId:
                                    item.classRoomId,
                                className:
                                    item.className
                            })
                        );
                    })
                );

            setSections(
                sectionResults.flat()
            );
        }
        catch (error) {
            console.error(
                "Load lesson sections error:",
                error
            );

            setSections([]);
        }
    };

    useEffect(() => {
        loadCategories();
        loadTeacherClasses();
    }, []);

    useEffect(() => {
        if (teacherClasses.length === 0) {
            setSections([]);
            return;
        }

        loadSections();
    }, [
        teacherClasses,
        activeCategory?.lessonCategoryId,
        activeClass?.courseId,
        activeClass?.classRoomId
    ]);

    useEffect(() => {
        if (
            teacherClasses.length === 0 ||
            categories.length === 0
        ) {
            setLessonStatistics([]);
            return;
        }

        loadLessonStatistics();
    }, [
        teacherClasses,
        categories,
        activeClass?.courseId,
        activeClass?.classRoomId
    ]);

    const handleCloseSectionModal = (force = false) => {
        if (isSavingSection && !force) {
            return;
        }

        setShowSectionModal(false);
        setEditingSection(null);
        setSectionTitle("");
        setSectionClassId("");
        setSectionCategoryId("");
    };

    const handleCloseLessonModal = (force = false) => {
        if (isSavingLesson && !force) {
            return;
        }

        setShowLessonModal(false);
        setEditingLesson(null);
        setSelectedSectionId(null);
        setLessonTitle("");
        setLessonOrder(1);
        setPdfFile(null);
        setVideoFile(null);
    };

    const handleOpenCreateSection = () => {
        setEditingSection(null);
        setSectionTitle("");

        setSectionClassId("");

        setSectionCategoryId(
            activeCategory?.lessonCategoryId ||
            categories[0]?.lessonCategoryId ||
            ""
        );

        setShowSectionModal(true);
    };

    const handleOpenEditSection = section => {
        setEditingSection(section);
        setSectionTitle(section.title || "");

        setSectionClassId(
            section.classRoomId || ""
        );

        setSectionCategoryId(
            section.lessonCategoryId || ""
        );

        setShowSectionModal(true);
    };

    const handleOpenCreateLesson = sectionId => {
        setEditingLesson(null);
        setSelectedSectionId(sectionId);
        setLessonTitle("");
        setLessonOrder(1);
        setPdfFile(null);
        setVideoFile(null);
        setShowLessonModal(true);
    };

    const handleOpenEditLesson = (lesson, sectionId) => {
        setEditingLesson(lesson);
        setSelectedSectionId(sectionId);
        setLessonTitle(lesson.title || "");
        setLessonOrder(lesson.orderIndex || 1);
        setPdfFile(null);
        setVideoFile(null);
        setShowLessonModal(true);
    };

    const handleSaveSection = async () => {
        const normalizedTitle =
            sectionTitle.trim();

        if (!normalizedTitle) {
            alert("Please enter a Lesson Section title.");
            return;
        }

        if (!editingSection && !sectionClassId) {
            alert("Please select a class.");
            return;
        }

        if (!editingSection && !sectionCategoryId) {
            alert("Please select a skill.");
            return;
        }

        try {
            setIsSavingSection(true);

            if (editingSection) {
                await updateLessonSectionApi(
                    editingSection.lessonSectionId,
                    {
                        title: normalizedTitle
                    }
                );
            }
            else {
                const selectedClass =
                    teacherClasses.find(
                        item =>
                            item.classRoomId ===
                            Number(sectionClassId)
                    );

                if (!selectedClass) {
                    alert("The selected class could not be found.");
                    return;
                }

                await createLessonSectionApi({
                    title: normalizedTitle,

                    courseId:
                        selectedClass.courseId,

                    classRoomId:
                        selectedClass.classRoomId,

                    lessonCategoryId:
                        Number(sectionCategoryId)
                });
            }

            handleCloseSectionModal(true);

            await Promise.all([
                loadSections(),
                loadLessonStatistics()
            ]);
        }
        catch (error) {
            console.error(error);

            alert(
                editingSection
                    ? "Unable to update the Lesson Section"
                    : "Unable to create the Lesson Section"
            );
        }
        finally {
            setIsSavingSection(false);
        }
    };

    const handleDeleteSection = async sectionId => {
        const confirmed = window.confirm(
            "Delete this Lesson Section? All lessons inside it will also be deleted."
        );

        if (!confirmed) {
            return;
        }

        try {
            await deleteLessonSectionApi(sectionId);

            await Promise.all([
                loadSections(),
                loadLessonStatistics()
            ]);
        }
        catch (error) {
            console.error(error);
            alert("Unable to delete the Lesson Section.");
        }
    };

    const handleSaveLesson = async () => {
        const normalizedTitle = lessonTitle.trim();
        const normalizedOrder = Number(lessonOrder);

        if (!normalizedTitle) {
            alert("Please enter a lesson title.");
            return;
        }

        if (
            Number.isNaN(normalizedOrder) ||
            normalizedOrder < 1
        ) {
            alert("Lesson order must be at least 1.");
            return;
        }

        if (!editingLesson && !selectedSectionId) {
            alert("Unable to identify the Lesson Section.");
            return;
        }

        try {
            setIsSavingLesson(true);

            if (editingLesson) {
                await updateLessonApi(
                    editingLesson.lessonId,
                    {
                        title: normalizedTitle,
                        orderIndex: normalizedOrder
                    }
                );

                if (pdfFile) {
                    uploadLessonPdfApi(
                        editingLesson.lessonId,
                        pdfFile
                    ).catch(console.error);
                }

                if (videoFile) {
                    uploadLessonVideoApi(
                        editingLesson.lessonId,
                        videoFile
                    ).catch(console.error);
                }

                await delay(5000);

                handleCloseLessonModal(true);

                await Promise.all([
                    loadSections(),
                    loadLessonStatistics()
                ]);

                return;
            }

            const createResult = await createLessonApi({
                title: normalizedTitle,
                lessonSectionId: selectedSectionId,
                orderIndex: normalizedOrder
            });

            const createdLessonId =
                createResult.data.lessonId;

            if (pdfFile) {
                uploadLessonPdfApi(
                    createdLessonId,
                    pdfFile
                ).catch(console.error);
            }

            if (videoFile) {
                uploadLessonVideoApi(
                    createdLessonId,
                    videoFile
                ).catch(console.error);
            }

            await delay(5000);

            handleCloseLessonModal(true);

            await Promise.all([
                loadSections(),
                loadLessonStatistics()
            ]);
        }
        catch (error) {
            console.error(error);
            alert("Unable to save the lesson.");
        }
        finally {
            setIsSavingLesson(false);
        }
    };

    const handleDeleteLesson = async lessonId => {
        const confirmed = window.confirm(
            "Are you sure you want to delete this lesson?"
        );

        if (!confirmed) {
            return;
        }

        try {
            await deleteLessonApi(lessonId);

            await Promise.all([
                loadSections(),
                loadLessonStatistics()
            ]);
        }
        catch (error) {
            console.error(error);
            alert("Unable to delete the lesson.");
        }
    };

    const getCategoryType = category => {
        const categoryName = normalizeText(category?.name);

        if (
            categoryName.includes("grammar") ||
            categoryName.includes("ngữ pháp") ||
            categoryName.includes("ngu phap")
        ) {
            return "grammar";
        }

        if (
            categoryName.includes("listen") ||
            categoryName.includes("nghe")
        ) {
            return "listening";
        }

        if (
            categoryName.includes("speak") ||
            categoryName.includes("nói") ||
            categoryName.includes("noi")
        ) {
            return "speaking";
        }

        if (
            categoryName.includes("read") ||
            categoryName.includes("đọc") ||
            categoryName.includes("doc")
        ) {
            return "reading";
        }

        if (
            categoryName.includes("write") ||
            categoryName.includes("viết") ||
            categoryName.includes("viet")
        ) {
            return "writing";
        }

        return "default";
    };

    const getCategoryIcon = (
        category,
        iconSize = 18
    ) => {
        const type = getCategoryType(category);

        switch (type) {
            case "grammar":
                return <BookOpen size={iconSize} />;

            case "listening":
                return <Headphones size={iconSize} />;

            case "speaking":
                return <Mic2 size={iconSize} />;

            case "reading":
                return <FileText size={iconSize} />;

            case "writing":
                return <Pencil size={iconSize} />;

            default:
                return <LibraryBig size={iconSize} />;
        }
    };



    const categoryCounts = useMemo(() => {
        return categories.map(category => {
            const statistic =
                lessonStatistics.find(
                    item =>
                        item.lessonCategoryId ===
                        category.lessonCategoryId
                );

            return {
                ...category,
                sectionCount:
                    statistic?.sectionCount ?? 0,

                lessonCount:
                    statistic?.lessonCount ?? 0
            };
        });
    }, [categories, lessonStatistics]);

    const totalLessons = useMemo(() => {
        return lessonStatistics.reduce(
            (total, item) =>
                total + (item.lessonCount || 0),
            0
        );
    }, [lessonStatistics]);

    const filteredSections = useMemo(() => {
        const keyword = normalizeText(searchText);

        if (!keyword) {
            return sections;
        }

        return sections
            .map(section => {
                const sectionMatched = normalizeText(
                    section.title
                ).includes(keyword);

                const matchingLessons =
                    section.lessons?.filter(lesson =>
                        normalizeText(lesson.title)
                            .includes(keyword)
                    ) || [];

                if (sectionMatched) {
                    return section;
                }

                if (matchingLessons.length > 0) {
                    return {
                        ...section,
                        lessons: matchingLessons
                    };
                }

                return null;
            })
            .filter(Boolean);
    }, [sections, searchText]);
    const getSectionCategory = section => {
        return categories.find(
            category =>
                category.lessonCategoryId ===
                section.lessonCategoryId
        ) || {
            lessonCategoryId:
                section.lessonCategoryId,

            name:
                formatSkillName(section.categoryName) || "Lessons"
        };
    };
    return (
        <div className="lesson-layout">
            <TeacherSidebar />

            <main className="lesson-main">
                <div className="lesson-page-container">
                    <div className="lesson-breadcrumb">
                        <span>Home</span>
                        <ChevronRight size={16} />
                        <strong>Lessons</strong>
                    </div>

                    <header className="lesson-header">
                        <div className="lesson-title">
                            <div className="lesson-title-heading">
                                <BookOpen size={36} />
                                <h1>Lessons</h1>
                            </div>

                            <p>
                                Manage lessons, videos, and PDFs by
                                skill and class
                            </p>
                        </div>

                        <button
                            type="button"
                            className="lesson-create-section-btn"
                            disabled={
                                teacherClasses.length === 0 ||
                                categories.length === 0
                            }
                            onClick={handleOpenCreateSection}
                        >
                            <Plus size={20} />
                            <span>Create Lesson Section</span>
                        </button>
                    </header>





                    <section className="lesson-category-toolbar">
                        <div className="lesson-top-filters">
                            <label className="lesson-select-field">
                                <select
                                    value={
                                        activeClass?.classRoomId || ""
                                    }
                                    onChange={event => {
                                        const classRoomId =
                                            Number(event.target.value);

                                        if (!classRoomId) {
                                            setActiveClass(null);
                                            return;
                                        }

                                        const selected =
                                            teacherClasses.find(
                                                item =>
                                                    item.classRoomId ===
                                                    classRoomId
                                            );

                                        setActiveClass(
                                            selected || null
                                        );
                                    }}
                                >
                                    <option value="">
                                        {teacherClasses.length === 0
                                            ? "No classes available"
                                            : "All classes"}
                                    </option>

                                    {teacherClasses.map(item => (
                                        <option
                                            key={item.classRoomId}
                                            value={item.classRoomId}
                                        >
                                            {item.courseName}
                                            {" - "}
                                            {item.className}
                                        </option>
                                    ))}
                                </select>

                                <ChevronDown
                                    size={18}
                                    className="lesson-select-arrow"
                                />
                            </label>

                            <label className="lesson-select-field">
                                <select
                                    value={
                                        activeCategory
                                            ?.lessonCategoryId || ""
                                    }
                                    onChange={event => {
                                        const categoryId =
                                            Number(event.target.value);

                                        if (!categoryId) {
                                            setActiveCategory(null);
                                            return;
                                        }

                                        const selected =
                                            categories.find(
                                                category =>
                                                    category
                                                        .lessonCategoryId ===
                                                    categoryId
                                            );

                                        setActiveCategory(
                                            selected || null
                                        );
                                    }}
                                >
                                    <option value="">
                                        {categories.length === 0
                                            ? "No skills available"
                                            : "All skills"}
                                    </option>

                                    {categories.map(category => (
                                        <option
                                            key={
                                                category.lessonCategoryId
                                            }
                                            value={
                                                category.lessonCategoryId
                                            }
                                        >
                                            {formatSkillName(category.name)}
                                        </option>
                                    ))}
                                </select>

                                <ChevronDown
                                    size={18}
                                    className="lesson-select-arrow"
                                />
                            </label>
                        </div>

                        <div className="lesson-toolbar-bottom">
                            <div className="lesson-category-tabs">
                                <button
                                    type="button"
                                    className={[
                                        "lesson-category-tab",
                                        "total",
                                        activeCategory === null
                                            ? "active"
                                            : ""
                                    ]
                                        .filter(Boolean)
                                        .join(" ")}
                                    onClick={() =>
                                        setActiveCategory(null)
                                    }
                                >
                                    All
                                    <span>{totalLessons}</span>
                                </button>

                                {categories.map(category => (
                                    <button
                                        type="button"
                                        key={category.lessonCategoryId}
                                        className={[
                                            "lesson-category-tab",
                                            activeCategory
                                                ?.lessonCategoryId ===
                                                category.lessonCategoryId
                                                ? "active"
                                                : ""
                                        ]
                                            .filter(Boolean)
                                            .join(" ")}
                                        onClick={() =>
                                            setActiveCategory(category)
                                        }
                                    >
                                        {formatSkillName(category.name)}

                                        <span>
                                            {categoryCounts.find(
                                                item =>
                                                    item.lessonCategoryId ===
                                                    category.lessonCategoryId
                                            )?.lessonCount ?? 0}
                                        </span>
                                    </button>
                                ))}
                            </div>

                            <label className="lesson-search-box">
                                <Search size={20} />

                                <input
                                    type="text"
                                    placeholder="Search lessons..."
                                    value={searchText}
                                    onChange={event =>
                                        setSearchText(
                                            event.target.value
                                        )
                                    }
                                />

                                {searchText && (
                                    <button
                                        type="button"
                                        className="lesson-clear-search"
                                        onClick={() =>
                                            setSearchText("")
                                        }
                                        aria-label="Clear search"
                                    >
                                        <X size={17} />
                                    </button>
                                )}
                            </label>
                        </div>
                    </section>


                    <section className="lesson-section-container">
                        {filteredSections.length === 0 ? (
                            <div className="lesson-empty-state">
                                <div className="lesson-empty-icon">
                                    <BookOpen size={34} />
                                </div>

                                <h3>
                                    {searchText
                                        ? "No lessons found"
                                        : "No Lesson Sections yet"}
                                </h3>

                                <p>
                                    {searchText
                                        ? "No sections or lessons match your search."
                                        : "Create the first Lesson Section for this class and skill."}
                                </p>

                                {!searchText && (
                                    <button
                                        type="button"
                                        className="lesson-btn"
                                        onClick={
                                            handleOpenCreateSection
                                        }
                                    >
                                        <Plus size={18} />
                                        Create Lesson Section
                                    </button>
                                )}
                            </div>
                        ) : (
                            filteredSections.map(section => {
                                const sectionCategory =
                                    getSectionCategory(section);

                                return (
                                    <article
                                        key={section.lessonSectionId}
                                        className={[
                                            "lesson-section",
                                            `lesson-section-${getCategoryType(
                                                sectionCategory
                                            )}`
                                        ].join(" ")}
                                    >
                                        <div className="lesson-section-header">
                                            <div className="lesson-section-info">
                                                <div
                                                    className={[
                                                        "lesson-skill-badge",
                                                        `lesson-skill-${getCategoryType(
                                                            sectionCategory
                                                        )}`
                                                    ].join(" ")}
                                                >
                                                    {getCategoryIcon(
                                                        sectionCategory,
                                                        15
                                                    )}

                                                    <span>
                                                        {formatSkillName(sectionCategory.name)}
                                                    </span>
                                                </div>

                                                <span className="lesson-class-badge">
                                                    {section.className ||
                                                        activeClass?.className ||
                                                        "Class"}
                                                </span>

                                                <span className="lesson-count-badge">
                                                    {section.lessons?.length ||
                                                        0}
                                                    {" lessons"}
                                                </span>
                                            </div>

                                            <div className="lesson-section-title-box">
                                                <h2>
                                                    {section.title}
                                                </h2>

                                                <button
                                                    type="button"
                                                    className="lesson-collapse-btn"
                                                    onClick={() =>
                                                        toggleSection(
                                                            section.lessonSectionId
                                                        )
                                                    }
                                                    aria-label={
                                                        collapsedSections[
                                                            section.lessonSectionId
                                                        ]
                                                            ? "Expand Section"
                                                            : "Collapse Section"
                                                    }
                                                >
                                                    {collapsedSections[
                                                        section.lessonSectionId
                                                    ] ? (
                                                        <ChevronRight size={18} />
                                                    ) : (
                                                        <ChevronDown size={18} />
                                                    )}
                                                </button>
                                            </div>

                                            <div className="lesson-section-actions">
                                                <button
                                                    type="button"
                                                    className="lesson-add-lesson-btn"
                                                    onClick={() =>
                                                        handleOpenCreateLesson(
                                                            section.lessonSectionId
                                                        )
                                                    }
                                                >
                                                    <Plus size={19} />
                                                    <span>
                                                        Add Lesson
                                                    </span>
                                                </button>

                                                <button
                                                    type="button"
                                                    className="lesson-icon-action edit"
                                                    onClick={() =>
                                                        handleOpenEditSection(
                                                            section
                                                        )
                                                    }
                                                    aria-label="Edit Lesson Section"
                                                >
                                                    <Pencil size={18} />
                                                </button>

                                                <button
                                                    type="button"
                                                    className="lesson-icon-action delete"
                                                    onClick={() =>
                                                        handleDeleteSection(
                                                            section.lessonSectionId
                                                        )
                                                    }
                                                    aria-label="Delete Lesson Section"
                                                >
                                                    <Trash2 size={18} />
                                                </button>
                                            </div>
                                        </div>

                                        {
                                            !collapsedSections[
                                            section.lessonSectionId
                                            ] && (
                                                <div className="lesson-list">
                                                    <div className="lesson-table-head" aria-hidden="true">
                                                        <span>STT</span>
                                                        <span>Lesson Title</span>
                                                        <span>Video</span>
                                                        <span>PDF</span>
                                                        <span>Actions</span>
                                                    </div>
                                                    {section.lessons?.length ===
                                                        0 ? (
                                                        <div className="lesson-empty-list">
                                                            This section has no lessons yet.
                                                        </div>
                                                    ) : (
                                                        section.lessons.map(
                                                            (lesson, index) => (
                                                                <div
                                                                    key={
                                                                        lesson.lessonId
                                                                    }
                                                                    className="lesson-card"
                                                                >
                                                                    <span className="lesson-card-index">
                                                                        {index + 1}
                                                                    </span>

                                                                    <div className="lesson-card-main">
                                                                        <div className="lesson-document-icon">
                                                                            <FileText
                                                                                size={
                                                                                    20
                                                                                }
                                                                            />
                                                                        </div>

                                                                        <div className="lesson-card-content">
                                                                            <h3>
                                                                                {
                                                                                    lesson.title
                                                                                }
                                                                            </h3>
                                                                        </div>
                                                                    </div>

                                                                    <span
                                                                        className={
                                                                            lesson.videoUrl
                                                                                ? "lesson-file-status uploaded"
                                                                                : "lesson-file-status missing"
                                                                        }
                                                                    >
                                                                        <Video size={14} />
                                                                        {lesson.videoUrl
                                                                            ? "Uploaded"
                                                                            : "Not uploaded"}
                                                                    </span>

                                                                    <span
                                                                        className={
                                                                            lesson.pdfUrl
                                                                                ? "lesson-file-status pdf-uploaded"
                                                                                : "lesson-file-status missing"
                                                                        }
                                                                    >
                                                                        <FileText size={14} />
                                                                        {lesson.pdfUrl
                                                                            ? "Uploaded"
                                                                            : "Not uploaded"}
                                                                    </span>

                                                                    <div className="lesson-actions">
                                                                        <button
                                                                            type="button"
                                                                            className="lesson-icon-action edit"
                                                                            onClick={() =>
                                                                                handleOpenEditLesson(
                                                                                    lesson,
                                                                                    section.lessonSectionId
                                                                                )
                                                                            }
                                                                            aria-label="Edit lesson"
                                                                        >
                                                                            <Pencil
                                                                                size={
                                                                                    18
                                                                                }
                                                                            />
                                                                        </button>

                                                                        <button
                                                                            type="button"
                                                                            className="lesson-icon-action delete"
                                                                            onClick={() =>
                                                                                handleDeleteLesson(
                                                                                    lesson.lessonId
                                                                                )
                                                                            }
                                                                            aria-label="Delete lesson"
                                                                        >
                                                                            <Trash2
                                                                                size={
                                                                                    18
                                                                                }
                                                                            />
                                                                        </button>
                                                                    </div>
                                                                </div>
                                                            )
                                                        )
                                                    )}
                                                </div>
                                            )
                                        }
                                    </article>
                                );
                            })
                        )}
                    </section>
                </div>
            </main>

            {showSectionModal && (
                <div
                    className="lesson-modal-overlay"
                    onMouseDown={event => {
                        if (
                            event.target === event.currentTarget
                        ) {
                            handleCloseSectionModal(true);
                        }
                    }}
                >
                    <div className="lesson-modal">
                        <div className="lesson-modal-header">
                            <div>
                                <span className="lesson-modal-eyebrow">
                                    Lesson Section
                                </span>

                                <h2>
                                    {editingSection
                                        ? "Edit Section"
                                        : "Create New Section"}
                                </h2>
                            </div>

                            <button
                                type="button"
                                className="lesson-modal-close"
                                onClick={
                                    handleCloseSectionModal
                                }
                                disabled={isSavingSection}
                            >
                                <X size={20} />
                            </button>
                        </div>

                        <div className="lesson-form">
                            <label className="lesson-form-group">
                                <span>Lesson Section Title</span>

                                <input
                                    type="text"
                                    placeholder="e.g. Basic Listening Skills"
                                    value={sectionTitle}
                                    onChange={event =>
                                        setSectionTitle(
                                            event.target.value
                                        )
                                    }
                                    autoFocus
                                />
                            </label>

                            {!editingSection ? (
                                <div className="lesson-section-create-grid">
                                    <label className="lesson-form-group">
                                        <span>Class</span>

                                        <select
                                            value={sectionClassId}
                                            onChange={event =>
                                                setSectionClassId(
                                                    event.target.value
                                                )
                                            }
                                        >
                                            <option value="">
                                                Select a class
                                            </option>

                                            {teacherClasses.map(item => (
                                                <option
                                                    key={item.classRoomId}
                                                    value={item.classRoomId}
                                                >
                                                    {item.courseName}
                                                    {" - "}
                                                    {item.className}
                                                </option>
                                            ))}
                                        </select>
                                    </label>

                                    <label className="lesson-form-group">
                                        <span>Skill</span>

                                        <select
                                            value={sectionCategoryId}
                                            onChange={event =>
                                                setSectionCategoryId(
                                                    event.target.value
                                                )
                                            }
                                        >
                                            <option value="">
                                                Select a skill
                                            </option>

                                            {categories.map(category => (
                                                <option
                                                    key={
                                                        category.lessonCategoryId
                                                    }
                                                    value={
                                                        category.lessonCategoryId
                                                    }
                                                >
                                                    {formatSkillName(category.name)}
                                                </option>
                                            ))}
                                        </select>
                                    </label>
                                </div>
                            ) : (
                                <div className="lesson-modal-summary">
                                    <div>
                                        <span>Class</span>

                                        <strong>
                                            {editingSection.className ||
                                                activeClass?.className ||
                                                "Unknown"}
                                        </strong>
                                    </div>

                                    <div>
                                        <span>Skill</span>

                                        <strong>
                                            {formatSkillName(
                                                editingSection.categoryName ||
                                                getSectionCategory(
                                                    editingSection
                                                ).name
                                            )}
                                        </strong>
                                    </div>
                                </div>
                            )}

                            <div className="lesson-modal-actions">
                                <button
                                    type="button"
                                    className="lesson-btn lesson-btn-cancel"
                                    onClick={
                                        handleCloseSectionModal
                                    }
                                    disabled={isSavingSection}
                                >
                                    Cancel
                                </button>

                                <button
                                    type="button"
                                    className="lesson-btn"
                                    onClick={handleSaveSection}
                                    disabled={isSavingSection}
                                >
                                    {isSavingSection ? (
                                        <>
                                            <span className="lesson-small-spinner" />
                                            Saving...
                                        </>
                                    ) : (
                                        <>
                                            {editingSection ? (
                                                <Pencil size={17} />
                                            ) : (
                                                <Plus size={18} />
                                            )}

                                            {editingSection
                                                ? "Save Changes"
                                                : "Create Section"}
                                        </>
                                    )}
                                </button>
                            </div>
                        </div>
                    </div>
                </div>
            )}

            {showLessonModal && (
                <div
                    className="lesson-modal-overlay"
                    onMouseDown={event => {
                        if (
                            event.target === event.currentTarget
                        ) {
                            handleCloseLessonModal(true);
                        }
                    }}
                >
                    <div className="lesson-modal lesson-modal-large">
                        {isSavingLesson ? (
                            <div className="lesson-loading-box">
                                <div className="lesson-spinner" />

                                <p>Saving lesson...</p>

                                <span>
                                    Video and PDF are being uploaded to the
                                    system
                                </span>
                            </div>
                        ) : (
                            <>
                                <div className="lesson-modal-header">
                                    <div>
                                        <span className="lesson-modal-eyebrow">
                                            Lesson
                                        </span>

                                        <h2>
                                            {editingLesson
                                                ? "Edit Lesson"
                                                : "Create New Lesson"}
                                        </h2>
                                    </div>

                                    <button
                                        type="button"
                                        className="lesson-modal-close"
                                        onClick={
                                            handleCloseLessonModal
                                        }
                                    >
                                        <X size={20} />
                                    </button>
                                </div>

                                <div className="lesson-form">
                                    <label className="lesson-form-group">
                                        <span>Lesson Title</span>

                                        <input
                                            type="text"
                                            placeholder="Enter lesson title"
                                            value={lessonTitle}
                                            onChange={event =>
                                                setLessonTitle(
                                                    event.target.value
                                                )
                                            }
                                            autoFocus
                                        />
                                    </label>

                                    <label className="lesson-form-group">
                                        <span>
                                            Lesson Order
                                        </span>

                                        <input
                                            type="number"
                                            min="1"
                                            placeholder="Order"
                                            value={lessonOrder}
                                            onChange={event =>
                                                setLessonOrder(
                                                    event.target.value
                                                )
                                            }
                                        />
                                    </label>

                                    <div className="lesson-upload-grid">
                                        <label className="lesson-upload-box">
                                            <input
                                                type="file"
                                                    accept=".pdf,.doc,.docx"
                                                onChange={event =>
                                                    setPdfFile(
                                                        event.target
                                                            .files?.[0] ||
                                                        null
                                                    )
                                                }
                                            />

                                            <div className="lesson-upload-icon pdf">
                                                <FileText size={25} />
                                            </div>

                                            <div>
                                                <strong>
                                                    Upload document
                                                </strong>

                                                <span>
                                                    {pdfFile
                                                        ? pdfFile.name
                                                        : editingLesson
                                                            ?.pdfUrl
                                                            ? "A document already exists; select a file to replace it"
                                                            : "Select PDF, DOC, or DOCX file"}
                                                </span>
                                            </div>

                                            <Upload size={19} />
                                        </label>

                                        <label className="lesson-upload-box">
                                            <input
                                                type="file"
                                                accept="video/*"
                                                onChange={event =>
                                                    setVideoFile(
                                                        event.target
                                                            .files?.[0] ||
                                                        null
                                                    )
                                                }
                                            />

                                            <div className="lesson-upload-icon video">
                                                <Video size={25} />
                                            </div>

                                            <div>
                                                <strong>
                                                    Upload Video
                                                </strong>

                                                <span>
                                                    {videoFile
                                                        ? videoFile.name
                                                        : editingLesson
                                                            ?.videoUrl
                                                            ? "A video already exists; select a file to replace it"
                                                            : "Select video file"}
                                                </span>
                                            </div>

                                            <Upload size={19} />
                                        </label>
                                    </div>

                                    <div className="lesson-modal-actions">
                                        <button
                                            type="button"
                                            className="lesson-btn lesson-btn-cancel"
                                            onClick={
                                                handleCloseLessonModal
                                            }
                                        >
                                            Cancel
                                        </button>

                                        <button
                                            type="button"
                                            className="lesson-btn"
                                            onClick={
                                                handleSaveLesson
                                            }
                                        >
                                            {editingLesson ? (
                                                <Pencil size={17} />
                                            ) : (
                                                <Plus size={18} />
                                            )}

                                            {editingLesson
                                                ? "Save Lesson"
                                                : "Create Lesson"}
                                        </button>
                                    </div>
                                </div>
                            </>
                        )}
                    </div>
                </div>
            )}
        </div>
    );
}

export default TeacherLessonPage;
