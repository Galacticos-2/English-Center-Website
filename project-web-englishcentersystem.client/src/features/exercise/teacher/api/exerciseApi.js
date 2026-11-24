import apiClient from "../../../../app/apiClient";

export const getExercisesApi = async (
    courseType,
    skill
) => {
    const res = await apiClient.get("/Exercise", {
        params: {
            courseType: courseType || null,
            skill: skill || null
            
        }
    });

    return res.data.data;
};

export const getExerciseDetailApi = async (exerciseId) => {
    const res = await apiClient.get(`/Exercise/${exerciseId}`);
    return res.data.data;
};

export const getExerciseSourceFileApi = async (exerciseId) => {
    const res = await apiClient.get(`/Exercise/${exerciseId}/source-file`);
    return res.data.data;
};

export const createExerciseApi = async (data) => {
    const res = await apiClient.post("/Exercise", data);
    return res.data;
};

export const updateExerciseApi = async (exerciseId, data) => {
    const res = await apiClient.put(`/Exercise/${exerciseId}`, data);
    return res.data;
};

export const deleteExerciseApi = async (exerciseId) => {
    const res = await apiClient.delete(`/Exercise/${exerciseId}`);
    return res.data;
};
export const updateExerciseMetadataApi =
    async (
        exerciseId,
        data
    ) => {
        const res =
            await apiClient.put(
                `/Exercise/${exerciseId}/metadata`,
                data
            );

        return res.data;
    };
