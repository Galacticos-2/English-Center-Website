using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using EnglishCenterSystem.Domain.Common;

namespace EnglishCenterSystem.Domain.Entities.Listening;

public class ListeningQuestion : BaseEntity
{
    public int ListeningQuestionId { get; set; }

    public int ListeningTaskId { get; set; }

    public string Content { get; set; } = null!;

    public string? PromptImageUrl { get; set; }

    public string? PromptImageObjectName { get; set; }

    public int OrderIndex { get; set; }


    public double Point { get; set; } = 1;

    public string? Explanation { get; set; }

    public ListeningTask ListeningTask { get; set; } = null!;

    public ICollection<ListeningOption> Options { get; set; }
        = new List<ListeningOption>();
}
