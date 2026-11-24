using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using EnglishCenterSystem.Domain.Entities;

namespace EnglishCenterSystem.Application.Interfaces;

public interface IUserRepository
{
    Task<User?> GetByEmailAsync(string email);
    Task<User?> GetByUserNameAsync(string userName);
    Task<User?> GetByIdAsync(int id);
    Task AddAsync(User user);
    IQueryable<User> Query();
    void Remove(User user);
    Task<User?> GetProfileByIdAsync(int id);
    Task<bool> ExistsByEmailAsync(string email);
}
