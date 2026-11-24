using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using EnglishCenterSystem.Domain.Entities.Listening;

namespace EnglishCenterSystem.Application.Interfaces;

public interface IListeningTaskRepository
{
    IQueryable<ListeningTask> Query();

    Task<ListeningTask?> GetByIdAsync(int listeningTaskId);

    Task AddAsync(ListeningTask listeningTask);

    void Remove(ListeningTask listeningTask);
}
