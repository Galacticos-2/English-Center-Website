using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EnglishCenterSystem.Application.Features.Lessons.DTOs;

public class LessonDto
{
    public int LessonId { get; set; }

    public string Title { get; set; } = null!;

    public int OrderIndex { get; set; }

    public string? VideoUrl { get; set; }

    public string? PdfUrl { get; set; }
    public string? VideoProcessingStatus { get; set; }

    public string? VideoErrorMessage { get; set; }

    public string? HtmlContent { get; set; }
    public bool IsCompleted { get; set; }
}