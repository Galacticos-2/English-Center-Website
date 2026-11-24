import { Link } from "react-router-dom";
import {
    BookOpen,
    Mail,
    MapPin,
    Phone,
    Star,
    Users,
    BarChart3,
    Zap,
    MessageSquare,
    Shield,
    Globe,
    GraduationCap,
    CheckCircle,
    ArrowRight,
    Play,
    Target,
    TrendingUp
} from "lucide-react";
import { useEffect, useRef, useState } from "react";
import "../styles/homePage.css";

function HomePage() {
    const [scrolled, setScrolled] = useState(false);

    useEffect(() => {
        const onScroll = () => {
            setScrolled(window.scrollY > 40);
        };

        window.addEventListener("scroll", onScroll);
        return () => window.removeEventListener("scroll", onScroll);
    }, []);

    return (
        <div className="home-page">
            <ParticleBackground />
            <TopBar hidden={scrolled} />
            <Header scrolled={scrolled} />
            <Hero />
            <Stats />
            <Features />
            <Courses />
            <Steps />
            <CTA />
            <Footer />
        </div>
    );
}

function ParticleBackground() {
    const canvasRef = useRef(null);

    useEffect(() => {
        const canvas = canvasRef.current;
        const ctx = canvas.getContext("2d");

        let width = 0;
        let height = 0;
        let particles = [];
        let animationFrameId;

        const createParticles = () => {
            const particleCount = Math.min(
                95,
                Math.max(45, Math.floor((width * height) / 18000))
            );

            particles = Array.from({ length: particleCount }, () => ({
                x: Math.random() * width,
                y: Math.random() * height,
                vx: (Math.random() - 0.5) * 0.35,
                vy: (Math.random() - 0.5) * 0.35,
                radius: Math.random() * 1.6 + 1.1
            }));
        };

        const resizeCanvas = () => {
            width = window.innerWidth;
            height = window.innerHeight;

            const dpr = window.devicePixelRatio || 1;

            canvas.width = width * dpr;
            canvas.height = height * dpr;

            canvas.style.width = `${width}px`;
            canvas.style.height = `${height}px`;

            ctx.setTransform(dpr, 0, 0, dpr, 0, 0);

            createParticles();
        };

        const draw = () => {
            ctx.clearRect(0, 0, width, height);

            for (let i = 0; i < particles.length; i++) {
                const particle = particles[i];

                particle.x += particle.vx;
                particle.y += particle.vy;

                if (particle.x <= 0 || particle.x >= width) {
                    particle.vx *= -1;
                }

                if (particle.y <= 0 || particle.y >= height) {
                    particle.vy *= -1;
                }

                ctx.beginPath();
                ctx.arc(
                    particle.x,
                    particle.y,
                    particle.radius,
                    0,
                    Math.PI * 2
                );
                /* Use Sapphire Blue #1565C0 for particles. */
                ctx.fillStyle = "rgba(21, 101, 192, 0.4)";
                ctx.fill();

                for (let j = i + 1; j < particles.length; j++) {
                    const other = particles[j];

                    const dx = particle.x - other.x;
                    const dy = particle.y - other.y;
                    const distance = Math.sqrt(dx * dx + dy * dy);

                    if (distance < 135) {
                        const opacity = 1 - distance / 135;

                        ctx.beginPath();
                        ctx.moveTo(particle.x, particle.y);
                        ctx.lineTo(other.x, other.y);
                        /* Use Sapphire Blue for network connection lines. */
                        ctx.strokeStyle = `rgba(21, 101, 192, ${opacity * 0.15})`;
                        ctx.lineWidth = 1;
                        ctx.stroke();
                    }
                }
            }

            animationFrameId = requestAnimationFrame(draw);
        };

        resizeCanvas();
        draw();

        window.addEventListener("resize", resizeCanvas);

        return () => {
            cancelAnimationFrame(animationFrameId);
            window.removeEventListener("resize", resizeCanvas);
        };
    }, []);

    return <canvas ref={canvasRef} className="particle-network-canvas" />;
}

function TopBar({ hidden }) {
    return (
        <div className={`home-topbar ${hidden ? "home-topbar-hide" : ""}`}>
            <div>
                <span><Phone size={15} /> 0776 818 160</span>
                <span><Mail size={15} /> hello@anhnguTN.vn</span>
            </div>

            <span>
                <MapPin size={15} />
                387K1, 14B, Tran Nam Phu, Can Tho
            </span>
        </div>
    );
}

function Header({ scrolled }) {
    return (
        <header className={`home-header ${scrolled ? "home-header-scrolled" : ""}`}>
            <Link to="/" className="home-logo">
                <span className="home-logo-icon">
                    <BookOpen size={25} />
                </span>

                <span>
                    <strong>AnhNgu<span>TN</span></strong>
                    <small>Learn smarter</small>
                </span>
            </Link>

            <nav className="home-nav">
                <a href="#courses">Courses</a>
                <a href="#features">Features</a>
                <a href="#about">About us</a>
            </nav>

            <div className="home-header-actions">
                <Link to="/login" className="home-login-btn">
                    Log in
                </Link>

                
            </div>
        </header>
    );
}

function Hero() {
    return (
        <section className="home-hero">
            <div className="home-hero-left">
                <div className="home-badge">
                    <TrendingUp size={18} />
                    Can Tho's leading English learning platform
                </div>

                <h1>
                    Learn English
                    <span>the right way,</span>
                    on the right path
                </h1>

                <p>
                    Prepare for VSTEP, IELTS, and Cambridge — with teachers tracking your progress,
                    automated grading, and detailed feedback after every test.
                </p>

                <div className="home-hero-actions">
                    <Link to="/register" className="home-primary-btn">
                        Start learning now <ArrowRight size={22} />
                    </Link>

                    <Link to="/tests/free" className="home-outline-btn">
                        <Play size={20} />
                        View free practice tests
                    </Link>
                </div>

                <div className="home-slogan">
                    <span></span>
                    THE RIGHT METHOD
                    <b>•</b>
                    THE RIGHT PATH
                    <b>•</b>
                    THE RIGHT RESULTS
                    <span></span>
                </div>
            </div>

            <div className="home-hero-right">
                <div className="progress-card">
                    <div className="progress-card-header">
                        <div>
                            <BookOpen size={24} />
                        </div>
                        <span>
                            <strong>AnhNguTN</strong>
                            <small>Learning progress dashboard</small>
                        </span>
                    </div>

                    <ProgressItem title="VSTEP B1" percent={82} color="#1565C0" />
                    <ProgressItem title="VSTEP B2" percent={68} color="#0288D1" />
                    <ProgressItem title="TOEIC LISTENING AND READING" percent={55} color="#00838F" />
                    <ProgressItem title="TOEIC 4 SKILLS" percent={41} color="#2E7D32" />

                    <div className="mini-grid">
                        <MiniCard icon={<Users />} title="Learners" />
                        <MiniCard icon={<Star />} title="4.9 / 5" desc="Rating" />
                    </div>

                    <div className="teacher-row">
                        <GraduationCap size={22} />
                        <span>
                            <strong>Teachers</strong>
                            <small>Manage classes & learners</small>
                        </span>
                        <ArrowRight size={18} />
                    </div>
                </div>

                <div className="trust-card">
                    <Star size={20} />
                    <span>
                        <strong>1.000+</strong>
                        trusted learners
                    </span>
                </div>
            </div>
        </section>
    );
}

function ProgressItem({ title, percent, color }) {
    return (
        <div className="progress-item">
            <div>
                <span>{title}</span>
                <b>{percent}%</b>
            </div>

            <div className="progress-track">
                <span style={{ width: `${percent}%`, background: color }} />
            </div>
        </div>
    );
}

function MiniCard({ icon, title, desc }) {
    return (
        <div className="mini-card">
            <div>{icon}</div>
            <span>
                <strong>{title}</strong>
                <small>{desc}</small>
            </span>
        </div>
    );
}

function Stats() {
    return (
        <section className="home-stats">
            <Stat value="12.000+" label="Learners" />
            <Stat value="98%" label="Satisfaction" />
            <Stat value="4.9" label="Rating" />
            <Stat value="500+" label="Practice tests" />
        </section>
    );
}

function Stat({ value, label }) {
    return (
        <div>
            <strong>{value}</strong>
            <span>{label}</span>
        </div>
    );
}

function Features() {
    const features = [
        [<BookOpen />, "International-standard preparation", "Up-to-date VSTEP, IELTS, and Cambridge practice tests."],
        [<BarChart3 />, "Progress tracking", "Visual charts showing strengths and areas to improve in every skill."],
        [<Zap />, "Instant grading", "Automatic results after submission, with detailed feedback for every question."],
        [<Users />, "Personalized learning", "Content matched to your level, focused on your weak areas."],
        [<MessageSquare />, "Connected community", "Connect with teachers and fellow learners anytime, anywhere."],
        [<Shield />, "Curriculum-aligned", "Lessons and exercises aligned with VSTEP, IELTS, and Cambridge standards."]
    ];

    return (
        <section id="features" className="home-section">
            <SectionTitle
                tag="KEY FEATURES"
                title="A complete learning experience"
                highlight="powered by modern technology"
            />

            <div className="feature-grid">
                {features.map((item, index) => (
                    <div className="feature-card" key={index}>
                        <div>{item[0]}</div>
                        <h3>{item[1]}</h3>
                        <p>{item[2]}</p>
                    </div>
                ))}
            </div>
        </section>
    );
}

function Courses() {
    return (
        <section id="courses" className="home-section gray">
            <SectionTitle
                tag="LEARNING PROGRAMS"
                title="Courses for"
                highlight="every learner"
                desc="From primary school students to working professionals — NamThuEdu offers a tailored path for every goal."
            />

            <div className="course-list">
                <CourseCard
                    color="#1565C0"
                    icon={<BookOpen />}
                    title="VSTEP"
                    label="National"
                    subtitle="National English certification"
                    desc="Prepare for VSTEP B1, B2, and C1 under Ministry of Education standards — covering Listening, Speaking, Reading, and Writing."
                    items={["Practice tests aligned with Ministry standards", "Ideal for students & professionals", "Automated grading + AI Writing", "Levels B1 → C1"]}
                />

                <CourseCard
                    color="#0288D1"
                    icon={<Globe />}
                    title="IELTS"
                    label="International"
                    subtitle="International English certification"
                    desc="Prepare for IELTS Academic and General Training from band 4.0 to 8.5+, with detailed feedback for every skill."
                    items={["Academic & General Training", "AI feedback for Writing & Speaking", "Band score 4.0 → 8.5+", "International-standard Cambridge tests"]}
                />

                <CourseCard
                    color="#2E7D32"
                    icon={<GraduationCap />}
                    title="Cambridge"
                    label="School"
                    subtitle="General & advanced English"
                    desc="Cambridge programs for middle and high school students — building a strong foundation."
                    items={["For middle & high school students", "Close teacher support", "A clear path for every level", "Detailed grading"]}
                />
            </div>
        </section>
    );
}

function CourseCard({ color, icon, title, label, subtitle, desc, items }) {
    return (
        <div className="course-card" style={{ borderLeftColor: color }}>
            <div className="course-main">
                <div className="course-heading">
                    <div style={{ color }}>{icon}</div>

                    <span>
                        <h3>
                            {title}
                            <small style={{ color }}>{label}</small>
                        </h3>
                        <b style={{ color }}>{subtitle}</b>
                    </span>
                </div>

                <p>{desc}</p>

                <Link to="/register" style={{ borderColor: color, color }}>
                    Start preparing →
                </Link>
            </div>

            <div className="course-items">
                {items.map((item, index) => (
                    <span key={index}>
                        <CheckCircle size={20} style={{ color }} />
                        {item}
                    </span>
                ))}
            </div>
        </div>
    );
}

function Steps() {
    return (
        <section className="home-section gray">
            <SectionTitle
                tag="START IN JUST 3 STEPS"
                title="Learn now,"
                highlight="without waiting"
            />

            <div className="step-grid">
                <Step icon={<Users />} number="01" title="Create an account" desc="Sign up for free in just 30 seconds. No credit card required." />
                <Step icon={<Target />} number="02" title="Choose your path" desc="Choose a VSTEP, IELTS, or Cambridge course that fits your goal." />
                <Step icon={<TrendingUp />} number="03" title="Learn & track" desc="Complete exercises, get instant feedback, and track your daily progress." />
            </div>
        </section>
    );
}

function Step({ icon, number, title, desc }) {
    return (
        <div className="step-card">
            <strong>{number}</strong>
            <div>{icon}</div>
            <h3>{title}</h3>
            <p>{desc}</p>
        </div>
    );
}

function CTA() {
    return (
        <section className="home-cta">
            <span>✧ NAMTHUEDU ✧</span>
            <h2>
                Join NamThuEdu
                <b>today!</b>
            </h2>
            <p>Start learning smarter with a complete digital education ecosystem.</p>

            <div>
                <Link to="/register" className="home-primary-btn">
                    Try it now <ArrowRight size={22} />
                </Link>

                <a href="tel:0776818160" className="home-dark-outline-btn">
                    <Phone size={20} />
                    Contact us
                </a>
            </div>
        </section>
    );
}

function Footer() {
    return (
        <footer className="home-footer">
            <div>
                <Link to="/" className="home-logo">
                    <span className="home-logo-icon">
                        <BookOpen size={25} />
                    </span>

                    <span>
                        <strong>NamThu<span>Edu</span></strong>
                    </span>
                </Link>

                <p>
                    Can Tho's leading online English learning platform.
                    Prepare for VSTEP, IELTS, and Cambridge.
                </p>

                <small><Phone size={16} /> 0776 818 160</small>
                <small><Mail size={16} /> hello@namthu.vn</small>
                <small><MapPin size={16} /> Hẻm 387K1, 14B, Trần Nam Phú, Cần Thơ</small>
            </div>

            <div>
                <h4>COURSES</h4>
                <a>VSTEP B1, B2, C1</a>
                <a>IELTS Academic</a>
                <a>IELTS General</a>
                <a>Cambridge</a>
                <a>High School English</a>
            </div>

            <div>
                <h4>LINKS</h4>
                <a>Features</a>
                <a>About us</a>
                <a>Free practice tests</a>
                <a>Log in</a>
            </div>

            <div className="footer-bottom">
                <span>© 2024 NamThuEdu. All rights reserved.</span>
                <span>Privacy policy &nbsp;&nbsp; Terms</span>
            </div>
        </footer>
    );
}

function SectionTitle({ tag, title, highlight, desc }) {
    return (
        <div className="section-title">
            <span>{tag}</span>
            <h2>
                {title}
                <b>{highlight}</b>
            </h2>
            {desc && <p>{desc}</p>}
        </div>
    );
}

export default HomePage;
