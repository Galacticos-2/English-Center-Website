using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EnglishCenterSystem.Application.Features.Listening.DTOs;

public class CreateListeningQuestionDto
{
    public string Content { get; set; } = null!;

    public string? Explanation { get; set; }

    public int OrderIndex { get; set; }

  
    public double Point { get; set; } = 1;

    public List<CreateListeningOptionDto> Options { get; set; }
        = new();
}
