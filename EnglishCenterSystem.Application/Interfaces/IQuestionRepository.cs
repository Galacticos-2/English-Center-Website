using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using EnglishCenterSystem.Domain.Entities.Exercises;

namespace EnglishCenterSystem.Application.Interfaces;

public interface IQuestionRepository
{
    IQueryable<Question> Query();

    IQueryable<QuestionCategory> QueryCategories();

    Task<Question?> GetByIdAsync(int questionId);

    Task<QuestionCategory?> GetCategoryByIdAsync(int questionCategoryId);

    Task AddAsync(Question question);

    Task AddCategoryAsync(QuestionCategory category);
    

    void Remove(Question question);

    void RemoveCategory(QuestionCategory category);
}