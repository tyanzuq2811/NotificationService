namespace Common;

public static class EnvLoader
{
    private static bool _loaded = false;

    public static void Load(string? startDirectory = null)
    {
        if (_loaded) return;

        try
        {
            var dir = startDirectory ?? Directory.GetCurrentDirectory();
            while (!string.IsNullOrEmpty(dir))
            {
                var envFile = Path.Combine(dir, ".env");
                if (File.Exists(envFile))
                {
                    foreach (var line in File.ReadAllLines(envFile))
                    {
                        var trimmed = line.Trim();
                        if (string.IsNullOrEmpty(trimmed) || trimmed.StartsWith("#")) continue;

                        var idx = trimmed.IndexOf('=');
                        if (idx > 0)
                        {
                            var key = trimmed.Substring(0, idx).Trim();
                            var val = trimmed.Substring(idx + 1).Trim().Trim('"', '\'');

                            if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable(key)))
                            {
                                Environment.SetEnvironmentVariable(key, val);
                            }
                        }
                    }
                    _loaded = true;
                    break;
                }

                var parent = Directory.GetParent(dir);
                if (parent == null) break;
                dir = parent.FullName;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[EnvLoader Warning]: Failed to load .env: {ex.Message}");
        }
    }
}
