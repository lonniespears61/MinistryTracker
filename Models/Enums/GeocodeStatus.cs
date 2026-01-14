namespace MinistryTracker.Models.Enums
{
    public enum GeocodeStatus
    {
        None = 0,     // Never attempted
        Success = 1,  // We successfully derived coords/address
        Failed = 2    // We tried and it failed (likely needs better input or retry later)
    }
}
