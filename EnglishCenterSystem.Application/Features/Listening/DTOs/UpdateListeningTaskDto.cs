using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using EnglishCenterSystem.Domain.Enums;

namespace EnglishCenterSystem.Application.Features.Listening.DTOs;

public class UpdateListeningTaskDto
{
    public string Title { get; set; } = null!;

    public string? Instruction { get; set; }

    public string? Transcript { get; set; }

    public CourseType CourseType { get; set; }

  

    public bool IsActive { get; set; } = true;

    public List<CreateListeningQuestionDto> Questions { get; set; }
        = new();
}