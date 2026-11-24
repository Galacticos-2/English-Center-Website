import apiClient from "../../../../app/apiClient";

export const getExerciseResultsApi = async (
    exerciseAssignmentId
) => {
    const response = await apiClient.get(
        "/exercise-results",
        {
            params: {
                exerciseAssignmentId:
                    exerciseAssignmentId || null
            }
        }
    );

    return response.data.data;
};
