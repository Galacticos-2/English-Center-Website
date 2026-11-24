const VSTEP_SPEAKING_HEADING_PATTERN =
    /\bPart\s+[1-3]\s*:\s*(?:Social\s+Interaction|Solution\s+Discussion|Topic\s+Development)\b/i;

export const isVstepSpeakingTask = question =>
    VSTEP_SPEAKING_HEADING_PATTERN.test(
        `${question.speakingInstruction || ""}\n${
            question.speakingPromptText || ""
        }`
    );

export const getVstepDuration = value =>
    value?.match(/^\s*(\([^\r\n)]*[’'][^\r\n)]*\))/)?.[1] || "";

export const getToeicSpeakingGroup = question => {
    const instruction = String(question?.speakingInstruction || "").trim();
    const promptText = String(question?.speakingPromptText || "").trim();
    const singleQuestionMatch = promptText.match(
        /^\s*(Express\s+an\s+opinion\.?)\s+Directions?\s*:\s*([\s\S]*?)\s+Question\s*:\s*([\s\S]+)$/i
    );

    if (
        /^Express\s+an\s+opinion\.?$/i.test(instruction) &&
        singleQuestionMatch
    ) {
        return {
            title: "Question 11: Express an opinion.",
            direction: singleQuestionMatch[2].trim(),
            startQuestion: 11,
            promptText: singleQuestionMatch[3].trim()
        };
    }

    const groupedInstructionMatch = instruction.match(
        /\b(Questions?\s*(?:1\s*\D+\s*2|3\s*\D+\s*4|5\s*\D+\s*7|8\s*\D+\s*10|11)\s*:\s*[^\r\n]+)\s*(?:\r?\n|\s)+(?:Directions?\s*:\s*)?([\s\S]+)$/i
    );
    const match = groupedInstructionMatch || instruction.match(
        /\b(Questions?\s*1\s*[-–]\s*2\s*:\s*[^\r\n]+)\s*(?:\r?\n|\s)+(?:Directions?\s*:\s*)?([\s\S]+)$/i
    );

    if (!match) {
        return null;
    }

    return {
        title: match[1].trim(),
        direction: match[2].trim(),
        startQuestion: Number(match[1].match(/\d+/)?.[0])
    };
};

const formatSpeakingPromptText = (value, isVstep) => {
    if (!value || !isVstep) {
        return value;
    }

    return value
        .replace(/\r\n?/g, "\n")
        .replace(/^\s*\([^\r\n)]*[’'][^\r\n)]*\)\s*/, "")
        .replace(/[ \t]*[•●▪][ \t]*/g, "\n• ")
        .replace(/[ \t]*(?:\u2022|\u25cf|\u25aa)[ \t]*/g, "\n\n\u2022 ")
        .replace(/[ \t]+(Additional questions?:)/gi, "\n\n$1")
        .replace(/\n{3,}/g, "\n\n")
        .trim();
};

function SpeakingTaskContent({
    question,
    hideVstepHeading = false,
    hideToeicGroupInstruction = false,
    showPromptBeforeImage = false,
    promptTextOverride
}) {
    const isVstep = isVstepSpeakingTask(question);
    const promptText = promptTextOverride ?? question.speakingPromptText;
    const vstepDuration = isVstep
        ? getVstepDuration(question.speakingPromptText)
        : "";

    return (
        <>
            {(!isVstep || !hideVstepHeading) &&
                !hideToeicGroupInstruction && <h3>Speaking Task</h3>}

            {question.speakingInstruction &&
                (!isVstep || !hideVstepHeading) &&
                !hideToeicGroupInstruction && (
                <div
                    style={{
                        whiteSpace: "pre-line",
                        lineHeight: "1.7",
                        marginBottom: "14px"
                    }}
                >
                    {!isVstep && <strong>Instruction</strong>}
                    <div>
                        {question.speakingInstruction}
                        {vstepDuration && ` ${vstepDuration}`}
                    </div>
                </div>
            )}

            {showPromptBeforeImage && promptText && (
                <div
                    style={{
                        whiteSpace: "pre-line",
                        lineHeight: "1.8",
                        fontSize: "17px",
                        color: "#111827",
                        marginBottom: "14px"
                    }}
                >
                    {formatSpeakingPromptText(
                        promptText,
                        isVstep
                    )}
                </div>
            )}

            {question.speakingPromptImageUrl && (
                <img
                    src={question.speakingPromptImageUrl}
                    alt={question.content || "Speaking prompt"}
                    style={{
                        display: "block",
                        maxWidth: "100%",
                        maxHeight: "520px",
                        objectFit: "contain",
                        borderRadius: "10px",
                        border: "1px solid #e5e7eb",
                        marginBottom: "14px"
                    }}
                />
            )}

            {!showPromptBeforeImage && promptText && (
                <div
                    style={{
                        whiteSpace: "pre-line",
                        lineHeight: "1.8",
                        fontSize: "17px",
                        color: "#111827"
                    }}
                >
                    {formatSpeakingPromptText(
                        promptText,
                        isVstep
                    )}
                </div>
            )}

            {question.speakingReferenceText && (
                <div
                    style={{
                        whiteSpace: "pre-line",
                        lineHeight: "1.8",
                        marginTop: "14px"
                    }}
                >
                    {formatSpeakingPromptText(
                        question.speakingReferenceText,
                        isVstep
                    )}
                </div>
            )}

        </>
    );
}

export default SpeakingTaskContent;
