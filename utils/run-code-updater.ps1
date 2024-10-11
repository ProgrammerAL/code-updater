
$configPath = "$PSScriptRoot/code-updater-config.json"
$runLocation = "$PSScriptRoot/../"

$startPath = Get-Location

Set-Location $runLocation
& "code-updater" --config "$configPath"

Read-Host "Press enter to exit..."

Set-Location $startPath
