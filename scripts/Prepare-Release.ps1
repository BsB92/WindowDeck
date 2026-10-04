[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$repositoryRoot = Split-Path -Parent $PSScriptRoot
$projectPath = Join-Path $repositoryRoot 'WindowDeck.csproj'
$project = [xml](Get-Content -LiteralPath $projectPath -Raw)
$releaseVersion = [string]$project.Project.PropertyGroup.Version
$outputDirectory = Join-Path $repositoryRoot "artifacts/WindowDeck-$releaseVersion-win-x64"
$executablePath = Join-Path $outputDirectory 'WindowDeck.exe'
$checksumPath = Join-Path $outputDirectory 'WindowDeck.exe.sha256'
$notesTemplatePath = Join-Path $repositoryRoot "docs/notes/v$releaseVersion.md"
$notesPath = Join-Path $outputDirectory 'RELEASE_NOTES.md'
$checksum = $null
$notes = $null
$utf8 = New-Object System.Text.UTF8Encoding($false)

if ($releaseVersion -notmatch '^\d+\.\d+\.\d+$') {
    throw 'WindowDeck.csproj must contain a three-part release Version.'
}
if (-not (Test-Path -LiteralPath $notesTemplatePath)) {
    throw "Release notes template not found: $notesTemplatePath"
}
if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
    throw 'The .NET 10 SDK is required. Open Visual Studio Developer PowerShell or install the SDK.'
}

Push-Location $repositoryRoot
try {
    & dotnet publish $projectPath -p:PublishProfile=WinX64
    if ($LASTEXITCODE -ne 0) {
        throw "dotnet publish failed with exit code $LASTEXITCODE."
    }
    if (-not (Test-Path -LiteralPath $executablePath)) {
        throw "Published executable not found: $executablePath"
    }

    $checksum = (Get-FileHash -LiteralPath $executablePath -Algorithm SHA256).Hash
    $notes = [System.IO.File]::ReadAllText($notesTemplatePath)
    if (-not $notes.Contains('{{WINDOWDECK_SHA256}}')) {
        throw 'Release notes template is missing the SHA-256 placeholder.'
    }
    $notes = $notes.Replace('{{WINDOWDECK_SHA256}}', $checksum)
    [System.IO.File]::WriteAllText($checksumPath, "$checksum  WindowDeck.exe`r`n", $utf8)
    [System.IO.File]::WriteAllText($notesPath, $notes, $utf8)

    Write-Host "Prepared WindowDeck $releaseVersion"
    Write-Host "Executable: $executablePath"
    Write-Host "Checksum:   $checksumPath"
    Write-Host "Notes:      $notesPath"
    Write-Host "SHA-256:    $checksum"
    Write-Host 'Test this executable on Windows, outside Visual Studio, before uploading it.'
    Write-Host 'No GitHub release, tag, merge, or push has been performed.'
}
finally {
    Pop-Location
}
