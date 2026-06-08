namespace DesktopIconManager;

public static class AppLogger
{
    public static void Log(string message, Exception? exception = null)
    {
        try
        {
            var lines = new List<string>
            {
                $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}",
            };

            if (exception is not null)
            {
                lines.Add(exception.ToString());
            }

            File.AppendAllLines(AppPaths.LogPath, lines);
        }
        catch
        {
            // Logging must never break the app.
        }
    }
}

