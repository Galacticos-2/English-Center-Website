namespace EnglishCenterSystem.Application
    .Features.Documents.DTOs;

public class ParsedDocumentDto
{
    public string FileExtension { get; set; }
        = string.Empty;

    public string FullText { get; set; }
        = string.Empty;

    public List<ParsedDocumentPageDto> Pages { get; set; }
        = new();
}

public class ParsedDocumentPageDto
{
    public int PageNumber { get; set; }

    public double Width { get; set; }

    public double Height { get; set; }

    public string Text { get; set; }
        = string.Empty;

    public List<ParsedDocumentWordDto> Words { get; set; }
        = new();
}

public class ParsedDocumentWordDto
{
    public string Text { get; set; }
        = string.Empty;

    public double Left { get; set; }

    public double Bottom { get; set; }

    public double Right { get; set; }

    public double Top { get; set; }

    public double Width =>
        Right - Left;

    public double Height =>
        Top - Bottom;
}