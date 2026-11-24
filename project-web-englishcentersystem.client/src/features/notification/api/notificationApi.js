import apiClient
    from "../../../app/apiClient";

export const getMyNotificationsApi =
    async ({
        page = 1,
        pageSize = 20,
        search = "",
        isRead = null,
        type = null
    } = {}) => {
        const params = {
            page,
            pageSize
        };

        const normalizedSearch =
            search?.trim();

        if (normalizedSearch) {
            params.search =
                normalizedSearch;
        }

        if (
            isRead !== null &&
            isRead !== undefined
        ) {
            params.isRead =
                isRead;
        }

        if (
            type !== null &&
            type !== undefined &&
            type !== ""
        ) {
            params.type =
                type;
        }

        const response =
            await apiClient.get(
                "/notifications",
                {
                    params
                }
            );

        return response.data.data;
    };
export const markManyNotificationsAsReadApi =
    async userNotificationIds => {
        const response =
            await apiClient.put(
                "/notifications/read-many",
                {
                    userNotificationIds
                }
            );

        return response.data.data;
    };

export const deleteNotificationApi =
    async userNotificationId => {
        const response =
            await apiClient.delete(
                `/notifications/${userNotificationId}`
            );

        return response.data.data;
    };

export const deleteManyNotificationsApi =
    async userNotificationIds => {
        const response =
            await apiClient.post(
                "/notifications/delete-many",
                {
                    userNotificationIds
                }
            );

        return response.data.data;
    };
export const getUnreadNotificationCountApi =
    async () => {
        const response =
            await apiClient.get(
                "/notifications/unread-count"
            );

        return response.data.data;
    };

export const markNotificationAsReadApi =
    async userNotificationId => {
        const response =
            await apiClient.put(
                `/notifications/${userNotificationId}/read`
            );

        return response.data.data;
    };

export const markAllNotificationsAsReadApi =
    async () => {
        const response =
            await apiClient.put(
                "/notifications/read-all"
            );

        return response.data.data;
    };