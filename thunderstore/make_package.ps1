# Get the path to the project root
$RootDir = Split-Path -Parent (Split-Path -Parent $MyInvocation.MyCommand.Path)

# Load the csproj file to pull info from
$ProjectFilePath = Join-Path $RootDir "src/Sodalite/sodalite.csproj"
$ProjectXml = [xml](Get-Content $ProjectFilePath)

$PluginVersion = (Select-Xml -Xml $ProjectXml -XPath "//PackageVersion").Node.InnerText

$BuildDir = "../src/Sodalite/bin/Release/net35"

# Make a temporary folder to write our files into
$TempDir = Join-Path $RootDir "temp/"
Remove-Item $TempDir -Recurse -ErrorAction Ignore
New-Item -ItemType Directory -Path $TempDir | Out-Null
New-Item -ItemType Directory -Path (Join-Path $TempDir "plugins") | Out-Null
New-Item -ItemType Directory -Path (Join-Path $TempDir "patchers") | Out-Null

# Copy all our files into it
Copy-Item (Join-Path $RootDir "thunderstore/manifest.json") $TempDir
Copy-Item (Join-Path $RootDir "thunderstore/icon.png") $TempDir
Copy-Item (Join-Path $RootDir "README.md") $TempDir
Copy-Item (Join-Path $RootDir "LICENSE") $TempDir -ErrorAction Ignore
Copy-Item (Join-Path $BuildDir "${OutputPath}Sodalite.dll") (Join-Path $TempDir "plugins")
Copy-Item (Join-Path $BuildDir "${OutputPath}res/universalpanel") (Join-Path $TempDir "plugins")
Copy-Item (Join-Path $RootDir "src/libs/YamlDotNet.dll") (Join-Path $TempDir "plugins")
Copy-Item (Join-Path $BuildDir "${OutputPath}Sodalite.Patcher.dll") (Join-Path $TempDir "patchers")

# Replace values in the manifest with our project info
$ManifestPath = Join-Path $TempDir "manifest.json"
$ManifestContent = Get-Content $ManifestPath
$ManifestContent = $ManifestContent.replace("{VERSION}", $PluginVersion)
Set-Content $ManifestPath $ManifestContent

# Make a zip archive from the folder
$OutputPath = "Sodalite.zip"
Remove-Item $OutputPath -ErrorAction Ignore
Compress-Archive -Path "${TempDir}\*" -DestinationPath $OutputPath -Force

# Delete the temp folder and we're done!
Remove-Item $TempDir -Recurse
