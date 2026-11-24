import apiClient
    from "../../../../app/apiClient";

export const getCompetitionAssignmentOptionsApi =
    async () => {
        const response = await apiClient.get(
            "/teacher/competitions/options"
        );

        return response.data.data;
    };
export const updateTeacherCompetitionApi =
    async (
        competitionTestAssignmentId,
        payload
    ) => {
        const response = await apiClient.put(
            `/teacher/competitions/${competitionTestAssignmentId}`,
            payload
        );

        return response.data.data;
    };

export const hardDeleteTeacherCompetitionApi =
    async competitionTestAssignmentId => {
        const response = await apiClient.delete(
            `/teacher/competitions/${competitionTestAssignmentId}/hard-delete`
        );

        return response.data.data;
    };
export const createCompetitionAssignmentApi =
    async payload => {
        const response = await apiClient.post(
            "/teacher/competitions",
            payload
        );

        return response.data.data;
    };

export const getTeacherCompetitionsApi =
    async () => {
        const response = await apiClient.get(
            "/teacher/competitions"
        );

        return response.data.data;
    };

export const getTeacherCompetitionDetailApi =
    async competitionTestAssignmentId => {
        const response = await apiClient.get(
            `/teacher/competitions/${competitionTestAssignmentId}`
        );

        return response.data.data;
    };

export const cancelTeacherCompetitionApi =
    async competitionTestAssignmentId => {
        const response = await apiClient.delete(
            `/teacher/competitions/${competitionTestAssignmentId}`
        );

        return response.data.data;
    };

export const getTeacherCompetitionLeaderboardApi =
    async competitionTestAssignmentId => {
        const response = await apiClient.get(
            `/teacher/competition-results/${competitionTestAssignmentId}/leaderboard`
        );

        return response.data.data;
    };

export const getTeacherCompetitionAttemptDetailApi =
    async competitionTestAttemptId => {
        const response = await apiClient.get(
            `/teacher/competition-results/attempts/${competitionTestAttemptId}`
        );

        return response.data.data;
    };