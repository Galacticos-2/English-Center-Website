import apiClient
    from "../../../app/apiClient";

export const getCoursesApi =
    async () => {

        const response =
            await apiClient.get(
                "/course"
            );

        return response.data;
    };

export const createCourseApi =
    async (data) => {

        const response =
            await apiClient.post(
                "/course",
                data
            );

        return response.data;
    };
export const assignTeacherApi =
    async (data) => {

        const response =
            await apiClient.post(
                "/course/assign-teacher",
                data
            );

        return response.data;
    };
export const getClassesApi =
    async (courseId) => {

        const response =
            await apiClient.get(
                `/course/${courseId}/classes`
            );

        return response.data;
    };

export const createClassRoomApi =
    async (courseId, data) => {
        const response = await apiClient.post(
            `/course/${courseId}/classes`,
            data
        );

        return response.data;
    };

export const updateClassRoomApi =
    async (courseId, classRoomId, data) => {
        const response = await apiClient.put(
            `/course/${courseId}/classes/${classRoomId}`,
            data
        );

        return response.data;
    };

export const deleteClassRoomApi =
    async (courseId, classRoomId) => {
        const response = await apiClient.delete(
            `/course/${courseId}/classes/${classRoomId}`
        );

        return response.data;
    };
export const addTeacherClassApi =
    async (data) => {

        const response =
            await apiClient.post(
                "/course/teacher-class",
                data
            );

        return response.data;
    };

export const updateTeacherClassApi =
    async (data) => {

        const response =
            await apiClient.put(
                "/course/teacher-class",
                data
            );

        return response.data;
    };
export const updateCourseApi =
    async (id, data) => {

        const response =
            await apiClient.put(
                `/course/${id}`,
                data
            );

        return response.data;
    };

export const deleteCourseApi =
    async (id) => {

        const response =
            await apiClient.delete(
                `/course/${id}`
            );

        return response.data;
    };
export const assignStudentApi =
    async (data) => {

        const response =
            await apiClient.post(
                "/course/assign-student",
                data
            );

        return response.data;
    };

export const deleteTeacherClassApi =
    async (userId, classRoomId) => {

        const response =
            await apiClient.delete(
                `/course/teacher-class/${userId}/${classRoomId}`
            );

        return response.data;
    };

export const assignStudentCourseApi =
    async (data) => {

        const response =
            await apiClient.post(
                "/course/assign-student-course",
                data
            );

        return response.data;
    };
