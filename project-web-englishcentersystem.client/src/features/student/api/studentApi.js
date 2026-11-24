import apiClient
    from "../../../app/apiClient";

export const getStudentProfileApi =
    async () => {

        const response =
            await apiClient.get(
                "/users/student-profile"
            );

        return response.data;
    };

export const getStudentClassLeaderboardApi = async () => {
    const response = await apiClient.get("/users/student-class-leaderboard");

    return response.data;
};
export const getMyStudentExercisesApi = async () => {
    const response = await apiClient.get(
        "/student/exercises"
    );

    return response.data.data;
};

export const startExerciseAttemptApi = async (
    exerciseAssignmentId
) => {
    const response = await apiClient.post(
        "/student/exercises/start",
        {
            exerciseAssignmentId
        }
    );

    return response.data.data;
};
export const getMyExerciseSkillProgressApi = async () => {
    const response = await apiClient.get(
        "/student/exercises/skill-progress"
    );

    return response.data.data;
};
export const getExerciseAttemptResultApi = async (
    exerciseAttemptId
) => {
    const response = await apiClient.get(
        `/student/exercises/${exerciseAttemptId}/result`
    );

    return response.data.data;
};
export const getMyExerciseProgressApi = async () => {
    const response = await apiClient.get(
        "/student/exercises/progress"
    );

    return response.data.data;
};
export const getMyExerciseHistoryApi = async () => {
    const response = await apiClient.get(
        "/student/exercises/history"
    );

    return response.data.data;
};
export const getMyStudentExerciseSectionsApi = async (
    skill
) => {
    const response = await apiClient.get(
        "/student/exercises/sections",
        {
            params: {
                skill: skill || null
            }
        }
    );

    return response.data.data;
};
export const submitExerciseAttemptApi = async (
    exerciseAttemptId,
    answers
) => {
    const response = await apiClient.post(
        "/student/exercises/submit",
        {
            exerciseAttemptId,
            answers
        }
    );

    return response.data.data;
};
