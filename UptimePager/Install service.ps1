$binaryPathName = Resolve-Path(join-path $PSScriptRoot "UptimePager.exe")

New-Service -Name "UptimePager" -DisplayName "UptimePager" -Description "When UptimeObserver detects that a check is down, it sends a webhook request to this server, which triggers a PagerDuty alert for the configured service." -BinaryPathName $binaryPathName.Path -DependsOn Tcpip
sc.exe failure UptimePager actions= restart/0/restart/0/restart/0 reset= 86400