using System.Collections.Generic;
using MinistryTracker.Models;

namespace MinistryTracker.Models.Enums
{
    public static class InitialCallTypeValues
    {
        // Keep your intentional display order
        public static List<InitialCallType> All { get; } = new()
        {
            InitialCallType.HouseToHouse,
            InitialCallType.Cart,
            InitialCallType.SMPW,
            InitialCallType.Phone,
            InitialCallType.Letter,
            InitialCallType.Other
        };
    }
}
