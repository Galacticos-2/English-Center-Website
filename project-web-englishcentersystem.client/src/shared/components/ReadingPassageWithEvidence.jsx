import { formatReadingPassageText }
    from "../lib/readingFormat";

const escapeRegularExpression = value =>
    value.replace(/[.*+?^${}()|[\]\\]/g, "\\$&");

const getQuotedEvidence = explanation => {
    const text = String(explanation || "");
    const phrases = [];
    const doubleQuotePattern = /["\u201C]([^"\u201D\n]{6,240})["\u201D]/g;
    const singleQuotePattern = /(?:^|[\s([{:;])['\u2018]([^'\u2019\n]{6,240})['\u2019]/g;

    for (const pattern of [doubleQuotePattern, singleQuotePattern]) {
        let match;

        while ((match = pattern.exec(text)) !== null) {
            const phrase = match[1]
                .replace(/\s+/g, " ")
                .trim();

            if (phrase.length >= 6) {
                phrases.push(phrase);
            }
        }
    }

    return phrases;
};

const getEvidenceRanges = (passage, explanations) => {
    const phrases = (explanations || [])
        .flatMap(getQuotedEvidence)
        .filter((phrase, index, all) =>
            all.findIndex(candidate =>
                candidate.toLowerCase() === phrase.toLowerCase()
            ) === index
        );

    const ranges = phrases.flatMap(phrase => {
        const expression = new RegExp(
            escapeRegularExpression(phrase)
                .replace(/\s+/g, "\\s+"),
            "gi"
        );
        const matches = [];
        let match;

        while ((match = expression.exec(passage)) !== null) {
            matches.push({
                start: match.index,
                end: match.index + match[0].length
            });
        }

        return matches;
    });

    const sortedRanges = ranges
        .sort((left, right) =>
            left.start - right.start || right.end - left.end
        );

    return sortedRanges.reduce((accepted, range) => {
        const previous = accepted[accepted.length - 1];

        if (!previous || range.start >= previous.end) {
            accepted.push(range);
        }

        return accepted;
    }, []);
};

function ReadingPassageWithEvidence({ passage, explanations }) {
    const formattedPassage = formatReadingPassageText(passage);
    const ranges = getEvidenceRanges(formattedPassage, explanations);

    if (ranges.length === 0) {
        return formattedPassage;
    }

    const content = [];
    let cursor = 0;

    ranges.forEach((range, index) => {
        if (range.start > cursor) {
            content.push(formattedPassage.slice(cursor, range.start));
        }

        content.push(
            <mark
                className="reading-evidence-highlight"
                key={`${range.start}-${range.end}-${index}`}
            >
                {formattedPassage.slice(range.start, range.end)}
            </mark>
        );
        cursor = range.end;
    });

    if (cursor < formattedPassage.length) {
        content.push(formattedPassage.slice(cursor));
    }

    return content;
}

export default ReadingPassageWithEvidence;
