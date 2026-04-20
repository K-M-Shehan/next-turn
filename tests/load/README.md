# NT-43 Load and Performance Testing

This folder contains k6 scripts and execution utilities for the NextTurn load-testing story (NT-43).

## Scenarios

- `queue-join-view.js`: 100-300 concurrent users joining and polling queue status.
- `staff-serve-skip.js`: 50 concurrent staff serving/skipping queue entries.
- `appointment-booking-spike.js`: booking spike when slots become available.
- `queue-graceful-degradation.js`: stress profile to demonstrate system limits and graceful degradation.

## Success Criteria

- Normal scenarios: `P95 < 2000ms`
- Normal scenarios: `Error rate < 1%`
- Throughput target: set via `NT_MIN_REQ_RATE`
- Stress scenario: record limit behavior (latency increase, contention profile, server stability)

## Configuration

1. Copy `tests/load/.env.example` to `tests/load/.env.qa`.
2. Fill in QA environment values and credentials.
3. Keep `.env.qa` out of source control.

Required variables:

- `NT_BASE_URL`
- `NT_TENANT_ID`
- `NT_QUEUE_ID`
- `NT_ORGANISATION_ID`
- `NT_APPOINTMENT_PROFILE_ID`
- `NT_QUEUE_USERS_JSON`
- `NT_STAFF_USERS_JSON`
- `NT_APPOINTMENT_USERS_JSON`

## Run Against QA (Manual, Not Pipeline)

Run all scenarios and generate a report:

```powershell
pwsh ./tests/load/run-qa.ps1 -Scenario all -EnvFile tests/load/.env.qa -RunNotes "NT-43 baseline run"
```

Run one scenario only:

```powershell
pwsh ./tests/load/run-qa.ps1 -Scenario queue -EnvFile tests/load/.env.qa
```

Outputs:

- JSON summaries in `tests/load/results/qa-<timestamp>/`
- Auto-generated markdown report in `tests/load/results/qa-<timestamp>/PERFORMANCE-TESTING-REPORT.md`

## Confluence Update Checklist

1. Copy report content from the generated markdown into **Performance Testing Report**.
2. Add k6 console snapshots and Azure metrics screenshots (App Service, SQL, App Insights).
3. Document bottlenecks and mitigation recommendations.
4. Link the test run folder and PR.

## Viva Slide Checklist

Use `tests/load/results/VIVA-SUMMARY-SLIDE-TEMPLATE.md` and fill:

- Scenario load profile
- P95/error/throughput outcomes
- Limit behavior from stress run
- Top 3 recommendations
