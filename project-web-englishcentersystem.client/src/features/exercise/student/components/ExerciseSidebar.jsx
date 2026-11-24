import * as Accordion from "@radix-ui/react-accordion";
import { AnimatePresence, motion } from "framer-motion";
import {
    Check,
    ChevronDown,
    Circle,
    ClipboardList,
    Menu,
    PanelLeftClose,
    PanelLeftOpen,
    X
} from "lucide-react";

function ExerciseSidebar({
    sections = [],
    activeExercise,
    setActiveExercise,
    isSidebarOpen,
    setIsSidebarOpen
}) {
    const allExercises = sections.flatMap(
        section => section.exercises ?? []
    );

    const totalExercises = allExercises.length;

    const completedExercises = allExercises.filter(
        exercise => exercise.attemptCount > 0
    ).length;

    const progressPercent =
        totalExercises > 0
            ? Math.round(
                (completedExercises / totalExercises) * 100
            )
            : 0;

    const getSectionStats = (exercises = []) => {
        const completed = exercises.filter(
            exercise => exercise.attemptCount > 0
        ).length;

        return {
            total: exercises.length,
            completed,
            percent:
                exercises.length > 0
                    ? Math.round(
                        (completed / exercises.length) * 100
                    )
                    : 0
        };
    };

    const handleSelectExercise = exercise => {
        setActiveExercise(exercise);

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
                    aria-label="Open exercise list"
                >
                    <Menu size={20} />
                    <span>Exercise List</span>
                </button>
            )}

            <AnimatePresence>
                {isSidebarOpen && (
                    <motion.button
                        type="button"
                        className="student-sidebar-overlay"
                        onClick={() => setIsSidebarOpen(false)}
                        aria-label="Close exercise list"
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
                                <ClipboardList size={21} />
                            </div>

                            <div>
                                <span>Practice Program</span>
                                <strong>Exercise Content</strong>
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
                                ? "Thu gọn danh sách"
                                : "Mở danh sách"
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
                            initial={{ opacity: 0, x: -10 }}
                            animate={{ opacity: 1, x: 0 }}
                            exit={{ opacity: 0, x: -10 }}
                            transition={{ duration: 0.18 }}
                        >
                            <section className="student-course-progress">
                                <div className="student-progress-header">
                                    <div>
                                        <span>Exercise Progress</span>
                                        <strong>{progressPercent}%</strong>
                                    </div>

                                    <div className="student-progress-count">
                                        {completedExercises}/{totalExercises}
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
                                        ? "You have completed all exercises."
                                        : "Keep practicing to reach your goal."}
                                </p>
                            </section>

                            <div className="student-sidebar-section-heading">
                                <span>Content</span>
                                <strong>{sections.length} sections</strong>
                            </div>

                            {sections.length === 0 ? (
                                <div className="student-sidebar-empty">
                                    <ClipboardList size={28} />
                                    <strong>No content yet</strong>
                                    <span>
                                        There are no exercises for this skill yet.
                                    </span>
                                </div>
                            ) : (
                                <Accordion.Root
                                    type="multiple"
                                    defaultValue={sections.map(
                                        section =>
                                            String(
                                                section.exerciseSectionId
                                            )
                                    )}
                                    className="student-lesson-accordion"
                                >
                                    {sections.map(
                                        (section, sectionIndex) => {
                                            const exercises =
                                                section.exercises ?? [];

                                            const stats =
                                                getSectionStats(exercises);

                                            return (
                                                <Accordion.Item
                                                    key={
                                                        section.exerciseSectionId
                                                    }
                                                    value={String(
                                                        section.exerciseSectionId
                                                    )}
                                                    className="student-lesson-section"
                                                >
                                                    <Accordion.Header className="student-section-header">
                                                        <Accordion.Trigger className="student-section-trigger">
                                                            <div className="student-section-number">
                                                                {String(
                                                                    sectionIndex + 1
                                                                ).padStart(
                                                                    2,
                                                                    "0"
                                                                )}
                                                            </div>

                                                            <div className="student-section-main">
                                                                <strong>
                                                                    {section.title}
                                                                </strong>

                                                                <div className="student-section-subtitle">
                                                                    <span>
                                                                        {stats.completed}/
                                                                        {stats.total}{" "}
                                                                        completed exercises
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
                                                        {exercises.length === 0 ? (
                                                            <div className="student-section-empty">
                                                                No exercises in this section
                                                            </div>
                                                        ) : (
                                                            <div className="student-lesson-list">
                                                                {exercises.map(
                                                                    (
                                                                        exercise,
                                                                        exerciseIndex
                                                                    ) => {
                                                                        const isActive =
                                                                            activeExercise?.exerciseAssignmentId ===
                                                                            exercise.exerciseAssignmentId;

                                                                        const isCompleted =
                                                                            exercise.attemptCount > 0;

                                                                        return (
                                                                            <button
                                                                                type="button"
                                                                                key={
                                                                                    exercise.exerciseAssignmentId
                                                                                }
                                                                                className={[
                                                                                    "student-lesson-card",
                                                                                    isActive
                                                                                        ? "active"
                                                                                        : "",
                                                                                    isCompleted
                                                                                        ? "completed"
                                                                                        : ""
                                                                                ]
                                                                                    .filter(Boolean)
                                                                                    .join(" ")}
                                                                                onClick={() =>
                                                                                    handleSelectExercise(
                                                                                        exercise
                                                                                    )
                                                                                }
                                                                            >
                                                                                <div className="student-lesson-order">
                                                                                    {isCompleted ? (
                                                                                        <Check
                                                                                            size={15}
                                                                                        />
                                                                                    ) : (
                                                                                        <span>
                                                                                            {exerciseIndex + 1}
                                                                                        </span>
                                                                                    )}
                                                                                </div>

                                                                                <div className="student-lesson-card-content">
                                                                                    <strong>
                                                                                        {exercise.title}
                                                                                    </strong>

                                                                                    <span>
                                                                                        {isCompleted
                                                                                            ? `Completed ${exercise.attemptCount} attempt(s)`
                                                                                            : isActive
                                                                                                ? "Viewing"
                                                                                                : "Not started"}
                                                                                    </span>
                                                                                </div>

                                                                                {isActive ? (
                                                                                    <span className="student-active-indicator" />
                                                                                ) : (
                                                                                    <Circle
                                                                                        size={15}
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
                                                (113 * progressPercent) / 100
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
                                        key={section.exerciseSectionId}
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

export default ExerciseSidebar;
