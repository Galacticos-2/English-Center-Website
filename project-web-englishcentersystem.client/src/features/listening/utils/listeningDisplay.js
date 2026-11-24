export const getListeningQuestionDisplayContent = (
    content,
    questionNumber
) => {
    const value = String(content || "").trim();
    const number = Number(questionNumber);

    if (!value || number < 1 || number > 31) {
        return value;
    }

    const withoutRepeatedNumber = value
        .replace(
            new RegExp(
                `^Question\\s*${number}\\b\\s*[:.\\-]?\\s*`,
                "i"
            ),
            ""
        )
        .trim();

    if (!withoutRepeatedNumber) {
        return value;
    }

    const withoutWrappingParentheses =
        withoutRepeatedNumber.startsWith("(") &&
        withoutRepeatedNumber.endsWith(")")
            ? withoutRepeatedNumber.slice(1, -1).trim()
            : withoutRepeatedNumber;

    return withoutWrappingParentheses.charAt(0).toUpperCase() +
        withoutWrappingParentheses.slice(1);
};

export const formatListeningPartTitle = title => {
    const value = String(title || "").trim();
    const match = value.match(/^Part\s+(\d+)\b(.*)$/i);

    return match
        ? `Listening Part ${match[1]}${match[2]}`
        : value;
};

export const getToeicListeningPartContext = questionNumber => {
    const number = Number(questionNumber);

    if (number >= 1 && number <= 6) {
        return {
            title: "TOEIC Listening Part 1",
            direction: "For each question in this part, you will hear 4 statements about a picture. When you hear the statements, you must select the one statement that describes what you see in the picture."
        };
    }

    if (number >= 7 && number <= 31) {
        return {
            title: "TOEIC Listening Part 2",
            direction: "You will hear a question or statement and three responses spoken in English. They will not be printed in your test book and will be spoken only one time. Select the best response and mark the letter (A), (B), or (C) on your answer sheet."
        };
    }

    if (number >= 32 && number <= 70) {
        return {
            title: "TOEIC Listening Part 3",
            direction: "You will hear conversations between two or more people. Answer the questions about what the speakers say in each conversation."
        };
    }

    if (number >= 71 && number <= 100) {
        return {
            title: "TOEIC Listening Part 4",
            direction: "You will hear talks by a single speaker. Answer the questions about what the speaker says in each talk."
        };
    }

    return null;
};
