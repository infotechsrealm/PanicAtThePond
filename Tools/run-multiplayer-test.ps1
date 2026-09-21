<#
.SYNOPSIS
  Runs a real two-player match on this machine and prints both sides as one interleaved transcript.

.DESCRIPTION
  Launches the two test players built by "Panic At The Pond/Build Two Test Players", each with its
  own log file, waits for both to finish, then merges the [AUTOTEST] lines from both logs in
  timestamp order so host and client can be read against each other.

  The two players are separate builds with different productName values, which is what gives them
  separate PlayerPrefs hives -- two instances of one build would share saves and could not express
  a per-player bug.

.EXAMPLE
  ./Tools/run-multiplayer-test.ps1
  ./Tools/run-multiplayer-test.ps1 -Room MYROOM -TimeoutSeconds 120 -KeepWindows
#>
[CmdletBinding()]
param(
    # Keep this within the create-room InputField characterLimit (10) or the host silently
    # creates a truncated room the client will never find.
    [string]$Room = "AT$(Get-Random -Minimum 1000 -Maximum 9999)",
    [int]$TimeoutSeconds = 150,
    [int]$ClientDelaySeconds = 6,
    # Cosmetic sprite names to equip, one per side. Different values on each side is the point:
    # it is what makes "shows for the other player but not for me" detectable.
    [string]$HostFishHat = "",
    [string]$ClientFishHat = "",
    [string]$HostFishermanHat = "",
    [string]$ClientFishermanHat = "",
    # Keep the built player sitting in the match instead of quitting, so the Editor can join it
    # and the result can be looked at.
    [switch]$Hold,
    [switch]$KeepWindows,
    [switch]$RawLogs
)

$ErrorActionPreference = "Stop"
$root      = Split-Path -Parent $PSScriptRoot
$hostExe   = Join-Path $root "TestBuilds\Host\PanicAtThePond.exe"
$clientExe = Join-Path $root "TestBuilds\Client\PanicAtThePond.exe"
$logDir    = Join-Path $root "TestBuilds\Logs"

foreach ($exe in @($hostExe, $clientExe)) {
    if (-not (Test-Path $exe)) {
        throw "Missing $exe. Run 'Panic At The Pond/Build Two Test Players' in the Unity Editor first."
    }
}

New-Item -ItemType Directory -Force -Path $logDir | Out-Null
$stamp     = Get-Date -Format "yyyyMMdd-HHmmss"
$hostLog   = Join-Path $logDir "host-$stamp.log"
$clientLog = Join-Path $logDir "client-$stamp.log"

# Start-Process joins ArgumentList with spaces and does no quoting of its own, so any value
# containing a space (cosmetic names like "Fish Cap Hat") would arrive as several arguments.
function Quote-Arg([string]$v) { if ($v -match '\s') { return '"' + $v + '"' } else { return $v } }

# Leave any previous run's players dead, or they will still be sitting in the room.
Get-Process -Name "PanicAtThePond" -ErrorAction SilentlyContinue | Stop-Process -Force
Start-Sleep -Milliseconds 300

# Small windowed players so both fit on one desktop and neither grabs exclusive fullscreen.
$common = @("-screen-fullscreen", "0", "-screen-width", "960", "-screen-height", "540")

Write-Host "room:   $Room"
Write-Host "host:   $hostLog"
Write-Host "client: $clientLog"
Write-Host ""

$hostArgs = $common + @("-logFile", $hostLog, "-autotest", "host",
                        "-testroom", $Room, "-testnick", "AutoHost",
                        "-testtimeout", "$([Math]::Max(30, $TimeoutSeconds - 30))")
if ($Hold)             { $hostArgs += @("-testhold") }
if ($HostFishHat)      { $hostArgs += @("-testfishhat", $HostFishHat) }
if ($HostFishermanHat) { $hostArgs += @("-testfishermanhat", $HostFishermanHat) }
$hostArgs = $hostArgs | ForEach-Object { Quote-Arg $_ }
$hostProc = Start-Process -FilePath $hostExe -ArgumentList $hostArgs -PassThru

# The host needs to exist before the client can see its room in the lobby list.
Write-Host "host started (pid $($hostProc.Id)); waiting ${ClientDelaySeconds}s before the client..."
Start-Sleep -Seconds $ClientDelaySeconds

$clientArgs = $common + @("-logFile", $clientLog, "-autotest", "client",
                          "-testroom", $Room, "-testnick", "AutoClient",
                          "-testtimeout", "$([Math]::Max(30, $TimeoutSeconds - 30))")
if ($Hold)               { $clientArgs += @("-testhold") }
if ($ClientFishHat)      { $clientArgs += @("-testfishhat", $ClientFishHat) }
if ($ClientFishermanHat) { $clientArgs += @("-testfishermanhat", $ClientFishermanHat) }
$clientArgs = $clientArgs | ForEach-Object { Quote-Arg $_ }
$clientProc = Start-Process -FilePath $clientExe -ArgumentList $clientArgs -PassThru
Write-Host "client started (pid $($clientProc.Id))"

$deadline = (Get-Date).AddSeconds($TimeoutSeconds)
while ((Get-Date) -lt $deadline) {
    $hostDone   = $hostProc.HasExited
    $clientDone = $clientProc.HasExited
    if ($hostDone -and $clientDone) { break }
    Start-Sleep -Seconds 2
}

foreach ($p in @($hostProc, $clientProc)) {
    if (-not $p.HasExited) {
        Write-Host "pid $($p.Id) still running at timeout; stopping it." -ForegroundColor Yellow
        if (-not $KeepWindows) { Stop-Process -Id $p.Id -Force -ErrorAction SilentlyContinue }
    }
}
Start-Sleep -Milliseconds 500

function Read-AutoTestLines([string]$path, [string]$side) {
    if (-not (Test-Path $path)) { return @() }
    Get-Content -LiteralPath $path | Where-Object { $_ -match '\[AUTOTEST\]' } | ForEach-Object {
        $t = 0.0
        if ($_ -match 't=([0-9.]+)') { $t = [double]$Matches[1] }
        [pscustomobject]@{ Side = $side; Time = $t; Line = $_.Trim() }
    }
}

$merged = @()
$merged += Read-AutoTestLines $hostLog   "HOST  "
$merged += Read-AutoTestLines $clientLog "CLIENT"
$merged = $merged | Sort-Object Time

Write-Host ""
Write-Host "================ merged transcript ================" -ForegroundColor Cyan
foreach ($row in $merged) {
    $text = $row.Line -replace '^.*\[AUTOTEST\]\s*', ''
    $colour = "Gray"
    if ($text -match '\bFAIL\b|RESULT-FAIL') { $colour = "Red" }
    elseif ($text -match '\bOK\b|RESULT-PASS') { $colour = "Green" }
    elseif ($text -match '\bCOSMETIC\b') { $colour = "Magenta" }
    elseif ($text -match '\bSTEP\b') { $colour = "White" }
    Write-Host ("{0} | {1}" -f $row.Side, $text) -ForegroundColor $colour
}

Write-Host ""
Write-Host "================ summary ================" -ForegroundColor Cyan
$pass = ($merged | Where-Object { $_.Line -match 'RESULT-PASS' }).Count
$fail = ($merged | Where-Object { $_.Line -match 'RESULT-FAIL' }).Count
$fails = $merged | Where-Object { $_.Line -match '\bFAIL\b' }
if ($merged.Count -eq 0) {
    Write-Host "No [AUTOTEST] lines found. The harness did not run - check the raw logs:" -ForegroundColor Red
    Write-Host "  $hostLog"
    Write-Host "  $clientLog"
} else {
    Write-Host "sides reporting PASS: $pass    sides reporting FAIL: $fail"
    foreach ($f in $fails) { Write-Host ("  {0} {1}" -f $f.Side, ($f.Line -replace '^.*\[AUTOTEST\]\s*','')) -ForegroundColor Red }
}

if ($RawLogs) {
    foreach ($pair in @(@($hostLog, "HOST"), @($clientLog, "CLIENT"))) {
        Write-Host ""
        Write-Host "---------------- raw $($pair[1]) errors ----------------" -ForegroundColor DarkYellow
        if (Test-Path $pair[0]) {
            Get-Content -LiteralPath $pair[0] |
                Where-Object { $_ -match 'Exception|NullReference|error CS|Error:' } |
                Select-Object -First 40
        }
    }
}

Write-Host ""
Write-Host "raw logs:"
Write-Host "  $hostLog"
Write-Host "  $clientLog"

if ($fail -gt 0 -or $pass -lt 2) { exit 1 } else { exit 0 }
