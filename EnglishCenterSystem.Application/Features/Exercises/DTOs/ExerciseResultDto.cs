using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EnglishCenterSystem.Application.Features.Exercises.DTOs;

public class ExerciseResultDto
{
    public int ExerciseAttemptId { get; set; }

    public string ExerciseTitle { get; set; } = null!;

    public int TotalQuestions { get; set; }

    public int CorrectAnswers { get; set; }

    public double TotalPoints { get; set; }
    public string Status { get; set; } = null!;

    public double Score { get; set; }
    public List<int> PendingWritingSubmissionIds { get; set; }
    = new();

    public DateTime StartedAt { get; set; }

    public DateTime SubmittedAt { get; set; }

    public List<ExerciseAnswerResultDto> Answers { get; set; }
        = new();
}
