import { useEffect, useState } from 'react'
import axios from 'axios'
import { getUserPageData, requestPasswordReset, resetPassword } from './api/UserApi'
import { AdminDashboard } from './pages/Admindashboard'
import { StaffDashboard } from './pages/Staffdashboard'
import './App.css'

axios.interceptors.request.use(
  (config) => {
    const token = localStorage.getItem('ACCESS_TOKEN')
    if (token) {
      config.headers.Authorization = `Bearer ${token}`
    }
    return config
  },
  (error) => Promise.reject(error)
)

function normalizeRole(value = '') {
  return value.normalize('NFD').replace(/[\u0300-\u036f]/g, '').toUpperCase()
}

function App() {
  const [pageData, setPageData] = useState({ users: [], roles: [], branches: [] })
  const [loginForm, setLoginForm] = useState({ identifier: '', password: '' })
  const [authMode, setAuthMode] = useState('login')
  const [resetForm, setResetForm] = useState({ identifier: '', otp: '', newPassword: '', confirmPassword: '' })
  const [resetMessage, setResetMessage] = useState(null)
  const [currentUser, setCurrentUser] = useState(() => {
    const savedUser = localStorage.getItem('currentUser')
    return savedUser ? JSON.parse(savedUser) : null
  })
  const [isLoading, setIsLoading] = useState(false)
  const [error, setError] = useState('')

  useEffect(() => {
    async function loadUsers() {
      try {
        const data = await getUserPageData()
        setPageData({
          users: data.users ?? [],
          roles: data.roles ?? [],
          branches: data.branches ?? [],
        })
      } catch (err) {
        console.error('Lỗi Dữ Liệu nền:', err)
      }
    }
    loadUsers()
  }, [])

  function handleLoginChange(event) {
    const { name, value } = event.target
    setLoginForm((form) => ({ ...form, [name]: value }))
    setError('')
  }

  function handleResetChange(event) {
    const { name, value } = event.target
    setResetForm((form) => ({ ...form, [name]: value }))
    setResetMessage(null)
  }

  async function handleSubmit(event) {
    event.preventDefault()
    if (!loginForm.identifier || !loginForm.password) {
      setError('Vui lòng nhập email hoặc số điện thoại và mật khẩu')
      return
    }

    try {
      setIsLoading(true)
      const response = await axios.post('/api/User/login', {
        identifier: loginForm.identifier.trim(),
        password: loginForm.password,
      })

      const { token, user } = response.data
      localStorage.setItem('ACCESS_TOKEN', token)
      localStorage.setItem('currentUser', JSON.stringify(user))
      setCurrentUser(user)
      setError('')
    } catch (err) {
      setError(err.response?.data?.message || 'Tài Khoản Hoặc Mật Khẩu Không Chính Xác')
    } finally {
      setIsLoading(false)
    }
  }

  async function handleRequestOtp(event) {
    event.preventDefault()
    if (!resetForm.identifier.trim()) {
      setResetMessage({ type: 'error', text: 'Nhập User Hoặc Email' })
      return
    }

    try {
      setIsLoading(true)
      const response = await requestPasswordReset(resetForm.identifier.trim())
      setResetMessage({ type: 'success', text: response.message || 'Đã Gửi Mã OTP Về Mail.' })
    } catch (err) {
      setResetMessage({ type: 'error', text: err.response?.data?.message || 'Không Thể Gửi OTP.' })
    } finally {
      setIsLoading(false)
    }
  }

  async function handleResetPassword(event) {
    event.preventDefault()
    if (!resetForm.identifier.trim() || !resetForm.otp.trim() || !resetForm.newPassword) {
      setResetMessage({ type: 'error', text: 'Nhập đầy đủ email/số điện thoại, OTP và mật khẩu mới.' })
      return
    }
    if (resetForm.newPassword.length < 4) {
      setResetMessage({ type: 'error', text: 'Mật Khẩu Mới Tối Thiểu 4 Ký Tự.' })
      return
    }
    if (resetForm.newPassword !== resetForm.confirmPassword) {
      setResetMessage({ type: 'error', text: 'Mật khẩu nhập lại chưa khớp.' })
      return
    }

    try {
      setIsLoading(true)
      const response = await resetPassword({
        identifier: resetForm.identifier.trim(),
        otp: resetForm.otp.trim(),
        newPassword: resetForm.newPassword,
      })
      setLoginForm((form) => ({ ...form, identifier: resetForm.identifier.trim(), password: '' }))
      setResetForm({ identifier: '', otp: '', newPassword: '', confirmPassword: '' })
      setAuthMode('login')
      setError('')
      setResetMessage({ type: 'success', text: response.message || 'Đã đặt lại mật khẩu thành công.' })
    } catch (err) {
      setResetMessage({ type: 'error', text: err.response?.data?.message || 'Không thể đặt lại mật khẩu.' })
    } finally {
      setIsLoading(false)
    }
  }

  function handleLogout() {
    setCurrentUser(null)
    localStorage.removeItem('currentUser')
    localStorage.removeItem('ACCESS_TOKEN')
    setLoginForm({ identifier: '', password: '' })
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
    const roleName = normalizeRole(currentUser.roleName || currentUser.role || '')
    const isStaff = roleName.includes('STAFF') || roleName.includes('NHAN VIEN')
    const dashboardProps = {
      branches: pageData.branches,
      onLogout: handleLogout,
      onUserUpdated: handleUserUpdated,
      roles: pageData.roles,
      user: currentUser,
      users: pageData.users,
    }

    if (isStaff) {
      return <StaffDashboard {...dashboardProps} />
    }

    return <AdminDashboard {...dashboardProps} />
  }

  return (
    <main className="auth-root-simple">
      <div className="auth-card">
        <div className="auth-header">
          <div className="auth-logo">TriMinh</div>
          <h2>Quản lý nhân sự hệ thống căn tin</h2>
          <p>{authMode === 'login' ? 'Đăng Nhập để tiếp tục' : 'Đặt lại mật khẩu bằng OTP email'}</p>
        </div>

        {authMode === 'login' ? (
          <form className="auth-form" onSubmit={handleSubmit}>
            <div className="auth-field">
              <label htmlFor="identifier">Email hoặc số điện thoại</label>
              <input
                autoComplete="username"
                className="auth-input"
                id="identifier"
                name="identifier"
                onChange={handleLoginChange}
                placeholder="Nhập email hoặc số điện thoại"
                type="text"
                value={loginForm.identifier}
              />
            </div>

            <div className="auth-field">
              <label htmlFor="password">Mật khẩu</label>
              <input
                autoComplete="current-password"
                className="auth-input"
                id="password"
                name="password"
                onChange={handleLoginChange}
                placeholder="Nhập password"
                type="password"
                value={loginForm.password}
              />
            </div>

            {error && <div className="auth-error-msg">{error}</div>}
            {resetMessage?.type === 'success' && <div className="auth-success-msg">{resetMessage.text}</div>}

            <button className="auth-submit-btn" disabled={isLoading} type="submit">
              {isLoading ? 'Đang tải...' : 'Đăng Nhập'}
            </button>
            <button className="auth-link-btn" onClick={() => { setAuthMode('reset'); setError(''); setResetMessage(null) }} type="button">
              Quên mật khẩu?
            </button>
          </form>
        ) : (
          <form className="auth-form" onSubmit={handleResetPassword}>
            <div className="auth-field">
              <label htmlFor="identifier">Email hoặc số điện thoại</label>
              <input
                autoComplete="username"
                className="auth-input"
                id="identifier"
                name="identifier"
                onChange={handleResetChange}
                placeholder="Nhập email hoặc số điện thoại"
                type="text"
                value={resetForm.identifier}
              />
            </div>

            <button className="auth-secondary-btn" disabled={isLoading} onClick={handleRequestOtp} type="button">
              {isLoading ? 'Đang gửi...' : 'Gửi mã OTP'}
            </button>

            <div className="auth-field">
              <label htmlFor="otp">Mã OTP</label>
              <input
                autoComplete="one-time-code"
                className="auth-input"
                id="otp"
                inputMode="numeric"
                maxLength={6}
                name="otp"
                onChange={handleResetChange}
                placeholder="Nhập mã 6 số"
                type="text"
                value={resetForm.otp}
              />
            </div>

            <div className="auth-field">
              <label htmlFor="newPassword">Mật khẩu mới</label>
              <input
                autoComplete="new-password"
                className="auth-input"
                id="newPassword"
                name="newPassword"
                onChange={handleResetChange}
                placeholder="Nhập mật khẩu mới"
                type="password"
                value={resetForm.newPassword}
              />
            </div>

            <div className="auth-field">
              <label htmlFor="confirmPassword">Nhập lại mật khẩu mới</label>
              <input
                autoComplete="new-password"
                className="auth-input"
                id="confirmPassword"
                name="confirmPassword"
                onChange={handleResetChange}
                placeholder="Nhập lại mật khẩu mới"
                type="password"
                value={resetForm.confirmPassword}
              />
            </div>

            {resetMessage && (
              <div className={resetMessage.type === 'success' ? 'auth-success-msg' : 'auth-error-msg'}>
                {resetMessage.text}
              </div>
            )}

            <button className="auth-submit-btn" disabled={isLoading} type="submit">
              {isLoading ? 'Đang lưu...' : 'Tạo mật khẩu mới'}
            </button>
            <button className="auth-link-btn" onClick={() => { setAuthMode('login'); setResetMessage(null) }} type="button">
              Quay Lại Đăng Nhập
            </button>
          </form>
        )}
      </div>
    </main>
  )
}

export default App
