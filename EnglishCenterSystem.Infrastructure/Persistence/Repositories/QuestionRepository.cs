using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using EnglishCenterSystem.Application.Interfaces;
using EnglishCenterSystem.Domain.Entities.Exercises;
using EnglishCenterSystem.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace EnglishCenterSystem.Infrastructure.Persistence.Repositories;

public class QuestionRepository : IQuestionRepository
{
    private readonly AppDbContext _context;

    public QuestionRepository(AppDbContext context)
    {
        _context = context;
    }
    

   

    

  
    public IQueryable<Question> Query()
    {
        return _context.Questions
            .Include(x => x.QuestionCategory)
           
            .Include(x => x.Options)
            .Include(x => x.TeacherUser);
    }

    public IQueryable<QuestionCategory> QueryCategories()
    {
        return _context.QuestionCategories;
    }

    public async Task<Question?> GetByIdAsync(int questionId)
    {
        return await _context.Questions
            .Include(x => x.Options)
            .Include(x => x.QuestionCategory)
            .FirstOrDefaultAsync(x => x.QuestionId == questionId);
    }

    public async Task<QuestionCategory?> GetCategoryByIdAsync(
        int questionCategoryId)
    {
        return await _context.QuestionCategories
            .FirstOrDefaultAsync(x =>
                x.QuestionCategoryId == questionCategoryId);
    }

    public async Task AddAsync(Question question)
    {
        await _context.Questions.AddAsync(question);
    }

    public async Task AddCategoryAsync(QuestionCategory category)
    {
        await _context.QuestionCategories.AddAsync(category);
    }

    public void Remove(Question question)
    {
        question.IsDeleted = true;
        question.IsActive = false;
    }

    public void RemoveCategory(QuestionCategory category)
    {
        category.IsDeleted = true;
        category.IsActive = false;
    }
}