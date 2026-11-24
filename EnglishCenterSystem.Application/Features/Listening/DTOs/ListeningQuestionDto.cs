using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EnglishCenterSystem.Application.Features.Listening.DTOs;

public class ListeningQuestionDto
{
    public int ListeningQuestionId { get; set; }

    public string Content { get; set; } = null!;

    public string? PromptImageUrl { get; set; }

    public string? Explanation { get; set; }

    public int OrderIndex { get; set; }

    public double Point { get; set; }


    public List<ListeningOptionDto> Options { get; set; }
        = new();
}
