import apiClient
    from "../../../app/apiClient";

export const getAdminDashboardApi =
    async (days = 7) => {
        const response =
            await apiClient.get(
                "/admin/dashboard",
                {
                    params: {
                        days
                    }
                }
            );

        return response.data.data;
    };
