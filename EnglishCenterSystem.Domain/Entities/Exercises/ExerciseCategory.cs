using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using EnglishCenterSystem.Domain.Common;

namespace EnglishCenterSystem.Domain.Entities.Exercises;

public class ExerciseCategory : BaseEntity
{
    public int ExerciseCategoryId { get; set; }

    public string Name { get; set; } = null!;

    public int OrderIndex { get; set; }

    public bool IsActive { get; set; } = true;

    public ICollection<ExerciseSection> Sections { get; set; }
        = new List<ExerciseSection>();
}