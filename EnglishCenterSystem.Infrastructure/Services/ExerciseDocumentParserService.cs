using System.Text;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using EnglishCenterSystem.Application.Interfaces.Services;
using UglyToad.PdfPig;
using EnglishCenterSystem.Application
    .Features.Documents.DTOs;
namespace EnglishCenterSystem.Infrastructure.Services;

public class ExerciseDocumentParserService
    : IExerciseDocumentParserService
{
    private static readonly HashSet<string>
        SupportedExtensions =
    [
        ".pdf",
        ".docx",
        ".txt"
    ];

    public async Task<string> ExtractTextAsync(
        Stream stream,
        string fileName)
    {
        if (stream == null)
        {
            throw new ArgumentNullException(
                nameof(stream));
        }

        if (string.IsNullOrWhiteSpace(fileName))
        {
            throw new Exception(
                "Document file name is required");
        }

        var extension = Path
            .GetExtension(fileName)
            .ToLowerInvariant();

        if (!SupportedExtensions.Contains(extension))
        {
            throw new Exception(
                "Only PDF, DOCX or TXT files are supported");
        }

        ResetStream(stream);

        var extractedText = extension switch
        {
            ".pdf" => ExtractPdfText(stream),

            ".docx" => ExtractDocxText(stream),

            ".txt" => await ExtractTxtTextAsync(
                stream),

            _ => throw new Exception(
                "Unsupported document format")
        };

        extractedText = NormalizeExtractedText(
            extractedText);

        if (string.IsNullOrWhiteSpace(
                extractedText))
        {
            throw new Exception(
                $"No readable text found in this " +
                $"{extension.TrimStart('.').ToUpperInvariant()} file.");
        }

        return extractedText;
    }

    private static string ExtractPdfText(
        Stream stream)
    {
        ResetStream(stream);

        var builder = new StringBuilder();

        using var document =
            PdfDocument.Open(stream);

        foreach (var page in
            document.GetPages())
        {
            var words = page
                .GetWords()
                .Where(word =>
                    !string.IsNullOrWhiteSpace(
                        word.Text))
                .Select(word => new
                    ParsedDocumentWordDto
                {
                    Text = word.Text.Trim(),

                    Left =
                        word.BoundingBox.Left,

                    Bottom =
                        word.BoundingBox.Bottom,

                    Right =
                        word.BoundingBox.Right,

                    Top =
                        word.BoundingBox.Top
                })
                .ToList();

            var pageText =
                BuildPageText(words);

            if (!string.IsNullOrWhiteSpace(
                    pageText))
            {
                builder.AppendLine(pageText);
            }

            builder.AppendLine();
        }

        return builder.ToString();
    }

    private static string ExtractDocxText(
        Stream stream)
    {
        ResetStream(stream);

        /*
         * Copy sang MemoryStream riêng để OpenXML
         * có thể đọc ổn định và không ảnh hưởng
         * đến stream gốc của request/background job.
         */
        using var documentStream =
            new MemoryStream();

        stream.CopyTo(documentStream);

        documentStream.Position = 0;

        using var document =
            WordprocessingDocument.Open(
                documentStream,
                false);

        var body = document
            .MainDocumentPart?
            .Document?
            .Body;

        if (body == null)
        {
            return string.Empty;
        }

        var builder = new StringBuilder();
        var listCounters = new Dictionary<(int NumberingId, int Level), int>();
        var listNumberFormats = GetListNumberFormats(
            document.MainDocumentPart);

        /*
         * Duyệt theo đúng thứ tự phần tử trong Word:
         *
         * Paragraph
         * Table
         * Paragraph
         * ...
         *
         * Không lấy toàn bộ Descendants<Text>() một lần
         * vì cách đó có thể làm mất cấu trúc bảng.
         */
        foreach (var element in
            body.ChildElements)
        {
            switch (element)
            {
                case Paragraph paragraph:
                    AppendParagraph(
                        builder,
                        paragraph,
                        listCounters,
                        listNumberFormats);
                    break;

                case Table table:
                    AppendTable(
                        builder,
                        table);
                    break;
            }
        }

        return builder.ToString();
    }

    private static void AppendParagraph(
        StringBuilder builder,
        Paragraph paragraph,
        IDictionary<(int NumberingId, int Level), int> listCounters,
        IReadOnlyDictionary<(int NumberingId, int Level), string>
            listNumberFormats)
    {
        var paragraphText =
            GetParagraphText(paragraph);

        if (string.IsNullOrWhiteSpace(
                paragraphText))
        {
            return;
        }

        var listPrefix = GetListPrefix(
            paragraph,
            listCounters,
            listNumberFormats);

        builder.AppendLine($"{listPrefix}{paragraphText}");
    }

    private static string GetListPrefix(
        Paragraph paragraph,
        IDictionary<(int NumberingId, int Level), int> listCounters,
        IReadOnlyDictionary<(int NumberingId, int Level), string>
            listNumberFormats)
    {
        var numberingProperties = paragraph
            .ParagraphProperties?
            .NumberingProperties;
        var numberingId = numberingProperties
            ?.NumberingId?
            .Val?
            .Value;

        if (!numberingId.HasValue)
        {
            return string.Empty;
        }

        var level = numberingProperties
            .NumberingLevelReference?
            .Val?
            .Value ?? 0;
        var key = (numberingId.Value, level);

        if (level == 0)
        {
            foreach (var childKey in listCounters.Keys
                .Where(item =>
                    item.NumberingId == numberingId.Value &&
                    item.Level > level)
                .ToList())
            {
                listCounters.Remove(childKey);
            }
        }

        var nextNumber = listCounters.TryGetValue(
                key,
                out var currentNumber)
            ? currentNumber + 1
            : 1;

        listCounters[key] = nextNumber;

        var numberFormat = listNumberFormats.TryGetValue(
                key,
                out var format)
            ? format
            : "decimal";

        return numberFormat switch
        {
            "upperLetter" => $"{ToAlphabeticListLabel(nextNumber)}. ",
            "lowerLetter" => $"{ToAlphabeticListLabel(nextNumber).ToLowerInvariant()}. ",
            "bullet" => "• ",
            _ => $"{nextNumber}. "
        };
    }

    private static Dictionary<(int NumberingId, int Level), string>
        GetListNumberFormats(MainDocumentPart? mainDocumentPart)
    {
        var numbering = mainDocumentPart?
            .NumberingDefinitionsPart?
            .Numbering;

        if (numbering == null)
        {
            return new Dictionary<(int NumberingId, int Level), string>();
        }

        const string wordprocessingNamespace =
            "http://schemas.openxmlformats.org/wordprocessingml/2006/main";
        var abstractNumberIds = numbering
            .Elements<NumberingInstance>()
            .Select(instance => new
            {
                NumberingId = ParseOpenXmlInteger(
                    instance.GetAttribute(
                        "numId",
                        wordprocessingNamespace).Value),
                AbstractNumberingId = ParseOpenXmlInteger(
                    instance.Elements<AbstractNumId>()
                        .FirstOrDefault()?
                        .GetAttribute(
                            "val",
                            wordprocessingNamespace).Value)
            })
            .Where(item =>
                item.NumberingId.HasValue &&
                item.AbstractNumberingId.HasValue)
            .ToDictionary(
                item => item.NumberingId!.Value,
                item => item.AbstractNumberingId!.Value);

        var result = new Dictionary<(int NumberingId, int Level), string>();

        foreach (var abstractNumber in numbering.Elements<AbstractNum>())
        {
            var abstractNumberingId = ParseOpenXmlInteger(
                abstractNumber.GetAttribute(
                    "abstractNumId",
                    wordprocessingNamespace).Value);

            if (!abstractNumberingId.HasValue)
            {
                continue;
            }

            var numberingIds = abstractNumberIds
                .Where(item => item.Value == abstractNumberingId.Value)
                .Select(item => item.Key)
                .ToList();

            foreach (var level in abstractNumber.Elements<Level>())
            {
                var levelIndex = ParseOpenXmlInteger(
                    level.GetAttribute(
                        "ilvl",
                        wordprocessingNamespace).Value);
                var numberFormat = level
                    .Elements<NumberingFormat>()
                    .FirstOrDefault()?
                    .GetAttribute(
                        "val",
                        wordprocessingNamespace).Value;

                if (!levelIndex.HasValue ||
                    string.IsNullOrWhiteSpace(numberFormat))
                {
                    continue;
                }

                foreach (var numberingId in numberingIds)
                {
                    result[(numberingId, levelIndex.Value)] =
                        numberFormat;
                }
            }
        }

        return result;
    }

    private static int? ParseOpenXmlInteger(string? value)
    {
        return int.TryParse(value, out var result)
            ? result
            : null;
    }

    private static string ToAlphabeticListLabel(int value)
    {
        var builder = new StringBuilder();

        while (value > 0)
        {
            value--;
            builder.Insert(0, (char)('A' + value % 26));
            value /= 26;
        }

        return builder.ToString();
    }
    public Task<ParsedDocumentDto>
    ExtractStructuredAsync(
        Stream stream,
        string fileName)
    {
        if (stream == null)
        {
            throw new ArgumentNullException(
                nameof(stream));
        }

        if (string.IsNullOrWhiteSpace(fileName))
        {
            throw new Exception(
                "Document file name is required");
        }

        var extension = Path
            .GetExtension(fileName)
            .ToLowerInvariant();

        if (extension != ".pdf")
        {
            throw new Exception(
                "Structured document extraction " +
                "currently supports PDF files only");
        }

        ResetStream(stream);

        var result =
            ExtractStructuredPdf(stream);

        if (result.Pages.Count == 0)
        {
            throw new Exception(
                "No readable PDF pages found");
        }

        return Task.FromResult(result);
    }
    private static ParsedDocumentDto
    ExtractStructuredPdf(
        Stream stream)
    {
        ResetStream(stream);

        var result =
            new ParsedDocumentDto
            {
                FileExtension = ".pdf"
            };

        var fullTextBuilder =
            new StringBuilder();

        using var document =
            PdfDocument.Open(stream);

        foreach (var page in
            document.GetPages())
        {
            var pageDto =
                new ParsedDocumentPageDto
                {
                    PageNumber = page.Number,
                    Width = page.Width,
                    Height = page.Height
                };

            var words = page
                .GetWords()
                .Where(word =>
                    !string.IsNullOrWhiteSpace(
                        word.Text))
                .Select(word =>
                    new ParsedDocumentWordDto
                    {
                        Text = word.Text.Trim(),

                        Left =
                            word.BoundingBox.Left,

                        Bottom =
                            word.BoundingBox.Bottom,

                        Right =
                            word.BoundingBox.Right,

                        Top =
                            word.BoundingBox.Top
                    })
                .OrderByDescending(word =>
                    word.Top)
                .ThenBy(word =>
                    word.Left)
                .ToList();

            pageDto.Words = words;

            pageDto.Text =
                BuildPageText(words);

            result.Pages.Add(pageDto);

            if (!string.IsNullOrWhiteSpace(
                    pageDto.Text))
            {
                fullTextBuilder.AppendLine(
                    $"[[PAGE:{page.Number}]]");

                fullTextBuilder.AppendLine(
                    pageDto.Text);

                fullTextBuilder.AppendLine();
            }
        }

        result.FullText =
            fullTextBuilder
                .ToString()
                .Trim();

        return result;
    }
    private static string BuildPageText(
        List<ParsedDocumentWordDto> words)
    {
        if (words.Count == 0)
        {
            return string.Empty;
        }

        const double lineTolerance = 3;

        var lines = new List<
            List<ParsedDocumentWordDto>>();

        foreach (var word in words)
        {
            var currentLine =
                lines.FirstOrDefault(line =>
                    line.Count > 0 &&
                    Math.Abs(
                        line[0].Bottom -
                        word.Bottom)
                    <= lineTolerance);

            if (currentLine == null)
            {
                currentLine =
                    new List<
                        ParsedDocumentWordDto>();

                lines.Add(currentLine);
            }

            currentLine.Add(word);
        }

        var orderedLines = lines
            .Select(CreatePdfTextLine)
            .Where(line =>
                !string.IsNullOrWhiteSpace(
                    line.Text))
            .OrderByDescending(line =>
                line.Top)
            .ToList();

        if (orderedLines.Count == 0)
        {
            return string.Empty;
        }

        var paragraphGapThreshold =
            CalculateParagraphGapThreshold(
                orderedLines);

        var paragraphs =
            new List<List<PdfTextLine>>();

        foreach (var line in orderedLines)
        {
            var currentParagraph =
                paragraphs.LastOrDefault();

            if (currentParagraph == null ||
                ShouldStartNewParagraph(
                    currentParagraph.Last(),
                    line,
                    paragraphGapThreshold))
            {
                paragraphs.Add(
                    new List<PdfTextLine>
                    {
                        line
                    });

                continue;
            }

            currentParagraph.Add(line);
        }

        return string.Join(
            "\n\n",
            paragraphs
                .Select(paragraph =>
                    string.Join(
                        " ",
                        paragraph.Select(line =>
                            line.Text)))
                .Where(text =>
                    !string.IsNullOrWhiteSpace(
                        text)))
            .Trim();
    }

    private static PdfTextLine CreatePdfTextLine(
        List<ParsedDocumentWordDto> words)
    {
        var orderedWords = words
            .OrderBy(word =>
                word.Left)
            .ToList();

        return new PdfTextLine
        {
            Text = string.Join(
                " ",
                orderedWords.Select(word =>
                    word.Text)).Trim(),

            Left = orderedWords.Min(word =>
                word.Left),

            Right = orderedWords.Max(word =>
                word.Right),

            Top = orderedWords.Max(word =>
                word.Top),

            Bottom = orderedWords.Min(word =>
                word.Bottom),

            AverageHeight = orderedWords
                .Average(word =>
                    Math.Max(word.Height, 1))
        };
    }

    private static double CalculateParagraphGapThreshold(
        List<PdfTextLine> orderedLines)
    {
        var lineHeights = orderedLines
            .Select(line =>
                Math.Max(line.AverageHeight, 1))
            .OrderBy(value =>
                value)
            .ToList();

        var medianLineHeight =
            lineHeights[lineHeights.Count / 2];

        var verticalGaps =
            orderedLines
                .Zip(
                    orderedLines.Skip(1),
                    (previous, current) =>
                        previous.Bottom - current.Top)
                .Where(gap =>
                    gap > 0)
                .OrderBy(gap =>
                    gap)
                .ToList();

        var medianGap =
            verticalGaps.Count == 0
                ? medianLineHeight * 0.25
                : verticalGaps[verticalGaps.Count / 2];

        return Math.Max(
            medianLineHeight * 0.75,
            medianGap * 2.2);
    }

    private static bool ShouldStartNewParagraph(
        PdfTextLine previous,
        PdfTextLine current,
        double paragraphGapThreshold)
    {
        var verticalGap =
            previous.Bottom - current.Top;

        if (verticalGap > paragraphGapThreshold)
        {
            return true;
        }

        var currentIndent =
            current.Left - previous.Left;

        var previousLineIsShort =
            previous.Right <
            current.Right - 40;

        return currentIndent > 18 &&
            previousLineIsShort;
    }
    private static string GetParagraphText(
        Paragraph paragraph)
    {
        var builder = new StringBuilder();

        foreach (var element in
            paragraph.Descendants())
        {
            switch (element)
            {
                case Text text:
                    builder.Append(
                        text.Text);
                    break;

                case TabChar:
                    builder.Append('\t');
                    break;

                case Break:
                case CarriageReturn:
                    builder.AppendLine();
                    break;
            }
        }

        return builder
            .ToString()
            .Trim();
    }

    private static void AppendTable(
        StringBuilder builder,
        Table table)
    {
        foreach (var row in
            table.Elements<TableRow>())
        {
            var cells = row
                .Elements<TableCell>()
                .Select(GetTableCellText)
                .Where(cellText =>
                    !string.IsNullOrWhiteSpace(
                        cellText))
                .ToList();

            if (cells.Count == 0)
            {
                continue;
            }

            /*
             * Dùng tab để phân cách các cột.
             * Sau này AI vẫn nhận diện được:
             *
             * 1    Question    A    B    C    D
             */
            builder.AppendLine(
                string.Join(
                    "\t",
                    cells));
        }

        builder.AppendLine();
    }

    private static string GetTableCellText(
        TableCell cell)
    {
        var paragraphs = cell
            .Elements<Paragraph>()
            .Select(GetParagraphText)
            .Where(text =>
                !string.IsNullOrWhiteSpace(
                    text));

        return string.Join(
            " ",
            paragraphs)
            .Trim();
    }

    private static async Task<string>
        ExtractTxtTextAsync(
            Stream stream)
    {
        ResetStream(stream);

        /*
         * detectEncodingFromByteOrderMarks = true
         * giúp đọc UTF-8 BOM và một số TXT encoding
         * thông dụng tốt hơn.
         *
         * leaveOpen = true để không đóng stream
         * do caller truyền vào.
         */
        using var reader =
            new StreamReader(
                stream,
                Encoding.UTF8,
                detectEncodingFromByteOrderMarks: true,
                bufferSize: 4096,
                leaveOpen: true);

        return await reader
            .ReadToEndAsync();
    }

    private static string NormalizeExtractedText(
        string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return string.Empty;
        }

        text = text
            .Replace("\r\n", "\n")
            .Replace("\r", "\n")
            .Replace("\f", "\n")
            .Replace('\u00A0', ' ')
            .Replace("", "•");

        var lines = text
            .Split('\n')
            .Select(line =>
                NormalizeLine(line))
            .ToList();

        var builder = new StringBuilder();
        var previousLineWasEmpty = false;

        foreach (var line in lines)
        {
            var isEmpty =
                string.IsNullOrWhiteSpace(line);

            /*
             * Không để quá nhiều dòng trống liên tục,
             * nhưng vẫn giữ một dòng trống để phân đoạn.
             */
            if (isEmpty)
            {
                if (!previousLineWasEmpty)
                {
                    builder.AppendLine();
                }

                previousLineWasEmpty = true;
                continue;
            }

            builder.AppendLine(line);
            previousLineWasEmpty = false;
        }

        return builder
            .ToString()
            .Trim();
    }

    private static string NormalizeLine(
        string line)
    {
        if (string.IsNullOrWhiteSpace(line))
        {
            return string.Empty;
        }

        line = line
            .Replace("\t", " ")
            .Trim();

        while (line.Contains("  "))
        {
            line = line.Replace(
                "  ",
                " ");
        }

        return line;
    }

    private class PdfTextLine
    {
        public string Text { get; set; } = string.Empty;

        public double Left { get; set; }

        public double Right { get; set; }

        public double Top { get; set; }

        public double Bottom { get; set; }

        public double AverageHeight { get; set; }
    }

    private static void ResetStream(
        Stream stream)
    {
        if (stream.CanSeek)
        {
            stream.Position = 0;
        }
    }
}
