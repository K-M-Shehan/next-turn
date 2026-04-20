param(
  [ValidateSet("all", "queue", "staff", "appointment", "stress")]
  [string]$Scenario = "all",
  [string]$EnvFile = "tests/load/.env.qa",
  [string]$ResultsRoot = "tests/load/results",
  [switch]$AllowNonQaBaseUrl,
  [string]$ConfluenceUrl = "",
  [string]$RunNotes = ""
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
Set-Location $repoRoot

function Import-EnvFile {
  param([Parameter(Mandatory = $true)] [string]$Path)

  if (-not (Test-Path $Path)) {
    throw "Environment file not found: $Path"
  }

  foreach ($line in Get-Content $Path) {
    $trimmed = $line.Trim()
    if ([string]::IsNullOrWhiteSpace($trimmed) -or $trimmed.StartsWith("#")) {
      continue
    }

    $parts = $trimmed.Split("=", 2)
    if ($parts.Length -ne 2) {
      continue
    }

    $name = $parts[0].Trim()
    $value = $parts[1].Trim()
    [Environment]::SetEnvironmentVariable($name, $value)
  }
}

function Require-Env {
  param([Parameter(Mandatory = $true)] [string]$Name)

  $value = [Environment]::GetEnvironmentVariable($Name)
  if ([string]::IsNullOrWhiteSpace($value)) {
    throw "Required environment variable missing: $Name"
  }
}

function Write-Step {
  param([Parameter(Mandatory = $true)] [string]$Message)
  Write-Host "==> $Message"
}

function Run-Scenario {
  param(
    [Parameter(Mandatory = $true)] [string]$Name,
    [Parameter(Mandatory = $true)] [string]$ScriptPath,
    [Parameter(Mandatory = $true)] [string]$SummaryPath
  )

  Write-Step "Running scenario: $Name"
  k6 run $ScriptPath --summary-export=$SummaryPath
}

Import-EnvFile -Path $EnvFile

Require-Env -Name "NT_BASE_URL"
Require-Env -Name "NT_TENANT_ID"
Require-Env -Name "NT_QUEUE_ID"
Require-Env -Name "NT_QUEUE_USERS_JSON"
Require-Env -Name "NT_STAFF_USERS_JSON"
Require-Env -Name "NT_ORGANISATION_ID"
Require-Env -Name "NT_APPOINTMENT_PROFILE_ID"
Require-Env -Name "NT_APPOINTMENT_USERS_JSON"

$baseUrl = [Environment]::GetEnvironmentVariable("NT_BASE_URL")
if (-not $AllowNonQaBaseUrl -and ($baseUrl -notmatch "qa")) {
  throw "NT_BASE_URL must target QA. Use -AllowNonQaBaseUrl only for emergency fallback runs. Current value: $baseUrl"
}

$timestamp = Get-Date -Format "yyyyMMdd-HHmmss"
$resultDir = Join-Path $ResultsRoot "qa-$timestamp"
New-Item -ItemType Directory -Path $resultDir -Force | Out-Null

$scripts = @{
  queue = "tests/load/scenarios/queue-join-view.js"
  staff = "tests/load/scenarios/staff-serve-skip.js"
  appointment = "tests/load/scenarios/appointment-booking-spike.js"
  stress = "tests/load/scenarios/queue-graceful-degradation.js"
}

if ($Scenario -eq "all") {
  Run-Scenario -Name "queue" -ScriptPath $scripts.queue -SummaryPath (Join-Path $resultDir "queue-summary.json")
  Run-Scenario -Name "staff" -ScriptPath $scripts.staff -SummaryPath (Join-Path $resultDir "staff-summary.json")
  Run-Scenario -Name "appointment" -ScriptPath $scripts.appointment -SummaryPath (Join-Path $resultDir "appointment-summary.json")
  Run-Scenario -Name "stress" -ScriptPath $scripts.stress -SummaryPath (Join-Path $resultDir "stress-summary.json")
} else {
  Run-Scenario -Name $Scenario -ScriptPath $scripts[$Scenario] -SummaryPath (Join-Path $resultDir "$Scenario-summary.json")
}

Write-Step "Generating markdown report"
& "tests/load/generate-report.ps1" -ResultsDirectory $resultDir -EnvironmentName "QA" -ConfluenceUrl $ConfluenceUrl -RunNotes $RunNotes

Write-Host "NT-43 QA run completed. Results directory: $resultDir"
