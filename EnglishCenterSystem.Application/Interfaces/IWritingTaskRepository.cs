using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using EnglishCenterSystem.Domain.Entities.Writing;

namespace EnglishCenterSystem.Application.Interfaces;

public interface IWritingTaskRepository
{
    IQueryable<WritingTask> Query();

    Task<WritingTask?> GetByIdAsync(int writingTaskId);

    Task AddAsync(WritingTask writingTask);

    void Remove(WritingTask writingTask);
}
