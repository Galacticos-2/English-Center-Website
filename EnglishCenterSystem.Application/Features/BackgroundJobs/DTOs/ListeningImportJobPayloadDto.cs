using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using EnglishCenterSystem.Application.Features.Exercises.DTOs;

namespace EnglishCenterSystem.Application.Features.BackgroundJobs.DTOs;

public class ListeningImportJobPayloadDto
{
    public int TeacherUserId { get; set; }

    public string QuestionFilePath { get; set; } = null!;

    public string QuestionFileName { get; set; } = null!;

    public string QuestionCountsJson { get; set; } = "[]";

    public ImportListeningExerciseDto ImportDto { get; set; } = null!;

    public List<ListeningImportAudioFileDto> AudioFiles { get; set; } = new();
}

public class ListeningImportAudioFileDto
{
    public string FilePath { get; set; } = null!;

    public string FileName { get; set; } = null!;

    public string ContentType { get; set; } = null!;
}