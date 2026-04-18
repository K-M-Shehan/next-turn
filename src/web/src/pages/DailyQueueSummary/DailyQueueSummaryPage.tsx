import { useMemo, useState } from 'react'
import { useNavigate, useParams } from 'react-router-dom'
import { getDailyQueueSummaryReport, type DailyQueueMetricTrend, type DailyQueueSummaryReportResult } from '../../api/queues'
import type { ApiError } from '../../types/api'
import { clearToken, getTokenPayload } from '../../utils/authToken'
import logoImg from '../../assets/nextTurn-logo.png'
import styles from './DailyQueueSummaryPage.module.css'

function formatDate(value: Date): string {
  const year = value.getFullYear()
  const month = `${value.getMonth() + 1}`.padStart(2, '0')
  const day = `${value.getDate()}`.padStart(2, '0')
  return `${year}-${month}-${day}`
}

function trendClass(value: number): string {
  if (value > 0) return styles.trendUp
  if (value < 0) return styles.trendDown
  return styles.trendNeutral
}

function renderTrend(trend: DailyQueueMetricTrend): string {
  const day = trend.deltaFromPreviousDay
  const week = trend.deltaFromPreviousWeek
  const dayLabel = day > 0 ? `+${day}` : `${day}`
  const weekLabel = week > 0 ? `+${week}` : `${week}`
  return `D ${dayLabel} | W ${weekLabel}`
}

export function DailyQueueSummaryPage() {
  const navigate = useNavigate()
  const { tenantId } = useParams<{ tenantId: string }>()
  const payload = getTokenPayload()

  const [date, setDate] = useState(() => formatDate(new Date()))
  const [report, setReport] = useState<DailyQueueSummaryReportResult | null>(null)
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState<string | null>(null)

  if (!payload) {
    clearToken()
    navigate('/', { replace: true })
    return null
  }

  const hasRows = useMemo(() => (report?.rows.length ?? 0) > 0, [report])

  async function loadSummary() {
    if (!tenantId) return

    setLoading(true)
    setError(null)

    try {
      const result = await getDailyQueueSummaryReport(tenantId, date)
      setReport(result)
    } catch (err) {
      const apiErr = err as ApiError
      setError(apiErr.detail ?? 'Could not load daily summary report.')
      setReport(null)
    } finally {
      setLoading(false)
    }
  }

  return (
    <div className={styles.page}>
      <nav className={styles.topNav}>
        <img src={logoImg} alt="NextTurn" className={styles.logo} />
        <div className={styles.navActions}>
          <button type="button" className={styles.secondaryBtn} onClick={() => navigate(`/admin/${tenantId}/reports`)}>
            Queue Performance
          </button>
          <button type="button" className={styles.secondaryBtn} onClick={() => navigate(`/admin/${tenantId}`)}>
            Back to Admin
          </button>
        </div>
      </nav>

      <main className={styles.main}>
        <section className={styles.header}>
          <h1 className={styles.title}>Daily Queue Summary</h1>
          <p className={styles.subtitle}>Served, skipped, and no-show activity per office and service with day/week trend context.</p>
        </section>

        <section className={styles.controls}>
          <label className={styles.field}>
            <span>Summary date</span>
            <input className={styles.input} type="date" value={date} onChange={e => setDate(e.target.value)} />
          </label>
          <button type="button" className={styles.primaryBtn} onClick={loadSummary} disabled={loading}>
            {loading ? 'Generating...' : 'Generate summary'}
          </button>
        </section>

        {error && <div className={styles.errorBanner}>{error}</div>}

        {report && (
          <section className={styles.results}>
            <div className={styles.summaryCards}>
              <article className={styles.card}>
                <span className={styles.cardLabel}>Total Served</span>
                <strong className={styles.cardValue}>{report.totalServed}</strong>
                <span className={`${styles.cardTrend} ${trendClass(report.totalServedTrend.deltaFromPreviousDay)}`}>
                  {renderTrend(report.totalServedTrend)}
                </span>
              </article>
              <article className={styles.card}>
                <span className={styles.cardLabel}>Total Skipped</span>
                <strong className={styles.cardValue}>{report.totalSkipped}</strong>
                <span className={`${styles.cardTrend} ${trendClass(report.totalSkippedTrend.deltaFromPreviousDay)}`}>
                  {renderTrend(report.totalSkippedTrend)}
                </span>
              </article>
              <article className={styles.card}>
                <span className={styles.cardLabel}>Total No-Shows</span>
                <strong className={styles.cardValue}>{report.totalNoShows}</strong>
                <span className={`${styles.cardTrend} ${trendClass(report.totalNoShowsTrend.deltaFromPreviousDay)}`}>
                  {renderTrend(report.totalNoShowsTrend)}
                </span>
              </article>
            </div>

            {!hasRows && <p className={styles.emptyText}>No queue activity found for this date.</p>}

            {hasRows && (
              <div className={styles.tableWrap}>
                <table className={styles.table}>
                  <thead>
                    <tr>
                      <th>Office</th>
                      <th>Service</th>
                      <th>Served</th>
                      <th>Skipped</th>
                      <th>No-Shows</th>
                    </tr>
                  </thead>
                  <tbody>
                    {report.rows.map(row => (
                      <tr key={`${row.officeId}:${row.serviceId}`}>
                        <td>{row.officeName}</td>
                        <td>{row.serviceName}</td>
                        <td>
                          <div className={styles.metricCell}>
                            <strong>{row.served}</strong>
                            <span className={trendClass(row.servedTrend.deltaFromPreviousDay)}>{renderTrend(row.servedTrend)}</span>
                          </div>
                        </td>
                        <td>
                          <div className={styles.metricCell}>
                            <strong>{row.skipped}</strong>
                            <span className={trendClass(row.skippedTrend.deltaFromPreviousDay)}>{renderTrend(row.skippedTrend)}</span>
                          </div>
                        </td>
                        <td>
                          <div className={styles.metricCell}>
                            <strong>{row.noShows}</strong>
                            <span className={trendClass(row.noShowsTrend.deltaFromPreviousDay)}>{renderTrend(row.noShowsTrend)}</span>
                          </div>
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            )}
          </section>
        )}
      </main>
    </div>
  )
}
