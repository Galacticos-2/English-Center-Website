using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EnglishCenterSystem.Domain.Enums;

public enum BackgroundJobType
{
    ListeningImport = 1,
    SpeakingImport = 2,
    ReadingImport = 3,
    WritingGrading = 4,
    SpeakingGrading = 5,
    TestImport = 6,
    TestWritingGrading = 7,
    TestSpeakingGrading = 8,
    CompetitionTestImport = 9
}