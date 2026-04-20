# Onboarding Tour (Citizen, Staff, Admin)

## Overview
The web app now includes a role-aware onboarding tour that appears automatically for first-time users and can be replayed at any time from profile/settings controls.

## Behavior
- First run: The tour appears the first time a user opens their role dashboard.
- Persistence: Completion is stored in browser local storage per user-role-tenant scope.
- Replay: Users can click `Restart onboarding tour` from profile/settings sections.
- UX: Tour is skippable and non-blocking.

## Role-Specific Coverage
- Citizen dashboard:
  - Sidebar navigation
  - Queues tab
  - Appointments tab
  - Notifications tab
  - Join-by-link widget
- Staff dashboard:
  - Queue selector
  - Serve/Skip controls
  - Waiting list
  - Profile/settings restart control
- Admin dashboard:
  - Operations sidebar
  - Queue, Appointment, Staff, and Reports tabs
  - Profile/settings restart control

## Implementation Notes
- Shared component: `src/web/src/components/OnboardingTour/OnboardingTour.tsx`
- Role content source: `src/web/src/onboarding/roleTours.ts`
- First-run logic: `src/web/src/onboarding/useOnboardingTour.ts`

## Validation
- Dashboard tests include onboarding visibility and restart assertions:
  - `src/web/src/pages/Dashboard/__tests__/DashboardPage.test.tsx`
  - `src/web/src/pages/Staff/__tests__/StaffDashboardPage.test.tsx`
  - `src/web/src/pages/Admin/__tests__/AdminDashboardPage.test.tsx`
