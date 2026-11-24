import apiClient from "../../../../app/apiClient";

export const importAssignTestApi = async (formData) => {
    const response = await apiClient.post(
        "/test-import/import-assign",
        formData,
        {
            headers: {
                "Content-Type": "multipart/form-data"
            }
        }
    );

    return response.data.data;
};

export const getTestBackgroundJobApi = async (backgroundJobId) => {
    const response = await apiClient.get(
        `/background-jobs/${backgroundJobId}`
    );

    return response.data.data;
};