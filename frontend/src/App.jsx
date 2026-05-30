import { useEffect, useMemo, useState } from 'react'
import { getUserPageData } from './api/UserApi'
import { AdminDashboard } from './pages/Admindashboard'
import { StaffDashboard } from './pages/Staffdashboard'
import './App.css'

function normalizeRole(roleName = '') {
  const value = roleName.toLowerCase()
  if (value.includes('admin') || value.includes('quan tri')) return 'admin'
  if (value.includes('manager') || value.includes('quan ly')) return 'manager'
  if (value.includes('staff') || value.includes('nhan vien')) return 'staff'
  return 'default'
}

function App() {
  const [pageData, setPageData] = useState({ users: [], roles: [], branches: [] })
  const [loginForm, setLoginForm] = useState({ username: '', password: '' })
  const [currentUser, setCurrentUser] = useState(() => {
    const savedUser = localStorage.getItem('currentUser')
    return savedUser ? JSON.parse(savedUser) : null
  })
  const [isLoading, setIsLoading] = useState(true)
  const [error, setError] = useState('')

  useEffect(() => {
    async function loadUsers() {
      try {
        setIsLoading(true)
        setError('')
        const data = await getUserPageData()
        setPageData({
          users: data.users ?? [],
          roles: data.roles ?? [],
          branches: data.branches ?? [],
        })
      } catch (err) {
        setError(err.message || 'Không thể tải dữ liệu')
      } finally {
        setIsLoading(false)
      }
    }
    loadUsers()
  }, [])

  const selectedUser = useMemo(
    () =>
      pageData.users.find(
        (user) =>
          user.username?.toLowerCase() === loginForm.username.trim().toLowerCase(),
      ),
    [pageData.users, loginForm.username],
  )

  function handleLoginChange(event) {
    const { name, value } = event.target
    setLoginForm((form) => ({ ...form, [name]: value }))
    setError('')
  }

  function handleSubmit(event) {
    event.preventDefault()
    if (!selectedUser || selectedUser.password !== loginForm.password) {
      setError('Username hoặc password không đúng')
      return
    }
    setCurrentUser(selectedUser)
    localStorage.setItem('currentUser', JSON.stringify(selectedUser))
    setError('')
  }

  function handleLogout() {
    setCurrentUser(null)
    localStorage.removeItem('currentUser')
    setLoginForm({ username: '', password: '' })
  }

  function handleUserUpdated(updatedUser) {
    setCurrentUser(updatedUser)
    localStorage.setItem('currentUser', JSON.stringify(updatedUser))
    setPageData((data) => ({
      ...data,
      users: data.users.map((user) => (user.id === updatedUser.id ? updatedUser : user)),
    }))
  }

  if (currentUser) {
    const roleKey = normalizeRole(currentUser.roleName)
    const dashboardProps = {
      branches: pageData.branches,
      onLogout: handleLogout,
      onUserUpdated: handleUserUpdated,
      roles: pageData.roles,
      user: currentUser,
      users: pageData.users,
    }
    if (roleKey === 'admin' || roleKey === 'manager') {
      return <AdminDashboard {...dashboardProps} />
    }
    return <StaffDashboard {...dashboardProps} />
  }

  return (
    <main className="auth-root">

      {/* ── LEFT PANEL (form) ── */}
      <section className="auth-left">
        <div className="auth-left-inner">

          {/* Brand */}
          <div className="auth-brand">
            <span className="auth-brand-mark">CT</span>
            <div>
              <span className="auth-brand-name">Canteen</span>
              <span className="auth-brand-sub">Management System</span>
            </div>
          </div>

          {/* Heading */}
          <div className="auth-heading">
            <h1>Chào mừng trở lại</h1>
            <p>Đăng nhập để tiếp tục quản lý hệ thống</p>
          </div>

          {/* Form */}
          <div className="auth-form">
            <div className="auth-field">
              <label htmlFor="username">Username</label>
              <div className="auth-input-wrap">
                <span className="auth-input-icon">◈</span>
                <input
                  autoComplete="username"
                  id="username"
                  name="username"
                  onChange={handleLoginChange}
                  placeholder="Nhập username"
                  type="text"
                  value={loginForm.username}
                />
              </div>
            </div>

            <div className="auth-field">
              <label htmlFor="password">Password</label>
              <div className="auth-input-wrap">
                <span className="auth-input-icon">◉</span>
                <input
                  autoComplete="current-password"
                  id="password"
                  name="password"
                  onChange={handleLoginChange}
                  placeholder="Nhập password"
                  type="password"
                  value={loginForm.password}
                />
              </div>
            </div>

            {/* Role preview */}
            <div className="auth-role-row">
              <span className="auth-role-label">Role được phát hiện</span>
              <span className={`auth-role-chip ${selectedUser ? 'active' : ''}`}>
                {selectedUser?.roleName || 'Chưa xác định'}
              </span>
            </div>

            {error && (
              <div className="auth-error">
                <span>⚠</span>
                {error}
              </div>
            )}

            <button
              className="auth-submit"
              disabled={isLoading}
              onClick={handleSubmit}
              type="button"
            >
              {isLoading ? (
                <span className="auth-spinner" />
              ) : (
                <>Đăng nhập →</>
              )}
            </button>
          </div>

        </div>
      </section>

      {/* ── RIGHT PANEL (info) ── */}
      <aside className="auth-right">
        <div className="auth-right-inner">
          <div className="auth-right-copy">
            <p className="auth-eyebrow">Hệ thống quản lý</p>
            <h2>Mời nhân viên vào đúng dashboard theo role</h2>
            <p className="auth-right-desc">
              Hệ thống tự động điều hướng Admin, Manager và Staff đến giao diện phù hợp sau khi đăng nhập.
            </p>
          </div>

          <div className="auth-metrics">
            <MetricCard icon="◈" label="Nhân viên" value={pageData.users.length} accent="#2563eb" />
            <MetricCard icon="⬡" label="Roles"     value={pageData.roles.length}  accent="#0891b2" />
            <MetricCard icon="⊞" label="Chi nhánh" value={pageData.branches.length} accent="#7c3aed" />
          </div>

          <div className="auth-roles-list">
            {[
              { role: 'ADMIN',   desc: 'Toàn quyền quản lý hệ thống',   color: '#1d4ed8' },
              { role: 'MANAGER', desc: 'Quản lý nhân viên & chi nhánh',  color: '#0891b2' },
              { role: 'STAFF',   desc: 'Thao tác nghiệp vụ hàng ngày',   color: '#059669' },
            ].map(({ role, desc, color }) => (
              <div className="auth-role-item" key={role}>
                <span className="auth-role-dot" style={{ background: color }} />
                <div>
                  <strong style={{ color }}>{role}</strong>
                  <span>{desc}</span>
                </div>
              </div>
            ))}
          </div>
        </div>
      </aside>

    </main>
  )
}

function MetricCard({ icon, label, value, accent }) {
  return (
    <div className="auth-metric" style={{ '--ac': accent }}>
      <span className="auth-metric-icon">{icon}</span>
      <div>
        <p className="auth-metric-label">{label}</p>
        <p className="auth-metric-value">{value}</p>
      </div>
    </div>
  )
}

export default App