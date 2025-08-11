using MinistryTracker.Models.Enums;
using MinistryTracker.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MinistryTracker.ViewModels
{
    public static class InitialCallTypeValues
    {
        public static List<InitialCallType> All { get; } = new(Enum.GetValues<InitialCallType>());
    }
}
