import { apiClient, parseApiError } from './client'
import { getToken } from '../utils/authToken'

export interface OfficeDto {
  officeId: string
  name: string
  address: string
  latitude: number | null
  longitude: number | null
  openingHours: string
  isActive: boolean
  deactivatedAt: string | null
  createdAt: string
  updatedAt: string
}

export interface ListOfficesResult {
  items: OfficeDto[]
  pageNumber: number
  pageSize: number
  totalCount: number
}

export interface UpsertOfficeBody {
  name: string
  address: string
  latitude?: number | null
  longitude?: number | null
  openingHours: string
}

export interface ListOfficesParams {
  isActive?: boolean
  search?: string
  pageNumber?: number
  pageSize?: number
}

export async function createOffice(tenantId: string, body: UpsertOfficeBody): Promise<OfficeDto> {
  try {
    const token = getToken()
    const { data } = await apiClient.post<OfficeDto>('/offices', body, {
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

export async function listOffices(tenantId: string, params: ListOfficesParams = {}): Promise<ListOfficesResult> {
  try {
    const token = getToken()
    const { data } = await apiClient.get<ListOfficesResult>('/offices', {
      params,
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

export async function getOfficeById(tenantId: string, officeId: string): Promise<OfficeDto> {
  try {
    const token = getToken()
    const { data } = await apiClient.get<OfficeDto>(`/offices/${officeId}`, {
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

export async function updateOffice(tenantId: string, officeId: string, body: UpsertOfficeBody): Promise<OfficeDto> {
  try {
    const token = getToken()
    const { data } = await apiClient.put<OfficeDto>(`/offices/${officeId}`, body, {
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

export async function deactivateOffice(tenantId: string, officeId: string): Promise<void> {
  try {
    const token = getToken()
    await apiClient.patch(`/offices/${officeId}/deactivate`, null, {
      headers: {
        Authorization: `Bearer ${token}`,
        'X-Tenant-Id': tenantId,
      },
    })
  } catch (err) {
    throw parseApiError(err)
  }
}
