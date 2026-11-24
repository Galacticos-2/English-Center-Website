using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using EnglishCenterSystem.Domain.Entities;

namespace EnglishCenterSystem.Application.Interfaces;

public interface IClassRoomRepository
{
    IQueryable<ClassRoom> Query();
    Task<ClassRoom?> GetByIdAsync(int id);
    Task AddAsync(ClassRoom classRoom);
}
