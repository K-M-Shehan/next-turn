import { describe, it, expect, vi, beforeEach } from 'vitest'
import { render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter, Route, Routes } from 'react-router-dom'

import { StaffDashboardPage } from '../StaffDashboardPage'
import * as queuesApi from '../../../api/queues'

const TENANT_ID = 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa'
const QUEUE_ID = 'bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb'

vi.mock('../../../api/queues', async (importOriginal) => {
  const actual = await importOriginal<typeof import('../../../api/queues')>()
  return {
    ...actual,
    getStaffQueues: vi.fn(),
    getQueueDashboard: vi.fn(),
    serveNext: vi.fn(),
    skipQueueEntry: vi.fn(),
  }
})

vi.mock('../../../utils/authToken', () => ({
  getTokenPayload: vi.fn(() => ({
    sub: 'staff-user-id',
    email: 'staff@nextturn.dev',
    name: 'Test Staff',
    role: 'Staff',
    tid: TENANT_ID,
    exp: Math.floor(Date.now() / 1000) + 3600,
    iat: Math.floor(Date.now() / 1000),
  })),
  clearToken: vi.fn(),
  getToken: vi.fn(() => 'test-jwt-token'),
}))

const mockGetStaffQueues = vi.mocked(queuesApi.getStaffQueues)
const mockGetQueueDashboard = vi.mocked(queuesApi.getQueueDashboard)
const mockServeNext = vi.mocked(queuesApi.serveNext)
const mockSkipQueueEntry = vi.mocked(queuesApi.skipQueueEntry)

function renderPage() {
  return render(
    <MemoryRouter initialEntries={[`/staff/${TENANT_ID}`]}>
      <Routes>
        <Route path="/staff/:tenantId" element={<StaffDashboardPage />} />
      </Routes>
    </MemoryRouter>
  )
}

beforeEach(() => {
  mockGetStaffQueues.mockReset()
  mockGetQueueDashboard.mockReset()
  mockServeNext.mockReset()
  mockSkipQueueEntry.mockReset()
  window.localStorage.clear()

  vi.spyOn(window, 'prompt').mockReturnValue('No-show')

  mockGetStaffQueues.mockResolvedValue([
    {
      queueId: QUEUE_ID,
      name: 'Main Counter',
      maxCapacity: 50,
      averageServiceTimeSeconds: 180,
      status: 'Active',
      shareableLink: `/queues/${TENANT_ID}/${QUEUE_ID}`,
    },
  ])

  mockGetQueueDashboard.mockResolvedValue({
    queueId: QUEUE_ID,
    queueName: 'Main Counter',
    queueStatus: 'Active',
    waitingCount: 1,
    currentlyServing: null,
    waitingEntries: [
      {
        entryId: 'entry-1',
        ticketNumber: 1,
        joinedAt: '2026-03-01T08:00:00Z',
      },
    ],
  })

  mockServeNext.mockResolvedValue({
    entryId: 'entry-1',
    ticketNumber: 1,
    status: 'Served',
  })

  mockSkipQueueEntry.mockResolvedValue({
    entryId: 'entry-1',
    ticketNumber: 1,
    status: 'NoShow',
  })
})

describe('StaffDashboardPage', () => {
  it('shows onboarding on first load and can restart from settings', async () => {
    const user = userEvent.setup()
    renderPage()

    expect(await screen.findByTestId('onboarding-tour')).toBeInTheDocument()
    await user.click(screen.getByRole('button', { name: /skip tour/i }))
    await waitFor(() => {
      expect(screen.queryByTestId('onboarding-tour')).not.toBeInTheDocument()
    })

    await user.click(screen.getByRole('button', { name: /restart onboarding tour/i }))
    expect(await screen.findByTestId('onboarding-tour')).toBeInTheDocument()
  })

  it('loads queues and dashboard for selected queue', async () => {
    renderPage()

    await waitFor(() => expect(mockGetStaffQueues).toHaveBeenCalledWith(TENANT_ID))
    await waitFor(() => expect(mockGetQueueDashboard).toHaveBeenCalledWith(QUEUE_ID, TENANT_ID))

    expect(await screen.findByText('Main Counter')).toBeInTheDocument()
    expect(screen.getByTestId('queue-selector')).toBeInTheDocument()
    expect(screen.getByTestId('waiting-list')).toBeInTheDocument()
  })

  it('serves queue head and refreshes dashboard', async () => {
    mockGetQueueDashboard
      .mockResolvedValueOnce({
        queueId: QUEUE_ID,
        queueName: 'Main Counter',
        queueStatus: 'Active',
        waitingCount: 1,
        currentlyServing: null,
        waitingEntries: [{ entryId: 'entry-1', ticketNumber: 1, joinedAt: '2026-03-01T08:00:00Z' }],
      })
      .mockResolvedValueOnce({
        queueId: QUEUE_ID,
        queueName: 'Main Counter',
        queueStatus: 'Active',
        waitingCount: 0,
        currentlyServing: null,
        waitingEntries: [],
      })

    const user = userEvent.setup()
    renderPage()

    const btn = await screen.findByTestId('serve-next-btn')
    await user.click(btn)

    expect(mockServeNext).toHaveBeenCalledWith(QUEUE_ID, TENANT_ID)
    await waitFor(() => expect(screen.queryByTestId('current-ticket')).not.toBeInTheDocument())
  })

  it('disables serve/skip actions when queue has no active entries', async () => {
    mockGetQueueDashboard.mockResolvedValue({
      queueId: QUEUE_ID,
      queueName: 'Main Counter',
      queueStatus: 'Active',
      waitingCount: 0,
      currentlyServing: null,
      waitingEntries: [],
    })

    renderPage()

    const serveBtn = await screen.findByTestId('serve-next-btn')
    const skipBtn = await screen.findByTestId('skip-next-btn')

    expect(serveBtn).toBeDisabled()
    expect(skipBtn).toBeDisabled()
  })

  it('skips queue head and sends optional reason', async () => {
    mockGetQueueDashboard
      .mockResolvedValueOnce({
        queueId: QUEUE_ID,
        queueName: 'Main Counter',
        queueStatus: 'Active',
        waitingCount: 0,
        currentlyServing: { entryId: 'entry-1', ticketNumber: 1, joinedAt: '2026-03-01T08:00:00Z' },
        waitingEntries: [],
      })
      .mockResolvedValueOnce({
        queueId: QUEUE_ID,
        queueName: 'Main Counter',
        queueStatus: 'Active',
        waitingCount: 1,
        currentlyServing: null,
        waitingEntries: [{ entryId: 'entry-2', ticketNumber: 2, joinedAt: '2026-03-01T08:01:00Z' }],
      })

    const user = userEvent.setup()
    renderPage()

    const skipBtn = await screen.findByTestId('skip-next-btn')
    await user.click(skipBtn)

    expect(mockSkipQueueEntry).toHaveBeenCalledWith(QUEUE_ID, TENANT_ID, { reason: 'No-show' })
    await waitFor(() => expect(screen.getByTestId('waiting-list')).toBeInTheDocument())
  })
})
