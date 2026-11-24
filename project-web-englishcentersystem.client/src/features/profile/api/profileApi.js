import apiClient from "../../../app/apiClient";

export const getMyInfoApi = async () => {
    const response = await apiClient.get("/users/me");
    return response.data;
};

export const uploadAvatarApi = async (file, originalFile) => {
    const formData = new FormData();

    formData.append("file", file);
    formData.append("originalFile", originalFile);

    const response = await apiClient.post(
        "/users/me/avatar",
        formData,
        {
            headers: {
                "Content-Type": "multipart/form-data",
            },
        }
    );

    return response.data;
};
