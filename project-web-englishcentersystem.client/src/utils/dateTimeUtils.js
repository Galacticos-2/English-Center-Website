export const normalizeUtcValue = value => {
    if (!value) return null;

    if (value instanceof Date) {
        return value;
    }

    if (typeof value === "string") {
        const normalizedValue = value.trim();

        const hasTimeZone =
            normalizedValue.endsWith("Z") ||
            /[+-]\d{2}:\d{2}$/.test(
                normalizedValue
            );

        return hasTimeZone
            ? normalizedValue
            : `${normalizedValue}Z`;
    }

    return value;
};
export const parseUtcDate = value => {
    const normalizedValue =
        normalizeUtcValue(value);

    if (!normalizedValue) {
        return null;
    }

    const date =
        normalizedValue instanceof Date
            ? normalizedValue
            : new Date(normalizedValue);

    return Number.isNaN(
        date.getTime()
    )
        ? null
        : date;
};
export const formatVietnamDateTime = (value) => {
    const utcValue = normalizeUtcValue(value);

    if (!utcValue) return "";

    return new Date(utcValue).toLocaleString("vi-VN", {
        timeZone: "Asia/Ho_Chi_Minh",
        hour12: false,
        year: "numeric",
        month: "2-digit",
        day: "2-digit",
        hour: "2-digit",
        minute: "2-digit"
    });
};

export const formatVietnamDate = (value) => {
    const utcValue = normalizeUtcValue(value);

    if (!utcValue) return "";

    return new Date(utcValue).toLocaleDateString("vi-VN", {
        timeZone: "Asia/Ho_Chi_Minh",
        year: "numeric",
        month: "2-digit",
        day: "2-digit"
    });
};
export const formatVietnamDateOnly = value => {
    const date = parseUtcDate(value);

    if (!date) {
        return "";
    }

    return new Intl.DateTimeFormat("vi-VN", {
        timeZone: "Asia/Ho_Chi_Minh",
        day: "2-digit",
        month: "2-digit",
        year: "numeric"
    }).format(date);
};

export const formatVietnamHourMinute = value => {
    const date = parseUtcDate(value);

    if (!date) {
        return "";
    }

    return new Intl.DateTimeFormat("vi-VN", {
        timeZone: "Asia/Ho_Chi_Minh",
        hour: "2-digit",
        minute: "2-digit",
        hour12: false
    }).format(date);
};
export const formatVietnamDateTimeWithSeconds = value => {
    const date = parseUtcDate(value);

    if (!date) {
        return "";
    }

    return new Intl.DateTimeFormat("vi-VN", {
        timeZone: "Asia/Ho_Chi_Minh",
        day: "2-digit",
        month: "2-digit",
        year: "numeric",
        hour: "2-digit",
        minute: "2-digit",
        second: "2-digit",
        hour12: false
    }).format(date);
};
export const formatVietnamTime = (value) => {
    const utcValue = normalizeUtcValue(value);

    if (!utcValue) return "";

    return new Date(utcValue).toLocaleTimeString("vi-VN", {
        timeZone: "Asia/Ho_Chi_Minh",
        hour12: false,
        hour: "2-digit",
        minute: "2-digit"
    });
};