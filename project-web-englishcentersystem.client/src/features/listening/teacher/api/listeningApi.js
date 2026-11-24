import apiClient from "../../../../app/apiClient";

export const getListeningTasksApi = async () => {
    const res = await apiClient.get(
        "/listening"
    );

    return res.data.data;
};

export const createListeningTaskApi =
    async (formData) => {

        const res = await apiClient.post(
            "/listening",
            formData,
            {
                headers: {
                    "Content-Type":
                        "multipart/form-data"
                }
            }
        );

        return res.data.data;
    };

export const deleteListeningTaskApi =
    async (id) => {

        await apiClient.delete(
            `/listening/${id}`
        );
    };