/**
 * AdminDashboardPage — Org admin queue management.
 *
 * Route: /admin/:tenantId  (wrapped by ProtectedRoute with OrgAdmin/SystemAdmin roles)
 *
 * Features:
 *  - Lists all queues owned by this organisation (loaded on mount)
 *  - Create queue form (name, max capacity, avg service time)
 *  - After create: shows the shareable link with a copy button
 *  - Per-queue: copy shareable link button
 */
import { useMemo, useState, useEffect, type CSSProperties } from 'react'
import { useNavigate, useParams } from 'react-router-dom'
import {
  assignStaffToQueue,
  createQueue,
  deleteQueue,
  getDailyQueueSummaryReport,
  getOrgQueues,
  getQueuePerformanceReport,
  listQueueStaffAssignments,
  unassignStaffFromQueue,
  type DailyQueueMetricTrend,
  type DailyQueueSummaryReportResult,
  type OrgQueueSummary,
  type QueuePerformanceReportResult,
  type QueueStaffAssignment,
} from '../../api/queues'
import {
  inviteStaffUser,
  listStaffUsers,
  reactivateStaffUser,
  type InviteStaffUserResult,
  type StaffUserSummary,
} from '../../api/auth'
import {
  listStaff,
  updateStaff,
  deactivateStaff,
  type StaffDto,
} from '../../api/staff'
import { listOffices, type OfficeDto } from '../../api/offices'
import {
  assignStaffToAppointmentProfile,
  getAppointmentSchedule,
  listAppointmentProfileStaffAssignments,
  configureAppointmentSchedule,
  listAppointmentProfiles,
  createAppointmentProfile,
  unassignStaffFromAppointmentProfile,
  type AppointmentStaffAssignment,
  type AppointmentProfileSummary,
  type AppointmentDayRule,
} from '../../api/appointments'
import { listServices } from '../../api/services'
import { OnboardingTour } from '../../components/OnboardingTour'
import { ROLE_TOUR_CONTENT } from '../../onboarding/roleTours'
import { useOnboardingTour } from '../../onboarding/useOnboardingTour'
import { OfficeManagementPage } from '../Offices'
import { ServiceManagementPage } from '../Services'
import type { ApiError } from '../../types/api'
import { clearToken, getTokenPayload } from '../../utils/authToken'
import logoImg from '../../assets/nextTurn-logo.png'
import styles from './AdminDashboardPage.module.css'

interface CreateForm {
  name: string
  maxCapacity: string
  averageServiceTimeSeconds: string
}

interface CreateStaffForm {
  name: string
  email: string
  phone: string
}

interface StaffProfileForm {
  name: string
  phone: string
  shiftStart: string
  shiftEnd: string
  officeId: string
}

type AdminTab = 'home' | 'offices' | 'services' | 'queues' | 'appointments' | 'staff' | 'reports' | 'profile' | 'settings'

function roleBadgeLabel(role: string): { label: string; className: string } {
  switch (role) {
    case 'Staff':
      return { label: 'Staff', className: styles.roleStaff }
    case 'OrgAdmin':
      return { label: 'Org Admin', className: styles.roleOrgAdmin }
    case 'SystemAdmin':
      return { label: 'System Admin', className: styles.roleSystemAdmin }
    default:
      return { label: 'User', className: styles.roleUser }
  }
}

const sidebarTabOrder: AdminTab[] = [
  'home',
  'queues',
  'appointments',
  'services',
  'offices',
  'staff',
  'reports',
  'profile',
  'settings',
]

const defaultForm: CreateForm = {
  name: '',
  maxCapacity: '50',
  averageServiceTimeSeconds: '300',
}

const defaultStaffForm: CreateStaffForm = {
  name: '',
  email: '',
  phone: '',
}

const defaultStaffProfileForm: StaffProfileForm = {
  name: '',
  phone: '',
  shiftStart: '',
  shiftEnd: '',
  officeId: '',
}

const dayLabels = ['Sunday', 'Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday', 'Saturday']

function toInputTime(time: string): string {
  return time.slice(0, 5)
}

function toMinutes(time: string): number {
  const [h, m] = time.split(':').map(Number)
  if (!Number.isFinite(h) || !Number.isFinite(m)) return 0
  return (h * 60) + m
}

function slotsForRule(rule: AppointmentDayRule): number {
  if (!rule.isEnabled) return 0
  const windowMinutes = toMinutes(rule.endTime) - toMinutes(rule.startTime)
  if (windowMinutes <= 0 || rule.slotDurationMinutes <= 0) return 0
  return Math.floor(windowMinutes / rule.slotDurationMinutes)
}

function normalizeShiftInput(value: string | null | undefined): string {
  if (!value) return ''
  return value.length >= 5 ? value.slice(0, 5) : value
}

function getFirstValidationMessage(apiError: ApiError): string | undefined {
  if (!apiError.errors) return undefined

  for (const value of Object.values(apiError.errors)) {
    if (Array.isArray(value) && value.length > 0) {
      const first = value[0]
      if (typeof first === 'string' && first.trim().length > 0) {
        return first
      }
    }
  }

  return undefined
}

function formatDate(value: Date): string {
  const year = value.getFullYear()
  const month = `${value.getMonth() + 1}`.padStart(2, '0')
  const day = `${value.getDate()}`.padStart(2, '0')
  return `${year}-${month}-${day}`
}

function defaultStartDate(): string {
  const date = new Date()
  date.setDate(date.getDate() - 7)
  return formatDate(date)
}

function defaultEndDate(): string {
  return formatDate(new Date())
}

function trendClass(value: number): 'up' | 'down' | 'neutral' {
  if (value > 0) return 'up'
  if (value < 0) return 'down'
  return 'neutral'
}

function renderTrend(trend: DailyQueueMetricTrend): string {
  const day = trend.deltaFromPreviousDay
  const week = trend.deltaFromPreviousWeek
  const dayLabel = day > 0 ? `+${day}` : `${day}`
  const weekLabel = week > 0 ? `+${week}` : `${week}`
  return `D ${dayLabel} | W ${weekLabel}`
}

export function AdminDashboardPage() {
  const navigate = useNavigate()
  const { tenantId } = useParams<{ tenantId: string }>()
  const payload = getTokenPayload()
  const onboarding = useOnboardingTour(`admin:${payload?.sub ?? 'anonymous'}:${tenantId ?? 'global'}`)

  const [queues, setQueues] = useState<OrgQueueSummary[]>([])
  const [loadError, setLoadError] = useState<string | null>(null)
  const [form, setForm] = useState<CreateForm>(defaultForm)
  const [formErrors, setFormErrors] = useState<Partial<CreateForm>>({})
  const [creating, setCreating] = useState(false)
  const [createError, setCreateError] = useState<string | null>(null)
  const [newLink, setNewLink] = useState<string | null>(null)
  const [copiedId, setCopiedId] = useState<string | null>(null)

  const [appointmentProfiles, setAppointmentProfiles] = useState<AppointmentProfileSummary[]>([])
  const [newAppointmentProfileName, setNewAppointmentProfileName] = useState('')
  const [profileError, setProfileError] = useState<string | null>(null)
  const [profileLoading, setProfileLoading] = useState(true)
  const [profileCreating, setProfileCreating] = useState(false)
  const [scheduleLoading, setScheduleLoading] = useState(false)
  const [scheduleSaving, setScheduleSaving] = useState(false)
  const [scheduleError, setScheduleError] = useState<string | null>(null)
  const [scheduleSuccess, setScheduleSuccess] = useState<string | null>(null)
  const [copiedAppointmentLink, setCopiedAppointmentLink] = useState(false)
  const [activeTab, setActiveTab] = useState<AdminTab>('home')
  const [appointmentEditorOpen, setAppointmentEditorOpen] = useState(false)
  const [editingAppointmentProfile, setEditingAppointmentProfile] = useState<AppointmentProfileSummary | null>(null)
  const [editorRules, setEditorRules] = useState<AppointmentDayRule[]>([])
  const [editorShareLink, setEditorShareLink] = useState<string | null>(null)
  const [appointmentStaffAssignments, setAppointmentStaffAssignments] = useState<Record<string, AppointmentStaffAssignment[]>>({})
  const [editorAssignedStaffUserIds, setEditorAssignedStaffUserIds] = useState<string[]>([])

  const [staffForm, setStaffForm] = useState<CreateStaffForm>(defaultStaffForm)
  const [staffCreating, setStaffCreating] = useState(false)
  const [staffError, setStaffError] = useState<string | null>(null)
  const [staffSuccess, setStaffSuccess] = useState<string | null>(null)
  const [latestInvite, setLatestInvite] = useState<InviteStaffUserResult | null>(null)
  const [staffAccounts, setStaffAccounts] = useState<StaffUserSummary[]>([])
  const [staffAccountsLoading, setStaffAccountsLoading] = useState(false)
  const [staffActionUserId, setStaffActionUserId] = useState<string | null>(null)
  const [staffProfiles, setStaffProfiles] = useState<StaffDto[]>([])
  const [staffProfilesLoading, setStaffProfilesLoading] = useState(false)
  const [officeOptions, setOfficeOptions] = useState<OfficeDto[]>([])
  const [selectedStaffProfileId, setSelectedStaffProfileId] = useState<string | null>(null)
  const [staffProfileForm, setStaffProfileForm] = useState<StaffProfileForm>(defaultStaffProfileForm)
  const [staffProfileSaving, setStaffProfileSaving] = useState(false)
  const [queueAssignments, setQueueAssignments] = useState<Record<string, QueueStaffAssignment[]>>({})
  const [queueAssignmentSelection, setQueueAssignmentSelection] = useState<Record<string, string>>({})
  const [queueAssignmentLoading, setQueueAssignmentLoading] = useState(false)
  const [queueAssignmentBusyKey, setQueueAssignmentBusyKey] = useState<string | null>(null)
  const [queueDeleteBusyId, setQueueDeleteBusyId] = useState<string | null>(null)
  const [queueAssignmentError, setQueueAssignmentError] = useState<string | null>(null)
  const [queueAssignmentSuccess, setQueueAssignmentSuccess] = useState<string | null>(null)
  const [homeOfficeCount, setHomeOfficeCount] = useState(0)
  const [homeServiceCount, setHomeServiceCount] = useState(0)
  const [reportOfficeOptions, setReportOfficeOptions] = useState<OfficeDto[]>([])
  const [reportServiceOptions, setReportServiceOptions] = useState<Array<{ serviceId: string; name: string }>>([])
  const [reportStartDate, setReportStartDate] = useState(defaultStartDate)
  const [reportEndDate, setReportEndDate] = useState(defaultEndDate)
  const [reportOfficeId, setReportOfficeId] = useState('')
  const [reportServiceId, setReportServiceId] = useState('')
  const [queueReportLoading, setQueueReportLoading] = useState(false)
  const [queueReportError, setQueueReportError] = useState<string | null>(null)
  const [queueReport, setQueueReport] = useState<QueuePerformanceReportResult | null>(null)
  const [dailyReportDate, setDailyReportDate] = useState(defaultEndDate)
  const [dailyReportLoading, setDailyReportLoading] = useState(false)
  const [dailyReportError, setDailyReportError] = useState<string | null>(null)
  const [dailyReport, setDailyReport] = useState<DailyQueueSummaryReportResult | null>(null)

  const appointmentSummary = useMemo(() => {
    const enabledDays = editorRules.filter(r => r.isEnabled).length
    const totalWeeklySlots = editorRules.reduce((sum, rule) => sum + slotsForRule(rule), 0)
    return { enabledDays, totalWeeklySlots }
  }, [editorRules])

  const staffProfileByEmail = useMemo(
    () => new Map(staffProfiles.map(profile => [profile.email.toLowerCase(), profile])),
    [staffProfiles],
  )

  const officeNameById = useMemo(
    () => new Map(officeOptions.map(office => [office.officeId, office.name])),
    [officeOptions],
  )

  async function loadAppointmentAssignments(
    currentTenantId: string,
    profiles: AppointmentProfileSummary[],
  ): Promise<Record<string, AppointmentStaffAssignment[]>> {
    if (profiles.length === 0) return {}

    const pairs = await Promise.all(
      profiles.map(async profile => {
        const assigned = await listAppointmentProfileStaffAssignments(
          currentTenantId,
          profile.appointmentProfileId,
        )

        return [profile.appointmentProfileId, assigned] as const
      }),
    )

    return Object.fromEntries(pairs)
  }

  if (!payload) {
    clearToken()
    navigate('/', { replace: true })
    return null
  }

  const badge = roleBadgeLabel(payload.role)

  // eslint-disable-next-line react-hooks/rules-of-hooks
  useEffect(() => {
    if (!tenantId) return

    getOrgQueues(tenantId)
      .then(setQueues)
      .catch(() => setLoadError('Could not load queues. Please refresh the page.'))

    listAppointmentProfiles(tenantId)
      .then(async profiles => {
        setAppointmentProfiles(profiles)
        const assignments = await loadAppointmentAssignments(tenantId, profiles)
        setAppointmentStaffAssignments(assignments)
      })
      .catch((err: ApiError) => {
        if (err.status === 403) {
          setProfileError('You do not have permission to load appointment profiles.')
          return
        }

        if (err.status === 401) {
          setProfileError('Your session is not authorized. Please sign in again.')
          return
        }

        setProfileError(err.detail ?? 'Could not load appointment profiles.')
      })
      .finally(() => setProfileLoading(false))
  }, [tenantId])

  useEffect(() => {
    if (!tenantId) return

    listOffices(tenantId, { isActive: true, pageNumber: 1, pageSize: 200 })
      .then(result => {
        setHomeOfficeCount(result.totalCount)
        setReportOfficeOptions(result.items)
      })
      .catch(() => {
        setHomeOfficeCount(0)
        setReportOfficeOptions([])
      })

    listStaffUsers(tenantId)
      .then(result => setStaffAccounts(result))
      .catch(() => setStaffAccounts([]))

    listServices(tenantId, { activeOnly: true, pageNumber: 1, pageSize: 200 })
      .then(result => {
        setHomeServiceCount(result.totalCount)
        setReportServiceOptions(result.items.map(item => ({ serviceId: item.serviceId, name: item.name })))
      })
      .catch(() => {
        setHomeServiceCount(0)
        setReportServiceOptions([])
      })
  }, [tenantId])

  useEffect(() => {
    function onAdminShortcut(event: KeyboardEvent) {
      if (event.defaultPrevented || isTypingTarget(event.target)) return

      const mapped = keyToAdminTab(event.key.toLowerCase())
      if (!mapped) return

      event.preventDefault()
      setActiveTab(mapped)
    }

    window.addEventListener('keydown', onAdminShortcut)
    return () => window.removeEventListener('keydown', onAdminShortcut)
  }, [])

  function handleLogout() {
    clearToken()
    navigate('/', { replace: true })
  }

  function validate(): boolean {
    const errors: Partial<CreateForm> = {}
    if (!form.name.trim()) errors.name = 'Queue name is required.'
    if (!form.maxCapacity || parseInt(form.maxCapacity, 10) < 1) {
      errors.maxCapacity = 'Capacity must be at least 1.'
    }
    if (!form.averageServiceTimeSeconds || parseInt(form.averageServiceTimeSeconds, 10) < 1) {
      errors.averageServiceTimeSeconds = 'Service time must be at least 1 second.'
    }
    setFormErrors(errors)
    return Object.keys(errors).length === 0
  }

  async function handleCreate(e: React.FormEvent) {
    e.preventDefault()
    if (!tenantId || !validate()) return

    setCreating(true)
    setCreateError(null)
    setNewLink(null)

    try {
      const result = await createQueue(tenantId, {
        name: form.name.trim(),
        maxCapacity: parseInt(form.maxCapacity, 10),
        averageServiceTimeSeconds: parseInt(form.averageServiceTimeSeconds, 10),
      })

      const newQueue: OrgQueueSummary = {
        queueId: result.queueId,
        name: form.name.trim(),
        maxCapacity: parseInt(form.maxCapacity, 10),
        averageServiceTimeSeconds: parseInt(form.averageServiceTimeSeconds, 10),
        status: 'Active',
        shareableLink: result.shareableLink,
      }

      setQueues(prev => [newQueue, ...prev])
      setNewLink(result.shareableLink)
      setForm(defaultForm)
      setFormErrors({})
    } catch (err) {
      const apiErr = err as ApiError
      if (apiErr.status === 422) {
        const firstError = apiErr.errors ? Object.values(apiErr.errors)[0]?.[0] : undefined
        setCreateError(firstError ?? 'Please check your input and try again.')
      } else {
        setCreateError(apiErr.detail ?? 'Failed to create queue. Please try again.')
      }
    } finally {
      setCreating(false)
    }
  }

  async function copyLink(queue: OrgQueueSummary) {
    const fullUrl = `${window.location.origin}${queue.shareableLink}`
    await navigator.clipboard.writeText(fullUrl)
    setCopiedId(queue.queueId)
    setTimeout(() => setCopiedId(null), 2000)
  }

  async function copyAppointmentLink() {
    if (!editorShareLink) return
    await navigator.clipboard.writeText(`${window.location.origin}${editorShareLink}`)
    setCopiedAppointmentLink(true)
    setTimeout(() => setCopiedAppointmentLink(false), 2000)
  }

  function updateRule(dayOfWeek: number, changes: Partial<AppointmentDayRule>) {
    setEditorRules(prev =>
      prev.map(rule => (rule.dayOfWeek === dayOfWeek ? { ...rule, ...changes } : rule)),
    )
    setScheduleSuccess(null)
    setScheduleError(null)
  }

  function toggleEditorStaffAssignment(staffUserId: string) {
    setEditorAssignedStaffUserIds(prev =>
      prev.includes(staffUserId)
        ? prev.filter(id => id !== staffUserId)
        : [...prev, staffUserId],
    )
  }

  async function openAppointmentEditor(profile: AppointmentProfileSummary) {
    if (!tenantId) return

    setEditingAppointmentProfile(profile)
    setAppointmentEditorOpen(true)
    setScheduleLoading(true)
    setScheduleError(null)
    setScheduleSuccess(null)

    if (staffAccounts.length === 0) {
      await loadStaffAccounts(tenantId)
    }

    try {
      const config = await getAppointmentSchedule(tenantId, profile.appointmentProfileId)
      const assignedStaff = await listAppointmentProfileStaffAssignments(tenantId, profile.appointmentProfileId)
      setEditorRules(config.dayRules)
      setEditorShareLink(config.shareableLink)
      setEditorAssignedStaffUserIds(assignedStaff.map(a => a.staffUserId))
      setAppointmentStaffAssignments(prev => ({
        ...prev,
        [profile.appointmentProfileId]: assignedStaff,
      }))
    } catch {
      setScheduleError('Could not load appointment schedule.')
      setEditorRules([])
      setEditorShareLink(null)
      setEditorAssignedStaffUserIds([])
    } finally {
      setScheduleLoading(false)
    }
  }

  function closeAppointmentEditor() {
    setAppointmentEditorOpen(false)
    setEditingAppointmentProfile(null)
    setEditorRules([])
    setEditorShareLink(null)
    setEditorAssignedStaffUserIds([])
    setCopiedAppointmentLink(false)
  }

  async function saveSchedule() {
    if (!tenantId || !editingAppointmentProfile || editorRules.length !== 7) return

    setScheduleSaving(true)
    setScheduleError(null)
    setScheduleSuccess(null)

    try {
      const result = await configureAppointmentSchedule(
        tenantId,
        editingAppointmentProfile.appointmentProfileId,
        editorRules,
      )

      const currentAssigned = appointmentStaffAssignments[editingAppointmentProfile.appointmentProfileId] ?? []
      const currentIds = new Set(currentAssigned.map(a => a.staffUserId))
      const nextIds = new Set(editorAssignedStaffUserIds)

      const toAssign = editorAssignedStaffUserIds.filter(id => !currentIds.has(id))
      const toUnassign = currentAssigned
        .map(a => a.staffUserId)
        .filter(id => !nextIds.has(id))

      for (const staffUserId of toAssign) {
        await assignStaffToAppointmentProfile(
          tenantId,
          editingAppointmentProfile.appointmentProfileId,
          staffUserId,
        )
      }

      for (const staffUserId of toUnassign) {
        await unassignStaffFromAppointmentProfile(
          tenantId,
          editingAppointmentProfile.appointmentProfileId,
          staffUserId,
        )
      }

      const refreshedAssigned = await listAppointmentProfileStaffAssignments(
        tenantId,
        editingAppointmentProfile.appointmentProfileId,
      )

      setAppointmentStaffAssignments(prev => ({
        ...prev,
        [editingAppointmentProfile.appointmentProfileId]: refreshedAssigned,
      }))

      setEditorShareLink(result.shareableLink)
      setScheduleSuccess('Appointment settings saved.')
    } catch (err) {
      const apiErr = err as ApiError
      setScheduleError(apiErr.detail ?? 'Could not save appointment settings.')
    } finally {
      setScheduleSaving(false)
    }
  }

  async function handleCreateAppointmentProfile(e: React.FormEvent) {
    e.preventDefault()
    if (!tenantId || !newAppointmentProfileName.trim()) return

    setProfileCreating(true)
    setProfileError(null)

    try {
      const created = await createAppointmentProfile(tenantId, newAppointmentProfileName.trim())
      setAppointmentProfiles(prev => [created, ...prev])
      setAppointmentStaffAssignments(prev => ({
        ...prev,
        [created.appointmentProfileId]: [],
      }))
      setNewAppointmentProfileName('')
      setScheduleSuccess('Appointment profile created.')
    } catch (err) {
      const apiErr = err as ApiError
      setProfileError(apiErr.detail ?? 'Could not create appointment profile.')
    } finally {
      setProfileCreating(false)
    }
  }

  async function handleCreateStaff(e: React.FormEvent) {
    e.preventDefault()
    if (!tenantId) return

    setStaffCreating(true)
    setStaffError(null)
    setStaffSuccess(null)

    try {
      const invite = await inviteStaffUser(tenantId, {
        name: staffForm.name.trim(),
        email: staffForm.email.trim(),
        phone: staffForm.phone.trim() || undefined,
      })

      setStaffForm(defaultStaffForm)
      setLatestInvite(invite)
      setStaffSuccess('Staff invite created successfully.')
      await loadStaffAccounts(tenantId)
    } catch (err) {
      const apiErr = err as ApiError
      if (apiErr.status === 422) {
        const firstError = getFirstValidationMessage(apiErr)
        setStaffError(firstError ?? 'Please check the staff details and try again.')
      } else {
        setStaffError(apiErr.detail ?? 'Could not create staff account.')
      }
    } finally {
      setStaffCreating(false)
    }
  }

  async function loadStaffAccounts(currentTenantId: string) {
    setStaffAccountsLoading(true)
    try {
      const users = await listStaffUsers(currentTenantId)
      setStaffAccounts(users)
    } catch (err) {
      const apiErr = err as ApiError
      setStaffError(apiErr.detail ?? 'Could not load staff accounts.')
    } finally {
      setStaffAccountsLoading(false)
    }
  }

  async function loadStaffManagementData(currentTenantId: string) {
    setStaffProfilesLoading(true)

    try {
      const [staffResult, officeResult] = await Promise.all([
        listStaff(currentTenantId, 1, 100),
        listOffices(currentTenantId, { isActive: true, pageNumber: 1, pageSize: 100 }),
      ])

      setStaffProfiles(staffResult.items)
      setOfficeOptions(officeResult.items)

      setSelectedStaffProfileId(prev => {
        if (prev && staffResult.items.some(x => x.staffUserId === prev)) return prev
        return null
      })
    } catch (err) {
      const apiErr = err as ApiError
      setStaffError(apiErr.detail ?? 'Could not load staff profile management data.')
    } finally {
      setStaffProfilesLoading(false)
    }
  }

  function beginEditStaffProfile(profile: StaffDto) {
    setSelectedStaffProfileId(profile.staffUserId)
    setStaffProfileForm({
      name: profile.name,
      phone: profile.phone ?? '',
      shiftStart: normalizeShiftInput(profile.shiftStart),
      shiftEnd: normalizeShiftInput(profile.shiftEnd),
      officeId: profile.officeIds[0] ?? '',
    })
    setStaffError(null)
    setStaffSuccess(null)
  }

  function resetStaffProfileEditor() {
    setSelectedStaffProfileId(null)
    setStaffProfileForm(defaultStaffProfileForm)
  }

  async function saveStaffProfile() {
    if (!tenantId || !selectedStaffProfileId) return

    const shiftStart = staffProfileForm.shiftStart.trim()
    const shiftEnd = staffProfileForm.shiftEnd.trim()
    const hasShiftStart = shiftStart.length > 0
    const hasShiftEnd = shiftEnd.length > 0

    if (hasShiftStart !== hasShiftEnd) {
      setStaffError('Shift start and shift end must both be provided.')
      return
    }

    if (hasShiftStart && hasShiftEnd && toMinutes(shiftStart) >= toMinutes(shiftEnd)) {
      setStaffError('Shift end must be after shift start.')
      return
    }

    if (!staffProfileForm.officeId) {
      setStaffError('Please assign exactly one office.')
      return
    }

    setStaffProfileSaving(true)
    setStaffError(null)
    setStaffSuccess(null)

    try {
      await updateStaff(tenantId, selectedStaffProfileId, {
        name: staffProfileForm.name.trim(),
        phone: staffProfileForm.phone.trim() || null,
        officeIds: [staffProfileForm.officeId],
        shiftStart: shiftStart || null,
        shiftEnd: shiftEnd || null,
      })

      setStaffSuccess('Staff profile updated successfully.')
      await Promise.all([
        loadStaffAccounts(tenantId),
        loadStaffManagementData(tenantId),
      ])
      resetStaffProfileEditor()
    } catch (err) {
      const apiErr = err as ApiError
      if (apiErr.status === 422) {
        const firstError = getFirstValidationMessage(apiErr)
        setStaffError(firstError ?? apiErr.detail ?? 'Could not update staff profile.')
      } else {
        setStaffError(apiErr.detail ?? 'Could not update staff profile.')
      }
    } finally {
      setStaffProfileSaving(false)
    }
  }

  async function loadQueueAssignments(currentTenantId: string, currentQueues: OrgQueueSummary[]) {
    if (currentQueues.length === 0) {
      setQueueAssignments({})
      return
    }

    setQueueAssignmentLoading(true)
    setQueueAssignmentError(null)

    try {
      const pairs = await Promise.all(
        currentQueues.map(async queue => {
          const assigned = await listQueueStaffAssignments(queue.queueId, currentTenantId)
          return [queue.queueId, assigned] as const
        })
      )

      setQueueAssignments(Object.fromEntries(pairs))
    } catch (err) {
      const apiErr = err as ApiError
      setQueueAssignmentError(apiErr.detail ?? 'Could not load queue staff assignments.')
    } finally {
      setQueueAssignmentLoading(false)
    }
  }

  useEffect(() => {
    if (activeTab !== 'staff' || !tenantId) return

    void Promise.all([
      loadStaffAccounts(tenantId),
      loadStaffManagementData(tenantId),
    ])
  }, [activeTab, tenantId])

  useEffect(() => {
    if (activeTab !== 'queues' || !tenantId) return

    if (staffAccounts.length === 0) {
      void loadStaffAccounts(tenantId)
    }

    void loadQueueAssignments(tenantId, queues)
  }, [activeTab, tenantId, queues])

  async function handleStaffStatusToggle(user: StaffUserSummary) {
    if (!tenantId) return

    setStaffError(null)
    setStaffSuccess(null)
    setStaffActionUserId(user.userId)

    try {
      if (user.isActive) {
        await deactivateStaff(tenantId, user.userId)
        setStaffSuccess('Staff account deactivated.')
      } else {
        await reactivateStaffUser(tenantId, user.userId)
        setStaffSuccess('Staff account reactivated.')
      }

      await Promise.all([
        loadStaffAccounts(tenantId),
        loadStaffManagementData(tenantId),
      ])
    } catch (err) {
      const apiErr = err as ApiError
      setStaffError(apiErr.detail ?? 'Could not update staff account status.')
    } finally {
      setStaffActionUserId(null)
    }
  }

  async function handleAssignStaff(queueId: string) {
    if (!tenantId) return

    const selectedStaffUserId = queueAssignmentSelection[queueId]
    if (!selectedStaffUserId) return

    setQueueAssignmentBusyKey(`${queueId}:${selectedStaffUserId}:assign`)
    setQueueAssignmentError(null)
    setQueueAssignmentSuccess(null)

    try {
      await assignStaffToQueue(queueId, selectedStaffUserId, tenantId)
      setQueueAssignmentSelection(prev => ({ ...prev, [queueId]: '' }))
      setQueueAssignmentSuccess('Staff assignment updated.')
      await loadQueueAssignments(tenantId, queues)
    } catch (err) {
      const apiErr = err as ApiError
      setQueueAssignmentError(apiErr.detail ?? 'Could not assign staff to queue.')
    } finally {
      setQueueAssignmentBusyKey(null)
    }
  }

  async function handleUnassignStaff(queueId: string, staffUserId: string) {
    if (!tenantId) return

    setQueueAssignmentBusyKey(`${queueId}:${staffUserId}:unassign`)
    setQueueAssignmentError(null)
    setQueueAssignmentSuccess(null)

    try {
      await unassignStaffFromQueue(queueId, staffUserId, tenantId)
      setQueueAssignmentSuccess('Staff assignment removed.')
      await loadQueueAssignments(tenantId, queues)
    } catch (err) {
      const apiErr = err as ApiError
      setQueueAssignmentError(apiErr.detail ?? 'Could not remove queue assignment.')
    } finally {
      setQueueAssignmentBusyKey(null)
    }
  }

  async function handleDeleteQueue(queue: OrgQueueSummary) {
    if (!tenantId) return

    const confirmed = window.confirm(
      `Delete queue "${queue.name}"? This cannot be undone and may remove active entries.`,
    )

    if (!confirmed) return

    setQueueDeleteBusyId(queue.queueId)
    setQueueAssignmentError(null)
    setQueueAssignmentSuccess(null)

    try {
      await deleteQueue(queue.queueId, tenantId)
      setQueues(prev => prev.filter(item => item.queueId !== queue.queueId))
      setQueueAssignments(prev => {
        const next = { ...prev }
        delete next[queue.queueId]
        return next
      })
      setQueueAssignmentSelection(prev => {
        const next = { ...prev }
        delete next[queue.queueId]
        return next
      })
      setQueueAssignmentSuccess(`Queue "${queue.name}" deleted.`)
    } catch (err) {
      const apiErr = err as ApiError
      setQueueAssignmentError(apiErr.detail ?? 'Could not delete queue.')
    } finally {
      setQueueDeleteBusyId(null)
    }
  }

  async function loadQueuePerformanceReportInline() {
    if (!tenantId) return

    setQueueReportLoading(true)
    setQueueReportError(null)

    try {
      const result = await getQueuePerformanceReport(tenantId, {
        startDate: reportStartDate,
        endDate: reportEndDate,
        officeId: reportOfficeId || undefined,
        serviceId: reportServiceId || undefined,
      })
      setQueueReport(result)
    } catch (err) {
      const apiErr = err as ApiError
      setQueueReportError(apiErr.detail ?? 'Could not load queue performance report.')
      setQueueReport(null)
    } finally {
      setQueueReportLoading(false)
    }
  }

  async function loadDailySummaryInline() {
    if (!tenantId) return

    setDailyReportLoading(true)
    setDailyReportError(null)

    try {
      const result = await getDailyQueueSummaryReport(tenantId, dailyReportDate)
      setDailyReport(result)
    } catch (err) {
      const apiErr = err as ApiError
      setDailyReportError(apiErr.detail ?? 'Could not load daily summary report.')
      setDailyReport(null)
    } finally {
      setDailyReportLoading(false)
    }
  }

  const activeSidebarIndex = Math.max(sidebarTabOrder.indexOf(activeTab), 0)
  const sidebarNavStyle = { '--active-index': activeSidebarIndex } as CSSProperties

  return (
    <div className={styles.page}>
      <header className={styles.navbar}>
        <div className={styles.navInner}>
          <div className={styles.navBrand}>
            <img src={logoImg} alt="NextTurn" className={styles.navLogo} />
          </div>

          <div className={styles.navUser}>
            <div className={styles.avatarCircle} aria-hidden="true">
              {payload.name.charAt(0).toUpperCase()}
            </div>
            <div className={styles.userMeta}>
              <span className={styles.userName}>{payload.name}</span>
              <span className={`${styles.roleBadge} ${badge.className}`}>{badge.label}</span>
            </div>
            <button className={styles.logoutBtn} onClick={handleLogout} type="button" aria-label="Sign out">
              <LogoutIcon />
              <span>Sign out</span>
            </button>
          </div>
        </div>
      </header>

      <main className={styles.main}>
        <div className={styles.workspace}>
          <aside className={styles.sidebar} aria-label="Admin navigation" data-onboarding="admin-sidebar">
            <div className={styles.sidebarHeader}>
              <h1 className={styles.sidebarTitle}>Operations Hub</h1>
              <p className={styles.sidebarSubtitle}>Manage high-impact admin tasks with less clutter.</p>
            </div>

            <nav className={styles.sidebarNav} style={sidebarNavStyle}>
              <button
                type="button"
                className={`${styles.sideNavBtn} ${activeTab === 'home' ? styles.sideNavBtnActive : ''}`}
                onClick={() => setActiveTab('home')}
                title="Quick overview of your operations"
              >
                Home
              </button>
              <button
                type="button"
                className={`${styles.sideNavBtn} ${activeTab === 'queues' ? styles.sideNavBtnActive : ''}`}
                onClick={() => setActiveTab('queues')}
                title="Create and control queues"
                data-onboarding="admin-queues-tab"
              >
                Queue Management
              </button>
              <button
                type="button"
                className={`${styles.sideNavBtn} ${activeTab === 'appointments' ? styles.sideNavBtnActive : ''}`}
                onClick={() => setActiveTab('appointments')}
                title="Configure appointment profiles and schedules"
                data-onboarding="admin-appointments-tab"
              >
                Appointment Management
              </button>
              <button
                type="button"
                className={`${styles.sideNavBtn} ${activeTab === 'services' ? styles.sideNavBtnActive : ''}`}
                onClick={() => setActiveTab('services')}
                title="Manage service catalog and availability"
              >
                Service Management
              </button>
              <button
                type="button"
                className={`${styles.sideNavBtn} ${activeTab === 'offices' ? styles.sideNavBtnActive : ''}`}
                onClick={() => setActiveTab('offices')}
                title="Manage office locations and status"
              >
                Office Management
              </button>
              <button
                type="button"
                className={`${styles.sideNavBtn} ${activeTab === 'staff' ? styles.sideNavBtnActive : ''}`}
                onClick={() => setActiveTab('staff')}
                title="Invite and maintain staff access"
                data-onboarding="admin-staff-tab"
              >
                Staff Management
              </button>
              <button
                type="button"
                className={`${styles.sideNavBtn} ${activeTab === 'reports' ? styles.sideNavBtnActive : ''}`}
                onClick={() => setActiveTab('reports')}
                title="Open queue and daily summary reports"
                data-onboarding="admin-reports-tab"
              >
                Reports
              </button>
              <button
                type="button"
                className={`${styles.sideNavBtn} ${activeTab === 'profile' ? styles.sideNavBtnActive : ''}`}
                onClick={() => setActiveTab('profile')}
                title="View account details"
              >
                Profile
              </button>
              <button
                type="button"
                className={`${styles.sideNavBtn} ${activeTab === 'settings' ? styles.sideNavBtnActive : ''}`}
                onClick={() => setActiveTab('settings')}
                title="Open preferences and onboarding settings"
                data-onboarding="admin-settings-tab"
              >
                Settings
              </button>
            </nav>
            <p className={styles.sidebarHint}>Shortcuts: 1-9 or H/Q/A/S/O/T/R/P/G</p>
          </aside>

          <div className={styles.contentArea}>
            <section className={styles.toolbar}>
              <div className={styles.toolbarHeader}>
                <h2 className={styles.pageTitle}>Operations Control Center</h2>
                <p className={styles.pageSubtitle}>Manage queues, appointments, people, and operations from one place.</p>
              </div>
            </section>

            <div key={activeTab} className={styles.tabPanel}>

            {activeTab === 'home' && (
              <section className={styles.section}>
                <h2 className={styles.sectionTitle}>Quick Overview</h2>
                <p className={styles.sectionHint}>A clean snapshot of your operations. Use the sidebar to dive deeper into each area.</p>

                <div className={styles.homeSummaryGrid}>
                  <article className={styles.summaryCard} title="Total queues configured for this organisation">
                    <span className={styles.summaryLabel}>Queues</span>
                    <strong className={styles.summaryValue}>{queues.length}</strong>
                  </article>
                  <article className={styles.summaryCard} title="Appointment profiles available to end users">
                    <span className={styles.summaryLabel}>Appointment Profiles</span>
                    <strong className={styles.summaryValue}>{appointmentProfiles.length}</strong>
                  </article>
                  <article className={styles.summaryCard} title="Active office locations currently configured">
                    <span className={styles.summaryLabel}>Active Offices</span>
                    <strong className={styles.summaryValue}>{homeOfficeCount}</strong>
                  </article>
                  <article className={styles.summaryCard} title="Active service offerings available for routing">
                    <span className={styles.summaryLabel}>Active Services</span>
                    <strong className={styles.summaryValue}>{homeServiceCount}</strong>
                  </article>
                  <article className={styles.summaryCard} title="Total staff users in your organisation">
                    <span className={styles.summaryLabel}>Staff Accounts</span>
                    <strong className={styles.summaryValue}>{staffAccounts.length}</strong>
                  </article>
                </div>

              </section>
            )}

            {activeTab === 'profile' && (
              <section className={styles.section}>
                <h2 className={styles.sectionTitle}>Profile</h2>
                <p className={styles.sectionHint}>Your admin account details for this workspace.</p>

                <div className={styles.homeSummaryGrid}>
                  <article className={styles.summaryCard}>
                    <span className={styles.summaryLabel}>Name</span>
                    <strong className={styles.summaryValue}>{payload?.name ?? 'Unknown'}</strong>
                  </article>
                  <article className={styles.summaryCard}>
                    <span className={styles.summaryLabel}>Email</span>
                    <strong className={styles.summaryValue}>{payload?.email ?? 'Unknown'}</strong>
                  </article>
                  <article className={styles.summaryCard}>
                    <span className={styles.summaryLabel}>Role</span>
                    <strong className={styles.summaryValue}>{payload?.role ?? 'Unknown'}</strong>
                  </article>
                </div>
              </section>
            )}

            {activeTab === 'settings' && (
              <section className={styles.section} data-onboarding="admin-settings">
                <h2 className={styles.sectionTitle}>Settings</h2>
                <p className={styles.sectionHint}>Update workspace preferences and replay guided onboarding.</p>

                <div className={styles.reportCard}>
                  <h3 className={styles.sectionSubTitle}>Onboarding</h3>
                  <p className={styles.sectionHint}>Replay the onboarding walkthrough whenever your team needs a refresher.</p>
                  <button
                    type="button"
                    className={styles.createBtn}
                    onClick={onboarding.restartTour}
                  >
                    Restart onboarding tour
                  </button>
                </div>
              </section>
            )}

            {activeTab === 'reports' && (
              <section className={styles.section}>
                <h2 className={styles.sectionTitle}>Reports</h2>
                <p className={styles.sectionHint}>Generate both report types directly here without leaving the dashboard.</p>

                <div className={styles.reportCard}>
                  <h3 className={styles.sectionSubTitle}>Queue Performance Report</h3>
                  <p className={styles.sectionHint}>Analyze served volumes, average wait times, and peak-hour demand.</p>

                  <div className={styles.formGrid}>
                    <div className={styles.formGroup}>
                      <label className={styles.label} htmlFor="report-start-date">Start date</label>
                      <input id="report-start-date" className={styles.input} type="date" value={reportStartDate} onChange={e => setReportStartDate(e.target.value)} />
                    </div>
                    <div className={styles.formGroup}>
                      <label className={styles.label} htmlFor="report-end-date">End date</label>
                      <input id="report-end-date" className={styles.input} type="date" value={reportEndDate} onChange={e => setReportEndDate(e.target.value)} />
                    </div>
                    <div className={styles.formGroup}>
                      <label className={styles.label} htmlFor="report-service">Service</label>
                      <select id="report-service" className={styles.input} value={reportServiceId} onChange={e => setReportServiceId(e.target.value)}>
                        <option value="">All services</option>
                        {reportServiceOptions.map(service => (
                          <option key={service.serviceId} value={service.serviceId}>{service.name}</option>
                        ))}
                      </select>
                    </div>
                    <div className={styles.formGroup}>
                      <label className={styles.label} htmlFor="report-office">Office</label>
                      <select id="report-office" className={styles.input} value={reportOfficeId} onChange={e => setReportOfficeId(e.target.value)}>
                        <option value="">All offices</option>
                        {reportOfficeOptions.map(office => (
                          <option key={office.officeId} value={office.officeId}>{office.name}</option>
                        ))}
                      </select>
                    </div>
                  </div>

                  <button type="button" className={styles.createBtn} onClick={loadQueuePerformanceReportInline} disabled={queueReportLoading}>
                    {queueReportLoading ? 'Generating...' : 'Generate Queue Performance Report'}
                  </button>

                  {queueReportError && <div className={styles.errorBanner} role="alert">{queueReportError}</div>}

                  {queueReport && (
                    <div className={styles.reportMetrics}>
                      <article className={styles.summaryCard}>
                        <span className={styles.summaryLabel}>Total Served</span>
                        <strong className={styles.summaryValue}>{queueReport.totalServed}</strong>
                      </article>
                      <article className={styles.summaryCard}>
                        <span className={styles.summaryLabel}>Average Wait</span>
                        <strong className={styles.summaryValue}>{queueReport.averageWaitMinutes.toFixed(2)} min</strong>
                      </article>
                      <article className={styles.summaryCard}>
                        <span className={styles.summaryLabel}>Peak Hours</span>
                        <strong className={styles.summaryValue}>{queueReport.peakHours.length}</strong>
                      </article>
                    </div>
                  )}
                </div>

                <div className={styles.reportCard}>
                  <h3 className={styles.sectionSubTitle}>Daily Summary Report</h3>
                  <p className={styles.sectionHint}>Track served, skipped, and no-show counts with trend context.</p>

                  <div className={styles.formGrid}>
                    <div className={styles.formGroup}>
                      <label className={styles.label} htmlFor="daily-report-date">Summary date</label>
                      <input id="daily-report-date" className={styles.input} type="date" value={dailyReportDate} onChange={e => setDailyReportDate(e.target.value)} />
                    </div>
                  </div>

                  <button type="button" className={styles.createBtn} onClick={loadDailySummaryInline} disabled={dailyReportLoading}>
                    {dailyReportLoading ? 'Generating...' : 'Generate Daily Summary'}
                  </button>

                  {dailyReportError && <div className={styles.errorBanner} role="alert">{dailyReportError}</div>}

                  {dailyReport && (
                    <>
                      <div className={styles.reportMetrics}>
                        <article className={styles.summaryCard}>
                          <span className={styles.summaryLabel}>Served</span>
                          <strong className={styles.summaryValue}>{dailyReport.totalServed}</strong>
                          <span className={styles.sectionHint}>{renderTrend(dailyReport.totalServedTrend)}</span>
                        </article>
                        <article className={styles.summaryCard}>
                          <span className={styles.summaryLabel}>Skipped</span>
                          <strong className={styles.summaryValue}>{dailyReport.totalSkipped}</strong>
                          <span className={styles.sectionHint}>{renderTrend(dailyReport.totalSkippedTrend)}</span>
                        </article>
                        <article className={styles.summaryCard}>
                          <span className={styles.summaryLabel}>No-shows</span>
                          <strong className={styles.summaryValue}>{dailyReport.totalNoShows}</strong>
                          <span className={styles.sectionHint}>{renderTrend(dailyReport.totalNoShowsTrend)}</span>
                        </article>
                      </div>

                      {dailyReport.rows.length > 0 && (
                        <div className={styles.reportTableWrap}>
                          <table className={styles.reportTable}>
                            <thead>
                              <tr>
                                <th>Office</th>
                                <th>Service</th>
                                <th>Served</th>
                                <th>Skipped</th>
                                <th>No-shows</th>
                              </tr>
                            </thead>
                            <tbody>
                              {dailyReport.rows.map(row => (
                                <tr key={`${row.officeId}:${row.serviceId}`}>
                                  <td>{row.officeName}</td>
                                  <td>{row.serviceName}</td>
                                  <td>
                                    {row.served}
                                    <span className={styles.rowTrend}> {renderTrend(row.servedTrend)}</span>
                                  </td>
                                  <td>
                                    {row.skipped}
                                    <span className={styles.rowTrend}> {renderTrend(row.skippedTrend)}</span>
                                  </td>
                                  <td>
                                    {row.noShows}
                                    <span className={styles.rowTrend}> {renderTrend(row.noShowsTrend)}</span>
                                  </td>
                                </tr>
                              ))}
                            </tbody>
                          </table>
                        </div>
                      )}
                    </>
                  )}
                </div>
              </section>
            )}

        {activeTab === 'offices' && (
          <OfficeManagementPage embedded />
        )}

        {activeTab === 'services' && (
          <ServiceManagementPage embedded />
        )}

        {activeTab === 'queues' && (
          <>
            <section className={styles.section}>
              <h2 className={styles.sectionTitle}>Create a New Queue</h2>
              <p className={styles.sectionHint}>Create and share queue links for customers to join in seconds.</p>

              {createError && (
                <div className={styles.errorBanner} role="alert" data-testid="create-error">
                  {createError}
                </div>
              )}

              {newLink && (
                <div className={styles.successBanner} role="status" data-testid="new-link-banner">
                  <span>Queue created! Shareable link:</span>
                  <strong className={styles.linkText}>{window.location.origin}{newLink}</strong>
                </div>
              )}

              <form className={styles.createForm} onSubmit={handleCreate} noValidate>
                <div className={styles.formGrid}>
                  <div className={styles.formGroup}>
                    <label className={styles.label} htmlFor="queue-name">Queue Name</label>
                    <input
                      id="queue-name"
                      className={`${styles.input} ${formErrors.name ? styles.inputError : ''}`}
                      type="text"
                      placeholder="e.g. General Counter"
                      value={form.name}
                      onChange={e => setForm(f => ({ ...f, name: e.target.value }))}
                    />
                    {formErrors.name && (
                      <span className={styles.fieldError}>{formErrors.name}</span>
                    )}
                  </div>

                  <div className={styles.formGroup}>
                    <label className={styles.label} htmlFor="queue-capacity">Max Capacity</label>
                    <input
                      id="queue-capacity"
                      className={`${styles.input} ${formErrors.maxCapacity ? styles.inputError : ''}`}
                      type="number"
                      min={1}
                      value={form.maxCapacity}
                      onChange={e => setForm(f => ({ ...f, maxCapacity: e.target.value }))}
                    />
                    {formErrors.maxCapacity && (
                      <span className={styles.fieldError}>{formErrors.maxCapacity}</span>
                    )}
                  </div>

                  <div className={styles.formGroup}>
                    <label className={styles.label} htmlFor="queue-avg-time">
                      Avg. Service Time (seconds)
                    </label>
                    <input
                      id="queue-avg-time"
                      className={`${styles.input} ${formErrors.averageServiceTimeSeconds ? styles.inputError : ''}`}
                      type="number"
                      min={1}
                      value={form.averageServiceTimeSeconds}
                      onChange={e => setForm(f => ({ ...f, averageServiceTimeSeconds: e.target.value }))}
                    />
                    {formErrors.averageServiceTimeSeconds && (
                      <span className={styles.fieldError}>{formErrors.averageServiceTimeSeconds}</span>
                    )}
                  </div>
                </div>

                <button
                  className={styles.createBtn}
                  type="submit"
                  disabled={creating}
                  data-testid="create-queue-btn"
                >
                  {creating ? 'Creating…' : 'Create Queue'}
                </button>
              </form>
            </section>

            <section className={styles.section}>
              <h2 className={styles.sectionTitle}>Your Queues</h2>
              <p className={styles.sectionHint}>Copy and share queue links, then assign staff who can operate each queue.</p>

              {loadError && (
                <div className={styles.errorBanner} role="alert">{loadError}</div>
              )}

              {queueAssignmentError && (
                <div className={styles.errorBanner} role="alert">{queueAssignmentError}</div>
              )}

              {queueAssignmentSuccess && (
                <div className={styles.successBanner} role="status">{queueAssignmentSuccess}</div>
              )}

              {queues.length === 0 && !loadError && (
                <p className={styles.emptyNote}>
                  No queues yet. Create your first queue above and share the link with users.
                </p>
              )}

              {queues.length > 0 && (
                <ul className={styles.queueList} data-testid="queue-list">
                  {queues.map(queue => (
                    <li key={queue.queueId} className={styles.queueCard} data-testid="queue-card">
                      <div className={styles.queueCardLeft}>
                        <span className={styles.queueName}>{queue.name}</span>
                        <span className={styles.queueMeta}>
                          Capacity: {queue.maxCapacity} &middot; Avg. {queue.averageServiceTimeSeconds}s &middot;{' '}
                          <span
                            className={
                              queue.status === 'Active'
                                ? styles.statusActive
                                : styles.statusInactive
                            }
                          >
                            {queue.status}
                          </span>
                        </span>
                        <span className={styles.queueLink}>
                          {window.location.origin}{queue.shareableLink}
                        </span>
                      </div>
                      <div className={styles.queueCardActions}>
                        <button
                          className={styles.copyBtn}
                          type="button"
                          onClick={() => copyLink(queue)}
                          data-testid={`copy-btn-${queue.queueId}`}
                        >
                          {copiedId === queue.queueId ? '✓ Copied!' : 'Copy Link'}
                        </button>
                        <button
                          className={styles.deleteBtn}
                          type="button"
                          onClick={() => handleDeleteQueue(queue)}
                          disabled={queueDeleteBusyId === queue.queueId}
                          data-testid={`delete-btn-${queue.queueId}`}
                        >
                          {queueDeleteBusyId === queue.queueId ? 'Deleting...' : 'Delete Queue'}
                        </button>
                      </div>

                      <div className={styles.assignmentArea}>
                        <h4 className={styles.assignmentTitle}>Assigned Staff</h4>
                        <div className={styles.assignmentRow}>
                          <select
                            className={styles.input}
                            value={queueAssignmentSelection[queue.queueId] ?? ''}
                            onChange={e => setQueueAssignmentSelection(prev => ({
                              ...prev,
                              [queue.queueId]: e.target.value,
                            }))}
                            disabled={queueAssignmentLoading || staffAccountsLoading}
                          >
                            <option value="">Select staff account</option>
                            {staffAccounts
                              .filter(staff => staff.isActive)
                              .map(staff => (
                                <option key={staff.userId} value={staff.userId}>
                                  {staff.name} ({staff.email})
                                </option>
                              ))}
                          </select>
                          <button
                            className={styles.copyBtn}
                            type="button"
                            onClick={() => handleAssignStaff(queue.queueId)}
                            disabled={
                              !queueAssignmentSelection[queue.queueId] ||
                              queueAssignmentBusyKey === `${queue.queueId}:${queueAssignmentSelection[queue.queueId]}:assign`
                            }
                          >
                            {queueAssignmentBusyKey === `${queue.queueId}:${queueAssignmentSelection[queue.queueId]}:assign`
                              ? 'Assigning...'
                              : 'Assign'}
                          </button>
                        </div>

                        {queueAssignmentLoading && (
                          <p className={styles.emptyNote}>Loading assignments...</p>
                        )}

                        {!queueAssignmentLoading && (queueAssignments[queue.queueId]?.length ?? 0) === 0 && (
                          <p className={styles.emptyNote}>No staff assigned yet.</p>
                        )}

                        {!queueAssignmentLoading && (queueAssignments[queue.queueId]?.length ?? 0) > 0 && (
                          <ul className={styles.assignmentList}>
                            {(queueAssignments[queue.queueId] ?? []).map(assignment => (
                              <li key={assignment.staffUserId} className={styles.assignmentItem}>
                                <div className={styles.assignmentInfo}>
                                  <strong>{assignment.name}</strong>
                                  <span className={styles.staffMeta}>{assignment.email}</span>
                                  <span className={assignment.isActive ? styles.statusActive : styles.statusInactive}>
                                    {assignment.isActive ? 'Active' : 'Inactive'}
                                  </span>
                                </div>
                                <button
                                  className={styles.copyBtn}
                                  type="button"
                                  onClick={() => handleUnassignStaff(queue.queueId, assignment.staffUserId)}
                                  disabled={queueAssignmentBusyKey === `${queue.queueId}:${assignment.staffUserId}:unassign`}
                                >
                                  {queueAssignmentBusyKey === `${queue.queueId}:${assignment.staffUserId}:unassign`
                                    ? 'Removing...'
                                    : 'Remove'}
                                </button>
                              </li>
                            ))}
                          </ul>
                        )}
                      </div>
                    </li>
                  ))}
                </ul>
              )}
            </section>
          </>
        )}

        {activeTab === 'appointments' && (
          <>
            <section className={styles.section}>
              <h2 className={styles.sectionTitle}>Create Appointment Profile</h2>
              <p className={styles.sectionHint}>
                Create a dedicated appointment link for each service stream.
              </p>

              {profileError && (
                <div className={styles.errorBanner} role="alert">{profileError}</div>
              )}

              <form className={styles.createForm} onSubmit={handleCreateAppointmentProfile} noValidate>
                <div className={styles.formGrid}>
                  <div className={styles.formGroup}>
                    <label className={styles.label} htmlFor="appointment-profile-name">Appointment Profile Name</label>
                    <input
                      id="appointment-profile-name"
                      className={styles.input}
                      type="text"
                      placeholder="e.g. Haircut Bookings"
                      value={newAppointmentProfileName}
                      onChange={e => setNewAppointmentProfileName(e.target.value)}
                    />
                  </div>
                </div>
                <button className={styles.createBtn} type="submit" disabled={profileCreating || !newAppointmentProfileName.trim()}>
                  {profileCreating ? 'Creating…' : 'Create Appointment Profile'}
                </button>
              </form>
            </section>

            <section className={styles.section}>
              <h2 className={styles.sectionTitle}>Your Appointments</h2>
              <p className={styles.sectionHint}>
                View available appointment profiles, then edit a profile to configure schedule and assign staff.
              </p>

              {profileLoading && <p className={styles.emptyNote}>Loading appointment profiles...</p>}

              {!profileLoading && appointmentProfiles.length === 0 && (
                <p className={styles.emptyNote}>No appointment profiles yet. Create one above to start configuring.</p>
              )}

              {scheduleError && (
                <div className={styles.errorBanner} role="alert">{scheduleError}</div>
              )}

              {scheduleSuccess && (
                <div className={styles.successBanner} role="status">{scheduleSuccess}</div>
              )}

              {!profileLoading && appointmentProfiles.length > 0 && (
                <ul className={styles.appointmentProfileList}>
                  {appointmentProfiles.map(profile => {
                    const assignedCount = appointmentStaffAssignments[profile.appointmentProfileId]?.length ?? 0
                    return (
                      <li key={profile.appointmentProfileId} className={styles.appointmentProfileCard}>
                        <div className={styles.appointmentProfileCardInfo}>
                          <strong className={styles.appointmentProfileName}>{profile.name}</strong>
                          <span className={styles.appointmentProfileMeta}>
                            {profile.isActive ? 'Active profile' : 'Inactive profile'}
                          </span>
                          <span className={styles.appointmentProfileMeta}>
                            Assigned staff: {assignedCount}
                          </span>
                        </div>
                        <div className={styles.appointmentProfileCardActions}>
                          <button
                            className={styles.copyBtn}
                            type="button"
                            onClick={() => navigator.clipboard.writeText(`${window.location.origin}${profile.shareableLink}`)}
                          >
                            Copy Link
                          </button>
                          <button
                            className={styles.createBtn}
                            type="button"
                            onClick={() => openAppointmentEditor(profile)}
                          >
                            Edit
                          </button>
                        </div>
                      </li>
                    )
                  })}
                </ul>
              )}

              {appointmentEditorOpen && editingAppointmentProfile && (
                <div className={styles.appointmentModalOverlay} onClick={closeAppointmentEditor} role="presentation">
                  <div className={styles.appointmentModal} onClick={e => e.stopPropagation()} role="dialog" aria-modal="true" aria-labelledby="appointment-editor-title">
                    <div className={styles.appointmentModalHeader}>
                      <h3 id="appointment-editor-title" className={styles.appointmentModalTitle}>
                        Edit Appointment: {editingAppointmentProfile.name}
                      </h3>
                      <button className={styles.copyBtn} type="button" onClick={closeAppointmentEditor}>Close</button>
                    </div>

                    <div className={styles.appointmentModalBody}>
                      <div className={styles.statsRow}>
                        <article className={styles.statCard}>
                          <span className={styles.statLabel}>Enabled days</span>
                          <strong className={styles.statValue}>{appointmentSummary.enabledDays}/7</strong>
                        </article>
                        <article className={styles.statCard}>
                          <span className={styles.statLabel}>Total weekly capacity</span>
                          <strong className={styles.statValue}>{appointmentSummary.totalWeeklySlots}</strong>
                          <span className={styles.statHint}>appointments/week</span>
                        </article>
                      </div>

                      {editorShareLink && (
                        <div className={styles.successBanner} role="status">
                          <span>Shareable appointment link:</span>
                          <strong className={styles.linkText}>{window.location.origin}{editorShareLink}</strong>
                          <button className={styles.copyBtn} type="button" onClick={copyAppointmentLink}>
                            {copiedAppointmentLink ? '✓ Copied!' : 'Copy Link'}
                          </button>
                        </div>
                      )}

                      <div className={styles.staffAssignCard}>
                        <h4 className={styles.sectionSubTitle}>Assign Staff</h4>
                        <p className={styles.sectionHint}>Select staff members responsible for this appointment profile.</p>
                        {staffAccounts.filter(staff => staff.isActive).length === 0 && (
                          <p className={styles.emptyNote}>No active staff accounts available.</p>
                        )}
                        <div className={styles.staffAssignGrid}>
                          {staffAccounts
                            .filter(staff => staff.isActive)
                            .map(staff => (
                              <label key={`appointment-staff-${staff.userId}`} className={styles.staffAssignItem}>
                                <input
                                  type="checkbox"
                                  checked={editorAssignedStaffUserIds.includes(staff.userId)}
                                  onChange={() => toggleEditorStaffAssignment(staff.userId)}
                                />
                                <span>
                                  <strong>{staff.name}</strong>
                                  <span className={styles.staffMeta}>{staff.email}</span>
                                </span>
                              </label>
                            ))}
                        </div>
                      </div>

                      {scheduleLoading && <p className={styles.emptyNote}>Loading schedule...</p>}

                      {!scheduleLoading && editorRules.length > 0 && (
                        <div className={styles.scheduleList}>
                          {editorRules
                            .slice()
                            .sort((a, b) => a.dayOfWeek - b.dayOfWeek)
                            .map(rule => {
                              const daySlots = slotsForRule(rule)
                              return (
                                <article
                                  key={rule.dayOfWeek}
                                  className={`${styles.scheduleCard} ${rule.isEnabled ? styles.scheduleCardEnabled : styles.scheduleCardDisabled}`}
                                >
                                  <header className={styles.scheduleCardHeader}>
                                    <div>
                                      <h3 className={styles.scheduleDay}>{dayLabels[rule.dayOfWeek]}</h3>
                                      <p className={styles.scheduleSummary}>
                                        {rule.isEnabled
                                          ? `${toInputTime(rule.startTime)} - ${toInputTime(rule.endTime)} · every ${rule.slotDurationMinutes} min`
                                          : 'Closed'}
                                      </p>
                                    </div>
                                    <span className={styles.capacityPill}>{daySlots} slots</span>
                                  </header>

                                  <div className={styles.scheduleGrid}>
                                    <label className={styles.checkboxLabel}>
                                      <input
                                        type="checkbox"
                                        checked={rule.isEnabled}
                                        onChange={e => updateRule(rule.dayOfWeek, { isEnabled: e.target.checked })}
                                      />
                                      <span>Open for appointments</span>
                                    </label>

                                    <div className={styles.formGroup}>
                                      <label className={styles.label}>Start time</label>
                                      <input
                                        className={styles.input}
                                        type="time"
                                        value={toInputTime(rule.startTime)}
                                        disabled={!rule.isEnabled}
                                        onChange={e => updateRule(rule.dayOfWeek, { startTime: `${e.target.value}:00` })}
                                      />
                                    </div>

                                    <div className={styles.formGroup}>
                                      <label className={styles.label}>End time</label>
                                      <input
                                        className={styles.input}
                                        type="time"
                                        value={toInputTime(rule.endTime)}
                                        disabled={!rule.isEnabled}
                                        onChange={e => updateRule(rule.dayOfWeek, { endTime: `${e.target.value}:00` })}
                                      />
                                    </div>

                                    <div className={styles.formGroup}>
                                      <label className={styles.label}>Duration per appointment (mins)</label>
                                      <input
                                        className={styles.input}
                                        type="number"
                                        min={5}
                                        max={240}
                                        value={rule.slotDurationMinutes}
                                        disabled={!rule.isEnabled}
                                        onChange={e => {
                                          const parsed = Number.parseInt(e.target.value || '30', 10)
                                          updateRule(rule.dayOfWeek, {
                                            slotDurationMinutes: Number.isNaN(parsed) ? 30 : parsed,
                                          })
                                        }}
                                      />
                                    </div>
                                  </div>
                                </article>
                              )
                            })}
                        </div>
                      )}
                    </div>

                    <div className={styles.appointmentModalActions}>
                      <button className={styles.secondaryBtn} type="button" onClick={closeAppointmentEditor}>
                        Cancel
                      </button>
                      <button
                        className={styles.createBtn}
                        type="button"
                        onClick={saveSchedule}
                        disabled={scheduleSaving || scheduleLoading || editorRules.length !== 7}
                      >
                        {scheduleSaving ? 'Saving…' : 'Save Appointment Settings'}
                      </button>
                    </div>
                  </div>
                </div>
              )}
            </section>
          </>
        )}

        {activeTab === 'staff' && (
          <section className={styles.section}>
            <h2 className={styles.sectionTitle}>Create Staff Account</h2>
            <p className={styles.sectionHint}>
              Staff users are restricted to queue-operations for this organisation.
            </p>

            {staffError && (
              <div className={styles.errorBanner} role="alert">{staffError}</div>
            )}

            {staffSuccess && (
              <div className={styles.successBanner} role="status">{staffSuccess}</div>
            )}

            <form className={styles.createForm} onSubmit={handleCreateStaff} noValidate>
              <div className={styles.formGrid}>
                <div className={styles.formGroup}>
                  <label className={styles.label} htmlFor="staff-name">Full Name</label>
                  <input
                    id="staff-name"
                    className={styles.input}
                    type="text"
                    placeholder="e.g. Jane Operator"
                    value={staffForm.name}
                    onChange={e => setStaffForm(prev => ({ ...prev, name: e.target.value }))}
                  />
                </div>

                <div className={styles.formGroup}>
                  <label className={styles.label} htmlFor="staff-email">Email</label>
                  <input
                    id="staff-email"
                    className={styles.input}
                    type="email"
                    placeholder="staff@yourorg.com"
                    value={staffForm.email}
                    onChange={e => setStaffForm(prev => ({ ...prev, email: e.target.value }))}
                  />
                </div>

                <div className={styles.formGroup}>
                  <label className={styles.label} htmlFor="staff-phone">Phone (optional)</label>
                  <input
                    id="staff-phone"
                    className={styles.input}
                    type="tel"
                    placeholder="+44..."
                    value={staffForm.phone}
                    onChange={e => setStaffForm(prev => ({ ...prev, phone: e.target.value }))}
                  />
                </div>

              </div>

              <button
                className={styles.createBtn}
                type="submit"
                disabled={
                  staffCreating ||
                  !staffForm.name.trim() ||
                  !staffForm.email.trim()
                }
              >
                {staffCreating ? 'Creating…' : 'Send Staff Invite'}
              </button>
            </form>

            {latestInvite && (
              <div className={styles.successBanner} role="status">
                <span>Invite link:</span>
                <strong className={styles.linkText}>{window.location.origin}{latestInvite.invitePath}</strong>
                <button
                  className={styles.copyBtn}
                  type="button"
                  onClick={() => navigator.clipboard.writeText(`${window.location.origin}${latestInvite.invitePath}`)}
                >
                  Copy Invite Link
                </button>
              </div>
            )}

            <div className={styles.staffListBlock}>
              <h3 className={styles.sectionSubTitle}>Existing Staff Accounts</h3>

              {staffAccountsLoading && (
                <p className={styles.emptyNote}>Loading staff accounts...</p>
              )}

              {!staffAccountsLoading && staffAccounts.length === 0 && (
                <p className={styles.emptyNote}>No staff accounts yet.</p>
              )}

              {!staffAccountsLoading && staffAccounts.length > 0 && (
                <ul className={styles.staffList}>
                  {staffAccounts.map(user => (
                    <li key={user.userId} className={styles.staffCard}>
                      {(() => {
                        const profile = staffProfileByEmail.get(user.email.toLowerCase())
                        return (
                          <>
                            <div className={styles.staffCardInfo}>
                              <strong className={styles.staffName}>{user.name}</strong>
                              <span className={styles.staffMeta}>{user.email}</span>
                              {user.phone && <span className={styles.staffMeta}>{user.phone}</span>}
                              {profile?.shiftStart && profile?.shiftEnd && (
                                <span className={styles.staffMeta}>Shift: {profile.shiftStart}-{profile.shiftEnd}</span>
                              )}
                              {profile && (
                                <span className={styles.staffMeta}>
                                  Assigned office: {profile.officeIds[0] ? (officeNameById.get(profile.officeIds[0]) ?? 'Unmapped office') : 'Unassigned'}
                                </span>
                              )}
                              <span className={styles.staffMeta}>
                                Created {new Date(user.createdAt).toLocaleDateString()}
                              </span>
                            </div>

                            <div className={styles.staffActions}>
                              <span className={user.isActive ? styles.statusActive : styles.statusInactive}>
                                {user.isActive ? 'Active' : 'Inactive'}
                              </span>
                              <button
                                type="button"
                                className={styles.copyBtn}
                                disabled={staffActionUserId === user.userId}
                                onClick={() => handleStaffStatusToggle(user)}
                              >
                                {staffActionUserId === user.userId
                                  ? 'Updating...'
                                  : (user.isActive ? 'Deactivate' : 'Reactivate')}
                              </button>
                              <button
                                type="button"
                                className={styles.copyBtn}
                                onClick={() => profile && beginEditStaffProfile(profile)}
                                disabled={!profile}
                              >
                                Edit Details
                              </button>
                            </div>
                          </>
                        )
                      })()}
                    </li>
                  ))}
                </ul>
              )}
            </div>

            <div className={styles.staffListBlock}>
              <h3 className={styles.sectionSubTitle}>Staff Profile Management</h3>
              <p className={styles.sectionHint}>Edit assigned offices, counter and shift details from Staff Accounts.</p>

              {staffProfilesLoading && (
                <p className={styles.emptyNote}>Loading staff profile data...</p>
              )}

              {!staffProfilesLoading && selectedStaffProfileId && (
                <div className={styles.profileEditorCard}>
                  <div className={styles.formGrid}>
                    <div className={styles.formGroup}>
                      <label className={styles.label} htmlFor="staff-edit-name">Name</label>
                      <input
                        id="staff-edit-name"
                        className={styles.input}
                        type="text"
                        value={staffProfileForm.name}
                        onChange={e => setStaffProfileForm(prev => ({ ...prev, name: e.target.value }))}
                      />
                    </div>

                    <div className={styles.formGroup}>
                      <label className={styles.label} htmlFor="staff-edit-phone">Phone</label>
                      <input
                        id="staff-edit-phone"
                        className={styles.input}
                        type="tel"
                        value={staffProfileForm.phone}
                        onChange={e => setStaffProfileForm(prev => ({ ...prev, phone: e.target.value }))}
                      />
                    </div>

                    <div className={styles.formGroup}>
                      <label className={styles.label} htmlFor="staff-edit-shift-start">Shift Start (HH:mm)</label>
                      <input
                        id="staff-edit-shift-start"
                        className={styles.input}
                        type="time"
                        value={staffProfileForm.shiftStart}
                        onChange={e => setStaffProfileForm(prev => ({ ...prev, shiftStart: e.target.value }))}
                      />
                    </div>

                    <div className={styles.formGroup}>
                      <label className={styles.label} htmlFor="staff-edit-shift-end">Shift End (HH:mm)</label>
                      <input
                        id="staff-edit-shift-end"
                        className={styles.input}
                        type="time"
                        value={staffProfileForm.shiftEnd}
                        onChange={e => setStaffProfileForm(prev => ({ ...prev, shiftEnd: e.target.value }))}
                      />
                    </div>
                  </div>

                  <div>
                    <p className={styles.sectionSubTitle}>Assigned Office</p>
                    {officeOptions.length === 0 ? (
                      <p className={styles.emptyNote}>No active offices found.</p>
                    ) : (
                      <div className={styles.formGroup}>
                        <label className={styles.label} htmlFor="staff-edit-office">Office</label>
                        <select
                          id="staff-edit-office"
                          className={styles.input}
                          value={staffProfileForm.officeId}
                          onChange={e => setStaffProfileForm(prev => ({ ...prev, officeId: e.target.value }))}
                        >
                          <option value="">Select one office</option>
                          {officeOptions.map(office => (
                            <option key={office.officeId} value={office.officeId}>
                              {office.name}
                            </option>
                          ))}
                        </select>
                      </div>
                    )}
                  </div>

                  <div className={styles.staffEditorActions}>
                    <button
                      type="button"
                      className={styles.createBtn}
                      onClick={saveStaffProfile}
                      disabled={staffProfileSaving || !staffProfileForm.name.trim()}
                    >
                      {staffProfileSaving ? 'Saving...' : 'Save Staff Details'}
                    </button>
                    <button
                      type="button"
                      className={styles.copyBtn}
                      onClick={resetStaffProfileEditor}
                      disabled={staffProfileSaving}
                    >
                      Cancel
                    </button>
                  </div>
                </div>
              )}

              {!staffProfilesLoading && !selectedStaffProfileId && (
                <p className={styles.emptyNote}>Choose a staff account above and click Edit Details.</p>
              )}
            </div>
          </section>
        )}
            </div>

            <OnboardingTour
              isOpen={onboarding.isOpen}
              title="Quick tour: admin dashboard"
              steps={ROLE_TOUR_CONTENT.admin}
              onClose={onboarding.completeTour}
            />
          </div>
        </div>
      </main>
    </div>
  )
}

function isTypingTarget(target: EventTarget | null): boolean {
  if (!(target instanceof HTMLElement)) return false

  const tag = target.tagName.toLowerCase()
  if (tag === 'input' || tag === 'textarea' || tag === 'select') return true

  return target.isContentEditable
}

function keyToAdminTab(key: string): AdminTab | null {
  if (key === '1' || key === 'h') return 'home'
  if (key === '2' || key === 'q') return 'queues'
  if (key === '3' || key === 'a') return 'appointments'
  if (key === '4' || key === 's') return 'services'
  if (key === '5' || key === 'o') return 'offices'
  if (key === '6' || key === 't') return 'staff'
  if (key === '7' || key === 'r') return 'reports'
  if (key === '8' || key === 'p') return 'profile'
  if (key === '9' || key === 'g') return 'settings'
  return null
}

function LogoutIcon() {
  return (
    <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
      <path d="M9 21H5a2 2 0 01-2-2V5a2 2 0 012-2h4" />
      <polyline points="16 17 21 12 16 7" />
      <line x1="21" y1="12" x2="9" y2="12" />
    </svg>
  )
}
