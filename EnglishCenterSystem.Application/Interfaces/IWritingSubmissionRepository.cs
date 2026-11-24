using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using EnglishCenterSystem.Domain.Entities.Writing;

namespace EnglishCenterSystem.Application.Interfaces;

public interface IWritingSubmissionRepository
{
    IQueryable<WritingSubmission> Query();

    Task AddAsync(WritingSubmission submission);
}