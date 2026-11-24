using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using EnglishCenterSystem.Application.Features.Speaking.DTOs;

namespace EnglishCenterSystem.Application.Interfaces.Services;

public interface ISpeechAssessmentService
{
    Task<SpeakingAssessmentResultDto> AssessAsync(
        Stream audioStream,
        string fileName,
        string? referenceText);
}
