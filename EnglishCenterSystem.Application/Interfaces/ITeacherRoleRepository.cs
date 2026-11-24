using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using EnglishCenterSystem.Domain.Entities;

namespace EnglishCenterSystem.Application.Interfaces;

public interface ITeacherRoleRepository
{
    IQueryable<TeacherRole> Query();
    void Remove(TeacherRole teacherRole);
    Task AddAsync(
        TeacherRole teacherRole);
}