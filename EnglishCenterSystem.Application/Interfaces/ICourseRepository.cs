using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using EnglishCenterSystem.Domain.Entities;

namespace EnglishCenterSystem.Application.Interfaces;

public interface ICourseRepository
{
    IQueryable<Course> Query();
    Task<Course?> GetByIdAsync(int id);

    void Remove(Course course);
    Task AddAsync(Course course);
}
