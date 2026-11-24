using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EnglishCenterSystem.Application.Interfaces.Services;

public interface IVideoProcessingService
{
    Task ConvertToHlsAsync(
        string inputPath,
        string outputFolder);
}