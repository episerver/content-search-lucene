Param([string]$version = "")
$ErrorActionPreference = "Stop"
Import-Module .\build\exechelper.ps1

# Install .NET tooling

exec .\build\dotnet-cli-install.ps1
$dotnetVersion = .\build\dotnet\dotnet.exe --version
Write-Host "Using dotnet cli version $dotnetVersion"

$output = Join-Path -Path (get-item $PSScriptRoot).parent.FullName -ChildPath "artifacts"

# Packaging ASP.NET

New-Item -ItemType:Directory "$output" -ErrorAction:Ignore
Write-Host "Creating ASP.NET Core Packages at $output\aspnetcore"

foreach($i in Get-ChildItem -File .\src\**\*.csproj)
{
    exec .\build\dotnet\dotnet.exe 'pack "$($i.FullName)" --no-build --version-suffix aspnetcore --output "$output\aspnetcore" --configuration Release'
}

# Packaging public packages

New-Item -ItemType:Directory "$output\packages" -ErrorAction:Ignore
Write-Host "Creating Public packages at $output\packages"

if($version -eq "")
{
    $version = . .\build\get-version.ps1
    $version = "$version-developerbuild"
}

foreach($i in Get-ChildItem -File .\build\NuGetPackaging\*.nuspec)
{
    exec .\build\nuget.exe 'pack "$($i.FullName)" -Properties "Configuration=Release;CmsCoreVersion=$version;PreReleaseInfo=;version=$version" -BasePath .\ -OutputDirectory "$output\packages" -Symbols'
}

