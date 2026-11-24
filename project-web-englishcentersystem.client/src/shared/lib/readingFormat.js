const namedSectionPattern =
    /([.!?])\s+([A-Z][A-Za-z'\u2019.-]*(?:\s+[A-Z][A-Za-z'\u2019.-]*){0,3}\s+[\u2013-]\s+[A-Z][A-Za-z'\u2019/-]*(?:\s+[A-Z][A-Za-z'\u2019/-]*){0,5})/g;

const namedSectionBodyPattern =
    /([A-Z][A-Za-z'\u2019.-]*(?:\s+[A-Z][A-Za-z'\u2019.-]*){0,3}\s+[\u2013-]\s+[A-Z][A-Za-z'\u2019/-]*(?:\s+[A-Z][A-Za-z'\u2019/-]*){0,5}?)\s+(?=(?:[A-Z][a-z]+ing|The|My|I|This|It|In|For|When|As|One|Our|Responding|Capturing)\b)/g;

const coralParagraphStartPattern =
    /\s+(?=(?:Corals are actually|Coral reefs not only|However, coral reefs|Another significant issue|Just as forests)\b)/g;

const sentencePattern =
    /[^.!?]+[.!?]+(?:["')\]]+)?|[^.!?]+$/g;

const hasParagraphBreak = text =>
    /\n\s*\n/.test(text);

const splitSentences = text =>
    text.match(sentencePattern)
        ?.map(sentence => sentence.trim())
        .filter(Boolean) || [];

const groupSentencesIntoParagraphs = text => {
    const sentences = splitSentences(text);

    if (sentences.length <= 2) {
        return text.trim();
    }

    const groups = [];
    let currentGroup = [];
    let currentLength = 0;

    sentences.forEach(sentence => {
        currentGroup.push(sentence);
        currentLength += sentence.length;

        const shouldCloseGroup =
            currentGroup.length >= 4 ||
            (
                currentGroup.length >= 3 &&
                currentLength >= 430
            ) ||
            currentLength >= 760;

        if (shouldCloseGroup) {
            groups.push(currentGroup.join(" "));
            currentGroup = [];
            currentLength = 0;
        }
    });

    if (currentGroup.length > 0) {
        if (
            groups.length > 0 &&
            currentGroup.length === 1
        ) {
            groups[groups.length - 1] =
                `${groups[groups.length - 1]} ${currentGroup.join(" ")}`;
        }
        else {
            groups.push(currentGroup.join(" "));
        }
    }

    return groups.join("\n\n");
};

const isCoralReefPassage = text =>
    /coral reefs/i.test(text) &&
    /bleaching|ocean acidification/i.test(text);

const applyTopicParagraphs = text => {
    if (isCoralReefPassage(text)) {
        return text.replace(
            coralParagraphStartPattern,
            "\n\n");
    }

    return text;
};

const applyFallbackParagraphs = text =>
    applyTopicParagraphs(text)
        .replace(namedSectionPattern, "$1\n\n$2")
        .replace(namedSectionBodyPattern, "$1\n")
        .split(/\n{2,}/)
        .map(groupSentencesIntoParagraphs)
        .join("\n\n");

export const formatReadingPassageText = value => {
    if (!value) {
        return "";
    }

    const normalized = String(value)
        .replace(/\r\n?/g, "\n")
        .replace(/\u00A0/g, " ")
        .replace(/[ \t]+\n/g, "\n")
        .replace(/\n[ \t]+/g, "\n")
        .replace(/\n{3,}/g, "\n\n")
        .trim();

    if (!normalized) {
        return "";
    }

    if (hasParagraphBreak(normalized)) {
        return normalized;
    }

    return applyFallbackParagraphs(normalized)
        .replace(/\n{3,}/g, "\n\n")
        .trim();
};

export const isToeicReadingClozeQuestion = question => {
    if (!question?.isReadingQuestion) {
        return false;
    }

    const match = String(question.content || "")
        .trim()
        .match(/^Blank\s+(\d{1,3})$/i);

    if (!match) {
        return false;
    }

    const escapedNumber = match[1].replace(
        /[.*+?^${}()|[\]\\]/g,
        "\\$&"
    );
    const blankPattern = new RegExp(
        `-{3,}\\s*\\(${escapedNumber}\\)`,
        "i"
    );

    return blankPattern.test(
        String(question.readingPassageText || "")
    );
};

export const isToeicReadingRangeTitle = value =>
    /^\s*Questions?\s+\d{1,3}\s*[-–—]\s*\d{1,3}\s*$/i.test(
        String(value || "")
    );

export const isToeicReadingPartLabel = value =>
    /^\s*Incomplete\s+Sentences\s*$/i.test(
        String(value || "")
    );

export const splitToeicReadingPassageTitle = value => {
    const title = String(value || "").trim();
    const match = title.match(
        /^(TOEIC\s+Reading\s+Part\s+[67])\s*-\s*(Questions?\s+\d{1,3}\s*[-–—]\s*\d{1,3}\s+refer.+)$/i
    );

    return match
        ? {
            partTitle: match[1],
            groupTitle: match[2]
        }
        : null;
};

export const splitVstepReadingInstruction = value => {
    const instruction = String(value || "").trim();
    const match = instruction.match(
        /^(?<passage>(?:Reading\s+)?Passage\s+\d+[^\n]*)(?:\n\s*\n|\n)(?<direction>[\s\S]+)$/i
    );

    return match
        ? {
            passageLabel: match.groups.passage.trim(),
            direction: match.groups.direction.trim()
        }
        : null;
};

export const getVstepReadingDisplayContent = ({
    passageTitle,
    passageText,
    instruction
}) => {
    const title = String(passageTitle || "").trim();
    const text = String(passageText || "").replace(/\r\n?/g, "\n");
    const legacyLabelMatch = title.match(
        /^(?:Reading\s+)?Passage\s+\d+[^\n]*$/i
    );
    const parsedInstruction = splitVstepReadingInstruction(instruction);
    const passageLabel = legacyLabelMatch?.[0] ||
        parsedInstruction?.passageLabel;

    if (!passageLabel) {
        return null;
    }

    const textLines = text
        .split("\n")
        .map(line => line.trim())
        .filter(Boolean);
    const displayTitle = legacyLabelMatch
        ? textLines.shift() || "Reading Passage"
        : title || textLines.shift() || "Reading Passage";

    if (
        !legacyLabelMatch &&
        textLines[0] &&
        textLines[0].toLowerCase() === displayTitle.toLowerCase()
    ) {
        textLines.shift();
    }

    const direction = legacyLabelMatch
        ? String(instruction || "")
            .replace(/^\s*Directions?\s*:\s*/i, "")
            .trim()
        : parsedInstruction?.direction || "";

    return {
        passageLabel,
        passageTitle: displayTitle,
        passageText: textLines.join("\n"),
        direction
    };
};
