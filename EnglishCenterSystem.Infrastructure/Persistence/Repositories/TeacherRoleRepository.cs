using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using EnglishCenterSystem.Application.Interfaces;

using EnglishCenterSystem.Domain.Entities;

using EnglishCenterSystem.Infrastructure
.Persistence.Context;
using Microsoft.EntityFrameworkCore;
namespace EnglishCenterSystem.Infrastructure
.Persistence.Repositories;

public class TeacherRoleRepository
    : ITeacherRoleRepository
{
    private readonly AppDbContext _context;

    public TeacherRoleRepository(
        AppDbContext context)
    {
        _context = context;
    }
    public void Remove(
    TeacherRole teacherRole)
    {
        _context.TeacherRoles
            .Remove(teacherRole);
    }
    public IQueryable<TeacherRole> Query()
    {
        return _context.TeacherRoles
            .Include(x => x.TeacherClasses)
                .ThenInclude(x => x.ClassRoom)
                    .ThenInclude(x => x.Course)
            .AsQueryable();
    }

    public async Task AddAsync(
        TeacherRole teacherRole)
    {
        await _context.TeacherRoles
            .AddAsync(teacherRole);
    }
}