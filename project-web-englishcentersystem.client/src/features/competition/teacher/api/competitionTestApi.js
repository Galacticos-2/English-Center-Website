import apiClient
    from "../../../../app/apiClient";

export const getTeacherCompetitionTestsApi =
    async () => {
        const response =
            await apiClient.get(
                "/teacher/competition-tests"
            );

        return response.data.data;
    };
export const getCompetitionSourceFileApi =
    async competitionTestId => {
        const response =
            await apiClient.get(
                `/teacher/competition-tests/${competitionTestId}/source-file`
            );

        return response.data.data;
    };
export const replaceCompetitionTestApi =
    async (
        competitionTestId,
        formData
    ) => {
        formData.set(
            "replacingCompetitionTestId",
            String(
                competitionTestId
            )
        );

        const response =
            await apiClient.post(
                "/competition-test-import/import",
                formData
            );

        return response.data.data;
    };

export const importCompetitionTestApi =
    async formData => {
        const response =
            await apiClient.post(
                "/competition-test-import/import",
                formData
            );

        return response.data.data;
    };

export const updateCompetitionTestApi =
    async (
        competitionTestId,
        payload
    ) => {
        const response =
            await apiClient.put(
                `/teacher/competition-tests/${competitionTestId}`,
                payload
            );

        return response.data.data;
    };

export const deleteCompetitionTestApi =
    async competitionTestId => {
        const response =
            await apiClient.delete(
                `/teacher/competition-tests/${competitionTestId}`
            );

        return response.data.data;
    };

export const getCompetitionImportJobApi =
    async backgroundJobId => {
        const response =
            await apiClient.get(
                `/background-jobs/${backgroundJobId}`
            );

        return response.data.data;
    };