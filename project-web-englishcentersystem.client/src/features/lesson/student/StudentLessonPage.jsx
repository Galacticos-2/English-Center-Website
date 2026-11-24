import { useEffect, useMemo, useState } from "react";
import {
    BookOpen,
    Check,
  
    CirclePlay,
    Download,
    FileText,
    Headphones,
    LoaderCircle,
    Mic,
    PenLine,
 
    RotateCcw,
    CheckCircle2,
    VideoOff
} from "lucide-react";

import HlsPlayer from "./components/HlsPlayer";
import PdfViewer from "./components/PdfViewer";
import LessonSidebar from "./components/LessonSidebar";

import {
    getLessonCategoriesApi,
    getMyLessonSectionsApi,
    markLessonCompletedApi
} from "../api/lessonApi";

import "../styles/lesson.css";

function StudentLessonPage() {
    const [categories, setCategories] = useState([]);
    const [activeCategory, setActiveCategory] = useState(null);

    const [sections, setSections] = useState([]);
    const [activeLesson, setActiveLesson] = useState(null);

    const [isSidebarOpen, setIsSidebarOpen] = useState(true);
    const [isLoadingCategories, setIsLoadingCategories] =
        useState(true);
    const [isLoadingLessons, setIsLoadingLessons] =
        useState(false);
    const [isCompleting, setIsCompleting] =
        useState(false);

    const isWordDocument = fileUrl =>
        /\.(doc|docx)(?:$|\?)/i.test(fileUrl || "");

    const getCategoryIcon = categoryName => {
        const name = categoryName?.toLowerCase();

        if (
            name?.includes("ngữ pháp") ||
            name?.includes("grammar")
        ) {
            return <FileText size={18} />;
        }

        if (
            name?.includes("nghe") ||
            name?.includes("listening")
        ) {
            return <Headphones size={18} />;
        }

        if (
            name?.includes("nói") ||
            name?.includes("speaking")
        ) {
            return <Mic size={18} />;
        }

        if (
            name?.includes("đọc") ||
            name?.includes("reading")
        ) {
            return <BookOpen size={18} />;
        }

        if (
            name?.includes("viết") ||
            name?.includes("writing")
        ) {
            return <PenLine size={18} />;
        }

        return <BookOpen size={18} />;
    };

    const formatSkillName = categoryName => {
        const name = categoryName?.toLowerCase().trim();

        if (name?.includes("ngữ pháp") || name?.includes("grammar")) return "Grammar";
        if (name?.includes("nghe") || name?.includes("listening")) return "Listening";
        if (name?.includes("nói") || name?.includes("speaking")) return "Speaking";
        if (name?.includes("đọc") || name?.includes("reading")) return "Reading";
        if (name?.includes("viết") || name?.includes("writing")) return "Writing";

        return categoryName;
    };

    const allLessons = useMemo(
        () =>
            sections.flatMap(
                section => section.lessons ?? []
            ),
        [sections]
    );

   

    const activeLessonIndex = useMemo(
        () =>
            allLessons.findIndex(
                lesson =>
                    lesson.lessonId ===
                    activeLesson?.lessonId
            ),
        [allLessons, activeLesson]
    );

    const previousLesson =
        activeLessonIndex > 0
            ? allLessons[activeLessonIndex - 1]
            : null;

    const nextLesson =
        activeLessonIndex >= 0 &&
            activeLessonIndex < allLessons.length - 1
            ? allLessons[activeLessonIndex + 1]
            : null;

 
    const loadCategories = async () => {
        try {
            setIsLoadingCategories(true);

            const data =
                await getLessonCategoriesApi();

            setCategories(data ?? []);

            if (data?.length > 0) {
                setActiveCategory(data[0]);
            }
        } catch (error) {
            console.error(
                "Unable to load lesson categories:",
                error
            );

            setCategories([]);
        } finally {
            setIsLoadingCategories(false);
        }
    };
    const getCategoryDescription =
        categoryName => {
            const name =
                categoryName
                    ?.toLowerCase()
                    .trim();

            if (
                name?.includes("ngữ pháp") ||
                name?.includes("grammar")
            ) {
                return "Structure and foundations";
            }

            if (
                name?.includes("nghe") ||
                name?.includes("listening")
            ) {
                return "Listening practice and response";
            }

            if (
                name?.includes("nói") ||
                name?.includes("speaking")
            ) {
                return "Pronunciation and communication";
            }

            if (
                name?.includes("đọc") ||
                name?.includes("reading")
            ) {
                return "Reading comprehension and vocabulary";
            }

            if (
                name?.includes("viết") ||
                name?.includes("writing")
            ) {
                return "Expression and academic writing";
            }

            return "Learning content";
        };
    const loadSections = async () => {
        if (!activeCategory) {
            setSections([]);
            setActiveLesson(null);
            return;
        }

        try {
            setIsLoadingLessons(true);

            const data =
                await getMyLessonSectionsApi(
                    activeCategory.lessonCategoryId
                );

            const normalizedSections =
                data ?? [];

            setSections(normalizedSections);

            const firstLesson =
                normalizedSections
                    .flatMap(
                        section =>
                            section.lessons ?? []
                    )
                    .at(0);

            setActiveLesson(firstLesson ?? null);
        } catch (error) {
            console.error(
                "Unable to load lessons:",
                error
            );

            setSections([]);
            setActiveLesson(null);
        } finally {
            setIsLoadingLessons(false);
        }
    };

    const handleMarkCompleted = async () => {
        if (
            !activeLesson ||
            activeLesson.isCompleted ||
            isCompleting
        ) {
            return;
        }

        try {
            setIsCompleting(true);

            await markLessonCompletedApi(
                activeLesson.lessonId
            );

            setSections(previousSections =>
                previousSections.map(section => ({
                    ...section,
                    lessons:
                        section.lessons?.map(
                            lesson =>
                                lesson.lessonId ===
                                    activeLesson.lessonId
                                    ? {
                                        ...lesson,
                                        isCompleted:
                                            true
                                    }
                                    : lesson
                        ) ?? []
                }))
            );

            setActiveLesson(previousLessonState => ({
                ...previousLessonState,
                isCompleted: true
            }));
        } catch (error) {
            console.error(
                "Unable to mark the lesson as completed:",
                error
            );
        } finally {
            setIsCompleting(false);
        }
    };

    const handleChangeCategory = category => {
        if (
            category.lessonCategoryId ===
            activeCategory?.lessonCategoryId
        ) {
            return;
        }

        setActiveCategory(category);
        setActiveLesson(null);
    };

    useEffect(() => {
        loadCategories();
    }, []);

    useEffect(() => {
        loadSections();
    }, [activeCategory]);

    return (
        <div className="student-learning-page">
            <section className="student-learning-skill-navigation">
                <div className="student-skill-navigation-inner">
                    <div className="student-skill-navigation-heading">
                        <span className="student-skill-eyebrow">
                            Learning Program
                        </span>

                        <strong>
                            Select a skill
                        </strong>
                    </div>

                    <div className="student-skill-tabs-wrapper">
                        {isLoadingCategories ? (
                            <div className="student-skill-loading">
                                <LoaderCircle
                                    size={18}
                                    className="student-spin"
                                />

                                <span>
                                    Loading skills...
                                </span>
                            </div>
                        ) : (
                            <div
                                className="student-skill-switcher"
                                role="tablist"
                                aria-label="Learning skill list"
                            >
                                {categories.map(category => {
                                    const isActive =
                                        activeCategory
                                            ?.lessonCategoryId ===
                                        category.lessonCategoryId;

                                    const lessonCount =
                                        isActive
                                            ? allLessons.length
                                            : null;

                                    return (
                                        <button
                                            type="button"
                                            role="tab"
                                            aria-selected={isActive}
                                            key={
                                                category.lessonCategoryId
                                            }
                                            className={
                                                isActive
                                                    ? "student-skill-switcher-item active"
                                                    : "student-skill-switcher-item"
                                            }
                                            onClick={() =>
                                                handleChangeCategory(
                                                    category
                                                )
                                            }
                                        >
                                            <span className="student-skill-switcher-icon">
                                                {getCategoryIcon(
                                                    category.name
                                                )}
                                            </span>

                                            <span className="student-skill-switcher-content">
                                                <strong>
                                                    {formatSkillName(
                                                        category.name
                                                    )}
                                                </strong>

                                                <small>
                                                    {getCategoryDescription(
                                                        category.name
                                                    )}
                                                </small>
                                            </span>

                                            {isActive && (
                                                <span className="student-skill-switcher-badge">
                                                    {lessonCount} lessons
                                                </span>
                                            )}

                                            <span className="student-skill-switcher-indicator" />
                                        </button>
                                    );
                                })}
                            </div>
                        )}
                    </div>

                    
                </div>
            </section>

            <div
                className={
                    isSidebarOpen
                        ? "lesson-student-layout"
                        : "lesson-student-layout sidebar-collapsed"
                }
            >
                <LessonSidebar
                    sections={sections}
                    activeLesson={activeLesson}
                    setActiveLesson={setActiveLesson}
                    isSidebarOpen={isSidebarOpen}
                    setIsSidebarOpen={setIsSidebarOpen}
                />

                <main className="student-lesson-main">
                    {isLoadingLessons ? (
                        <div className="student-lesson-loading-state">
                            <div className="student-lesson-loading-icon">
                                <LoaderCircle
                                    size={34}
                                    className="student-spin"
                                />
                            </div>

                            <h2>
                                Loading lesson content
                            </h2>

                            <p>
                                Please wait a moment.
                            </p>
                        </div>
                    ) : !activeLesson ? (
                        <div className="student-lesson-empty-state">
                            <div className="student-lesson-empty-icon">
                                <BookOpen size={36} />
                            </div>

                            <span className="student-empty-eyebrow">
                                Learning Content
                            </span>

                            <h2>
                                No Lessons Yet
                            </h2>

                            <p>
                                There is no content for this
                                skill yet. Select another skill
                                to continue learning.
                            </p>
                        </div>
                    ) : (
                        <div className="student-lesson-content">
                                    <section className="student-lesson-hero">
                                        <div className="student-lesson-hero-content">

                                            <div className="student-lesson-breadcrumb">
                                                <span>
                                                    {formatSkillName(
                                                        activeCategory?.name
                                                    )}
                                                </span>

                                                <span>/</span>

                                                <span>
                                                    {
                                                        sections.find(section =>
                                                            section.lessons?.some(
                                                                lesson =>
                                                                    lesson.lessonId ===
                                                                    activeLesson.lessonId
                                                            )
                                                        )?.title
                                                    }
                                                </span>

                                                <span>/</span>

                                                <strong>
                                                    {activeLesson.title}
                                                </strong>
                                            </div>

                                            <div className="student-lesson-hero-main">

                                                <div className="student-lesson-hero-icon">
                                                    <CirclePlay size={32} />
                                                </div>

                                                <div className="student-lesson-hero-info">

                                                    <h1>
                                                        {activeLesson.title}
                                                    </h1>

                                                    

                                                </div>

                                                <div className="student-lesson-hero-status">

                                                    {activeLesson.isCompleted ? (
                                                        <span className="student-lesson-status completed">
                                                            <CheckCircle2 size={18} />
                                                            Completed
                                                        </span>
                                                    ) : (
                                                        <span className="student-lesson-status">
                                                            In progress
                                                        </span>
                                                    )}

                                                </div>

                                            </div>

                                        </div>
                                    </section>

                            <section className="student-learning-resource-card student-video-card">
                                <div className="student-resource-card-header">
                                    <div className="student-resource-card-title">
                                        <div className="student-resource-icon video">
                                            <CirclePlay
                                                size={21}
                                            />
                                        </div>

                                        <div>
                                            <span>
                                                Lecture
                                            </span>

                                            <h2>
                                                Lesson Video
                                            </h2>
                                        </div>
                                    </div>

                                    {activeLesson.videoUrl && (
                                        <span className="student-resource-available">
                                            Available
                                        </span>
                                    )}
                                </div>

                                <div className="student-video-frame">
                                    {activeLesson.videoProcessingStatus ===
                                        "Processing" ? (
                                        <div className="student-resource-placeholder">
                                            <div className="student-placeholder-icon processing">
                                                <LoaderCircle
                                                    size={34}
                                                    className="student-spin"
                                                />
                                            </div>

                                            <h3>
                                                Video is being
                                                processed
                                            </h3>

                                            <p>
                                                The system is
                                                preparing the video.
                                                Please return
                                                later.
                                            </p>
                                        </div>
                                    ) : activeLesson.videoProcessingStatus ===
                                        "Failed" ? (
                                        <div className="student-resource-placeholder">
                                            <div className="student-placeholder-icon failed">
                                                <RotateCcw
                                                    size={34}
                                                />
                                            </div>

                                            <h3>
                                                Unable to process
                                                video
                                            </h3>

                                            <p>
                                                An error occurred
                                                while processing
                                                the video.
                                            </p>
                                        </div>
                                    ) : activeLesson.videoUrl ? (
                                        <HlsPlayer
                                            src={
                                                activeLesson.videoUrl
                                            }
                                        />
                                    ) : (
                                        <div className="student-resource-placeholder">
                                            <div className="student-placeholder-icon">
                                                <VideoOff
                                                    size={34}
                                                />
                                            </div>

                                            <h3>
                                                No Video Yet
                                            </h3>

                                            <p>
                                                The teacher has not
                                                added a video to
                                                this lesson yet.
                                            </p>
                                        </div>
                                    )}
                                </div>
                            </section>

                            <section className="student-learning-resource-card student-document-card">
                                <div className="student-resource-card-header">
                                    <div className="student-resource-card-title">
                                        <div className="student-resource-icon document">
                                            <FileText
                                                size={21}
                                            />
                                        </div>

                                        <div>
                                            <span>
                                                Learning Materials
                                            </span>

                                            <h2>
                                                Lesson Content
                                            </h2>
                                        </div>
                                    </div>

                                    {activeLesson.pdfUrl && (
                                        <span className="student-resource-available document">
                                            {isWordDocument(activeLesson.pdfUrl)
                                                ? "WORD"
                                                : "PDF"}
                                        </span>
                                    )}
                                </div>

                                <div className="student-document-frame">
                                    {activeLesson.pdfUrl ? (
                                        isWordDocument(activeLesson.pdfUrl) ? (
                                            <div className="student-resource-placeholder document">
                                                <div className="student-placeholder-icon document">
                                                    <FileText size={34} />
                                                </div>

                                                <h3>Word document</h3>

                                                <p>
                                                    Download the lesson material to open it in a compatible document reader.
                                                </p>

                                                <a
                                                    className="student-document-download"
                                                    href={activeLesson.pdfUrl}
                                                    target="_blank"
                                                    rel="noreferrer"
                                                >
                                                    <Download size={18} />
                                                    Download document
                                                </a>
                                            </div>
                                        ) : (
                                            <PdfViewer
                                                file={
                                                    activeLesson.pdfUrl
                                                }
                                            />
                                        )
                                    ) : (
                                        <div className="student-resource-placeholder document">
                                            <div className="student-placeholder-icon document">
                                                <FileText
                                                    size={34}
                                                />
                                            </div>

                                            <h3>
                                                No Materials Yet
                                            </h3>

                                            <p>
                                                No document material has
                                                been attached to this
                                                lesson yet.
                                            </p>
                                        </div>
                                    )}
                                </div>
                            </section>

                            <footer className="student-lesson-footer">
                                <div className="student-lesson-footer-navigation">
                                    <button
                                        type="button"
                                        className="student-lesson-nav-btn"
                                        disabled={!previousLesson}
                                        onClick={() =>
                                            setActiveLesson(
                                                previousLesson
                                            )
                                        }
                                    >
                                        <span>←</span>

                                        <div>
                                            <small>
                                                Previous Lesson
                                            </small>

                                            <strong>
                                                {previousLesson
                                                    ?.title ??
                                                    "None"}
                                            </strong>
                                        </div>
                                    </button>

                                    <button
                                        type="button"
                                        className="student-lesson-nav-btn next"
                                        disabled={!nextLesson}
                                        onClick={() =>
                                            setActiveLesson(
                                                nextLesson
                                            )
                                        }
                                    >
                                        <div>
                                            <small>
                                                Next Lesson
                                            </small>

                                            <strong>
                                                {nextLesson
                                                    ?.title ??
                                                    "No more lessons"}
                                            </strong>
                                        </div>

                                        <span>→</span>
                                    </button>
                                </div>

                                <button
                                    type="button"
                                    className={
                                        activeLesson.isCompleted
                                            ? "student-complete-lesson-btn completed"
                                            : "student-complete-lesson-btn"
                                    }
                                    onClick={
                                        handleMarkCompleted
                                    }
                                    disabled={
                                        activeLesson.isCompleted ||
                                        isCompleting
                                    }
                                >
                                    {isCompleting ? (
                                        <LoaderCircle
                                            size={19}
                                            className="student-spin"
                                        />
                                    ) : (
                                        <Check size={19} />
                                    )}

                                    <span>
                                        {activeLesson.isCompleted
                                            ? "Lesson Completed"
                                            : "Mark as Completed"}
                                    </span>
                                </button>
                            </footer>
                        </div>
                    )}
                </main>
            </div>
        </div>
    );
}

export default StudentLessonPage;
