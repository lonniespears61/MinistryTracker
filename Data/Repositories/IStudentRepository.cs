using MinistryTracker.Models;
using MinistryTracker.Models.DTOs;
using MinistryTracker.Models.Enums;

namespace MinistryTracker.Data.Repositories;

public interface IStudentRepository
{
    Task<int> AddStudentAsync(Student student, CancellationToken ct = default);
    Task<int> UpdateStudentAsync(Student student, CancellationToken ct = default);
    Task<int> SoftDeleteStudentAsync(int studentId, CancellationToken ct = default);
    Task<Student?> GetStudentByIdAsync(int studentId, CancellationToken ct = default);
    Task<List<Student>> GetStudentsAsync(bool includeDeleted = false, CancellationToken ct = default);
    Task<List<Student>> GetWorkingScopeStudentsAsync(CancellationToken ct = default);
    Task<int> GetActiveStudentsCountAsync(CancellationToken ct = default);
    Task<List<Student>> GetMappableStudentsAsync(CancellationToken ct = default);
    Task<List<Student>> GetStudentsByHomeAddressAsync(string primaryAddress, CancellationToken ct = default);
    Task<int> UpdateStudentPrimaryLocationAsync(
        int studentId,
        string? primaryAddress,
        bool isHomeAddress,
        LocationContext locationContext,
        double? primaryLatitude,
        double? primaryLongitude,
        GeocodeStatus geocodeStatus,
        CancellationToken ct = default);
    Task<List<CheckOnStudentSuggestion>> GetCheckOnStudentSuggestionsAsync(
        int take = 5,
        int minDaysSinceVisit = 30,
        CancellationToken ct = default);
}
