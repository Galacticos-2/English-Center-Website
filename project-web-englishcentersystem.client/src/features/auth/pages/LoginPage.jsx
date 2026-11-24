import { useState } from "react";
import { useNavigate } from "react-router-dom";
import { BookOpen, Eye, EyeOff, LockKeyhole, UserRound } from "lucide-react";

import { loginApi } from "../api/authApi";
import { authStore } from "../store/authStore";
import "../styles/loginPage.css";

function LoginPage() {
    const navigate = useNavigate();
    const [formData, setFormData] = useState({ userName: "", password: "" });
    const [rememberMe, setRememberMe] = useState(false);
    const [showPassword, setShowPassword] = useState(false);
    const [loading, setLoading] = useState(false);
    const [error, setError] = useState("");

    const handleChange = event => {
        setFormData({ ...formData, [event.target.name]: event.target.value });
    };

    const handleSubmit = async event => {
        event.preventDefault();

        try {
            setLoading(true);
            setError("");

            const response = await loginApi(formData);
            const data = response.data;

            authStore.saveAuth({
                token: data.token,
                userId: data.userId,
                userName: data.userName,
                email: data.email,
                role: data.role
            }, rememberMe);

            if (data.role === "Admin") navigate("/admin/dashboard");
            else if (data.role === "Teacher") navigate("/teacher");
            else if (data.role === "Student") navigate("/student");
            else navigate("/login");
        }
        catch (err) {
            setError(err.response?.data?.message || "Login failed. Please try again.");
        }
        finally {
            setLoading(false);
        }
    };

    return (
        <main className="login-page">
            <section className="login-panel">
                <div className="login-card">
                    <div className="login-brand">
                        <span className="login-brand__mark"><BookOpen size={23} strokeWidth={2.4} /></span>
                        <span>TN English Center</span>
                    </div>
                    <header className="login-card__header">
                        <h2>Welcome back</h2>
                        <p>Log in to continue your journey.</p>
                    </header>

                    <form className="login-form" onSubmit={handleSubmit}>
                        <label className="login-form__label" htmlFor="userName">Username</label>
                        <div className="login-input">
                            <UserRound size={20} aria-hidden="true" />
                            <input id="userName" type="text" name="userName" autoComplete="username" placeholder="Enter your username" value={formData.userName} onChange={handleChange} required />
                        </div>

                        <label className="login-form__label" htmlFor="password">Password</label>
                        <div className="login-input">
                            <LockKeyhole size={20} aria-hidden="true" />
                            <input id="password" type={showPassword ? "text" : "password"} name="password" autoComplete="current-password" placeholder="Enter your password" value={formData.password} onChange={handleChange} required />
                            <button type="button" className="login-input__toggle" onClick={() => setShowPassword(!showPassword)} aria-label={showPassword ? "Hide password" : "Show password"}>
                                {showPassword ? <EyeOff size={20} /> : <Eye size={20} />}
                            </button>
                        </div>

                        <label className="login-remember">
                            <input type="checkbox" checked={rememberMe} onChange={event => setRememberMe(event.target.checked)} />
                            <span className="login-remember__switch" aria-hidden="true" />
                            <span>Remember me</span>
                        </label>

                        {error && <p className="login-form__error" role="alert">{error}</p>}

                        <button className="login-submit" type="submit" disabled={loading}>
                            <BookOpen size={20} />
                            {loading ? "Logging in..." : "Log in"}
                        </button>
                    </form>

                    <p className="login-card__help">Need help? Contact <a href="tel:0776818160">0776 818 160</a></p>
                </div>
            </section>
        </main>
    );
}

export default LoginPage;
