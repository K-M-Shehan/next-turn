import { useState } from 'react'
import { Link } from 'react-router-dom'
import { resolveMemberLogin } from '../../api/organisations'
import type { MemberWorkspaceOption } from '../../api/organisations'
import type { ApiError } from '../../types/api'
import logoImg from '../../assets/nextTurn-logo.png'
import styles from './OrgLoginLookupPage.module.css'

export function OrgLoginLookupPage() {
  const [email, setEmail] = useState('')
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [results, setResults] = useState<MemberWorkspaceOption[]>([])
  const singleResult = results.length === 1 ? results[0] : null

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault()
    if (!email.trim()) return

    setLoading(true)
    setError(null)
    setResults([])

    try {
      const resolved = await resolveMemberLogin(email.trim())
      if (resolved.length === 0) {
        setError('No staff/admin workspace was found for this email.')
        return
      }

      setResults(resolved)
    } catch (err) {
      const apiErr = err as ApiError
      setError(apiErr.detail ?? 'Could not resolve organisation login links for this email.')
    } finally {
      setLoading(false)
    }
  }

  return (
    <div className={styles.page}>
      <header className={styles.header}>
        <Link to="/" className={styles.logoLink}>
          <img src={logoImg} alt="NextTurn" className={styles.logo} />
        </Link>
      </header>

      <main className={styles.main}>
        <section className={styles.card}>
          <h1 className={styles.title}>Find Organisation Login</h1>
          <p className={styles.subtitle}>
            Enter your work email to get your staff/admin workspace login link.
          </p>

          <form onSubmit={handleSubmit} className={styles.form} noValidate>
            <label className={styles.label} htmlFor="work-email">Work email</label>
            <input
              id="work-email"
              className={styles.input}
              type="email"
              value={email}
              onChange={e => setEmail(e.target.value)}
              placeholder="staff@yourorg.com"
              autoComplete="email"
            />

            <button
              type="submit"
              className={styles.submitBtn}
              disabled={loading || !email.trim()}
            >
              {loading ? 'Finding...' : 'Find Login Links'}
            </button>
          </form>

          {error && (
            <p className={styles.errorText} role="alert">{error}</p>
          )}

          {singleResult && (
            <div className={styles.successBox} role="status">
              <p>
                <strong>{singleResult.organisationName}</strong>
              </p>
              <p>
                Login URL: <strong>{window.location.origin}{singleResult.loginPath}</strong>
              </p>
              <Link to={singleResult.loginPath} className={styles.loginBtn}>
                Go to Organisation Login
              </Link>
            </div>
          )}

          {results.length > 1 && (
            <div className={styles.multiBox} role="status">
              <p className={styles.multiTitle}>Multiple workspaces found</p>
              <p className={styles.multiSubtitle}>Choose your organisation to continue.</p>
              <ul className={styles.workspaceList}>
                {results.map(result => (
                  <li key={`${result.organisationId}-${result.role}`} className={styles.workspaceItem}>
                    <div className={styles.workspaceMeta}>
                      <strong>{result.organisationName}</strong>
                      <span>{result.role}</span>
                    </div>
                    <Link to={result.loginPath} className={styles.loginBtn}>
                      Open Login
                    </Link>
                  </li>
                ))}
              </ul>
            </div>
          )}

          <p className={styles.helpText}>
            If you still cannot access your account, contact your organisation owner or system admin.
          </p>
        </section>
      </main>
    </div>
  )
}
