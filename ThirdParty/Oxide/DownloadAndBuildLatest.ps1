#
# Script.ps1
#

$gitUrl = "https://github.com/OxideMod/Oxide.git"
$oxideDirectory = ".\Oxide\"

If ((Test-Path ($oxideDirectory + ".git")) -eq $false)
{
	Invoke-Expression "git clone -b master --single-branch --depth 1 $gitUrl"
}

Set-Location ".\Oxide"
Invoke-Expression "git checkout master"
Invoke-Expression "git pull origin master"

Set-Location ".."

Write-Host "Building and bundling Oxide..."
Invoke-MsBuild ($oxideDirectory + "Oxide.sln") -MsBuildParameters "/target:Clean;Build /p:Configuration=Release" -AutoLaunchBuildLogOnFailure