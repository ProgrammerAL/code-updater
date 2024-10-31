
$configPath = "$PSScriptRoot/code-updater-config.json"
$runLocation = "$PSScriptRoot/../"

$startPath = Get-Location

# Change path so the updater runs 1 directory higher, not inside this directory
Set-Location $runLocation
& "code-updater" --config "$configPath"

# Go back to the original path
Set-Location $startPath

Read-Host "Press enter to exit..."
