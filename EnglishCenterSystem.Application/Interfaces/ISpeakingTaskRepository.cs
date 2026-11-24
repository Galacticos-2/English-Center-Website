using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using EnglishCenterSystem.Domain.Entities.Speaking;

namespace EnglishCenterSystem.Application.Interfaces;

public interface ISpeakingTaskRepository
{
    IQueryable<SpeakingTask> Query();

    Task<SpeakingTask?> GetByIdAsync(int speakingTaskId);

    Task AddAsync(SpeakingTask speakingTask);

    void Remove(SpeakingTask speakingTask);
}
