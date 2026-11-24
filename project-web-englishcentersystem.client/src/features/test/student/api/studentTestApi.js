import apiClient from "../../../../app/apiClient";

export const getMyStudentTestsApi = async () => {
    const response = await apiClient.get(
        "/student/tests"
    );

    return response.data.data;
};

export const startTestAttemptApi = async (testAssignmentId) => {
    const response = await apiClient.post(
        "/student/tests/start",
        {
            testAssignmentId
        }
    );

    return response.data.data;
};

export const submitTestAttemptApi = async (
    testAttemptId,
    answers
) => {
    const response = await apiClient.post(
        "/student/tests/submit",
        {
            testAttemptId,
            answers
        }
    );

    return response.data.data;
};

export const submitTestSpeakingAnswerApi = async (formData) => {
    const response = await apiClient.post(
        "/student/tests/speaking-answer",
        formData,
        {
            headers: {
                "Content-Type": "multipart/form-data"
            }
        }
    );

    return response.data.data;
};

export const getTestAttemptResultApi = async (testAttemptId) => {
    const response = await apiClient.get(
        `/student/tests/${testAttemptId}/result`
    );

    return response.data.data;
};

export const getMyTestHistoryApi = async () => {
    const response = await apiClient.get(
        "/student/tests/history"
    );

    return response.data.data;
};