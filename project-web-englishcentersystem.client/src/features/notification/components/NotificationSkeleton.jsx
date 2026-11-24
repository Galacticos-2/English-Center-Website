function NotificationSkeleton({
    count = 6
}) {
    return (
        <div className="notification-skeleton-list">
            {Array.from({
                length: count
            }).map((_, index) => (
                <div
                    key={index}
                    className="notification-skeleton-card"
                >
                    <div className="notification-skeleton-icon" />

                    <div className="notification-skeleton-content">
                        <div className="notification-skeleton-title" />

                        <div className="notification-skeleton-line" />

                        <div className="notification-skeleton-line medium" />

                        <div className="notification-skeleton-line short" />
                    </div>
                </div>
            ))}
        </div>
    );
}

export default NotificationSkeleton;