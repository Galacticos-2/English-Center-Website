using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using EnglishCenterSystem.Domain.Common;
using EnglishCenterSystem.Domain.Enums;

namespace EnglishCenterSystem.Domain.Entities.BackgroundJobs;

public class SystemBackgroundJob : BaseEntity
{
    public int SystemBackgroundJobId { get; set; }

    public string? HangfireJobId { get; set; }

    public BackgroundJobType JobType { get; set; }

    public BackgroundJobStatus Status { get; set; } =
        BackgroundJobStatus.Pending;

    public int CreatedByUserId { get; set; }

    public string PayloadJson { get; set; } = null!;

    public string? ResultJson { get; set; }

    public string? ErrorMessage { get; set; }

    public DateTime? StartedAt { get; set; }

    public DateTime? CompletedAt { get; set; }
}