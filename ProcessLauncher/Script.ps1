$ErrorActionPreference = 'Continue'

$scriptId = $args[0]
Write-Output "Starting script $scriptId"
Start-Sleep -Seconds 1
Write-Error "Script ${scriptId}: error output"
Start-Sleep -Seconds 1
Write-Output "Terminating script $scriptId"
Start-Sleep -Seconds 1

Exit 0
