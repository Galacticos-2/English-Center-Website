using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EnglishCenterSystem.Domain.Enums;
using EnglishCenterSystem.Domain.Common;

namespace EnglishCenterSystem.Domain.Entities.Writing;

public class WritingSubmission : BaseEntity
{
    public int WritingSubmissionId { get; set; }

    public int ExerciseAttemptId { get; set; }

    public int WritingTaskId { get; set; }

    public int StudentUserId { get; set; }

    public string EssayText { get; set; } = null!;

    public WritingSubmissionStatus Status { get; set; }
        = WritingSubmissionStatus.Pending;
}
