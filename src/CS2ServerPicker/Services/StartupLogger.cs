using System.IO;
using CS2ServerPicker.Models;

namespace CS2ServerPicker.Services;

internal static class StartupLogger
{
    private static readonly string LogDirectory = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "CS2ServerPicker");

    private static readonly string LogPath = Path.Combine(LogDirectory, "startup.log");

    public static void Log(string message)
    {
        try
        {
            if (!Directory.Exists(LogDirectory))
                Directory.CreateDirectory(LogDirectory);

            File.AppendAllText(LogPath, $"[{DateTime.Now:O}] {message}{Environment.NewLine}");
        }
        catch
        {
            // Ignore logging failures
        }
    }

    public static void LogException(Exception ex)
    {
        Log(ex.ToString());
    }
}

