import { useEffect, useMemo, useState } from 'react'
import { getUserPageData } from './api/UserApi'
import { AdminDashboard } from './pages/Admindashboard'
import './App.css'

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
    const dashboardProps = {
      branches: pageData.branches,
      onLogout: handleLogout,
      onUserUpdated: handleUserUpdated,
      roles: pageData.roles,
      user: currentUser,
      users: pageData.users,
    }
    return <AdminDashboard {...dashboardProps} />
  }

 return (
    <main className="auth-root-simple">
      <div className="auth-card">
        {/* Tiêu đề */}
        <div className="auth-header">
          <div className="auth-logo">TriMinh</div>
          <h2>Quản Lý Nhân Sự Hệ Thống Căn Tin</h2>
          <p>Đăng nhập để tiếp tục</p>
        </div>

        {/* Form đăng nhập */}
        <form className="auth-form" onSubmit={handleSubmit}>
          <div className="auth-field">
            <label htmlFor="username">Tài khoản</label>
            <input
              autoComplete="username"
              id="username"
              name="username"
              onChange={handleLoginChange}
              placeholder="Nhập username"
              type="text"
              value={loginForm.username}
              className="auth-input"
            />
          </div>

          <div className="auth-field">
            <label htmlFor="password">Mật khẩu</label>
            <input
              autoComplete="current-password"
              id="password"
              name="password"
              onChange={handleLoginChange}
              placeholder="Nhập password"
              type="password"
              value={loginForm.password}
              className="auth-input"
            />
          </div>


          {/* Hiển thị lỗi */}
          {error && <div className="auth-error-msg">⚠ {error}</div>}

          {/* Nút đăng nhập */}
          <button
            className="auth-submit-btn"
            disabled={isLoading}
            type="submit"
          >
            {isLoading ? 'Đang tải...' : 'Đăng nhập'}
          </button>
        </form>
      </div>
    </main>
  )
}

export default App
