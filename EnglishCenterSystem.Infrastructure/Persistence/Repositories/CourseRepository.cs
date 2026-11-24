using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using EnglishCenterSystem.Application.Interfaces;

using EnglishCenterSystem.Domain.Entities;

using EnglishCenterSystem.Infrastructure
.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace EnglishCenterSystem.Infrastructure
.Persistence.Repositories;

public class CourseRepository
    : ICourseRepository
{
    private readonly AppDbContext _context;

    public CourseRepository(
        AppDbContext context)
    {
        _context = context;
    }

    public IQueryable<Course> Query()
    {
        return _context.Courses
            .Include(x => x.Classes)
                .ThenInclude(x => x.Students)

            .Include(x => x.Classes)
                .ThenInclude(x => x.TeacherClasses)
                    .ThenInclude(x => x.TeacherRole)
                        .ThenInclude(x => x.User)

            .AsQueryable();
    }
    public async Task<Course?>
    GetByIdAsync(int id)
    {
        return await _context.Courses
            .Include(x => x.Classes)
            .FirstOrDefaultAsync(x =>
                x.CourseId == id);
    }

    public void Remove(Course course)
    {
        _context.Courses.Remove(course);
    }
    public async Task AddAsync(
        Course course)
    {
        await _context.Courses
            .AddAsync(course);
    }
}
