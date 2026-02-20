using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text.Json;
using CS2ServerPicker.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CS2ServerPicker.Services;

public sealed class UpdateService : IUpdateService
{
    private readonly HttpClient _httpClient;
    private readonly GitHubOptions _gitHubOptions;
    private readonly ILogger<UpdateService> _logger;

    public UpdateService(HttpClient httpClient, IOptions<GitHubOptions> gitHubOptions, ILogger<UpdateService> logger)
    {
        _httpClient = httpClient;
        _gitHubOptions = gitHubOptions.Value;
        _logger = logger;
    }

    public async Task<UpdateInfo?> CheckForUpdateAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, _gitHubOptions.ReleasesApiUrl);
            request.Headers.UserAgent.Add(new ProductInfoHeaderValue("CS2ServerPicker", _gitHubOptions.UserAgentVersion));

            var response = await _httpClient.SendAsync(request, cancellationToken);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            using var doc = JsonDocument.Parse(json);
            var releases = doc.RootElement;

            if (releases.GetArrayLength() == 0)
                return null;

            var latestTagName = releases[0].GetProperty("tag_name").GetString() ?? "v0.0.0";
            var latestVersion = latestTagName.TrimStart('v');
            var currentVersion = Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "0.0.0";

            var updateAvailable = CompareVersions(latestVersion, currentVersion) > 0;

            return new UpdateInfo(latestVersion, currentVersion, updateAvailable, _gitHubOptions.ReleasesPageUrl);
        }
        catch (Exception exception)
        {
            _logger.LogWarning(exception, "Failed to check for updates from {Url}.", _gitHubOptions.ReleasesApiUrl);
            return null;
        }
    }

    private static int CompareVersions(string firstVersion, string secondVersion)
    {
        var firstParts = firstVersion.Split('.').Select(part => int.TryParse(part, out var number) ? number : 0).ToArray();
        var secondParts = secondVersion.Split('.').Select(part => int.TryParse(part, out var number) ? number : 0).ToArray();
        var maxPartCount = Math.Max(firstParts.Length, secondParts.Length);

        for (int partIndex = 0; partIndex < maxPartCount; partIndex++)
        {
            var firstPartValue = partIndex < firstParts.Length ? firstParts[partIndex] : 0;
            var secondPartValue = partIndex < secondParts.Length ? secondParts[partIndex] : 0;

            if (firstPartValue != secondPartValue)
                return firstPartValue.CompareTo(secondPartValue);
        }

        return 0;
    }
}
