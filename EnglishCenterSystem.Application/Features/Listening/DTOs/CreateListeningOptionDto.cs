using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EnglishCenterSystem.Application.Features.Listening.DTOs;

public class CreateListeningOptionDto
{
    public string Content { get; set; } = null!;

    public bool IsCorrect { get; set; }

    public int OrderIndex { get; set; }
}