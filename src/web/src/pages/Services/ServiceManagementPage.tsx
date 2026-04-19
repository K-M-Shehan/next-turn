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
import { createQueue, getOrgQueues } from '../../api/queues'
import {
  configureAppointmentSchedule,
  createAppointmentProfile,
  getAppointmentSchedule,
  listAppointmentProfiles,
} from '../../api/appointments'
import type { ApiError } from '../../types/api'
import { clearToken, getTokenPayload } from '../../utils/authToken'
import styles from './ServiceManagementPage.module.css'

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

interface GeneratedFlowLink {
  officeName: string
  link: string
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
  const [saving, setSaving] = useState(false)
  const [editServiceId, setEditServiceId] = useState<string | null>(null)
  const [editForm, setEditForm] = useState<ServiceForm>(defaultForm)
  const [editModalOpen, setEditModalOpen] = useState(false)
  const [savingEdit, setSavingEdit] = useState(false)
  const [activeOnly, setActiveOnly] = useState(true)

  const [assignmentServiceId, setAssignmentServiceId] = useState<string>('')
  const [selectedOfficeIds, setSelectedOfficeIds] = useState<string[]>([])
  const [operationServiceId, setOperationServiceId] = useState<string>('')
  const [selectedOperationOfficeIds, setSelectedOperationOfficeIds] = useState<string[]>([])
  const [savingAssignments, setSavingAssignments] = useState(false)
  const [creatingFromService, setCreatingFromService] = useState<'queue' | 'appointment' | null>(null)
  const [latestQueueLinks, setLatestQueueLinks] = useState<GeneratedFlowLink[]>([])
  const [latestAppointmentLinks, setLatestAppointmentLinks] = useState<GeneratedFlowLink[]>([])

  const selectedService = useMemo(
    () => services.find(s => s.serviceId === assignmentServiceId) ?? null,
    [services, assignmentServiceId],
  )

  const selectedOperationService = useMemo(
    () => services.find(s => s.serviceId === operationServiceId) ?? null,
    [services, operationServiceId],
  )

  const operationServiceOffices = useMemo(() => {
    if (!selectedOperationService) return []

    const officeIds = new Set(selectedOperationService.assignedOfficeIds)
    return offices.filter(office => officeIds.has(office.officeId))
  }, [offices, selectedOperationService])

  const hasPendingAssignmentChanges = useMemo(() => {
    if (!selectedService) return false

    const currentSorted = [...selectedService.assignedOfficeIds].sort()
    const selectedSorted = [...selectedOfficeIds].sort()

    if (currentSorted.length !== selectedSorted.length) return true

    return currentSorted.some((id, idx) => id !== selectedSorted[idx])
  }, [selectedOfficeIds, selectedService])

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

  useEffect(() => {
    if (!selectedOperationService) {
      setSelectedOperationOfficeIds([])
      return
    }

    setSelectedOperationOfficeIds(selectedOperationService.assignedOfficeIds)
  }, [selectedOperationService])

  useEffect(() => {
    if (!editModalOpen) return

    const handleEscape = (event: KeyboardEvent) => {
      if (event.key === 'Escape' && !savingEdit) {
        closeEditModal()
      }
    }

    window.addEventListener('keydown', handleEscape)
    return () => window.removeEventListener('keydown', handleEscape)
  }, [editModalOpen, savingEdit])

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
      setOperationServiceId(prev => {
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
    setForm(defaultForm)
  }

  function beginEdit(service: ServiceDto) {
    setEditServiceId(service.serviceId)
    setEditForm({
      name: service.name,
      code: service.code,
      description: service.description,
      estimatedDurationMinutes: String(service.estimatedDurationMinutes),
      isActive: service.isActive,
    })
    setEditModalOpen(true)
    setError(null)
    setSuccess(null)
  }

  function closeEditModal() {
    setEditModalOpen(false)
    setEditServiceId(null)
    setEditForm(defaultForm)
  }

  async function handleSubmit(event: React.FormEvent) {
    event.preventDefault()
    if (!tenantId) return

    setSaving(true)
    setError(null)
    setSuccess(null)

    try {
      await createService(tenantId, {
        name: form.name.trim(),
        code: form.code.trim(),
        description: form.description.trim(),
        estimatedDurationMinutes: Number(form.estimatedDurationMinutes),
        isActive: form.isActive,
      })
      setSuccess('Service created successfully.')

      resetForm()
      await loadData(tenantId, activeOnly)
    } catch (err) {
      const apiErr = err as ApiError
      setError(apiErr.detail ?? 'Could not save service.')
    } finally {
      setSaving(false)
    }
  }

  async function handleSaveEdit(event: React.FormEvent) {
    event.preventDefault()
    if (!tenantId || !editServiceId) return

    setSavingEdit(true)
    setError(null)
    setSuccess(null)

    try {
      await updateService(tenantId, editServiceId, {
        name: editForm.name.trim(),
        description: editForm.description.trim(),
        estimatedDurationMinutes: Number(editForm.estimatedDurationMinutes),
      })
      setSuccess('Service updated successfully.')
      closeEditModal()
      await loadData(tenantId, activeOnly)
    } catch (err) {
      const apiErr = err as ApiError
      setError(apiErr.detail ?? 'Could not save service updates.')
    } finally {
      setSavingEdit(false)
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

  function toggleOperationOfficeSelection(officeId: string) {
    setSelectedOperationOfficeIds(prev =>
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
    if (!tenantId || !selectedOperationService) return

    if (assignmentServiceId === operationServiceId && hasPendingAssignmentChanges) {
      setError('Save office assignments before creating queues from this service.')
      return
    }

    if (selectedOperationOfficeIds.length === 0) {
      setError('Assign at least one office to this service before creating queues.')
      return
    }

    setCreatingFromService('queue')
    setError(null)
    setSuccess(null)
    setLatestQueueLinks([])

    try {
      const selectedOffices = operationServiceOffices.filter(office =>
        selectedOperationOfficeIds.includes(office.officeId),
      )

      const existingQueues = await getOrgQueues(tenantId)
      const existingQueueNames = new Set(existingQueues.map(queue => queue.name.trim().toLowerCase()))

      const createdQueueLinks: GeneratedFlowLink[] = []
      let skippedCount = 0

      for (const office of selectedOffices) {
        const queueName = `${selectedOperationService.name} - ${office.name} Queue`
        const queueNameKey = queueName.trim().toLowerCase()

        if (existingQueueNames.has(queueNameKey)) {
          skippedCount += 1
          continue
        }

        const result = await createQueue(tenantId, {
          name: queueName,
          maxCapacity: 50,
          averageServiceTimeSeconds: Math.max(60, selectedOperationService.estimatedDurationMinutes * 60),
        })

        existingQueueNames.add(queueNameKey)
        createdQueueLinks.push({
          officeName: office.name,
          link: result.shareableLink,
        })
      }

      setLatestQueueLinks(createdQueueLinks)

      if (createdQueueLinks.length === 0) {
        setSuccess(`No queues created. A queue already exists for each selected office of "${selectedOperationService.name}".`)
      } else {
        setSuccess(
          `Created ${createdQueueLinks.length} queue${createdQueueLinks.length === 1 ? '' : 's'} for "${selectedOperationService.name}"${skippedCount > 0 ? `; skipped ${skippedCount} duplicate${skippedCount === 1 ? '' : 's'}` : ''}.`,
        )
      }
    } catch (err) {
      const apiErr = err as ApiError
      setError(apiErr.detail ?? 'Could not create queue from this service.')
    } finally {
      setCreatingFromService(null)
    }
  }

  async function handleCreateAppointmentProfileFromService() {
    if (!tenantId || !selectedOperationService) return

    if (assignmentServiceId === operationServiceId && hasPendingAssignmentChanges) {
      setError('Save office assignments before creating appointment profiles from this service.')
      return
    }

    if (selectedOperationOfficeIds.length === 0) {
      setError('Assign at least one office to this service before creating appointment profiles.')
      return
    }

    setCreatingFromService('appointment')
    setError(null)
    setSuccess(null)
    setLatestAppointmentLinks([])

    try {
      const slotDurationMinutes = Math.min(240, Math.max(5, selectedOperationService.estimatedDurationMinutes))

      const selectedOffices = operationServiceOffices.filter(office =>
        selectedOperationOfficeIds.includes(office.officeId),
      )

      const existingProfiles = await listAppointmentProfiles(tenantId)
      const existingProfileNames = new Set(existingProfiles.map(profile => profile.name.trim().toLowerCase()))

      const createdAppointmentLinks: GeneratedFlowLink[] = []
      let skippedCount = 0

      for (const office of selectedOffices) {
        const profileName = `${selectedOperationService.name} - ${office.name} Appointments`
        const profileNameKey = profileName.trim().toLowerCase()

        if (existingProfileNames.has(profileNameKey)) {
          skippedCount += 1
          continue
        }

        const profile = await createAppointmentProfile(
          tenantId,
          profileName,
        )

        // Apply service duration as default slot duration while preserving existing day open/close windows.
        const config = await getAppointmentSchedule(tenantId, profile.appointmentProfileId)

        await configureAppointmentSchedule(
          tenantId,
          profile.appointmentProfileId,
          config.dayRules.map(rule => ({
            ...rule,
            slotDurationMinutes,
          })),
        )

        existingProfileNames.add(profileNameKey)
        createdAppointmentLinks.push({
          officeName: office.name,
          link: profile.shareableLink,
        })
      }

      setLatestAppointmentLinks(createdAppointmentLinks)

      if (createdAppointmentLinks.length === 0) {
        setSuccess(`No appointment profiles created. A profile already exists for each selected office of "${selectedOperationService.name}".`)
      } else {
        setSuccess(
          `Created ${createdAppointmentLinks.length} appointment profile${createdAppointmentLinks.length === 1 ? '' : 's'} for "${selectedOperationService.name}" with ${slotDurationMinutes}-minute slots${skippedCount > 0 ? `; skipped ${skippedCount} duplicate${skippedCount === 1 ? '' : 's'}` : ''}.`,
        )
      }
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

      <section className={styles.formCard} data-onboarding="admin-service-create">
        <h2>Create Service Definition</h2>
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
          <label className={styles.checkboxRow}>
            <input
              type="checkbox"
              checked={form.isActive}
              onChange={(event) => setForm(prev => ({ ...prev, isActive: event.target.checked }))}
            />
            Active at creation
          </label>

          <div className={styles.actions}>
            <button type="submit" className={styles.primaryBtn} disabled={saving}>
              {saving ? 'Saving...' : 'Create Service'}
            </button>
          </div>
        </form>
      </section>

      <section className={styles.listCard} data-onboarding="admin-service-definitions">
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

      <section className={styles.assignmentsCard} data-onboarding="admin-service-availability">
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
          </>
        )}
      </section>

      <section className={styles.operationsCard} data-onboarding="admin-service-operations">
        <h2>Create Operational Flows from Service</h2>
        <p className={styles.helperText}>
          Choose the exact service and offices to target. The system creates one queue and one appointment profile per selected office, and skips duplicates.
        </p>
        {services.length === 0 ? (
          <p>Create a service first.</p>
        ) : (
          <>
            <label className={styles.meta}>Target service</label>
            <select
              className={styles.select}
              value={operationServiceId}
              onChange={(event) => setOperationServiceId(event.target.value)}
            >
              {services.map(service => (
                <option key={service.serviceId} value={service.serviceId}>
                  {service.name} ({service.code})
                </option>
              ))}
            </select>

            <label className={styles.meta}>Target offices</label>
            {operationServiceOffices.length === 0 ? (
              <p className={styles.helperText}>No offices are assigned to this service yet. Use Office Availability (Now) above.</p>
            ) : (
              <>
                <div className={styles.officeGrid}>
                  {operationServiceOffices.map(office => (
                    <label key={`operation-${office.officeId}`} className={styles.officeCard}>
                      <input
                        type="checkbox"
                        checked={selectedOperationOfficeIds.includes(office.officeId)}
                        onChange={() => toggleOperationOfficeSelection(office.officeId)}
                      />
                      <span>{office.name}</span>
                    </label>
                  ))}
                </div>

                <div className={styles.targetSummary}>
                  <p className={styles.meta}>
                    Targeted service: <strong>{selectedOperationService?.name ?? '-'}</strong>
                  </p>
                  <p className={styles.meta}>Targeted offices ({selectedOperationOfficeIds.length}):</p>
                  <div className={styles.targetChips}>
                    {operationServiceOffices
                      .filter(office => selectedOperationOfficeIds.includes(office.officeId))
                      .map(office => (
                        <span key={`chip-${office.officeId}`} className={styles.targetChip}>{office.name}</span>
                      ))}
                    {selectedOperationOfficeIds.length === 0 && (
                      <span className={styles.targetChip}>No office selected</span>
                    )}
                  </div>
                </div>
              </>
            )}

            <div className={styles.opsActions}>
              <button
                type="button"
                className={styles.secondaryBtn}
                onClick={handleCreateQueueFromService}
                disabled={!selectedOperationService || creatingFromService !== null || selectedOperationOfficeIds.length === 0}
              >
                {creatingFromService === 'queue' ? 'Creating queues...' : 'Create Queues for Selected Offices'}
              </button>
              <button
                type="button"
                className={styles.secondaryBtn}
                onClick={handleCreateAppointmentProfileFromService}
                disabled={!selectedOperationService || creatingFromService !== null || selectedOperationOfficeIds.length === 0}
              >
                {creatingFromService === 'appointment' ? 'Creating profiles...' : 'Create Appointment Profiles for Selected Offices'}
              </button>
            </div>

            {(latestQueueLinks.length > 0 || latestAppointmentLinks.length > 0) && (
              <div className={styles.generatedLinks} role="status">
                {latestQueueLinks.map(item => (
                  <p key={`queue-${item.officeName}-${item.link}`}>
                    Queue ({selectedOperationService?.name} - {item.officeName}): <strong>{window.location.origin}{item.link}</strong>
                  </p>
                ))}
                {latestAppointmentLinks.map(item => (
                  <p key={`appointment-${item.officeName}-${item.link}`}>
                    Appointment ({selectedOperationService?.name} - {item.officeName}): <strong>{window.location.origin}{item.link}</strong>
                  </p>
                ))}
              </div>
            )}
          </>
        )}
      </section>

      {editModalOpen && editServiceId && (
        <div className={`${styles.modalOverlay} ${styles.modalOverlayEnter}`} onClick={closeEditModal}>
          <div
            className={`${styles.modalCard} ${styles.modalCardEnter}`}
            role="dialog"
            aria-modal="true"
            aria-labelledby="service-edit-title"
            onClick={(event) => event.stopPropagation()}
          >
            <div className={styles.modalHeader}>
              <h3 id="service-edit-title" className={styles.modalTitle}>Edit Service Definition</h3>
              <button type="button" className={styles.secondaryBtn} onClick={closeEditModal} disabled={savingEdit}>
                Close
              </button>
            </div>

            <form onSubmit={handleSaveEdit} className={styles.form}>
              <input
                className={styles.input}
                placeholder="Service name"
                value={editForm.name}
                onChange={(event) => setEditForm(prev => ({ ...prev, name: event.target.value }))}
                required
              />
              <input
                className={styles.input}
                placeholder="Service code"
                value={editForm.code}
                disabled
                aria-label="Service code"
              />
              <textarea
                className={styles.textarea}
                placeholder="Description"
                value={editForm.description}
                onChange={(event) => setEditForm(prev => ({ ...prev, description: event.target.value }))}
                required
              />
              <input
                className={styles.input}
                type="number"
                min={1}
                max={1440}
                placeholder="Estimated duration (minutes)"
                value={editForm.estimatedDurationMinutes}
                onChange={(event) => setEditForm(prev => ({ ...prev, estimatedDurationMinutes: event.target.value }))}
                required
              />

              <div className={styles.actions}>
                <button type="submit" className={styles.primaryBtn} disabled={savingEdit}>
                  {savingEdit ? 'Saving...' : 'Save Changes'}
                </button>
                <button type="button" className={styles.secondaryBtn} onClick={closeEditModal} disabled={savingEdit}>
                  Cancel
                </button>
              </div>
            </form>
          </div>
        </div>
      )}
    </div>
  )
}
