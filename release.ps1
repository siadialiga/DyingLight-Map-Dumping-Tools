$csprojPath = "AutoMapDumperGUI\AutoMapDumperGUI.csproj"
[xml]$csproj = Get-Content $csprojPath
$rawVersion = $csproj.Project.PropertyGroup.Version
$formattedVersion = $rawVersion -replace '\.', '_'
$zipName = "DLMDT_v$formattedVersion.zip"

Write-Host "Target ZIP Archive: $zipName"

Remove-Item -Recurse -Force DLMDT_Release -ErrorAction SilentlyContinue
Remove-Item -Force DLMDT_*.zip -ErrorAction SilentlyContinue

Copy-Item -Path "favicon.ico" -Destination "AutoMapDumperGUI\favicon.ico" -Force

New-Item -ItemType Directory -Path DLMDT_Release\tools\SO18_Dumper -Force | Out-Null
New-Item -ItemType Directory -Path DLMDT_Release\tools\Map2EDS -Force | Out-Null

dotnet clean -c Release

dotnet publish AutoMapDumperGUI\AutoMapDumperGUI.csproj -c Release -o DLMDT_Release
dotnet publish SoDumper\SO18_Dumper.csproj -c Release -o DLMDT_Release\tools\SO18_Dumper
dotnet publish Map2EDS\Map2EDS.csproj -c Release -o DLMDT_Release\tools\Map2EDS

Get-ChildItem -Path DLMDT_Release -Recurse | Where-Object { $_.Extension -eq '.pdb' -or $_.Name -eq 'favicon.ico' } | Remove-Item -Force

Compress-Archive -Path DLMDT_Release\* -DestinationPath $zipName -Force