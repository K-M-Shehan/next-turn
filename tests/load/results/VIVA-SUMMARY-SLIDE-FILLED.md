# Viva Slide Content - NT-43 Load and Performance Testing

## Objective

Validate NextTurn performance under realistic concurrent load before final delivery.

## Scenarios Executed

- Queue join/view load
- Staff serve/skip concurrent operations
- Appointment booking spike

## Success Criteria

- P95 response time < 2s
- Error rate < 1%
- Stable throughput under sustained load

## Results (Local Validation Run)

- Queue join/view: PASS | P95: 74.81ms | Error: 0.00% | Throughput: 104.85 req/s
- Staff serve/skip: PASS | P95: 50.31ms | Error: 0.00% | Throughput: 44.02 req/s
- Appointment booking: PASS | P95: 49.19ms | Error: 0.00% | Throughput: 70.90 req/s

## Key Findings

1. Baseline API latency under tested load remained significantly below 2s P95 target.
2. Error rate stabilized to zero after correcting expected status handling and setup data constraints.
3. Tooling and scripts are now reproducible and ready for QA environment execution.

## Risks / Follow-up

1. Execute the same suite on QA with real infrastructure and data shape.
2. Add charts from JSON outputs to Confluence for final sign-off.
3. Track any infra-specific regressions (network latency, DB DTU/vCore saturation) observed in QA.
