import * as Accordion from "@radix-ui/react-accordion";
import { AnimatePresence, motion } from "framer-motion";
import {
    BookOpen,
    Check,
    ChevronDown,
    Circle,
    Menu,
    PanelLeftClose,
    PanelLeftOpen,
    X
} from "lucide-react";

function LessonSidebar({
    sections = [],
    activeLesson,
    setActiveLesson,
    isSidebarOpen,
    setIsSidebarOpen
}) {
    const allLessons = sections.flatMap(
        section => section.lessons ?? []
    );

    const totalLessons = allLessons.length;

    const completedLessons = allLessons.filter(
        lesson => lesson.isCompleted
    ).length;

    const progressPercent =
        totalLessons > 0
            ? Math.round(
                (completedLessons / totalLessons) * 100
            )
            : 0;

    const getSectionStats = (lessons = []) => {
        const completed = lessons.filter(
            lesson => lesson.isCompleted
        ).length;

        return {
            total: lessons.length,
            completed,
            percent:
                lessons.length > 0
                    ? Math.round(
                        (completed / lessons.length) * 100
                    )
                    : 0
        };
    };

    const handleSelectLesson = lesson => {
        setActiveLesson(lesson);

        if (window.innerWidth <= 900) {
            setIsSidebarOpen(false);
        }
    };

    return (
        <>
            {!isSidebarOpen && (
                <button
                    type="button"
                    className="student-sidebar-mobile-open"
                    onClick={() => setIsSidebarOpen(true)}
                    aria-label="Open lesson list"
                >
                    <Menu size={20} />

                    <span>Lesson List</span>
                </button>
            )}

            <AnimatePresence>
                {isSidebarOpen && (
                    <motion.button
                        type="button"
                        className="student-sidebar-overlay"
                        onClick={() => setIsSidebarOpen(false)}
                        aria-label="Close lesson list"
                        initial={{ opacity: 0 }}
                        animate={{ opacity: 1 }}
                        exit={{ opacity: 0 }}
                    />
                )}
            </AnimatePresence>

            <motion.aside
                className={
                    isSidebarOpen
                        ? "lesson-student-sidebar open"
                        : "lesson-student-sidebar collapsed"
                }
                animate={{
                    width: isSidebarOpen ? 350 : 72
                }}
                transition={{
                    duration: 0.25,
                    ease: "easeInOut"
                }}
            >
                <div className="student-sidebar-topbar">
                    {isSidebarOpen && (
                        <div className="student-sidebar-brand">
                            <div className="student-sidebar-brand-icon">
                                <BookOpen size={21} />
                            </div>

                            <div>
                                <span>Learning Program</span>
                                <strong>Lesson Content</strong>
                            </div>
                        </div>
                    )}

                    <button
                        type="button"
                        className="student-sidebar-toggle"
                        onClick={() =>
                            setIsSidebarOpen(!isSidebarOpen)
                        }
                        title={
                            isSidebarOpen
                                ? "Collapse list"
                                : "Open list"
                        }
                        aria-label={
                            isSidebarOpen
                                ? "Collapse list"
                                : "Open list"
                        }
                    >
                        <span className="student-sidebar-desktop-icon">
                            {isSidebarOpen ? (
                                <PanelLeftClose size={20} />
                            ) : (
                                <PanelLeftOpen size={20} />
                            )}
                        </span>

                        <X
                            size={20}
                            className="student-sidebar-mobile-close"
                        />
                    </button>
                </div>

                <AnimatePresence mode="wait">
                    {isSidebarOpen ? (
                        <motion.div
                            key="sidebar-content"
                            className="student-sidebar-content"
                            initial={{
                                opacity: 0,
                                x: -10
                            }}
                            animate={{
                                opacity: 1,
                                x: 0
                            }}
                            exit={{
                                opacity: 0,
                                x: -10
                            }}
                            transition={{
                                duration: 0.18
                            }}
                        >
                            <section className="student-course-progress">
                                <div className="student-progress-header">
                                    <div>
                                        <span>Course Progress</span>

                                        <strong>
                                            {progressPercent}%
                                        </strong>
                                    </div>

                                    <div className="student-progress-count">
                                        {completedLessons}/{totalLessons}
                                    </div>
                                </div>

                                <div className="student-progress-track">
                                    <motion.div
                                        className="student-progress-value"
                                        initial={{ width: 0 }}
                                        animate={{
                                            width: `${progressPercent}%`
                                        }}
                                        transition={{
                                            duration: 0.5,
                                            ease: "easeOut"
                                        }}
                                    />
                                </div>

                                <p>
                                    {progressPercent === 100
                                        ? "You have completed all lessons."
                                        : "Keep learning to complete the course."}
                                </p>
                            </section>

                            <div className="student-sidebar-section-heading">
                                <span>Content</span>

                                <strong>
                                    {sections.length} sections
                                </strong>
                            </div>

                            {sections.length === 0 ? (
                                <div className="student-sidebar-empty">
                                    <BookOpen size={28} />

                                    <strong>
                                        No content yet
                                    </strong>

                                    <span>
                                        This course does not have any lessons yet.
                                    </span>
                                </div>
                            ) : (
                                <Accordion.Root
                                    type="multiple"
                                    defaultValue={sections.map(
                                        section =>
                                            String(
                                                section.lessonSectionId
                                            )
                                    )}
                                    className="student-lesson-accordion"
                                >
                                    {sections.map(
                                        (section, sectionIndex) => {
                                            const lessons =
                                                section.lessons ?? [];

                                            const stats =
                                                getSectionStats(
                                                    lessons
                                                );

                                            return (
                                                <Accordion.Item
                                                    key={
                                                        section.lessonSectionId
                                                    }
                                                    value={String(
                                                        section.lessonSectionId
                                                    )}
                                                    className="student-lesson-section"
                                                >
                                                    <Accordion.Header className="student-section-header">
                                                        <Accordion.Trigger className="student-section-trigger">
                                                            <div className="student-section-number">
                                                                {String(
                                                                    sectionIndex +
                                                                    1
                                                                ).padStart(
                                                                    2,
                                                                    "0"
                                                                )}
                                                            </div>

                                                            <div className="student-section-main">
                                                                <strong>
                                                                    {
                                                                        section.title
                                                                    }
                                                                </strong>

                                                                <div className="student-section-subtitle">
                                                                    <span>
                                                                        {
                                                                            stats.completed
                                                                        }
                                                                        /
                                                                        {
                                                                            stats.total
                                                                        }{" "}
                                                                        lessons
                                                                        completed
                                                                    </span>

                                                                    {stats.percent ===
                                                                        100 && (
                                                                            <span className="student-section-complete">
                                                                                Completed
                                                                            </span>
                                                                        )}
                                                                </div>
                                                            </div>

                                                            <ChevronDown
                                                                size={18}
                                                                className="student-section-chevron"
                                                            />
                                                        </Accordion.Trigger>
                                                    </Accordion.Header>

                                                    <Accordion.Content className="student-section-content">
                                                        {lessons.length ===
                                                            0 ? (
                                                            <div className="student-section-empty">
                                                                No lessons in
                                                                this section
                                                            </div>
                                                        ) : (
                                                            <div className="student-lesson-list">
                                                                {lessons.map(
                                                                    (
                                                                        lesson,
                                                                        lessonIndex
                                                                    ) => {
                                                                        const isActive =
                                                                            activeLesson?.lessonId ===
                                                                            lesson.lessonId;

                                                                        return (
                                                                            <button
                                                                                type="button"
                                                                                key={
                                                                                    lesson.lessonId
                                                                                }
                                                                                className={[
                                                                                    "student-lesson-card",
                                                                                    isActive
                                                                                        ? "active"
                                                                                        : "",
                                                                                    lesson.isCompleted
                                                                                        ? "completed"
                                                                                        : ""
                                                                                ]
                                                                                    .filter(
                                                                                        Boolean
                                                                                    )
                                                                                    .join(
                                                                                        " "
                                                                                    )}
                                                                                onClick={() =>
                                                                                    handleSelectLesson(
                                                                                        lesson
                                                                                    )
                                                                                }
                                                                            >
                                                                                <div className="student-lesson-order">
                                                                                    {lesson.isCompleted ? (
                                                                                        <Check
                                                                                            size={
                                                                                                15
                                                                                            }
                                                                                        />
                                                                                    ) : (
                                                                                        <span>
                                                                                            {lessonIndex +
                                                                                                1}
                                                                                        </span>
                                                                                    )}
                                                                                </div>

                                                                                <div className="student-lesson-card-content">
                                                                                    <strong>
                                                                                        {
                                                                                            lesson.title
                                                                                        }
                                                                                    </strong>

                                                                                    <span>
                                                                                        {lesson.isCompleted
                                                                                            ? "Completed"
                                                                                            : isActive
                                                                                                ? "Learning"
                                                                                                : "Not completed"}
                                                                                    </span>
                                                                                </div>

                                                                                {isActive ? (
                                                                                    <span className="student-active-indicator" />
                                                                                ) : (
                                                                                    <Circle
                                                                                        size={
                                                                                            15
                                                                                        }
                                                                                        className="student-lesson-state-icon"
                                                                                    />
                                                                                )}
                                                                            </button>
                                                                        );
                                                                    }
                                                                )}
                                                            </div>
                                                        )}
                                                    </Accordion.Content>
                                                </Accordion.Item>
                                            );
                                        }
                                    )}
                                </Accordion.Root>
                            )}
                        </motion.div>
                    ) : (
                        <motion.div
                            key="collapsed-sidebar"
                            className="student-sidebar-collapsed-content"
                            initial={{ opacity: 0 }}
                            animate={{ opacity: 1 }}
                            exit={{ opacity: 0 }}
                        >
                            <div className="student-collapsed-progress">
                                <svg
                                    viewBox="0 0 44 44"
                                    className="student-progress-circle"
                                >
                                    <circle
                                        cx="22"
                                        cy="22"
                                        r="18"
                                        className="student-progress-circle-bg"
                                    />

                                    <circle
                                        cx="22"
                                        cy="22"
                                        r="18"
                                        className="student-progress-circle-value"
                                        style={{
                                            strokeDashoffset:
                                                113 -
                                                (113 *
                                                    progressPercent) /
                                                100
                                        }}
                                    />
                                </svg>

                                <span>{progressPercent}%</span>
                            </div>

                            <div className="student-collapsed-divider" />

                            {sections.slice(0, 7).map(
                                (section, index) => (
                                    <button
                                        type="button"
                                        key={
                                            section.lessonSectionId
                                        }
                                        className="student-collapsed-section"
                                        onClick={() =>
                                            setIsSidebarOpen(true)
                                        }
                                        title={section.title}
                                    >
                                        {index + 1}
                                    </button>
                                )
                            )}
                        </motion.div>
                    )}
                </AnimatePresence>
            </motion.aside>
        </>
    );
}

export default LessonSidebar;
