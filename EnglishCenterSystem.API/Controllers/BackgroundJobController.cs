using EnglishCenterSystem.Application.Common.Models;
using EnglishCenterSystem.Infrastructure.Persistence.Context;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace EnglishCenterSystem.API.Controllers;

[ApiController]
[Route("api/background-jobs")]
public class BackgroundJobController : ControllerBase
{
    private readonly AppDbContext _context;

    public BackgroundJobController(AppDbContext context)
    {
        _context = context;
    }

    [Authorize]
    [HttpGet("{id}")]
    public async Task<IActionResult> GetJob(int id)
    {
        var currentUserId = int.Parse(
            User.FindFirst(
                ClaimTypes.NameIdentifier)!
                .Value);

        var isAdmin =
            User.IsInRole("Admin");

        var job = await _context.SystemBackgroundJobs
            .Where(x =>
                x.SystemBackgroundJobId == id &&
                (isAdmin ||
                 x.CreatedByUserId == currentUserId))
            .Select(x => new
            {
                x.SystemBackgroundJobId,
                x.JobType,
                x.Status,
                x.ResultJson,
                x.ErrorMessage,
                x.CreatedAt,
                x.StartedAt,
                x.CompletedAt
            })
            .FirstOrDefaultAsync();

        if (job == null)
        {
            return NotFound(ApiResponse<object>.FailResponse(
                "Background job not found"));
        }

        return Ok(ApiResponse<object>.SuccessResponse(
            job,
            "Background job status"));
    }
}
