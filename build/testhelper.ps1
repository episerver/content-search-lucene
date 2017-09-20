Import-Module .\build\exechelper.ps1

function test_with_code_coverage {
    Param($executable,$arguments)
    $assembly_filter = "+:EPiServer;+:EPiServer.*;-:*Specs;-:*Test;-:*TestTools;-:EPiServer.Cms.AspNet;-:EPiServer.Framework.AspNet"
    $dot_cover= Join-Path -Path $env:DOTCOVER_PATH -ChildPath dotCover.exe
    $uniqueName = "Coverage_" + [guid]::NewGuid().ToString() + ".xml"
    $testCoverage =  $ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath(".\artifacts\$uniqueName.xml")

    exec $dot_cover 'c /TargetExecutable="$executable" /TargetArguments="$arguments" /TargetWorkingDir="$working_directory" /Output="$testCoverage" /Filters="$assembly_filter"'
    Write-Output "##teamcity[importData type='dotNetCoverage' tool='dotcover' path='$testCoverage']"
}


function test
{
    [CmdletBinding()]
    Param([parameter(Mandatory=$true)]$executable,[parameter()]$arguments)

    if ($env:DOTCOVER_ENABLED -eq "True") {
        test_with_code_coverage executable arguments
    }
    else {
        exec $executable $arguments
    }
}
