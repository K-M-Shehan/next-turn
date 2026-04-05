import { useEffect, useMemo, useState } from 'react'
import { useNavigate, useParams } from 'react-router-dom'
import {
  assignServiceOffices,
  createService,
  deactivateService,
  listServices,
  removeServiceOfficeAssignment,
  updateService,
  type ServiceDto,
} from '../../api/services'
import { listOffices, type OfficeDto } from '../../api/offices'
import { createQueue } from '../../api/queues'
import {
  configureAppointmentSchedule,
  createAppointmentProfile,
  getAppointmentSchedule,
} from '../../api/appointments'
import type { ApiError } from '../../types/api'
import { clearToken, getTokenPayload } from '../../utils/authToken'
import styles from './ServiceManagementPage.module.css'

type Mode = 'create' | 'edit'

interface ServiceForm {
  name: string
  code: string
  description: string
  estimatedDurationMinutes: string
  isActive: boolean
}

const defaultForm: ServiceForm = {
  name: '',
  code: '',
  description: '',
  estimatedDurationMinutes: '15',
  isActive: true,
}

interface ServiceManagementPageProps {
  embedded?: boolean
}

export function ServiceManagementPage({ embedded = false }: ServiceManagementPageProps = {}) {
  const navigate = useNavigate()
  const { tenantId } = useParams<{ tenantId: string }>()
  const payload = getTokenPayload()

  const [services, setServices] = useState<ServiceDto[]>([])
  const [offices, setOffices] = useState<OfficeDto[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [success, setSuccess] = useState<string | null>(null)
  const [form, setForm] = useState<ServiceForm>(defaultForm)
  const [mode, setMode] = useState<Mode>('create')
  const [editServiceId, setEditServiceId] = useState<string | null>(null)
  const [saving, setSaving] = useState(false)
  const [activeOnly, setActiveOnly] = useState(true)

  const [assignmentServiceId, setAssignmentServiceId] = useState<string>('')
  const [selectedOfficeIds, setSelectedOfficeIds] = useState<string[]>([])
  const [savingAssignments, setSavingAssignments] = useState(false)
  const [creatingFromService, setCreatingFromService] = useState<'queue' | 'appointment' | null>(null)
  const [latestQueueLink, setLatestQueueLink] = useState<string | null>(null)
  const [latestAppointmentLink, setLatestAppointmentLink] = useState<string | null>(null)

  const selectedService = useMemo(
    () => services.find(s => s.serviceId === assignmentServiceId) ?? null,
    [services, assignmentServiceId],
  )

  useEffect(() => {
    if (!payload) {
      clearToken()
      navigate('/', { replace: true })
      return
    }

    if (!tenantId) return

    void loadData(tenantId, activeOnly)
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [tenantId, activeOnly])

  useEffect(() => {
    if (!selectedService) {
      setSelectedOfficeIds([])
      return
    }

    setSelectedOfficeIds(selectedService.assignedOfficeIds)
  }, [selectedService])

  async function loadData(currentTenantId: string, activeOnlyFilter: boolean) {
    setLoading(true)
    setError(null)

    try {
      const [serviceResult, officeResult] = await Promise.all([
        listServices(currentTenantId, { activeOnly: activeOnlyFilter, pageNumber: 1, pageSize: 100 }),
        listOffices(currentTenantId, { isActive: true, pageNumber: 1, pageSize: 100 }),
      ])

      setServices(serviceResult.items)
      setOffices(officeResult.items)
      setAssignmentServiceId(prev => {
        if (prev && serviceResult.items.some(x => x.serviceId === prev)) return prev
        return serviceResult.items[0]?.serviceId ?? ''
      })
    } catch (err) {
      const apiErr = err as ApiError
      setError(apiErr.detail ?? 'Could not load services.')
    } finally {
      setLoading(false)
    }
  }

  function resetForm() {
    setMode('create')
    setEditServiceId(null)
    setForm(defaultForm)
  }

  function beginEdit(service: ServiceDto) {
    setMode('edit')
    setEditServiceId(service.serviceId)
    setForm({
      name: service.name,
      code: service.code,
      description: service.description,
      estimatedDurationMinutes: String(service.estimatedDurationMinutes),
      isActive: service.isActive,
    })
    setError(null)
    setSuccess(null)
  }

  async function handleSubmit(event: React.FormEvent) {
    event.preventDefault()
    if (!tenantId) return

    setSaving(true)
    setError(null)
    setSuccess(null)

    try {
      if (mode === 'create') {
        await createService(tenantId, {
          name: form.name.trim(),
          code: form.code.trim(),
          description: form.description.trim(),
          estimatedDurationMinutes: Number(form.estimatedDurationMinutes),
          isActive: form.isActive,
        })
        setSuccess('Service created successfully.')
      } else if (editServiceId) {
        await updateService(tenantId, editServiceId, {
          name: form.name.trim(),
          description: form.description.trim(),
          estimatedDurationMinutes: Number(form.estimatedDurationMinutes),
        })
        setSuccess('Service updated successfully.')
      }

      resetForm()
      await loadData(tenantId, activeOnly)
    } catch (err) {
      const apiErr = err as ApiError
      setError(apiErr.detail ?? 'Could not save service.')
    } finally {
      setSaving(false)
    }
  }

  async function handleDeactivate(serviceId: string) {
    if (!tenantId) return

    setError(null)
    setSuccess(null)

    try {
      await deactivateService(tenantId, serviceId)
      setSuccess('Service deactivated successfully.')
      await loadData(tenantId, activeOnly)
    } catch (err) {
      const apiErr = err as ApiError
      setError(apiErr.detail ?? 'Could not deactivate service.')
    }
  }

  function toggleOfficeSelection(officeId: string) {
    setSelectedOfficeIds(prev =>
      prev.includes(officeId)
        ? prev.filter(id => id !== officeId)
        : [...prev, officeId],
    )
  }

  async function handleSaveAssignments() {
    if (!tenantId || !selectedService) return

    setSavingAssignments(true)
    setError(null)
    setSuccess(null)

    try {
      const currentSet = new Set(selectedService.assignedOfficeIds)
      const nextSet = new Set(selectedOfficeIds)

      const toAdd = selectedOfficeIds.filter(id => !currentSet.has(id))
      const toRemove = selectedService.assignedOfficeIds.filter(id => !nextSet.has(id))

      if (toAdd.length > 0) {
        await assignServiceOffices(tenantId, selectedService.serviceId, toAdd)
      }

      for (const officeId of toRemove) {
        await removeServiceOfficeAssignment(tenantId, selectedService.serviceId, officeId)
      }

      setSuccess('Service-office assignments updated.')
      await loadData(tenantId, activeOnly)
    } catch (err) {
      const apiErr = err as ApiError
      setError(apiErr.detail ?? 'Could not update assignments.')
    } finally {
      setSavingAssignments(false)
    }
  }

  async function handleCreateQueueFromService() {
    if (!tenantId || !selectedService) return

    setCreatingFromService('queue')
    setError(null)
    setSuccess(null)
    setLatestQueueLink(null)

    try {
      const result = await createQueue(tenantId, {
        name: `${selectedService.name} Queue`,
        maxCapacity: 50,
        averageServiceTimeSeconds: Math.max(60, selectedService.estimatedDurationMinutes * 60),
      })

      setLatestQueueLink(result.shareableLink)
      setSuccess(`Queue created from service "${selectedService.name}".`)
    } catch (err) {
      const apiErr = err as ApiError
      setError(apiErr.detail ?? 'Could not create queue from this service.')
    } finally {
      setCreatingFromService(null)
    }
  }

  async function handleCreateAppointmentProfileFromService() {
    if (!tenantId || !selectedService) return

    setCreatingFromService('appointment')
    setError(null)
    setSuccess(null)
    setLatestAppointmentLink(null)

    try {
      const profile = await createAppointmentProfile(
        tenantId,
        `${selectedService.name} Appointments`,
      )

      // Apply service duration as default slot duration while preserving existing day open/close windows.
      const config = await getAppointmentSchedule(tenantId, profile.appointmentProfileId)
      const slotDurationMinutes = Math.min(240, Math.max(5, selectedService.estimatedDurationMinutes))

      await configureAppointmentSchedule(
        tenantId,
        profile.appointmentProfileId,
        config.dayRules.map(rule => ({
          ...rule,
          slotDurationMinutes,
        })),
      )

      setLatestAppointmentLink(profile.shareableLink)
      setSuccess(`Appointment profile created from service "${selectedService.name}" with ${slotDurationMinutes}-minute slots.`)
    } catch (err) {
      const apiErr = err as ApiError
      setError(apiErr.detail ?? 'Could not create appointment profile from this service.')
    } finally {
      setCreatingFromService(null)
    }
  }

  return (
    <div className={embedded ? styles.embeddedPage : styles.page}>
      <header className={styles.header}>
        <div>
          <h1 className={styles.title}>Service Catalog</h1>
          <p className={styles.subtitle}>Define what your organisation delivers, then choose where each service is available.</p>
        </div>
        {!embedded && (
          <button type="button" className={styles.backBtn} onClick={() => navigate(`/admin/${tenantId}`)}>
            Back to Admin
          </button>
        )}
      </header>

      {error && <div className={styles.error}>{error}</div>}
      {success && <div className={styles.success}>{success}</div>}

      <section className={styles.filters}>
        <label className={styles.checkboxRow}>
          <input
            type="checkbox"
            checked={activeOnly}
            onChange={(event) => setActiveOnly(event.target.checked)}
          />
          Show active services only
        </label>
        <p className={styles.helperText}>
          A service is the <strong>what</strong> (e.g., ID Renewal). Queues and appointments are the <strong>how</strong> and <strong>when</strong>.
        </p>
      </section>

      <section className={styles.formCard}>
        <h2>{mode === 'create' ? 'Create Service Definition' : 'Update Service Definition'}</h2>
        <form onSubmit={handleSubmit} className={styles.form}>
          <input
            className={styles.input}
            placeholder="Service name"
            value={form.name}
            onChange={(event) => setForm(prev => ({ ...prev, name: event.target.value }))}
            required
          />
          <input
            className={styles.input}
            placeholder="Service code"
            value={form.code}
            onChange={(event) => setForm(prev => ({ ...prev, code: event.target.value }))}
            disabled={mode === 'edit'}
            required
          />
          <textarea
            className={styles.textarea}
            placeholder="Description"
            value={form.description}
            onChange={(event) => setForm(prev => ({ ...prev, description: event.target.value }))}
            required
          />
          <input
            className={styles.input}
            type="number"
            min={1}
            max={1440}
            placeholder="Estimated duration (minutes)"
            value={form.estimatedDurationMinutes}
            onChange={(event) => setForm(prev => ({ ...prev, estimatedDurationMinutes: event.target.value }))}
            required
          />
          {mode === 'create' && (
            <label className={styles.checkboxRow}>
              <input
                type="checkbox"
                checked={form.isActive}
                onChange={(event) => setForm(prev => ({ ...prev, isActive: event.target.checked }))}
              />
              Active at creation
            </label>
          )}

          <div className={styles.actions}>
            <button type="submit" className={styles.primaryBtn} disabled={saving}>
              {saving ? 'Saving...' : mode === 'create' ? 'Create Service' : 'Update Service'}
            </button>
            {mode === 'edit' && (
              <button type="button" className={styles.secondaryBtn} onClick={resetForm}>
                Cancel Edit
              </button>
            )}
          </div>
        </form>
      </section>

      <section className={styles.listCard}>
        <h2>Service Definitions</h2>
        {loading ? (
          <p>Loading services...</p>
        ) : services.length === 0 ? (
          <p>No services found for current filter.</p>
        ) : (
          <ul className={styles.list}>
            {services.map(service => (
              <li key={service.serviceId} className={styles.item}>
                <div>
                  <h3>{service.name}</h3>
                  <p className={styles.meta}>Code: {service.code} · {service.estimatedDurationMinutes} mins · Offices: {service.assignedOfficeIds.length}</p>
                  <p>{service.description}</p>
                  <p className={styles.meta}>
                    <span className={service.isActive ? styles.activeBadge : styles.inactiveBadge}>
                      {service.isActive ? 'Active' : 'Inactive'}
                    </span>
                  </p>
                </div>
                <div className={styles.itemActions}>
                  <button
                    type="button"
                    className={styles.secondaryBtn}
                    onClick={() => beginEdit(service)}
                    disabled={!service.isActive}
                  >
                    Edit
                  </button>
                  <button
                    type="button"
                    className={styles.dangerBtn}
                    onClick={() => handleDeactivate(service.serviceId)}
                    disabled={!service.isActive}
                  >
                    Deactivate
                  </button>
                </div>
              </li>
            ))}
          </ul>
        )}
      </section>

      <section className={styles.assignmentsCard}>
        <h2>Office Availability (Now)</h2>
        <p className={styles.helperText}>Choose which active offices can deliver each service.</p>
        {services.length === 0 ? (
          <p>Create a service first.</p>
        ) : (
          <>
            <select
              className={styles.select}
              value={assignmentServiceId}
              onChange={(event) => setAssignmentServiceId(event.target.value)}
            >
              {services.map(service => (
                <option key={service.serviceId} value={service.serviceId}>
                  {service.name} ({service.code})
                </option>
              ))}
            </select>

            <div className={styles.officeGrid}>
              {offices.map(office => (
                <label key={office.officeId} className={styles.officeCard}>
                  <input
                    type="checkbox"
                    checked={selectedOfficeIds.includes(office.officeId)}
                    onChange={() => toggleOfficeSelection(office.officeId)}
                  />
                  <span>{office.name}</span>
                </label>
              ))}
            </div>

            <button
              type="button"
              className={styles.primaryBtn}
              onClick={handleSaveAssignments}
              disabled={savingAssignments || !selectedService}
            >
              {savingAssignments ? 'Saving assignments...' : 'Save Assignments'}
            </button>

            <div className={styles.opsDivider} />

            <h3 className={styles.opsTitle}>Create Operational Flows from Service</h3>
            <p className={styles.helperText}>
              Generate a queue or appointment profile using this service definition as defaults.
            </p>

            <div className={styles.opsActions}>
              <button
                type="button"
                className={styles.secondaryBtn}
                onClick={handleCreateQueueFromService}
                disabled={!selectedService || creatingFromService !== null}
              >
                {creatingFromService === 'queue' ? 'Creating queue...' : 'Create Queue from Service'}
              </button>
              <button
                type="button"
                className={styles.secondaryBtn}
                onClick={handleCreateAppointmentProfileFromService}
                disabled={!selectedService || creatingFromService !== null}
              >
                {creatingFromService === 'appointment' ? 'Creating profile...' : 'Create Appointment Profile from Service'}
              </button>
            </div>

            {(latestQueueLink || latestAppointmentLink) && (
              <div className={styles.generatedLinks} role="status">
                {latestQueueLink && (
                  <p>
                    Queue link: <strong>{window.location.origin}{latestQueueLink}</strong>
                  </p>
                )}
                {latestAppointmentLink && (
                  <p>
                    Appointment link: <strong>{window.location.origin}{latestAppointmentLink}</strong>
                  </p>
                )}
              </div>
            )}
          </>
        )}
      </section>
    </div>
  )
}
