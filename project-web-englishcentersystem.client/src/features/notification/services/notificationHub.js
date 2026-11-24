import {
    HubConnectionBuilder,
    HubConnectionState,
    LogLevel
} from "@microsoft/signalr";

import apiClient
    from "../../../app/apiClient";

import {
    authStore
} from "../../auth/store/authStore";

const RECEIVE_NOTIFICATION_EVENT =
    "ReceiveNotification";

const UNREAD_COUNT_CHANGED_EVENT =
    "UnreadCountChanged";

const getApiOrigin = () => {
    const baseUrl =
        apiClient.defaults.baseURL ||
        "https://localhost:7157/api";

    return baseUrl.replace(
        /\/api\/?$/i,
        ""
    );
};

class NotificationHubService {
    constructor() {
        this.connection = null;

        this.receiveNotificationHandlers =
            new Set();

        this.unreadCountHandlers =
            new Set();

        this.connectionStateHandlers =
            new Set();

        this.startPromise = null;
        this.stopTimer = null;
    }

    createConnection() {
        if (this.connection) {
            return this.connection;
        }

        const hubUrl =
            `${getApiOrigin()}/hubs/notifications`;

        this.connection =
            new HubConnectionBuilder()
                .withUrl(
                    hubUrl,
                    {
                        accessTokenFactory:
                            () =>
                                authStore
                                    .getToken() || ""
                    }
                )
                .withAutomaticReconnect([
                    0,
                    2000,
                    5000,
                    10000,
                    30000
                ])
                .configureLogging(
                    LogLevel.Information
                )
                .build();

        this.connection.on(
            RECEIVE_NOTIFICATION_EVENT,
            notification => {
                this.receiveNotificationHandlers
                    .forEach(handler => {
                        handler(notification);
                    });
            }
        );

        this.connection.on(
            UNREAD_COUNT_CHANGED_EVENT,
            payload => {
                const unreadCount =
                    typeof payload === "number"
                        ? payload
                        : payload?.unreadCount;

                this.unreadCountHandlers
                    .forEach(handler => {
                        handler(
                            Number(
                                unreadCount || 0
                            )
                        );
                    });
            }
        );

        this.connection.onreconnecting(
            error => {
                console.warn(
                    "SignalR đang kết nối lại:",
                    error
                );

                this.emitConnectionState(
                    "reconnecting"
                );
            }
        );

        this.connection.onreconnected(
            connectionId => {
                console.log(
                    "SignalR đã kết nối lại:",
                    connectionId
                );

                this.emitConnectionState(
                    "connected"
                );
            }
        );

        this.connection.onclose(
            error => {
                if (error) {
                    console.error(
                        "SignalR đã đóng do lỗi:",
                        error
                    );
                }

                this.emitConnectionState(
                    "disconnected"
                );
            }
        );

        return this.connection;
    }
    cancelScheduledStop() {
        if (this.stopTimer === null) {
            return;
        }

        window.clearTimeout(
            this.stopTimer
        );

        this.stopTimer = null;
    }

    scheduleStop(delayMilliseconds = 200) {
        this.cancelScheduledStop();

        this.stopTimer =
            window.setTimeout(() => {
                this.stopTimer = null;

                this.stop().catch(error => {
                    console.warn(
                        "Không thể dừng SignalR:",
                        error
                    );
                });
            }, delayMilliseconds);
    }
    async start() {
        this.cancelScheduledStop();
        const token =
            authStore.getToken();

        if (!token) {
            return;
        }

        const connection =
            this.createConnection();

        if (
            connection.state ===
            HubConnectionState.Connected
        ) {
            this.emitConnectionState(
                "connected"
            );

            return;
        }

        if (
            connection.state ===
            HubConnectionState.Connecting
        ) {
            return (
                this.startPromise ||
                Promise.resolve()
            );
        }

        if (this.startPromise) {
            return this.startPromise;
        }

        this.emitConnectionState(
            "connecting"
        );

        this.startPromise =
            connection
                .start()
                .then(() => {
                    console.log(
                        "SignalR notification connected"
                    );

                    this.emitConnectionState(
                        "connected"
                    );
                })
                .catch(error => {
                    console.error(
                        "Không thể kết nối SignalR:",
                        error
                    );

                    this.emitConnectionState(
                        "disconnected"
                    );

                    throw error;
                })
                .finally(() => {
                    this.startPromise = null;
                    this.stopTimer = null;
                });

        return this.startPromise;
    }

    async stop() {
        this.cancelScheduledStop();

        if (!this.connection) {
            return;
        }

        /*
         * Nếu connection đang start thì chờ quá trình
         * start kết thúc trước khi yêu cầu stop.
         */
        if (this.startPromise) {
            try {
                await this.startPromise;
            }
            catch {
                /*
                 * Start thất bại thì connection thường
                 * đã về trạng thái Disconnected.
                 */
            }
        }

        if (
            this.connection.state ===
            HubConnectionState.Disconnected
        ) {
            this.emitConnectionState(
                "disconnected"
            );

            return;
        }

        try {
            await this.connection.stop();
        }
        catch (error) {
            console.error(
                "Không thể dừng SignalR:",
                error
            );
        }
        finally {
            this.emitConnectionState(
                "disconnected"
            );
        }
    }

    onReceiveNotification(handler) {
        this.receiveNotificationHandlers
            .add(handler);

        return () => {
            this.receiveNotificationHandlers
                .delete(handler);
        };
    }

    onUnreadCountChanged(handler) {
        this.unreadCountHandlers
            .add(handler);

        return () => {
            this.unreadCountHandlers
                .delete(handler);
        };
    }

    onConnectionStateChanged(handler) {
        this.connectionStateHandlers
            .add(handler);

        return () => {
            this.connectionStateHandlers
                .delete(handler);
        };
    }

    emitConnectionState(state) {
        this.connectionStateHandlers
            .forEach(handler => {
                handler(state);
            });
    }
}

export const notificationHub =
    new NotificationHubService();