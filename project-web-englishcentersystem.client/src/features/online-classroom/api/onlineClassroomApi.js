import apiClient from "../../../app/apiClient";

export const getTeacherOnlineClassroomsApi = async () =>
    (await apiClient.get("/online-classrooms/teacher")).data.data;

export const getStudentOnlineClassroomsApi = async () =>
    (await apiClient.get("/online-classrooms/student")).data.data;

export const createOnlineClassroomApi = async (data) =>
    (await apiClient.post("/online-classrooms", data)).data.data;

export const updateOnlineClassroomApi = async (id, data) =>
    (await apiClient.put(`/online-classrooms/${id}`, data)).data.data;

export const deleteOnlineClassroomApi = async (id) =>
    apiClient.delete(`/online-classrooms/${id}`);
