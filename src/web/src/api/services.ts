import { apiClient, parseApiError } from './client'
import { getToken } from '../utils/authToken'

export interface ServiceDto {
  serviceId: string
  name: string
  code: string
  description: string
  estimatedDurationMinutes: number
  isActive: boolean
  assignedOfficeIds: string[]
  deactivatedAt: string | null
  createdAt: string
  updatedAt: string
}

export interface ListServicesResult {
  items: ServiceDto[]
  pageNumber: number
  pageSize: number
  totalCount: number
}

export interface CreateServiceBody {
  name: string
  code: string
  description: string
  estimatedDurationMinutes: number
  isActive: boolean
}

export interface UpdateServiceBody {
  name: string
  description: string
  estimatedDurationMinutes: number
}

export interface ListServicesParams {
  activeOnly?: boolean
  pageNumber?: number
  pageSize?: number
}

export async function listServices(tenantId: string, params: ListServicesParams = {}): Promise<ListServicesResult> {
  try {
    const token = getToken()
    const { data } = await apiClient.get<ListServicesResult>('/services', {
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

export async function createService(tenantId: string, body: CreateServiceBody): Promise<ServiceDto> {
  try {
    const token = getToken()
    const { data } = await apiClient.post<ServiceDto>('/services', body, {
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

export async function updateService(tenantId: string, serviceId: string, body: UpdateServiceBody): Promise<ServiceDto> {
  try {
    const token = getToken()
    const { data } = await apiClient.put<ServiceDto>(`/services/${serviceId}`, body, {
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

export async function deactivateService(tenantId: string, serviceId: string): Promise<void> {
  try {
    const token = getToken()
    await apiClient.patch(`/services/${serviceId}/deactivate`, null, {
      headers: {
        Authorization: `Bearer ${token}`,
        'X-Tenant-Id': tenantId,
      },
    })
  } catch (err) {
    throw parseApiError(err)
  }
}

export async function assignServiceOffices(tenantId: string, serviceId: string, officeIds: string[]): Promise<void> {
  try {
    const token = getToken()
    await apiClient.post(
      `/services/${serviceId}/offices`,
      { officeIds },
      {
        headers: {
          Authorization: `Bearer ${token}`,
          'X-Tenant-Id': tenantId,
        },
      },
    )
  } catch (err) {
    throw parseApiError(err)
  }
}

export async function removeServiceOfficeAssignment(tenantId: string, serviceId: string, officeId: string): Promise<void> {
  try {
    const token = getToken()
    await apiClient.delete(`/services/${serviceId}/offices/${officeId}`, {
      headers: {
        Authorization: `Bearer ${token}`,
        'X-Tenant-Id': tenantId,
      },
    })
  } catch (err) {
    throw parseApiError(err)
  }
}
