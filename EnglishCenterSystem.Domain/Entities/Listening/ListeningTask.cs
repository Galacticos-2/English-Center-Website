using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using EnglishCenterSystem.Domain.Common;
using EnglishCenterSystem.Domain.Enums;

namespace EnglishCenterSystem.Domain.Entities.Listening;

public class ListeningTask : BaseEntity
{
    public int ListeningTaskId { get; set; }

    public int TeacherUserId { get; set; }

    public string Title { get; set; } = null!;

    public string? Instruction { get; set; }
    public string? PartTitle { get; set; }

    public string? Direction { get; set; }
    public string? LittlePart { get; set; }

    public string AudioUrl { get; set; } = null!;
    public string? SourceQuestionFileUrl { get; set; }

    public string? SourceQuestionFileObjectName { get; set; }

    public string? AudioObjectName { get; set; }

    public string? Transcript { get; set; }

    public CourseType CourseType { get; set; }

   

    public bool IsActive { get; set; } = true;

    public User TeacherUser { get; set; } = null!;

    public ICollection<ListeningQuestion> Questions { get; set; }
        = new List<ListeningQuestion>();
}
