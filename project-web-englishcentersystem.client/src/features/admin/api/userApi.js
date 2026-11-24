import apiClient
    from "../../../app/apiClient";

export const getUsersApi =
    async () => {

        const response =
            await apiClient.get("/users");

        return response.data;
    };
export const getStudentsApi =
    async () => {

        const response =
            await apiClient.get(
                "/users/students"
            );

        return response.data;
    };
export const updateStudentApi =
    async (
        userId,
        data
    ) => {
        const response =
            await apiClient.put(
                `/users/students/${userId}`,
                data
            );

        return response.data;
    };
export const deleteStudentApi =
    async (id) => {

        const response =
            await apiClient.delete(
                `/users/students/${id}`
            );

        return response.data;
    };
export const createStudentApi =
    async (data) => {

        const response =
            await apiClient.post(
                "/users/students",
                data
            );

        return response.data;
    };