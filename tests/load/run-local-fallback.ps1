Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
Set-Location $repoRoot

$baseUrl = if ($env:NT_BASE_URL) { $env:NT_BASE_URL.TrimEnd('/') } else { "http://localhost:5258" }

$sqlHost = if ($env:NT_SQL_HOST) { $env:NT_SQL_HOST } else { "nextturn-sql" }
$sqlPort = if ($env:NT_SQL_PORT) { $env:NT_SQL_PORT } else { "1433" }
$sqlDb = if ($env:NT_SQL_DB) { $env:NT_SQL_DB } else { "NextTurnDev" }
$sqlUser = if ($env:NT_SQL_USER) { $env:NT_SQL_USER } else { "sa" }
$sqlPassword = if ($env:NT_SQL_PASSWORD) { $env:NT_SQL_PASSWORD } else { "NextTurn_Dev#2026" }

$conn = "Server=$sqlHost,$sqlPort;Database=$sqlDb;User Id=$sqlUser;Password=$sqlPassword;TrustServerCertificate=True;MultipleActiveResultSets=true"
$jwtSecret = "local-load-testing-secret-key-1234567890"
$resultsDir = Join-Path $repoRoot "tests/load/results"
$logFile = Join-Path $resultsDir "local-api.log"
$errLogFile = Join-Path $resultsDir "local-api.err.log"

New-Item -ItemType Directory -Force -Path $resultsDir | Out-Null
if (Test-Path $logFile) {
  Remove-Item $logFile -Force
}
if (Test-Path $errLogFile) {
  Remove-Item $errLogFile -Force
}

$env:ConnectionStrings__DefaultConnection = $conn
$env:JwtSettings__Secret = $jwtSecret
$env:ASPNETCORE_ENVIRONMENT = "Development"

function Write-Step($message) {
  Write-Host "==> $message"
}

function Resolve-CommandPath {
  param(
    [Parameter(Mandatory = $true)] [string]$CommandName,
    [string[]]$CandidatePaths = @()
  )

  $command = Get-Command $CommandName -ErrorAction SilentlyContinue
  if ($null -ne $command) {
    return $command.Source
  }

  foreach ($path in $CandidatePaths) {
    if (Test-Path $path) {
      return $path
    }
  }

  throw "Command not found: $CommandName"
}

function Invoke-Json {
  param(
    [Parameter(Mandatory = $true)] [string]$Method,
    [Parameter(Mandatory = $true)] [string]$Url,
    [hashtable]$Headers,
    [object]$Body
  )

  $args = @{
    Method = $Method
    Uri = $Url
    Headers = $Headers
  }

  if ($null -ne $Body) {
    $args["Body"] = ($Body | ConvertTo-Json -Depth 8 -Compress)
    $args["ContentType"] = "application/json"
  }

  return Invoke-RestMethod @args
}

function Wait-ApiReady {
  param([int]$TimeoutSeconds = 90)

  $deadline = (Get-Date).AddSeconds($TimeoutSeconds)
  while ((Get-Date) -lt $deadline) {
    try {
      $seed = Invoke-Json -Method "POST" -Url "$baseUrl/api/dev/seed"
      if ($seed -and $seed.tenantId) {
        return $seed
      }
    } catch {
      Start-Sleep -Milliseconds 700
    }
  }

  throw "API was not ready within $TimeoutSeconds seconds."
}

function Get-LatestTemporaryPassword {
  param([int]$TimeoutSeconds = 30)

  $deadline = (Get-Date).AddSeconds($TimeoutSeconds)
  while ((Get-Date) -lt $deadline) {
    if (Test-Path $logFile) {
      $text = Get-Content $logFile -Raw
      if ($text -match "Temporary password:\s*([^\s\r\n]+)") {
        return $Matches[1].Trim()
      }
    }

    Start-Sleep -Milliseconds 700
  }

  throw "Could not find temporary admin password in local API log."
}

function New-TenantUsers {
  param(
    [string]$TenantId,
    [int]$Count,
    [string]$Suffix
  )

  $users = @()
  for ($i = 1; $i -le $Count; $i++) {
    $email = "load-tenant-$Suffix-$i@example.test"
    $password = "P@ssw0rd123!"

    Invoke-Json -Method "POST" -Url "$baseUrl/api/auth/register" -Headers @{ "X-Tenant-Id" = $TenantId } -Body @{
      name = "Tenant User $i"
      email = $email
      phone = "070000000$i"
      password = $password
    } | Out-Null

    $users += [pscustomobject]@{ email = $email; password = $password }
  }

  return $users
}

function New-GlobalUsers {
  param(
    [int]$Count,
    [string]$Suffix
  )

  $users = @()
  for ($i = 1; $i -le $Count; $i++) {
    $email = "load-global-$Suffix-$i@example.test"
    $password = "P@ssw0rd123!"

    Invoke-Json -Method "POST" -Url "$baseUrl/api/auth/register-global" -Body @{
      name = "Global User $i"
      email = $email
      phone = "080000000$i"
      password = $password
    } | Out-Null

    $users += [pscustomobject]@{ email = $email; password = $password }
  }

  return $users
}

function Get-ScenarioMetrics {
  param([string]$SummaryPath)

  $json = Get-Content $SummaryPath -Raw | ConvertFrom-Json

  $duration = $json.metrics.http_req_duration
  $failed = $json.metrics.http_req_failed
  $reqs = $json.metrics.http_reqs

  $p95 = if ($null -ne $duration.'p(95)') { [double]$duration.'p(95)' } else { [double]$duration.values.'p(95)' }
  $errorRate = if ($null -ne $failed.value) { [double]$failed.value } else { [double]$failed.values.rate }
  $reqRate = if ($null -ne $reqs.rate) { [double]$reqs.rate } else { [double]$reqs.values.rate }

  return [pscustomobject]@{
    P95 = $p95
    ErrorRate = $errorRate
    ReqRate = $reqRate
  }
}

$apiProcess = $null

try {
  $sqlcmdExe = Resolve-CommandPath -CommandName "sqlcmd" -CandidatePaths @(
    "C:\Program Files\SqlCmd\sqlcmd.exe"
  )
  $k6Exe = Resolve-CommandPath -CommandName "k6" -CandidatePaths @(
    "C:\Program Files\k6\k6.exe"
  )

  Write-Step "Ensuring SQL database exists"
  & $sqlcmdExe -S "$sqlHost,$sqlPort" -U $sqlUser -P $sqlPassword -Q "IF DB_ID('$sqlDb') IS NULL CREATE DATABASE [$sqlDb];" | Out-Host

  Write-Step "Applying EF migrations"
  dotnet ef database update --project src/NextTurn.Infrastructure --startup-project src/NextTurn.API | Out-Host

  Write-Step "Starting API"
  $apiProcess = Start-Process -FilePath "dotnet" `
    -ArgumentList "run --project src/NextTurn.API --no-launch-profile --urls http://localhost:5258" `
    -WorkingDirectory $repoRoot `
    -RedirectStandardOutput $logFile `
    -RedirectStandardError $errLogFile `
    -PassThru

  $seed = Wait-ApiReady
  Write-Step "API is ready"

  $suffix = [DateTimeOffset]::UtcNow.ToUnixTimeSeconds().ToString()
  $adminEmail = "load-admin-$suffix@example.test"

  Write-Step "Registering organisation"
  $org = Invoke-Json -Method "POST" -Url "$baseUrl/api/organisations" -Body @{
    orgName = "Load Test Org $suffix"
    addressLine1 = "1 Load Street"
    city = "Load City"
    postalCode = "L0 0AD"
    country = "GB"
    orgType = "Government"
    adminName = "Load Admin"
    adminEmail = $adminEmail
  }

  $tenantId = [string]$org.organisationId
  if ([string]::IsNullOrWhiteSpace($tenantId)) {
    throw "Organisation registration did not return organisationId."
  }

  $adminPassword = Get-LatestTemporaryPassword

  Write-Step "Logging in org admin"
  $adminLogin = Invoke-Json -Method "POST" -Url "$baseUrl/api/auth/login" -Headers @{ "X-Tenant-Id" = $tenantId } -Body @{
    email = $adminEmail
    password = $adminPassword
  }

  $adminToken = [string]$adminLogin.accessToken
  if ([string]::IsNullOrWhiteSpace($adminToken)) {
    throw "Admin login did not return accessToken."
  }

  $adminHeaders = @{
    Authorization = "Bearer $adminToken"
    "X-Tenant-Id" = $tenantId
  }

  Write-Step "Creating queue"
  $queue = Invoke-Json -Method "POST" -Url "$baseUrl/api/queues" -Headers $adminHeaders -Body @{
    name = "Load Queue $suffix"
    maxCapacity = 1000
    averageServiceTimeSeconds = 120
  }

  $queueId = [string]$queue.queueId

  Write-Step "Creating appointment profile"
  $profile = Invoke-Json -Method "POST" -Url "$baseUrl/api/appointments/profiles" -Headers $adminHeaders -Body @{
    name = "Load Appointment Profile $suffix"
  }

  $appointmentProfileId = [string]$profile.appointmentProfileId

  $dayRules = @(
    @{ dayOfWeek = 0; isEnabled = $false; startTime = "09:00:00"; endTime = "17:00:00"; slotDurationMinutes = 30 },
    @{ dayOfWeek = 1; isEnabled = $true; startTime = "09:00:00"; endTime = "17:00:00"; slotDurationMinutes = 30 },
    @{ dayOfWeek = 2; isEnabled = $true; startTime = "09:00:00"; endTime = "17:00:00"; slotDurationMinutes = 30 },
    @{ dayOfWeek = 3; isEnabled = $true; startTime = "09:00:00"; endTime = "17:00:00"; slotDurationMinutes = 30 },
    @{ dayOfWeek = 4; isEnabled = $true; startTime = "09:00:00"; endTime = "17:00:00"; slotDurationMinutes = 30 },
    @{ dayOfWeek = 5; isEnabled = $true; startTime = "09:00:00"; endTime = "17:00:00"; slotDurationMinutes = 30 },
    @{ dayOfWeek = 6; isEnabled = $false; startTime = "09:00:00"; endTime = "17:00:00"; slotDurationMinutes = 30 }
  )

  Write-Step "Configuring appointment schedule"
  Invoke-Json -Method "PUT" -Url "$baseUrl/api/appointments/config?appointmentProfileId=$appointmentProfileId" -Headers $adminHeaders -Body @{ dayRules = $dayRules } | Out-Null

  Write-Step "Creating staff users"
  $staffCreds = @()
  for ($i = 1; $i -le 5; $i++) {
    $email = "load-staff-$suffix-$i@example.test"
    $password = "P@ssw0rd123!"

    Invoke-Json -Method "POST" -Url "$baseUrl/api/auth/staff" -Headers $adminHeaders -Body @{
      name = "Load Staff $i"
      email = $email
      phone = "090000000$i"
      password = $password
    } | Out-Null

    $staffCreds += [pscustomobject]@{ email = $email; password = $password }
  }

  $staffList = Invoke-Json -Method "GET" -Url "$baseUrl/api/auth/staff" -Headers $adminHeaders
  $staffIds = @($staffList | Select-Object -First 3 | ForEach-Object { [string]$_.userId })

  $assignedStaffEmails = @($staffList | Select-Object -First 3 | ForEach-Object { [string]$_.email })
  $assignedStaffCreds = @($staffCreds | Where-Object { $assignedStaffEmails -contains $_.email })

  Write-Step "Assigning staff users to queue"
  foreach ($staffId in $staffIds) {
    Invoke-Json -Method "POST" -Url "$baseUrl/api/queues/$queueId/staff-assignments/$staffId" -Headers $adminHeaders | Out-Null
  }

  Write-Step "Creating tenant and global users"
  # Keep setup login bursts below login endpoint rate limit (10 req / 60s per IP).
  $queueUsers = New-TenantUsers -TenantId $tenantId -Count 8 -Suffix $suffix
  $appointmentUsers = New-GlobalUsers -Count 8 -Suffix $suffix

  Write-Step "Setting k6 environment"
  $env:NT_BASE_URL = $baseUrl
  $env:NT_TENANT_ID = $tenantId
  $env:NT_QUEUE_ID = $queueId
  $env:NT_ORGANISATION_ID = $tenantId
  $env:NT_APPOINTMENT_PROFILE_ID = $appointmentProfileId

  $env:NT_QUEUE_USERS_JSON = ($queueUsers | ConvertTo-Json -Compress)
  $env:NT_STAFF_USERS_JSON = ($assignedStaffCreds | ConvertTo-Json -Compress)
  $env:NT_APPOINTMENT_USERS_JSON = ($appointmentUsers | ConvertTo-Json -Compress)

  $env:NT_QUEUE_VUS = "60"
  $env:NT_QUEUE_DURATION = "1m"
  $env:NT_STAFF_VUS = "20"
  $env:NT_STAFF_DURATION = "1m"
  $env:NT_APPOINTMENT_VUS = "40"
  $env:NT_APPOINTMENT_DURATION = "1m"
  $env:NT_P95_MS = "2000"
  $env:NT_MAX_ERROR_RATE = "0.01"
  $env:NT_MIN_REQ_RATE = "5"

  Write-Step "Running queue scenario"
  & $k6Exe run tests/load/scenarios/queue-join-view.js --summary-export=tests/load/results/queue-summary.json | Out-Host

  Write-Step "Running staff scenario"
  & $k6Exe run tests/load/scenarios/staff-serve-skip.js --summary-export=tests/load/results/staff-summary.json | Out-Host

  Write-Step "Running appointment scenario"
  & $k6Exe run tests/load/scenarios/appointment-booking-spike.js --summary-export=tests/load/results/appointment-summary.json | Out-Host

  $queueMetrics = Get-ScenarioMetrics -SummaryPath (Join-Path $resultsDir "queue-summary.json")
  $staffMetrics = Get-ScenarioMetrics -SummaryPath (Join-Path $resultsDir "staff-summary.json")
  $apptMetrics = Get-ScenarioMetrics -SummaryPath (Join-Path $resultsDir "appointment-summary.json")

  $bootstrap = [pscustomobject]@{
    baseUrl = $baseUrl
    tenantId = $tenantId
    queueId = $queueId
    organisationId = $tenantId
    appointmentProfileId = $appointmentProfileId
    queueUsers = $queueUsers
    staffUsers = $assignedStaffCreds
    appointmentUsers = $appointmentUsers
    metrics = [pscustomobject]@{
      queue = $queueMetrics
      staff = $staffMetrics
      appointment = $apptMetrics
    }
  }

  $bootstrap | ConvertTo-Json -Depth 8 | Set-Content -Path (Join-Path $resultsDir "local-bootstrap-values.json") -Encoding UTF8

  Write-Step "Run complete"
  Write-Host "queue p95=$([math]::Round($queueMetrics.P95,2))ms error=$([math]::Round($queueMetrics.ErrorRate,4)) reqRate=$([math]::Round($queueMetrics.ReqRate,2))"
  Write-Host "staff p95=$([math]::Round($staffMetrics.P95,2))ms error=$([math]::Round($staffMetrics.ErrorRate,4)) reqRate=$([math]::Round($staffMetrics.ReqRate,2))"
  Write-Host "appointment p95=$([math]::Round($apptMetrics.P95,2))ms error=$([math]::Round($apptMetrics.ErrorRate,4)) reqRate=$([math]::Round($apptMetrics.ReqRate,2))"
}
finally {
  if ($null -ne $apiProcess -and -not $apiProcess.HasExited) {
    Stop-Process -Id $apiProcess.Id -Force
  }
}
