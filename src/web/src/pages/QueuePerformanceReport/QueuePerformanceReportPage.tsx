import { useEffect, useMemo, useState } from 'react'
import { useNavigate, useParams } from 'react-router-dom'
import {
  exportQueuePerformanceReportCsv,
  getQueuePerformanceReport,
  type QueuePerformanceReportResult,
} from '../../api/queues'
import { listOffices, type OfficeDto } from '../../api/offices'
import { listServices, type ServiceDto } from '../../api/services'
import type { ApiError } from '../../types/api'
import { clearToken, getTokenPayload } from '../../utils/authToken'
import logoImg from '../../assets/nextTurn-logo.png'
import styles from './QueuePerformanceReportPage.module.css'

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

export function QueuePerformanceReportPage() {
  const navigate = useNavigate()
  const { tenantId } = useParams<{ tenantId: string }>()
  const payload = getTokenPayload()

  const [startDate, setStartDate] = useState(defaultStartDate)
  const [endDate, setEndDate] = useState(defaultEndDate)
  const [officeId, setOfficeId] = useState('')
  const [serviceId, setServiceId] = useState('')

  const [report, setReport] = useState<QueuePerformanceReportResult | null>(null)
  const [offices, setOffices] = useState<OfficeDto[]>([])
  const [services, setServices] = useState<ServiceDto[]>([])

  const [loading, setLoading] = useState(false)
  const [exporting, setExporting] = useState(false)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    if (!tenantId) return

    listOffices(tenantId, { isActive: true, pageSize: 200 })
      .then(result => setOffices(result.items))
      .catch(() => setOffices([]))

    listServices(tenantId, { activeOnly: true, pageSize: 200 })
      .then(result => setServices(result.items))
      .catch(() => setServices([]))
  }, [tenantId])

  if (!payload) {
    clearToken()
    navigate('/', { replace: true })
    return null
  }

  const selectedServiceName = useMemo(
    () => services.find(s => s.serviceId === serviceId)?.name ?? 'All services',
    [serviceId, services],
  )

  const selectedOfficeName = useMemo(
    () => offices.find(o => o.officeId === officeId)?.name ?? 'All offices',
    [officeId, offices],
  )

  async function loadReport() {
    if (!tenantId) return

    setLoading(true)
    setError(null)

    try {
      const response = await getQueuePerformanceReport(tenantId, {
        startDate,
        endDate,
        serviceId: serviceId || undefined,
        officeId: officeId || undefined,
      })
      setReport(response)
    } catch (err) {
      const apiErr = err as ApiError
      setError(apiErr.detail ?? 'Could not load queue performance report.')
      setReport(null)
    } finally {
      setLoading(false)
    }
  }

  async function handleExportCsv() {
    if (!tenantId) return

    setExporting(true)
    setError(null)

    try {
      const blob = await exportQueuePerformanceReportCsv(tenantId, {
        startDate,
        endDate,
        serviceId: serviceId || undefined,
        officeId: officeId || undefined,
      })

      const fileName = `queue-performance-${startDate}-${endDate}.csv`
      const url = window.URL.createObjectURL(blob)
      const link = document.createElement('a')
      link.href = url
      link.download = fileName
      document.body.appendChild(link)
      link.click()
      link.remove()
      window.URL.revokeObjectURL(url)
    } catch (err) {
      const apiErr = err as ApiError
      setError(apiErr.detail ?? 'Could not export queue performance report.')
    } finally {
      setExporting(false)
    }
  }

  return (
    <div className={styles.page}>
      <nav className={styles.topNav}>
        <img src={logoImg} alt="NextTurn" className={styles.logo} />
        <button
          type="button"
          className={styles.backBtn}
          onClick={() => navigate(`/admin/${tenantId}`)}
        >
          Back to Admin
        </button>
      </nav>

      <main className={styles.main}>
        <section className={styles.header}>
          <h1 className={styles.title}>Queue Performance Reports</h1>
          <p className={styles.subtitle}>Track wait times and peak demand windows across your organisation.</p>
        </section>

        <section className={styles.filtersCard}>
          <div className={styles.filtersGrid}>
            <label className={styles.field}>
              <span>Start date</span>
              <input
                type="date"
                value={startDate}
                onChange={e => setStartDate(e.target.value)}
                className={styles.input}
              />
            </label>

            <label className={styles.field}>
              <span>End date</span>
              <input
                type="date"
                value={endDate}
                onChange={e => setEndDate(e.target.value)}
                className={styles.input}
              />
            </label>

            <label className={styles.field}>
              <span>Service</span>
              <select
                value={serviceId}
                onChange={e => setServiceId(e.target.value)}
                className={styles.input}
              >
                <option value="">All services</option>
                {services.map(service => (
                  <option key={service.serviceId} value={service.serviceId}>
                    {service.name}
                  </option>
                ))}
              </select>
            </label>

            <label className={styles.field}>
              <span>Office</span>
              <select
                value={officeId}
                onChange={e => setOfficeId(e.target.value)}
                className={styles.input}
              >
                <option value="">All offices</option>
                {offices.map(office => (
                  <option key={office.officeId} value={office.officeId}>
                    {office.name}
                  </option>
                ))}
              </select>
            </label>
          </div>

          <div className={styles.actions}>
            <button
              type="button"
              className={styles.primaryBtn}
              onClick={loadReport}
              disabled={loading}
            >
              {loading ? 'Loading report...' : 'Load report'}
            </button>
            <button
              type="button"
              className={styles.secondaryBtn}
              onClick={handleExportCsv}
              disabled={exporting}
            >
              {exporting ? 'Exporting...' : 'Export CSV'}
            </button>
          </div>
        </section>

        {error && <div className={styles.errorBanner}>{error}</div>}

        {report && (
          <section className={styles.results}>
            <div className={styles.summaryGrid}>
              <article className={styles.metricCard}>
                <span className={styles.metricLabel}>Total Served</span>
                <strong className={styles.metricValue}>{report.totalServed}</strong>
              </article>
              <article className={styles.metricCard}>
                <span className={styles.metricLabel}>Average Wait</span>
                <strong className={styles.metricValue}>{report.averageWaitMinutes.toFixed(2)} min</strong>
              </article>
              <article className={styles.metricCard}>
                <span className={styles.metricLabel}>Service Filter</span>
                <strong className={styles.metricValue}>{selectedServiceName}</strong>
              </article>
              <article className={styles.metricCard}>
                <span className={styles.metricLabel}>Office Filter</span>
                <strong className={styles.metricValue}>{selectedOfficeName}</strong>
              </article>
            </div>

            <div className={styles.peakCard}>
              <h2 className={styles.sectionTitle}>Peak Hours</h2>
              {report.peakHours.length === 0 && (
                <p className={styles.emptyText}>No served entries in the selected range.</p>
              )}

              {report.peakHours.length > 0 && (
                <ul className={styles.peakList}>
                  {report.peakHours.map(item => (
                    <li key={item.hourOfDay} className={styles.peakRow}>
                      <span>{`${String(item.hourOfDay).padStart(2, '0')}:00 - ${String(item.hourOfDay).padStart(2, '0')}:59`}</span>
                      <strong>{item.servedCount} served</strong>
                    </li>
                  ))}
                </ul>
              )}
            </div>
          </section>
        )}
      </main>
    </div>
  )
}
