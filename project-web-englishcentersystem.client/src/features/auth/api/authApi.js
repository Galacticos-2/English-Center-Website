import apiClient from "../../../app/apiClient";

export const loginApi = async (data) => {

    const response = await apiClient.post(
        "/auth/login",
        data
    );

    return response.data;
};

export const registerApi = async (
    data
) => {

    const response = await apiClient.post(
        "/auth/register",
        data
    );

    return response.data;
};