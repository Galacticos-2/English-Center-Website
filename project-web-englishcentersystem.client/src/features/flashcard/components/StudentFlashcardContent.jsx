import { useEffect, useMemo, useRef, useState } from "react";
import {
    ArrowLeft,
    BookOpen,
    CheckCircle2,
    ChevronLeft,
    ChevronRight,
    CircleHelp,
    Play,
    RotateCcw,
    Search,
    Target,
    Volume2
} from "lucide-react";
import { useNavigate } from "react-router-dom";
import {
    completeFlashcardStudyApi,
    getFlashcardStudyProgressApi,
    getFlashcardStudySessionApi,
    getStudentFlashcardsApi,
    resetFlashcardStudyProgressApi
} from "../api/flashcardApi";
import "../styles/flashcard.css";

const PAGE_SIZE = 50;
const shuffle = items => [...items].sort(() => Math.random() - 0.5);
const normalizeMeaning = meaning => meaning.trim().toLocaleLowerCase();
const EMPTY_PROGRESS = { totalCount: 0, completedCount: 0 };

function StudentFlashcardContent() {
    const navigate = useNavigate();
    const quizTimerRef = useRef(null);
    const quizSessionRef = useRef(0);
    const [flashcards, setFlashcards] = useState([]);
    const [practiceCards, setPracticeCards] = useState([]);
    const [quizCards, setQuizCards] = useState([]);
    const [studyProgress, setStudyProgress] = useState({
        practice: EMPTY_PROGRESS,
        quiz: EMPTY_PROGRESS
    });
    const [view, setView] = useState("list");
    const [index, setIndex] = useState(0);
    const [quizIndex, setQuizIndex] = useState(0);
    const [quizOptions, setQuizOptions] = useState([]);
    const [quizSelection, setQuizSelection] = useState("");
    const [quizState, setQuizState] = useState("");
    const [practiceSessionComplete, setPracticeSessionComplete] = useState(false);
    const [quizSessionComplete, setQuizSessionComplete] = useState(false);
    const [isFlipped, setIsFlipped] = useState(false);
    const [searchTerm, setSearchTerm] = useState("");
    const [page, setPage] = useState(1);
    const [loading, setLoading] = useState(true);
    const [sessionLoading, setSessionLoading] = useState(false);
    const [error, setError] = useState("");

    useEffect(() => {
        const load = async () => {
            setLoading(true);
            setError("");

            try {
                const [cards, progress] = await Promise.all([
                    getStudentFlashcardsApi(),
                    getFlashcardStudyProgressApi()
                ]);

                setFlashcards(cards || []);
                setStudyProgress(progress || {
                    practice: EMPTY_PROGRESS,
                    quiz: EMPTY_PROGRESS
                });
            } catch (requestError) {
                setError(requestError.response?.data?.message || "Unable to load flashcards.");
            } finally {
                setLoading(false);
            }
        };

        load();
        return () => {
            quizSessionRef.current += 1;
            window.speechSynthesis?.cancel();
            window.clearTimeout(quizTimerRef.current);
        };
    }, []);

    const filteredFlashcards = useMemo(() => {
        const normalizedSearch = searchTerm.trim().toLocaleLowerCase();
        if (!normalizedSearch) return flashcards;

        return flashcards.filter(card => [card.word, card.meaning, card.exampleSentence]
            .some(value => value?.toLocaleLowerCase().includes(normalizedSearch)));
    }, [flashcards, searchTerm]);

    const totalPages = Math.max(1, Math.ceil(filteredFlashcards.length / PAGE_SIZE));
    const currentPage = Math.min(page, totalPages);
    const listStart = (currentPage - 1) * PAGE_SIZE;
    const visibleFlashcards = filteredFlashcards.slice(listStart, listStart + PAGE_SIZE);
    const currentCard = practiceCards[index];
    const quizCard = quizCards[quizIndex];
    const practiceProgress = studyProgress.practice || EMPTY_PROGRESS;
    const quizProgress = studyProgress.quiz || EMPTY_PROGRESS;

    const formatProgress = progress =>
        `${progress.completedCount.toLocaleString()} / ${progress.totalCount.toLocaleString()}`;

    const progressPercentage = progress =>
        progress.totalCount === 0 ? 0 : (progress.completedCount / progress.totalCount) * 100;

    const updateModeProgress = (mode, progress) => {
        setStudyProgress(current => ({ ...current, [mode]: progress }));
    };

    const speak = (text, onComplete) => {
        if (!("speechSynthesis" in window) || !text) {
            onComplete?.();
            return;
        }

        window.speechSynthesis.cancel();
        const utterance = new SpeechSynthesisUtterance(text);
        utterance.lang = "en-US";
        utterance.rate = 0.85;
        let completed = false;
        const complete = () => {
            if (completed) return;

            completed = true;
            onComplete?.();
        };

        utterance.onend = complete;
        utterance.onerror = complete;
        window.speechSynthesis.speak(utterance);
    };

    const playQuizFeedbackSound = isCorrect => {
        const AudioContext = window.AudioContext || window.webkitAudioContext;
        if (!AudioContext) return;

        const audioContext = new AudioContext();
        const notes = isCorrect ? [523.25, 659.25, 783.99] : [311.13, 233.08];
        const now = audioContext.currentTime;

        notes.forEach((frequency, noteIndex) => {
            const oscillator = audioContext.createOscillator();
            const gain = audioContext.createGain();
            const startTime = now + noteIndex * (isCorrect ? 0.1 : 0.14);

            oscillator.type = isCorrect ? "triangle" : "sawtooth";
            oscillator.frequency.setValueAtTime(frequency, startTime);
            gain.gain.setValueAtTime(0.0001, startTime);
            gain.gain.exponentialRampToValueAtTime(isCorrect ? 0.22 : 0.16, startTime + 0.02);
            gain.gain.exponentialRampToValueAtTime(0.0001, startTime + (isCorrect ? 0.18 : 0.2));

            oscillator.connect(gain);
            gain.connect(audioContext.destination);
            oscillator.start(startTime);
            oscillator.stop(startTime + (isCorrect ? 0.19 : 0.21));
        });

        window.setTimeout(() => audioContext.close(), isCorrect ? 520 : 400);
    };

    const createQuizOptions = card => {
        const meanings = new Map();
        flashcards.forEach(flashcard => {
            if (flashcard.meaning) meanings.set(normalizeMeaning(flashcard.meaning), flashcard.meaning);
        });

        const correctMeaning = meanings.get(normalizeMeaning(card.meaning)) || card.meaning;
        const distractors = shuffle([...meanings.entries()]
            .filter(([meaning]) => meaning !== normalizeMeaning(card.meaning))
            .map(([, meaning]) => meaning))
            .slice(0, 3);

        return shuffle([correctMeaning, ...distractors]);
    };

    const startPractice = async () => {
        window.speechSynthesis?.cancel();
        setSessionLoading(true);
        setError("");

        try {
            const session = await getFlashcardStudySessionApi("practice");
            updateModeProgress("practice", session.progress);
            setPracticeCards(shuffle(session.remainingFlashcards));
            setPracticeSessionComplete(
                session.remainingFlashcards.length === 0 && session.progress.totalCount > 0
            );
            setIndex(0);
            setIsFlipped(false);
            setView("practice");
        } catch (requestError) {
            setError(requestError.response?.data?.message || "Unable to start practice.");
        } finally {
            setSessionLoading(false);
        }
    };

    const startQuiz = async () => {
        const uniqueMeanings = new Set(flashcards.map(card => normalizeMeaning(card.meaning)));
        if (uniqueMeanings.size < 4) {
            setError("Vocabulary Quiz requires at least four different meanings.");
            return;
        }

        quizSessionRef.current += 1;
        window.clearTimeout(quizTimerRef.current);
        setSessionLoading(true);
        setError("");

        try {
            const session = await getFlashcardStudySessionApi("quiz");
            const cards = shuffle(session.remainingFlashcards);

            updateModeProgress("quiz", session.progress);
            setQuizCards(cards);
            setQuizIndex(0);
            setQuizOptions(cards.length > 0 ? createQuizOptions(cards[0]) : []);
            setQuizSelection("");
            setQuizState("");
            setQuizSessionComplete(cards.length === 0 && session.progress.totalCount > 0);
            setView("quiz");
        } catch (requestError) {
            setError(requestError.response?.data?.message || "Unable to start Vocabulary Quiz.");
        } finally {
            setSessionLoading(false);
        }
    };

    const returnToList = () => {
        quizSessionRef.current += 1;
        window.speechSynthesis?.cancel();
        window.clearTimeout(quizTimerRef.current);
        setView("list");
        setIsFlipped(false);
        setQuizState("");
    };

    const moveTo = nextIndex => {
        window.speechSynthesis?.cancel();
        setIndex(nextIndex);
        setIsFlipped(false);
    };

    const advancePractice = async () => {
        if (!currentCard) return;

        try {
            const progress = await completeFlashcardStudyApi("practice", currentCard.vocabularyFlashcardId);
            updateModeProgress("practice", progress);

            if (index + 1 < practiceCards.length) {
                moveTo(index + 1);
                return;
            }

            if (progress.completedCount >= progress.totalCount) {
                setPracticeSessionComplete(true);
                return;
            }

            const session = await getFlashcardStudySessionApi("practice");
            const cards = shuffle(session.remainingFlashcards);
            updateModeProgress("practice", session.progress);
            setPracticeCards(cards);
            setPracticeSessionComplete(cards.length === 0 && session.progress.totalCount > 0);
            setIndex(0);
            setIsFlipped(false);
        } catch (requestError) {
            setError(requestError.response?.data?.message || "Unable to save practice progress.");
        }
    };

    const restartPractice = async () => {
        try {
            const progress = await resetFlashcardStudyProgressApi("practice");
            updateModeProgress("practice", progress);
            setPracticeSessionComplete(false);
            await startPractice();
        } catch (requestError) {
            setError(requestError.response?.data?.message || "Unable to restart practice.");
        }
    };

    const moveToNextQuizQuestion = async progress => {
        const nextIndex = quizIndex + 1;
        if (nextIndex < quizCards.length) {
            setQuizIndex(nextIndex);
            setQuizOptions(createQuizOptions(quizCards[nextIndex]));
            setQuizSelection("");
            setQuizState("");
            return;
        }

        if (progress.completedCount >= progress.totalCount) {
            setQuizSessionComplete(true);
            return;
        }

        const session = await getFlashcardStudySessionApi("quiz");
        const cards = shuffle(session.remainingFlashcards);
        updateModeProgress("quiz", session.progress);
        setQuizCards(cards);
        setQuizIndex(0);
        setQuizOptions(cards.length > 0 ? createQuizOptions(cards[0]) : []);
        setQuizSelection("");
        setQuizState("");
        setQuizSessionComplete(cards.length === 0 && session.progress.totalCount > 0);
    };

    const selectQuizMeaning = async meaning => {
        if (quizState === "correct") return;

        setQuizSelection(meaning);
        if (normalizeMeaning(meaning) !== normalizeMeaning(quizCard.meaning)) {
            setQuizState("incorrect");
            playQuizFeedbackSound(false);
            return;
        }

        setQuizState("correct");
        playQuizFeedbackSound(true);
        const quizSession = quizSessionRef.current;

        try {
            const [progress] = await Promise.all([
                completeFlashcardStudyApi("quiz", quizCard.vocabularyFlashcardId),
                new Promise(resolve => speak(quizCard.word, resolve))
            ]);

            if (quizSession !== quizSessionRef.current) return;

            updateModeProgress("quiz", progress);
            quizTimerRef.current = window.setTimeout(() => {
                if (quizSession === quizSessionRef.current) {
                    moveToNextQuizQuestion(progress).catch(requestError => {
                        setError(requestError.response?.data?.message || "Unable to load the next quiz question.");
                    });
                }
            }, 1500);
        } catch (requestError) {
            if (quizSession !== quizSessionRef.current) return;

            setQuizSelection("");
            setQuizState("");
            setError(requestError.response?.data?.message || "Unable to save quiz progress.");
        }
    };

    const restartQuiz = async () => {
        try {
            const progress = await resetFlashcardStudyProgressApi("quiz");
            updateModeProgress("quiz", progress);
            setQuizSessionComplete(false);
            await startQuiz();
        } catch (requestError) {
            setError(requestError.response?.data?.message || "Unable to restart Vocabulary Quiz.");
        }
    };

    const handleSearch = value => {
        setSearchTerm(value);
        setPage(1);
    };

    const pageOptions = Array.from({ length: totalPages }, (_, pageIndex) => {
        const start = pageIndex * PAGE_SIZE + 1;
        const end = Math.min((pageIndex + 1) * PAGE_SIZE, filteredFlashcards.length);
        return { value: pageIndex + 1, label: `${start} - ${end}` };
    });

    return (
        <section className={`student-flashcard-page student-flashcard-hub${view === "list" ? " student-flashcard-list-page" : ""}`}>
            {error && <div className="flashcard-error">{error}</div>}

            {loading ? (
                <div className="student-flashcard-empty">Loading flashcards...</div>
            ) : flashcards.length === 0 ? (
                <div className="student-flashcard-empty">
                    <BookOpen size={40} />
                    <h2>No flashcards available</h2>
                    <p>Your teacher has not added vocabulary for your class yet.</p>
                    <button type="button" onClick={() => navigate("/student")}>Return to dashboard</button>
                </div>
            ) : view === "list" ? (
                <section className="flashcard-word-list">
                    <header className="flashcard-list-overview">
                        <div className="flashcard-list-header">
                        <div>
                            <span>VOCABULARY PRACTICE</span>
                            <h1>Flashcards – Vocabulary List</h1>
                            <p>Manage and review your vocabulary collection.</p>
                        </div>
                        <div className="flashcard-list-header-actions">
                            <button type="button" className="flashcard-quiz-button" onClick={startQuiz} disabled={sessionLoading}>
                                <CircleHelp size={17} />
                                <span>Vocabulary Quiz</span>
                                <small>{formatProgress(quizProgress)}</small>
                            </button>
                            <button type="button" className="flashcard-practice-button" onClick={startPractice} disabled={sessionLoading}>
                                <Play size={16} fill="currentColor" />
                                <span>Start Practice</span>
                                <small>{formatProgress(practiceProgress)}</small>
                            </button>
                            <label className="flashcard-page-range">
                                <select value={currentPage} onChange={event => setPage(Number(event.target.value))}>
                                    {pageOptions.map(option => <option key={option.value} value={option.value}>{option.label}</option>)}
                                </select>
                            </label>
                        </div>
                        </div>
                        <div className="flashcard-summary-grid">
                            <div className="flashcard-summary-card">
                                <div className="flashcard-summary-icon is-words"><BookOpen size={20} /></div>
                                <div><strong>{flashcards.length.toLocaleString()}</strong><small>Total words</small></div>
                            </div>
                            <div className="flashcard-summary-card">
                                <div className="flashcard-summary-icon is-practiced"><CheckCircle2 size={20} /></div>
                                <div><strong>{practiceProgress.completedCount.toLocaleString()}</strong><small>Words practiced</small></div>
                            </div>
                            <div className="flashcard-summary-card is-progress">
                                <div className="flashcard-summary-icon is-progress"><Target size={20} /></div>
                                <div><strong>{Math.round(progressPercentage(practiceProgress) * 10) / 10}%</strong><small>Course progress</small><span><i style={{ width: `${progressPercentage(practiceProgress)}%` }} /></span></div>
                            </div>
                        </div>
                    </header>

                    <div className="flashcard-list-tools">
                        <label className="flashcard-search">
                            <Search size={16} />
                            <input value={searchTerm} onChange={event => handleSearch(event.target.value)} placeholder="Search a word..." aria-label="Search flashcards" />
                        </label>
                        <span>Showing <strong>{visibleFlashcards.length === 0 ? 0 : listStart + 1}–{Math.min(listStart + visibleFlashcards.length, filteredFlashcards.length)}</strong> of <strong>{filteredFlashcards.length.toLocaleString()}</strong> words</span>
                    </div>

                    {visibleFlashcards.length === 0 ? (
                        <div className="student-flashcard-empty">
                            <BookOpen size={34} />
                            <h2>No matching words</h2>
                            <p>Try a different search term.</p>
                        </div>
                    ) : (
                        <>
                            <div className="flashcard-student-table-wrap">
                                <table className="flashcard-student-table">
                                    <thead><tr><th>No.</th><th>Word</th><th>Meaning</th><th>Sentence</th></tr></thead>
                                    <tbody>
                                        {visibleFlashcards.map((card, cardIndex) => (
                                            <tr key={card.vocabularyFlashcardId}>
                                                <td>{listStart + cardIndex + 1}</td>
                                                <td>
                                                    <strong>{card.word}</strong>
                                                    <button type="button" onClick={() => speak(card.word)} aria-label={`Read ${card.word}`}><Volume2 size={15} /></button>
                                                </td>
                                                <td>{card.meaning}</td>
                                                <td>{card.exampleSentence}</td>
                                            </tr>
                                        ))}
                                    </tbody>
                                </table>
                            </div>
                            <nav className="flashcard-pagination" aria-label="Flashcard pages">
                                <button type="button" disabled={currentPage === 1} onClick={() => setPage(currentPage - 1)} aria-label="Previous page"><ChevronLeft size={18} /></button>
                                {pageOptions.map(option => <button key={option.value} type="button" className={option.value === currentPage ? "is-active" : ""} onClick={() => setPage(option.value)}>{option.value}</button>)}
                                <button type="button" disabled={currentPage === totalPages} onClick={() => setPage(currentPage + 1)} aria-label="Next page"><ChevronRight size={18} /></button>
                            </nav>
                        </>
                    )}
                </section>
            ) : view === "quiz" ? (
                quizSessionComplete ? (
                    <div className="flashcard-complete">
                        <div className="flashcard-complete-icon">🎉</div>
                        <span>QUIZ COMPLETE!</span>
                        <h2>You completed every vocabulary question.</h2>
                        <p>Great work. Start a new review cycle whenever you want to practice every word again.</p>
                        <div>
                            <button type="button" className="flashcard-primary" onClick={restartQuiz}><RotateCcw size={18} /> Review from the beginning</button>
                            <button type="button" className="flashcard-secondary" onClick={returnToList}>Back to list</button>
                        </div>
                    </div>
                ) : (
                    <section className="flashcard-quiz">
                        <header className="flashcard-practice-header">
                            <button type="button" className="flashcard-back-link" onClick={returnToList}><ArrowLeft size={16} /> Back to list</button>
                            <h1>Vocabulary Quiz</h1>
                            <span>{formatProgress(quizProgress)}</span>
                        </header>
                        <div className="flashcard-practice-progress"><span style={{ width: `${progressPercentage(quizProgress)}%` }} /></div>
                        <article className="flashcard-quiz-card">
                            <span>CHOOSE THE MEANING</span>
                            <div className="flashcard-practice-word">
                                <button type="button" onClick={() => speak(quizCard.word)} aria-label="Read word"><Volume2 size={21} /></button>
                                <h2>{quizCard.word}</h2>
                            </div>
                            <div className="flashcard-quiz-options">
                                {quizOptions.map(meaning => (
                                    <button key={meaning} type="button" className={quizSelection === meaning ? `is-${quizState}` : ""} onClick={() => selectQuizMeaning(meaning)} disabled={quizState === "correct"}>{meaning}</button>
                                ))}
                            </div>
                            {quizState === "incorrect" && <p className="flashcard-quiz-feedback is-incorrect">Not quite. Choose another meaning.</p>}
                            {quizState === "correct" && <p className="flashcard-quiz-feedback is-correct">Correct! Loading the next word…</p>}
                        </article>
                    </section>
                )
            ) : practiceSessionComplete ? (
                <div className="flashcard-complete">
                    <div className="flashcard-complete-icon">🎉</div>
                    <span>WELL DONE!</span>
                    <h2>You completed every flashcard.</h2>
                    <p>Great work. Start a new review cycle whenever you want to practice every word again.</p>
                    <div>
                        <button type="button" className="flashcard-primary" onClick={restartPractice}><RotateCcw size={18} /> Review from the beginning</button>
                        <button type="button" className="flashcard-secondary" onClick={returnToList}>Back to list</button>
                    </div>
                </div>
            ) : (
                <section className="flashcard-practice">
                    <header className="flashcard-practice-header">
                        <button type="button" className="flashcard-back-link" onClick={returnToList}><ArrowLeft size={16} /> Back to list</button>
                        <h1>Flashcards – Practice</h1>
                        <span>{formatProgress(practiceProgress)}</span>
                    </header>
                    <div className="flashcard-practice-progress"><span style={{ width: `${progressPercentage(practiceProgress)}%` }} /></div>
                    <div className="flashcard-practice-stage">
                        <button type="button" className="flashcard-round-nav" disabled={index === 0} onClick={() => moveTo(index - 1)} aria-label="Previous flashcard"><ChevronLeft size={22} /></button>
                        <article className="flashcard-practice-card">
                            <span>WORD</span>
                            <div className="flashcard-practice-word">
                                <button type="button" onClick={() => speak(currentCard.word)} aria-label="Read word"><Volume2 size={21} /></button>
                                <h2>{currentCard.word}</h2>
                            </div>
                            <div className={`flashcard-practice-details${isFlipped ? " is-revealed" : ""}`}>
                                <div><strong>Meaning</strong><p>{isFlipped ? currentCard.meaning : "Flip the card to reveal the meaning."}</p></div>
                                <div>
                                    <strong>Sentence</strong>
                                    <button type="button" onClick={() => speak(currentCard.exampleSentence)} aria-label="Read example sentence"><Volume2 size={16} /></button>
                                    <p>{currentCard.exampleSentence}</p>
                                </div>
                            </div>
                        </article>
                        <button type="button" className="flashcard-round-nav" onClick={advancePractice} aria-label="Next flashcard"><ChevronRight size={22} /></button>
                    </div>
                    <div className="flashcard-practice-controls">
                        <button type="button" className="flashcard-secondary" onClick={() => setIsFlipped(current => !current)}><RotateCcw size={17} /> {isFlipped ? "Hide meaning" : "Flip"}</button>
                        <button type="button" className="flashcard-primary" onClick={advancePractice}>Next word<ChevronRight size={17} /></button>
                    </div>
                </section>
            )}
        </section>
    );
}

export default StudentFlashcardContent;
