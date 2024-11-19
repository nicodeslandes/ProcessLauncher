$ErrorActionPreference = 'Continue'

$sw = [System.Diagnostics.Stopwatch]::StartNew()
$scriptId = $args[0]
Write-Output "Starting script $scriptId"
Start-Sleep -Seconds 1
Write-Error "Script ${scriptId}: error output"
Start-Sleep -Seconds 1
Write-Output "Terminating script $scriptId"
Start-Sleep -Seconds 1
Write-Output ("Elapsed time: {0:N2}s" -f $sw.Elapsed.TotalSeconds)
Exit 0
