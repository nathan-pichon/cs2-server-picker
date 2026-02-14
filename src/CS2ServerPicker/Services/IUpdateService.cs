namespace CS2ServerPicker.Services;

public record UpdateInfo(string LatestVersion, string CurrentVersion, bool UpdateAvailable, string ReleaseUrl);

public interface IUpdateService
{
    Task<UpdateInfo?> CheckForUpdateAsync(CancellationToken ct = default);
}
