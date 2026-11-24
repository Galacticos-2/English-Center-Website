using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using EnglishCenterSystem.Domain.Entities.Exercises;

namespace EnglishCenterSystem.Application.Interfaces;

public interface IExerciseAssignmentRepository
{
    IQueryable<ExerciseAssignment> Query();

    Task<ExerciseAssignment?> GetByIdAsync(int exerciseAssignmentId);

    Task AddAsync(ExerciseAssignment assignment);

    void Remove(ExerciseAssignment assignment);
}