import {
    BookOpen,
    CheckCheck,
    ClipboardList,
    FileText,
    Trophy,
    UserRoundX,
    GraduationCap,
    UsersRound,
    School
} from "lucide-react";
import { parseUtcDate } from "../../../utils/dateTimeUtils";
import {
    useNavigate
} from "react-router-dom";

import {
    
    useNotification
} from "../hooks/useNotification";

import "../styles/notification.css";

const getNotificationIcon =
    type => {
        const normalizedType =
            typeof type === "string"
                ? type
                : Number(type);

        switch (normalizedType) {
            case 1:
            case "LessonCreated":
                return (
                    <BookOpen size={19} />
                );

            case 2:
            case "ExerciseAssigned":
                return (
                    <ClipboardList
                        size={19}
                    />
                );

            case 3:
            case "TestAssigned":
                return (
                    <FileText size={19} />
                );

            case 4:
            case "CompetitionAssigned":
                return (
                    <Trophy size={19} />
                );
            case 5:
            case "AdminTeacherUnassigned":
                return (
                    <UserRoundX size={19} />
                );

            case 6:
            case "AdminClassWithoutTeacher":
                return (
                    <GraduationCap size={19} />
                );

            case 7:
            case "AdminStudentUnassigned":
                return (
                    <UsersRound size={19} />
                );

            case 8:
            case "AdminClassAtCapacity":
                return (
                    <School size={19} />
                );
            default:
                return (
                    <FileText size={19} />
                );
        }
    };

const getTypeClassName =
    type => {
        const normalizedType =
            typeof type === "string"
                ? type
                : Number(type);

        switch (normalizedType) {
            case 1:
            case "LessonCreated":
                return "lesson";

            case 2:
            case "ExerciseAssigned":
                return "exercise";

            case 3:
            case "TestAssigned":
                return "test";

            case 4:
            case "CompetitionAssigned":
                return "competition";
            case 5:
            case "AdminTeacherUnassigned":
                return "admin-teacher";

            case 6:
            case "AdminClassWithoutTeacher":
                return "admin-class";

            case 7:
            case "AdminStudentUnassigned":
                return "admin-student";

            case 8:
            case "AdminClassAtCapacity":
                return "admin-capacity";
            default:
                return "default";
        }
    };

const formatNotificationTime =
    value => {
        const date =
            parseUtcDate(value);

        if (!date) {
            return "";
        }

        const differenceMilliseconds =
            Date.now() -
            date.getTime();

        const seconds =
            Math.max(
                0,
                Math.floor(
                    differenceMilliseconds /
                    1000
                )
            );

        if (seconds < 30) {
            return "Just now";
        }

        if (seconds < 60) {
            return `${seconds} seconds ago`;
        }

        const minutes =
            Math.floor(
                seconds / 60
            );

        if (minutes < 60) {
            return `${minutes} minutes ago`;
        }

        const hours =
            Math.floor(
                minutes / 60
            );

        if (hours < 24) {
            return `${hours} hours ago`;
        }

        const days =
            Math.floor(
                hours / 24
            );

        if (days < 7) {
            return `${days} days ago`;
        }

        return date.toLocaleString(
            "vi-VN",
            {
                timeZone:
                    "Asia/Ho_Chi_Minh",
                hour12: false,
                year: "numeric",
                month: "2-digit",
                day: "2-digit",
                hour: "2-digit",
                minute: "2-digit"
            }
        );
    };

function NotificationDropdown({
    onClose
}) {
    const navigate =
        useNavigate();

    const {
        notifications,
        unreadCount,
        loading,
        connectionState,
        markAsRead,
        markAllAsRead
    } = useNotification();

    const handleNotificationClick =
        async notification => {
            try {
                if (
                    !notification.isRead
                ) {
                    await markAsRead(
                        notification
                            .userNotificationId
                    );
                }
            }
            catch {
                // Vẫn cho phép điều hướng nếu
                // API mark read gặp lỗi.
            }

            onClose?.();

            if (
                notification.actionUrl
            ) {
                navigate(
                    notification.actionUrl
                );
            }
        };

    const handleMarkAllAsRead =
        async () => {
            try {
                await markAllAsRead();
            }
            catch {
                alert(
                    "Unable to mark all notifications as read"
                );
            }
        };

    return (
        <div className="notification-dropdown">
            <div className="notification-dropdown-header">
                <div>
                    <h3>Notifications</h3>

                    <span
                        className={
                            connectionState ===
                                "connected"
                                ? "notification-connection connected"
                                : "notification-connection"
                        }
                    >
                        {connectionState ===
                            "connected"
                            ? "Connected live"
                            : connectionState ===
                                "reconnecting"
                                ? "Reconnecting"
                                : "Using server data"}
                    </span>
                </div>

                {unreadCount > 0 && (
                    <button
                        type="button"
                        className="notification-read-all-btn"
                        onClick={
                            handleMarkAllAsRead
                        }
                    >
                        <CheckCheck
                            size={16}
                        />

                        Mark all as read
                    </button>
                )}
            </div>

            <div className="notification-dropdown-body">
                {loading &&
                    notifications.length ===
                    0 && (
                        <div className="notification-empty">
                            Loading notifications...
                        </div>
                    )}

                {!loading &&
                    notifications.length ===
                    0 && (
                        <div className="notification-empty">
                            <span>
                                You have no notifications.
                            </span>
                        </div>
                    )}

                {notifications.map(
                    notification => (
                        <button
                            type="button"
                            key={
                                notification
                                    .userNotificationId
                            }
                            className={
                                notification.isRead
                                    ? "notification-item"
                                    : "notification-item unread"
                            }
                            onClick={() =>
                                handleNotificationClick(
                                    notification
                                )
                            }
                        >
                            <span
                                className={
                                    `notification-item-icon ` +
                                    getTypeClassName(
                                        notification.typeName ||
                                        notification.type
                                    )
                                }
                            >
                                {getNotificationIcon(
                                    notification.typeName ||
                                    notification.type
                                )}
                            </span>

                            <span className="notification-item-content">
                                <strong>
                                    {
                                        notification
                                            .title
                                    }
                                </strong>

                                <span className="notification-item-message">
                                    {
                                        notification
                                            .message
                                    }
                                </span>

                                <span className="notification-item-meta">
                                    {formatNotificationTime(
                                        notification
                                            .createdAt
                                    )}

                                    {notification
                                        .classRoomName && (
                                            <>
                                                {" · "}
                                                {
                                                    notification
                                                        .classRoomName
                                                }
                                            </>
                                        )}
                                </span>
                            </span>

                            {!notification
                                .isRead && (
                                    <span className="notification-unread-dot" />
                                )}
                        </button>
                    )
                )}
            </div>
            <div className="notification-dropdown-footer">
                <button
                    type="button"
                    onClick={() => {
                        onClose?.();

                        navigate(
                            "/notifications"
                        );
                    }}
                >
                    View all notifications
                </button>
            </div>
        </div>
    );
}

export default NotificationDropdown;
