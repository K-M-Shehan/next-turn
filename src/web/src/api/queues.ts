/**
 * Queue API calls.
 * POST /api/queues/{queueId}/join requires a valid JWT (Authorization: Bearer {token})
 * and an X-Tenant-Id header.
 */
import { apiClient, parseApiError } from './client'
import { getToken } from '../utils/authToken'

/** Shape returned by POST /api/queues/{queueId}/join on HTTP 200. */
export interface JoinQueueResult {
  ticketNumber: number
  positionInQueue: number
  estimatedWaitSeconds: number
}

/**
 * POST /api/queues/{queueId}/join
 *
 * The API extracts the userId from the JWT sub claim server-side — the client
 * never passes a userId in the body (prevents impersonation).
 *
 * @throws ApiError on:
 *   400 — queue not found
 *   401 — missing or invalid JWT
 *   409 — already in queue (detail: "Already in this queue.")
 *         OR queue full   (raw.canBookAppointment === true)
 *   422 — validation failed
 */
export async function joinQueue(
  queueId: string,
  tenantId: string,
): Promise<JoinQueueResult> {
  try {
    const token = getToken()
    const { data } = await apiClient.post<JoinQueueResult>(
      `/queues/${queueId}/join`,
      null,
      {
        headers: {
          Authorization: `Bearer ${token}`,
          'X-Tenant-Id': tenantId,
        },
      }
    )
    return data
  } catch (err) {
    throw parseApiError(err)
  }
}
