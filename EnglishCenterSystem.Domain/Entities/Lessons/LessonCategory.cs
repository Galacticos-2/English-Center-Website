using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using EnglishCenterSystem.Domain.Common;

namespace EnglishCenterSystem.Domain.Entities;

public class LessonCategory : BaseEntity
{
    public int LessonCategoryId { get; set; }

    public string Name { get; set; }
        = null!;

    public ICollection<LessonSection> Sections { get; set; }
    = new List<LessonSection>();
}