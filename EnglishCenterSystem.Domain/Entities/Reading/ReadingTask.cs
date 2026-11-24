using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using EnglishCenterSystem.Domain.Common;
using EnglishCenterSystem.Domain.Enums;

namespace EnglishCenterSystem.Domain.Entities.Reading;

public class ReadingTask : BaseEntity
{
    public int ReadingTaskId { get; set; }

    public int TeacherUserId { get; set; }
    public ICollection<ReadingPassageImage>
    PassageImages
    { get; set; }
        = new List<ReadingPassageImage>();
    public string Title { get; set; } = null!;

    public string? Instruction { get; set; }

    public string PassageText { get; set; } = null!;

    public CourseType CourseType { get; set; }

   

    public bool IsActive { get; set; } = true;

    public User TeacherUser { get; set; } = null!;

    public ICollection<ReadingQuestion> Questions { get; set; }
        = new List<ReadingQuestion>();
}