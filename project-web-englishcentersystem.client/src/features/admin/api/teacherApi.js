import apiClient
    from "../../../app/apiClient";

export const getTeachersApi =
    async () => {

        const response =
            await apiClient.get(
                "/users/teachers"
            );

        return response.data;
    };
export const deleteTeacherApi =
    async (id) => {

        const response =
            await apiClient.delete(
                `/users/teachers/${id}`
            );

        return response.data;
    };
export const createTeacherApi =
    async (data) => {

        const response =
            await apiClient.post(
                "/users/teachers",
                data
            );

        return response.data;
    };

export const updateTeacherApi =
    async (id, data) => {
        const response =
            await apiClient.put(
                `/users/teachers/${id}`,
                data
            );

        return response.data;
    };
