# NextTurn Performance Report (NT-43) - Local Validation Run

## 1. Test Context

- Environment: Local fallback (API local, SQL in Docker container nextturn-sql)
- API base URL: http://localhost:5258
- Test date: 2026-04-20
- Test operator: GitHub Copilot automation run
- Notes: This run validates scripts and thresholds in a production-like local setup when QA credentials are unavailable.

## 2. Success Criteria

- P95 response time < 2000ms
- Error rate < 1%
- Throughput meets expected profile per scenario

## 3. Scenario Matrix

| Scenario | Script | Users (VUs) | Duration | Result |
|---|---|---:|---:|---|
| Queue join/view | tests/load/scenarios/queue-join-view.js | 60 | 1m | PASS |
| Staff serve/skip | tests/load/scenarios/staff-serve-skip.js | 20 | 1m | PASS |
| Appointment booking spike | tests/load/scenarios/appointment-booking-spike.js | 40 | 1m | PASS |

## 4. Key Metrics

| Scenario | P95 (ms) | Error Rate (%) | Throughput (req/s) |
|---|---:|---:|---:|
| Queue join/view | 74.81 | 0.00 | 104.85 |
| Staff serve/skip | 50.31 | 0.00 | 44.02 |
| Appointment booking spike | 49.19 | 0.00 | 70.90 |

## 5. Interpretation

- All three scenarios met the configured NT-43 threshold gates in this local validation run.
- Throughput remained stable across scenarios under constant VU load.
- No transport-level errors remained after:
  - using login-global for appointment users,
  - reducing login burst size to remain under auth rate limits,
  - treating expected business-contention statuses as expected responses in k6.

## 6. Bottlenecks/Issues Found and Fixed During Setup

1. LocalDB startup failure blocked initial local execution.
2. API/database mismatch (NextTurnDev vs NextTurn_LoadTest) caused SQL error 4060.
3. Auth login rate limit (10/min/IP) caused setup failures with oversized credential pools.
4. Staff scenario initially included non-assigned staff users, causing authorization failures.

## 7. Artifacts Produced

- tests/load/results/queue-summary.json
- tests/load/results/staff-summary.json
- tests/load/results/appointment-summary.json
- tests/load/results/local-bootstrap-values.json
- tests/load/results/local-api.log
- tests/load/results/local-api.err.log

## 8. QA Execution Readiness

The load suite is now execution-ready for QA with the same scripts and env model. For QA runs, provide NT_BASE_URL and QA credentials/resource IDs (or bootstrap values) and re-run the same scenario commands documented in tests/load/README.md.
