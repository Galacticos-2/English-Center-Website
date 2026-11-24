import { useNavigate, useLocation } from "react-router-dom";

import {
    LayoutDashboard,
    BookOpen,
    FileText,
    ClipboardList,
    Users,
    LogOut,
    ChevronRight,
    Trophy,
    Video,
    Layers3
} from "lucide-react";
function TeacherSidebar() {
    const navigate = useNavigate();
    const location = useLocation();

    const logout = () => {
        localStorage.clear();
        navigate("/login");
    };

    const menuItems = [
        {
            label: "Dashboard",
            path: "/teacher",
            icon: <LayoutDashboard size={20} />
        },
       
        {
            label: "Lessons",
            path: "/teacher/lessons",
            icon: <BookOpen size={20} />
        },
       
        {
            label: "Exercises",
            path: "/teacher/exercises",
            icon: <FileText size={20} />
        },
        {
            label: "Flashcards",
            path: "/teacher/flashcards",
            icon: <Layers3 size={20} />
        },
        
        {
            label: "Tests",
            path: "/teacher/exams",
            icon: <ClipboardList size={20} />
        },
        {
            label: "Competition Tests",
            path: "/teacher/competition",
            icon: <Trophy size={20} />
        },
        {
            label: "Online Classroom",
            path: "/teacher/online-classrooms",
            icon: <Video size={20} />
        },
        {
            label: "Students",
            path: "/teacher/students",
            icon: <Users size={20} />
        }
    ];

    return (
        <aside className="sidebar teacher-sidebar">
            <div className="logo">
              
            </div>

            <div className="admin-badge">
                Teacher
            </div>

            <nav className="menu">
                {menuItems.map(item => (
                    <div
                        key={item.path}
                        className={
                            location.pathname === item.path
                                ? "menu-item active"
                                : "menu-item"
                        }
                        onClick={() => navigate(item.path)}
                    >
                        <div className="menu-left">
                            {item.icon}
                            <span>{item.label}</span>
                        </div>

                        {location.pathname === item.path && (
                            <ChevronRight size={18} />
                        )}
                    </div>
                ))}
            </nav>
            
            <button
                className="logout-btn"
                onClick={logout}
            >
                <LogOut size={18} />
                Logout
            </button>
        </aside>
    );
}

export default TeacherSidebar;
