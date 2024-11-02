$configPath = "$PSScriptRoot/code-updater-config.json"

& "code-updater" --config "$configPath"

Read-Host "Press enter to exit..."
