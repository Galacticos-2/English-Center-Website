import apiClient from "../../../../app/apiClient";

export const getReadingTasksApi = async (courseType) => {
    const res = await apiClient.get("/reading", {
        params: {
            courseType: courseType || null
        }
    });

    return res.data.data;
};

export const getReadingTaskDetailApi = async (readingTaskId) => {
    const res = await apiClient.get(`/reading/${readingTaskId}`);
    return res.data.data;
};

export const createReadingTaskApi = async (data) => {
    const res = await apiClient.post("/reading", data);
    return res.data;
};

export const updateReadingTaskApi = async (readingTaskId, data) => {
    const res = await apiClient.put(`/reading/${readingTaskId}`, data);
    return res.data;
};

export const deleteReadingTaskApi = async (readingTaskId) => {
    const res = await apiClient.delete(`/reading/${readingTaskId}`);
    return res.data;
};