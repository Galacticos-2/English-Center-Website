using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using EnglishCenterSystem.Application.Features.Writing.DTOs;
using EnglishCenterSystem.Domain.Entities.Writing;

namespace EnglishCenterSystem.Application.Interfaces.Services;

public interface IWritingAiEvaluationService
{
    Task<WritingAiScoreDto> EvaluateAsync(
        WritingTask writingTask,
        string essayText);
}
