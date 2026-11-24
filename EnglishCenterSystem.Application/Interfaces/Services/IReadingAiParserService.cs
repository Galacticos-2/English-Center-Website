using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using EnglishCenterSystem.Application.Features.Reading.DTOs;

namespace EnglishCenterSystem.Application.Interfaces.Services;

public interface IReadingAiParserService
{
    Task<AiParsedReadingExerciseDto> ParseReadingAsync(
        string rawText);
}