using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using EnglishCenterSystem.Domain.Entities.Writing;

namespace EnglishCenterSystem.Application.Interfaces;

public interface IWritingScoreRepository
{
    IQueryable<WritingScore> Query();

    Task AddAsync(WritingScore score);
}
