export const notificationInitialState = {
    notifications: [],
    unreadCount: 0,
    totalCount: 0,
    loading: false,
    connected: false,
    connectionState: "disconnected"
};

export const NOTIFICATION_ACTIONS = {
    LOAD_START:
        "notification/load-start",

    LOAD_SUCCESS:
        "notification/load-success",

    LOAD_FAILED:
        "notification/load-failed",

    RECEIVE_NOTIFICATION:
        "notification/receive",

    SET_UNREAD_COUNT:
        "notification/set-unread-count",

    MARK_AS_READ:
        "notification/mark-as-read",

    MARK_ALL_AS_READ:
        "notification/mark-all-as-read",

    CONNECTION_STATE_CHANGED:
        "notification/connection-state-changed",

    RESET:
        "notification/reset"
};

const addNotificationWithoutDuplicate = (
    notifications,
    incomingNotification
) => {
    const existed =
        notifications.some(item =>
            Number(
                item.userNotificationId
            ) ===
            Number(
                incomingNotification
                    .userNotificationId
            )
        );

    if (existed) {
        return notifications;
    }

    return [
        incomingNotification,
        ...notifications
    ];
};

export const notificationReducer = (
    state,
    action
) => {
    switch (action.type) {
        case NOTIFICATION_ACTIONS.LOAD_START:
            return {
                ...state,
                loading: true
            };

        case NOTIFICATION_ACTIONS.LOAD_SUCCESS:
            return {
                ...state,
                notifications:
                    action.payload.items || [],

                unreadCount:
                    Number(
                        action.payload
                            .unreadCount || 0
                    ),

                totalCount:
                    Number(
                        action.payload
                            .totalCount || 0
                    ),

                loading: false
            };

        case NOTIFICATION_ACTIONS.LOAD_FAILED:
            return {
                ...state,
                loading: false
            };

        case NOTIFICATION_ACTIONS
            .RECEIVE_NOTIFICATION: {
                const nextNotifications =
                    addNotificationWithoutDuplicate(
                        state.notifications,
                        action.payload
                    );

                const wasAdded =
                    nextNotifications !==
                    state.notifications;

                return {
                    ...state,

                    notifications:
                        nextNotifications,

                    totalCount:
                        wasAdded
                            ? state.totalCount + 1
                            : state.totalCount
                };
            }

        case NOTIFICATION_ACTIONS
            .SET_UNREAD_COUNT:
            return {
                ...state,

                unreadCount:
                    Math.max(
                        0,
                        Number(
                            action.payload || 0
                        )
                    )
            };

        case NOTIFICATION_ACTIONS
            .MARK_AS_READ: {
                const userNotificationId =
                    Number(action.payload);

                const target =
                    state.notifications.find(
                        item =>
                            Number(
                                item
                                    .userNotificationId
                            ) ===
                            userNotificationId
                    );

                if (
                    !target ||
                    target.isRead
                ) {
                    return state;
                }

                return {
                    ...state,

                    notifications:
                        state.notifications.map(
                            item =>
                                Number(
                                    item
                                        .userNotificationId
                                ) ===
                                    userNotificationId
                                    ? {
                                        ...item,
                                        isRead: true,
                                        readAt:
                                            new Date()
                                                .toISOString()
                                    }
                                    : item
                        ),

                    unreadCount:
                        Math.max(
                            0,
                            state.unreadCount - 1
                        )
                };
            }

        case NOTIFICATION_ACTIONS
            .MARK_ALL_AS_READ:
            return {
                ...state,

                notifications:
                    state.notifications.map(
                        item => ({
                            ...item,
                            isRead: true,
                            readAt:
                                item.readAt ||
                                new Date()
                                    .toISOString()
                        })
                    ),

                unreadCount: 0
            };

        case NOTIFICATION_ACTIONS
            .CONNECTION_STATE_CHANGED:
            return {
                ...state,

                connected:
                    action.payload ===
                    "connected",

                connectionState:
                    action.payload
            };

        case NOTIFICATION_ACTIONS.RESET:
            return {
                ...notificationInitialState
            };

        default:
            return state;
    }
};