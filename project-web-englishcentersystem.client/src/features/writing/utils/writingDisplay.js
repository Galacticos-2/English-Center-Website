const QUESTION_NUMBER_PATTERN =
    /(?:question|câu)\s*(\d+)/i;

export const getWritingQuestionNumber = (
    question,
    displayQuestionNumber
) => {
    const contentMatch =
        String(question?.content || "")
            .match(QUESTION_NUMBER_PATTERN);

    if (contentMatch) {
        return Number(contentMatch[1]);
    }

    const fallbackNumber =
        Number(displayQuestionNumber);

    return Number.isFinite(fallbackNumber)
        ? fallbackNumber
        : null;
};

export const isToeicPictureWritingQuestion = (
    question,
    displayQuestionNumber
) => {
    const questionNumber =
        getWritingQuestionNumber(
            question,
            displayQuestionNumber
        );

    return Boolean(
        question?.isWritingQuestion &&
        question?.writingPromptImageUrl &&
        questionNumber >= 1 &&
        questionNumber <= 5
    );
};

export const shouldShowWritingInstruction = (
    question,
    displayQuestionNumber
) => {
    if (
        !isToeicPictureWritingQuestion(
            question,
            displayQuestionNumber
        )
    ) {
        return true;
    }

    return getWritingQuestionNumber(
        question,
        displayQuestionNumber
    ) === 1;
};

export const getWritingTaskDisplayTitle = (
    question,
    displayQuestionNumber
) => {
    if (
        isToeicPictureWritingQuestion(
            question,
            displayQuestionNumber
        )
    ) {
        return getWritingQuestionNumber(
            question,
            displayQuestionNumber
        ) === 1
            ? "Questions 1-5: Write a sentence based on a picture"
            : "Write a sentence based on a picture";
    }

    return question?.writingTaskType === "Essay"
        ? "Essay Task"
        : "Email / Reply Task";
};

export const getToeicPictureWritingPromptTitle = (
    question,
    displayQuestionNumber
) => {
    if (!isToeicPictureWritingQuestion(question, displayQuestionNumber)) {
        return "";
    }

    const content = String(question?.content || "")
        .replace(QUESTION_NUMBER_PATTERN, "")
        .replace(/\s*TOEIC\b[\s\S]*$/i, "")
        .replace(/^\s*[:.\-]?\s*/, "")
        .trim();

    return content || "Write a sentence based on a picture";
};

const normalizeWritingText = value =>
    String(value || "")
        .replace(/\s+/g, " ")
        .trim();

const extractToeicWrittenRequestDirection = values => {
    for (const value of values) {
        const match = normalizeWritingText(value).match(
            /\bDirections?\s*:\s*(.*?)(?=\s*Question\s*:|$)/i
        );

        if (match?.[1]?.trim()) {
            return match[1].trim();
        }
    }

    return "Read the e-mail.";
};

const extractToeicWrittenRequestPrompt = values => {
    for (const value of values) {
        const text = normalizeWritingText(value);
        const match = text.match(/\bQuestion\s*:\s*(.+)$/i);
        const prompt = (match?.[1] || text)
            .replace(/^\s*Directions?\s*:\s*.*?(?=\bQuestion\s*:)/i, "")
            .split(/\b(?:From\s*:|Dear\s+)/i)[0]
            .trim();

        if (prompt && !/^Directions?\s*:/i.test(prompt)) {
            return prompt;
        }
    }

    return "Respond to the e-mail.";
};

export const getToeicWrittenRequestDisplay = (
    question,
    displayQuestionNumber
) => {
    const questionNumber = getWritingQuestionNumber(
        question,
        displayQuestionNumber
    );
    const sourceText = String(question?.writingSourceText || "").trim();

    if (
        !question?.isWritingQuestion ||
        questionNumber < 6 ||
        questionNumber > 7 ||
        !sourceText
    ) {
        return null;
    }

    const values = [
        question?.writingInstruction,
        question?.writingPromptText,
        question?.writingRequirements
    ];

    return {
        questionNumber,
        sectionTitle: "Questions 6–7: Respond to a written request",
        direction: extractToeicWrittenRequestDirection(values),
        prompt: extractToeicWrittenRequestPrompt(values),
        sourceText
    };
};

export const splitWritingEmailPromptText = value => {
    const promptText = String(value || "")
        .replace(/\r\n?/g, "\n");
    const emailStartMatch = promptText.match(
        /Read(?:\s+part\s+of)?\s+(?:the\s+)?(?:email|message).*?(?:below\.?|:)\s*/i
    );

    if (!emailStartMatch) {
        return null;
    }

    const emailStartIndex =
        emailStartMatch.index + emailStartMatch[0].length;
    const remainingText = promptText.slice(emailStartIndex);
    const emailEndMatch = remainingText.match(
        /\b(?:Write|Reply)\s+(?:a\s+)?(?:reply|email)\b/i
    );

    if (!emailEndMatch || emailEndMatch.index === 0) {
        return null;
    }

    const emailEndIndex = emailStartIndex + emailEndMatch.index;

    return {
        beforeEmail: promptText.slice(0, emailStartIndex).trim(),
        emailContent: promptText.slice(emailStartIndex, emailEndIndex).trim(),
        afterEmail: promptText.slice(emailEndIndex).trim()
    };
};
