namespace MinistryTracker.Services;

public static class WorkflowRoutes
{
    public const string CancelToCalendar = "calendar";
    public const string CancelToDashboard = "dashboard";

    private const string MyCalendarPage = "MyCalendarPage";
    private const string AddVisitPage = "AddVisitPage";
    private const string SelectStudentForVisitPage = "SelectStudentForVisitPage";
    private const string UpdateVisitPage = "UpdateVisitPage";
    private const string StudentProfilePage = "StudentProfilePage";

    public static string ScheduleCalendar(int studentId, int? replaceVisitId = null)
    {
        var route =
            $"{MyCalendarPage}?mode=schedule&studentId={studentId}";

        if (replaceVisitId is int existingVisitId)
            route += $"&replaceVisitId={existingVisitId}";

        return route;
    }

    public static string AddVisit(
        int studentId,
        DateTime date,
        string cancelTo,
        int? replaceVisitId = null)
    {
        var route =
            $"{AddVisitPage}?studentId={studentId}" +
            $"&date={date:yyyy-MM-dd}" +
            $"&cancelTo={cancelTo}";

        if (replaceVisitId is int existingVisitId)
            route += $"&replaceVisitId={existingVisitId}";

        return route;
    }

    public static string SelectStudent(DateTime date) =>
        $"{SelectStudentForVisitPage}?date={date:yyyy-MM-dd}";

    public static string UpdateVisit(int visitId) =>
        $"{UpdateVisitPage}?visitId={visitId}";

    public static string StudentProfile(int studentId) =>
        $"{StudentProfilePage}?studentId={studentId}";
}
