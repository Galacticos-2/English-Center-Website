using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EnglishCenterSystem.Domain.Enums;

public enum BackgroundJobStatus
{
    Pending = 1,
    Processing = 2,
    Completed = 3,
    Failed = 4
}