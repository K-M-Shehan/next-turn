/**
 * QueuePage — allows an authenticated user to join a queue.
 *
 * Route: /queues/:tenantId/:queueId  (wrapped by ProtectedRoute)
 *
 * States:
 *  idle       — shows a "Join Queue" button
 *  joining    — button disabled, spinner shown
 *  joined     — success card: ticket number, position, ETA
 *  alreadyIn  — 409 without canBookAppointment: user is already in this queue
 *  full       — 409 with canBookAppointment: queue is at capacity
 *  error      — any other error
 */
import { useState } from 'react'
import { useParams } from 'react-router-dom'
import { joinQueue, type JoinQueueResult } from '../../api/queues'
import type { ApiError } from '../../types/api'
import styles from './QueuePage.module.css'

type PageState =
  | { status: 'idle' }
  | { status: 'joining' }
  | { status: 'joined'; result: JoinQueueResult }
  | { status: 'alreadyIn' }
  | { status: 'full' }
  | { status: 'error'; detail: string }

function formatEta(seconds: number): string {
  if (seconds < 60) return `${seconds}s`
  const mins = Math.round(seconds / 60)
  return mins === 1 ? '1 min' : `${mins} mins`
}

export function QueuePage() {
  const { tenantId, queueId } = useParams<{ tenantId: string; queueId: string }>()
  const [state, setState] = useState<PageState>({ status: 'idle' })

  async function handleJoin() {
    if (!tenantId || !queueId) return
    setState({ status: 'joining' })

    try {
      const result = await joinQueue(queueId, tenantId)
      setState({ status: 'joined', result })
    } catch (err) {
      const apiErr = err as ApiError
      if (apiErr.status === 409) {
        // Distinguish "already in queue" (no canBookAppointment) from "queue full"
        const raw = apiErr.raw as Record<string, unknown>
        if (raw.canBookAppointment === true) {
          setState({ status: 'full' })
        } else {
          setState({ status: 'alreadyIn' })
        }
      } else {
        setState({ status: 'error', detail: apiErr.detail ?? 'Something went wrong.' })
      }
    }
  }

  return (
    <div className={styles.page}>
      <div className={styles.card}>
        <h1 className={styles.heading}>Join Queue</h1>

        {/* ── Idle ── */}
        {state.status === 'idle' && (
          <>
            <p className={styles.body}>Press the button below to take your place in the queue.</p>
            <button className={styles.joinBtn} onClick={handleJoin} type="button">
              Join Queue
            </button>
          </>
        )}

        {/* ── Joining (loading) ── */}
        {state.status === 'joining' && (
          <div className={styles.loadingRow}>
            <span className={styles.spinner} aria-hidden="true" />
            <span>Joining queue…</span>
          </div>
        )}

        {/* ── Joined (success) ── */}
        {state.status === 'joined' && (
          <div className={styles.successBlock} data-testid="success-block">
            <div className={styles.ticketBadge}>
              <span className={styles.ticketLabel}>Your ticket</span>
              <span className={styles.ticketNumber}>#{state.result.ticketNumber}</span>
            </div>
            <dl className={styles.statsList}>
              <div className={styles.stat}>
                <dt>Position in queue</dt>
                <dd>{state.result.positionInQueue}</dd>
              </div>
              <div className={styles.stat}>
                <dt>Estimated wait</dt>
                <dd>{formatEta(state.result.estimatedWaitSeconds)}</dd>
              </div>
            </dl>
            <p className={styles.successNote}>
              Please stay nearby. You&apos;ll be called when it&apos;s your turn.
            </p>
          </div>
        )}

        {/* ── Already in queue ── */}
        {state.status === 'alreadyIn' && (
          <div className={styles.infoBlock} data-testid="already-in-block">
            <p className={styles.infoText}>You already have an active ticket for this queue.</p>
            <button
              className={`${styles.joinBtn} ${styles.joinBtnSecondary}`}
              onClick={() => setState({ status: 'idle' })}
              type="button"
            >
              Back
            </button>
          </div>
        )}

        {/* ── Queue full ── */}
        {state.status === 'full' && (
          <div className={styles.fullBlock} data-testid="queue-full-block">
            <p className={styles.fullText}>
              This queue is currently full.
            </p>
            <p className={styles.body}>
              You can book an appointment instead to guarantee a time slot.
            </p>
            <button className={styles.joinBtn} type="button" disabled>
              Queue is Full
            </button>
            <button
              className={`${styles.joinBtn} ${styles.joinBtnAppointment}`}
              type="button"
              data-testid="book-appointment-btn"
            >
              Book an Appointment
            </button>
          </div>
        )}

        {/* ── Generic error ── */}
        {state.status === 'error' && (
          <div className={styles.errorBlock} data-testid="error-block">
            <p className={styles.errorText}>{state.detail}</p>
            <button
              className={`${styles.joinBtn} ${styles.joinBtnSecondary}`}
              onClick={() => setState({ status: 'idle' })}
              type="button"
            >
              Try Again
            </button>
          </div>
        )}
      </div>
    </div>
  )
}
