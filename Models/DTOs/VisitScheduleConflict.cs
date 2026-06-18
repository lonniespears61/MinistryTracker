namespace MinistryTracker.Models.DTOs;

public enum VisitScheduleConflictType
{
    ExistingFutureVisit,
    TimeSpacing
}

public sealed record VisitScheduleConflict(
    VisitScheduleConflictType Type,
    Visit Visit);
