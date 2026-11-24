using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EnglishCenterSystem.Domain.Enums;

public enum ExerciseAttemptStatus
{
    InProgress = 1,
    Submitted = 2,
    PendingReview = 3,
    Reviewed = 4,
    PendingAiGrading = 5,
    AiGrading = 6,
    AiGraded = 7,
    AiGradingFailed = 8
}