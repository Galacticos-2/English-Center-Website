using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using EnglishCenterSystem.Domain.Common;

namespace EnglishCenterSystem.Domain.Entities;

public class LessonDocument : BaseEntity
{
    public int LessonDocumentId
    {
        get; set;
    }

    public int LessonId { get; set; }

    public string PdfUrl
    {
        get; set;
    }
        = null!;

    public string? HtmlContent { get; set; }

    public Lesson Lesson { get; set; }
        = null!;
}