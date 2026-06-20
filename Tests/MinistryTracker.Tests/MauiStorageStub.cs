namespace Microsoft.Maui.Storage;

public static class FileSystem
{
    public static string AppDataDirectory =>
        throw new InvalidOperationException(
            "Tests must construct DataService with an isolated database path.");
}
