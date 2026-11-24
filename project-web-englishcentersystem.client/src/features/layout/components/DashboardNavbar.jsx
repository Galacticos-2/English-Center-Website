import {
    useEffect,
    useRef,
    useState
} from "react";

import {
    Bell,
    ChevronDown,
    KeyRound,
    LogOut,
    User
} from "lucide-react";

import {
    NavLink,
    useNavigate
} from "react-router-dom";

import {
    getMyInfoApi
} from "../../profile/api/profileApi";

import {
    authStore
} from "../../auth/store/authStore";

import {
    useNotification
} from "../../notification/hooks/useNotification";

import NotificationDropdown
    from "../../notification/components/NotificationDropdown";
import "../styles/layout.css";
import "../../notification/styles/notification.css";


function DashboardNavbar() {
    const navigate =
        useNavigate();

    const notificationWrapRef =
        useRef(null);

    const userMenuWrapRef =
        useRef(null);

    const [
        userMenuOpen,
        setUserMenuOpen
    ] = useState(false);

    const [
        notificationOpen,
        setNotificationOpen
    ] = useState(false);

    const [user, setUser] =
        useState(null);

    const {
        unreadCount
    } = useNotification();

    const currentUser =
        authStore.getUser();

    const role =
        currentUser?.role ||
        currentUser?.user?.role;

    const isStudent =
        role === "Student";

    const goHomeByRole = () => {
        switch (role) {
            case "Student":
                navigate("/student");
                break;

            case "Teacher":
                navigate("/teacher");
                break;

            case "Admin":
                navigate(
                    "/admin/dashboard"
                );
                break;

            default:
                navigate("/");
                break;
        }
    };

    const loadUser = async () => {
        try {
            const response =
                await getMyInfoApi();

            setUser(
                response.data
            );
        }
        catch (error) {
            console.error(
                "Không thể tải thông tin người dùng:",
                error
            );
        }
    };

    useEffect(() => {
        loadUser();

        const handler =
            () => loadUser();

        window.addEventListener(
            "avatar-updated",
            handler
        );

        return () => {
            window.removeEventListener(
                "avatar-updated",
                handler
            );
        };
    }, []);

    useEffect(() => {
        const handleOutsideClick =
            event => {
                if (
                    notificationWrapRef
                        .current &&
                    !notificationWrapRef
                        .current
                        .contains(
                            event.target
                        )
                ) {
                    setNotificationOpen(
                        false
                    );
                }

                if (
                    userMenuWrapRef
                        .current &&
                    !userMenuWrapRef
                        .current
                        .contains(
                            event.target
                        )
                ) {
                    setUserMenuOpen(
                        false
                    );
                }
            };

        document.addEventListener(
            "mousedown",
            handleOutsideClick
        );

        return () => {
            document.removeEventListener(
                "mousedown",
                handleOutsideClick
            );
        };
    }, []);

    const toggleNotifications =
        () => {
            setUserMenuOpen(false);

            setNotificationOpen(
                current => !current
            );
        };

    const toggleUserMenu =
        () => {
            setNotificationOpen(false);

            setUserMenuOpen(
                current => !current
            );
        };

    const logout = () => {
        setNotificationOpen(false);
        setUserMenuOpen(false);

        authStore.clearAuth();

        navigate(
            "/login",
            {
                replace: true
            }
        );
    };

    const badgeText =
        unreadCount > 99
            ? "99+"
            : String(unreadCount);

    return (
        <header className="dashboard-navbar">
            <div
                className="brand-box"
                onClick={goHomeByRole}
            >
                <img
                    src="/logo.png"
                    alt="logo"
                    className="brand-logo"
                />

                <span>
                    TN English
                </span>
            </div>

            {isStudent && (
                <nav className="student-navbar-menu">
                    <NavLink
                        to="/student/learning"
                        className={({
                            isActive
                        }) =>
                            `student-navbar-link ${isActive
                                ? "active"
                                : ""
                            }`
                        }
                    >
                        Learning Tutorial
                    </NavLink>

                    <NavLink
                        to="/student/exercises"
                        className={({
                            isActive
                        }) =>
                            `student-navbar-link ${isActive
                                ? "active"
                                : ""
                            }`
                        }
                    >
                        Exercises
                    </NavLink>

                    <NavLink
                        to="/student/flashcards"
                        className={({
                            isActive
                        }) =>
                            `student-navbar-link ${isActive
                                ? "active"
                                : ""
                            }`
                        }
                    >
                        Flashcards
                    </NavLink>

                    <NavLink
                        to="/student/tests"
                        className={({
                            isActive
                        }) =>
                            `student-navbar-link ${isActive
                                ? "active"
                                : ""
                            }`
                        }
                    >
                        Test
                    </NavLink>

                    <NavLink
                        to="/student/competitions"
                        className={({
                            isActive
                        }) =>
                            `student-navbar-link ${isActive
                                ? "active"
                                : ""
                            }`
                        }
                    >
                        Competition Test
                    </NavLink>

                    <NavLink
                        to="/student/online-classrooms"
                        className={({
                            isActive
                        }) =>
                            `student-navbar-link ${isActive
                                ? "active"
                                : ""
                            }`
                        }
                    >
                        Online Classroom
                    </NavLink>
                </nav>
            )}

            <div className="nav-actions">
                <div
                    ref={
                        notificationWrapRef
                    }
                    className="notification-wrap"
                >
                    <button
                        type="button"
                        className={
                            notificationOpen
                                ? "notification-btn active"
                                : "notification-btn"
                        }
                        aria-label="Open notifications"
                        aria-expanded={
                            notificationOpen
                        }
                        onClick={
                            toggleNotifications
                        }
                    >
                        <Bell size={20} />

                        {unreadCount > 0 && (
                            <span className="notification-badge">
                                {badgeText}
                            </span>
                        )}
                    </button>

                    {notificationOpen && (
                        <NotificationDropdown
                            onClose={() =>
                                setNotificationOpen(
                                    false
                                )
                            }
                        />
                    )}
                </div>

                <div
                    ref={userMenuWrapRef}
                    className="user-menu-wrap"
                >
                    <button
                        type="button"
                        className="user-menu-btn"
                        onClick={
                            toggleUserMenu
                        }
                    >
                        <img
                            src={
                                user?.avatarUrl ||
                                "/default-avatar.png"
                            }
                            alt="avatar"
                            className="nav-avatar"
                        />

                        <strong>
                            {user?.fullName ||
                                user?.userName}
                        </strong>

                        <ChevronDown
                            size={16}
                        />
                    </button>

                    {userMenuOpen && (
                        <div className="user-dropdown">
                            <button
                                type="button"
                                onClick={() => {
                                    setUserMenuOpen(
                                        false
                                    );

                                    navigate(
                                        "/my-info"
                                    );
                                }}
                            >
                                <User
                                    size={17}
                                />

                                My Account
                            </button>

                            <button type="button">
                                <KeyRound
                                    size={17}
                                />

                                Change Password
                            </button>

                            <div className="dropdown-line" />

                            <button
                                type="button"
                                className="logout-item"
                                onClick={logout}
                            >
                                <LogOut
                                    size={17}
                                />

                                Log out
                            </button>
                        </div>
                    )}
                </div>
            </div>
        </header>
    );
}

export default DashboardNavbar;
