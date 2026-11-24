import apiClient from "../../../../app/apiClient";

export const getExerciseAssignmentsApi = async () => {
    const res = await apiClient.get("/ExerciseAssignment");
    return res.data.data;
};

export const createExerciseAssignmentApi = async (data) => {
    const res = await apiClient.post("/ExerciseAssignment", data);
    return res.data;
};

export const deleteExerciseAssignmentApi = async (
    exerciseAssignmentId
) => {
    const res = await apiClient.delete(
        `/ExerciseAssignment/${exerciseAssignmentId}`
    );

    return res.data;
};