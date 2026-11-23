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

public class ClassRoomRepository
    : IClassRoomRepository
{
    private readonly AppDbContext _context;

    public ClassRoomRepository(
        AppDbContext context)
    {
        _context = context;
    }

    public IQueryable<ClassRoom> Query()
    {
        return _context.ClassRooms
            .AsQueryable();
    }
    public async Task<ClassRoom?> GetByIdAsync(int id)
    {
        return await _context.ClassRooms
            .FirstOrDefaultAsync(x => x.ClassRoomId == id);
    }
    public async Task AddAsync(
        ClassRoom classRoom)
    {
        await _context.ClassRooms
            .AddAsync(classRoom);
    }
}