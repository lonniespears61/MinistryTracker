namespace MinistryTracker.Models.DTOs
{
    public sealed class VisitWithStudent
    {
        public required Visit Visit { get; init; }
        public required Student Student { get; init; }
    }
}
