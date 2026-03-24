import { useEffect, useMemo, useState } from 'react'
import { useNavigate, useParams } from 'react-router-dom'
import {
  createOffice,
  deactivateOffice,
  listOffices,
  updateOffice,
  type OfficeDto,
} from '../../api/offices'
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

export function OfficeManagementPage() {
  const navigate = useNavigate()
  const { tenantId } = useParams<{ tenantId: string }>()
  const payload = getTokenPayload()

  const [offices, setOffices] = useState<OfficeDto[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [isActiveFilter, setIsActiveFilter] = useState<'all' | 'active' | 'inactive'>('active')
  const [search, setSearch] = useState('')
  const [form, setForm] = useState<OfficeForm>(defaultForm)
  const [mode, setMode] = useState<Mode>('create')
  const [editOfficeId, setEditOfficeId] = useState<string | null>(null)
  const [saving, setSaving] = useState(false)
  const [success, setSuccess] = useState<string | null>(null)

  const derivedIsActive = useMemo(() => {
    if (isActiveFilter === 'all') return undefined
    return isActiveFilter === 'active'
  }, [isActiveFilter])

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
      const result = await listOffices(currentTenantId, {
        isActive,
        search: searchTerm?.trim() ? searchTerm.trim() : undefined,
        pageNumber: 1,
        pageSize: 50,
      })

      setOffices(result.items)
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
    <div className={styles.page}>
      <header className={styles.header}>
        <h1>Office Management</h1>
        <button type="button" className={styles.backBtn} onClick={() => navigate(`/admin/${tenantId}`)}>
          Back to Admin
        </button>
      </header>

      {error && <div className={styles.error}>{error}</div>}
      {success && <div className={styles.success}>{success}</div>}

      <section className={styles.filters}>
        <input
          type="text"
          value={search}
          onChange={(e) => setSearch(e.target.value)}
          placeholder="Search by name or address"
          className={styles.input}
        />
        <select
          value={isActiveFilter}
          onChange={(e) => setIsActiveFilter(e.target.value as 'all' | 'active' | 'inactive')}
          className={styles.select}
        >
          <option value="active">Active</option>
          <option value="inactive">Inactive</option>
          <option value="all">All</option>
        </select>
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
                    {office.isActive ? 'Active' : 'Inactive'}
                    {office.deactivatedAt ? ` · Deactivated ${new Date(office.deactivatedAt).toLocaleString()}` : ''}
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
