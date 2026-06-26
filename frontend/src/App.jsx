import { useEffect, useState } from 'react'
import axios from 'axios' // 👉 Thêm import axios
import { getUserPageData } from './api/UserApi'
import { AdminDashboard } from './pages/Admindashboard'
import { StaffDashboard } from './pages/Staffdashboard'
import './App.css'

// =====================================================================
// 👉 BỘ CHẶN AXIOS: Tự động đính kèm ACCESS_TOKEN vào mọi API gửi đi
// =====================================================================
axios.interceptors.request.use(
  (config) => {
    const token = localStorage.getItem('ACCESS_TOKEN')
    if (token) {
      config.headers['Authorization'] = `Bearer ${token}`
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
  const [loginForm, setLoginForm] = useState({ username: '', password: '' })
  
  const [currentUser, setCurrentUser] = useState(() => {
    const savedUser = localStorage.getItem('currentUser')
    return savedUser ? JSON.parse(savedUser) : null
  })
  
  const [isLoading, setIsLoading] = useState(false) // Đổi mặc định thành false để không bị quay vòng vòng lúc mới mở
  const [error, setError] = useState('')

  // Vẫn giữ nguyên logic tải dữ liệu cho Dashboard
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
        console.error("Lỗi tải dữ liệu nền:", err)
      }
    }
    loadUsers()
  }, [])

  function handleLoginChange(event) {
    const { name, value } = event.target
    setLoginForm((form) => ({ ...form, [name]: value }))
    setError('')
  }

  // =====================================================================
  // 👉 NÂNG CẤP: GỌI API ĐĂNG NHẬP THẬT XUỐNG BACKEND (JWT)
  // =====================================================================
  async function handleSubmit(event) {
    event.preventDefault()
    
    if (!loginForm.username || !loginForm.password) {
      setError('Vui lòng nhập đầy đủ tài khoản và mật khẩu')
      return
    }

    try {
      setIsLoading(true)
      
      // Gọi API Login xuống C#
      const response = await axios.post('/api/User/login', {
        username: loginForm.username.trim(),
        password: loginForm.password
      })

      const { token, user } = response.data

      // Lưu Token (Chìa khóa mở các API) và Thông tin User vào Local Storage
      localStorage.setItem('ACCESS_TOKEN', token)
      localStorage.setItem('currentUser', JSON.stringify(user))
      
      setCurrentUser(user)
      setError('')
      
    } catch (err) {
      // Bắt lỗi 401 từ Backend và hiển thị lên UI
      setError(err.response?.data?.message || 'Tài khoản hoặc mật khẩu không chính xác')
    } finally {
      setIsLoading(false)
    }
  }

  // =====================================================================
  // 👉 NÂNG CẤP: XÓA CẢ TOKEN KHI ĐĂNG XUẤT
  // =====================================================================
  function handleLogout() {
    setCurrentUser(null)
    localStorage.removeItem('currentUser')
    localStorage.removeItem('ACCESS_TOKEN') // Xóa sạch Token để khóa cửa lại
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