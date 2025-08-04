using System;
using System.Collections.Generic;
using MinistryTracker.Models.Enums;

namespace MinistryTracker.Utilities
{
    public static class VisitTypeValues
    {
        public static List<VisitType> All { get; } =
            new List<VisitType>((VisitType[])Enum.GetValues(typeof(VisitType)));
    }
}
