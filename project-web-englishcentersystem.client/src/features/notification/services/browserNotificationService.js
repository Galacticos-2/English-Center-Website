const SOUND_ENABLED_KEY =
    "notification-sound-enabled";

const BROWSER_NOTIFICATION_ENABLED_KEY =
    "browser-notification-enabled";

let audioContext = null;

export const isBrowserNotificationSupported =
    () => {
        return (
            typeof window !== "undefined" &&
            "Notification" in window
        );
    };

export const getBrowserNotificationPermission =
    () => {
        if (
            !isBrowserNotificationSupported()
        ) {
            return "unsupported";
        }

        return window.Notification.permission;
    };

export const getSoundEnabled =
    () => {
        const storedValue =
            localStorage.getItem(
                SOUND_ENABLED_KEY
            );

        /*
         * Mặc định tắt để tránh website
         * tự phát âm khi user chưa chọn.
         */
        return storedValue === "true";
    };

export const saveSoundEnabled =
    enabled => {
        localStorage.setItem(
            SOUND_ENABLED_KEY,
            String(Boolean(enabled))
        );
    };

export const getBrowserNotificationEnabled =
    () => {
        if (
            !isBrowserNotificationSupported()
        ) {
            return false;
        }

        if (
            window.Notification
                .permission !==
            "granted"
        ) {
            return false;
        }

        return (
            localStorage.getItem(
                BROWSER_NOTIFICATION_ENABLED_KEY
            ) === "true"
        );
    };

export const saveBrowserNotificationEnabled =
    enabled => {
        localStorage.setItem(
            BROWSER_NOTIFICATION_ENABLED_KEY,
            String(Boolean(enabled))
        );
    };

export const requestBrowserNotificationPermission =
    async () => {
        if (
            !isBrowserNotificationSupported()
        ) {
            return "unsupported";
        }

        const permission =
            await window.Notification
                .requestPermission();

        const enabled =
            permission === "granted";

        saveBrowserNotificationEnabled(
            enabled
        );

        return permission;
    };

const getAudioContext =
    () => {
        if (
            typeof window === "undefined"
        ) {
            return null;
        }

        const AudioContextClass =
            window.AudioContext ||
            window.webkitAudioContext;

        if (!AudioContextClass) {
            return null;
        }

        if (!audioContext) {
            audioContext =
                new AudioContextClass();
        }

        return audioContext;
    };

export const unlockNotificationSound =
    async () => {
        try {
            const context =
                getAudioContext();

            if (!context) {
                return false;
            }

            if (
                context.state ===
                "suspended"
            ) {
                await context.resume();
            }

            return (
                context.state ===
                "running"
            );
        }
        catch (error) {
            console.warn(
                "Không thể mở khóa âm thanh:",
                error
            );

            return false;
        }
    };

export const playNotificationSound =
    async () => {
        if (!getSoundEnabled()) {
            return;
        }

        try {
            const context =
                getAudioContext();

            if (!context) {
                return;
            }

            if (
                context.state ===
                "suspended"
            ) {
                await context.resume();
            }

            if (
                context.state !==
                "running"
            ) {
                return;
            }

            const now =
                context.currentTime;

            const oscillator =
                context.createOscillator();

            const gain =
                context.createGain();

            oscillator.type =
                "sine";

            oscillator.frequency
                .setValueAtTime(
                    880,
                    now
                );

            oscillator.frequency
                .exponentialRampToValueAtTime(
                    660,
                    now + 0.18
                );

            gain.gain.setValueAtTime(
                0.0001,
                now
            );

            gain.gain
                .exponentialRampToValueAtTime(
                    0.12,
                    now + 0.02
                );

            gain.gain
                .exponentialRampToValueAtTime(
                    0.0001,
                    now + 0.24
                );

            oscillator.connect(gain);
            gain.connect(
                context.destination
            );

            oscillator.start(now);
            oscillator.stop(
                now + 0.25
            );

            oscillator.onended =
                () => {
                    oscillator.disconnect();
                    gain.disconnect();
                };
        }
        catch (error) {
            /*
             * Không để lỗi âm thanh làm hỏng
             * luồng notification chính.
             */
            console.warn(
                "Không thể phát âm thông báo:",
                error
            );
        }
    };

export const showBrowserNotification =
    notification => {
        if (
            !isBrowserNotificationSupported()
        ) {
            return null;
        }

        if (
            !getBrowserNotificationEnabled()
        ) {
            return null;
        }

        if (
            window.Notification.permission !==
            "granted"
        ) {
            return null;
        }

        const userNotificationId =
            notification
                ?.userNotificationId;

        const browserNotification =
            new window.Notification(
                notification?.title ||
                "New notification",
                {
                    body:
                        notification
                            ?.message ||
                        "",

                    icon:
                        "/logo.png",

                    badge:
                        "/logo.png",

                    tag:
                        userNotificationId
                            ? `notification-${userNotificationId}`
                            : undefined,

                    renotify:
                        false
                }
            );

        browserNotification.onclick =
            () => {
                window.focus();

                const actionUrl =
                    notification
                        ?.actionUrl;

                if (actionUrl) {
                    window.location.assign(
                        actionUrl
                    );
                }

                browserNotification.close();
            };

        return browserNotification;
    };
