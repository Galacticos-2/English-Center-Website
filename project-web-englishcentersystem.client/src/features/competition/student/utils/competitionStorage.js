const ATTEMPT_KEY =
    "student-competition-attempt";

const RESULT_KEY =
    "student-competition-result";

export const saveCompetitionAttempt =
    attempt => {
        if (!attempt) {
            return;
        }

        const value = {
            competitionTestAttemptId:
                attempt.competitionTestAttemptId,

            competitionTestAssignmentId:
                attempt.competitionTestAssignmentId,

            competitionTestId:
                attempt.competitionTestId,

            startedAt:
                attempt.startedAt,

            expiresAt:
                attempt.expiresAt,

            deadlineAt:
                attempt.deadlineAt,

            serverTime:
                attempt.serverTime,

            durationMinutes:
                attempt.durationMinutes,

            competitionEndAt:
                attempt.competitionEndAt
        };

        try {
            localStorage.setItem(
                ATTEMPT_KEY,
                JSON.stringify(value)
            );
        }
        catch (error) {
            console.error(
                "Không thể lưu tạm Competition attempt:",
                error
            );
        }
    };

export const getSavedCompetitionAttempt =
    () => {
        const value =
            localStorage.getItem(
                ATTEMPT_KEY
            );

        if (!value) {
            return null;
        }

        try {
            return JSON.parse(value);
        }
        catch {
            return null;
        }
    };

export const clearCompetitionAttempt =
    () => {
        localStorage.removeItem(
            ATTEMPT_KEY
        );
    };

export const saveCompetitionResult =
    result => {
        try {
            localStorage.setItem(
                RESULT_KEY,
                JSON.stringify(result)
            );
        }
        catch (error) {
            console.error(
                "Không thể lưu kết quả Competition:",
                error
            );
        }
    };

export const getSavedCompetitionResult =
    () => {
        const value =
            localStorage.getItem(
                RESULT_KEY
            );

        if (!value) {
            return null;
        }

        try {
            return JSON.parse(value);
        }
        catch {
            return null;
        }
    };
