import { apiClient, parseApiError } from './client'
import type { ApiError } from '../types/api'
import { getToken } from '../utils/authToken'

export interface InAppNotification {
  notificationId: string
  notificationType: string
  title: string
  message: string
  isRead: boolean
  createdAt: string
  readAt?: string | null
  queueId?: string | null
  queueEntryId?: string | null
}

function buildHeaders(tenantId?: string): Record<string, string> {
  const token = getToken()
  const headers: Record<string, string> = {
    Authorization: `Bearer ${token}`,
  }

  if (tenantId) {
    headers['X-Tenant-Id'] = tenantId
  }

  return headers
}

export async function listMyNotifications(take = 20, tenantId?: string): Promise<InAppNotification[]> {
  try {
    const { data } = await apiClient.get<InAppNotification[]>(`/notifications?take=${take}`, {
      headers: buildHeaders(tenantId),
    })
    return data
  } catch (err) {
    const parsed: ApiError = parseApiError(err)
    throw parsed
  }
}

export async function markNotificationRead(notificationId: string, tenantId?: string): Promise<void> {
  try {
    await apiClient.patch(`/notifications/${notificationId}/read`, null, {
      headers: buildHeaders(tenantId),
    })
  } catch (err) {
    const parsed: ApiError = parseApiError(err)
    throw parsed
  }
}

export async function markAllNotificationsRead(tenantId?: string): Promise<void> {
  try {
    await apiClient.patch('/notifications/read-all', null, {
      headers: buildHeaders(tenantId),
    })
  } catch (err) {
    const parsed: ApiError = parseApiError(err)
    throw parsed
  }
}
