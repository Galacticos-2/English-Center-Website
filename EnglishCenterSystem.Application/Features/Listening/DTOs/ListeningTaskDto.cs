using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using EnglishCenterSystem.Domain.Enums;

namespace EnglishCenterSystem.Application.Features.Listening.DTOs;

public class ListeningTaskDto
{
    public int ListeningTaskId { get; set; }

    public int TeacherUserId { get; set; }

    public string TeacherName { get; set; } = null!;

    public string Title { get; set; } = null!;

    public string? Instruction { get; set; }

    public string? AudioUrl { get; set; }

    public string? Transcript { get; set; }

    public CourseType CourseType { get; set; }



    public bool IsActive { get; set; }

    public int TotalQuestions { get; set; }

    public List<ListeningQuestionDto> Questions { get; set; }
        = new();
}