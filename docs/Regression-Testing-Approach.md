# Regression Testing Approach

## Purpose
Define and automate a consistent regression suite that runs before QA and main promotion to detect regressions early across backend logic and end-to-end user journeys.

## Scope
The regression suite includes:
- All unit tests
- Critical integration tests for auth, queue, and appointment flows
- All Playwright E2E journeys

## Test Inventory

### Unit Tests
Project:
- `tests/NextTurn.UnitTests/NextTurn.UnitTests.csproj`

Classification:
- Layer: Unit
- Suite: Regression
- Type: Full

Execution model:
- All tests in the unit test project run as part of regression.

### Critical Integration Tests
Project:
- `tests/NextTurn.IntegrationTests/NextTurn.IntegrationTests.csproj`

Included flow groups:
- `tests/NextTurn.IntegrationTests/Auth/*.cs`
- `tests/NextTurn.IntegrationTests/Queue/*.cs`
- `tests/NextTurn.IntegrationTests/Appointment/*.cs`

Classification:
- Layer: Integration
- Suite: Regression (xUnit Trait)
- Type: Full (xUnit Trait)

Applied xUnit traits on class-level:
- `Trait("Suite", "Regression")`
- `Trait("Type", "Full")`
- `Trait("Layer", "Integration")`

### Playwright E2E Journeys
Project:
- `tests/e2e-playwright/e2e-playwright.csproj`

Journeys:
- `Journey1_GlobalCitizenQueueTest` (login/join/leave)
- `Journey2_AppointmentTest` (book/cancel)
- `Journey3_OrgAdminTest`
- `Journey4_StaffTest` (serve/skip)
- `Journey5_QueuePositionUpdateTest`

Classification:
- Full regression category: `Category("Regression")` on all journey classes
- Smoke category: `Category("Smoke")` on selected tests

Smoke subset definition:
- Login: `Journey1_GlobalCitizenQueueTest.GlobalCitizenCanRegisterLoginJoinAndLeaveQueueAsync`
- Book: `Journey2AppointmentTest.CitizenCanBookAndCancelAppointmentAsync`
- Serve: `Journey4StaffTest.StaffCanServeAndSkipCitizensAsync`

## Tagging Strategy

### .NET/xUnit
- Filter dimensions:
  - Suite = Regression
  - Type = Full
  - Layer = Integration
- Unit tests are included by project-level selection (entire unit test project).
- Critical integration selection uses trait filtering.

### Playwright (NUnit)
- Regression filter: `NUnit.Where="cat == Regression"`
- Smoke filter: `NUnit.Where="cat == Smoke"`
- Smoke and full are selected by category filters in CI jobs.

## CI/CD Execution Points

## QA Branch Validation
Workflow:
- `.github/workflows/qa.yml`

Triggers:
- Push to `qa`
- PR targeting `qa`

Jobs:
- Unit Tests
- Critical Integration Tests
- Playwright Smoke (5-minute timeout gate on smoke test step)
- Playwright Full Regression
- Regression Summary

## Main Promotion Gate
Workflow:
- `.github/workflows/cd.yml`

Gate:
- `regression-gate` job calls reusable regression workflow (`qa.yml`) via `workflow_call`
- Backend and frontend deployment jobs depend on `regression-gate`
- Deploy does not proceed unless full regression suite is green

## Reporting and Artifacts
Each suite uploads artifacts with clear names:
- `unit-test-results` (TRX)
- `critical-integration-test-results` (TRX)
- `playwright-smoke-artifacts` (TRX + generated HTML + screenshots + traces)
- `playwright-regression-artifacts` (TRX + generated HTML + screenshots + traces)

GitHub Actions summaries include:
- Per-job status
- Filter used
- Artifact names
- Final summary with suite-level pass/fail and run link

## Local Run Commands
From repository root:

```bash
# Unit tests (full)
dotnet test tests/NextTurn.UnitTests/NextTurn.UnitTests.csproj --configuration Release

# Critical integration (auth + queue + appointment via traits)
dotnet test tests/NextTurn.IntegrationTests/NextTurn.IntegrationTests.csproj --configuration Release --filter "Suite=Regression&Layer=Integration"

# Playwright smoke (login + book + serve)
dotnet test tests/e2e-playwright/e2e-playwright.csproj --configuration Release --settings tests/e2e-playwright/playwright.runsettings -- NUnit.Where="cat == Smoke"

# Playwright full regression (all journeys)
dotnet test tests/e2e-playwright/e2e-playwright.csproj --configuration Release --settings tests/e2e-playwright/playwright.runsettings -- NUnit.Where="cat == Regression"
```

## Sprint Demo Steps
1. Push a branch to `qa` or open a PR targeting `qa`.
2. Open GitHub Actions and run/view `Regression Suite`.
3. Confirm all jobs are green:
   - Unit Tests
   - Critical Integration Tests
   - Playwright Smoke
   - Playwright Full Regression
4. Open artifacts and verify:
   - TRX files present for unit/integration/e2e
   - Playwright HTML report exists in smoke and full artifacts
   - Screenshots/traces available when failures occur
5. Trigger `main` deployment and confirm `Regression Gate (pre-main deploy)` runs and passes before deployment jobs start.

## Risks and Known Gaps
- Smoke completion target (<5 minutes) is enforced by timeout in CI, but actual duration depends on environment speed and target environment responsiveness.
- Full Playwright regression duration can vary with hosted runner performance and external environment latency.
- Integration tests rely on test container/database setup; temporary infrastructure/network issues can impact timing.

## Ownership
- QA owns smoke and regression scope curation.
- Dev + QA jointly own test stability, flaky test triage, and CI reliability.
