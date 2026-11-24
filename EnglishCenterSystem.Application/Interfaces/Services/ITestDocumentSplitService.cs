using EnglishCenterSystem.Application.Features.Tests.DTOs;
using EnglishCenterSystem.Domain.Enums;

namespace EnglishCenterSystem.Application.Interfaces.Services;

public interface ITestDocumentSplitService
{
    Task<TestDocumentSplitResultDto> SplitAsync(
        string rawText,
        TestType testType);
}