import apiClient from "../../../../app/apiClient";

export const getTeacherTestAttemptsApi = async (testId) => {
    const response = await apiClient.get(
        `/teacher/tests/${testId}/attempts`
    );

    return response.data.data;
};

export const getTeacherTestAttemptDetailApi = async (testAttemptId) => {
    const response = await apiClient.get(
        `/teacher/tests/attempts/${testAttemptId}`
    );

    return response.data.data;
};
export const updateTeacherTestApi = async (testId, payload) => {
    const response = await apiClient.put(
        `/teacher/tests/${testId}`,
        payload
    );

    return response.data.data;
};

export const deleteTeacherTestApi = async (testId) => {
    const response = await apiClient.delete(
        `/teacher/tests/${testId}`
    );

    return response.data.data;
};
export const getTeacherTestAnalyticsApi = async (testId) => {
    const response = await apiClient.get(
        `/teacher/tests/${testId}/analytics`
    );

    return response.data.data;
};
export const exportTeacherTestResultsApi = async (testId) => {
    const response = await apiClient.get(
        `/teacher/tests/${testId}/export`,
        {
            responseType: "blob"
        }
    );

    return response.data;
};
export const getTeacherTestsApi = async () => {
    const response = await apiClient.get(
        "/teacher/tests"
    );

    return response.data.data;
};

export const getTeacherTestSourceFileApi = async (testId) => {
    const response = await apiClient.get(
        `/teacher/tests/${testId}/source-file`
    );

    return response.data.data;
};
