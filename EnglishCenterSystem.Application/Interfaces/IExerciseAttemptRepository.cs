using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using EnglishCenterSystem.Domain.Entities.Exercises;

namespace EnglishCenterSystem.Application.Interfaces;

public interface IExerciseAttemptRepository
{
    IQueryable<ExerciseAttempt> Query();
    IQueryable<ExerciseAttempt> QueryBasic();
    Task<ExerciseAttempt?> GetByIdAsync(int exerciseAttemptId);
    void RemoveRange(List<ExerciseAttempt> attempts);
    Task AddAsync(ExerciseAttempt attempt);
}
