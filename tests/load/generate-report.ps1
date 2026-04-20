param(
  [Parameter(Mandatory = $true)] [string]$ResultsDirectory,
  [Parameter(Mandatory = $true)] [string]$EnvironmentName,
  [string]$OutputFile,
  [string]$ConfluenceUrl = "",
  [string]$RunNotes = ""
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Get-MetricValue {
  param(
    [object]$Metric,
    [Parameter(Mandatory = $true)] [string]$Key,
    [double]$DefaultValue = 0
  )

  if ($null -eq $Metric) {
    return $DefaultValue
  }

  $directProperty = $Metric.PSObject.Properties[$Key]
  if ($null -ne $directProperty -and $null -ne $directProperty.Value) {
    return [double]$directProperty.Value
  }

  $valuesProperty = $Metric.PSObject.Properties["values"]
  if ($null -ne $valuesProperty -and $null -ne $valuesProperty.Value) {
    $nested = $valuesProperty.Value.PSObject.Properties[$Key]
    if ($null -ne $nested -and $null -ne $nested.Value) {
      return [double]$nested.Value
    }
  }

  return $DefaultValue
}

function Get-ScenarioMetrics {
  param(
    [Parameter(Mandatory = $true)] [string]$ScenarioName,
    [Parameter(Mandatory = $true)] [string]$SummaryPath
  )

  if (-not (Test-Path $SummaryPath)) {
    throw "Summary not found for scenario '$ScenarioName': $SummaryPath"
  }

  $json = Get-Content $SummaryPath -Raw | ConvertFrom-Json

  $p95 = Get-MetricValue -Metric $json.metrics.http_req_duration -Key 'p(95)'
  $errorRate = Get-MetricValue -Metric $json.metrics.http_req_failed -Key 'rate'
  if ($errorRate -eq 0) {
    $errorRate = Get-MetricValue -Metric $json.metrics.http_req_failed -Key 'value'
  }
  $reqRate = Get-MetricValue -Metric $json.metrics.http_reqs -Key 'rate'

  return [pscustomobject]@{
    Scenario = $ScenarioName
    P95Ms = [math]::Round($p95, 2)
    ErrorRatePct = [math]::Round(($errorRate * 100), 3)
    ReqRate = [math]::Round($reqRate, 2)
  }
}

$resolvedResultsDirectory = (Resolve-Path $ResultsDirectory).Path
$timestamp = Get-Date -Format "yyyy-MM-dd HH:mm:ss"

$scenarioFiles = @{
  "Queue join/view" = "queue-summary.json"
  "Staff serve/skip" = "staff-summary.json"
  "Appointment booking spike" = "appointment-summary.json"
  "Graceful degradation (stress)" = "stress-summary.json"
}

$metrics = @()
foreach ($kvp in $scenarioFiles.GetEnumerator()) {
  $summaryPath = Join-Path $resolvedResultsDirectory $kvp.Value
  if (Test-Path $summaryPath) {
    $metrics += Get-ScenarioMetrics -ScenarioName $kvp.Key -SummaryPath $summaryPath
  }
}

if ($metrics.Count -eq 0) {
  throw "No scenario summary files found in $resolvedResultsDirectory"
}

if ([string]::IsNullOrWhiteSpace($OutputFile)) {
  $OutputFile = Join-Path $resolvedResultsDirectory "PERFORMANCE-TESTING-REPORT.md"
}

$rows = @()
foreach ($item in $metrics) {
  $rows += "| $($item.Scenario) | $($item.P95Ms) | $($item.ErrorRatePct) | $($item.ReqRate) |"
}

$p95BarData = ($metrics | ForEach-Object { "`"$($_.Scenario)`"" }) -join ", "
$p95Values = ($metrics | ForEach-Object { "$($_.P95Ms)" }) -join ", "
$rateValues = ($metrics | ForEach-Object { "$($_.ReqRate)" }) -join ", "

$confluenceLine = if ([string]::IsNullOrWhiteSpace($ConfluenceUrl)) {
  "- Confluence page: Add link after publishing (Performance Testing Report)"
} else {
  "- Confluence page: $ConfluenceUrl"
}

$notesLine = if ([string]::IsNullOrWhiteSpace($RunNotes)) {
  "- Run notes: Not provided"
} else {
  "- Run notes: $RunNotes"
}

$content = @"
# NextTurn Performance Testing Report (NT-43)

## 1. Test Context

- Environment: $EnvironmentName
- Execution time: $timestamp
- Result folder: $resolvedResultsDirectory
$confluenceLine
$notesLine

## 2. Success Criteria

- P95 response time < 2000ms for normal scenarios
- Error rate < 1% for normal scenarios
- Throughput remains stable and does not collapse under expected peak load
- Stress scenario documents system limit behavior and graceful degradation pattern

## 3. Scenario Results

| Scenario | P95 (ms) | Error Rate (%) | Throughput (req/s) |
|---|---:|---:|---:|
$($rows -join "`n")

## 4. Graphs

### P95 by Scenario

~~~mermaid
xychart-beta
  title "P95 Response Time by Scenario"
  x-axis [$p95BarData]
  y-axis "P95 (ms)" 0 --> 5000
  bar [$p95Values]
~~~

### Throughput by Scenario

~~~mermaid
xychart-beta
  title "Throughput by Scenario"
  x-axis [$p95BarData]
  y-axis "Requests per second" 0 --> 500
  bar [$rateValues]
~~~

## 5. Bottlenecks and Observations

- Fill this section with bottlenecks found in QA telemetry (App Service CPU, SQL DTU/vCore, lock/contention, rate limiting).
- Confirm whether failures were transport failures (5xx/timeout) or business contention (409/400 expected under pressure).
- Add links/screenshots from App Insights, Azure metrics, and k6 run output.

## 6. Graceful Degradation Evidence

- Describe how the stress scenario behaved as VUs ramped up.
- Document whether latency increased gradually and whether error profile stayed controlled.
- State limit reached (for example sustained VUs or req/s) and recommended operating envelope.

## 7. Recommendations

1. Keep k6 scripts in routine QA regression before release cut.
2. Add alerts for latency and error SLOs in QA and production.
3. Re-run after any major queue/appointment workflow changes.
"@

Set-Content -Path $OutputFile -Value $content -Encoding UTF8
Write-Host "Report generated: $OutputFile"
