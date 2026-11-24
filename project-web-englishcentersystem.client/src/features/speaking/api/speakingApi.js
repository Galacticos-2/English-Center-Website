import apiClient from "../../../app/apiClient";

export const importSpeakingExerciseApi = async (formData) => {
    const response = await apiClient.post(
        "/exercise-import/speaking-import-assign",
        formData,
        {
            headers: {
                "Content-Type": "multipart/form-data"
            }
        }
    );

    return response.data.data;
};

export const submitSpeakingAnswerApi = async (formData) => {
    const response = await apiClient.post(
        "/student/exercises/speaking-answer",
        formData,
        {
            headers: {
                "Content-Type": "multipart/form-data"
            }
        }
    );

    return response.data.data;
};