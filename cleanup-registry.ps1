# Run this script as Administrator to clean up old registry entries
# This script removes old context menu entries before installing the updated MSI

Write-Host "Cleaning up old Raymond Extends Explorer registry entries..." -ForegroundColor Cyan

# Remove all folder context menu entries (all naming variants used across versions)
$folderKeys = @(
    'HKCR:\Directory\shell\RaymondSetToday',
    'HKCR:\Directory\shell\RaymondMoveToJustWatched',
    'HKCR:\Directory\shell\RaymondSep1',
    'HKCR:\Directory\shell\RaymondSep2',
    'HKCR:\Directory\shell\Raymond_0_Separator',
    'HKCR:\Directory\shell\Raymond_1_SetToday',
    'HKCR:\Directory\shell\Raymond_2_MoveToJustWatched',
    'HKCR:\Directory\shell\Raymond_9_Separator'
)

# Remove all file context menu entries (all naming variants used across versions)
$fileKeys = @(
    'HKCR:\*\shell\RaymondMoveToShowFolder',
    'HKCR:\*\shell\RaymondSetToday',
    'HKCR:\*\shell\RaymondSep1',
    'HKCR:\*\shell\RaymondSep2',
    'HKCR:\*\shell\Raymond_0_Separator',
    'HKCR:\*\shell\Raymond_1_SetToday',
    'HKCR:\*\shell\Raymond_9_Separator'
)

foreach ($key in ($folderKeys + $fileKeys)) {
    if (Test-Path $key) {
        Remove-Item $key -Recurse -Force
        Write-Host "  Removed: $key" -ForegroundColor Yellow
    }
}

Write-Host "Registry cleanup complete!" -ForegroundColor Green
Write-Host ""
Write-Host "Clearing icon cache..." -ForegroundColor Cyan

# Clear icon cache
ie4uinit.exe -show

Write-Host "Icon cache cleared!" -ForegroundColor Green
Write-Host ""
Write-Host "Now rebuild the MSI installer and install it." -ForegroundColor Yellow
