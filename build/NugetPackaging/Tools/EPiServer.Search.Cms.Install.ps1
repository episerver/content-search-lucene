param($installPath, $toolsPath, $package, $project)

import-Module (join-path $toolsPath "Search.psm1")

Add-EPiBindingRedirect $installPath  $project

Set-EPiBaseUri $project

