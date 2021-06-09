param([String]$platform="x86")

write-host "Platform $platform"
start-sleep 2
function DeleteBindAndObj([String] $path){
	get-childitem -path $path -Recurse | 
		Where-Object { $_.Name -eq "bin" -or $_.Name -eq "obj"} | 
		ForEach-Object { 
			Write-Host "Directory: $($_.FullName)"
			get-childitem -path "$($_.FullName)" -File -Recurse |
				ForEach-Object { 
					Write-Host "File: $($_.FullName)"
					Remove-Item "$($_.FullName)" -Recurse -Force
				}
			Remove-Item "$($_.FullName)" -Recurse -Force
		}

}

DeleteBindAndObj("..\Applications")
DeleteBindAndObj("..\Modules")

function Build-Sln([String] $path, [String] $configuration, [String] $platform){
	$msbuild = Find-MsBuild
	Write-Host -ForegroundColor Green "$msbuild $item /t:Rebuild /t:restore /p:Configuration=$configuration /p:Platform="$platform\""
    & "$msbuild" -verbosity:m -nologo $item /t:"Restore" /p:Configuration=$configuration /p:Platform="$platform"
	& "$msbuild" -verbosity:m -nologo $item /t:"Rebuild" /p:Configuration=$configuration /p:Platform="$platform"
	write-host $Result
	if ($LASTEXITCODE -ne 0)
	{
		pause
	}
}

Function Find-MsBuild([int] $MaxVersion = 2019)
{
	$agentPath2 = "$Env:programfiles (x86)\Microsoft Visual Studio\2019\Preview\MSBuild\Current\Bin\msbuild.exe"
    $agentPath = "$Env:programfiles (x86)\Microsoft Visual Studio\2017\BuildTools\MSBuild\15.0\Bin\msbuild.exe"
    $devPath = "$Env:programfiles (x86)\Microsoft Visual Studio\2017\Enterprise\MSBuild\15.0\Bin\msbuild.exe"
    $proPath = "$Env:programfiles (x86)\Microsoft Visual Studio\2017\Professional\MSBuild\15.0\Bin\msbuild.exe"
    $communityPath = "$Env:programfiles (x86)\Microsoft Visual Studio\2017\Community\MSBuild\15.0\Bin\msbuild.exe"
    $fallback2015Path = "${Env:ProgramFiles(x86)}\MSBuild\14.0\Bin\MSBuild.exe"
    $fallback2013Path = "${Env:ProgramFiles(x86)}\MSBuild\12.0\Bin\MSBuild.exe"
    $fallbackPath = "C:\Windows\Microsoft.NET\Framework\v4.0.30319"
	If ((2019 -le $MaxVersion) -And (Test-Path $agentPath2)) { return $agentPath2 } 
    If ((2017 -le $MaxVersion) -And (Test-Path $agentPath)) { return $agentPath } 
    If ((2017 -le $MaxVersion) -And (Test-Path $devPath)) { return $devPath } 
    If ((2017 -le $MaxVersion) -And (Test-Path $proPath)) { return $proPath } 
    If ((2017 -le $MaxVersion) -And (Test-Path $communityPath)) { return $communityPath } 
    If ((2015 -le $MaxVersion) -And (Test-Path $fallback2015Path)) { return $fallback2015Path } 
    If ((2013 -le $MaxVersion) -And (Test-Path $fallback2013Path)) { return $fallback2013Path } 
    If (Test-Path $fallbackPath) { return $fallbackPath } 
        
    throw "Yikes - Unable to find msbuild"
}

$baseDir = (Get-Item -Path "..\Libraries\" -Verbose).FullName
$items = Get-ChildItem -Path $baseDir -Include *.sln -Recurse
foreach ($item in $items){
	Build-Sln $item "Debug" "Any CPU"
}

$baseDir = (Get-Item -Path "..\Modules\" -Verbose).FullName
$items = Get-ChildItem -Path $baseDir -Include *.sln -Recurse
foreach ($item in $items){
	Build-Sln $item "Debug" $platform
}

$baseDir = (Get-Item -Path "..\Applications\" -Verbose).FullName
$items = Get-ChildItem -Path $baseDir -Include *.sln -Recurse
foreach ($item in $items){
	
	Build-Sln $item "Debug" $platform
}