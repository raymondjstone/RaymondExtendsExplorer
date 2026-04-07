# Raymond Extends Explorer

An example project demonstrating how to build Windows Explorer context menu extensions using .NET and WiX v5, packaged as an MSI installer.

This is intended as a learning reference, not a production-ready tool.

## Commands

Right-click a folder in Windows Explorer (under "Show more options" on Windows 11) to access:

- **Set all files as today** — Recursively sets the last modified date of all files under the selected folder to the current time.
- **Moved to Just Watched** — Moves the selected folder from an `Unwatched` parent directory into a sibling `JustWatched` directory.

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

## How It Works

- A .NET console app (`RaymondExtendsExplorer.exe`) implements the two commands, selected via command-line arguments.
- The MSI writes registry keys under `HKCR\Directory\shell\` that invoke the exe when a folder is right-clicked.
- WiX v5 (SDK-style `.wixproj`) builds the MSI and handles install/uninstall/upgrade via `MajorUpgrade`.

## Project Structure

```
RaymondExtendsExplorer.csproj   # .NET console app (the command handler)
Program.cs                      # Command implementations
Installer/
  Installer.wixproj             # WiX v5 installer project
  Package.wxs                   # MSI package definition
```
