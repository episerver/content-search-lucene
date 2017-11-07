param($installPath, $toolsPath, $package, $project)

Write-Host 'Importing Search.psm1'

import-Module (join-path $toolsPath "Search.psm1")

Add-EPiBindingRedirect $installPath  $project
