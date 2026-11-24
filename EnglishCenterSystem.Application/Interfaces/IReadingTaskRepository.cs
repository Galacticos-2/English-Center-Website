using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using EnglishCenterSystem.Domain.Entities.Reading;

namespace EnglishCenterSystem.Application.Interfaces;

public interface IReadingTaskRepository
{
    IQueryable<ReadingTask> Query();

    Task<ReadingTask?> GetByIdAsync(int readingTaskId);

    Task AddAsync(ReadingTask readingTask);

    void Remove(ReadingTask readingTask);
}