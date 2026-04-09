# Run this script as Administrator to clean up old registry entries
# This script removes old context menu entries before installing the updated MSI

Write-Host "Cleaning up old Raymond Extends Explorer registry entries..." -ForegroundColor Cyan

# Remove old folder context menu entries
Remove-Item 'HKCR:\Directory\shell\RaymondSetToday' -Recurse -ErrorAction SilentlyContinue
Remove-Item 'HKCR:\Directory\shell\RaymondMoveToJustWatched' -Recurse -ErrorAction SilentlyContinue
Remove-Item 'HKCR:\Directory\shell\RaymondSep1' -Recurse -ErrorAction SilentlyContinue
Remove-Item 'HKCR:\Directory\shell\RaymondSep2' -Recurse -ErrorAction SilentlyContinue

# Remove old file context menu entries
Remove-Item 'HKCR:\*\shell\RaymondSetToday' -Recurse -ErrorAction SilentlyContinue
Remove-Item 'HKCR:\*\shell\RaymondSep1' -Recurse -ErrorAction SilentlyContinue
Remove-Item 'HKCR:\*\shell\RaymondSep2' -Recurse -ErrorAction SilentlyContinue

Write-Host "Registry cleanup complete!" -ForegroundColor Green
Write-Host ""
Write-Host "Clearing icon cache..." -ForegroundColor Cyan

# Clear icon cache
ie4uinit.exe -show

Write-Host "Icon cache cleared!" -ForegroundColor Green
Write-Host ""
Write-Host "Now you can install the updated MSI installer." -ForegroundColor Yellow
