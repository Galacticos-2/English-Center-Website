using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using EnglishCenterSystem.Domain.Common;

namespace EnglishCenterSystem.Domain.Entities;

public class LessonVideo : BaseEntity
{
    public int LessonVideoId { get; set; }

    public int LessonId { get; set; }

    public string OriginalUrl
    {
        get; set;
    }
        = null!;

    public string HlsUrl
    {
        get; set;
    }
        = null!;

    public string ThumbnailUrl
    {
        get; set;
    }
        = null!;

    public int DurationSeconds
    {
        get; set;
    }
    public string ProcessingStatus { get; set; }
    = "Processing";

    public string? ErrorMessage { get; set; }
    public Lesson Lesson { get; set; }
        = null!;
}