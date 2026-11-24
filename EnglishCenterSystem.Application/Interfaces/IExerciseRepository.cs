using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using EnglishCenterSystem.Domain.Entities.Exercises;

namespace EnglishCenterSystem.Application.Interfaces;

public interface IExerciseRepository
{
    IQueryable<Exercise> Query();

    Task<List<string>> GetListeningObjectNamesAsync(
        int exerciseId,
        int teacherUserId);

    Task HardDeleteAsync(
    int exerciseId,
    int teacherUserId);
    Task<Exercise?> GetByIdAsync(int exerciseId);
    IQueryable<Exercise> QueryBasic();
    Task AddAsync(Exercise exercise);

    void Remove(Exercise exercise);
}
