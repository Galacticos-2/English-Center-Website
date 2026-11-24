import {
    useCallback,
    useEffect,
    useMemo,
    useRef,
    useState
} from "react";
import {
    formatVietnamDateTime
} from "../../../utils/dateTimeUtils";
import {
    Bell,
    BookOpen,
    ClipboardList,
    FileText,
    LoaderCircle,
    Search,
    Trophy,
    CheckCheck,
    Trash2
} from "lucide-react";

import {
    useNavigate
} from "react-router-dom";

import {
    deleteManyNotificationsApi,
    deleteNotificationApi,
    getMyNotificationsApi,
    markManyNotificationsAsReadApi
} from "../api/notificationApi";

import {
    useNotification
} from "../hooks/useNotification";

import NotificationSkeleton
    from "../components/NotificationSkeleton";

import "../styles/notificationCenter.css";

const PAGE_SIZE = 20;

const notificationTypes = [
    {
        value: "",
        label: "All types"
    },
    {
        value: "1",
        label: "Lessons"
    },
    {
        value: "2",
        label: "Exercises"
    },
    {
        value: "3",
        label: "Tests"
    },
    {
        value: "4",
        label: "Competitions"
    },
    {
        value: "5",
        label: "Teachers without classes"
    },
    {
        value: "6",
        label: "Classes without teachers"
    },
    {
        value: "7",
        label: "Students without classes"
    },
    {
        value: "8",
        label: "Classes at capacity"
    }
];

const readStatuses = [
    {
        value: "all",
        label: "All statuses"
    },
    {
        value: "unread",
        label: "Unread"
    },
    {
        value: "read",
        label: "Read"
    }
];

const resolveType =
    notification => {
        return (
            notification.typeName ||
            notification.type
        );
    };
const normalizeNotificationType =
    value => {
        if (
            value === 1 ||
            value === "1" ||
            value === "LessonCreated"
        ) {
            return 1;
        }

        if (
            value === 2 ||
            value === "2" ||
            value === "ExerciseAssigned"
        ) {
            return 2;
        }

        if (
            value === 3 ||
            value === "3" ||
            value === "TestAssigned"
        ) {
            return 3;
        }

        if (
            value === 4 ||
            value === "4" ||
            value === "CompetitionAssigned"
        ) {
            return 4;
        }
        if (
            value === 5 ||
            value === "5" ||
            value ===
            "AdminTeacherUnassigned"
        ) {
            return 5;
        }

        if (
            value === 6 ||
            value === "6" ||
            value ===
            "AdminClassWithoutTeacher"
        ) {
            return 6;
        }

        if (
            value === 7 ||
            value === "7" ||
            value ===
            "AdminStudentUnassigned"
        ) {
            return 7;
        }

        if (
            value === 8 ||
            value === "8" ||
            value ===
            "AdminClassAtCapacity"
        ) {
            return 8;
        }

        return null;
    };
const getNotificationIcon =
    type => {
        const normalized =
            typeof type === "string"
                ? type
                : Number(type);

        switch (normalized) {
            case 1:
            case "1":
            case "LessonCreated":
                return (
                    <BookOpen size={22} />
                );

            case 2:
            case "2":
            case "ExerciseAssigned":
                return (
                    <ClipboardList
                        size={22}
                    />
                );

            case 3:
            case "3":
            case "TestAssigned":
                return (
                    <FileText size={22} />
                );

            case 4:
            case "4":
            case "CompetitionAssigned":
                return (
                    <Trophy size={22} />
                );

            default:
                return (
                    <Bell size={22} />
                );
        }
    };

const getNotificationTypeClass =
    type => {
        const normalized =
            typeof type === "string"
                ? type
                : Number(type);

        switch (normalized) {
            case 1:
            case "1":
            case "LessonCreated":
                return "lesson";

            case 2:
            case "2":
            case "ExerciseAssigned":
                return "exercise";

            case 3:
            case "3":
            case "TestAssigned":
                return "test";

            case 4:
            case "4":
            case "CompetitionAssigned":
                return "competition";

            default:
                return "default";
        }
    };

const getAdminNotificationContent = notification => {
    const type = normalizeNotificationType(
        resolveType(notification)
    );

    const message = String(notification.message || "");

    if (type === 5) {
        const name = message
            .replace(/^Giáo viên\s+/i, "")
            .replace(/\s+hiện chưa được phân lớp\.?$/i, "")
            .trim();

        return {
            title: "Teacher has no assigned class",
            message: name
                ? `Teacher ${name} has not been assigned to any class yet.`
                : "This teacher has not been assigned to any class yet."
        };
    }

    if (type === 6) {
        const name = message
            .replace(/^Lớp\s+/i, "")
            .replace(/\s+hiện chưa có giáo viên phụ trách\.?$/i, "")
            .trim();

        return {
            title: "Class has no assigned teacher",
            message: name
                ? `Class ${name} does not have an assigned teacher yet.`
                : "This class does not have an assigned teacher yet."
        };
    }

    if (type === 7) {
        const name = message
            .replace(/^Học viên\s+/i, "")
            .replace(/\s+hiện chưa được xếp vào lớp học\.?$/i, "")
            .trim();

        return {
            title: "Student has no assigned class",
            message: name
                ? `Student ${name} has not been assigned to a class yet.`
                : "This student has not been assigned to a class yet."
        };
    }

    if (type === 8) {
        return {
            title: "Class has reached maximum capacity",
            message
        };
    }

    return {
        title: notification.title,
        message
    };
};



const mergeNotifications = (
    currentItems,
    incomingItems
) => {
    const existedIds =
        new Set(
            currentItems.map(item =>
                Number(
                    item.userNotificationId
                )
            )
        );
    
    const newItems =
        incomingItems.filter(item =>
            !existedIds.has(
                Number(
                    item.userNotificationId
                )
            )
        );

    return [
        ...currentItems,
        ...newItems
    ];
};

function NotificationCenterPage() {
    const navigate =
        useNavigate();

    const {
        notifications:
        realtimeNotifications,

        markAsRead,
        reloadNotifications,

        soundEnabled,

        browserNotificationEnabled,

        browserNotificationPermission,

        browserNotificationSupported,

        setNotificationSoundEnabled,

        enableBrowserNotifications,

        disableBrowserNotifications
    } = useNotification();

    const [items, setItems] =
        useState([]);

    const [page, setPage] =
        useState(1);

    const [totalItems, setTotalItems] =
        useState(0);

  
    const [unreadCount, setUnreadCount] =
        useState(0);

    const [hasNextPage, setHasNextPage] =
        useState(false);

    const [initialLoading, setInitialLoading] =
        useState(true);

    const [loadingMore, setLoadingMore] =
        useState(false);

    const [searchInput, setSearchInput] =
        useState("");

    const [search, setSearch] =
        useState("");

    const [selectedType, setSelectedType] =
        useState("");

    const [readStatus, setReadStatus] =
        useState("all");
    const [
        selectedIds,
        setSelectedIds
    ] = useState([]);

    const [
        bulkActionLoading,
        setBulkActionLoading
    ] = useState(false);
    const [
        notificationSettingLoading,
        setNotificationSettingLoading
    ] = useState(false);
    const loadMoreRef =
        useRef(null);

    const observerRef =
        useRef(null);

    const requestVersionRef =
        useRef(0);

    const loadingMoreRef =
        useRef(false);

    const hasNextPageRef =
        useRef(false);

    const pageRef =
        useRef(1);
    const lastRealtimeNotificationIdRef =
        useRef(null);
    const resolvedIsRead =
        useMemo(() => {
            if (
                readStatus === "unread"
            ) {
                return false;
            }

            if (
                readStatus === "read"
            ) {
                return true;
            }

            return null;
        }, [readStatus]);
    const selectedIdSet =
        useMemo(
            () =>
                new Set(
                    selectedIds.map(
                        Number
                    )
                ),
            [selectedIds]
        );

    const allVisibleSelected =
        items.length > 0 &&
        items.every(item =>
            selectedIdSet.has(
                Number(
                    item.userNotificationId
                )
            )
        );

    useEffect(() => {
        loadingMoreRef.current =
            loadingMore;
    }, [loadingMore]);

    useEffect(() => {
        hasNextPageRef.current =
            hasNextPage;
    }, [hasNextPage]);

    useEffect(() => {
        pageRef.current =
            page;
    }, [page]);

    const loadNotifications =
        useCallback(
            async (
                targetPage,
                append = false
            ) => {
                /*
                 * Không cho phép hai request
                 * load-more chạy đồng thời.
                 */
                if (
                    append &&
                    loadingMoreRef.current
                ) {
                    return;
                }

                const requestVersion =
                    ++requestVersionRef.current;

                if (append) {
                    loadingMoreRef.current =
                        true;

                    setLoadingMore(true);
                }
                else {
                    setInitialLoading(true);
                }

                try {
                    const data =
                        await getMyNotificationsApi({
                            page:
                                targetPage,

                            pageSize:
                                PAGE_SIZE,

                            search,

                            isRead:
                                resolvedIsRead,

                            type:
                                selectedType ===
                                    ""
                                    ? null
                                    : Number(
                                        selectedType
                                    )
                        });

                    /*
                     * Bỏ qua response cũ khi user
                     * vừa đổi search hoặc filter.
                     */
                    if (
                        requestVersion !==
                        requestVersionRef.current
                    ) {
                        return;
                    }

                    const incomingItems =
                        Array.isArray(
                            data?.items
                        )
                            ? data.items
                            : [];

                    if (append) {
                        setItems(current =>
                            mergeNotifications(
                                current,
                                incomingItems
                            )
                        );
                    }
                    else {
                        setItems(
                            incomingItems
                        );
                    }

                    const responsePage =
                        Number(
                            data?.page ||
                            targetPage
                        );

                    const responseTotalItems =
                        Number(
                            data?.totalItems ||
                            0
                        );

                    const responseTotalPages =
                        Number(
                            data?.totalPages ||
                            0
                        );

                    const responseUnreadCount =
                        Number(
                            data?.unreadCount ||
                            0
                        );

                    const responseHasNextPage =
                        typeof data
                            ?.hasNextPage ===
                            "boolean"
                            ? data
                                .hasNextPage
                            : responsePage <
                            responseTotalPages;

                    pageRef.current =
                        responsePage;

                    hasNextPageRef.current =
                        responseHasNextPage;

                    setPage(
                        responsePage
                    );

                    setTotalItems(
                        responseTotalItems
                    );

                   

                    setUnreadCount(
                        responseUnreadCount
                    );

                    setHasNextPage(
                        responseHasNextPage
                    );
                }
                catch (error) {
                    if (
                        requestVersion !==
                        requestVersionRef.current
                    ) {
                        return;
                    }

                    console.error(
                        "Unable to load notifications:",
                        error.response?.data ||
                        error
                    );

                    /*
                     * Khi load trang đầu lỗi,
                     * xóa dữ liệu cũ để tránh
                     * hiển thị sai filter.
                     *
                     * Khi load-more lỗi, giữ lại
                     * danh sách đã tải.
                     */
                    if (!append) {
                        setItems([]);
                        setTotalItems(0);
                   
                        setHasNextPage(false);

                        hasNextPageRef.current =
                            false;
                    }
                }
                finally {
                    if (
                        requestVersion ===
                        requestVersionRef.current
                    ) {
                        setInitialLoading(false);
                        setLoadingMore(false);

                        loadingMoreRef.current =
                            false;
                    }
                }
            },
            [
                search,
                selectedType,
                resolvedIsRead
            ]
        );

    /*
     * Debounce search.
     */
    useEffect(() => {
        const timerId =
            window.setTimeout(() => {
                setSearch(
                    searchInput.trim()
                );
            }, 400);

        return () => {
            window.clearTimeout(
                timerId
            );
        };
    }, [searchInput]);

    /*
     * Khi search hoặc filter thay đổi:
     *
     * - hủy hiệu lực request cũ;
     * - xóa danh sách hiện tại;
     * - quay về trang 1;
     * - tải dữ liệu mới.
     */
    useEffect(() => {
        requestVersionRef.current += 1;

        pageRef.current = 1;
        hasNextPageRef.current = false;
        loadingMoreRef.current = false;

        setPage(1);
        setItems([]);
        setSelectedIds([]);
        setTotalItems(0);
       
        setHasNextPage(false);
        setLoadingMore(false);

        loadNotifications(
            1,
            false
        );
    }, [
        search,
        selectedType,
        resolvedIsRead,
        loadNotifications
    ]);

    /*
     * IntersectionObserver:
     *
     * Khi sentinel gần xuất hiện trong viewport,
     * tải trang tiếp theo.
     */
    useEffect(() => {
        const target =
            loadMoreRef.current;

        if (!target) {
            return;
        }

        if (
            observerRef.current
        ) {
            observerRef.current
                .disconnect();
        }

        observerRef.current =
            new IntersectionObserver(
                entries => {
                    const firstEntry =
                        entries[0];

                    if (
                        !firstEntry
                            .isIntersecting
                    ) {
                        return;
                    }

                    if (
                        !hasNextPageRef
                            .current ||
                        loadingMoreRef
                            .current ||
                        initialLoading
                    ) {
                        return;
                    }

                    const nextPage =
                        pageRef.current +
                        1;

                    loadNotifications(
                        nextPage,
                        true
                    );
                },
                {
                    root: null,

                    /*
                     * Tải sớm khi người dùng còn
                     * cách cuối danh sách 250px.
                     */
                    rootMargin:
                        "0px 0px 250px 0px",

                    threshold: 0
                }
            );

        observerRef.current
            .observe(target);

        return () => {
            observerRef.current
                ?.disconnect();
        };
    }, [
        loadNotifications,
        initialLoading
    ]);

    const handleTypeChange =
        event => {
            setSelectedType(
                event.target.value
            );
        };
    const handleSoundSettingChange =
        async event => {
            const enabled =
                event.target.checked;

            setNotificationSettingLoading(
                true
            );

            try {
                const result =
                    await setNotificationSoundEnabled(
                        enabled
                    );

                if (!result?.success) {
                    alert(
                        result?.message ||
                        "Unable to change the notification sound setting."
                    );
                }
            }
            catch (error) {
                console.error(
                    "Unable to change notification sound:",
                    error
                );

                alert(
                    "Unable to change the notification sound setting."
                );
            }
            finally {
                setNotificationSettingLoading(
                    false
                );
            }
        };
    const handleBrowserNotificationChange =
        async event => {
            const enabled =
                event.target.checked;

            setNotificationSettingLoading(
                true
            );

            try {
                if (!enabled) {
                    disableBrowserNotifications();
                    return;
                }

                const result =
                    await enableBrowserNotifications();

                if (!result?.success) {
                    alert(
                        result?.message ||
                        "Unable to enable browser notifications."
                    );
                }
            }
            catch (error) {
                console.error(
                    "Unable to change browser notification settings:",
                    error
                );

                alert(
                    "Unable to enable browser notifications."
                );
            }
            finally {
                setNotificationSettingLoading(
                    false
                );
            }
        };
    const reloadCenterFromFirstPage =
        useCallback(async () => {
            requestVersionRef.current += 1;

            pageRef.current = 1;
            hasNextPageRef.current = false;
            loadingMoreRef.current = false;

            setPage(1);
            setItems([]);
            setSelectedIds([]);
            setTotalItems(0);
            
            setHasNextPage(false);
            setLoadingMore(false);

            await loadNotifications(
                1,
                false
            );
        }, [loadNotifications]);
    const handleReadStatusChange =
        event => {
            setReadStatus(
                event.target.value
            );
        };
    const toggleSelectedNotification =
        userNotificationId => {
            const normalizedId =
                Number(
                    userNotificationId
                );

            setSelectedIds(current => {
                const existed =
                    current.some(
                        id =>
                            Number(id) ===
                            normalizedId
                    );

                if (existed) {
                    return current.filter(
                        id =>
                            Number(id) !==
                            normalizedId
                    );
                }

                return [
                    ...current,
                    normalizedId
                ];
            });
        };
    const toggleSelectAllVisible =
        () => {
            const visibleIds =
                items.map(item =>
                    Number(
                        item.userNotificationId
                    )
                );

            if (allVisibleSelected) {
                setSelectedIds(current =>
                    current.filter(
                        id =>
                            !visibleIds.includes(
                                Number(id)
                            )
                    )
                );

                return;
            }

            setSelectedIds(current =>
                Array.from(
                    new Set([
                        ...current.map(Number),
                        ...visibleIds
                    ])
                )
            );
        };
    const handleMarkSelectedAsRead =
        async () => {
            if (
                selectedIds.length === 0 ||
                bulkActionLoading
            ) {
                return;
            }

            const selectedUnreadIds =
                items
                    .filter(item =>
                        selectedIdSet.has(
                            Number(
                                item
                                    .userNotificationId
                            )
                        ) &&
                        !item.isRead
                    )
                    .map(item =>
                        Number(
                            item
                                .userNotificationId
                        )
                    );

            if (
                selectedUnreadIds.length ===
                0
            ) {
                setSelectedIds([]);
                return;
            }

            setBulkActionLoading(true);

            try {
                await markManyNotificationsAsReadApi(
                    selectedUnreadIds
                );

                /*
                 * Khi đang lọc chưa đọc,
                 * kết quả phân trang thay đổi.
                 * Reset lại từ page 1 để tránh
                 * thiếu item do offset bị dịch.
                 */
                if (
                    readStatus === "unread"
                ) {
                    await reloadCenterFromFirstPage();
                }
                else {
                    const readAt =
                        new Date()
                            .toISOString();

                    setItems(current =>
                        current.map(item =>
                            selectedUnreadIds.includes(
                                Number(
                                    item
                                        .userNotificationId
                                )
                            )
                                ? {
                                    ...item,
                                    isRead: true,
                                    readAt
                                }
                                : item
                        )
                    );

                    setUnreadCount(current =>
                        Math.max(
                            0,
                            current -
                            selectedUnreadIds
                                .length
                        )
                    );

                    setSelectedIds([]);
                }

                /*
                 * Đồng bộ dropdown và badge.
                 */
                await reloadNotifications();
            }
            catch (error) {
                console.error(
                    "Unable to mark selected notifications as read:",
                    error.response?.data ||
                    error
                );

                alert(
                    error.response?.data
                        ?.message ||
                    "Unable to mark selected notifications as read."
                );
            }
            finally {
                setBulkActionLoading(
                    false
                );
            }
        };
    const handleDeleteOne =
        async (
            event,
            userNotificationId
        ) => {
            event.stopPropagation();

            if (bulkActionLoading) {
                return;
            }

            const confirmed =
                window.confirm(
                    "Are you sure you want to delete this notification?"
                );

            if (!confirmed) {
                return;
            }

            setBulkActionLoading(true);

            try {
                await deleteNotificationApi(
                    userNotificationId
                );

                /*
                 * Xóa làm thay đổi offset của
                 * các page đã tải nên reset lại.
                 */
                await reloadCenterFromFirstPage();

                await reloadNotifications();
            }
            catch (error) {
                console.error(
                    "Unable to delete notification:",
                    error.response?.data ||
                    error
                );

                alert(
                    error.response?.data
                        ?.message ||
                    "Unable to delete notification."
                );
            }
            finally {
                setBulkActionLoading(
                    false
                );
            }
        };
    const handleDeleteSelected =
        async () => {
            if (
                selectedIds.length === 0 ||
                bulkActionLoading
            ) {
                return;
            }

            const confirmed =
                window.confirm(
                    `Are you sure you want to delete ${selectedIds.length} selected notifications?`
                );

            if (!confirmed) {
                return;
            }

            setBulkActionLoading(true);

            try {
                await deleteManyNotificationsApi(
                    selectedIds
                );

                await reloadCenterFromFirstPage();

                await reloadNotifications();
            }
            catch (error) {
                console.error(
                    "Unable to delete notifications:",
                    error.response?.data ||
                    error
                );

                alert(
                    error.response?.data
                        ?.message ||
                    "Unable to delete notifications."
                );
            }
            finally {
                setBulkActionLoading(
                    false
                );
            }
        };
    const handleNotificationClick =
        async notification => {
            const userNotificationId =
                Number(
                    notification
                        .userNotificationId
                );

            if (
                !notification.isRead
            ) {
                try {
                    await markAsRead(
                        userNotificationId
                    );

                    /*
                     * Nếu đang lọc Chưa đọc,
                     * item vừa đọc phải biến mất.
                     */
                    if (
                        readStatus ===
                        "unread"
                    ) {
                        setItems(current =>
                            current.filter(
                                item =>
                                    Number(
                                        item
                                            .userNotificationId
                                    ) !==
                                    userNotificationId
                            )
                        );

                        setTotalItems(
                            current => {
                                const next =
                                    Math.max(
                                        0,
                                        current -
                                        1
                                    );

                     

                                return next;
                            }
                        );
                    }
                    else {
                        setItems(current =>
                            current.map(
                                item =>
                                    Number(
                                        item
                                            .userNotificationId
                                    ) ===
                                        userNotificationId
                                        ? {
                                            ...item,

                                            isRead:
                                                true,

                                            readAt:
                                                new Date()
                                                    .toISOString()
                                        }
                                        : item
                            )
                        );
                    }

                    setUnreadCount(
                        current =>
                            Math.max(
                                0,
                                current - 1
                            )
                    );

                    await reloadNotifications();
                }
                catch (error) {
                    console.error(
                        "Unable to mark notification as read:",
                        error
                    );
                }
            }

            if (
                notification.actionUrl
            ) {
                navigate(
                    notification.actionUrl
                );
            }
        };
    const matchesCurrentFilter =
        useCallback(
            notification => {
                if (!notification) {
                    return false;
                }

                if (
                    selectedType !== ""
                ) {
                    const incomingType =
                        normalizeNotificationType(
                            notification.type ??
                            notification.typeName
                        );

                    if (
                        incomingType !==
                        Number(selectedType)
                    ) {
                        return false;
                    }
                }

                if (
                    resolvedIsRead !== null &&
                    Boolean(
                        notification.isRead
                    ) !==
                    resolvedIsRead
                ) {
                    return false;
                }

                if (search) {
                    const keyword =
                        search.toLowerCase();

                    const searchableText =
                        `${notification.title ||
                            ""
                            } ${notification.message ||
                            ""
                            }`
                            .toLowerCase();

                    if (
                        !searchableText.includes(
                            keyword
                        )
                    ) {
                        return false;
                    }
                }

                return true;
            },
            [
                selectedType,
                resolvedIsRead,
                search
            ]
        );
    useEffect(() => {
        const incoming =
            realtimeNotifications?.[0];

        if (!incoming) {
            return;
        }

        const incomingId =
            Number(
                incoming
                    .userNotificationId
            );

        if (
            !Number.isFinite(
                incomingId
            ) ||
            incomingId <= 0
        ) {
            return;
        }

        if (
            lastRealtimeNotificationIdRef
                .current ===
            incomingId
        ) {
            return;
        }

        lastRealtimeNotificationIdRef
            .current =
            incomingId;

        if (
            !matchesCurrentFilter(
                incoming
            )
        ) {
            return;
        }

        setItems(current => {
            const existed =
                current.some(item =>
                    Number(
                        item
                            .userNotificationId
                    ) ===
                    incomingId
                );

            if (existed) {
                return current;
            }

            setTotalItems(total =>
                total + 1
            );

            /*
             * unreadCount trên page đang là state
             * riêng nên cập nhật ngay để UI phản hồi.
             */
            if (!incoming.isRead) {
                setUnreadCount(count =>
                    count + 1
                );
            }

            return [
                incoming,
                ...current
            ];
        });
    }, [
        realtimeNotifications,
        matchesCurrentFilter
    ]);
    
    
    const showEmptyState =
        !initialLoading &&
        items.length === 0;

    const showEndMessage =
        !initialLoading &&
        !loadingMore &&
        items.length > 0 &&
        !hasNextPage;

    return (
        <div className="notification-center-page">
            <header className="notification-center-header">
                <div>
                    <span className="notification-center-eyebrow">
                        Notification Center
                    </span>

                    <h1>
                        Notifications
                    </h1>

                    <p>
                        Track new lessons,
                        exercises, tests, and
                        competitions.
                    </p>
                </div>

                <div className="notification-center-summary">
                    <div>
                        <span>
                            Total
                        </span>

                        <strong>
                            {totalItems}
                        </strong>
                    </div>

                    <div>
                        <span>
                            Unread
                        </span>

                        <strong>
                            {unreadCount}
                        </strong>
                    </div>
                </div>
            </header>
            <section className="notification-preference-panel">
                <div className="notification-preference-heading">
                    <h2>
                        Notification Settings
                    </h2>

                    <p>
                        Choose how to receive real-time
                        notifications on this device.
                    </p>
                </div>

                <div className="notification-preference-options">
                    <label className="notification-preference-item">
                        <div>
                            <strong>
                                Notification Sound
                            </strong>

                            <span>
                                Play a sound when a new
                                notification arrives.
                            </span>
                        </div>

                        <input
                            type="checkbox"
                            checked={
                                Boolean(
                                    soundEnabled
                                )
                            }
                            disabled={
                                notificationSettingLoading
                            }
                            onChange={
                                handleSoundSettingChange
                            }
                        />
                    </label>

                    <label className="notification-preference-item">
                        <div>
                            <strong>
                                Browser Notifications
                            </strong>

                            <span>
                                Show system notifications
                                while you are on another tab.
                            </span>

                            {!browserNotificationSupported && (
                                <small className="notification-setting-error">
                                    This browser does not support
                                    system notifications.
                                </small>
                            )}

                            {browserNotificationPermission ===
                                "denied" && (
                                    <small className="notification-setting-error">
                                        Permission is blocked.
                                        Open Site Settings to
                                        enable notification permission.
                                    </small>
                                )}
                        </div>

                        <input
                            type="checkbox"
                            checked={
                                Boolean(
                                    browserNotificationEnabled
                                )
                            }
                            disabled={
                                notificationSettingLoading ||
                                !browserNotificationSupported ||
                                browserNotificationPermission ===
                                "denied"
                            }
                            onChange={
                                handleBrowserNotificationChange
                            }
                        />
                    </label>
                </div>
            </section>
            <section className="notification-center-toolbar">
                <div className="notification-center-search">
                    <Search size={18} />

                    <input
                        type="text"
                        value={searchInput}
                        placeholder="Search by title or content..."
                        onChange={event =>
                            setSearchInput(
                                event.target
                                    .value
                            )
                        }
                    />
                </div>

                <select
                    value={selectedType}
                    onChange={
                        handleTypeChange
                    }
                    aria-label="Filter by notification type"
                >
                    {notificationTypes.map(
                        item => (
                            <option
                                key={
                                    item.value ||
                                    "all"
                                }
                                value={
                                    item.value
                                }
                            >
                                {item.label}
                            </option>
                        )
                    )}
                </select>

                <select
                    value={readStatus}
                    onChange={
                        handleReadStatusChange
                    }
                    aria-label="Filter by read status"
                >
                    {readStatuses.map(
                        item => (
                            <option
                                key={
                                    item.value
                                }
                                value={
                                    item.value
                                }
                            >
                                {item.label}
                            </option>
                        )
                    )}
                </select>
            </section>
            {items.length > 0 && (
                <div className="notification-selection-row">
                    <label className="notification-select-all">
                        <input
                            type="checkbox"
                            checked={
                                allVisibleSelected
                            }
                            onChange={
                                toggleSelectAllVisible
                            }
                            disabled={
                                bulkActionLoading
                            }
                        />

                        <span>
                            Select all visible
                        </span>
                    </label>

                    <span className="notification-loaded-count">
                        Loaded {items.length}
                        {" / "}
                        {totalItems} notifications
                    </span>
                </div>
            )}
            {selectedIds.length > 0 && (
                <div className="notification-bulk-toolbar">
                    <strong>
                        Selected{" "}
                        {selectedIds.length}
                        {" "}
                        notifications
                    </strong>

                    <div className="notification-bulk-actions">
                        <button
                            type="button"
                            onClick={
                                handleMarkSelectedAsRead
                            }
                            disabled={
                                bulkActionLoading
                            }
                        >
                            <CheckCheck
                                size={17}
                            />

                            Mark as read
                        </button>

                        <button
                            type="button"
                            className="danger"
                            onClick={
                                handleDeleteSelected
                            }
                            disabled={
                                bulkActionLoading
                            }
                        >
                            <Trash2
                                size={17}
                            />

                            Delete selected
                        </button>
                    </div>
                </div>
            )}
            {initialLoading &&
                items.length === 0 && (
                    <NotificationSkeleton
                        count={6}
                    />
                )}

            {showEmptyState && (
                <div className="notification-center-empty">
                    <div className="notification-center-empty-icon">
                        <Bell size={38} />
                    </div>

                    <h2>
                        No notifications found
                    </h2>

                    <p>
                        Try changing the search keyword
                        or current filters.
                    </p>
                </div>
            )}

            {items.length > 0 && (
                <section className="notification-center-list">
                    {items.map(
                        notification => {
                            const type =
                                resolveType(
                                    notification
                                );
                            const content =
                                getAdminNotificationContent(
                                    notification
                                );

                            return (
                                <article
                                    key={
                                        notification
                                            .userNotificationId
                                    }
                                    className={
                                        notification.isRead
                                            ? "notification-center-item"
                                            : "notification-center-item unread"
                                    }
                                    onClick={() =>
                                        handleNotificationClick(
                                            notification
                                        )
                                    }
                                >
                                    <div
                                        className="notification-item-checkbox"
                                        onClick={event =>
                                            event.stopPropagation()
                                        }
                                    >
                                        <input
                                            type="checkbox"
                                            checked={
                                                selectedIdSet.has(
                                                    Number(
                                                        notification
                                                            .userNotificationId
                                                    )
                                                )
                                            }
                                            onChange={() =>
                                                toggleSelectedNotification(
                                                    notification
                                                        .userNotificationId
                                                )
                                            }
                                            disabled={
                                                bulkActionLoading
                                            }
                                            aria-label={
                                                `Select notification ${content.title}`
                                            }
                                        />
                                    </div>

                                    <div
                                        className={
                                            `notification-center-item-icon ` +
                                            getNotificationTypeClass(
                                                type
                                            )
                                        }
                                    >
                                        {getNotificationIcon(
                                            type
                                        )}
                                    </div>

                                    <div className="notification-center-item-content">
                                        <div className="notification-center-item-title">
                                            <h3>
                                                {
                                                    content.title
                                                }
                                            </h3>

                                            {!notification
                                                .isRead && (
                                                    <span className="notification-center-unread-dot" />
                                                )}
                                        </div>

                                        <p>
                                            {
                                                    content.message
                                            }
                                        </p>

                                        <div className="notification-center-item-meta">
                                            <span>
                                                {formatVietnamDateTime(
                                                    notification.createdAt
                                                )}
                                            </span>

                                            {notification
                                                .classRoomName && (
                                                    <>
                                                        <span>
                                                            ·
                                                        </span>

                                                        <span>
                                                            {
                                                                notification
                                                                    .classRoomName
                                                            }
                                                        </span>
                                                    </>
                                                )}

                                            {notification
                                                .createdByName && (
                                                    <>
                                                        <span>
                                                            ·
                                                        </span>

                                                        <span>
                                                            {
                                                                notification
                                                                    .createdByName
                                                            }
                                                        </span>
                                                    </>
                                                )}
                                        </div>
                                    </div>

                                    <button
                                        type="button"
                                        className="notification-item-delete-btn"
                                        title="Delete notification"
                                        aria-label="Delete notification"
                                        disabled={
                                            bulkActionLoading
                                        }
                                        onClick={event =>
                                            handleDeleteOne(
                                                event,
                                                notification
                                                    .userNotificationId
                                            )
                                        }
                                    >
                                        <Trash2 size={18} />
                                    </button>
                                </article>
                            );
                        }
                    )}
                </section>
            )}

            {/*
             * Sentinel luôn nằm sau danh sách.
             * Observer theo dõi phần tử này.
             */}
            <div
                ref={loadMoreRef}
                className="notification-load-more-sentinel"
                aria-hidden="true"
            />

            {loadingMore && (
                <div className="notification-bottom-loader">
                    <LoaderCircle
                        size={21}
                        className="notification-loader-icon"
                    />

                    <span>
                        Loading more notifications...
                    </span>
                </div>
            )}

            {showEndMessage && (
                <div className="notification-end-message">
                    All notifications are displayed.
                </div>
            )}
        </div>
    );
}

export default NotificationCenterPage;
