using System.Diagnostics;
using System.Text.Json;
using System.Text.RegularExpressions;
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
    "move-to-show-folder" => MoveToShowFolder(paths, settings.ShowConfirmations),
    "move-episode-to-just-watched" => MoveEpisodeToJustWatched(paths, settings.ShowConfirmations),
    "flatten-structure" => FlattenStructure(paths, settings.ShowConfirmations),
    "send-to-remarkable" => SendToRemarkable(paths),
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

static int MoveToShowFolder(List<string> paths, bool showConfirmations)
{
    var episodePattern = new Regex(@"[Ss]\d{1,2}[Ee]\d{1,2}|\d{1,2}[Xx]\d{1,2}", RegexOptions.None);
    var errors = new List<string>();
    int moved = 0;

    foreach (var filePath in paths)
    {
        if (!File.Exists(filePath) || Directory.Exists(filePath))
        {
            errors.Add($"{filePath}: not a file, skipped.");
            continue;
        }

        var fileName = Path.GetFileNameWithoutExtension(filePath);
        var match = episodePattern.Match(fileName);

        if (!match.Success)
        {
            errors.Add($"{Path.GetFileName(filePath)}: no episode pattern found (e.g. S01E01 or 1x01).");
            continue;
        }

        var showName = fileName[..match.Index].Replace('.', ' ').Trim();

        if (string.IsNullOrEmpty(showName))
        {
            errors.Add($"{Path.GetFileName(filePath)}: could not determine show name.");
            continue;
        }

        var parentDir = Path.GetDirectoryName(filePath)!;
        var showFolder = Path.Combine(parentDir, showName);

        try
        {
            Directory.CreateDirectory(showFolder);
            var destination = Path.Combine(showFolder, Path.GetFileName(filePath));

            if (File.Exists(destination))
            {
                errors.Add($"{Path.GetFileName(filePath)}: already exists in '{showName}'.");
                continue;
            }

            File.Move(filePath, destination);
            moved++;
        }
        catch (Exception ex)
        {
            errors.Add($"{Path.GetFileName(filePath)}: {ex.Message}");
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
            ? $"Moved {moved} file(s).\n\nErrors:\n{summary}"
            : $"Failed to move:\n{summary}";
        MessageBox.Show(msg, "Move to Show Folder", MessageBoxButtons.OK, MessageBoxIcon.Warning);
    }
    else
    {
        MessageBox.Show($"Moved {moved} file(s) to show folder(s).",
            "Move to Show Folder", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    return errors.Count > 0 ? 1 : 0;
}

static int FlattenStructure(List<string> paths, bool showConfirmations)
{
    var errors = new List<string>();
    int moved = 0;
    int deleted = 0;

    foreach (var rootPath in paths)
    {
        if (!Directory.Exists(rootPath))
        {
            errors.Add($"{rootPath}: not a folder, skipped.");
            continue;
        }

        List<string> files;
        try
        {
            files = Directory.EnumerateFiles(rootPath, "*", SearchOption.AllDirectories)
                .Where(f => !string.Equals(Path.GetDirectoryName(f), rootPath, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }
        catch (Exception ex)
        {
            errors.Add($"{rootPath}: {ex.Message}");
            continue;
        }

        foreach (var filePath in files)
        {
            var fileName = Path.GetFileName(filePath);
            var destination = Path.Combine(rootPath, fileName);

            if (File.Exists(destination))
            {
                errors.Add($"{fileName}: already exists in root, skipped.");
                continue;
            }

            try
            {
                File.Move(filePath, destination);
                moved++;
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                errors.Add($"{fileName}: {ex.Message}");
            }
        }

        // Delete empty subdirectories deepest-first
        try
        {
            var subdirs = Directory.EnumerateDirectories(rootPath, "*", SearchOption.AllDirectories)
                .OrderByDescending(d => d.Length)
                .ToList();

            foreach (var dir in subdirs)
            {
                if (!Directory.EnumerateFileSystemEntries(dir).Any())
                {
                    try
                    {
                        Directory.Delete(dir);
                        deleted++;
                    }
                    catch (Exception ex)
                    {
                        errors.Add($"{Path.GetFileName(dir)}: could not delete: {ex.Message}");
                    }
                }
            }
        }
        catch (Exception ex)
        {
            errors.Add($"{rootPath}: error cleaning up folders: {ex.Message}");
        }
    }

    if (!showConfirmations)
        return errors.Count > 0 ? 1 : 0;

    if (moved == 0 && deleted == 0 && errors.Count == 0)
    {
        MessageBox.Show("No files to move.",
            "Flatten Structure", MessageBoxButtons.OK, MessageBoxIcon.Information);
        return 0;
    }

    if (errors.Count > 0)
    {
        var summary = string.Join("\n", errors.Take(20));
        if (errors.Count > 20)
            summary += $"\n... and {errors.Count - 20} more";
        var msg = moved > 0
            ? $"Moved {moved} file(s), removed {deleted} empty folder(s).\n\nErrors:\n{summary}"
            : $"Failed:\n{summary}";
        MessageBox.Show(msg, "Flatten Structure", MessageBoxButtons.OK, MessageBoxIcon.Warning);
    }
    else
    {
        MessageBox.Show($"Moved {moved} file(s) to root, removed {deleted} empty folder(s).",
            "Flatten Structure", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    return errors.Count > 0 ? 1 : 0;
}

static int ShowError(string message)
{
    MessageBox.Show(message, "Raymond Extends Explorer", MessageBoxButtons.OK, MessageBoxIcon.Error);
    return 1;
}

static int SendToRemarkable(List<string> paths)
{
    const string remarkableExe = @"C:\Program Files\reMarkable\reMarkable.exe";
    if (!File.Exists(remarkableExe))
        return 0;

    var psi = new ProcessStartInfo(remarkableExe);
    psi.ArgumentList.Add(paths[0]);
    Process.Start(psi);
    return 0;
}

static int MoveEpisodeToJustWatched(List<string> paths, bool showConfirmations)
{
    var errors = new List<string>();
    int moved = 0;

    foreach (var filePath in paths)
    {
        if (!File.Exists(filePath) || Directory.Exists(filePath))
        {
            errors.Add($"{filePath}: not a file, skipped.");
            continue;
        }

        // Expect ...\\NotWatched\\<ShowName>\\Season...\\<file>
        var dir = Path.GetDirectoryName(filePath);
        if (dir == null)
        {
            errors.Add($"{filePath}: cannot determine parent folder.");
            continue;
        }
        var parts = dir.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        // Find NotWatched in the path
        int notWatchedIdx = Array.FindLastIndex(parts, p => string.Equals(p, "NotWatched", StringComparison.OrdinalIgnoreCase));
        if (notWatchedIdx < 0 || notWatchedIdx + 2 >= parts.Length)
        {
            errors.Add($"{filePath}: not in NotWatched/Show/Season... structure.");
            continue;
        }
        var showName = parts[notWatchedIdx + 1];
        var seasonFolder = parts[notWatchedIdx + 2];
        if (!seasonFolder.StartsWith("Season", StringComparison.OrdinalIgnoreCase))
        {
            errors.Add($"{filePath}: folder after show is not a Season folder.");
            continue;
        }

        // Build destination path
        var destParts = parts.ToArray();
        destParts[notWatchedIdx] = "JustWatched";
        var destDir = string.Join(Path.DirectorySeparatorChar.ToString(), destParts);
        var destFile = Path.Combine(destDir, Path.GetFileName(filePath));

        // Only require JustWatched to exist
        var justWatchedRoot = string.Join(Path.DirectorySeparatorChar.ToString(), destParts.Take(notWatchedIdx + 1));
        if (!Directory.Exists(justWatchedRoot))
        {
            errors.Add($"{filePath}: JustWatched folder does not exist: {justWatchedRoot}");
            continue;
        }

        // Create subfolders as needed
        try
        {
            Directory.CreateDirectory(destDir);
        }
        catch (Exception ex)
        {
            errors.Add($"{filePath}: could not create destination folder: {destDir} ({ex.Message})");
            continue;
        }

        if (File.Exists(destFile))
        {
            errors.Add($"{filePath}: already exists in JustWatched.");
            continue;
        }

        try
        {
            File.Move(filePath, destFile);
            moved++;
        }
        catch (Exception ex)
        {
            errors.Add($"{filePath}: {ex.Message}");
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
            ? $"Moved {moved} file(s).\n\nErrors:\n{summary}"
            : $"Failed to move:\n{summary}";
        MessageBox.Show(msg, "Move Episode to JustWatched", MessageBoxButtons.OK, MessageBoxIcon.Warning);
    }
    else
    {
        MessageBox.Show($"Moved {moved} file(s) to JustWatched.",
            "Move Episode to JustWatched", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    return errors.Count > 0 ? 1 : 0;
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
