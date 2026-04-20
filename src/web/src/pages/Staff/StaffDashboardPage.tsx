import { useCallback, useEffect, useState } from 'react'
import { useNavigate, useParams } from 'react-router-dom'
import {
  getStaffQueues,
  getQueueDashboard,
  serveNext,
  skipQueueEntry,
  type OrgQueueSummary,
  type QueueDashboardResult,
} from '../../api/queues'
import type { ApiError } from '../../types/api'
import { OnboardingTour } from '../../components/OnboardingTour'
import { ROLE_TOUR_CONTENT } from '../../onboarding/roleTours'
import { useOnboardingTour } from '../../onboarding/useOnboardingTour'
import { clearToken, getTokenPayload } from '../../utils/authToken'
import logoImg from '../../assets/nextTurn-logo.png'
import styles from './StaffDashboardPage.module.css'

export function StaffDashboardPage() {
  const navigate = useNavigate()
  const { tenantId } = useParams<{ tenantId: string }>()
  const payload = getTokenPayload()

  const [queues, setQueues] = useState<OrgQueueSummary[]>([])
  const [selectedQueueId, setSelectedQueueId] = useState<string | null>(null)
  const [dashboard, setDashboard] = useState<QueueDashboardResult | null>(null)

  const [loadingQueues, setLoadingQueues] = useState(true)
  const [loadingDashboard, setLoadingDashboard] = useState(false)
  const [actionBusy, setActionBusy] = useState<'serve-next' | 'skip' | null>(null)
  const [error, setError] = useState<string | null>(null)
  const [lastUpdated, setLastUpdated] = useState<Date | null>(null)
  const onboarding = useOnboardingTour(`staff:${payload.sub}:${tenantId ?? 'global'}`)

  if (!payload) {
    clearToken()
    navigate('/', { replace: true })
    return null
  }

  const loadDashboard = useCallback(async (queueId: string, showLoading: boolean) => {
    if (!tenantId) return

    if (showLoading) {
      setLoadingDashboard(true)
    }

    try {
      const result = await getQueueDashboard(queueId, tenantId)
      setDashboard(result)
      setLastUpdated(new Date())
      setError(null)
    } catch (err) {
      const apiErr = err as ApiError
      setError(apiErr.detail ?? 'Failed to load queue dashboard.')
    } finally {
      if (showLoading) {
        setLoadingDashboard(false)
      }
    }
  }, [tenantId])

  useEffect(() => {
    if (!tenantId) return

    setLoadingQueues(true)
    setError(null)

    getStaffQueues(tenantId)
      .then((result) => {
        setQueues(result)

        if (result.length === 0) {
          setSelectedQueueId(null)
          setDashboard(null)
          return
        }

        setSelectedQueueId((prev) => {
          if (prev && result.some(q => q.queueId === prev)) {
            return prev
          }
          return result[0].queueId
        })
      })
      .catch((err: ApiError) => {
        setError(err.detail ?? 'Failed to load assigned queues.')
      })
      .finally(() => {
        setLoadingQueues(false)
      })
  }, [tenantId])

  useEffect(() => {
    if (!selectedQueueId) return
    void loadDashboard(selectedQueueId, true)
  }, [selectedQueueId, loadDashboard])

  useEffect(() => {
    if (!selectedQueueId) return

    const id = setInterval(() => {
      void loadDashboard(selectedQueueId, false)
    }, 30_000)

    return () => clearInterval(id)
  }, [selectedQueueId, loadDashboard])

  async function handleServeNext() {
    if (!tenantId || !selectedQueueId) return

    setActionBusy('serve-next')
    setError(null)

    try {
      await serveNext(selectedQueueId, tenantId)
      await loadDashboard(selectedQueueId, false)
    } catch (err) {
      const apiErr = err as ApiError
      setError(apiErr.detail ?? 'Could not serve the next ticket.')
    } finally {
      setActionBusy(null)
    }
  }

  async function handleSkipNext() {
    if (!tenantId || !selectedQueueId) return

    const rawReason = window.prompt('Optional skip reason (max 200 chars):', '')
    if (rawReason === null) {
      return
    }

    setActionBusy('skip')
    setError(null)

    try {
      await skipQueueEntry(selectedQueueId, tenantId, { reason: rawReason.trim() || undefined })
      await loadDashboard(selectedQueueId, false)
    } catch (err) {
      const apiErr = err as ApiError
      setError(apiErr.detail ?? 'Could not skip the next ticket.')
    } finally {
      setActionBusy(null)
    }
  }

  function handleLogout() {
    clearToken()
    navigate('/', { replace: true })
  }

  const hasServingEntry = Boolean(dashboard?.currentlyServing)
  const hasActionableEntry = Boolean(dashboard && (dashboard.waitingCount > 0 || hasServingEntry))

  return (
    <div className={styles.page}>
      <header className={styles.navbar}>
        <div className={styles.navInner}>
          <div className={styles.brandWrap}>
            <img src={logoImg} alt="NextTurn" className={styles.logo} />
            <div className={styles.brandMeta}>
              <h1 className={styles.title}>Staff Queue Dashboard</h1>
              <p className={styles.subtitle}>Live queue control and ticket flow</p>
            </div>
          </div>

          <div className={styles.navActions}>
            <span className={styles.userChip}>{payload.name}</span>
            <button className={styles.logoutBtn} onClick={handleLogout} type="button">
              Sign out
            </button>
          </div>
        </div>
      </header>

      <main className={styles.main}>
        <div className={styles.content}>
          {error && (
            <div className={styles.errorBanner} role="alert" data-testid="staff-error">
              {error}
            </div>
          )}

          <section className={styles.selectorCard}>
            <label htmlFor="queue-selector" className={styles.selectorLabel}>Queue</label>
            <select
              id="queue-selector"
              data-testid="queue-selector"
              data-onboarding="staff-queue-selector"
              className={styles.selector}
              value={selectedQueueId ?? ''}
              onChange={(e) => setSelectedQueueId(e.target.value || null)}
              disabled={loadingQueues || queues.length === 0}
            >
              {queues.length === 0 && <option value="">No queues available</option>}
              {queues.map((queue) => (
                <option key={queue.queueId} value={queue.queueId}>
                  {queue.name} ({queue.status})
                </option>
              ))}
            </select>
            {lastUpdated && (
              <p className={styles.updatedAt}>
                Updated {lastUpdated.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })} · refreshes every 30s
              </p>
            )}
          </section>

          <section className={styles.settingsCard} data-onboarding="staff-settings">
            <h2 className={styles.panelTitle}>Profile and Settings</h2>
            <p className={styles.muted}>Replay onboarding whenever you want a quick refresher.</p>
            <button
              type="button"
              className={styles.ghostBtn}
              onClick={onboarding.restartTour}
            >
              Restart onboarding tour
            </button>
          </section>

          {!loadingQueues && queues.length === 0 && (
            <section className={styles.emptyCard} data-testid="staff-empty">
              <h2>No queues yet</h2>
              <p>Ask your organisation admin to create a queue before serving tickets.</p>
            </section>
          )}

          {selectedQueueId && loadingDashboard && (
            <section className={styles.loadingCard}>
              <span className={styles.spinner} aria-hidden="true" />
              <p>Loading queue activity…</p>
            </section>
          )}

          {dashboard && !loadingDashboard && (
            <>
              <section className={styles.heroCard}>
                <div>
                  <p className={styles.heroLabel}>Current queue</p>
                  <h2 className={styles.heroTitle}>{dashboard.queueName}</h2>
                  <p className={styles.heroMeta}>Status: {dashboard.queueStatus} · Waiting: {dashboard.waitingCount}</p>
                </div>

                <div className={styles.controls} data-onboarding="staff-actions">
                  <button
                    type="button"
                    className={styles.primaryBtn}
                    data-testid="serve-next-btn"
                    onClick={handleServeNext}
                    disabled={!hasActionableEntry || actionBusy !== null}
                  >
                    {actionBusy === 'serve-next' ? 'Saving…' : 'Serve Next'}
                  </button>
                  <button
                    type="button"
                    className={styles.secondaryBtn}
                    data-testid="skip-next-btn"
                    onClick={handleSkipNext}
                    disabled={!hasActionableEntry || actionBusy !== null}
                  >
                    {actionBusy === 'skip' ? 'Saving…' : 'Skip Next'}
                  </button>
                </div>
              </section>

              <section className={styles.grid}>
                <article className={styles.panel}>
                  <h3 className={styles.panelTitle}>Now Serving</h3>
                  {dashboard.currentlyServing ? (
                    <div className={styles.ticketNow} data-testid="current-ticket">
                      <span className={styles.ticketBadge}>Ticket</span>
                      <strong>#{String(dashboard.currentlyServing.ticketNumber).padStart(3, '0')}</strong>
                    </div>
                  ) : (
                    <p className={styles.muted}>No ticket is currently being served.</p>
                  )}
                </article>

                <article className={styles.panel}>
                  <h3 className={styles.panelTitle}>Waiting Line</h3>
                  {dashboard.waitingEntries.length === 0 ? (
                    <p className={styles.muted}>Nobody is waiting.</p>
                  ) : (
                    <ol className={styles.waitingList} data-testid="waiting-list" data-onboarding="staff-waiting-list">
                      {dashboard.waitingEntries.map(entry => (
                        <li key={entry.entryId} className={styles.waitingItem}>
                          <span className={styles.waitingTicket}>#{String(entry.ticketNumber).padStart(3, '0')}</span>
                          <span className={styles.waitingMeta}>
                            Joined {new Date(entry.joinedAt).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })}
                          </span>
                        </li>
                      ))}
                    </ol>
                  )}
                </article>
              </section>
            </>
          )}
        </div>

        <OnboardingTour
          isOpen={onboarding.isOpen}
          title="Quick tour: staff dashboard"
          steps={ROLE_TOUR_CONTENT.staff}
          onClose={onboarding.completeTour}
        />
      </main>
    </div>
  )
}
