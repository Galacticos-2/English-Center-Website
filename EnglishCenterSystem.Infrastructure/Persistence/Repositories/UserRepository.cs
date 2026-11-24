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

public class UserRepository : IUserRepository
{
    private readonly AppDbContext _context;

    public UserRepository(AppDbContext context)
    {
        _context = context;
    }
    public async Task<User?> GetByUserNameAsync(string userName)
    {
        return await _context.Users
            .FirstOrDefaultAsync(x => x.UserName == userName);
    }
    public IQueryable<User> Query()
    {
        return _context.Users

            .Include(x => x.StudentRole)
                .ThenInclude(x => x.ClassRoom)
                    .ThenInclude(x => x.Course)

            .Include(x => x.StudentRole)
                .ThenInclude(x => x.Course)

            .Include(x => x.TeacherRole)
                .ThenInclude(x => x.TeacherClasses)
                    .ThenInclude(x => x.ClassRoom)
                        .ThenInclude(x => x.Course)

            .AsQueryable();
    }
    public async Task<User?> GetByEmailAsync(string email)
    {
        return await _context.Users
            .FirstOrDefaultAsync(x => x.Email == email);
    }
    public async Task<User?> GetProfileByIdAsync(int id)
    {
        return await _context.Users
            .Include(x => x.StudentRole)
                .ThenInclude(x => x.ClassRoom)
                    .ThenInclude(x => x.Course)
            .Include(x => x.StudentRole)
                .ThenInclude(x => x.Course)
            .Include(x => x.TeacherRole)
                .ThenInclude(x => x.TeacherClasses)
                    .ThenInclude(x => x.ClassRoom)
                        .ThenInclude(x => x.Course)
            .FirstOrDefaultAsync(x => x.UserId == id);
    }

    public async Task AddAsync(User user)
    {
        await _context.Users.AddAsync(user);
    }
    public void Remove(User user)
    {
        _context.Users.Remove(user);
    }
    public async Task<bool> ExistsByEmailAsync(string email)
    {
        return await _context.Users
            .AnyAsync(x => x.Email == email);
    }
    public async Task<User?>
GetByIdAsync(int id)
    {
        return await _context.Users
            .FirstOrDefaultAsync(x =>
                x.UserId == id);
    }
}
