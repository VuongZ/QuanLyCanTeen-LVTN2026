import { useEffect, useMemo, useState } from 'react'
import { getUserPageData, updateUser } from './api/UserApi'
import './App.css'

const roleProfiles = {
  admin: {
    title: 'Quan tri he thong',
    accent: 'admin',
    actions: ['Quan ly nguoi dung', 'Phan quyen', 'Theo doi chi nhanh'],
  },
  manager: {
    title: 'Quan ly chi nhanh',
    accent: 'manager',
    actions: ['Lich lam viec', 'Ton kho', 'Bao cao ca'],
  },
  staff: {
    title: 'Nhan vien',
    accent: 'staff',
    actions: ['Cham cong', 'Ca lam hien tai', 'Yeu cau ho tro'],
  },
  default: {
    title: 'Bang dieu khien',
    accent: 'default',
    actions: ['Thong tin ca nhan', 'Chi nhanh', 'Lich lam viec'],
  },
}

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
        setError(err.message || 'Khong the tai du lieu nguoi dung')
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
  }

  function handleSubmit(event) {
    event.preventDefault()

    if (!selectedUser || selectedUser.password !== loginForm.password) {
      setError('Username hoac password khong dung')
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
    return (
      <Dashboard
        branches={pageData.branches}
        onLogout={handleLogout}
        onUserUpdated={handleUserUpdated}
        roles={pageData.roles}
        user={currentUser}
        users={pageData.users}
      />
    )
  }

  return (
    <main className="auth-page redesigned-auth">
      <section className="login-panel" aria-labelledby="login-title">
        <div className="login-header">
          <div className="brand-mark">CT</div>
          <div>
            <p className="eyebrow">Canteen Management</p>
            <h1 id="login-title">Đăng Nhập</h1>
          </div>
        </div>

        <form className="login-form" onSubmit={handleSubmit}>
          <label htmlFor="username">Username</label>
          <input
            autoComplete="username"
            id="username"
            name="username"
            onChange={handleLoginChange}
            placeholder="Nhap username"
            type="text"
            value={loginForm.username}
          />

          <label htmlFor="password">Password</label>
          <input
            autoComplete="current-password"
            id="password"
            name="password"
            onChange={handleLoginChange}
            placeholder="Nhap password"
            type="password"
            value={loginForm.password}
          />

          <div className="selected-role">
            <span>Role</span>
            <strong>{selectedUser?.roleName || 'Chưa Xác Định'}</strong>
          </div>

          {error && <p className="form-error">{error}</p>}

          <button disabled={isLoading} type="submit">
            {isLoading ? 'Dang tai...' : 'Đăng nhập'}
          </button>
        </form>
      </section>

      <aside className="login-summary" aria-label="Thong tin he thong">
        <div className="summary-copy">
          <p className="eyebrow">Role dashboard</p>
          <h2>Mời Nhân Viên vào đúng dashboard theo role của mình</h2>
        </div>
        <div className="summary-metrics">
          <Metric label="Users" value={pageData.users.length} />
          <Metric label="Roles" value={pageData.roles.length} />
          <Metric label="Branches" value={pageData.branches.length} />
        </div>
      </aside>
    </main>
  )
}

function Dashboard({ branches, onLogout, onUserUpdated, roles, user, users }) {
  const roleKey = normalizeRole(user.roleName)
  const profile = roleProfiles[roleKey]
  const branch = branches.find((item) => item.id === user.branchId)
  const usersInBranch = users.filter((item) => item.branchId === user.branchId)

  return (
    <main className={`dashboard dashboard-${profile.accent}`}>
      <header className="topbar">
        <div>
          <p className="eyebrow">{user.roleName || 'Role'}</p>
          <h1>{profile.title}</h1>
        </div>
        <button className="secondary-button" onClick={onLogout} type="button">
          Dang xuat
        </button>
      </header>

      <section className="profile-strip">
        <span className="avatar">{getInitials(user.fullName || user.username)}</span>
        <div>
          <h2>{user.fullName || user.username}</h2>
          <p>{branch?.name || user.branchName || 'Chua gan chi nhanh'}</p>
        </div>
      </section>

      <section className="metric-grid" aria-label="Chi so tong quan">
        <Metric label="Nguoi dung" value={users.length} />
        <Metric label="Role" value={roles.length} />
        <Metric label="Cung chi nhanh" value={usersInBranch.length} />
      </section>

      <section className="work-area">
        <div className="primary-panel">
          <div className="section-heading">
            <p className="eyebrow">Cong viec</p>
            <h2>Chuc nang theo role</h2>
          </div>
          <div className="action-list">
            {profile.actions.map((action) => (
              <button key={action} type="button">
                <span>{action}</span>
                <strong>Mo</strong>
              </button>
            ))}
          </div>
        </div>

        <div className="info-panel">
          <div className="section-heading">
            <p className="eyebrow">Tai khoan</p>
            <h2>Thong tin User</h2>
          </div>
          <dl>
            <div>
              <dt>Username</dt>
              <dd>{user.username}</dd>
            </div>
            <div>
              <dt>Role</dt>
              <dd>{user.roleName || 'Chua co'}</dd>
            </div>
            <div>
              <dt>Ngay vao lam</dt>
              <dd>{formatDate(user.hireDate)}</dd>
            </div>
          </dl>
          <PasswordForm onUserUpdated={onUserUpdated} user={user} />
        </div>
      </section>
    </main>
  )
}

function PasswordForm({ onUserUpdated, user }) {
  const [form, setForm] = useState({
    currentPassword: '',
    newPassword: '',
    confirmPassword: '',
  })
  const [status, setStatus] = useState('')
  const [isSaving, setIsSaving] = useState(false)

  function handleChange(event) {
    const { name, value } = event.target
    setForm((valueForm) => ({ ...valueForm, [name]: value }))
  }

  async function handleSubmit(event) {
    event.preventDefault()
    setStatus('')

    if (form.currentPassword !== user.password) {
      setStatus('Password hien tai khong dung')
      return
    }

    if (form.newPassword.length < 4) {
      setStatus('Password moi can toi thieu 4 ky tu')
      return
    }

    if (form.newPassword !== form.confirmPassword) {
      setStatus('Nhap lai password moi chua khop')
      return
    }

    try {
      setIsSaving(true)
      const updatedUser = { ...user, password: form.newPassword }
      await updateUser(user.id, updatedUser)
      onUserUpdated(updatedUser)
      setForm({ currentPassword: '', newPassword: '', confirmPassword: '' })
      setStatus('Da cap nhat password')
    } catch (err) {
      setStatus(err.message || 'Khong the cap nhat password')
    } finally {
      setIsSaving(false)
    }
  }

  return (
    <form className="password-form" onSubmit={handleSubmit}>
      <div className="section-heading">
        <p className="eyebrow">Bao mat</p>
        <h2>Doi password</h2>
      </div>
      <input
        autoComplete="current-password"
        name="currentPassword"
        onChange={handleChange}
        placeholder="Password hien tai"
        type="password"
        value={form.currentPassword}
      />
      <input
        autoComplete="new-password"
        name="newPassword"
        onChange={handleChange}
        placeholder="Password moi"
        type="password"
        value={form.newPassword}
      />
      <input
        autoComplete="new-password"
        name="confirmPassword"
        onChange={handleChange}
        placeholder="Nhap lai password moi"
        type="password"
        value={form.confirmPassword}
      />
      {status && <p className="form-status">{status}</p>}
      <button disabled={isSaving} type="submit">
        {isSaving ? 'Dang luu...' : 'Cap nhat password'}
      </button>
    </form>
  )
}

function Metric({ label, value }) {
  return (
    <div className="metric">
      <span>{label}</span>
      <strong>{value}</strong>
    </div>
  )
}

function getInitials(name = '') {
  return name
    .split(' ')
    .filter(Boolean)
    .slice(-2)
    .map((part) => part[0])
    .join('')
    .toUpperCase()
}

function formatDate(value) {
  if (!value) return 'Chua co'
  return new Intl.DateTimeFormat('vi-VN').format(new Date(value))
}

export default App
