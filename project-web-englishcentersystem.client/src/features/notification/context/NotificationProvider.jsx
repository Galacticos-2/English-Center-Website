import {
  
    useCallback,
    useEffect,
    useMemo,
    useReducer,
    useRef,
    useState
} from "react";
import {
    NotificationContext
} from "./NotificationContext";
import {
    getMyNotificationsApi,
    markAllNotificationsAsReadApi,
    markNotificationAsReadApi
} from "../api/notificationApi";
import {
    getBrowserNotificationEnabled,
    getBrowserNotificationPermission,
    getSoundEnabled,
    isBrowserNotificationSupported,
    playNotificationSound,
    requestBrowserNotificationPermission,
    saveBrowserNotificationEnabled,
    saveSoundEnabled,
    showBrowserNotification,
    unlockNotificationSound
} from "../services/browserNotificationService";
import {
    notificationHub
} from "../services/notificationHub";

import {
    NOTIFICATION_ACTIONS,
    notificationInitialState,
    notificationReducer
} from "../store/notificationStore";

import {
    authStore
} from "../../auth/store/authStore";



export function NotificationProvider({
    children
}) {
    const [
        state,
        dispatch
    ] = useReducer(
        notificationReducer,
        notificationInitialState
    );
    const [
        soundEnabled,
        setSoundEnabledState
    ] = useState(
        getSoundEnabled()
    );

    const [
        browserNotificationEnabled,
        setBrowserNotificationEnabledState
    ] = useState(
        getBrowserNotificationEnabled()
    );

    const [
        browserNotificationPermission,
        setBrowserNotificationPermission
    ] = useState(
        getBrowserNotificationPermission()
    );

    const processedRealtimeIdsRef =
        useRef(new Set());
    const loadNotifications =
        useCallback(async () => {
            if (
                !authStore
                    .isAuthenticated()
            ) {
                return;
            }

            dispatch({
                type:
                    NOTIFICATION_ACTIONS
                        .LOAD_START
            });

            try {
                const data =
                    await getMyNotificationsApi({
                        page: 1,
                        pageSize: 30
                    });

                dispatch({
                    type:
                        NOTIFICATION_ACTIONS
                            .LOAD_SUCCESS,

                    payload: {
                        items:
                            Array.isArray(
                                data?.items
                            )
                                ? data.items
                                : [],

                        unreadCount:
                            data?.unreadCount ||
                            0,

                        totalCount:
                            data?.totalItems || 0
                    }
                });
            }
            catch (error) {
                console.error(
                    "Không thể tải notification:",
                    error.response?.data ||
                    error
                );

                dispatch({
                    type:
                        NOTIFICATION_ACTIONS
                            .LOAD_FAILED
                });
            }
        }, []);
    const disableBrowserNotifications =
        useCallback(() => {
            saveBrowserNotificationEnabled(
                false
            );

            setBrowserNotificationEnabledState(
                false
            );
        }, []);
    const rememberRealtimeNotification =
        useCallback(
            userNotificationId => {
                const normalizedId =
                    Number(
                        userNotificationId
                    );

                if (
                    !Number.isFinite(
                        normalizedId
                    ) ||
                    normalizedId <= 0
                ) {
                    return true;
                }

                const processedIds =
                    processedRealtimeIdsRef
                        .current;

                if (
                    processedIds.has(
                        normalizedId
                    )
                ) {
                    return false;
                }

                processedIds.add(
                    normalizedId
                );

                /*
                 * Chỉ giữ tối đa 300 ID gần nhất
                 * để Set không tăng vô hạn.
                 */
                if (
                    processedIds.size >
                    300
                ) {
                    const ids =
                        Array.from(
                            processedIds
                        );

                    const idsToRemove =
                        ids.slice(
                            0,
                            ids.length -
                            200
                        );

                    idsToRemove.forEach(
                        id =>
                            processedIds
                                .delete(id)
                    );
                }

                return true;
            },
            []
        );
    const setNotificationSoundEnabled =
        useCallback(
            async enabled => {
                const nextEnabled =
                    Boolean(enabled);

                if (nextEnabled) {
                    const unlocked =
                        await unlockNotificationSound();

                    if (!unlocked) {
                        return {
                            success: false,
                            message:
                                "Trình duyệt chưa cho phép phát âm thanh."
                        };
                    }
                }

                saveSoundEnabled(
                    nextEnabled
                );

                setSoundEnabledState(
                    nextEnabled
                );

                if (nextEnabled) {
                    await playNotificationSound();
                }

                return {
                    success: true
                };
            },
            []
        );
    const enableBrowserNotifications =
        useCallback(async () => {
            if (
                !isBrowserNotificationSupported()
            ) {
                setBrowserNotificationPermission(
                    "unsupported"
                );

                return {
                    success: false,
                    permission:
                        "unsupported",

                    message:
                        "Trình duyệt không hỗ trợ thông báo hệ thống."
                };
            }

            const permission =
                await requestBrowserNotificationPermission();

            setBrowserNotificationPermission(
                permission
            );

            const enabled =
                permission === "granted";

            setBrowserNotificationEnabledState(
                enabled
            );

            return {
                success:
                    enabled,

                permission,

                message:
                    enabled
                        ? null
                        : permission ===
                            "denied"
                            ? "Bạn đã chặn quyền thông báo của website."
                            : "Bạn chưa cấp quyền thông báo."
            };
        }, []);
    const markAsRead =
        useCallback(
            async userNotificationId => {
                const notification =
                    state.notifications.find(
                        item =>
                            Number(
                                item
                                    .userNotificationId
                            ) ===
                            Number(
                                userNotificationId
                            )
                    );

                if (
                    !notification ||
                    notification.isRead
                ) {
                    return;
                }

                dispatch({
                    type:
                        NOTIFICATION_ACTIONS
                            .MARK_AS_READ,

                    payload:
                        userNotificationId
                });

                try {
                    await markNotificationAsReadApi(
                        userNotificationId
                    );
                }
                catch (error) {
                    console.error(
                        "Không thể đánh dấu notification đã đọc:",
                        error.response?.data ||
                        error
                    );

                    await loadNotifications();

                    throw error;
                }
            },
            [
                state.notifications,
                loadNotifications
            ]
        );

    const markAllAsRead =
        useCallback(async () => {
            if (
                state.unreadCount <= 0
            ) {
                return;
            }

            dispatch({
                type:
                    NOTIFICATION_ACTIONS
                        .MARK_ALL_AS_READ
            });

            try {
                await markAllNotificationsAsReadApi();
            }
            catch (error) {
                console.error(
                    "Không thể đánh dấu tất cả đã đọc:",
                    error.response?.data ||
                    error
                );

                await loadNotifications();

                throw error;
            }
        }, [
            state.unreadCount,
            loadNotifications
        ]);

    useEffect(() => {
        if (
            !authStore
                .isAuthenticated()
        ) {
            dispatch({
                type:
                    NOTIFICATION_ACTIONS
                        .RESET
            });

            return;
        }

        loadNotifications();

        const handleReceiveNotification =
            async notification => {
                const isNewNotification =
                    rememberRealtimeNotification(
                        notification
                            ?.userNotificationId
                    );

                if (!isNewNotification) {
                    return;
                }

                dispatch({
                    type:
                        NOTIFICATION_ACTIONS
                            .RECEIVE_NOTIFICATION,

                    payload:
                        notification
                });

                await playNotificationSound();

                if (
                    typeof document !==
                    "undefined" &&
                    document.hidden
                ) {
                    showBrowserNotification(
                        notification
                    );
                }
            };

        const removeNotificationHandler =
            notificationHub
                .onReceiveNotification(
                    handleReceiveNotification
                );

        const removeUnreadCountHandler =
            notificationHub
                .onUnreadCountChanged(
                    unreadCount => {
                        dispatch({
                            type:
                                NOTIFICATION_ACTIONS
                                    .SET_UNREAD_COUNT,

                            payload:
                                Math.max(
                                    0,
                                    Number(
                                        unreadCount ||
                                        0
                                    )
                                )
                        });
                    }
                );

        const removeConnectionHandler =
            notificationHub
                .onConnectionStateChanged(
                    connectionState => {
                        dispatch({
                            type:
                                NOTIFICATION_ACTIONS
                                    .CONNECTION_STATE_CHANGED,

                            payload:
                                connectionState
                        });

                        if (
                            connectionState ===
                            "connected"
                        ) {
                            loadNotifications();
                        }
                    }
                );

        notificationHub
            .start()
            .catch(() => {
                // REST API vẫn hoạt động nếu
                // kết nối realtime thất bại.
            });

        return () => {
            removeNotificationHandler?.();
            removeUnreadCountHandler?.();
            removeConnectionHandler?.();

            /*
             * Không stop tức thời vì React StrictMode
             * sẽ cleanup rồi mount lại ngay trong development.
             */
            notificationHub.scheduleStop(
                200
            );
        };
    }, [
        loadNotifications,
        rememberRealtimeNotification
    ]);
    useEffect(() => {
        const synchronizePermission =
            () => {
                const permission =
                    getBrowserNotificationPermission();

                setBrowserNotificationPermission(
                    permission
                );

                if (
                    permission !==
                    "granted"
                ) {
                    saveBrowserNotificationEnabled(
                        false
                    );

                    setBrowserNotificationEnabledState(
                        false
                    );
                }
            };

        window.addEventListener(
            "focus",
            synchronizePermission
        );

        document.addEventListener(
            "visibilitychange",
            synchronizePermission
        );

        return () => {
            window.removeEventListener(
                "focus",
                synchronizePermission
            );

            document.removeEventListener(
                "visibilitychange",
                synchronizePermission
            );
        };
    }, []);
    const value =
        useMemo(
            () => ({
                notifications:
                    state.notifications,

                unreadCount:
                    state.unreadCount,

                totalCount:
                    state.totalCount,

                loading:
                    state.loading,

                connected:
                    state.connected,

                connectionState:
                    state.connectionState,

                soundEnabled,

                browserNotificationEnabled,

                browserNotificationPermission,

                browserNotificationSupported:
                    isBrowserNotificationSupported(),

                reloadNotifications:
                    loadNotifications,

                markAsRead,

                markAllAsRead,

                setNotificationSoundEnabled,

                enableBrowserNotifications,

                disableBrowserNotifications
            }),
            [
                state.notifications,
                state.unreadCount,
                state.totalCount,
                state.loading,
                state.connected,
                state.connectionState,
                soundEnabled,
                browserNotificationEnabled,
                browserNotificationPermission,
                loadNotifications,
                markAsRead,
                markAllAsRead,
                setNotificationSoundEnabled,
                enableBrowserNotifications,
                disableBrowserNotifications
            ]
        );

    return (
        <NotificationContext.Provider
            value={value}
        >
            {children}
        </NotificationContext.Provider>
    );
}