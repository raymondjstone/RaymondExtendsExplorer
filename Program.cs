using System.Text.Json;
using System.Windows.Forms;

if (args.Length < 1)
{
    MessageBox.Show("Usage: RaymondExtendsExplorer.exe <command> <path> [<path> ...]",
        "Raymond Extends Explorer", MessageBoxButtons.OK, MessageBoxIcon.Error);
    return 1;
}

var command = args[0];

if (command == "toggle-confirmations")
{
    var s = Settings.Load();
    s.ShowConfirmations = !s.ShowConfirmations;
    s.Save();
    MessageBox.Show(
        $"Confirmations are now {(s.ShowConfirmations ? "enabled" : "disabled")}.",
        "Raymond Extends Explorer", MessageBoxButtons.OK, MessageBoxIcon.Information);
    return 0;
}

if (args.Length < 2)
{
    MessageBox.Show("Usage: RaymondExtendsExplorer.exe <command> <path> [<path> ...]",
        "Raymond Extends Explorer", MessageBoxButtons.OK, MessageBoxIcon.Error);
    return 1;
}

var paths = args.Skip(1).Select(p => p.TrimEnd('\\', '/')).ToList();

var invalid = paths.Where(p => !Directory.Exists(p) && !File.Exists(p)).ToList();
if (invalid.Count > 0)
{
    MessageBox.Show($"Paths not found:\n{string.Join("\n", invalid)}",
        "Raymond Extends Explorer", MessageBoxButtons.OK, MessageBoxIcon.Error);
    return 1;
}

var settings = Settings.Load();

return command switch
{
    "set-today" => SetAllFilesAsToday(paths, settings.ShowConfirmations),
    "move-to-just-watched" => MoveToJustWatched(paths, settings.ShowConfirmations),
    _ => ShowError($"Unknown command: {command}")
};

static int SetAllFilesAsToday(List<string> paths, bool showConfirmations)
{
    var now = DateTime.Now;
    var errors = new List<string>();
    int count = 0;

    foreach (var path in paths)
    {
        if (File.Exists(path) && !Directory.Exists(path))
        {
            try
            {
                File.SetLastWriteTime(path, now);
                count++;
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                errors.Add($"{path}: {ex.Message}");
            }
            continue;
        }

        try
        {
            foreach (var file in Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories))
            {
                try
                {
                    File.SetLastWriteTime(file, now);
                    count++;
                }
                catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
                {
                    errors.Add($"{file}: {ex.Message}");
                }
            }
        }
        catch (Exception ex)
        {
            errors.Add($"{path}: {ex.Message}");
        }
    }

    if (!showConfirmations)
        return errors.Count > 0 ? 1 : 0;

    if (count == 0 && errors.Count == 0)
    {
        MessageBox.Show($"No files found.",
            "Set All Files As Today", MessageBoxButtons.OK, MessageBoxIcon.Information);
        return 0;
    }

    if (errors.Count > 0)
    {
        var summary = string.Join("\n", errors.Take(20));
        if (errors.Count > 20)
            summary += $"\n... and {errors.Count - 20} more";
        MessageBox.Show($"Updated {count} files.\n\nFailed to update {errors.Count} files:\n{summary}",
            "Set All Files As Today", MessageBoxButtons.OK, MessageBoxIcon.Warning);
    }
    else
    {
        MessageBox.Show($"Updated {count} files to {now:yyyy-MM-dd HH:mm:ss}.",
            "Set All Files As Today", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    return 0;
}

static int MoveToJustWatched(List<string> paths, bool showConfirmations)
{
    var errors = new List<string>();
    int moved = 0;

    foreach (var folderPath in paths)
    {
        if (!Directory.Exists(folderPath))
        {
            errors.Add($"{folderPath}: not a folder, skipped.");
            continue;
        }

        var parentDir = Directory.GetParent(folderPath);
        if (parentDir == null)
        {
            errors.Add($"{folderPath}: cannot determine parent folder.");
            continue;
        }

        if (!string.Equals(parentDir.Name, "NotWatched", StringComparison.OrdinalIgnoreCase))
        {
            errors.Add($"{folderPath}: parent is '{parentDir.Name}', not 'NotWatched'.");
            continue;
        }

        var grandparentDir = parentDir.Parent;
        if (grandparentDir == null)
        {
            errors.Add($"{folderPath}: cannot determine grandparent folder.");
            continue;
        }

        var justWatchedDir = Path.Combine(grandparentDir.FullName, "JustWatched");
        if (!Directory.Exists(justWatchedDir))
        {
            errors.Add($"{folderPath}: 'JustWatched' folder not found at {justWatchedDir}.");
            continue;
        }

        var folderName = Path.GetFileName(folderPath);
        var destination = Path.Combine(justWatchedDir, folderName);

        if (Directory.Exists(destination))
        {
            errors.Add($"{folderName}: already exists in JustWatched.");
            continue;
        }

        try
        {
            Directory.Move(folderPath, destination);
            moved++;
        }
        catch (Exception ex)
        {
            errors.Add($"{folderName}: {ex.Message}");
        }
    }

    if (!showConfirmations)
        return errors.Count > 0 ? 1 : 0;

    if (moved == 0 && errors.Count == 0)
        return 0;

    if (errors.Count > 0)
    {
        var summary = string.Join("\n", errors.Take(20));
        if (errors.Count > 20)
            summary += $"\n... and {errors.Count - 20} more";
        var msg = moved > 0
            ? $"Moved {moved} folder(s).\n\nErrors:\n{summary}"
            : $"Failed to move:\n{summary}";
        MessageBox.Show(msg, "Moved to Just Watched", MessageBoxButtons.OK, MessageBoxIcon.Warning);
    }
    else
    {
        MessageBox.Show($"Moved {moved} folder(s) to JustWatched.",
            "Moved to Just Watched", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    return errors.Count > 0 ? 1 : 0;
}

static int ShowError(string message)
{
    MessageBox.Show(message, "Raymond Extends Explorer", MessageBoxButtons.OK, MessageBoxIcon.Error);
    return 1;
}

class Settings
{
    static readonly string SettingsDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "RaymondExtendsExplorer");
    static readonly string SettingsFile = Path.Combine(SettingsDir, "settings.json");

    public bool ShowConfirmations { get; set; }

    public static Settings Load()
    {
        try
        {
            if (File.Exists(SettingsFile))
            {
                var json = File.ReadAllText(SettingsFile);
                return JsonSerializer.Deserialize<Settings>(json) ?? new Settings();
            }
        }
        catch { }
        return new Settings();
    }

    public void Save()
    {
        Directory.CreateDirectory(SettingsDir);
        var json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(SettingsFile, json);
    }
}
