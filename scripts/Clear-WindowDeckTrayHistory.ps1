[CmdletBinding()]
param(
    [switch]$Apply
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$registryPath = 'HKCU:\Control Panel\NotifyIconSettings'
$repositoryRoot = Split-Path -Parent $PSScriptRoot
$backupRoot = Join-Path $repositoryRoot 'artifacts\tray-icon-backups'
$entries = @()
$backupDirectory = $null
$runningInstances = @()

if (-not (Test-Path -LiteralPath $registryPath)) {
    Write-Output 'No Windows notification icon history was found.'
    return
}

$entries = @(
    Get-ChildItem -LiteralPath $registryPath | ForEach-Object {
        $executablePath = $_.GetValue('ExecutablePath', $null)
        $tooltip = $_.GetValue('InitialTooltip', $null)
        if ($executablePath -is [string] -and
            $executablePath.Trim().Trim('"') -match '(^|[\\/])WindowDeck\.exe$') {
            [pscustomobject]@{
                RegistryPath = $_.PSPath
                RegistryName = $_.Name
                ExecutablePath = $executablePath
                Tooltip = $tooltip
            }
        }
    }
)

if ($entries.Count -eq 0) {
    Write-Output 'No WindowDeck.exe notification icon history entries were found.'
    return
}

Write-Output ("Found {0} WindowDeck.exe notification icon history entr{1}:" -f
    $entries.Count, $(if ($entries.Count -eq 1) { 'y' } else { 'ies' }))
$entries | Select-Object ExecutablePath, Tooltip, RegistryName | Format-Table -AutoSize -Wrap | Out-Host

if (-not $Apply) {
    Write-Output 'Preview only. No registry values were changed.'
    Write-Output 'To remove only these WindowDeck.exe history entries, exit WindowDeck and rerun with -Apply.'
    return
}

$runningInstances = @(Get-Process -Name WindowDeck -ErrorAction SilentlyContinue)
if ($runningInstances.Count -gt 0) {
    throw 'Exit WindowDeck through the tray menu before cleaning icon history. No entries were removed.'
}

$backupDirectory = Join-Path $backupRoot (Get-Date -Format 'yyyyMMdd-HHmmss-fff')
New-Item -ItemType Directory -Path $backupDirectory -Force | Out-Null

# Back up every matching key before deleting any of them. Never delete a key if
# its export fails; failing halfway through the backup phase leaves all keys intact.
foreach ($entry in $entries) {
    $keyName = Split-Path -Path $entry.RegistryName -Leaf
    $safeName = $keyName -replace '[^a-zA-Z0-9_-]', '_'
    $backupFile = Join-Path $backupDirectory ($safeName + '.reg')
    & reg.exe export $entry.RegistryName $backupFile /y | Out-Null
    if ($LASTEXITCODE -ne 0 -or -not (Test-Path -LiteralPath $backupFile)) {
        throw "Failed to back up $($entry.RegistryName). No entries have been removed."
    }
}

foreach ($entry in $entries) {
    Remove-Item -LiteralPath $entry.RegistryPath -Recurse -Force -ErrorAction Stop
}

Write-Output ("Removed {0} WindowDeck.exe icon history entries." -f $entries.Count)
Write-Output "Backups: $backupDirectory"
Write-Output 'Other application entries were not changed.'
Write-Output 'Restart Windows Explorer or sign out and in, then launch WindowDeck from its permanent location.'
Write-Output 'You may need to re-enable its visibility in Windows Taskbar settings.'
