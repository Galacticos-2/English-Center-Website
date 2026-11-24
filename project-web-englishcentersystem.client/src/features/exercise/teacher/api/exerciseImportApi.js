import apiClient from "../../../../app/apiClient";

export const importAssignExerciseApi = async (data) => {
    const res = await apiClient.post(
        "/exercise-import/import-assign",
        data,
        {
            headers: {
                "Content-Type": "multipart/form-data"
            }
        }
    );

    return res.data;
};
export const importAssignListeningExerciseApi = async (data) => {
    const res = await apiClient.post(
        "/exercise-import/listening-import-assign",
        data,
        {
            headers: {
                "Content-Type": "multipart/form-data"
            }
        }
    );

    return res.data;
};
export const importAssignWritingExerciseApi = async (data) => {
    const res = await apiClient.post(
        "/exercise-import/writing-import-assign",
        data,
        {
            headers: {
                "Content-Type": "multipart/form-data"
            }
        }
    );

    return res.data;
};
export const importAssignReadingExerciseApi = async (data) => {
    const res = await apiClient.post(
        "/exercise-import/reading-import-assign",
        data,
        {
            headers: {
                "Content-Type": "multipart/form-data"
            }
        }
    );

    return res.data;
};
export const importAssignSpeakingExerciseApi = async (data) => {
    const res = await apiClient.post(
        "/exercise-import/speaking-import-assign",
        data,
        {
            headers: {
                "Content-Type": "multipart/form-data"
            }
        }
    );

    return res.data;
};
export const getBackgroundJobApi = async (backgroundJobId) => {
    const res = await apiClient.get(
        `/background-jobs/${backgroundJobId}`
    );

    return res.data;
};