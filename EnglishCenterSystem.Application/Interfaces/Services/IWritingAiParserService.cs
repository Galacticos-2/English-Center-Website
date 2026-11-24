using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using EnglishCenterSystem.Application.Features.Writing.DTOs;

namespace EnglishCenterSystem.Application.Interfaces.Services;

public interface IWritingAiParserService
{
    Task<AiParsedWritingTaskDto> ParseWritingTaskAsync(string rawText);
}
