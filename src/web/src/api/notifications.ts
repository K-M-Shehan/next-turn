import { apiClient, parseApiError } from './client'
import type { ApiError } from '../types/api'

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

export async function listMyNotifications(take = 20): Promise<InAppNotification[]> {
  try {
    const { data } = await apiClient.get<InAppNotification[]>(`/notifications?take=${take}`)
    return data
  } catch (err) {
    const parsed: ApiError = parseApiError(err)
    throw parsed
  }
}

export async function markNotificationRead(notificationId: string): Promise<void> {
  try {
    await apiClient.patch(`/notifications/${notificationId}/read`)
  } catch (err) {
    const parsed: ApiError = parseApiError(err)
    throw parsed
  }
}

export async function markAllNotificationsRead(): Promise<void> {
  try {
    await apiClient.patch('/notifications/read-all')
  } catch (err) {
    const parsed: ApiError = parseApiError(err)
    throw parsed
  }
}
