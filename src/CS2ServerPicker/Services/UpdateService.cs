using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text.Json;

namespace CS2ServerPicker.Services;

public sealed class UpdateService : IUpdateService
{
    private const string GitHubApiUrl = "https://api.github.com/repositories/649341649/releases";
    private const string ReleasesUrl = "https://github.com/FN-FAL113/cs2-server-picker/releases";

    private readonly HttpClient _httpClient;

    public UpdateService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<UpdateInfo?> CheckForUpdateAsync(CancellationToken ct = default)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, GitHubApiUrl);
            request.Headers.UserAgent.Add(new ProductInfoHeaderValue("CS2ServerPicker", "3.0"));

            var response = await _httpClient.SendAsync(request, ct);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync(ct);
            using var doc = JsonDocument.Parse(json);
            var releases = doc.RootElement;

            if (releases.GetArrayLength() == 0)
                return null;

            var latestTag = releases[0].GetProperty("tag_name").GetString() ?? "v0.0.0";
            var latestVersion = latestTag.TrimStart('v');
            var currentVersion = Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "0.0.0";

            var updateAvailable = CompareVersions(latestVersion, currentVersion) > 0;

            return new UpdateInfo(latestVersion, currentVersion, updateAvailable, ReleasesUrl);
        }
        catch
        {
            return null;
        }
    }

    private static int CompareVersions(string v1, string v2)
    {
        var parts1 = v1.Split('.').Select(p => int.TryParse(p, out var n) ? n : 0).ToArray();
        var parts2 = v2.Split('.').Select(p => int.TryParse(p, out var n) ? n : 0).ToArray();
        var maxLen = Math.Max(parts1.Length, parts2.Length);

        for (int i = 0; i < maxLen; i++)
        {
            var p1 = i < parts1.Length ? parts1[i] : 0;
            var p2 = i < parts2.Length ? parts2[i] : 0;
            if (p1 != p2) return p1.CompareTo(p2);
        }

        return 0;
    }
}
