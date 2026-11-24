import apiClient
    from "../../../../app/apiClient";

export const getMyCompetitionsApi =
    async () => {
        const response = await apiClient.get(
            "/student/competitions"
        );

        return response.data.data;
    };

export const startCompetitionAttemptApi =
    async competitionTestAssignmentId => {
        const response = await apiClient.post(
            `/student/competitions/${competitionTestAssignmentId}/start`
        );

        return response.data.data;
    };

export const submitCompetitionAttemptApi =
    async (
        competitionTestAttemptId,
        answers
    ) => {
        const response = await apiClient.post(
            "/student/competitions/submit",
            {
                competitionTestAttemptId,
                answers
            }
        );

        return response.data.data;
    };

export const submitCompetitionSpeakingAnswerApi =
    async formData => {
        const response = await apiClient.post(
            "/student/competitions/speaking-answer",
            formData,
            {
                headers: {
                    "Content-Type":
                        "multipart/form-data"
                }
            }
        );

        return response.data.data;
    };

export const getCompetitionAttemptResultApi =
    async competitionTestAttemptId => {
        const response = await apiClient.get(
            `/student/competitions/attempts/${competitionTestAttemptId}/result`
        );

        return response.data.data;
    };

export const getStudentCompetitionLeaderboardApi =
    async competitionTestAssignmentId => {
        const response = await apiClient.get(
            `/student/competitions/${competitionTestAssignmentId}/leaderboard`
        );

        return response.data.data;
    };