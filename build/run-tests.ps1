Param($configuration = "Release")
$ErrorActionPreference = "Stop"
Import-Module .\build\exechelper.ps1
Import-Module .\build\testhelper.ps1
$currentDir = (Get-Location)

$failedProjects = 0
foreach($testProject in (Get-ChildItem .\test\*Test | Select-Object -ExpandProperty FullName))
{
    Push-Location $testProject
    try
    {
        test "$currentDir\build\dotnet\dotnet.exe" "test --filter Category!=PDF --configuration $configuration --no-build --logger:trx -- RunConfiguration.DisableAppDomain=true"
    }
    catch
    {
        Write-Host "Test failed: $testProject"
        Write-Host $_.Exception.Message
        $failedProjects += 1
    }
    finally
    {
        Pop-Location
    }
}
if($failedProjects -ne 0)
{
    throw "$failedProjects test project(s) failed"
}