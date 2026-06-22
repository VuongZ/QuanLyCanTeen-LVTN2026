import { useState, useEffect } from 'react'
import { updateUser } from '../api/UserApi'
import { getAllBranches, createBranch, updateBranch, deleteBranch } from '../api/BranchApi'
import { getAllShifts, createShift, updateShift, deleteShift } from '../api/ShiftApi'
import { getAllPeriods, createPeriod, updatePeriod, deletePeriod } from '../api/PeriodApi'
import { EmployeeQrCard, UnifiedScheduleTab } from './Staffdashboard'
import { Html5QrcodeScanner } from 'html5-qrcode'
import axios from 'axios'
import './css/admindashboard.css'

// ==========================================
// CÁC HÀM TIỆN ÍCH DÙNG CHUNG
// ==========================================
function getInitials(name = '') {
  return name
    .split(' ')
    .filter(Boolean)
    .slice(-2)
    .map((p) => p[0])
    .join('')
    .toUpperCase()
}

function formatDate(value) {
  if (!value) return '—'
  return new Intl.DateTimeFormat('vi-VN').format(new Date(value))
}

function normalizeText(value = '') {
  return value.normalize('NFD').replace(/[\u0300-\u036f]/g, '').toUpperCase()
}

const EMPTY_FORM = {
  username: '',
  fullName: '',
  password: '',
  branchId: '',
  branchName: '',
  roleId: '',
  roleName: '',
  hireDate: '',
}

const ROLE_COLORS = {
  ADMIN: { bg: '#fef3c7', color: '#92400e' },
  MANAGER: { bg: '#dbeafe', color: '#1e40af' },
  STAFF: { bg: '#dcfce7', color: '#166534' },
}

// ==========================================
// COMPONENT CHÍNH: ADMIN DASHBOARD
// ==========================================
export function AdminDashboard({ onLogout, onUserUpdated, roles, user, users: initUsers }) {
  // --- KIỂM TRA QUYỀN ĐĂNG NHẬP ---
  const rawRoleName = normalizeText(user.roleName || '')
  const roleName = rawRoleName.includes('ADMIN') || rawRoleName.includes('QUAN TRI')
    ? 'ADMIN'
    : rawRoleName.includes('MANAGER') || rawRoleName.includes('QUAN LY')
      ? 'MANAGER'
      : rawRoleName.includes('STAFF') || rawRoleName.includes('NHAN VIEN')
        ? 'STAFF'
        : rawRoleName
  const isAdmin = roleName === 'ADMIN'
  const isManager = roleName === 'MANAGER'
  const isStaff = roleName === 'STAFF'
  const defaultTab = isStaff ? 'staffSchedule' : isManager ? 'periods' : 'overview'

  // Mặc định: Manager vào thẳng tab periods, Admin vào Tổng quan overview
  const [activeTab, setActiveTab] = useState(defaultTab)
  const [users, setUsers] = useState(initUsers)
  const [branches, setBranches] = useState([])

  const [modal, setModal] = useState(null)
  const [modalUser, setModalUser] = useState(null)
  const [form, setForm] = useState(EMPTY_FORM)
  const [formErr, setFormErr] = useState('')
  const [saving, setSaving] = useState(false)

  const [search, setSearch] = useState('')
  const [filterRole, setFilterRole] = useState('ALL')
  const [sortCol, setSortCol] = useState('fullName')
  const [sortDir, setSortDir] = useState('asc')

  const [isMenuOpen, setIsMenuOpen] = useState(false)
  const [selectedUser, setSelectedUser] = useState(null)

  useEffect(() => {
    getAllBranches()
      .then((data) => {
        setBranches(Array.isArray(data) ? data : [])
      })
      .catch(() => {
        setBranches([])
      })
  }, [])

  const branch = branches.find((b) => b.id === user.branchId)
  const allowedTabs = isAdmin
    ? ['overview', 'users', 'branches', 'systemSchedule', 'account']
    : isManager
      ? ['periods', 'scanQr', 'account']
      : isStaff
        ? ['staffSchedule', 'account']
        : ['account']
  const activeRoleTab = allowedTabs.includes(activeTab) ? activeTab : allowedTabs[0]

  const displayed = users
    .filter((u) => {
      const matchSearch = [u.fullName, u.username, u.branchName].some((v) =>
        v?.toLowerCase().includes(search.toLowerCase())
      )
      const matchRole = filterRole === 'ALL' || u.roleName?.toUpperCase() === filterRole
      return matchSearch && matchRole
    })
    .sort((a, b) => {
      const va = (a[sortCol] || '').toString().toLowerCase()
      const vb = (b[sortCol] || '').toString().toLowerCase()
      return sortDir === 'asc' ? va.localeCompare(vb) : vb.localeCompare(va)
    })

  function toggleSort(col) {
    if (sortCol === col) {
      setSortDir((d) => (d === 'asc' ? 'desc' : 'asc'))
    } else {
      setSortCol(col)
      setSortDir('asc')
    }
  }

  function SortIcon({ col }) {
    if (sortCol !== col) {
      return <span className="sd-sort-icon sd-sort-none">↕</span>
    }
    return <span className="sd-sort-icon">{sortDir === 'asc' ? '↑' : '↓'}</span>
  }

  function openAdd() {
    setForm(EMPTY_FORM)
    setFormErr('')
    setModal('add')
  }

  function openEdit(u) {
    setForm({ ...u })
    setFormErr('')
    setModalUser(u)
    setModal('edit')
  }

  function openDelete(u) {
    setModalUser(u)
    setFormErr('')
    setModal('delete')
  }

  function closeModal() {
    setModal(null)
    setModalUser(null)
  }

  function handleFormChange(e) {
    const { name, value } = e.target
    setForm((f) => {
      const next = { ...f, [name]: value }
      if (name === 'branchId') {
        const b = branches.find((b) => String(b.id) === value)
        next.branchName = b?.name || b?.branchName || ''
      }
      if (name === 'roleId') {
        const r = roles.find((r) => String(r.id) === value)
        next.roleName = r?.roleName || ''
      }
      return next
    })
  }

  async function handleSaveAdd() {
    if (!form.username || !form.fullName || !form.password) {
      return setFormErr('Vui lòng điền đầy đủ username, họ tên, password')
    }
    setSaving(true)
    setFormErr('')
    try {
      const res = await axios.post('/api/User', form)
      setUsers((prev) => [...prev, res.data])
      closeModal()
    } catch (err) {
      setFormErr(err.message || 'Không thể thêm nhân viên')
    } finally {
      setSaving(false)
    }
  }

  async function handleSaveEdit() {
    if (!form.username || !form.fullName) {
      return setFormErr('Username và họ tên không được để trống')
    }
    setSaving(true)
    setFormErr('')
    try {
      await updateUser(form.id, form)
      setUsers((prev) => prev.map((u) => (u.id === form.id ? { ...u, ...form } : u)))
      if (selectedUser && selectedUser.id === form.id) {
        setSelectedUser({ ...selectedUser, ...form })
      }
      if (form.id === user.id) {
        onUserUpdated({ ...user, ...form })
      }
      closeModal()
    } catch (err) {
      setFormErr(err.message || 'Không thể cập nhật')
    } finally {
      setSaving(false)
    }
  }

  async function handleDelete() {
    setSaving(true)
    try {
      await axios.delete(`/api/User/${modalUser.id}`)
      setUsers((prev) => prev.filter((u) => u.id !== modalUser.id))
      if (selectedUser && selectedUser.id === modalUser.id) {
        setSelectedUser(null)
      }
      closeModal()
    } catch (err) {
      setFormErr(err.message || 'Không thể xóa')
    } finally {
      setSaving(false)
    }
  }

  const countByRole = (r) => users.filter((u) => u.roleName?.toUpperCase() === r).length

  const getHeaderInfo = () => {
    switch (activeRoleTab) {
      case 'overview':
        return { eyebrow: 'Hệ thống', title: 'Tổng quan' }
      case 'users':
        return { eyebrow: 'Quản lý', title: selectedUser ? 'Hồ sơ nhân viên' : 'Nhân sự' }
      case 'account':
        return { eyebrow: 'Cài đặt', title: 'Tài khoản' }
      case 'staffSchedule':
        return { eyebrow: 'Cong viec', title: 'Lich & Dang ky ca' }
      case 'branches':
        return { eyebrow: 'Hệ thống', title: 'Quản lý Cơ sở' }
      case 'periods':
        return { eyebrow: 'Lịch trình', title: 'Đợt đăng ký ca' }
      case 'scanQr':
        return { eyebrow: 'Cham cong', title: 'Quet QR nhan vien' }
      case 'systemSchedule':
        return { eyebrow: 'Giám sát', title: 'Lịch làm các cơ sở' }
      default:
        return { eyebrow: '', title: '' }
    }
  }

  const headerInfo = getHeaderInfo()

  // 👉 DANH SÁCH MENU ĐÃ PHÂN CHIA QUYỀN RẠCH RÒI THEO Ý BẠN:
  const NAV_ITEMS = [
    {
      id: 'overview',
      icon: '⬡',
      label: 'Tổng quan',
    },
    ...(isAdmin
      ? [
          // ── DANH SÁCH TAB DÀNH RIÊNG CHO ADMIN ──
          {
            id: 'users',
            icon: '◈',
            label: 'Nhân viên',
          },
          {
            id: 'branches',
            icon: '🏢',
            label: 'Cơ sở',
          },
          {
            id: 'systemSchedule',
            icon: '🗓️',
            label: 'Lịch các cơ sở', // Tab độc quyền để Admin đi xem lịch chốt
          },
        ]
      : [
          // ── DANH SÁCH TAB DÀNH RIÊNG CHO MANAGER ──
          {
            id: 'periods',
            icon: '📅',
            label: 'Đợt đăng ký', // Manager độc quyền mở đợt và duyệt ca
          },
        ]),
    {
      id: 'scanQr',
      icon: 'Q',
      label: 'Quet QR',
    },
    {
      id: 'staffSchedule',
      icon: 'S',
      label: 'Lich & Dang ky',
    },
    {
      id: 'account',
      icon: '◎',
      label: 'Tài khoản',
    },
  ]

  const allowedNavItems = NAV_ITEMS.filter((item) => allowedTabs.includes(item.id))

  return (
    <div className="sd-root sd-root--left-nav">
      <header className="sd-topbar">
        <div className="sd-brand">
          <button className="sd-hamburger" onClick={() => setIsMenuOpen(true)}>
            ☰
          </button>
          <span className="sd-brand-icon">CT</span>
          <span className="sd-brand-name">Canteen Admin</span>
        </div>
        <div className="sd-flex-center">
          <div className="sd-branch-badge" style={{ marginRight: 12 }}>
            {user.roleName}
          </div>
          <button className="sd-logout-btn" onClick={onLogout}>
            <span>Đăng xuất</span> ↩
          </button>
        </div>
      </header>

      <div className="sd-layout">
        {isMenuOpen && (
          <div className="sd-menu-overlay" onClick={() => setIsMenuOpen(false)}></div>
        )}

        <nav className={`sd-left-nav ${isMenuOpen ? 'open' : ''}`}>
          <div className="sd-left-nav-user">
            <div className="sd-info-avatar sd-avatar-sm">
              {getInitials(user.fullName || user.username)}
            </div>
            <span className="sd-left-nav-name">{user.fullName || user.username}</span>
          </div>

          <div className="sd-left-nav-items">
            {allowedNavItems.map((item) => (
              <button
                key={item.id}
                className={`sd-left-nav-item ${activeRoleTab === item.id ? 'active' : ''}`}
                onClick={() => {
                  setActiveTab(item.id)
                  setSelectedUser(null)
                  setIsMenuOpen(false)
                }}
                type="button"
              >
                <span className="sd-nav-icon">{item.icon}</span>
                <span className="sd-nav-label">{item.label}</span>
              </button>
            ))}
          </div>
          <button className="sd-left-nav-logout" onClick={onLogout}>
            ↩ Đăng xuất
          </button>
        </nav>

        <main className="sd-main">
          <div className="sd-page-header">
            <div>
              <p className="sd-eyebrow">{headerInfo.eyebrow}</p>
              <h1>{headerInfo.title}</h1>
            </div>
          </div>

          <div className="sd-content">
            {/* ── 1. TỔNG QUAN ── */}
            {activeRoleTab === 'overview' && isAdmin && (
              <div className="sd-profile-layout">
                <div className="sd-stat-grid">
                  <div className="sd-stat-card">
                    <span className="sd-stat-icon">◈</span>
                    <h3>{users.length}</h3>
                    <p>Tổng nhân viên</p>
                  </div>
                  <div className="sd-stat-card">
                    <span className="sd-stat-icon">⊞</span>
                    <h3>{branches.length}</h3>
                    <p>Chi nhánh</p>
                  </div>
                </div>

                <div className="sd-card">
                  <div className="sd-card-header">
                    <p className="sd-eyebrow">Thống kê</p>
                    <h2>Phân bổ chức vụ</h2>
                  </div>
                  {roles
                    .filter((r) => r.roleName !== 'ADMIN')
                    .map((r) => {
                      const cnt = countByRole(r.roleName)
                      const pct = users.length ? Math.round((cnt / users.length) * 100) : 0
                      return (
                        <div key={r.id} className="sd-role-bar">
                          <div className="sd-role-bar-head">
                            <strong>{r.roleName}</strong>
                            <span>
                              {cnt} người · {pct}%
                            </span>
                          </div>
                          <div className="sd-bar-track">
                            <div className="sd-bar-fill" style={{ width: `${pct}%` }} />
                          </div>
                        </div>
                      )
                    })}
                </div>
              </div>
            )}

            {/* ── 2. NHÂN VIÊN (Chỉ Admin thấy) ── */}
            {activeRoleTab === 'users' && isAdmin && (
              <>
                {!selectedUser ? (
                  <div className="sd-users-page">
                    <div className="sd-users-toolbar">
                      <div className="sd-users-toolbar-left">
                        <div className="sd-search-wrap">
                          <span className="sd-search-icon">⌕</span>
                          <input
                            className="sd-input-search"
                            placeholder="Tìm tên, username, chi nhánh..."
                            value={search}
                            onChange={(e) => setSearch(e.target.value)}
                          />
                          {search && (
                            <button className="sd-search-clear" onClick={() => setSearch('')}>
                              ✕
                            </button>
                          )}
                        </div>
                        <div className="sd-filter-chips">
                          {['ALL', 'ADMIN', 'MANAGER', 'STAFF'].map((r) => (
                            <button
                              key={r}
                              className={`sd-filter-chip ${filterRole === r ? 'active' : ''}`}
                              onClick={() => setFilterRole(r)}
                            >
                              {r === 'ALL' ? 'Tất cả' : r}
                              {r !== 'ALL' && <span className="sd-chip-count">{countByRole(r)}</span>}
                            </button>
                          ))}
                        </div>
                      </div>
                      <div className="sd-users-toolbar-right">
                        <span className="sd-result-count">{displayed.length} nhân viên</span>
                        <button className="sd-btn-add" onClick={openAdd}>
                          <span>＋</span> Thêm nhân viên
                        </button>
                      </div>
                    </div>

                    <div className="sd-table-wrap">
                      <table className="sd-table">
                        <thead>
                          <tr>
                            <th className="sd-th sd-th-avatar" style={{ width: 48 }}></th>
                            <th className="sd-th sd-th-sortable sd-td-name-col" onClick={() => toggleSort('fullName')}>
                              Họ và tên <SortIcon col="fullName" />
                            </th>
                            <th className="sd-th sd-th-sortable sd-hide-mobile" onClick={() => toggleSort('username')}>
                              Username <SortIcon col="username" />
                            </th>
                            <th className="sd-th sd-th-sortable sd-hide-mobile" onClick={() => toggleSort('roleName')}>
                              Chức vụ <SortIcon col="roleName" />
                            </th>
                            <th className="sd-th sd-th-sortable sd-td-info-col" onClick={() => toggleSort('branchName')}>
                              Chi nhánh <SortIcon col="branchName" />
                            </th>
                            <th className="sd-th sd-th-sortable sd-hide-mobile" onClick={() => toggleSort('hireDate')}>
                              Ngày vào làm <SortIcon col="hireDate" />
                            </th>
                          </tr>
                        </thead>
                        <tbody>
                          {displayed.length === 0 && (
                            <tr>
                              <td colSpan={6} className="sd-td-empty">
                                <div className="sd-empty-state">
                                  <span className="sd-empty-icon">◈</span>
                                  <p>Không tìm thấy nhân sự</p>
                                </div>
                              </td>
                            </tr>
                          )}
                          {displayed.map((u, idx) => {
                            const roleColor = ROLE_COLORS[u.roleName?.toUpperCase()] || { bg: '#f1f5f9', color: '#475569' }
                            return (
                              <tr
                                key={u.id}
                                className="sd-tr"
                                style={{ animationDelay: `${idx * 30}ms`, cursor: 'pointer' }}
                                onClick={() => setSelectedUser(u)}
                              >
                                <td className="sd-td sd-td-avatar sd-hide-mobile">
                                  <div className="sd-info-avatar sd-avatar-sm">
                                    {getInitials(u.fullName || u.username)}
                                  </div>
                                </td>
                                <td className="sd-td sd-td-name-col">
                                  <span className="sd-td-name">{u.fullName || '—'}</span>
                                </td>
                                <td className="sd-td sd-hide-mobile">
                                  <span className="sd-td-username">@{u.username}</span>
                                </td>
                                <td className="sd-td sd-hide-mobile">
                                  <span
                                    className="sd-role-pill"
                                    style={{ background: roleColor.bg, color: roleColor.color }}
                                  >
                                    {u.roleName || '—'}
                                  </span>
                                </td>
                                <td className="sd-td sd-td-info-col">
                                  <span className="sd-td-branch">
                                    {u.branchName || <em className="sd-muted">Chưa gán</em>}
                                  </span>
                                </td>
                                <td className="sd-td sd-hide-mobile">
                                  <span className="sd-td-date">{formatDate(u.hireDate)}</span>
                                </td>
                              </tr>
                            )
                          })}
                        </tbody>
                      </table>
                    </div>
                  </div>
                ) : (
                  <div className="sd-user-detail-page">
                    <button className="sd-btn-back" onClick={() => setSelectedUser(null)}>
                      ← Quay lại danh sách
                    </button>

                    <div className="sd-profile-layout">
                      <div className="sd-card">
                        <div className="sd-info-hero">
                          <div className="sd-info-avatar">
                            {getInitials(selectedUser.fullName || selectedUser.username)}
                          </div>
                          <div>
                            <h3>{selectedUser.fullName || selectedUser.username}</h3>
                            <span
                              className="sd-role-badge"
                              style={{
                                background: ROLE_COLORS[selectedUser.roleName?.toUpperCase()]?.bg || '#ea580c',
                                color: ROLE_COLORS[selectedUser.roleName?.toUpperCase()]?.color || '#fff',
                              }}
                            >
                              {selectedUser.roleName || '—'}
                            </span>
                          </div>
                        </div>
                        <dl className="sd-dl">
                          <InfoRow label="Username" value={`@${selectedUser.username}`} />
                          <InfoRow label="Chi nhánh" value={selectedUser.branchName || 'Chưa gán'} />
                          <InfoRow label="Ngày vào làm" value={formatDate(selectedUser.hireDate)} />
                        </dl>

                        <div className="sd-detail-actions">
                          <button className="sd-btn-ghost btn-edit" onClick={() => openEdit(selectedUser)}>
                            ✎ Chỉnh sửa
                          </button>
                          {selectedUser.id !== user.id && (
                            <button className="sd-btn-ghost btn-delete" onClick={() => openDelete(selectedUser)}>
                              ✕ Xóa nhân sự
                            </button>
                          )}
                        </div>
                      </div>
                    </div>
                  </div>
                )}
              </>
            )}

            {/* ── 3. CƠ SỞ & CA LÀM (Chỉ Admin thấy) ── */}
            {activeRoleTab === 'branches' && isAdmin && (
              <AdminBranchTab branches={branches} setBranches={setBranches} />
            )}

            {/* ── 4. ĐỢT ĐĂNG KÝ (Chỉ Manager thấy để tự động xếp lịch và duyệt) ── */}
            {activeRoleTab === 'periods' && isManager && (
              <AdminPeriodTab user={user} isManager={isManager} branches={branches} />
            )}

            {/* ── 👉 5. TAB MỚI: XEM LỊCH CHÍNH THỨC CÁC CƠ SỞ (Chỉ ADMIN thấy) ── */}
            {activeRoleTab === 'scanQr' && isManager && (
              <ManagerQrAttendanceTab user={user} />
            )}

            {activeRoleTab === 'systemSchedule' && isAdmin && (
              <AdminSystemScheduleTab branches={branches} />
            )}

            {activeRoleTab === 'staffSchedule' && isStaff && (
              <>
                <EmployeeQrCard user={user} />
                <UnifiedScheduleTab user={user} />
              </>
            )}

            {/* ── 6. TÀI KHOẢN CÁ NHÂN ── */}
            {activeRoleTab === 'account' && (
              <div className="sd-profile-layout">
                <div className="sd-card">
                  <div className="sd-card-header">
                    <p className="sd-eyebrow">Chi tiết</p>
                    <h2>Hồ sơ cá nhân</h2>
                  </div>
                  <div className="sd-info-hero">
                    <div className="sd-info-avatar">{getInitials(user.fullName || user.username)}</div>
                    <div>
                      <h3>{user.fullName || user.username}</h3>
                      <span className="sd-role-badge">{user.roleName}</span>
                    </div>
                  </div>
                  <dl className="sd-dl">
                    <InfoRow label="Tên đăng nhập" value={user.username} />
                    <InfoRow label="Chi nhánh" value={branch?.name || user.branchName || 'Chưa có'} />
                    <InfoRow label="Ngày vào làm" value={formatDate(user.hireDate)} />
                  </dl>
                </div>

                <div className="sd-card">
                  <div className="sd-card-header">
                    <p className="sd-eyebrow">Bảo mật</p>
                    <h2>Đổi mật khẩu</h2>
                  </div>
                  <PasswordForm onUserUpdated={onUserUpdated} user={user} />
                </div>
              </div>
            )}
          </div>
        </main>
      </div>

      {/* --- MODAL CỦA NHÂN VIÊN --- */}
      {(modal === 'add' || modal === 'edit') && (
        <div className="sd-overlay" onClick={closeModal}>
          <div className="sd-modal" onClick={(e) => e.stopPropagation()}>
            <div className="sd-modal-header">
              <h2>{modal === 'add' ? 'Thêm nhân viên' : 'Chỉnh sửa'}</h2>
              <button onClick={closeModal}>✕</button>
            </div>
            <div className="sd-modal-body">
              <div className="sd-modal-grid">
                <div className="sd-field">
                  <label>Họ và tên *</label>
                  <input name="fullName" value={form.fullName} onChange={handleFormChange} />
                </div>
                <div className="sd-field">
                  <label>Username *</label>
                  <input name="username" value={form.username} onChange={handleFormChange} />
                </div>
                <div className="sd-field">
                  <label>Password *</label>
                  <input type="password" name="password" value={form.password} onChange={handleFormChange} placeholder="••••••" />
                </div>
                <div className="sd-field">
                  <label>Ngày vào làm</label>
                  <input type="date" name="hireDate" value={form.hireDate?.slice(0, 10) || ''} onChange={handleFormChange} />
                </div>
                <div className="sd-field">
                  <label>Role</label>
                  <select name="roleId" value={form.roleId || ''} onChange={handleFormChange}>
                    <option value="">-- Chọn role --</option>
                    {roles.map((r) => (
                      <option key={r.id} value={r.id}>
                        {r.roleName}
                      </option>
                    ))}
                  </select>
                </div>
                <div className="sd-field">
                  <label>Chi nhánh</label>
                  <select name="branchId" value={form.branchId || ''} onChange={handleFormChange}>
                    <option value="">-- Chọn chi nhánh --</option>
                    {branches.map((b) => (
                      <option key={b.id} value={b.id}>
                        {b.name}
                      </option>
                    ))}
                  </select>
                </div>
              </div>
              {formErr && <p className="sd-status sd-status-error">{formErr}</p>}
            </div>
            <div className="sd-modal-footer">
              <button className="sd-btn-ghost" onClick={closeModal}>Huỷ</button>
              <button className="sd-btn-primary" disabled={saving} onClick={modal === 'add' ? handleSaveAdd : handleSaveEdit}>
                {saving ? 'Đang lưu...' : 'Lưu lại'}
              </button>
            </div>
          </div>
        </div>
      )}

      {modal === 'delete' && (
        <div className="sd-overlay" onClick={closeModal}>
          <div className="sd-modal" onClick={(e) => e.stopPropagation()}>
            <div className="sd-modal-header">
              <h2>Xác nhận xoá</h2>
              <button onClick={closeModal}>✕</button>
            </div>
            <div className="sd-modal-body">
              <p>Bạn có chắc muốn xoá nhân viên <strong>{modalUser?.fullName}</strong>?</p>
              {formErr && <p className="sd-status sd-status-error">{formErr}</p>}
            </div>
            <div className="sd-modal-footer">
              <button className="sd-btn-ghost" onClick={closeModal}>Huỷ</button>
              <button className="sd-btn-primary btn-danger" disabled={saving} onClick={handleDelete}>
                {saving ? 'Đang xoá...' : 'Xoá ngay'}
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  )
}

function InfoRow({ label, value }) {
  return (
    <div className="sd-info-row">
      <dt>{label}</dt>
      <dd>{value}</dd>
    </div>
  )
}

function PasswordForm({ onUserUpdated, user }) {
  const [form, setForm] = useState({ currentPassword: '', newPassword: '', confirmPassword: '' })
  const [status, setStatus] = useState(null)
  const [isSaving, setIsSaving] = useState(false)

  function handleChange(e) {
    setForm((f) => ({ ...f, [e.target.name]: e.target.value }))
  }

  async function handleSubmit(e) {
    e.preventDefault()
    setStatus(null)
    if (form.currentPassword !== user.password) return setStatus({ type: 'error', msg: 'Mật khẩu hiện tại không đúng' })
    if (form.newPassword.length < 4) return setStatus({ type: 'error', msg: 'Mật khẩu mới cần tối thiểu 4 ký tự' })
    if (form.newPassword !== form.confirmPassword) return setStatus({ type: 'error', msg: 'Nhập lại mật khẩu chưa khớp' })
    try {
      setIsSaving(true)
      const updatedUser = { ...user, password: form.newPassword }
      await updateUser(user.id, updatedUser)
      onUserUpdated(updatedUser)
      setForm({ currentPassword: '', newPassword: '', confirmPassword: '' })
      setStatus({ type: 'success', msg: 'Đã cập nhật mật khẩu thành công' })
    } catch (err) {
      setStatus({ type: 'error', msg: err.message || 'Lỗi cập nhật' })
    } finally {
      setIsSaving(false)
    }
  }

  return (
    <form className="sd-pw-form" onSubmit={handleSubmit}>
      {['currentPassword', 'newPassword', 'confirmPassword'].map((field) => (
        <div key={field} className="sd-field">
          <label>{field === 'currentPassword' ? 'Mật khẩu hiện tại' : field === 'newPassword' ? 'Mật khẩu mới' : 'Nhập lại mật khẩu'}</label>
          <input type="password" name={field} value={form[field]} onChange={handleChange} />
        </div>
      ))}
      {status && <p className={`sd-status sd-status-${status.type}`}>{status.msg}</p>}
      <button className="sd-btn-primary" disabled={isSaving} type="submit">{isSaving ? 'Đang lưu…' : 'Cập nhật mật khẩu'}</button>
    </form>
  )
}

function ManagerQrAttendanceTab({ user }) {
  const [shifts, setShifts] = useState([])
  const [shiftId, setShiftId] = useState('')
  const [workDate, setWorkDate] = useState(() => new Date().toISOString().slice(0, 10))
  const [manualQr, setManualQr] = useState('')
  const [status, setStatus] = useState(null)
  const [scanResult, setScanResult] = useState(null)
  const [isSubmitting, setIsSubmitting] = useState(false)
  const [lastQrText, setLastQrText] = useState('')

  useEffect(() => {
    getAllShifts()
      .then((data) => {
        const branchShifts = (Array.isArray(data) ? data : []).filter((s) => String(s.branchId) === String(user.branchId))
        setShifts(branchShifts)
        setShiftId((current) => current || branchShifts[0]?.id?.toString() || '')
      })
      .catch(() => {
        setShifts([])
      })
  }, [user.branchId])

  useEffect(() => {
    const scanner = new Html5QrcodeScanner(
      'manager-qr-reader',
      { fps: 8, qrbox: { width: 240, height: 240 } },
      false
    )

    scanner.render(
      (decodedText) => {
        if (decodedText && decodedText !== lastQrText) {
          setLastQrText(decodedText)
          handleQrText(decodedText)
        }
      },
      () => {}
    )

    return () => {
      scanner.clear().catch(() => {})
    }
  }, [lastQrText, shiftId, workDate])

  function parseEmployeeQr(text) {
    const parsed = JSON.parse(text)
    const employeeId = parsed.id || parsed.employeeId || parsed.userId
    if (!employeeId) throw new Error('QR khong co ma nhan vien')
    return { ...parsed, employeeId: Number(employeeId) }
  }

  async function handleQrText(text) {
    if (!shiftId || !workDate) {
      setStatus({ type: 'error', msg: 'Vui long chon ngay va ca truoc khi quet QR.' })
      return
    }

    setIsSubmitting(true)
    setStatus(null)
    try {
      const employeeQr = parseEmployeeQr(text)
      const res = await axios.post('/api/StaffRegistration/scan-attendance', {
        managerId: user.id,
        employeeId: employeeQr.employeeId,
        shiftId: Number(shiftId),
        workDate,
        checkInTime: new Date().toISOString(),
      })
      setScanResult(res.data)
      setStatus({ type: 'success', msg: 'Da luu vao CaFinalSchedule va CaAttendance.' })
    } catch (err) {
      setStatus({ type: 'error', msg: err.response?.data?.message || err.message || 'Khong doc duoc ma QR.' })
    } finally {
      setIsSubmitting(false)
    }
  }

  function handleManualSubmit(e) {
    e.preventDefault()
    if (!manualQr.trim()) return
    setLastQrText(manualQr.trim())
    handleQrText(manualQr.trim())
  }

  return (
    <div className="sd-users-page">
      <div className="sd-card sd-qr-scan-card">
        <div className="sd-card-header">
          <p className="sd-eyebrow">Cham cong</p>
          <h2>Quet QR nhan vien</h2>
        </div>

        <div className="sd-modal-grid">
          <div className="sd-field">
            <label>Ngay lam viec</label>
            <input type="date" value={workDate} onChange={(e) => setWorkDate(e.target.value)} />
          </div>
          <div className="sd-field">
            <label>Ca lam</label>
            <select value={shiftId} onChange={(e) => setShiftId(e.target.value)}>
              <option value="">-- Chon ca --</option>
              {shifts.map((s) => (
                <option key={s.id} value={s.id}>
                  {s.shiftName} ({s.startTime?.slice(0, 5)} - {s.endTime?.slice(0, 5)})
                </option>
              ))}
            </select>
          </div>
        </div>

        <div id="manager-qr-reader" className="sd-qr-reader"></div>

        <form className="sd-qr-manual" onSubmit={handleManualSubmit}>
          <div className="sd-field">
            <label>Nhap du lieu QR neu khong mo duoc camera</label>
            <textarea value={manualQr} onChange={(e) => setManualQr(e.target.value)} placeholder='{"type":"EMPLOYEE","id":1,...}' />
          </div>
          <button className="sd-btn-primary" disabled={isSubmitting || !shiftId || !workDate} type="submit">
            {isSubmitting ? 'Dang luu...' : 'Luu du lieu QR'}
          </button>
        </form>

        {status && <p className={`sd-status sd-status-${status.type}`}>{status.msg}</p>}
      </div>

      {scanResult && (
        <div className="sd-card">
          <div className="sd-card-header">
            <p className="sd-eyebrow">Ket qua moi nhat</p>
            <h2>{scanResult.employee?.fullName || scanResult.employee?.username}</h2>
          </div>
          <dl className="sd-dl">
            <InfoRow label="Schedule ID" value={scanResult.scheduleId} />
            <InfoRow label="Attendance ID" value={scanResult.attendanceId} />
            <InfoRow label="Ca" value={scanResult.shift?.shiftName || '---'} />
            <InfoRow label="Ngay" value={formatDate(scanResult.workDate)} />
            <InfoRow label="Check-in" value={scanResult.checkInTime ? new Date(scanResult.checkInTime).toLocaleString('vi-VN') : '---'} />
          </dl>
        </div>
      )}
    </div>
  )
}

export function AdminBranchTab({ branches, setBranches }) {
  const [selectedBranch, setSelectedBranch] = useState(null)
  const [branchModal, setBranchModal] = useState(null)
  const [branchForm, setBranchForm] = useState({ name: '', address: '', latitude: '', longitude: '' })
  const [search, setSearch] = useState('')
  const [shifts, setShifts] = useState([])
  const [shiftModal, setShiftModal] = useState(null)
  const [shiftForm, setShiftForm] = useState({ shiftName: '', startTime: '', endTime: '', maxStaff: 0, isOt: false })
  const [modalShift, setModalShift] = useState(null)
  const [saving, setSaving] = useState(false)
  const [error, setError] = useState('')

  useEffect(() => {
    getAllShifts()
      .then((data) => setShifts(Array.isArray(data) ? data : []))
      .catch(() => {})
  }, [])

  const displayedBranches = branches.filter((b) =>
    (b.name?.toLowerCase() || '').includes(search.toLowerCase()) ||
    (b.address?.toLowerCase() || '').includes(search.toLowerCase())
  )

  function openAddBranch() { setBranchForm({ name: '', address: '', latitude: '', longitude: '' }); setError(''); setBranchModal('add') }
  function openEditBranch(b) { setBranchForm({ ...b }); setError(''); setBranchModal('edit') }
  function openDeleteBranch() { setError(''); setBranchModal('delete') }

  async function handleSaveBranch() {
    if (!branchForm.name || !branchForm.address) return setError('Vui lòng nhập tên và địa chỉ cơ sở')
    setSaving(true); setError('')
    try {
      const payload = { ...branchForm, latitude: branchForm.latitude === '' ? null : parseFloat(branchForm.latitude), longitude: branchForm.longitude === '' ? null : parseFloat(branchForm.longitude) }
      if (branchModal === 'add') { await createBranch(payload) } else { await updateBranch(branchForm.id, payload); if (selectedBranch) { setSelectedBranch({ ...selectedBranch, ...payload }) } }
      const newData = await getAllBranches(); setBranches(Array.isArray(newData) ? newData : []); setBranchModal(null)
    } catch (err) { console.error(err); setError('Lỗi lưu cơ sở!') } finally { setSaving(false) }
  }

  async function handleDeleteBranch() {
    setSaving(true); setError('')
    try { await deleteBranch(selectedBranch.id); setBranches((prev) => prev.filter((b) => b.id !== selectedBranch.id)); setSelectedBranch(null); setBranchModal(null) } 
    catch (err) { setError('Lỗi xóa cơ sở!') } finally { setSaving(false) }
  }

  const displayedShifts = selectedBranch ? shifts.filter((s) => s.branchId === selectedBranch.id) : []

  function openAddShift() { setShiftForm({ shiftName: '', startTime: '', endTime: '', maxStaff: 0, isOt: false }); setError(''); setShiftModal('add') }
  function openEditShift(s) { setShiftForm({ ...s }); setError(''); setModalShift(s); setShiftModal('edit') }
  function openDeleteShift(s) { setModalShift(s); setError(''); setShiftModal('delete') }

  async function handleSaveShift() {
    if (!shiftForm.shiftName || !shiftForm.startTime || !shiftForm.endTime) return setError('Vui lòng nhập Tên ca và Giờ')
    setSaving(true); setError('')
    try {
      const formatTime = (time) => (time.length === 5 ? `${time}:00` : time)
      const payloadShift = { ...shiftForm, startTime: formatTime(shiftForm.startTime), endTime: formatTime(shiftForm.endTime), maxStaff: shiftForm.maxStaff === '' ? 0 : parseInt(shiftForm.maxStaff, 10) }
      if (shiftModal === 'add') { const payload = { ...payloadShift, branchId: selectedBranch.id }; await createShift(payload) } else { await updateShift(shiftForm.id, payloadShift) }
      const newData = await getAllShifts(); setShifts(Array.isArray(newData) ? newData : []); setShiftModal(null)
    } catch (err) { console.error(err); setError('Dữ liệu không hợp lệ!') } finally { setSaving(false) }
  }

  async function handleDeleteShift() {
    setSaving(true); setError('')
    try { await deleteShift(modalShift.id); setShifts((prev) => prev.filter((s) => s.id !== modalShift.id)); setShiftModal(null) } 
    catch (err) { setError('Lỗi khi xóa ca.') } finally { setSaving(false) }
  }

  return (
    <div className="sd-users-page">
      {!selectedBranch ? (
        <>
          <div className="sd-users-toolbar">
            <div className="sd-users-toolbar-left">
              <div className="sd-search-wrap">
                <span className="sd-search-icon">⌕</span>
                <input className="sd-input-search" placeholder="Tìm tên cơ sở, địa chỉ..." value={search} onChange={(e) => setSearch(e.target.value)} />
                {search && <button className="sd-search-clear" onClick={() => setSearch('')}>✕</button>}
              </div>
            </div>
            <div className="sd-users-toolbar-right">
              <span className="sd-result-count">{displayedBranches.length} cơ sở</span>
              <button className="sd-btn-add" onClick={openAddBranch}><span>＋</span> Thêm cơ sở</button>
            </div>
          </div>
          <div className="sd-table-wrap">
            <table className="sd-table">
              <thead>
                <tr>
                  <th className="sd-th sd-text-center sd-hide-mobile" style={{ width: 60 }}>ID</th>
                  <th className="sd-th sd-td-name-col">Tên Cơ Sở</th>
                  <th className="sd-th sd-td-info-col sd-hide-mobile">Địa Chỉ</th>
                </tr>
              </thead>
              <tbody>
                {displayedBranches.map((b) => (
                  <tr key={b.id} className="sd-tr" onClick={() => setSelectedBranch(b)} style={{ cursor: 'pointer' }}>
                    <td className="sd-td sd-text-center sd-text-bold sd-text-muted sd-hide-mobile" style={{ width: 60 }}>#{b.id}</td>
                    <td className="sd-td sd-td-name-col"><span className="sd-td-name sd-text-primary">{b.name}</span></td>
                    <td className="sd-td sd-td-info-col sd-hide-mobile"><span className="sd-text-sm sd-text-muted">{b.address}</span></td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </>
      ) : (
        <div>
          <button className="sd-btn-back" onClick={() => setSelectedBranch(null)}>← Quay lại danh sách cơ sở</button>
          <div className="sd-card">
            <div className="sd-card-header append-flex">
              <div><p className="sd-eyebrow">Chi tiết</p><h2>{selectedBranch.name}</h2></div>
              <div className="sd-flex-start">
                <button className="sd-action-btn sd-action-edit" onClick={() => openEditBranch(selectedBranch)}>✎</button>
                <button className="sd-action-btn sd-action-delete" onClick={openDeleteBranch}>✕</button>
              </div>
            </div>
            <div className="sd-flex-column sd-text-muted" style={{ fontSize: 14 }}>
              <p style={{ margin: 0 }}>📍 <strong className="sd-text-bold">Địa chỉ:</strong> {selectedBranch.address}</p>
              <p style={{ margin: 0 }}>🗺️ <strong className="sd-text-bold">Tọa độ GPS:</strong> {selectedBranch.latitude || '—'}, {selectedBranch.longitude || '—'}</p>
            </div>
          </div>
          <div className="sd-card">
            <div className="sd-card-header sd-flex-between" style={{ marginBottom: 20 }}>
              <div><p className="sd-eyebrow">Cấu hình</p><h2>Ca làm việc</h2></div>
              <button className="sd-btn-add" onClick={openAddShift}><span>＋</span> Thêm ca</button>
            </div>
            <div className="sd-table-wrap sd-box-bordered">
              <table className="sd-table">
                <thead style={{ background: '#f8fafc' }}>
                  <tr>
                    <th className="sd-th sd-td-name-col">Tên Ca</th>
                    <th className="sd-th">Thời Gian</th>
                    <th className="sd-th sd-text-center sd-hide-mobile">NV Tối Đa</th>
                    <th className="sd-th sd-text-center sd-hide-mobile">Tăng Ca (OT)</th>
                    <th className="sd-th sd-th-actions">Thao tác</th>
                  </tr>
                </thead>
                <tbody>
                  {displayedShifts.length === 0 && (
                    <tr><td colSpan={5} className="sd-td-empty sd-td-empty-sm"><div className="sd-empty-state"><span className="sd-empty-icon">⏱️</span><p>Chưa có ca làm nào</p></div></td></tr>
                  )}
                  {displayedShifts.map((s) => (
                    <tr key={s.id} className="sd-tr">
                      <td className="sd-td sd-td-name-col"><strong style={{ color: '#1e293b' }}>{s.shiftName}</strong></td>
                      <td className="sd-td sd-td-info-col"><span className="sd-badge-time">{s.startTime?.slice(0, 5)} - {s.endTime?.slice(0, 5)}</span></td>
                      <td className="sd-td sd-text-center sd-hide-mobile"><strong>{s.maxStaff || 0}</strong></td>
                      <td className="sd-td sd-text-center sd-hide-mobile"><span className={`sd-role-pill ${s.isOt ? 'sd-badge-success' : 'sd-badge-neutral'}`}>{s.isOt ? 'Có' : 'Không'}</span></td>
                      <td className="sd-td sd-td-actions">
                        <button className="sd-action-btn sd-action-edit" onClick={() => openEditShift(s)}>✎</button>
                        <button className="sd-action-btn sd-action-delete" onClick={() => openDeleteShift(s)}>✕</button>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          </div>
        </div>
      )}

      {/* --- MODALS CƠ SỞ & CA --- */}
      {(branchModal === 'add' || branchModal === 'edit') && (
        <div className="sd-overlay" onClick={() => setBranchModal(null)}><div className="sd-modal" onClick={e => e.stopPropagation()}><div className="sd-modal-header"><h2>{branchModal === 'add' ? 'Thêm cơ sở' : 'Sửa cơ sở'}</h2><button onClick={() => setBranchModal(null)}>✕</button></div><div className="sd-modal-body"><div className="sd-field"><label>Tên cơ sở *</label><input value={branchForm.name} onChange={(e) => setBranchForm({ ...branchForm, name: e.target.value })} /></div><div className="sd-field"><label>Địa chỉ *</label><input value={branchForm.address} onChange={(e) => setBranchForm({ ...branchForm, address: e.target.value })} /></div><div className="sd-modal-grid"><div className="sd-field"><label>Vĩ độ (Lat)</label><input type="number" value={branchForm.latitude} onChange={(e) => setBranchForm({ ...branchForm, latitude: e.target.value })} /></div><div className="sd-field"><label>Kinh độ (Lng)</label><input type="number" value={branchForm.longitude} onChange={(e) => setBranchForm({ ...branchForm, longitude: e.target.value })} /></div></div>{error && <p className="sd-status sd-status-error">{error}</p>}</div><div className="sd-modal-footer"><button className="sd-btn-ghost" onClick={() => setBranchModal(null)}>Huỷ</button><button className="sd-btn-primary" disabled={saving} onClick={handleSaveBranch}>{saving ? 'Đang lưu...' : 'Lưu lại'}</button></div></div></div>
      )}
      {branchModal === 'delete' && (
        <div className="sd-overlay" onClick={() => setBranchModal(null)}><div className="sd-modal" onClick={e => e.stopPropagation()}><div className="sd-modal-header"><h2>Xác nhận xoá</h2><button onClick={() => setBranchModal(null)}>✕</button></div><div className="sd-modal-body"><p>Xoá cơ sở <strong className="sd-text-primary">{selectedBranch?.name}</strong>?</p>{error && <p className="sd-status sd-status-error">{error}</p>}</div><div className="sd-modal-footer"><button className="sd-btn-ghost" onClick={() => setBranchModal(null)}>Huỷ</button><button className="sd-btn-primary btn-danger" disabled={saving} onClick={handleDeleteBranch}>Xoá ngay</button></div></div></div>
      )}
      {(shiftModal === 'add' || shiftModal === 'edit') && (
        <div className="sd-overlay" onClick={() => setShiftModal(null)}><div className="sd-modal" onClick={e => e.stopPropagation()}><div className="sd-modal-header"><h2>{shiftModal === 'add' ? 'Thêm ca làm' : 'Sửa ca làm'}</h2><button onClick={() => setShiftModal(null)}>✕</button></div><div className="sd-modal-body"><div className="sd-field"><label>Tên ca (VD: Ca Sáng) *</label><input value={shiftForm.shiftName} onChange={(e) => setShiftForm({ ...shiftForm, shiftName: e.target.value })} /></div><div className="sd-modal-grid"><div className="sd-field"><label>Giờ bắt đầu *</label><input type="time" value={shiftForm.startTime?.slice(0, 5)} onChange={(e) => setShiftForm({ ...shiftForm, startTime: e.target.value })} /></div><div className="sd-field"><label>Giờ kết thúc *</label><input type="time" value={shiftForm.endTime?.slice(0, 5)} onChange={(e) => setShiftForm({ ...shiftForm, endTime: e.target.value })} /></div></div><div className="sd-modal-grid"><div className="sd-field"><label>Số NV tối đa mặc định</label><input type="number" value={shiftForm.maxStaff} onChange={(e) => setShiftForm({ ...shiftForm, maxStaff: e.target.value })} /></div><div className="sd-field sd-flex-center"><label style={{ display: 'flex', alignItems: 'center', gap: 8, cursor: 'pointer', marginTop: 24 }}><input type="checkbox" checked={shiftForm.isOt} onChange={(e) => setShiftForm({ ...shiftForm, isOt: e.target.checked })} style={{ width: 18, height: 18 }} />Ca tính Tăng ca (OT)?</label></div></div>{error && <p className="sd-status sd-status-error">{error}</p>}</div><div className="sd-modal-footer"><button className="sd-btn-ghost" onClick={() => setShiftModal(null)}>Huỷ</button><button className="sd-btn-primary" disabled={saving} onClick={handleSaveShift}>{saving ? 'Đang lưu...' : 'Lưu lại'}</button></div></div></div>
      )}
      {shiftModal === 'delete' && (
        <div className="sd-overlay" onClick={() => setShiftModal(null)}><div className="sd-modal" onClick={e => e.stopPropagation()}><div className="sd-modal-header"><h2>Xác nhận xoá</h2><button onClick={() => setShiftModal(null)}>✕</button></div><div className="sd-modal-body"><p>Xoá ca <strong className="sd-text-primary">{modalShift?.shiftName}</strong>?</p>{error && <p className="sd-status sd-status-error">{error}</p>}</div><div className="sd-modal-footer"><button className="sd-btn-ghost" onClick={() => setShiftModal(null)}>Huỷ</button><button className="sd-btn-primary btn-danger" disabled={saving} onClick={handleDeleteShift}>Xoá ngay</button></div></div></div>
      )}
    </div>
  )
}

// ==========================================
// ĐỢT ĐĂNG KÝ (BẢNG DUYỆT CỦA MANAGER)
// ==========================================
export function AdminPeriodTab({ user, isManager, branches }) {
  const [periods, setPeriods] = useState([])
  const [modal, setModal] = useState(null)
  const [selectedPeriod, setSelectedPeriod] = useState(null)
  const [search, setSearch] = useState('')
  const [saving, setSaving] = useState(false)
  const [error, setError] = useState('')
  const [reviewingPeriod, setReviewingPeriod] = useState(null)

  const [form, setForm] = useState({ startDate: '', endDate: '', status: 'Mở' })

  useEffect(() => { loadPeriods() }, [])

  async function loadPeriods() {
    try {
      const data = await getAllPeriods()
      setPeriods(Array.isArray(data) ? data : [])
    } catch (err) { console.error(err) }
  }

  const filteredPeriods = periods
    .filter((p) => {
      const matchBranch = p.branchId === user.branchId 
      const dateRangeStr = `${formatDate(p.startDate)} ${formatDate(p.endDate)}`.toLowerCase()
      return matchBranch && dateRangeStr.includes(search.toLowerCase())
    })
    .sort((a, b) => new Date(b.startDate) - new Date(a.startDate))

  function openAdd() { setForm({ startDate: '', endDate: '', status: 'Mở' }); setError(''); setModal('add') }
  function openEdit(p) { setForm({ id: p.id, startDate: p.startDate?.slice(0, 10) || '', endDate: p.endDate?.slice(0, 10) || '', status: p.status || 'Mở' }); setError(''); setModal('edit') }
  function openDelete(p) { setSelectedPeriod(p); setError(''); setModal('delete') }

  async function handleSave() {
    if (!form.startDate || !form.endDate) return setError('Vui lòng điền ngày bắt đầu và kết thúc')
    setSaving(true); setError('')
    try {
      const payload = { ...form, branchId: user.branchId }
      if (modal === 'add') await createPeriod(payload)
      else await updatePeriod(form.id, payload)
      await loadPeriods()
      setModal(null)
    } catch (err) { setError('Lỗi khi lưu đợt.') } finally { setSaving(false) }
  }

  async function handleDelete() {
    setSaving(true); setError('')
    try { await deletePeriod(selectedPeriod.id); await loadPeriods(); setModal(null) } 
    catch (err) { setError('Không thể xóa đợt đăng ký này!') } finally { setSaving(false) }
  }

  if (reviewingPeriod) {
    return <PeriodReviewScreen period={reviewingPeriod} user={user} onBack={() => { setReviewingPeriod(null); loadPeriods() }} />
  }

  return (
    <div className="sd-users-page">
      <div className="sd-users-toolbar">
        <div className="sd-users-toolbar-left">
          <div className="sd-search-wrap">
            <span className="sd-search-icon">⌕</span>
            <input className="sd-input-search" placeholder="Tìm theo ngày..." value={search} onChange={(e) => setSearch(e.target.value)} />
            {search && <button className="sd-search-clear" onClick={() => setSearch('')}>✕</button>}
          </div>
        </div>
        <div className="sd-users-toolbar-right">
          <span className="sd-result-count">{filteredPeriods.length} đợt</span>
          <button className="sd-btn-add" onClick={openAdd}><span>＋</span> Mở đợt mới</button>
        </div>
      </div>

      <div className="sd-table-wrap">
        <table className="sd-table">
          <thead>
            <tr>
              <th className="sd-th sd-td-name-col">Thời gian đợt đăng ký</th>
              <th className="sd-th sd-text-center">Trạng Thái</th>
              <th className="sd-th sd-text-right">Thao tác</th>
            </tr>
          </thead>
          <tbody>
            {filteredPeriods.length === 0 && (
              <tr>
                <td colSpan={3} className="sd-td-empty">
                  <div className="sd-empty-state"><span className="sd-empty-icon">📅</span><p>Chưa có đợt đăng ký lịch làm nào</p></div>
                </td>
              </tr>
            )}
            {filteredPeriods.map((p) => {
              const isOpen = p.status?.toLowerCase() === 'mở' || p.status === 'Open' || p.status === 'OPEN'
              const isPublished = p.status === 'PUBLISHED'
              return (
                <tr key={p.id} className="sd-tr" style={{ cursor: 'pointer' }} onClick={() => setReviewingPeriod(p)}>
                  <td className="sd-td sd-td-name-col"><strong style={{ color: '#1e293b' }}>Từ {formatDate(p.startDate)} đến {formatDate(p.endDate)}</strong></td>
                  <td className="sd-td sd-text-center sd-td-info-col">
                    {isPublished ? (
                      <span className="sd-status-pill sd-status-pill--closed" style={{background: '#e0e7ff', color: '#1d4ed8', borderColor: '#bfdbfe'}}>Đã Chốt</span>
                    ) : (
                      <span className={`sd-status-pill ${isOpen ? 'sd-status-pill--open' : 'sd-status-pill--closed'}`}>{isOpen ? 'Đang Mở' : 'Đã Đóng'}</span>
                    )}
                  </td>
                  <td className="sd-td sd-text-right" style={{ whiteSpace: 'nowrap' }}>
                    <button className="sd-action-btn sd-action-edit" onClick={(e) => { e.stopPropagation(); openEdit(p) }}>✎</button>
                    <button className="sd-action-btn sd-action-delete" onClick={(e) => { e.stopPropagation(); openDelete(p) }}>✕</button>
                  </td>
                </tr>
              )
            })}
          </tbody>
        </table>
      </div>

      {/* MODALS MỞ ĐỢT */}
      {(modal === 'add' || modal === 'edit') && (
        <div className="sd-overlay" onClick={() => setModal(null)}><div className="sd-modal" onClick={(e) => e.stopPropagation()}><div className="sd-modal-header"><h2>{modal === 'add' ? 'Mở đợt đăng ký mới' : 'Chỉnh sửa'}</h2><button onClick={() => setModal(null)}>✕</button></div><div className="sd-modal-body"><div className="sd-modal-grid"><div className="sd-field"><label>Ngày bắt đầu đợt *</label><input type="date" value={form.startDate} onChange={(e) => setForm({ ...form, startDate: e.target.value })} /></div><div className="sd-field"><label>Ngày kết thúc đợt *</label><input type="date" value={form.endDate} onChange={(e) => setForm({ ...form, endDate: e.target.value })} /></div></div><div className="sd-field"><label>Trạng thái đợt đăng ký</label><select value={form.status} onChange={(e) => setForm({ ...form, status: e.target.value })}><option value="Mở">Mở (Cho phép nhân viên đăng ký)</option><option value="Đóng">Đóng (Khóa đăng ký)</option></select></div>{error && <p className="sd-status sd-status-error">{error}</p>}</div><div className="sd-modal-footer"><button className="sd-btn-ghost" onClick={() => setModal(null)}>Huỷ</button><button className="sd-btn-primary" disabled={saving} onClick={handleSave}>Lưu đợt</button></div></div></div>
      )}
      {modal === 'delete' && (
        <div className="sd-overlay" onClick={() => setModal(null)}><div className="sd-modal" onClick={(e) => e.stopPropagation()}><div className="sd-modal-header"><h2>Xác nhận xoá đợt</h2><button onClick={() => setModal(null)}>✕</button></div><div className="sd-modal-body"><p>Bạn có chắc chắn muốn xoá đợt từ <strong>{formatDate(selectedPeriod?.startDate)}</strong>?</p>{error && <p className="sd-status sd-status-error">{error}</p>}</div><div className="sd-modal-footer"><button className="sd-btn-ghost" onClick={() => setModal(null)}>Huỷ</button><button className="sd-btn-primary btn-danger" disabled={saving} onClick={handleDelete}>Xoá ngay</button></div></div></div>
      )}
    </div>
  )
}

// ==========================================
// MÀN HÌNH DUYỆT MA TRẬN CHO MANAGER
// ==========================================
const DAY_NAMES = ['Chủ nhật', 'Thứ 2', 'Thứ 3', 'Thứ 4', 'Thứ 5', 'Thứ 6', 'Thứ 7']

function PeriodReviewScreen({ period, onBack, user }) {
  const [registrations, setRegistrations] = useState([])
  const [shifts, setShifts] = useState([])
  const [dates, setDates] = useState([])
  const [loading, setLoading] = useState(true)
  const [draftApproved, setDraftApproved] = useState(new Set())
  const [activeSwapId, setActiveSwapId] = useState(null) 

  useEffect(() => {
    async function loadBoardData() {
      setLoading(true)
      try {
        const [regRes, shiftRes] = await Promise.all([
          axios.get(`/api/StaffRegistration/period/${period.id}`),
          getAllShifts()
        ])
        const allRegs = regRes.data || []
        const branchShifts = shiftRes.filter(s => s.branchId === period.branchId)
        setRegistrations(allRegs)
        setShifts(branchShifts)

        const dArray = []
        let curr = new Date(period.startDate); const end = new Date(period.endDate)
        while (curr <= end) { dArray.push(new Date(curr)); curr.setDate(curr.getDate() + 1) }
        setDates(dArray)

        const newDraft = new Set()
        const grouped = {}
        allRegs.forEach(r => {
          const key = r.workDate.slice(0, 10) + '_' + r.shiftId
          if (!grouped[key]) grouped[key] = []
          grouped[key].push(r)
        })

        Object.keys(grouped).forEach(key => {
          const shiftId = parseInt(key.split('_')[1])
          const shift = branchShifts.find(s => s.id === shiftId)
          const max = shift?.maxStaff || 0
          const allowedStaff = max > 0 ? max - 1 : 999 
          const sorted = grouped[key]
          for (let i = 0; i < Math.min(allowedStaff, sorted.length); i++) { newDraft.add(sorted[i].id) }
        })
        setDraftApproved(newDraft)
      } catch (error) { console.error(error) } finally { setLoading(false) }
    }
    loadBoardData()
  }, [period])

  function toDateString(dateObj) {
    const offset = dateObj.getTimezoneOffset()
    const d = new Date(dateObj.getTime() - (offset * 60 * 1000))
    return d.toISOString().split('T')[0]
  }

  const boardMatrix = {}
  dates.forEach(dObj => {
    const dStr = toDateString(dObj)
    boardMatrix[dStr] = {}
    shifts.forEach(s => {
      boardMatrix[dStr][s.id] = registrations.filter(r => r.workDate.slice(0, 10) === dStr && r.shiftId === s.id)
    })
  })

  const handlePublish = async () => {
    if(!window.confirm("Bạn có chắc chắn muốn CHỐT LỊCH?")) return
    try {
      const payload = { periodId: period.id, approvedRegistrationIds: Array.from(draftApproved) }
      await axios.post('/api/StaffRegistration/publish', payload)
      alert("✅ Đã chốt lịch làm việc thành công!")
      onBack() 
    } catch (error) { alert("Lỗi chốt lịch") }
  }

  return (
    <div className="sd-users-page">
      <button className="sd-btn-back" onClick={onBack}>← Quay lại danh sách đợt</button>
      <div className="sd-publish-banner">
        <div>
          <h2 style={{ margin: '0 0 4px', fontSize: 18 }}>Bảng xếp lịch: Từ {formatDate(period.startDate)} đến {formatDate(period.endDate)}</h2>
          <p style={{ margin: 0, fontSize: 13, opacity: 0.8 }}>Bấm vào nhân viên để đổi vị trí sang người dự bị.</p>
        </div>
        <button className="sd-btn-primary" style={{ width: 'auto', marginTop: 0 }} onClick={handlePublish}>🔒 Chốt & Cập nhật Lịch</button>
      </div>

      {loading ? <p>Đang tải ma trận...</p> : (
        <div className="sd-board-wrap">
          <table className="sd-schedule-board">
            <thead>
              <tr>
                <th style={{ width: 90 }}>NGÀY</th>
                {shifts.map(s => <th key={s.id}>{s.shiftName}<br/><span style={{fontWeight: 500, fontSize: 11}}>{s.startTime?.slice(0, 5)} - {s.endTime?.slice(0, 5)}</span></th>)}
              </tr>
            </thead>
            <tbody>
              {dates.map((dateObj) => {
                const dStr = toDateString(dateObj)
                const dayOfWeek = DAY_NAMES[dateObj.getDay()]
                return (
                  <tr key={dStr}>
                    <td className="sd-board-date-col"><strong>{dayOfWeek}</strong><small>{dateObj.getDate()}/{dateObj.getMonth() + 1}</small></td>
                    {shifts.map(shift => {
                      const cellRegs = boardMatrix[dStr][shift.id] || []
                      const max = shift.maxStaff || 0
                      const allowedStaff = max > 0 ? max - 1 : 0 
                      const assignedRegs = cellRegs.filter(r => draftApproved.has(r.id))
                      const backupRegs = cellRegs.filter(r => !draftApproved.has(r.id))
                      const slots = []; for (let i = 0; i < allowedStaff; i++) slots.push(assignedRegs[i] || null)

                      return (
                        <td key={shift.id}>
                          <div className="sd-reg-card" style={{ background: '#ffedd5', borderColor: '#fdba74', color: '#9a3412' }}>
                            <span className="sd-reg-name">👑 {user.fullName || user.username}</span>
                          </div>
                          {slots.map((r, idx) => {
                            if (!r) {
                              const emptyId = `empty_${dStr}_${shift.id}_${idx}`
                              return (
                                <div key={emptyId} style={{ position: 'relative' }}>
                                  <div className="sd-reg-card" style={{ background: '#f8fafc', borderColor: '#e2e8f0', color: '#94a3b8', borderStyle: 'dashed', cursor: 'pointer' }} onClick={() => setActiveSwapId(activeSwapId === emptyId ? null : emptyId)}><span>+ Thêm NV</span></div>
                                  {activeSwapId === emptyId && (
                                    <div className="sd-swap-dropdown">
                                      {backupRegs.map(backup => <div key={backup.id} className="sd-swap-item" onClick={() => { const next = new Set(draftApproved); next.add(backup.id); setDraftApproved(next); setActiveSwapId(null) }}>{backup.user?.fullName}</div>)}
                                    </div>
                                  )}
                                </div>
                              )
                            }
                            return (
                              <div key={r.id} style={{ position: 'relative' }}>
                                <div className="sd-reg-card" style={{ background: '#dcfce7', borderColor: '#bbf7d0', color: '#166534', cursor: 'pointer' }} onClick={() => setActiveSwapId(activeSwapId === r.id ? null : r.id)}>
                                  <span className="sd-reg-name">{r.user?.fullName}</span>
                                </div>
                                {activeSwapId === r.id && (
                                  <div className="sd-swap-dropdown">
                                    {backupRegs.map(backup => <div key={backup.id} className="sd-swap-item" onClick={() => { const next = new Set(draftApproved); next.delete(r.id); next.add(backup.id); setDraftApproved(next); setActiveSwapId(null) }}>{backup.user?.fullName}</div>)}
                                  </div>
                                )}
                              </div>
                            )
                          })}
                        </td>
                      )
                    })}
                  </tr>
                )
              })}
            </tbody>
          </table>
        </div>
      )}
    </div>
  )
}

// ==========================================
// 👉 👉 5. TAB MỚI: XEM LỊCH CHÍNH THỨC DÀNH CHO ADMIN
// ==========================================
function AdminSystemScheduleTab({ branches }) {
  const [periods, setPeriods] = useState([])
  const [shifts, setShifts] = useState([])
  const [dates, setDates] = useState([])
  const [registrations, setRegistrations] = useState([])

  // State lưu cơ sở và tuần đang chọn trên dropdown lọc
  const [selectedBranchId, setSelectedBranchId] = useState('')
  const [selectedPeriodId, setSelectedPeriodId] = useState('')
  const [loading, setLoading] = useState(false)

  // 1. Lần đầu vào tab, lấy mặc định chi nhánh đầu tiên
  useEffect(() => {
    if (branches.length > 0) {
      setSelectedBranchId(branches[0].id.toString())
    }
  }, [branches])

  // 2. Khi chọn xong Chi nhánh -> Đi tìm các Đợt "ĐÃ CHỐT" (PUBLISHED) của chi nhánh đó
  useEffect(() => {
    if (!selectedBranchId) return
    async function loadBranchPeriods() {
      try {
        const allPeriods = await getAllPeriods()
        const pPeriods = allPeriods
          .filter(p => String(p.branchId) === String(selectedBranchId) && p.status === 'PUBLISHED')
          .sort((a, b) => new Date(b.startDate) - new Date(a.startDate))
        
        setPeriods(pPeriods)
        if (pPeriods.length > 0) {
          setSelectedPeriodId(pPeriods[0].id.toString())
        } else {
          setSelectedPeriodId('')
          setRegistrations([])
          setDates([])
        }
      } catch (e) { console.error(e) }
    }
    loadBranchPeriods()
  }, [selectedBranchId])

  // 3. Khi đã có cả Chi nhánh + Đợt đã chốt -> Vẽ Ma trận lịch làm chính thức
  useEffect(() => {
    if (!selectedBranchId || !selectedPeriodId) return
    async function loadOfficialSchedule() {
      setLoading(true)
      try {
        const period = periods.find(p => p.id.toString() === selectedPeriodId)
        if (!period) return

        const [regRes, shiftRes] = await Promise.all([
          axios.get(`/api/StaffRegistration/period/${period.id}`),
          getAllShifts()
        ])

        // Chỉ lọc lấy những người mang trạng thái "Đã Duyệt" chốt hạ
        setRegistrations((regRes.data || []).filter(r => r.status === 'Đã Duyệt'))
        setShifts(shiftRes.filter(s => String(s.branchId) === String(selectedBranchId)))

        const dArray = []
        let curr = new Date(period.startDate); const end = new Date(period.endDate)
        while (curr <= end) { dArray.push(new Date(curr)); curr.setDate(curr.getDate() + 1) }
        setDates(dArray)
      } catch (e) { console.error(e) } finally { setLoading(false) }
    }
    loadOfficialSchedule()
  }, [selectedPeriodId, selectedBranchId, periods])

  function toDateString(dateObj) {
    const offset = dateObj.getTimezoneOffset()
    const d = new Date(dateObj.getTime() - (offset * 60 * 1000))
    return d.toISOString().split('T')[0]
  }

  const boardMatrix = {}
  dates.forEach(dObj => {
    const dStr = toDateString(dObj)
    boardMatrix[dStr] = {}
    shifts.forEach(s => {
      boardMatrix[dStr][s.id] = registrations.filter(r => r.workDate.slice(0, 10) === dStr && r.shiftId === s.id)
    })
  })

return (
    <div className="sd-card" style={{ padding: '20px 0' }}>
      
      {/* KHỐI DROPDOWN KÉP: CHO ADMIN CHỌN CƠ SỞ VÀ TUẦN LÀM VIỆC */}
      <div style={{ padding: '0 20px 16px', display: 'flex', flexDirection: 'column', gap: 12, borderBottom: '1px solid #f1f5f9', marginBottom: 16 }}>
        <div className="sd-field" style={{ marginBottom: 0 }}>
          <label>1. Chọn cơ sở canteen giám sát:</label>
          
          {/* 👉 ĐÃ XÓA className="sd-input-search" ĐỂ TRẢ VỀ DÁNG FORM CHUẨN */}
          <select value={selectedBranchId} onChange={(e) => setSelectedBranchId(e.target.value)}>
            {branches.map(b => <option key={b.id} value={b.id}>{b.name}</option>)}
          </select>
        </div>

        <div className="sd-field" style={{ marginBottom: 0 }}>
          <label>2. Chọn tuần làm việc đã chốt sổ:</label>
          
          {/* 👉 ĐÃ XÓA className="sd-input-search" */}
          <select value={selectedPeriodId} onChange={(e) => setSelectedPeriodId(e.target.value)} disabled={periods.length === 0}>
            {periods.length === 0 ? (
              <option value="">-- Canteen này chưa có lịch chốt chính thức --</option>
            ) : (
              periods.map(p => <option key={p.id} value={p.id}>Từ {formatDate(p.startDate)} đến {formatDate(p.endDate)}</option>)
            )}
          </select>
        </div>
      </div>

      {/* HIỂN THỊ KẾT QUẢ MA TRẬN CHỈ XEM (READ-ONLY) */}
      <div style={{ padding: '0 20px' }}>
        {loading ? <p>Đang tải dữ liệu lịch làm việc...</p> : periods.length === 0 ? (
          <div className="sd-empty-state" style={{ padding: '30px 0' }}><span className="sd-empty-icon">🗓️</span><p>Cơ sở này hiện chưa được Quản lý xuất bản (Publish) lịch làm việc.</p></div>
        ) : (
          <div className="sd-board-wrap" style={{ borderRadius: 12 }}>
            <table className="sd-schedule-board">
              <thead>
                <tr>
                  <th style={{ width: 90 }}>NGÀY</th>
                  {shifts.map(s => <th key={s.id}>{s.shiftName}<br/><span style={{fontWeight: 500, fontSize: 11}}>{s.startTime?.slice(0, 5)} - {s.endTime?.slice(0, 5)}</span></th>)}
                </tr>
              </thead>
              <tbody>
                {dates.map((dateObj) => {
                  const dStr = toDateString(dateObj)
                  const dayOfWeek = DAY_NAMES[dateObj.getDay()]
                  return (
                    <tr key={dStr}>
                      <td className="sd-board-date-col"><strong>{dayOfWeek}</strong><small>{dateObj.getDate()}/{dateObj.getMonth() + 1}</small></td>
                      {shifts.map(shift => {
                        const cellRegs = boardMatrix[dStr][shift.id] || []
                        return (
                          <td key={shift.id}>
                            <div className="sd-reg-card" style={{ background: '#ffedd5', borderColor: '#fdba74', color: '#9a3412' }}>
                              <span className="sd-reg-name">👑 Quản lý ca</span>
                            </div>
                            {cellRegs.map(r => (
                              <div key={r.id} className="sd-reg-cardapproved" style={{ background: '#f8fafc', borderColor: '#e2e8f0', color: '#475569', padding: '6px 8px', borderRadius: 6, marginBottom: 6, fontSize: 12, fontWeight: 600 }}>
                                <span>{r.user?.fullName}</span>
                              </div>
                            ))}
                          </td>
                        )
                      })}
                    </tr>
                  )
                })}
              </tbody>
            </table>
          </div>
        )}
      </div>
    </div>
  )
}
