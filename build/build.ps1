Param($configuration = "Release")
$ErrorActionPreference = "Stop"
Import-Module .\build\exechelper.ps1
Import-Module .\build\testhelper.ps1
$currentDir = (Get-Location)


# Install .NET tooling

exec .\build\dotnet-cli-install.ps1
$dotnetVersion = .\build\dotnet\dotnet.exe --version
Write-Host "Using dotnet cli version $dotnetVersion"

# Build dotnet projects

exec .\build\dotnet\dotnet.exe restore
exec .\build\dotnet\dotnet.exe "build --configuration $configuration"

# Run XUnit test projects

exec .\build\run-tests.ps1 $configuration

#Remove old build artifacts
Remove-Item -Path ./InstallerSource -Recurse -Force -Confirm:$false -ErrorAction Ignore
Remove-Item -Path ./EPiServer.Search.Cms.zip -Confirm:$false -Force -ErrorAction Ignore

#Create folder if not existed
New-Item -Path "." -Name "InstallerSource" -ItemType "directory" -Force

#Copy 
Copy-Item "./src/EPiServer.Search.Cms/module.config" -Destination "./InstallerSource/" -Force

#Create zip file
$compress = @{
  Path = "./InstallerSource/*"
  CompressionLevel = "Optimal"
  DestinationPath = "./EPiServer.Search.Cms.zip"
}
Compress-Archive @compress