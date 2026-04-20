# Performance Testing Report (Confluence Draft)

## Test Scope

- Story: NT-43 Load and Performance Testing
- Environment: QA
- Date:
- Operator:
- Build/Commit:

## Scenarios and Profiles

| Scenario | Users / VUs | Duration | Goal |
|---|---:|---:|---|
| Queue join/view | | | Validate customer queue traffic under realistic concurrency |
| Staff serve/skip | | | Validate staff operation concurrency and consistency |
| Appointment booking spike | | | Validate slot booking under burst demand |
| Graceful degradation (stress) | | | Demonstrate limits and stability behavior |

## Success Criteria

- P95 response time < 2s for normal scenarios
- Error rate < 1% for normal scenarios
- Throughput meets expected target for each scenario
- Stress run documents limits and degradation pattern

## Results Summary

| Scenario | P95 (ms) | Error Rate (%) | Throughput (req/s) | Status |
|---|---:|---:|---:|---|
| Queue join/view | | | | |
| Staff serve/skip | | | | |
| Appointment booking spike | | | | |
| Graceful degradation (stress) | | | | |

## Graphs

- Insert chart screenshot(s) from k6 outputs.
- Insert Azure App Service and SQL usage charts for the same test window.

## Bottlenecks and Findings

1. 
2. 
3. 

## Recommendations

1. 
2. 
3. 

## Follow-up Actions

1. 
2. 
3. 
