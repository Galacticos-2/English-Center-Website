import apiClient
    from "../../../app/apiClient";

export const getTeacherDashboardApi =
    async (days = 7) => {
        const response = await apiClient.get(
            "/teacher/dashboard",
            {
                params: {
                    days
                }
            }
        );

        return response.data.data;
    };