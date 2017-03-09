#
# Script.ps1
#

$gitUrl = "https://github.com/OxideMod/Oxide.git"
$oxideDirectory = ".\Oxide\.git"

If ((Test-Path $oxideDirectory) -eq $false)
{
	Invoke-Expression "git clone -b master --single-branch --depth 1 $gitUrl"
}

Set-Location ".\Oxide"
Invoke-Expression "git checkout master"
Invoke-Expression "git pull origin master"

Set-Location ".."

Write-Host "Building and bundling Oxide..."
Invoke-MsBuild ".\Oxide\Oxide.sln" -MsBuildParameters "/target:Clean;Build" -AutoLaunchBuildLogOnFailure