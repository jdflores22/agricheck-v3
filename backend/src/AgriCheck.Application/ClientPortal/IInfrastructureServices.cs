namespace AgriCheck.Application.ClientPortal;

public interface ICurrentUserService
{
    Guid? UserUuid { get; }
    long? UserId { get; }
    bool IsInRole(string role);
}

public interface IFileStorageService
{
    Task<(string storedFileName, long size)> SaveAsync(Stream stream, string folder, string originalFileName, CancellationToken cancellationToken = default);
    string GetPhysicalPath(string folder, string storedFileName);
}
