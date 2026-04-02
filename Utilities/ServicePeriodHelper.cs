using MinistryTracker.Models.Enums;
using System;

namespace MinistryTracker.Utilities
{
    public static class ServicePeriodHelper
    {
        public static TimeSpan ToTime(ServicePeriod period)
        {
            return period switch
            {
                ServicePeriod.Morning => new TimeSpan(10, 0, 0),
                ServicePeriod.Afternoon => new TimeSpan(13, 0, 0),
                ServicePeriod.Evening => new TimeSpan(18, 0, 0),
                _ => new TimeSpan(10, 0, 0) // fallback
            };
        }
    }
}