using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EnglishCenterSystem.Application.Features.Writing.DTOs;

public class SubmitWritingDto
{
    public int WritingTaskId { get; set; }

    public int ExerciseAttemptId { get; set; }

    public string EssayText { get; set; } = null!;
}
