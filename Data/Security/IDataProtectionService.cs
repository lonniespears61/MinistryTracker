namespace MinistryTracker.Data.Security;

public interface IDataProtectionService
{
    Task InitializeAsync();
    string? Protect(string? plaintext);
    string? Unprotect(string? protectedValue);
}
