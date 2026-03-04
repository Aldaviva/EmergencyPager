$binaryPathName = Resolve-Path(join-path $PSScriptRoot "EmergencyPager.exe")

New-Service -Name "EmergencyPager" -DisplayName "EmergencyPager" -Description "When a PagerDuty alert is triggered, turn on Kasa outlets and show toasts on Windows by sending push notifications." -BinaryPathName $binaryPathName.Path -DependsOn Tcpip
sc.exe failure EmergencyPager actions= restart/0/restart/0/restart/0 reset= 86400