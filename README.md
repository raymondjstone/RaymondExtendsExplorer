# Raymond Extends Explorer

An example project demonstrating how to build Windows Explorer context menu extensions using .NET and WiX v5, packaged as an MSI installer.

This is intended as a learning reference, not a production-ready tool.

## Context Menu Commands

Right-click a folder in Windows Explorer (under "Show more options" on Windows 11) to access:

- **Moved to Just Watched** — Moves the selected folder from a `NotWatched` parent directory into a sibling `JustWatched` directory.
- **Set all files as today** — Recursively sets the last modified date of all files under the selected folder to the current time.
- **Toggle Confirmations** — Enables or disables dialog confirmations for the commands above.

Right-click a file to access:

- **Set all files as today** — Sets the last modified date of the selected file to the current time.

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

## How It Works

- A .NET console app (`RaymondExtendsExplorer.exe`) implements the commands, selected via command-line arguments.
- The MSI writes registry keys under `HKCR\Directory\shell\` and `HKCR\*\shell\` that invoke the exe when a folder or file is right-clicked.
- Context menu entries use `SeparatorBefore` / `SeparatorAfter` registry values to visually group the entries.
- A custom icon (`app.ico`) is installed alongside the executable and referenced by each menu entry.
- WiX v5 (SDK-style `.wixproj`) builds the MSI and handles install/uninstall/upgrade via `MajorUpgrade`.

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
