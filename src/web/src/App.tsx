/**
 * App — root router.
 *
 * Routes:
 *  /                    → WelcomePage (landing)
 *  /register/:tenantId  → RegisterPage
 *  /login/:tenantId     → LoginPage       (NT-11-10)
 *  /dashboard/:tenantId → DashboardPage   (NT-11-11 — protected, placeholder until NT-11-11)
 *  *                    → redirect to /
 */
import { Routes, Route, Navigate } from 'react-router-dom'
import { WelcomePage } from './pages/Welcome'
import { RegisterPage } from './pages/Register'
import { LoginPage } from './pages/Login'

function App() {
  return (
    <Routes>
      <Route path="/" element={<WelcomePage />} />
      <Route path="/register/:tenantId" element={<RegisterPage />} />
      <Route path="/login/:tenantId" element={<LoginPage />} />
      {/* /dashboard — stub for NT-11-11; replaced with ProtectedRoute + DashboardPage */}
      <Route path="/dashboard/:tenantId" element={<Navigate to="/" replace />} />
      <Route path="*" element={<Navigate to="/" replace />} />
    </Routes>
  )
}

export default App
