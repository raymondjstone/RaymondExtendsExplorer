using System.Windows.Forms;

if (args.Length < 2)
{
    MessageBox.Show("Usage: RaymondExtendsExplorer.exe <command> <folder-path>",
        "Raymond Extends Explorer", MessageBoxButtons.OK, MessageBoxIcon.Error);
    return 1;
}

var command = args[0];
var folderPath = args[1].TrimEnd('\\', '/');

if (!Directory.Exists(folderPath))
{
    MessageBox.Show($"Folder not found:\n{folderPath}\n\nAll args: {string.Join(" | ", args)}",
        "Raymond Extends Explorer", MessageBoxButtons.OK, MessageBoxIcon.Error);
    return 1;
}

return command switch
{
    "set-today" => SetAllFilesAsToday(folderPath),
    "move-to-just-watched" => MoveToJustWatched(folderPath),
    _ => ShowError($"Unknown command: {command}")
};

static int SetAllFilesAsToday(string folderPath)
{
    var now = DateTime.Now;
    var errors = new List<string>();
    int count = 0;

    try
    {
        foreach (var file in Directory.EnumerateFiles(folderPath, "*", SearchOption.AllDirectories))
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
        return ShowError($"Failed to enumerate files in:\n{folderPath}\n\n{ex.Message}");
    }

    if (count == 0 && errors.Count == 0)
    {
        MessageBox.Show($"No files found under:\n{folderPath}",
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

static int MoveToJustWatched(string folderPath)
{
    var parentDir = Directory.GetParent(folderPath);
    if (parentDir == null)
        return ShowError("Cannot determine parent folder.");

    if (!string.Equals(parentDir.Name, "NotWatched", StringComparison.OrdinalIgnoreCase))
        return ShowError($"Parent folder is not named 'NotWatched'.\nParent: {parentDir.Name}");

    var grandparentDir = parentDir.Parent;
    if (grandparentDir == null)
        return ShowError("Cannot determine grandparent folder.");

    var justWatchedDir = Path.Combine(grandparentDir.FullName, "JustWatched");
    if (!Directory.Exists(justWatchedDir))
        return ShowError($"'JustWatched' folder not found at:\n{justWatchedDir}");

    var folderName = Path.GetFileName(folderPath);
    var destination = Path.Combine(justWatchedDir, folderName);

    if (Directory.Exists(destination))
        return ShowError($"A folder named '{folderName}' already exists in JustWatched.");

    try
    {
        Directory.Move(folderPath, destination);
    }
    catch (Exception ex)
    {
        return ShowError($"Failed to move folder:\n{ex.Message}");
    }

    return 0;
}

static int ShowError(string message)
{
    MessageBox.Show(message, "Raymond Extends Explorer", MessageBoxButtons.OK, MessageBoxIcon.Error);
    return 1;
}
