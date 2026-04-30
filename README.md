# Raymond Extends Explorer

An example project demonstrating how to build Windows Explorer context menu extensions using .NET and WiX v5, packaged as an MSI installer.

This is intended as a learning reference, not a production-ready tool.

## Context Menu Commands

Right-click a **folder** in Windows Explorer (under "Show more options" on Windows 11) to access:

- **Flatten Structure** — Moves all files from subdirectories into the selected folder root, then removes the now-empty subdirectories. Supports multi-folder selection.
- **Moved to Just Watched** — Moves the selected folder from a `NotWatched` parent directory into a sibling `JustWatched` directory.
- **Set all files as today** — Recursively sets the last modified date of all files under the selected folder to the current time.
- **Toggle Confirmations** — Enables or disables dialog confirmations for the commands above.

Right-click a **file** to access:

- **Move to Show Folder** — Parses an episode filename (e.g. `Show.Name.S01E02.mkv`) and moves the file into a subfolder named after the show. Supports multi-file selection.
- **Move episode to JustWatched** — Moves an episode file from a `NotWatched\Show\Season N\` structure into the equivalent `JustWatched\Show\Season N\` path, creating subdirectories as needed. Supports multi-file selection.
- **Set all files as today** — Sets the last modified date of the selected file to the current time.
- **Send to reMarkable** — Opens the selected file in the reMarkable desktop app. Only appears when exactly one file is selected, and only installed if `reMarkable.exe` is detected at install time.

Each menu entry is displayed with a custom icon and separated from other context menu items.

## Confirmations

By default, commands run silently with no dialog popups. To enable confirmation dialogs (showing success/error summaries after each operation), right-click any folder and select **Toggle Confirmations**. The setting persists in `%LOCALAPPDATA%\RaymondExtendsExplorer\settings.json`.

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download) (for building)
- [.NET 10 Runtime](https://dotnet.microsoft.com/download) (on the target machine)

## Building

```
dotnet build Installer/Installer.wixproj -c Release
```

The MSI is output to `Installer/bin/Release/Installer.msi`.

To build with a specific version:

```
dotnet build Installer/Installer.wixproj -c Release -p:Version=1.2.3
```

## Installing

Run the MSI. It installs the executable to Program Files and registers the context menu entries. Uninstalling cleanly removes everything.

A `cleanup-registry.ps1` script is provided for removing leftover registry entries from previous versions if needed. Run it as Administrator before installing a new version.

> **Note:** The **Send to reMarkable** menu entry is only registered if `C:\Program Files\reMarkable\reMarkable.exe` exists at the time the MSI is installed. If reMarkable is installed afterwards, repair or reinstall the MSI to enable the entry.

## How It Works

- A .NET console app (`RaymondExtendsExplorer.exe`) implements the commands, selected via command-line arguments.
- The MSI writes registry keys under `HKCR\Directory\shell\` and `HKCR\*\shell\` that invoke the exe when a folder or file is right-clicked.
- Context menu entries use `SeparatorBefore` / `SeparatorAfter` registry values to visually group the entries.
- `MultiSelectModel = "Player"` enables multi-selection for commands that support it; `MultiSelectModel = "Single"` restricts a command to single-file selection only.
- A custom icon (`app.ico`) is installed alongside the executable and referenced by each menu entry.
- WiX v5 (SDK-style `.wixproj`) builds the MSI and handles install/uninstall/upgrade via `MajorUpgrade`.
- Optional context menu entries (e.g. Send to reMarkable) are registered in a separate WiX component with a `<Condition>` that evaluates a `<FileSearch>` result, so the registry keys are only written when the prerequisite application is present.

## Project Structure

```
RaymondExtendsExplorer.csproj   # .NET console app (the command handler)
Program.cs                      # Command implementations and settings
app.ico                         # Context menu icon
Installer/
  Installer.wixproj             # WiX v5 installer project
  Package.wxs                   # MSI package definition
cleanup-registry.ps1            # Helper to remove old registry entries
```
