import { useEffect, useState } from 'react'
import { Link, useNavigate, useParams } from 'react-router-dom'
import { clearToken, getTokenPayload } from '../../utils/authToken'
import {
  getAppointmentNotificationPreferences,
  getQueueNotificationPreference,
  updateAppointmentNotificationPreferences,
  updateQueueNotificationPreference,
} from '../../api/auth'
import {
  listMyNotifications,
  markAllNotificationsRead,
  markNotificationRead,
  type InAppNotification,
} from '../../api/notifications'
import { getMyQueues, type MyQueueEntry } from '../../api/queues'
import { cancelAppointment, getMyAppointmentBookings, type MyAppointmentBooking } from '../../api/appointments'
import type { ApiError } from '../../types/api'
import logoImg from '../../assets/nextTurn-logo.png'
import styles from './DashboardPage.module.css'

type DashboardTab = 'home' | 'queues' | 'appointments' | 'notifications'

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

export function DashboardPage() {
  const navigate = useNavigate()
  const { tenantId: routeTenantId } = useParams<{ tenantId?: string }>()
  const payload = getTokenPayload()

  if (!payload) {
    clearToken()
    navigate('/', { replace: true })
    return null
  }

  const { name, email, role } = payload
  const badge = roleBadgeLabel(role)

  const [activeTab, setActiveTab] = useState<DashboardTab>('home')

  const [queues, setQueues] = useState<MyQueueEntry[]>([])
  const [queuesLoading, setQueuesLoading] = useState(true)
  const [queuesError, setQueuesError] = useState<string | null>(null)

  const [appointments, setAppointments] = useState<MyAppointmentBooking[]>([])
  const [appointmentsLoading, setAppointmentsLoading] = useState(true)
  const [appointmentsError, setAppointmentsError] = useState<string | null>(null)
  const [appointmentsSuccess, setAppointmentsSuccess] = useState<string | null>(null)
  const [cancellingAppointmentId, setCancellingAppointmentId] = useState<string | null>(null)

  const [queueNotificationsEnabled, setQueueNotificationsEnabled] = useState(true)
  const [notificationsLoading, setNotificationsLoading] = useState(true)
  const [notificationsSaving, setNotificationsSaving] = useState(false)
  const [notificationsMessage, setNotificationsMessage] = useState<string | null>(null)
  const [notificationsError, setNotificationsError] = useState<string | null>(null)

  const [inAppNotifications, setInAppNotifications] = useState<InAppNotification[]>([])
  const [inAppNotificationsLoading, setInAppNotificationsLoading] = useState(true)
  const [inAppNotificationsError, setInAppNotificationsError] = useState<string | null>(null)
  const [markingAllRead, setMarkingAllRead] = useState(false)
  const [markingSingleReadId, setMarkingSingleReadId] = useState<string | null>(null)

  const [appointmentNotificationsLoading, setAppointmentNotificationsLoading] = useState(true)
  const [appointmentNotificationsSaving, setAppointmentNotificationsSaving] = useState(false)
  const [appointmentNotificationsMessage, setAppointmentNotificationsMessage] = useState<string | null>(null)
  const [appointmentNotificationsError, setAppointmentNotificationsError] = useState<string | null>(null)
  const [appointmentBookedNotificationsEnabled, setAppointmentBookedNotificationsEnabled] = useState(true)
  const [appointmentRescheduledNotificationsEnabled, setAppointmentRescheduledNotificationsEnabled] = useState(true)
  const [appointmentCancelledNotificationsEnabled, setAppointmentCancelledNotificationsEnabled] = useState(true)

  const [linkInput, setLinkInput] = useState('')
  const [linkError, setLinkError] = useState<string | null>(null)

  const [appointmentLinkInput, setAppointmentLinkInput] = useState('')
  const [appointmentLinkError, setAppointmentLinkError] = useState<string | null>(null)

  const tenantId = routeTenantId ?? (payload.tid === '00000000-0000-0000-0000-000000000000' ? undefined : payload.tid)

  useEffect(() => {
    getMyQueues()
      .then(data => {
        setQueues(data)
        setQueuesLoading(false)
      })
      .catch(() => {
        setQueuesError('Could not load your queues.')
        setQueuesLoading(false)
      })

    getMyAppointmentBookings()
      .then(data => {
        setAppointments(data)
        setAppointmentsLoading(false)
      })
      .catch(() => {
        setAppointmentsError('Could not load your appointment bookings.')
        setAppointmentsLoading(false)
      })

    getQueueNotificationPreference(tenantId)
      .then(data => {
        setQueueNotificationsEnabled(data.queueTurnApproachingNotificationsEnabled)
        setNotificationsLoading(false)
      })
      .catch(() => {
        setNotificationsError('Could not load your queue notification setting.')
        setNotificationsLoading(false)
      })

    getAppointmentNotificationPreferences(tenantId)
      .then(data => {
        setAppointmentBookedNotificationsEnabled(data.appointmentBookedNotificationsEnabled)
        setAppointmentRescheduledNotificationsEnabled(data.appointmentRescheduledNotificationsEnabled)
        setAppointmentCancelledNotificationsEnabled(data.appointmentCancelledNotificationsEnabled)
        setAppointmentNotificationsLoading(false)
      })
      .catch(() => {
        setAppointmentNotificationsError('Could not load your appointment notification settings.')
        setAppointmentNotificationsLoading(false)
      })

    listMyNotifications(25, tenantId)
      .then(data => {
        setInAppNotifications(data)
        setInAppNotificationsLoading(false)
      })
      .catch(() => {
        setInAppNotificationsError('Could not load in-app notifications.')
        setInAppNotificationsLoading(false)
      })
  }, [tenantId])

  useEffect(() => {
    const timer = window.setInterval(() => {
      listMyNotifications(25, tenantId)
        .then(data => {
          setInAppNotifications(data)
          setInAppNotificationsError(null)
        })
        .catch(() => {
          setInAppNotificationsError('Could not refresh in-app notifications.')
        })
    }, 30000)

    return () => window.clearInterval(timer)
  }, [tenantId])

  useEffect(() => {
    if (!appointmentsSuccess) return

    const timer = window.setTimeout(() => {
      setAppointmentsSuccess(null)
    }, 4000)

    return () => window.clearTimeout(timer)
  }, [appointmentsSuccess])

  function handleJoinByLink() {
    setLinkError(null)
    try {
      const url = new URL(linkInput.includes('://') ? linkInput : `https://x.com${linkInput}`)
      const match = url.pathname.match(/\/queues\/([^/]+)\/([^/]+)/)
      if (!match) throw new Error('invalid')
      const [, linkTenant, linkQueue] = match
      navigate(`/queues/${linkTenant}/${linkQueue}`)
    } catch {
      setLinkError('Invalid queue link. Paste the full URL or the /queues/... path.')
    }
  }

  function handleOpenAppointmentByLink() {
    setAppointmentLinkError(null)
    try {
      const url = new URL(appointmentLinkInput.includes('://') ? appointmentLinkInput : `https://x.com${appointmentLinkInput}`)
      const match = url.pathname.match(/\/appointments\/([^/]+)\/([^/]+)/)
      if (!match) throw new Error('invalid')
      const [, linkTenant, linkProfile] = match
      navigate(`/appointments/${linkTenant}/${linkProfile}`)
    } catch {
      setAppointmentLinkError('Invalid appointment link. Paste the full URL or the /appointments/tenant/profile path.')
    }
  }

  function handleLogout() {
    clearToken()
    navigate('/', { replace: true })
  }

  async function handleSaveNotificationPreference() {
    setNotificationsError(null)
    setNotificationsMessage(null)
    setNotificationsSaving(true)

    try {
      await updateQueueNotificationPreference(tenantId, queueNotificationsEnabled)
      setNotificationsMessage('Queue notification preference saved.')
    } catch (err) {
      const apiErr = err as ApiError
      setNotificationsError(apiErr.detail ?? 'Could not save queue notification setting.')
    } finally {
      setNotificationsSaving(false)
    }
  }

  async function handleSaveAppointmentNotificationPreferences() {
    setAppointmentNotificationsError(null)
    setAppointmentNotificationsMessage(null)
    setAppointmentNotificationsSaving(true)

    try {
      await updateAppointmentNotificationPreferences(tenantId, {
        appointmentBookedNotificationsEnabled,
        appointmentRescheduledNotificationsEnabled,
        appointmentCancelledNotificationsEnabled,
      })
      setAppointmentNotificationsMessage('Appointment notification preferences saved.')
    } catch (err) {
      const apiErr = err as ApiError
      setAppointmentNotificationsError(apiErr.detail ?? 'Could not save appointment notification settings.')
    } finally {
      setAppointmentNotificationsSaving(false)
    }
  }

  async function handleMarkNotificationRead(notificationId: string) {
    setMarkingSingleReadId(notificationId)
    try {
      await markNotificationRead(notificationId, tenantId)
      setInAppNotifications(prev => prev.map(n => (n.notificationId === notificationId ? { ...n, isRead: true } : n)))
    } catch {
      setInAppNotificationsError('Could not mark notification as read.')
    } finally {
      setMarkingSingleReadId(null)
    }
  }

  async function handleMarkAllNotificationsRead() {
    setMarkingAllRead(true)
    try {
      await markAllNotificationsRead(tenantId)
      setInAppNotifications(prev => prev.map(n => ({ ...n, isRead: true })))
    } catch {
      setInAppNotificationsError('Could not mark all notifications as read.')
    } finally {
      setMarkingAllRead(false)
    }
  }

  async function handleCancelAppointment(appointment: MyAppointmentBooking) {
    const shouldCancel = window.confirm('Cancel this appointment booking?')
    if (!shouldCancel) return

    setAppointmentsError(null)
    setAppointmentsSuccess(null)
    setCancellingAppointmentId(appointment.appointmentId)

    try {
      await cancelAppointment(appointment.appointmentId, appointment.organisationId)
      setAppointments(prev => prev.filter(a => a.appointmentId !== appointment.appointmentId))
      setAppointmentsSuccess('Appointment booking cancelled successfully.')
    } catch (err) {
      const apiErr = err as ApiError
      setAppointmentsError(apiErr.detail ?? 'Could not cancel this appointment booking.')
    } finally {
      setCancellingAppointmentId(null)
    }
  }

  const unreadNotificationsCount = inAppNotifications.filter(n => !n.isRead).length

  useEffect(() => {
    function handleTabShortcuts(event: KeyboardEvent) {
      if (event.defaultPrevented || isTypingTarget(event.target)) {
        return
      }

      const key = event.key.toLowerCase()
      const nextTab = keyToDashboardTab(key)
      if (!nextTab) {
        return
      }

      event.preventDefault()
      setActiveTab(nextTab)
    }

    window.addEventListener('keydown', handleTabShortcuts)
    return () => window.removeEventListener('keydown', handleTabShortcuts)
  }, [])

  return (
    <div className={styles.page}>
      <header className={styles.navbar}>
        <div className={styles.navInner}>
          <div className={styles.navBrand}>
            <img src={logoImg} alt="NextTurn" className={styles.navLogo} />
          </div>

          <div className={styles.navUser}>
            <div className={styles.avatarCircle} aria-hidden="true">
              {name.charAt(0).toUpperCase()}
            </div>
            <div className={styles.userMeta}>
              <span className={styles.userName}>{name}</span>
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
          <aside className={styles.sidebar} aria-label="Dashboard navigation">
            <div className={styles.sidebarHeader}>
              <p className={styles.sidebarTitle}>Dashboard</p>
              <p className={styles.sidebarSubtitle}>Everything in one place</p>
            </div>

            <nav className={styles.sidebarNav}>
              <button
                type="button"
                className={`${styles.navItem} ${activeTab === 'home' ? styles.navItemActive : ''}`}
                onClick={() => setActiveTab('home')}
                aria-keyshortcuts="1 h"
              >
                <HomeIcon />
                <span>Home</span>
              </button>
              <button
                type="button"
                className={`${styles.navItem} ${activeTab === 'queues' ? styles.navItemActive : ''}`}
                onClick={() => setActiveTab('queues')}
                aria-keyshortcuts="2 q"
              >
                <QueueIcon />
                <span>Queues</span>
                <span className={styles.navCount}>{queues.length}</span>
              </button>
              <button
                type="button"
                className={`${styles.navItem} ${activeTab === 'appointments' ? styles.navItemActive : ''}`}
                onClick={() => setActiveTab('appointments')}
                aria-keyshortcuts="3 a"
              >
                <CalendarIcon />
                <span>Appointments</span>
                <span className={styles.navCount}>{appointments.length}</span>
              </button>
              <button
                type="button"
                className={`${styles.navItem} ${activeTab === 'notifications' ? styles.navItemActive : ''}`}
                onClick={() => setActiveTab('notifications')}
                aria-keyshortcuts="4 n"
              >
                <BellIcon />
                <span>Notifications</span>
                <span className={styles.navCount}>{unreadNotificationsCount}</span>
              </button>
            </nav>

            <p className={styles.shortcutHint}>Shortcuts: 1/2/3/4 or H/Q/A/N</p>
          </aside>

          <div className={styles.contentInner}>
            <div className={styles.welcome}>
              <div>
                <h1 className={styles.welcomeHeading}>Welcome back, {name.split(' ')[0]}!</h1>
                <p className={styles.welcomeSub}>{email}</p>
              </div>

              <div className={styles.activeTabBadge}>
                {activeTab === 'home' && 'Home'}
                {activeTab === 'queues' && 'Queues'}
                {activeTab === 'appointments' && 'Appointments'}
                {activeTab === 'notifications' && 'Notifications'}
              </div>
            </div>

            <div key={activeTab} className={styles.tabPanel}>
              {activeTab === 'home' && (
                <>
                <section className={styles.quickStats} aria-label="Quick overview">
                  <article className={styles.statCard}>
                    <p className={styles.statLabel}>Active queues</p>
                    <p className={styles.statValue}>{queues.length}</p>
                  </article>
                  <article className={styles.statCard}>
                    <p className={styles.statLabel}>Active appointments</p>
                    <p className={styles.statValue}>{appointments.length}</p>
                  </article>
                  <article className={styles.statCard}>
                    <p className={styles.statLabel}>Unread notifications</p>
                    <p className={styles.statValue}>{unreadNotificationsCount}</p>
                  </article>
                </section>

                <section className={styles.joinWidget} aria-label="Join a queue by link">
                  <div className={styles.sectionHeader}>
                    <LinkIcon />
                    <h2 className={styles.sectionTitle}>Join Queue by Link</h2>
                  </div>
                  <p className={styles.joinWidgetDesc}>Paste a queue URL to jump in immediately.</p>
                  <div className={styles.joinWidgetRow}>
                    <input
                      className={styles.joinWidgetInput}
                      type="text"
                      placeholder="https://... or /queues/tenant/queue"
                      value={linkInput}
                      onChange={e => {
                        setLinkInput(e.target.value)
                        setLinkError(null)
                      }}
                      onKeyDown={e => e.key === 'Enter' && handleJoinByLink()}
                      aria-label="Queue link"
                    />
                    <button className={styles.joinWidgetBtn} onClick={handleJoinByLink} type="button" disabled={!linkInput.trim()}>
                      Open
                    </button>
                  </div>
                  {linkError && <p className={styles.joinWidgetError}>{linkError}</p>}
                </section>

                <section className={styles.joinWidget} aria-label="Open appointment booking by link">
                  <div className={styles.sectionHeader}>
                    <CalendarIcon />
                    <h2 className={styles.sectionTitle}>Open Appointment by Link</h2>
                  </div>
                  <p className={styles.joinWidgetDesc}>Paste an appointment booking URL to continue quickly.</p>
                  <div className={styles.joinWidgetRow}>
                    <input
                      className={styles.joinWidgetInput}
                      type="text"
                      placeholder="https://... or /appointments/tenant/profile"
                      value={appointmentLinkInput}
                      onChange={e => {
                        setAppointmentLinkInput(e.target.value)
                        setAppointmentLinkError(null)
                      }}
                      onKeyDown={e => e.key === 'Enter' && handleOpenAppointmentByLink()}
                      aria-label="Appointment link"
                    />
                    <button
                      className={styles.joinWidgetBtn}
                      onClick={handleOpenAppointmentByLink}
                      type="button"
                      disabled={!appointmentLinkInput.trim()}
                    >
                      Open
                    </button>
                  </div>
                  {appointmentLinkError && <p className={styles.joinWidgetError}>{appointmentLinkError}</p>}
                </section>
                </>
              )}

              {activeTab === 'queues' && (
                <section className={styles.queueSection} aria-label="My active queues">
                <div className={styles.sectionHeader}>
                  <QueueIcon />
                  <h2 className={styles.sectionTitle}>My Active Queues</h2>
                </div>

                {queuesLoading && (
                  <div className={styles.queuePlaceholder}>
                    <span className={styles.queueSpinner} aria-hidden="true" />
                    <span>Loading queues...</span>
                  </div>
                )}

                {!queuesLoading && queuesError && <p className={styles.queueError}>{queuesError}</p>}

                {!queuesLoading && !queuesError && queues.length === 0 && (
                  <p className={styles.queueEmpty}>You haven't joined any queues yet.</p>
                )}

                {!queuesLoading && queues.length > 0 && (
                  <ul className={styles.queueList}>
                    {queues.map(q => (
                      <li key={q.queueId} className={styles.queueCard}>
                        <div className={styles.queueCardInfo}>
                          <span className={styles.queueCardName}>{q.queueName}</span>
                          <QueueStatusBadge status={q.queueStatus} />
                          <span className={styles.ticketChip}>#{q.ticketNumber}</span>
                        </div>
                        <Link to={`/queues/${q.organisationId}/${q.queueId}`} className={styles.queueJoinLink}>
                          View &rarr;
                        </Link>
                      </li>
                    ))}
                  </ul>
                )}
                </section>
              )}

              {activeTab === 'appointments' && (
                <section className={styles.queueSection} aria-label="My active appointment bookings">
                <div className={styles.sectionHeader}>
                  <CalendarIcon />
                  <h2 className={styles.sectionTitle}>My Active Appointment Bookings</h2>
                </div>

                {appointmentsLoading && (
                  <div className={styles.queuePlaceholder}>
                    <span className={styles.queueSpinner} aria-hidden="true" />
                    <span>Loading bookings...</span>
                  </div>
                )}

                {!appointmentsLoading && appointmentsError && <p className={styles.queueError}>{appointmentsError}</p>}

                {!appointmentsLoading && !appointmentsError && appointmentsSuccess && (
                  <p className={styles.queueSuccess}>{appointmentsSuccess}</p>
                )}

                {!appointmentsLoading && !appointmentsError && appointments.length === 0 && (
                  <p className={styles.queueEmpty}>You don't have any active appointment bookings yet.</p>
                )}

                {!appointmentsLoading && appointments.length > 0 && (
                  <ul className={styles.queueList}>
                    {appointments.map(a => (
                      <li key={a.appointmentId} className={styles.appointmentCard}>
                        <div className={styles.appointmentInfo}>
                          <span className={styles.queueCardName}>{a.appointmentProfileName}</span>
                          <span className={styles.appointmentMeta}>{a.organisationName}</span>
                          <span className={styles.appointmentMeta}>{formatDashboardSlot(a.slotStart, a.slotEnd)}</span>
                        </div>

                        <div className={styles.appointmentActions}>
                          <Link to={`/appointments/${a.organisationId}/${a.appointmentProfileId}`} className={styles.queueJoinLink}>
                            View &rarr;
                          </Link>
                          <button
                            type="button"
                            className={styles.appointmentCancelBtn}
                            onClick={() => handleCancelAppointment(a)}
                            disabled={cancellingAppointmentId === a.appointmentId}
                          >
                            {cancellingAppointmentId === a.appointmentId ? 'Cancelling...' : 'Cancel'}
                          </button>
                        </div>
                      </li>
                    ))}
                  </ul>
                )}
                </section>
              )}

              {activeTab === 'notifications' && (
                <>
                <section className={styles.settingsSection} aria-label="In-app notifications">
                  <div className={styles.sectionHeaderRow}>
                    <div className={styles.sectionHeader}>
                      <BellIcon />
                      <h2 className={styles.sectionTitle}>In-App Notifications</h2>
                    </div>

                    <button
                      type="button"
                      className={styles.settingsSaveBtn}
                      onClick={handleMarkAllNotificationsRead}
                      disabled={markingAllRead || inAppNotifications.length === 0}
                    >
                      {markingAllRead ? 'Marking...' : 'Mark all as read'}
                    </button>
                  </div>

                  {inAppNotificationsLoading && <p className={styles.queueEmpty}>Loading notifications...</p>}
                  {!inAppNotificationsLoading && inAppNotificationsError && (
                    <p className={styles.queueError}>{inAppNotificationsError}</p>
                  )}
                  {!inAppNotificationsLoading && !inAppNotificationsError && inAppNotifications.length === 0 && (
                    <p className={styles.queueEmpty}>No notifications yet.</p>
                  )}

                  {!inAppNotificationsLoading && inAppNotifications.length > 0 && (
                    <ul className={styles.notificationsList}>
                      {inAppNotifications.map(notification => (
                        <li
                          key={notification.notificationId}
                          className={`${styles.notificationCard} ${notification.isRead ? styles.notificationRead : styles.notificationUnread}`}
                        >
                          <div className={styles.notificationBody}>
                            <p className={styles.notificationTitle}>{notification.title}</p>
                            <p className={styles.notificationMessage}>{notification.message}</p>
                            <span className={styles.notificationTime}>{formatRelativeTime(notification.createdAt)}</span>
                          </div>

                          {!notification.isRead && (
                            <button
                              type="button"
                              className={styles.notificationReadBtn}
                              onClick={() => handleMarkNotificationRead(notification.notificationId)}
                              disabled={markingSingleReadId === notification.notificationId}
                            >
                              {markingSingleReadId === notification.notificationId ? 'Saving...' : 'Mark read'}
                            </button>
                          )}
                        </li>
                      ))}
                    </ul>
                  )}
                </section>

                <section className={styles.settingsSection} aria-label="Queue notification settings">
                  <div className={styles.sectionHeader}>
                    <BellIcon />
                    <h2 className={styles.sectionTitle}>Queue Notification Settings</h2>
                  </div>

                  {notificationsLoading && <p className={styles.queueEmpty}>Loading settings...</p>}

                  {!notificationsLoading && (
                    <>
                      <label className={styles.settingsToggle}>
                        <input
                          type="checkbox"
                          checked={queueNotificationsEnabled}
                          onChange={e => {
                            setQueueNotificationsEnabled(e.target.checked)
                            setNotificationsMessage(null)
                            setNotificationsError(null)
                          }}
                        />
                        <span>Notify me by email when my queue turn is approaching.</span>
                      </label>

                      <button
                        type="button"
                        className={styles.settingsSaveBtn}
                        onClick={handleSaveNotificationPreference}
                        disabled={notificationsSaving}
                      >
                        {notificationsSaving ? 'Saving...' : 'Save preference'}
                      </button>

                      {notificationsMessage && <p className={styles.queueSuccess}>{notificationsMessage}</p>}
                      {notificationsError && <p className={styles.queueError}>{notificationsError}</p>}
                    </>
                  )}
                </section>

                <section className={styles.settingsSection} aria-label="Appointment notification settings">
                  <div className={styles.sectionHeader}>
                    <CalendarIcon />
                    <h2 className={styles.sectionTitle}>Appointment Notification Settings</h2>
                  </div>

                  {appointmentNotificationsLoading && <p className={styles.queueEmpty}>Loading settings...</p>}

                  {!appointmentNotificationsLoading && (
                    <>
                      <label className={styles.settingsToggle}>
                        <input
                          type="checkbox"
                          checked={appointmentBookedNotificationsEnabled}
                          onChange={e => {
                            setAppointmentBookedNotificationsEnabled(e.target.checked)
                            setAppointmentNotificationsMessage(null)
                            setAppointmentNotificationsError(null)
                          }}
                        />
                        <span>Email me booking confirmations.</span>
                      </label>

                      <label className={styles.settingsToggle}>
                        <input
                          type="checkbox"
                          checked={appointmentRescheduledNotificationsEnabled}
                          onChange={e => {
                            setAppointmentRescheduledNotificationsEnabled(e.target.checked)
                            setAppointmentNotificationsMessage(null)
                            setAppointmentNotificationsError(null)
                          }}
                        />
                        <span>Email me when appointments are rescheduled.</span>
                      </label>

                      <label className={styles.settingsToggle}>
                        <input
                          type="checkbox"
                          checked={appointmentCancelledNotificationsEnabled}
                          onChange={e => {
                            setAppointmentCancelledNotificationsEnabled(e.target.checked)
                            setAppointmentNotificationsMessage(null)
                            setAppointmentNotificationsError(null)
                          }}
                        />
                        <span>Email me when appointments are cancelled.</span>
                      </label>

                      <button
                        type="button"
                        className={styles.settingsSaveBtn}
                        onClick={handleSaveAppointmentNotificationPreferences}
                        disabled={appointmentNotificationsSaving}
                      >
                        {appointmentNotificationsSaving ? 'Saving...' : 'Save preferences'}
                      </button>

                      {appointmentNotificationsMessage && <p className={styles.queueSuccess}>{appointmentNotificationsMessage}</p>}
                      {appointmentNotificationsError && <p className={styles.queueError}>{appointmentNotificationsError}</p>}
                    </>
                  )}
                </section>
                </>
              )}
            </div>

            <div className={styles.authCard} role="note">
              <CheckCircleIcon />
              <div>
                <p className={styles.authCardTitle}>You're signed in securely</p>
                <p className={styles.authCardBody}>Role: <strong>{role}</strong> - Use the left sidebar to switch between features.</p>
              </div>
            </div>
          </div>
        </div>
      </main>
    </div>
  )
}

function isTypingTarget(target: EventTarget | null): boolean {
  if (!(target instanceof HTMLElement)) {
    return false
  }

  const tagName = target.tagName.toLowerCase()
  if (tagName === 'input' || tagName === 'textarea' || tagName === 'select') {
    return true
  }

  return target.isContentEditable
}

function keyToDashboardTab(key: string): DashboardTab | null {
  if (key === '1' || key === 'h') return 'home'
  if (key === '2' || key === 'q') return 'queues'
  if (key === '3' || key === 'a') return 'appointments'
  if (key === '4' || key === 'n') return 'notifications'
  return null
}

function formatDashboardSlot(slotStart: string, slotEnd: string): string {
  const start = new Date(slotStart)
  const end = new Date(slotEnd)

  const date = start.toLocaleDateString([], { month: 'short', day: 'numeric', year: 'numeric' })
  const from = start.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })
  const to = end.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })

  return `${date} - ${from} to ${to}`
}

function formatRelativeTime(input: string): string {
  const value = new Date(input).getTime()
  const now = Date.now()
  const diffMs = now - value

  const minutes = Math.floor(diffMs / 60000)
  if (minutes < 1) return 'Just now'
  if (minutes < 60) return `${minutes}m ago`

  const hours = Math.floor(minutes / 60)
  if (hours < 24) return `${hours}h ago`

  const days = Math.floor(hours / 24)
  return `${days}d ago`
}

function QueueStatusBadge({ status }: { status: string }) {
  const cls =
    status === 'Active'
      ? styles.queueStatusActive
      : status === 'Paused'
        ? styles.queueStatusPaused
        : styles.queueStatusClosed
  return <span className={`${styles.queueStatusBadge} ${cls}`}>{status}</span>
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

function HomeIcon() {
  return (
    <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
      <path d="M3 10.5 12 3l9 7.5" />
      <path d="M5 9.5V21h14V9.5" />
      <path d="M10 21v-6h4v6" />
    </svg>
  )
}

function QueueIcon() {
  return (
    <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
      <line x1="8" y1="6" x2="21" y2="6" />
      <line x1="8" y1="12" x2="21" y2="12" />
      <line x1="8" y1="18" x2="21" y2="18" />
      <line x1="3" y1="6" x2="3.01" y2="6" />
      <line x1="3" y1="12" x2="3.01" y2="12" />
      <line x1="3" y1="18" x2="3.01" y2="18" />
    </svg>
  )
}

function LinkIcon() {
  return (
    <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
      <path d="M10 13a5 5 0 007.54.54l3-3a5 5 0 00-7.07-7.07l-1.72 1.71" />
      <path d="M14 11a5 5 0 00-7.54-.54l-3 3a5 5 0 007.07 7.07l1.71-1.71" />
    </svg>
  )
}

function CalendarIcon() {
  return (
    <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
      <rect x="3" y="4" width="18" height="18" rx="2" ry="2" />
      <line x1="16" y1="2" x2="16" y2="6" />
      <line x1="8" y1="2" x2="8" y2="6" />
      <line x1="3" y1="10" x2="21" y2="10" />
    </svg>
  )
}

function CheckCircleIcon() {
  return (
    <svg
      width="20"
      height="20"
      viewBox="0 0 24 24"
      fill="none"
      stroke="currentColor"
      strokeWidth="2"
      strokeLinecap="round"
      strokeLinejoin="round"
      style={{ flexShrink: 0, color: 'var(--color-primary)' }}
    >
      <path d="M22 11.08V12a10 10 0 11-5.93-9.14" />
      <polyline points="22 4 12 14.01 9 11.01" />
    </svg>
  )
}

function BellIcon() {
  return (
    <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
      <path d="M18 8a6 6 0 00-12 0c0 7-3 9-3 9h18s-3-2-3-9" />
      <path d="M13.73 21a2 2 0 01-3.46 0" />
    </svg>
  )
}
