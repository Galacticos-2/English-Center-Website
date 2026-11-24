import {
    useEffect,
    useState
} from "react";

import {
    Pencil,
    Trash2,
    Eye,
    Plus
} from "lucide-react";

import TeacherSidebar
    from "../../../lesson/teacher/TeacherSidebar";

import {
    createReadingTaskApi,
    deleteReadingTaskApi,
    getReadingTaskDetailApi,
    getReadingTasksApi,
    updateReadingTaskApi
} from "../api/readingApi";

import "../../../lesson/styles/lesson.css";
import "../../../exercise/teacher/styles/questionBank.css";

function TeacherReadingPage() {
    const [tasks, setTasks] = useState([]);
    const [courseType, setCourseType] = useState("");

    const [showModal, setShowModal] = useState(false);
    const [viewTask, setViewTask] = useState(null);
    const [editingTaskId, setEditingTaskId] = useState(null);

    const [title, setTitle] = useState("");
    const [instruction, setInstruction] = useState("");
    const [passageText, setPassageText] = useState("");
    const [formCourseType, setFormCourseType] = useState(1);
    const [isActive, setIsActive] = useState(true);

    const [questions, setQuestions] = useState([
        {
            content: "",
            orderIndex: 1,
            point: 1,
            options: [
                {
                    content: "",
                    isCorrect: true,
                    orderIndex: 1
                },
                {
                    content: "",
                    isCorrect: false,
                    orderIndex: 2
                },
                {
                    content: "",
                    isCorrect: false,
                    orderIndex: 3
                },
                {
                    content: "",
                    isCorrect: false,
                    orderIndex: 4
                }
            ]
        }
    ]);

    const loadTasks = async () => {
        const data = await getReadingTasksApi(courseType);
        setTasks(data);
    };

    useEffect(() => {
        loadTasks();
    }, [courseType]);

    const resetForm = () => {
        setEditingTaskId(null);
        setTitle("");
        setInstruction("");
        setPassageText("");
        setFormCourseType(1);
        setIsActive(true);
        setQuestions([
            {
                content: "",
                orderIndex: 1,
                point: 1,
                options: [
                    {
                        content: "",
                        isCorrect: true,
                        orderIndex: 1
                    },
                    {
                        content: "",
                        isCorrect: false,
                        orderIndex: 2
                    },
                    {
                        content: "",
                        isCorrect: false,
                        orderIndex: 3
                    },
                    {
                        content: "",
                        isCorrect: false,
                        orderIndex: 4
                    }
                ]
            }
        ]);
    };

    const openCreateModal = () => {
        resetForm();
        setShowModal(true);
    };

    const openEditModal = async (readingTaskId) => {
        const task = await getReadingTaskDetailApi(readingTaskId);

        setEditingTaskId(task.readingTaskId);
        setTitle(task.title);
        setInstruction(task.instruction || "");
        setPassageText(task.passageText || "");
        setFormCourseType(task.courseType);
        setIsActive(task.isActive);

        setQuestions(
            task.questions.map((q, qIndex) => ({
                content: q.content,
                orderIndex: q.orderIndex || qIndex + 1,
                point: q.point || 1,
                options: q.options.map((o, oIndex) => ({
                    content: o.content,
                    isCorrect: o.isCorrect,
                    orderIndex: o.orderIndex || oIndex + 1
                }))
            }))
        );

        setShowModal(true);
    };

    const openViewModal = async (readingTaskId) => {
        const task = await getReadingTaskDetailApi(readingTaskId);
        setViewTask(task);
    };

    const addQuestion = () => {
        setQuestions([
            ...questions,
            {
                content: "",
                orderIndex: questions.length + 1,
                point: 1,
                options: [
                    {
                        content: "",
                        isCorrect: true,
                        orderIndex: 1
                    },
                    {
                        content: "",
                        isCorrect: false,
                        orderIndex: 2
                    },
                    {
                        content: "",
                        isCorrect: false,
                        orderIndex: 3
                    },
                    {
                        content: "",
                        isCorrect: false,
                        orderIndex: 4
                    }
                ]
            }
        ]);
    };

    const removeQuestion = (questionIndex) => {
        const nextQuestions = questions
            .filter((_, index) => index !== questionIndex)
            .map((q, index) => ({
                ...q,
                orderIndex: index + 1
            }));

        setQuestions(nextQuestions);
    };

    const updateQuestionField = (
        questionIndex,
        field,
        value
    ) => {
        const nextQuestions = [...questions];

        nextQuestions[questionIndex] = {
            ...nextQuestions[questionIndex],
            [field]: value
        };

        setQuestions(nextQuestions);
    };

    const updateOptionContent = (
        questionIndex,
        optionIndex,
        value
    ) => {
        const nextQuestions = [...questions];

        nextQuestions[questionIndex].options[optionIndex].content =
            value;

        setQuestions(nextQuestions);
    };

    const setCorrectOption = (
        questionIndex,
        optionIndex
    ) => {
        const nextQuestions = [...questions];

        nextQuestions[questionIndex].options =
            nextQuestions[questionIndex].options.map((option, index) => ({
                ...option,
                isCorrect: index === optionIndex
            }));

        setQuestions(nextQuestions);
    };

    const handleSave = async () => {
        if (!title.trim()) {
            alert("Please enter a Reading title.");
            return;
        }

        if (!passageText.trim()) {
            alert("Please enter the passage text.");
            return;
        }

        if (questions.length === 0) {
            alert("Please add at least one question.");
            return;
        }

        for (const question of questions) {
            if (!question.content.trim()) {
                alert("Please enter the question content.");
                return;
            }

            if (question.options.some(option => !option.content.trim())) {
                alert("Please complete all answer options.");
                return;
            }

            if (
                question.options.filter(option => option.isCorrect).length !== 1
            ) {
                alert("Each question must have exactly one correct answer.");
                return;
            }
        }

        const payload = {
            title,
            instruction,
            passageText,
            courseType: Number(formCourseType),
            isActive,
            questions: questions.map((question, questionIndex) => ({
                content: question.content,
                orderIndex: questionIndex + 1,
                point: Number(question.point),
                options: question.options.map((option, optionIndex) => ({
                    content: option.content,
                    isCorrect: option.isCorrect,
                    orderIndex: optionIndex + 1
                }))
            }))
        };

        if (editingTaskId) {
            await updateReadingTaskApi(
                editingTaskId,
                payload
            );
        }
        else {
            await createReadingTaskApi(payload);
        }

        setShowModal(false);
        resetForm();
        await loadTasks();
    };

    const handleDelete = async (readingTaskId) => {
        if (!window.confirm("Delete this Reading Task?")) {
            return;
        }

        await deleteReadingTaskApi(readingTaskId);
        await loadTasks();
    };

    const getCourseTypeText = (value) => {
        if (value === 1) return "TOEIC";
        if (value === 2) return "VSTEP";
        if (value === 3) return "Communication";

        return "Unknown";
    };

    return (
        <div className="lesson-layout">
            <TeacherSidebar />

            <div className="lesson-main">
                <div className="lesson-header">
                    <div className="lesson-title">
                        <h1>Reading Library</h1>
                        <p>
                            Manage reading tasks imported from PDF files or created manually
                        </p>
                    </div>

                    <button
                        className="lesson-btn"
                        onClick={openCreateModal}
                    >
                        <Plus size={18} />
                        Create Reading Task
                    </button>
                </div>

                <div className="question-bank-filters">
                    <select
                        value={courseType}
                        onChange={e => setCourseType(e.target.value)}
                    >
                        <option value="">All courses</option>
                        <option value="1">TOEIC</option>
                        <option value="2">VSTEP</option>
                        <option value="3">Communication</option>
                    </select>
                </div>

                <div className="question-list">
                    {tasks.length === 0 && (
                        <p className="question-explanation">
                            No Reading Tasks yet.
                        </p>
                    )}

                    {tasks.map(task => (
                        <div
                            key={task.readingTaskId}
                            className="question-card"
                        >
                            <div className="question-card-header">
                                <div>
                                    <span className="question-badge">
                                        {getCourseTypeText(task.courseType)}
                                    </span>

                                    <span className="question-type-badge">
                                        {task.totalQuestions} questions
                                    </span>
                                </div>

                                <div className="question-card-actions">
                                    <button
                                        className="lesson-btn secondary"
                                        onClick={() =>
                                            openViewModal(task.readingTaskId)
                                        }
                                    >
                                        <Eye size={16} />
                                    </button>

                                    <button
                                        className="lesson-btn secondary"
                                        onClick={() =>
                                            openEditModal(task.readingTaskId)
                                        }
                                    >
                                        <Pencil size={16} />
                                    </button>

                                    <button
                                        className="lesson-btn danger"
                                        onClick={() =>
                                            handleDelete(task.readingTaskId)
                                        }
                                    >
                                        <Trash2 size={16} />
                                    </button>
                                </div>
                            </div>

                            <h3>{task.title}</h3>

                            <p className="question-explanation">
                                {task.instruction || "No instructions"}
                            </p>
                        </div>
                    ))}
                </div>
            </div>

            {showModal && (
                <div className="lesson-modal-overlay">
                    <div className="lesson-modal question-modal">
                        <h2>
                            {editingTaskId
                                ? "Edit Reading Task"
                                : "Create Reading Task"}
                        </h2>

                        <div className="lesson-form">
                            <input
                                placeholder="Title"
                                value={title}
                                onChange={e => setTitle(e.target.value)}
                            />

                            <textarea
                                placeholder="Instruction"
                                value={instruction}
                                onChange={e =>
                                    setInstruction(e.target.value)
                                }
                            />

                            <select
                                value={formCourseType}
                                onChange={e =>
                                    setFormCourseType(e.target.value)
                                }
                            >
                                <option value="1">TOEIC</option>
                                <option value="2">VSTEP</option>
                                <option value="3">Communication</option>
                            </select>

                            <textarea
                                placeholder="Passage Text"
                                value={passageText}
                                onChange={e =>
                                    setPassageText(e.target.value)
                                }
                                style={{
                                    minHeight: "220px"
                                }}
                            />

                            {editingTaskId && (
                                <select
                                    value={isActive ? "true" : "false"}
                                    onChange={e =>
                                        setIsActive(e.target.value === "true")
                                    }
                                >
                                    <option value="true">Active</option>
                                    <option value="false">Inactive</option>
                                </select>
                            )}

                            <div className="question-form-options">
                                <h3>Questions</h3>

                                {questions.map((question, questionIndex) => (
                                    <div
                                        key={questionIndex}
                                        className="question-card"
                                    >
                                        <textarea
                                            placeholder={`Question ${questionIndex + 1}`}
                                            value={question.content}
                                            onChange={e =>
                                                updateQuestionField(
                                                    questionIndex,
                                                    "content",
                                                    e.target.value
                                                )
                                            }
                                        />

                                        <input
                                            type="number"
                                            min="0.1"
                                            step="0.1"
                                            value={question.point}
                                            onChange={e =>
                                                updateQuestionField(
                                                    questionIndex,
                                                    "point",
                                                    e.target.value
                                                )
                                            }
                                            placeholder="Points"
                                        />

                                        {question.options.map(
                                            (option, optionIndex) => (
                                                <div
                                                    key={optionIndex}
                                                    className="question-form-option"
                                                >
                                                    <input
                                                        type="radio"
                                                        checked={option.isCorrect}
                                                        onChange={() =>
                                                            setCorrectOption(
                                                                questionIndex,
                                                                optionIndex
                                                            )
                                                        }
                                                    />

                                                    <input
                                                        placeholder={`Option ${String.fromCharCode(65 + optionIndex)}`}
                                                        value={option.content}
                                                        onChange={e =>
                                                            updateOptionContent(
                                                                questionIndex,
                                                                optionIndex,
                                                                e.target.value
                                                            )
                                                        }
                                                    />

                                                    <span>
                                                        {String.fromCharCode(
                                                            65 + optionIndex
                                                        )}
                                                    </span>
                                                </div>
                                            )
                                        )}

                                        <button
                                            type="button"
                                            className="lesson-btn danger"
                                            onClick={() =>
                                                removeQuestion(questionIndex)
                                            }
                                        >
                                            Delete Question
                                        </button>
                                    </div>
                                ))}

                                <button
                                    type="button"
                                    className="lesson-btn secondary"
                                    onClick={addQuestion}
                                >
                                    + Add Question
                                </button>
                            </div>

                            <button
                                className="lesson-btn"
                                onClick={handleSave}
                            >
                                Save
                            </button>

                            <button
                                className="lesson-btn secondary"
                                onClick={() => {
                                    setShowModal(false);
                                    resetForm();
                                }}
                            >
                                Close
                            </button>
                        </div>
                    </div>
                </div>
            )}

            {viewTask && (
                <div className="lesson-modal-overlay">
                    <div className="lesson-modal question-modal">
                        <h2>{viewTask.title}</h2>

                        <p className="question-explanation">
                            {viewTask.instruction || "No instructions"}
                        </p>

                        <div
                            className="question-card"
                            style={{
                                whiteSpace: "pre-line",
                                lineHeight: "1.8"
                            }}
                        >
                            {viewTask.passageText}
                        </div>

                        <div className="question-form-options">
                            {viewTask.questions.map((question, index) => (
                                <div
                                    key={question.readingQuestionId}
                                    className="question-card"
                                >
                                    <h3>
                                        Question {index + 1}: {question.content}
                                    </h3>

                                    {question.options.map(option => (
                                        <div
                                            key={option.readingOptionId}
                                            className={
                                                option.isCorrect
                                                    ? "question-option correct"
                                                    : "question-option"
                                            }
                                        >
                                            {option.content}
                                        </div>
                                    ))}
                                </div>
                            ))}
                        </div>

                        <button
                            className="lesson-btn secondary"
                            onClick={() => setViewTask(null)}
                        >
                            Close
                        </button>
                    </div>
                </div>
            )}
        </div>
    );
}

export default TeacherReadingPage;
