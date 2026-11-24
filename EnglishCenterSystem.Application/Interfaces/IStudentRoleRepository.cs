using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using EnglishCenterSystem.Domain.Entities;

namespace EnglishCenterSystem.Application.Interfaces;

public interface IStudentRoleRepository
{
    IQueryable<StudentRole> Query();
    void Remove(StudentRole studentRole);
    Task AddAsync(StudentRole studentRole);
}