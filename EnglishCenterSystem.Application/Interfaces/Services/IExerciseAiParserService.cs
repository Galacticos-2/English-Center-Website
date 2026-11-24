using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using EnglishCenterSystem.Application.Features.Exercises.DTOs;

namespace EnglishCenterSystem.Application.Interfaces.Services;

public interface IExerciseAiParserService
{
    Task<AiParsedExerciseDto> ParseQuestionsAsync(
        string rawText);
}