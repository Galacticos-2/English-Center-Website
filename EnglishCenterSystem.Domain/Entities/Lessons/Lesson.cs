using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using EnglishCenterSystem.Domain.Common;

namespace EnglishCenterSystem.Domain.Entities;

public class Lesson : BaseEntity
{
    public int LessonId { get; set; }

    public string Title { get; set; }
        = null!;

    public int LessonSectionId
    {
        get; set;
    }

    public int OrderIndex
    {
        get; set;
    }

    public LessonSection Section
    {
        get; set;
    }
        = null!;

    public LessonVideo? Video
    {
        get; set;
    }
    
    public LessonDocument? Document
    {
        get; set;
    }
    public List<StudentLessonProgress> StudentProgresses { get; set; } = new();
}