import { useEffect, useMemo, useState } from 'react'
import { getUserPageData } from './api/UserApi'
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
  const [username, setUsername] = useState('')
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

  const userOptions = useMemo(
    () =>
      pageData.users.map((user) => ({
        ...user,
        label: `${user.fullName || user.username} - ${user.roleName || 'Chua co role'}`,
      })),
    [pageData.users],
  )

  const selectedUser = useMemo(
    () =>
      pageData.users.find(
        (user) => user.username?.toLowerCase() === username.trim().toLowerCase(),
      ),
    [pageData.users, username],
  )

  function handleSubmit(event) {
    event.preventDefault()

    if (!selectedUser) {
      setError('Khong tim thay username trong danh sach User')
      return
    }

    setCurrentUser(selectedUser)
    localStorage.setItem('currentUser', JSON.stringify(selectedUser))
    setError('')
  }

  function handleLogout() {
    setCurrentUser(null)
    localStorage.removeItem('currentUser')
    setUsername('')
  }

  if (currentUser) {
    return (
      <Dashboard
        branches={pageData.branches}
        onLogout={handleLogout}
        roles={pageData.roles}
        user={currentUser}
        users={pageData.users}
      />
    )
  }

  return (
    <main className="auth-page">
      <section className="login-panel" aria-labelledby="login-title">
        <div className="brand-mark">CT</div>
        <div>
          <p className="eyebrow">Canteen Management</p>
          <h1 id="login-title">Dang nhap</h1>
        </div>

        <form className="login-form" onSubmit={handleSubmit}>
          <label htmlFor="username">Username</label>
          <input
            autoComplete="username"
            id="username"
            list="usernames"
            onChange={(event) => setUsername(event.target.value)}
            placeholder="Nhap username"
            type="text"
            value={username}
          />
          <datalist id="usernames">
            {userOptions.map((user) => (
              <option key={user.id} value={user.username}>
                {user.label}
              </option>
            ))}
          </datalist>

          <div className="selected-role">
            <span>Role</span>
            <strong>{selectedUser?.roleName || 'Chua chon user'}</strong>
          </div>

          {error && <p className="form-error">{error}</p>}

          <button disabled={isLoading} type="submit">
            {isLoading ? 'Dang tai...' : 'Vao dashboard'}
          </button>
        </form>
      </section>

      <aside className="login-summary" aria-label="Thong tin he thong">
        <Metric label="Users" value={pageData.users.length} />
        <Metric label="Roles" value={pageData.roles.length} />
        <Metric label="Branches" value={pageData.branches.length} />
      </aside>
    </main>
  )
}

function Dashboard({ branches, onLogout, roles, user, users }) {
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
        <div>
          <span className="avatar">{getInitials(user.fullName || user.username)}</span>
        </div>
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
        </div>
      </section>
    </main>
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
