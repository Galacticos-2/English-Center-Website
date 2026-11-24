import {
    useNavigate,
    useLocation
} from "react-router-dom";

import {
    LayoutDashboard,
    BookOpen,
    GraduationCap,
    Users,
 
    ChevronRight,
    LogOut
} from "lucide-react";

import { authStore }
    from "../../auth/store/authStore";

function Sidebar() {

    const navigate =
        useNavigate();

    const location =
        useLocation();

    const handleLogout = () => {

        authStore.clearAuth();

        navigate("/");
    };

    return (

        <aside className="sidebar">

            <div className="logo">
                
            </div>

            <div className="admin-badge">
                Administrator
            </div>

            <nav className="menu">

                <div
                    className={
                        location.pathname ===
                            "/admin/dashboard"
                            ? "menu-item active"
                            : "menu-item"
                    }
                    onClick={() =>
                        navigate("/admin/dashboard")
                    }
                >

                    <div className="menu-left">
                        <LayoutDashboard size={20} />
                        <span>Dashboard</span>
                    </div>

                    {
                        location.pathname ===
                        "/admin/dashboard" &&
                        <ChevronRight size={18} />
                    }

                </div>

                <div
                    className={
                        location.pathname ===
                            "/admin/courses"
                            ? "menu-item active"
                            : "menu-item"
                    }
                    onClick={() =>
                        navigate("/admin/courses")
                    }
                >

                    <div className="menu-left">
                        <BookOpen size={20} />
                        <span>Courses</span>
                    </div>

                    {
                        location.pathname ===
                        "/admin/courses" &&
                        <ChevronRight size={18} />
                    }

                </div>

                

                <div
                    className={
                        location.pathname ===
                            "/admin/students"
                            ? "menu-item active"
                            : "menu-item"
                    }
                    onClick={() =>
                        navigate("/admin/students")
                    }
                >

                    <div className="menu-left">
                        <Users size={20} />
                        <span>Students</span>
                    </div>

                    {
                        location.pathname ===
                        "/admin/students" &&
                        <ChevronRight size={18} />
                    }

                </div>
                <div
                    className={
                        location.pathname ===
                            "/admin/teachers"
                            ? "menu-item active"
                            : "menu-item"
                    }
                    onClick={() =>
                        navigate("/admin/teachers")
                    }
                >

                    <div className="menu-left">

                        <GraduationCap size={20} />

                        <span>
                            Teachers
                        </span>

                    </div>

                    <ChevronRight size={18} />

                </div>
                <div
                    className={
                        location.pathname ===
                            "/admin/statistics"
                            ? "menu-item active"
                            : "menu-item"
                    }
                >

                  

                </div>

            </nav>

            <button
                className="logout-btn"
                onClick={handleLogout}
            >
                <LogOut size={18} />
                Logout
            </button>

        </aside>
    );
}

export default Sidebar;
