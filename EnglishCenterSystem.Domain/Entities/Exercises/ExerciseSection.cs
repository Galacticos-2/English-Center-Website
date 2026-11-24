using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using EnglishCenterSystem.Domain.Common;

namespace EnglishCenterSystem.Domain.Entities.Exercises;

public class ExerciseSection : BaseEntity
{
    public int ExerciseSectionId { get; set; }

    public string Title { get; set; } = null!;

    public int CourseId { get; set; }

    public int ClassRoomId { get; set; }

    public int ExerciseCategoryId { get; set; }

    public int TeacherUserId { get; set; }

    public bool IsActive { get; set; } = true;

    public Course Course { get; set; } = null!;

    public ClassRoom ClassRoom { get; set; } = null!;

    public ExerciseCategory Category { get; set; } = null!;

    public User TeacherUser { get; set; } = null!;

    public ICollection<Exercise> Exercises { get; set; }
        = new List<Exercise>();
}
