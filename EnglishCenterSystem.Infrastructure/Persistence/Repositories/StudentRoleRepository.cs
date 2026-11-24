using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using EnglishCenterSystem.Application.Interfaces;
using EnglishCenterSystem.Domain.Entities;
using EnglishCenterSystem.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace EnglishCenterSystem.Infrastructure.Persistence.Repositories;

public class StudentRoleRepository
    : IStudentRoleRepository
{
    private readonly AppDbContext _context;

    public StudentRoleRepository(
        AppDbContext context)
    {
        _context = context;
    }

    public IQueryable<StudentRole> Query()
    {
        return _context.StudentRoles
            .Include(x => x.ClassRoom)
            .AsQueryable();
    }
    public void Remove(
    StudentRole studentRole)
    {
        _context.StudentRoles
            .Remove(studentRole);
    }
    public async Task AddAsync(
        StudentRole studentRole)
    {
        await _context.StudentRoles
            .AddAsync(studentRole);
    }
}