# Builds the plugin and packs dist\MycopunkThai-v<version>.zip for GitHub Releases.
# Needs the .NET SDK and Mycopunk installed (the build references the game's DLLs).
param([string]$GameDir)

$ErrorActionPreference = 'Stop'
$root = $PSScriptRoot
$buildArgs = @('build', "$root\src\ThaiLanguage.csproj", '-c', 'Release', '-nologo')
if ($GameDir) { $buildArgs += "-p:GameDir=$GameDir" }
dotnet @buildArgs
if ($LASTEXITCODE) { throw 'build failed' }

$version = (Select-String "$root\src\Plugin.cs" -Pattern 'Version = "([^"]+)"').Matches[0].Groups[1].Value
$stage = "$root\dist\MycopunkThai"
Remove-Item "$root\dist" -Recurse -Force -ErrorAction SilentlyContinue
New-Item -ItemType Directory "$stage\ThaiLanguage" -Force | Out-Null
Copy-Item "$root\installer\*" $stage
Copy-Item "$root\src\bin\Release\ThaiLanguage.dll", "$root\thai.csv" "$stage\ThaiLanguage"
Compress-Archive $stage "$root\dist\MycopunkThai-v$version.zip"
Write-Host "dist\MycopunkThai-v$version.zip"
