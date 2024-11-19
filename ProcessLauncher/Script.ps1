$ErrorActionPreference = 'Continue'

$scriptId = $args[0]
Write-Output "Starting script $scriptId"
Start-Sleep -Seconds 3
Write-Error "Script ${scriptId}: error output"
Start-Sleep -Seconds 3
Write-Output "Terminating script $scriptId"
Start-Sleep -Seconds 1

Exit 0
