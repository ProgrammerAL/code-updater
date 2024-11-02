$configPath = "$PSScriptRoot/code-updater-config.json"

& "code-updater" --options "$configPath"

Read-Host "Press enter to exit..."
