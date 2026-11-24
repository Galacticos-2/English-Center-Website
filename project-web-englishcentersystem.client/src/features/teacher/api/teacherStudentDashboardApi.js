import apiClient
    from "../../../app/apiClient";

export const getTeacherStudentDashboardApi =
    async ({
        classRoomId,
        courseId,
        search,
        sortBy
    }) => {
        const response =
            await apiClient.get(
                "/teacher/student-dashboard",
                {
                    params: {
                        classRoomId:
                            classRoomId || null,

                        courseId:
                            courseId || null,

                        search:
                            search?.trim() || null,

                        sortBy:
                            sortBy || null
                    }
                }
            );

        return response.data.data;
    };