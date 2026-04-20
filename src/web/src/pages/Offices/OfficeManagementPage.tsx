import { useEffect, useMemo, useState } from 'react'
import { useNavigate, useParams } from 'react-router-dom'
import {
  createOffice,
  deactivateOffice,
  listOffices,
  updateOffice,
  type OfficeDto,
} from '../../api/offices'
import { listStaff, type StaffDto } from '../../api/staff'
import type { ApiError } from '../../types/api'
import { clearToken, getTokenPayload } from '../../utils/authToken'
import styles from './OfficeManagementPage.module.css'

interface OfficeForm {
  name: string
  address: string
  latitude: string
  longitude: string
  openingHours: string
}

const defaultForm: OfficeForm = {
  name: '',
  address: '',
  latitude: '',
  longitude: '',
  openingHours: '',
}

interface OfficeManagementPageProps {
  embedded?: boolean
}

export function OfficeManagementPage({ embedded = false }: OfficeManagementPageProps = {}) {
  const navigate = useNavigate()
  const { tenantId } = useParams<{ tenantId: string }>()
  const payload = getTokenPayload()

  const [offices, setOffices] = useState<OfficeDto[]>([])
  const [staffMembers, setStaffMembers] = useState<StaffDto[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [isActiveFilter, setIsActiveFilter] = useState<'all' | 'active' | 'inactive'>('active')
  const [search, setSearch] = useState('')
  const [form, setForm] = useState<OfficeForm>(defaultForm)
  const [editOfficeId, setEditOfficeId] = useState<string | null>(null)
  const [editForm, setEditForm] = useState<OfficeForm>(defaultForm)
  const [editModalOpen, setEditModalOpen] = useState(false)
  const [selectedOffice, setSelectedOffice] = useState<OfficeDto | null>(null)
  const [saving, setSaving] = useState(false)
  const [savingEdit, setSavingEdit] = useState(false)
  const [success, setSuccess] = useState<string | null>(null)

  const activeCount = useMemo(() => offices.filter(x => x.isActive).length, [offices])
  const inactiveCount = useMemo(() => offices.filter(x => !x.isActive).length, [offices])

  const derivedIsActive = useMemo(() => {
    if (isActiveFilter === 'all') return undefined
    return isActiveFilter === 'active'
  }, [isActiveFilter])

  const staffByOffice = useMemo(() => {
    const map = new Map<string, StaffDto[]>()
    for (const staff of staffMembers) {
      for (const officeId of staff.officeIds) {
        const list = map.get(officeId) ?? []
        list.push(staff)
        map.set(officeId, list)
      }
    }
    return map
  }, [staffMembers])

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

  useEffect(() => {
    if (!payload) {
      clearToken()
      navigate('/', { replace: true })
      return
    }

    if (!tenantId) return

    void loadOffices(tenantId, derivedIsActive, search)
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [tenantId, derivedIsActive, search])

  async function loadOffices(currentTenantId: string, isActive?: boolean, searchTerm?: string) {
    setLoading(true)
    setError(null)

    try {
      const [officesResult, staffResult] = await Promise.all([
        listOffices(currentTenantId, {
          isActive,
          search: searchTerm?.trim() ? searchTerm.trim() : undefined,
          pageNumber: 1,
          pageSize: 50,
        }),
        listStaff(currentTenantId, 1, 100),
      ])

      setOffices(officesResult.items)
      setStaffMembers(staffResult.items)
    } catch (err) {
      const apiErr = err as ApiError
      setError(apiErr.detail ?? 'Could not load offices.')
    } finally {
      setLoading(false)
    }
  }

  function resetForm() {
    setForm(defaultForm)
  }

  function openOfficeDetails(office: OfficeDto) {
    setSelectedOffice(office)
  }

  function closeOfficeDetails() {
    setSelectedOffice(null)
  }

  function selectForEdit(office: OfficeDto) {
    setEditOfficeId(office.officeId)
    setEditForm({
      name: office.name,
      address: office.address,
      latitude: office.latitude?.toString() ?? '',
      longitude: office.longitude?.toString() ?? '',
      openingHours: office.openingHours,
    })
    setEditModalOpen(true)
    setSuccess(null)
    setError(null)
  }

  function closeEditModal() {
    setEditModalOpen(false)
    setEditOfficeId(null)
    setEditForm(defaultForm)
  }

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault()
    if (!tenantId) return

    setSaving(true)
    setError(null)
    setSuccess(null)

    const payloadBody = {
      name: form.name.trim(),
      address: form.address.trim(),
      latitude: form.latitude.trim() ? Number(form.latitude) : null,
      longitude: form.longitude.trim() ? Number(form.longitude) : null,
      openingHours: form.openingHours.trim(),
    }

    try {
      await createOffice(tenantId, payloadBody)
      setSuccess('Office created successfully.')

      resetForm()
      await loadOffices(tenantId, derivedIsActive, search)
    } catch (err) {
      const apiErr = err as ApiError
      setError(apiErr.detail ?? 'Could not save office.')
    } finally {
      setSaving(false)
    }
  }

  async function handleSaveEdit(e: React.FormEvent) {
    e.preventDefault()
    if (!tenantId || !editOfficeId) return

    setSavingEdit(true)
    setError(null)
    setSuccess(null)

    const payloadBody = {
      name: editForm.name.trim(),
      address: editForm.address.trim(),
      latitude: editForm.latitude.trim() ? Number(editForm.latitude) : null,
      longitude: editForm.longitude.trim() ? Number(editForm.longitude) : null,
      openingHours: editForm.openingHours.trim(),
    }

    try {
      await updateOffice(tenantId, editOfficeId, payloadBody)
      setSuccess('Office updated successfully.')
      closeEditModal()
      await loadOffices(tenantId, derivedIsActive, search)
    } catch (err) {
      const apiErr = err as ApiError
      setError(apiErr.detail ?? 'Could not save office updates.')
    } finally {
      setSavingEdit(false)
    }
  }

  async function handleDeactivate(officeId: string) {
    if (!tenantId) return

    if (!window.confirm('Deactivate this office? It will be hidden from public selection.')) {
      return
    }

    setError(null)
    setSuccess(null)

    try {
      await deactivateOffice(tenantId, officeId)
      setSuccess('Office deactivated successfully.')
      await loadOffices(tenantId, derivedIsActive, search)
    } catch (err) {
      const apiErr = err as ApiError
      setError(apiErr.detail ?? 'Could not deactivate office.')
    }
  }

  return (
    <div className={embedded ? styles.embeddedPage : styles.page}>
      <header className={styles.header}>
        <div>
          <h1 className={styles.title}>Office Management</h1>
          <p className={styles.subtitle}>Create and manage organisation branches and locations.</p>
        </div>
        {!embedded && (
          <button type="button" className={styles.backBtn} onClick={() => navigate(`/admin/${tenantId}`)}>
            Back to Admin
          </button>
        )}
      </header>

      {error && <div className={styles.error}>{error}</div>}
      {success && <div className={styles.success}>{success}</div>}

      <section className={styles.filters} data-onboarding="admin-office-filters">
        <div className={styles.filterGrid}>
          <input
            type="text"
            value={search}
            onChange={(e) => setSearch(e.target.value)}
            placeholder="Search by office name or address"
            className={styles.input}
          />
          <select
            value={isActiveFilter}
            onChange={(e) => setIsActiveFilter(e.target.value as 'all' | 'active' | 'inactive')}
            className={styles.select}
            aria-label="Office status filter"
          >
            <option value="active">Active only</option>
            <option value="inactive">Inactive only</option>
            <option value="all">All offices</option>
          </select>
          <button
            type="button"
            className={styles.secondaryBtn}
            onClick={() => {
              setSearch('')
              setIsActiveFilter('active')
            }}
          >
            Reset Filters
          </button>
        </div>
        <p className={styles.metaSummary}>
          Showing {offices.length} offices · {activeCount} active · {inactiveCount} inactive
        </p>
      </section>

      <section className={styles.formCard} data-onboarding="admin-office-create">
        <h2>Create Office</h2>
        <form onSubmit={handleSubmit} className={styles.form}>
          <input
            className={styles.input}
            placeholder="Office name"
            value={form.name}
            onChange={(e) => setForm(f => ({ ...f, name: e.target.value }))}
            required
          />
          <input
            className={styles.input}
            placeholder="Office address"
            value={form.address}
            onChange={(e) => setForm(f => ({ ...f, address: e.target.value }))}
            required
          />
          <div className={styles.coords}>
            <input
              className={styles.input}
              placeholder="Latitude (optional)"
              value={form.latitude}
              onChange={(e) => setForm(f => ({ ...f, latitude: e.target.value }))}
            />
            <input
              className={styles.input}
              placeholder="Longitude (optional)"
              value={form.longitude}
              onChange={(e) => setForm(f => ({ ...f, longitude: e.target.value }))}
            />
          </div>
          <textarea
            className={styles.textarea}
            placeholder="Opening hours (string or JSON)"
            value={form.openingHours}
            onChange={(e) => setForm(f => ({ ...f, openingHours: e.target.value }))}
            required
          />

          <div className={styles.actions}>
            <button type="submit" className={styles.primaryBtn} disabled={saving}>
              {saving ? 'Saving...' : 'Create Office'}
            </button>
          </div>
        </form>
      </section>

      <section className={styles.listCard} data-onboarding="admin-office-list">
        <h2>Offices</h2>
        {loading ? (
          <p>Loading offices...</p>
        ) : offices.length === 0 ? (
          <p>No offices found for current filters.</p>
        ) : (
          <ul className={styles.list}>
            {offices.map((office) => (
              <li
                key={office.officeId}
                className={styles.item}
                role="button"
                tabIndex={0}
                onClick={() => openOfficeDetails(office)}
                onKeyDown={(e) => {
                  if (e.key === 'Enter' || e.key === ' ') {
                    e.preventDefault()
                    openOfficeDetails(office)
                  }
                }}
              >
                <div>
                  <h3>{office.name}</h3>
                  <p>{office.address}</p>
                  <p className={styles.meta}>
                    <span className={office.isActive ? styles.activeBadge : styles.inactiveBadge}>
                      {office.isActive ? 'Active' : 'Inactive'}
                    </span>
                    {office.deactivatedAt ? ` · Deactivated ${new Date(office.deactivatedAt).toLocaleString()}` : ''}
                  </p>
                  {office.latitude !== null && office.longitude !== null && (
                    <p className={styles.meta}>Coordinates: {office.latitude}, {office.longitude}</p>
                  )}
                  <p className={styles.meta}>Hours: {office.openingHours}</p>
                  <p className={styles.meta}>
                    Staff assigned: {(staffByOffice.get(office.officeId)?.length ?? 0)}
                  </p>
                </div>

                <div className={styles.itemActions}>
                  <button
                    type="button"
                    className={styles.secondaryBtn}
                    onClick={(e) => {
                      e.stopPropagation()
                      selectForEdit(office)
                    }}
                    disabled={!office.isActive}
                  >
                    Edit
                  </button>
                  <button
                    type="button"
                    className={styles.dangerBtn}
                    onClick={(e) => {
                      e.stopPropagation()
                      void handleDeactivate(office.officeId)
                    }}
                    disabled={!office.isActive}
                  >
                    Deactivate
                  </button>
                </div>
              </li>
            ))}
          </ul>
        )}
      </section>

      {selectedOffice && (
        <div className={styles.modalOverlay} onClick={closeOfficeDetails}>
          <div
            className={styles.modalCard}
            role="dialog"
            aria-modal="true"
            aria-labelledby="office-detail-title"
            onClick={(e) => e.stopPropagation()}
          >
            <div className={styles.modalHeader}>
              <h3 id="office-detail-title" className={styles.modalTitle}>{selectedOffice.name}</h3>
              <button type="button" className={styles.secondaryBtn} onClick={closeOfficeDetails}>
                Close
              </button>
            </div>

            <div className={styles.modalBody}>
              <p><strong>Address:</strong> {selectedOffice.address}</p>
              <p><strong>Status:</strong> {selectedOffice.isActive ? 'Active' : 'Inactive'}</p>
              <p><strong>Opening hours:</strong> {selectedOffice.openingHours}</p>
              {selectedOffice.latitude !== null && selectedOffice.longitude !== null && (
                <p><strong>Coordinates:</strong> {selectedOffice.latitude}, {selectedOffice.longitude}</p>
              )}
              {selectedOffice.deactivatedAt && (
                <p><strong>Deactivated at:</strong> {new Date(selectedOffice.deactivatedAt).toLocaleString()}</p>
              )}

              <div className={styles.staffPanel}>
                <h4 className={styles.staffPanelTitle}>Assigned Staff</h4>
                {(staffByOffice.get(selectedOffice.officeId)?.length ?? 0) === 0 ? (
                  <p className={styles.meta}>No staff currently assigned to this office.</p>
                ) : (
                  <ul className={styles.staffList}>
                    {(staffByOffice.get(selectedOffice.officeId) ?? []).map(staff => (
                      <li key={staff.staffUserId} className={styles.staffItem}>
                        <strong>{staff.name}</strong>
                        <span className={styles.meta}>{staff.email}</span>
                        <span className={staff.isActive ? styles.activeBadge : styles.inactiveBadge}>
                          {staff.isActive ? 'Active' : 'Inactive'}
                        </span>
                      </li>
                    ))}
                  </ul>
                )}
              </div>
            </div>

            <div className={styles.modalActions}>
              <button
                type="button"
                className={styles.primaryBtn}
                onClick={() => {
                  selectForEdit(selectedOffice)
                  closeOfficeDetails()
                }}
                disabled={!selectedOffice.isActive}
              >
                Edit Office
              </button>
            </div>
          </div>
        </div>
      )}

      {editModalOpen && editOfficeId && (
        <div className={`${styles.modalOverlay} ${styles.modalOverlayEnter}`} onClick={closeEditModal}>
          <div
            className={`${styles.modalCard} ${styles.modalCardEnter}`}
            role="dialog"
            aria-modal="true"
            aria-labelledby="office-edit-title"
            onClick={(e) => e.stopPropagation()}
          >
            <div className={styles.modalHeader}>
              <h3 id="office-edit-title" className={styles.modalTitle}>Update Office</h3>
              <button type="button" className={styles.secondaryBtn} onClick={closeEditModal} disabled={savingEdit}>
                Close
              </button>
            </div>

            <form onSubmit={handleSaveEdit} className={styles.form}>
              <input
                className={styles.input}
                placeholder="Office name"
                value={editForm.name}
                onChange={(e) => setEditForm(f => ({ ...f, name: e.target.value }))}
                required
              />
              <input
                className={styles.input}
                placeholder="Office address"
                value={editForm.address}
                onChange={(e) => setEditForm(f => ({ ...f, address: e.target.value }))}
                required
              />
              <div className={styles.coords}>
                <input
                  className={styles.input}
                  placeholder="Latitude (optional)"
                  value={editForm.latitude}
                  onChange={(e) => setEditForm(f => ({ ...f, latitude: e.target.value }))}
                />
                <input
                  className={styles.input}
                  placeholder="Longitude (optional)"
                  value={editForm.longitude}
                  onChange={(e) => setEditForm(f => ({ ...f, longitude: e.target.value }))}
                />
              </div>
              <textarea
                className={styles.textarea}
                placeholder="Opening hours (string or JSON)"
                value={editForm.openingHours}
                onChange={(e) => setEditForm(f => ({ ...f, openingHours: e.target.value }))}
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

            <div className={styles.staffPanel}>
              <h3 className={styles.staffPanelTitle}>Staff In This Office</h3>
              {(staffByOffice.get(editOfficeId)?.length ?? 0) === 0 ? (
                <p className={styles.meta}>No staff currently assigned to this office.</p>
              ) : (
                <ul className={styles.staffList}>
                  {(staffByOffice.get(editOfficeId) ?? []).map(staff => (
                    <li key={staff.staffUserId} className={styles.staffItem}>
                      <strong>{staff.name}</strong>
                      <span className={styles.meta}>{staff.email}</span>
                      <span className={staff.isActive ? styles.activeBadge : styles.inactiveBadge}>
                        {staff.isActive ? 'Active' : 'Inactive'}
                      </span>
                    </li>
                  ))}
                </ul>
              )}
            </div>
          </div>
        </div>
      )}
    </div>
  )
}
