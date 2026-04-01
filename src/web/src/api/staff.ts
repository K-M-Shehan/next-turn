import { apiClient, parseApiError } from './client'
import { getToken } from '../utils/authToken'

export interface StaffDto {
  staffUserId: string
  name: string
  email: string
  phone: string | null
  isActive: boolean
  counterName: string | null
  shiftStart: string | null
  shiftEnd: string | null
  officeIds: string[]
  createdAt: string
}

export interface ListStaffResult {
  items: StaffDto[]
  pageNumber: number
  pageSize: number
  totalCount: number
}

export interface CreateStaffBody {
  name: string
  email: string
  phone?: string | null
  officeIds: string[]
  counterName?: string | null
  shiftStart?: string | null
  shiftEnd?: string | null
}

export interface UpdateStaffBody {
  name: string
  phone?: string | null
  officeIds: string[]
  counterName?: string | null
  shiftStart?: string | null
  shiftEnd?: string | null
}

export async function listStaff(tenantId: string, pageNumber = 1, pageSize = 20): Promise<ListStaffResult> {
  try {
    const token = getToken()
    const { data } = await apiClient.get<ListStaffResult>('/staff', {
      params: { pageNumber, pageSize },
      headers: {
        Authorization: `Bearer ${token}`,
        'X-Tenant-Id': tenantId,
      },
    })

    return data
  } catch (err) {
    throw parseApiError(err)
  }
}

export async function createStaff(tenantId: string, body: CreateStaffBody): Promise<StaffDto> {
  try {
    const token = getToken()
    const { data } = await apiClient.post<StaffDto>('/staff', body, {
      headers: {
        Authorization: `Bearer ${token}`,
        'X-Tenant-Id': tenantId,
      },
    })

    return data
  } catch (err) {
    throw parseApiError(err)
  }
}

export async function updateStaff(tenantId: string, staffUserId: string, body: UpdateStaffBody): Promise<StaffDto> {
  try {
    const token = getToken()
    const { data } = await apiClient.put<StaffDto>(`/staff/${staffUserId}`, body, {
      headers: {
        Authorization: `Bearer ${token}`,
        'X-Tenant-Id': tenantId,
      },
    })

    return data
  } catch (err) {
    throw parseApiError(err)
  }
}

export async function deactivateStaff(tenantId: string, staffUserId: string): Promise<void> {
  try {
    const token = getToken()
    await apiClient.patch(`/staff/${staffUserId}/deactivate`, null, {
      headers: {
        Authorization: `Bearer ${token}`,
        'X-Tenant-Id': tenantId,
      },
    })
  } catch (err) {
    throw parseApiError(err)
  }
}
