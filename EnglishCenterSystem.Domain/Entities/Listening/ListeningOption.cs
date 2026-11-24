using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using EnglishCenterSystem.Domain.Common;

namespace EnglishCenterSystem.Domain.Entities.Listening;

public class ListeningOption : BaseEntity
{
    public int ListeningOptionId { get; set; }

    public int ListeningQuestionId { get; set; }

    public string Content { get; set; } = null!;

    public bool IsCorrect { get; set; }

    public int OrderIndex { get; set; }

    public ListeningQuestion ListeningQuestion { get; set; } = null!;
}