export const formatSpeakingScore = value => {
    if (value == null || value === "") {
        return "-";
    }

    const numericValue = Number(value);

    if (!Number.isFinite(numericValue)) {
        return "-";
    }

    const normalizedScore = numericValue > 10
        ? numericValue / 10
        : numericValue;

    return Math.min(Math.max(normalizedScore, 0), 10)
        .toFixed(1);
};
