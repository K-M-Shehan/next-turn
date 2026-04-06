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

type Mode = 'create' | 'edit'

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
  const [mode, setMode] = useState<Mode>('create')
  const [editOfficeId, setEditOfficeId] = useState<string | null>(null)
  const [saving, setSaving] = useState(false)
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
    setMode('create')
    setEditOfficeId(null)
  }

  function selectForEdit(office: OfficeDto) {
    setMode('edit')
    setEditOfficeId(office.officeId)
    setForm({
      name: office.name,
      address: office.address,
      latitude: office.latitude?.toString() ?? '',
      longitude: office.longitude?.toString() ?? '',
      openingHours: office.openingHours,
    })
    setSuccess(null)
    setError(null)
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
      if (mode === 'create') {
        await createOffice(tenantId, payloadBody)
        setSuccess('Office created successfully.')
      } else if (editOfficeId) {
        await updateOffice(tenantId, editOfficeId, payloadBody)
        setSuccess('Office updated successfully.')
      }

      resetForm()
      await loadOffices(tenantId, derivedIsActive, search)
    } catch (err) {
      const apiErr = err as ApiError
      setError(apiErr.detail ?? 'Could not save office.')
    } finally {
      setSaving(false)
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

      <section className={styles.filters}>
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

      <section className={styles.formCard}>
        <h2>{mode === 'create' ? 'Create Office' : 'Update Office'}</h2>
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
              {saving ? 'Saving...' : mode === 'create' ? 'Create Office' : 'Update Office'}
            </button>
            {mode === 'edit' && (
              <button type="button" className={styles.secondaryBtn} onClick={resetForm}>
                Cancel Edit
              </button>
            )}
          </div>
        </form>

        {mode === 'edit' && editOfficeId && (
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
        )}
      </section>

      <section className={styles.listCard}>
        <h2>Offices</h2>
        {loading ? (
          <p>Loading offices...</p>
        ) : offices.length === 0 ? (
          <p>No offices found for current filters.</p>
        ) : (
          <ul className={styles.list}>
            {offices.map((office) => (
              <li key={office.officeId} className={styles.item}>
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
                    onClick={() => selectForEdit(office)}
                    disabled={!office.isActive}
                  >
                    Edit
                  </button>
                  <button
                    type="button"
                    className={styles.dangerBtn}
                    onClick={() => handleDeactivate(office.officeId)}
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
    </div>
  )
}
