import { describe, it, expect, vi, beforeEach } from 'vitest'
import { render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter } from 'react-router-dom'

import { OrgLoginLookupPage } from '../OrgLoginLookupPage'
import * as organisationsApi from '../../../api/organisations'

vi.mock('../../../api/organisations', async (importOriginal) => {
  const actual = await importOriginal<typeof organisationsApi>()
  return { ...actual, resolveMemberLogin: vi.fn() }
})

const mockResolveMemberLogin = vi.mocked(organisationsApi.resolveMemberLogin)

function renderPage() {
  return render(
    <MemoryRouter>
      <OrgLoginLookupPage />
    </MemoryRouter>
  )
}

describe('OrgLoginLookupPage', () => {
  beforeEach(() => {
    mockResolveMemberLogin.mockReset()
  })

  it('renders staff/admin member wording and work email label', () => {
    renderPage()

    expect(screen.getByRole('heading', { name: /find organisation login/i })).toBeInTheDocument()
    expect(screen.getByText(/staff\/admin workspace login link/i)).toBeInTheDocument()
    expect(screen.getByLabelText(/work email/i)).toBeInTheDocument()
  })

  it('resolves a single workspace and shows a direct login link', async () => {
    mockResolveMemberLogin.mockResolvedValueOnce([
      {
        organisationId: '11111111-1111-1111-1111-111111111111',
        organisationName: 'Acme Health',
        slug: 'acme-health',
        loginPath: '/login/o/acme-health',
        role: 'Staff',
      },
    ])

    renderPage()
    const user = userEvent.setup()

    await user.type(screen.getByLabelText(/work email/i), 'staff@acme.com')
    await user.click(screen.getByRole('button', { name: /find login links/i }))

    await waitFor(() => {
      expect(mockResolveMemberLogin).toHaveBeenCalledWith('staff@acme.com')
      expect(screen.getByRole('link', { name: /go to organisation login/i })).toHaveAttribute('href', '/login/o/acme-health')
    })
  })

  it('shows workspace selection when multiple workspaces are returned', async () => {
    mockResolveMemberLogin.mockResolvedValueOnce([
      {
        organisationId: '11111111-1111-1111-1111-111111111111',
        organisationName: 'Acme Health',
        slug: 'acme-health',
        loginPath: '/login/o/acme-health',
        role: 'Staff',
      },
      {
        organisationId: '22222222-2222-2222-2222-222222222222',
        organisationName: 'City Office',
        slug: 'city-office',
        loginPath: '/login/o/city-office',
        role: 'OrgAdmin',
      },
    ])

    renderPage()
    const user = userEvent.setup()

    await user.type(screen.getByLabelText(/work email/i), 'member@example.com')
    await user.click(screen.getByRole('button', { name: /find login links/i }))

    await waitFor(() => {
      expect(screen.getByText(/multiple workspaces found/i)).toBeInTheDocument()
      expect(screen.getAllByRole('link', { name: /open login/i })).toHaveLength(2)
    })

    const openLinks = screen.getAllByRole('link', { name: /open login/i })
    expect(openLinks).toHaveLength(2)
    expect(openLinks[0]).toHaveAttribute('href', '/login/o/acme-health')
    expect(openLinks[1]).toHaveAttribute('href', '/login/o/city-office')
  })

  it('shows a friendly message when no workspaces are returned', async () => {
    mockResolveMemberLogin.mockResolvedValueOnce([])

    renderPage()
    const user = userEvent.setup()

    await user.type(screen.getByLabelText(/work email/i), 'unknown@example.com')
    await user.click(screen.getByRole('button', { name: /find login links/i }))

    await waitFor(() => {
      expect(screen.getByRole('alert')).toHaveTextContent(/no staff\/admin workspace was found/i)
    })
  })

  it('shows API detail when lookup fails', async () => {
    mockResolveMemberLogin.mockRejectedValueOnce({
      status: 400,
      detail: 'Could not resolve this member email.',
      raw: {},
    })

    renderPage()
    const user = userEvent.setup()

    await user.type(screen.getByLabelText(/work email/i), 'bad@example.com')
    await user.click(screen.getByRole('button', { name: /find login links/i }))

    await waitFor(() => {
      expect(screen.getByRole('alert')).toHaveTextContent(/could not resolve this member email/i)
    })
  })
})
