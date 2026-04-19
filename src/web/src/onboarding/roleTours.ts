export type OnboardingRole = 'citizen' | 'staff' | 'admin'

export interface OnboardingStep {
  target: string
  title: string
  description: string
}

export const ROLE_TOUR_CONTENT: Record<OnboardingRole, OnboardingStep[]> = {
  citizen: [
    {
      target: '[data-onboarding="citizen-sidebar"]',
      title: 'Dashboard navigation',
      description: 'Use this sidebar to move between home, queues, appointments, and notifications.',
    },
    {
      target: '[data-onboarding="citizen-queues-tab"]',
      title: 'Queue activity',
      description: 'Open Queues to see active tickets and re-enter queue dashboards quickly.',
    },
    {
      target: '[data-onboarding="citizen-appointments-tab"]',
      title: 'Appointment activity',
      description: 'Appointments keeps your upcoming bookings and cancellation actions in one place.',
    },
    {
      target: '[data-onboarding="citizen-notifications-tab"]',
      title: 'Notifications',
      description: 'Notifications combines in-app alerts and your notification preferences.',
    },
    {
      target: '[data-onboarding="citizen-queue-link"]',
      title: 'Join by link',
      description: 'Paste a queue link here to jump straight into a queue.',
    },
  ],
  staff: [
    {
      target: '[data-onboarding="staff-queue-selector"]',
      title: 'Queue selector',
      description: 'Select which assigned queue you are currently operating.',
    },
    {
      target: '[data-onboarding="staff-actions"]',
      title: 'Serve and skip controls',
      description: 'Use these controls to progress queue flow in real time.',
    },
    {
      target: '[data-onboarding="staff-waiting-list"]',
      title: 'Waiting line',
      description: 'Track incoming tickets and who should be called next.',
    },
    {
      target: '[data-onboarding="staff-settings"]',
      title: 'Profile and settings',
      description: 'Restart onboarding here at any time if you want a refresher.',
    },
  ],
  admin: [
    {
      target: '[data-onboarding="admin-sidebar"]',
      title: 'Operations sidebar',
      description: 'Use tabs to switch between queues, appointments, staff, and reporting workspaces.',
    },
    {
      target: '[data-onboarding="admin-queues-tab"]',
      title: 'Queue management',
      description: 'Create, update, and assign queues from this tab.',
    },
    {
      target: '[data-onboarding="admin-appointments-tab"]',
      title: 'Appointment management',
      description: 'Configure schedules and profile availability here.',
    },
    {
      target: '[data-onboarding="admin-staff-tab"]',
      title: 'Staff management',
      description: 'Invite staff and maintain account access in one place.',
    },
    {
      target: '[data-onboarding="admin-reports-tab"]',
      title: 'Inline reports',
      description: 'Generate queue performance and daily summaries without leaving the dashboard.',
    },
    {
      target: '[data-onboarding="admin-settings"]',
      title: 'Profile and settings',
      description: 'Restart onboarding from settings whenever you need a walkthrough again.',
    },
  ],
}
