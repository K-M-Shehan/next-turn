import { useEffect, useMemo, useState } from 'react'
import { useNavigate, useParams } from 'react-router-dom'
import { listOffices, type OfficeDto } from '../../api/offices'
import { createStaff, deactivateStaff, listStaff, updateStaff, type StaffDto } from '../../api/staff'
import type { ApiError } from '../../types/api'
import { clearToken, getTokenPayload } from '../../utils/authToken'
import styles from './StaffManagementPage.module.css'

type Mode = 'create' | 'edit'

interface StaffForm {
  name: string
  email: string
  phone: string
  counterName: string
  shiftStart: string
  shiftEnd: string
}

const defaultForm: StaffForm = {
  name: '',
  email: '',
  phone: '',
  counterName: '',
  shiftStart: '',
  shiftEnd: '',
}

export function StaffManagementPage() {
  const navigate = useNavigate()
  const { tenantId } = useParams<{ tenantId: string }>()
  const payload = getTokenPayload()

  const [staff, setStaff] = useState<StaffDto[]>([])
  const [offices, setOffices] = useState<OfficeDto[]>([])
  const [selectedOfficeIds, setSelectedOfficeIds] = useState<string[]>([])
  const [mode, setMode] = useState<Mode>('create')
  const [editStaffId, setEditStaffId] = useState<string | null>(null)
  const [form, setForm] = useState<StaffForm>(defaultForm)
  const [loading, setLoading] = useState(true)
  const [saving, setSaving] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [success, setSuccess] = useState<string | null>(null)

  const activeOfficeSet = useMemo(() => new Set(selectedOfficeIds), [selectedOfficeIds])

  useEffect(() => {
    if (!payload) {
      clearToken()
      navigate('/', { replace: true })
      return
    }

    if (!tenantId) return
    void loadData(tenantId)
  // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [tenantId])

  async function loadData(currentTenantId: string) {
    setLoading(true)
    setError(null)

    try {
      const [staffResult, officesResult] = await Promise.all([
        listStaff(currentTenantId, 1, 100),
        listOffices(currentTenantId, { isActive: true, pageNumber: 1, pageSize: 200 }),
      ])

      setStaff(staffResult.items)
      setOffices(officesResult.items)
    } catch (err) {
      const apiErr = err as ApiError
      setError(apiErr.detail ?? 'Could not load staff management data.')
    } finally {
      setLoading(false)
    }
  }

  function resetForm() {
    setMode('create')
    setEditStaffId(null)
    setForm(defaultForm)
    setSelectedOfficeIds([])
  }

  function beginEdit(staffUser: StaffDto) {
    setMode('edit')
    setEditStaffId(staffUser.staffUserId)
    setForm({
      name: staffUser.name,
      email: staffUser.email,
      phone: staffUser.phone ?? '',
      counterName: staffUser.counterName ?? '',
      shiftStart: staffUser.shiftStart ?? '',
      shiftEnd: staffUser.shiftEnd ?? '',
    })
    setSelectedOfficeIds(staffUser.officeIds)
    setError(null)
    setSuccess(null)
  }

  function toggleOfficeSelection(officeId: string) {
    setSelectedOfficeIds(prev => prev.includes(officeId) ? prev.filter(x => x !== officeId) : [...prev, officeId])
  }

  async function handleSubmit(event: React.FormEvent) {
    event.preventDefault()
    if (!tenantId) return

    setSaving(true)
    setError(null)
    setSuccess(null)

    try {
      if (mode === 'create') {
        await createStaff(tenantId, {
          name: form.name.trim(),
          email: form.email.trim(),
          phone: form.phone.trim() || null,
          officeIds: selectedOfficeIds,
          counterName: form.counterName.trim() || null,
          shiftStart: form.shiftStart || null,
          shiftEnd: form.shiftEnd || null,
        })
        setSuccess('Staff account created successfully.')
      } else if (editStaffId) {
        await updateStaff(tenantId, editStaffId, {
          name: form.name.trim(),
          phone: form.phone.trim() || null,
          officeIds: selectedOfficeIds,
          counterName: form.counterName.trim() || null,
          shiftStart: form.shiftStart || null,
          shiftEnd: form.shiftEnd || null,
        })
        setSuccess('Staff account updated successfully.')
      }

      resetForm()
      await loadData(tenantId)
    } catch (err) {
      const apiErr = err as ApiError
      setError(apiErr.detail ?? 'Could not save staff account.')
    } finally {
      setSaving(false)
    }
  }

  async function handleDeactivate(staffUserId: string) {
    if (!tenantId) return

    setError(null)
    setSuccess(null)

    try {
      await deactivateStaff(tenantId, staffUserId)
      setSuccess('Staff account deactivated successfully.')
      await loadData(tenantId)
    } catch (err) {
      const apiErr = err as ApiError
      setError(apiErr.detail ?? 'Could not deactivate staff account.')
    }
  }

  return (
    <div className={styles.page}>
      <header className={styles.header}>
        <h1>Staff Account Management</h1>
        <button type="button" className={styles.backBtn} onClick={() => navigate(`/admin/${tenantId}`)}>
          Back to Admin
        </button>
      </header>

      {error && <div className={styles.error}>{error}</div>}
      {success && <div className={styles.success}>{success}</div>}

      <section className={styles.formCard}>
        <h2>{mode === 'create' ? 'Create Staff Account' : 'Update Staff Account'}</h2>
        <form onSubmit={handleSubmit} className={styles.form}>
          <input
            className={styles.input}
            placeholder="Name"
            value={form.name}
            onChange={event => setForm(prev => ({ ...prev, name: event.target.value }))}
            required
          />
          <input
            className={styles.input}
            placeholder="Email"
            type="email"
            value={form.email}
            onChange={event => setForm(prev => ({ ...prev, email: event.target.value }))}
            disabled={mode === 'edit'}
            required
          />
          <input
            className={styles.input}
            placeholder="Phone"
            value={form.phone}
            onChange={event => setForm(prev => ({ ...prev, phone: event.target.value }))}
          />
          <input
            className={styles.input}
            placeholder="Counter name (optional)"
            value={form.counterName}
            onChange={event => setForm(prev => ({ ...prev, counterName: event.target.value }))}
          />

          <div className={styles.shiftRow}>
            <input
              className={styles.input}
              placeholder="Shift start (HH:mm)"
              value={form.shiftStart}
              onChange={event => setForm(prev => ({ ...prev, shiftStart: event.target.value }))}
            />
            <input
              className={styles.input}
              placeholder="Shift end (HH:mm)"
              value={form.shiftEnd}
              onChange={event => setForm(prev => ({ ...prev, shiftEnd: event.target.value }))}
            />
          </div>

          <div>
            <p className={styles.sectionLabel}>Assigned Offices</p>
            <div className={styles.officeGrid}>
              {offices.map(office => (
                <label key={office.officeId} className={styles.officeItem}>
                  <input
                    type="checkbox"
                    checked={activeOfficeSet.has(office.officeId)}
                    onChange={() => toggleOfficeSelection(office.officeId)}
                  />
                  <span>{office.name}</span>
                </label>
              ))}
            </div>
          </div>

          <div className={styles.actions}>
            <button type="submit" className={styles.primaryBtn} disabled={saving}>
              {saving ? 'Saving...' : mode === 'create' ? 'Create Staff' : 'Update Staff'}
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
        <h2>Staff Accounts</h2>
        {loading ? (
          <p>Loading staff accounts...</p>
        ) : staff.length === 0 ? (
          <p>No staff accounts found.</p>
        ) : (
          <ul className={styles.list}>
            {staff.map(item => (
              <li key={item.staffUserId} className={styles.listItem}>
                <div>
                  <h3>{item.name}</h3>
                  <p>{item.email}</p>
                  <p className={styles.meta}>{item.phone ?? 'No phone'}</p>
                  <p className={styles.meta}>
                    {item.isActive ? 'Active' : 'Inactive'}
                    {item.counterName ? ` · ${item.counterName}` : ''}
                    {item.shiftStart && item.shiftEnd ? ` · ${item.shiftStart}-${item.shiftEnd}` : ''}
                  </p>
                </div>
                <div className={styles.rowActions}>
                  <button
                    type="button"
                    className={styles.secondaryBtn}
                    onClick={() => beginEdit(item)}
                    disabled={!item.isActive}
                  >
                    Edit
                  </button>
                  <button
                    type="button"
                    className={styles.dangerBtn}
                    onClick={() => handleDeactivate(item.staffUserId)}
                    disabled={!item.isActive}
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
