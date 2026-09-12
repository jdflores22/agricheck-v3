using AgriCheck.Application.ClientPortal;
using AgriCheck.Application.ClientPortal.Dtos;

namespace AgriCheck.Infrastructure.Services;

internal static class StoredFilePathResolver
{
    public static string ResolveAccreditationFilePath(IFileStorageService storage, Guid submissionUuid, string storedFileName)
    {
        foreach (var folder in AccreditationFolders(submissionUuid))
        {
            var path = storage.GetPhysicalPath(folder, storedFileName);
            if (File.Exists(path))
            {
                return path;
            }
        }

        throw new ClientPortalException("FILE_NOT_FOUND", "Stored file could not be located.");
    }

    public static string ResolveEntryFilePath(IFileStorageService storage, Guid entryUuid, string storedFileName)
    {
        foreach (var folder in EntryFolders(entryUuid))
        {
            var path = storage.GetPhysicalPath(folder, storedFileName);
            if (File.Exists(path))
            {
                return path;
            }
        }

        throw new ClientPortalException("FILE_NOT_FOUND", "Stored file could not be located.");
    }

    private static IEnumerable<string> AccreditationFolders(Guid submissionUuid)
    {
        yield return $"accreditation/{submissionUuid}";
        yield return $"accreditation/{submissionUuid}/compliance";
    }

    private static IEnumerable<string> EntryFolders(Guid entryUuid)
    {
        yield return $"entries/{entryUuid}";
        yield return $"entries/{entryUuid}/compliance";
    }
}
