import { useEffect, useMemo, useState } from "react";
import {
    ArrowRight,
    Award,
    BookOpen,
    CalendarDays,
    ChevronRight,
    Clock,
    GraduationCap,
    Headphones,
    LibraryBig,
    LoaderCircle,
    Mic2,
    PenLine,
    Play,
    Flame,
    LockKeyhole,
    Medal,
    Sparkles,
    Target,
    Trophy,
    RefreshCw
} from "lucide-react";
import { useNavigate } from "react-router-dom";
import { getStudentProfileApi } from "../api/studentApi";
import "../styles/studentpage.css";

const QUICK_ACTIONS = [
    {
        id: "learning",
        title: "Continue Learning",
        description: "Watch videos, review materials, and continue unfinished lessons.",
        icon: BookOpen,
        path: "/student/learning",
        tag: "Daily"
    },
    {
        id: "exercise",
        title: "Practice",
        description: "Build all four skills through a variety of exercises.",
        icon: Target,
        path: "/student/exercises",
        tag: "Daily"
    },
    {
        id: "test",
        title: "Tests & Assessments",
        description: "Challenge yourself with realistic tests and receive feedback.",
        icon: LibraryBig,
        path: "/student/tests",
        tag: "Assessment"
    },
    {
        id: "competition",
        title: "Arena & Competitions",
        description: "Compete, earn points, and climb the rankings.",
        icon: Trophy,
        path: "/student/competitions",
        tag: "Rankings"
    }
];

const SKILL_ITEMS = [
    {
        id: "listening",
        name: "Listening",
        description: "Listening comprehension and audio response",
        icon: Headphones,
        path: "/student/exercises?skill=listening",
        badge: "New Exercises"
    },
    {
        id: "speaking",
        name: "Speaking",
        description: "Pronunciation and natural communication",
        icon: Mic2,
        path: "/student/exercises?skill=speaking",
        badge: "AI Grading"
    },
    {
        id: "reading",
        name: "Reading",
        description: "Reading comprehension and vocabulary growth",
        icon: BookOpen,
        path: "/student/exercises?skill=reading",
        badge: "Trending Topic"
    },
    {
        id: "writing",
        name: "Writing",
        description: "Essay writing and sentence structure",
        icon: PenLine,
        path: "/student/exercises?skill=writing",
        badge: "1:1 Feedback"
    }
];

const MOTIVATIONAL_QUOTES = [
    "Every lesson today takes you one step closer to your goal.",
    "Fifteen consistent minutes a day is better than five hours once a week.",
    "You do not need to be great to start, but you need to start to become great.",
    "Success is the sum of small efforts repeated every day.",
    "A journey of a thousand miles begins with a single step. Keep going!"
];

const ACHIEVEMENTS = [
    { type: 1, label: "20-day streak", icon: Flame },
    { type: 2, label: "30 exercises", icon: Award },
    { type: 3, label: "Perfect test", icon: Medal },
    { type: 4, label: "Top 3 competition", icon: Trophy },
    { type: 5, label: "Five strong tests", icon: Target }
];

const CHAMPION_EXP = 13450;

function getGreeting() {
    const hour = new Date().getHours();
    if (hour < 12) return "Good morning";
    if (hour < 18) return "Good afternoon";
    return "Good evening";
}

function StudentPage() {
    const navigate = useNavigate();
    const [profile, setProfile] = useState(null);
    const [loading, setLoading] = useState(true);

    // Khởi tạo randomQuote an toàn trong state (Lazy Initial State)
    const [randomQuote] = useState(() => {
        const index = Math.floor(Math.random() * MOTIVATIONAL_QUOTES.length);
        return MOTIVATIONAL_QUOTES[index];
    });

    const loadProfile = async () => {
        setLoading(true);
        try {
            const response = await getStudentProfileApi();
            setProfile(response?.data || response);
        } catch (error) {
            console.error("Unable to load student information:", error);
        } finally {
            setLoading(false);
        }
    };

    useEffect(() => {
        loadProfile();
    }, []);

    const displayName = useMemo(() => {
        return profile?.fullName || profile?.userName || "Student";
    }, [profile]);

    const firstName = useMemo(() => {
        const parts = displayName.trim().split(/\s+/);
        return parts.at(-1) || displayName;
    }, [displayName]);

    const currentDateFormatted = useMemo(() => {
        return new Date().toLocaleDateString("en-US", {
            weekday: "long",
            day: "numeric",
            month: "long"
        });
    }, []);

    const totalExp = profile?.totalExp || 0;
    const championProgress = Math.min((totalExp / CHAMPION_EXP) * 100, 100);
    const unlockedAchievements = profile?.unlockedAchievementTypes || [];

    if (loading) {
        return (
            <div className="student-home-loading-screen">
                <div className="student-home-loader-card">
                    <LoaderCircle size={40} className="student-home-spinner" />
                    <h3>Preparing your learning space...</h3>
                    <p>Please wait a moment.</p>
                </div>
            </div>
        );
    }

    if (!profile) {
        return (
            <div className="student-home-error-screen">
                <div className="student-home-error-card">
                    <div className="student-home-error-icon">
                        <GraduationCap size={44} />
                    </div>
                    <h2>Unable to load learning data</h2>
                    <p>Your session may have expired, or the connection was interrupted.</p>
                    <button type="button" className="student-home-retry-btn" onClick={loadProfile}>
                        <RefreshCw size={16} /> Try Again
                    </button>
                </div>
            </div>
        );
    }

    return (
        <div className="student-home-wrapper">
            <main className="student-home-container">

                {/* HERO BANNER */}
                <section className="student-hero-banner">
                    <div className="student-hero-bg-glow glow-1" />
                    <div className="student-hero-bg-glow glow-2" />

                    <div className="student-hero-main">
                        <div className="student-hero-content">
                            <div className="student-hero-top-bar">
                                <div className="student-greeting-pill">
                                    <Sparkles size={15} className="sparkle-icon" />
                                    <span>{getGreeting()}, {firstName}!</span>
                                </div>
                                <div className="student-date-pill">
                                    <CalendarDays size={14} />
                                    <span>{currentDateFormatted}</span>
                                </div>
                            </div>

                            <h1 className="student-hero-title">
                                Ready to break through <br />
                                <span className="text-gradient">English goals Today?</span>
                            </h1>

                            <p className="student-hero-subtitle">
                                Consistency is key. Just 15–20 minutes of practice each day can help you master English naturally.
                            </p>
                        </div>

                        <div className="student-hero-footer">
                            <div className="student-current-course-badge">
                            <div className="course-badge-icon">
                                <GraduationCap size={20} />
                            </div>

                            <div className="course-badge-text">
                                <span className="label">
                                    Current Class
                                </span>

                                <strong className="value">
                                    {profile.hasClass
                                        ? profile.courseType ||
                                        profile.courseName ||
                                        "Advanced English Course"
                                        : "No class assigned"}
                                </strong>
                            </div>
                            </div>

                            <button
                                type="button"
                                className="student-primary-action-btn"
                                onClick={() => navigate("/student/learning")}
                            >
                            <Play size={18} fill="currentColor" />

                            <span>Continue Latest Lesson</span>

                            <ArrowRight
                                size={18}
                                className="arrow-icon"
                            />
                            </button>
                        </div>
                    </div>

                    <div className="student-hero-gamification">
                        <section className="student-exp-card">
                            <div className="student-exp-avatar" aria-hidden="true">
                                {profile.avatarUrl ? (
                                    <img src={profile.avatarUrl} alt="" />
                                ) : (
                                    <span>{displayName.charAt(0).toUpperCase()}</span>
                                )}
                            </div>
                            <div className="student-exp-details">
                                <div className="student-exp-heading">
                                    <span>Learning EXP</span>
                                    <span className="student-streak"><Flame size={14} /> {profile.currentStreakDays || 0} day streak</span>
                                </div>
                                <strong>{totalExp.toLocaleString()} EXP</strong>
                                <div className="student-exp-progress-track" aria-label={`${Math.round(championProgress)}% to Champion`}>
                                    <div className="student-exp-progress-fill" style={{ width: `${championProgress}%` }} />
                                </div>
                                <small>{totalExp.toLocaleString()} / {CHAMPION_EXP.toLocaleString()} EXP to Champion</small>
                            </div>
                        </section>

                        <section className="student-achievements-card" aria-label="Your achievements">
                            <div className="student-achievements-heading">
                                <span>Your achievements</span>
                                <strong>{unlockedAchievements.length}/{ACHIEVEMENTS.length}</strong>
                            </div>
                            <div className="student-achievement-list">
                                {ACHIEVEMENTS.map(({ type, label, icon: Icon }) => {
                                    const unlocked = unlockedAchievements.includes(type);
                                    return (
                                        <div key={type} className={`student-achievement-badge ${unlocked ? "is-unlocked" : ""}`} title={label}>
                                            {unlocked ? <Icon size={21} /> : <LockKeyhole size={18} />}
                                        </div>
                                    );
                                })}
                            </div>
                        </section>

                        <button
                            type="button"
                            className="student-class-rank-card"
                            onClick={() => navigate("/student/class-leaderboard")}
                            disabled={!profile.hasClass}
                            aria-label="View class EXP leaderboard"
                        >
                            <div className="student-class-rank-icon">
                                <Trophy size={22} />
                            </div>
                            <div className="student-class-rank-copy">
                                <span>Rank in class</span>
                                <strong>{profile.classRank ? `#${profile.classRank}` : "—"}</strong>
                                <small>{profile.hasClass ? `of ${profile.classStudentCount} students` : "No class assigned"}</small>
                            </div>
                            <ChevronRight size={18} className="student-class-rank-arrow" />
                        </button>
                    </div>
                </section>

                {/* QUICK ACCESS GRID */}
                <section className="student-section">
                    <div className="student-section-header">
                        <div>
                            <span className="student-eyebrow">Learning Shortcuts</span>
                            <h2 className="student-section-title">What would you like to do today?</h2>
                        </div>
                    </div>

                    <div className="student-quick-grid">
                        {QUICK_ACTIONS.map((action) => {
                            const Icon = action.icon;
                            return (
                                <div
                                    key={action.id}
                                    className="student-quick-card"
                                    onClick={() => navigate(action.path)}
                                    role="button"
                                    tabIndex={0}
                                >
                                    <div className="card-top">
                                        <div className="card-icon-wrapper">
                                            <Icon size={22} />
                                        </div>
                                        <span className="card-tag">{action.tag}</span>
                                    </div>

                                    <div className="card-body">
                                        <h3>{action.title}</h3>
                                        <p>{action.description}</p>
                                    </div>

                                    <div className="card-footer">
                                        <span>Get Started</span>
                                        <ChevronRight size={16} />
                                    </div>
                                </div>
                            );
                        })}
                    </div>
                </section>

                {/* BENTO GRID (MAIN CONTENT & SIDEBAR) */}
                <div className="student-bento-grid">

                    {/* SKILLS SECTION */}
                    <section className="student-bento-panel student-skills-panel">
                        <div className="panel-header">
                            <div>
                                <span className="student-eyebrow">Skill Practice</span>
                                <h2 className="student-section-title">Focused Practice</h2>
                            </div>
                            <button
                                type="button"
                                className="text-link-btn"
                                onClick={() => navigate("/student/exercises")}
                            >
                                All Skills <ArrowRight size={15} />
                            </button>
                        </div>

                        <div className="student-skills-grid">
                            {SKILL_ITEMS.map((skill) => {
                                const Icon = skill.icon;
                                return (
                                    <div
                                        key={skill.id}
                                        className="student-skill-card"
                                        onClick={() => navigate(skill.path)}
                                        role="button"
                                        tabIndex={0}
                                    >
                                        <div className="skill-icon-box">
                                            <Icon size={20} />
                                        </div>
                                        <div className="skill-info">
                                            <div className="skill-title-row">
                                                <h4>{skill.name}</h4>
                                                <span className="skill-badge">{skill.badge}</span>
                                            </div>
                                            <p>{skill.description}</p>
                                        </div>
                                        <div className="skill-arrow">
                                            <ChevronRight size={18} />
                                        </div>
                                    </div>
                                );
                            })}
                        </div>
                    </section>

                    {/* SIDEBAR PANEL */}
                    <aside className="student-bento-sidebar">

                        {/* SCHEDULE CARD */}
                        <div className="student-bento-panel student-schedule-card">
                            <div className="panel-header-compact">
                                <span className="student-eyebrow" style={{ display: 'block', marginBottom: '4px' }}>
                                    PLAN
                                </span>
                                <div className="header-title" style={{ justifyContent: 'space-between', width: '100%' }}>
                                    <h3>Today's Schedule</h3>
                                    <CalendarDays size={20} className="header-icon" />
                                </div>
                            </div>

                            {profile.hasClass ? (
                                <div className="schedule-box">
                                    <div className="schedule-time-pill">
                                        <Clock size={14} />
                                        <span>Based on your class schedule</span>
                                    </div>

                                    <h4 className="schedule-class-name" style={{ marginTop: '12px' }}>
                                        {profile.className || "Class A"}
                                    </h4>

                                    <p className="schedule-desc">
                                        Keep a steady learning routine to reach your goals faster.
                                    </p>

                                    <button
                                        type="button"
                                        className="student-secondary-btn"
                                        style={{
                                            display: 'flex',
                                            alignItems: 'center',
                                            justifyContent: 'center',
                                            gap: '8px',
                                            background: 'var(--color-primary, #1565C0)',
                                            color: '#ffffff',
                                            border: 'none',
                                            borderRadius: '12px',
                                            padding: '12px',
                                            fontSize: '14px',
                                            fontWeight: '700'
                                        }}
                                        onClick={() => navigate("/student/learning")}
                                    >
                                        <span>Go to Class</span>
                                        <ArrowRight size={16} />
                                    </button>
                                </div>
                            ) : (
                                <div className="schedule-empty">
                                    <CalendarDays size={32} />
                                    <h4>No new schedule yet</h4>
                                    <p>You have not enrolled in or been assigned to a class yet.</p>
                                </div>
                            )}
                        </div>

                        {/* DAILY GOAL CARD */}
                        <div className="student-bento-panel student-goal-card">
                            <div className="goal-card-header" style={{ marginBottom: 0 }}>
                                <div className="goal-icon-box">
                                    <Sparkles size={20} />
                                </div>
                                <div>
                                    <span className="goal-eyebrow">MOTIVATION CORNER</span>
                                    <h3 style={{ fontSize: '14px', fontWeight: '500', lineHeight: '1.5', marginTop: '4px', fontStyle: 'italic', opacity: 0.9 }}>
                                        “{randomQuote}”
                                    </h3>
                                </div>
                            </div>
                        </div>

                    </aside>
                </div>

            </main>
        </div>
    );
}

export default StudentPage;
