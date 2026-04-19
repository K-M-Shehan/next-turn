export type OnboardingRole = 'citizen' | 'staff' | 'admin'

export interface OnboardingStep {
  target: string
  title: string
  description: string
  activateTarget?: string
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
      description: 'Click this tab to open your live queue tickets and queue links.',
    },
    {
      target: '[data-onboarding="citizen-queues-list"]',
      title: 'Queue details',
      description: 'This panel shows each active queue ticket, status, and quick link to the queue page.',
      activateTarget: '[data-onboarding="citizen-queues-tab"]',
    },
    {
      target: '[data-onboarding="citizen-appointments-tab"]',
      title: 'Appointment activity',
      description: 'Click Appointments to review bookings, timeslots, and cancellation actions.',
    },
    {
      target: '[data-onboarding="citizen-appointments-list"]',
      title: 'Appointment details',
      description: 'Each card here includes profile name, slot time, and actions like View or Cancel.',
      activateTarget: '[data-onboarding="citizen-appointments-tab"]',
    },
    {
      target: '[data-onboarding="citizen-notifications-tab"]',
      title: 'Notifications',
      description: 'Click Notifications to view real-time alerts about queue and appointment events.',
    },
    {
      target: '[data-onboarding="citizen-inapp-notifications"]',
      title: 'In-app alerts',
      description: 'Unread items appear here first; use Mark read controls to keep this list tidy.',
      activateTarget: '[data-onboarding="citizen-notifications-tab"]',
    },
    {
      target: '[data-onboarding="citizen-settings-tab"]',
      title: 'Settings tab',
      description: 'Click Settings to control your email notification preferences and onboarding.',
    },
    {
      target: '[data-onboarding="citizen-queue-settings"]',
      title: 'Queue notification settings',
      description: 'This toggle controls whether you get queue turn-approaching emails.',
      activateTarget: '[data-onboarding="citizen-settings-tab"]',
    },
    {
      target: '[data-onboarding="citizen-appointment-settings"]',
      title: 'Appointment notification settings',
      description: 'Choose which appointment events should trigger email notifications.',
      activateTarget: '[data-onboarding="citizen-settings-tab"]',
    },
    {
      target: '[data-onboarding="citizen-settings"]',
      title: 'Replay onboarding',
      description: 'Restart onboarding any time from this button when you need a walkthrough again.',
      activateTarget: '[data-onboarding="citizen-settings-tab"]',
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
      description: 'Click this tab to create queues and manage queue-level operations.',
    },
    {
      target: '[data-onboarding="admin-queue-create-form"]',
      title: 'Queue form details',
      description: 'Set queue name, capacity, and average service time before publishing a shareable queue link.',
      activateTarget: '[data-onboarding="admin-queues-tab"]',
    },
    {
      target: '[data-onboarding="admin-appointments-tab"]',
      title: 'Appointment management',
      description: 'Click Appointments to configure profile schedules, slots, and assignments.',
    },
    {
      target: '[data-onboarding="admin-appointment-panel"]',
      title: 'Appointment details',
      description: 'Use this workspace to create appointment profiles and edit day-by-day availability.',
      activateTarget: '[data-onboarding="admin-appointments-tab"]',
    },
    {
      target: '[data-onboarding="admin-services-tab"]',
      title: 'Service management',
      description: 'Click Services to manage service catalog items and their operational setup.',
    },
    {
      target: '[data-onboarding="admin-service-create"]',
      title: 'Service creation',
      description: 'Create a service definition here with name, code, description, and estimated duration.',
      activateTarget: '[data-onboarding="admin-services-tab"]',
    },
    {
      target: '[data-onboarding="admin-service-definitions"]',
      title: 'Service definitions list',
      description: 'Review all defined services here and use Edit/Deactivate for lifecycle control.',
      activateTarget: '[data-onboarding="admin-services-tab"]',
    },
    {
      target: '[data-onboarding="admin-service-availability"]',
      title: 'Service availability',
      description: 'This section controls where each service is currently available across offices.',
      activateTarget: '[data-onboarding="admin-services-tab"]',
    },
    {
      target: '[data-onboarding="admin-service-operations"]',
      title: 'Operational flows',
      description: 'Generate queue and appointment operational flows from a selected service here.',
      activateTarget: '[data-onboarding="admin-services-tab"]',
    },
    {
      target: '[data-onboarding="admin-offices-tab"]',
      title: 'Office management',
      description: 'Click Offices to manage active locations and office availability.',
    },
    {
      target: '[data-onboarding="admin-office-filters"]',
      title: 'Office filters',
      description: 'Use search and status filters to quickly narrow office records.',
      activateTarget: '[data-onboarding="admin-offices-tab"]',
    },
    {
      target: '[data-onboarding="admin-office-create"]',
      title: 'Create office section',
      description: 'Create or update office information here, including address and opening hours.',
      activateTarget: '[data-onboarding="admin-offices-tab"]',
    },
    {
      target: '[data-onboarding="admin-office-list"]',
      title: 'Offices list',
      description: 'This list shows office cards with status, details, and action buttons.',
      activateTarget: '[data-onboarding="admin-offices-tab"]',
    },
    {
      target: '[data-onboarding="admin-staff-tab"]',
      title: 'Staff management',
      description: 'Click Staff to invite new operators and manage account access.',
    },
    {
      target: '[data-onboarding="admin-staff-panel"]',
      title: 'Staff details',
      description: 'This area covers invites, activation status, and staff profile maintenance.',
      activateTarget: '[data-onboarding="admin-staff-tab"]',
    },
    {
      target: '[data-onboarding="admin-staff-create"]',
      title: 'Staff creation section',
      description: 'Fill this form to invite a new staff member into your organisation workspace.',
      activateTarget: '[data-onboarding="admin-staff-tab"]',
    },
    {
      target: '[data-onboarding="admin-staff-list"]',
      title: 'Existing staff list',
      description: 'Review existing staff accounts, status, profile metadata, and account actions here.',
      activateTarget: '[data-onboarding="admin-staff-tab"]',
    },
    {
      target: '[data-onboarding="admin-staff-toggle-button"]',
      title: 'Deactivate or reactivate button',
      description: 'This button toggles account access: deactivate active users or reactivate inactive users.',
      activateTarget: '[data-onboarding="admin-staff-tab"]',
    },
    {
      target: '[data-onboarding="admin-staff-edit-button"]',
      title: 'Edit details button',
      description: 'Use Edit Details to open profile maintenance for office assignment and shift settings.',
      activateTarget: '[data-onboarding="admin-staff-tab"]',
    },
    {
      target: '[data-onboarding="admin-reports-tab"]',
      title: 'Inline reports',
      description: 'Click Reports to generate operational metrics directly inside this dashboard.',
    },
    {
      target: '[data-onboarding="admin-report-performance"]',
      title: 'Performance report fields',
      description: 'Use date, service, and office filters here to analyze wait times and served totals.',
      activateTarget: '[data-onboarding="admin-reports-tab"]',
    },
    {
      target: '[data-onboarding="admin-report-daily"]',
      title: 'Daily summary fields',
      description: 'Pick a date and generate daily served, skipped, and no-show trends.',
      activateTarget: '[data-onboarding="admin-reports-tab"]',
    },
    {
      target: '[data-onboarding="admin-settings-tab"]',
      title: 'Profile and settings',
      description: 'Restart onboarding from settings whenever you need a walkthrough again.',
    },
    {
      target: '[data-onboarding="admin-settings-panel"]',
      title: 'Settings details',
      description: 'Use this settings panel to replay onboarding and guide teammates through the dashboard.',
      activateTarget: '[data-onboarding="admin-settings-tab"]',
    },
  ],
}
