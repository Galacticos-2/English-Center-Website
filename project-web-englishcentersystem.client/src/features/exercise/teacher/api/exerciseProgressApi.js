import apiClient from "../../../../app/apiClient";

export const getTeacherExerciseProgressApi = async () => {
    const response = await apiClient.get(
        "/exercise-progress/teacher"
    );

    return response.data.data;
};