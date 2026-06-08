import { useState, useEffect } from 'react'
import { updateUser, GetALLBranh } from '../api/UserApi'
import axios from 'axios'
import './css/admindashboard.css'

function getInitials(name = '') {
  return name.split(' ').filter(Boolean).slice(-2).map((p) => p[0]).join('').toUpperCase()
}

function formatDate(value) {
  if (!value) return '—'
  return new Intl.DateTimeFormat('vi-VN').format(new Date(value))
}

const EMPTY_FORM = {
  username: '', fullName: '', password: '',
  branchId: '', branchName: '', roleId: '', roleName: '', hireDate: '',
}

const ROLE_COLORS = {
  ADMIN:   { bg: '#fef3c7', color: '#92400e' },
  MANAGER: { bg: '#dbeafe', color: '#1e40af' },
  STAFF:   { bg: '#dcfce7', color: '#166534' },
}

export function AdminDashboard({ onLogout, onUserUpdated, roles, user, users: initUsers }) {
  const [activeTab, setActiveTab] = useState('overview')
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

  useEffect(() => {
    GetALLBranh()
      .then((data) => setBranches(Array.isArray(data) ? data : []))
      .catch(() => setBranches([]))
  }, [])

  const branch = branches.find((b) => b.id === user.branchId)

  const displayed = users
    .filter((u) => {
      const matchSearch = [u.fullName, u.username, u.branchName].some((v) =>
        v?.toLowerCase().includes(search.toLowerCase()))
      const matchRole = filterRole === 'ALL' || u.roleName?.toUpperCase() === filterRole
      return matchSearch && matchRole
    })
    .sort((a, b) => {
      const va = (a[sortCol] || '').toString().toLowerCase()
      const vb = (b[sortCol] || '').toString().toLowerCase()
      return sortDir === 'asc' ? va.localeCompare(vb) : vb.localeCompare(va)
    })

  function toggleSort(col) {
    if (sortCol === col) setSortDir((d) => (d === 'asc' ? 'desc' : 'asc'))
    else { setSortCol(col); setSortDir('asc') }
  }

  function SortIcon({ col }) {
    if (sortCol !== col) return <span className="sd-sort-icon sd-sort-none">↕</span>
    return <span className="sd-sort-icon">{sortDir === 'asc' ? '↑' : '↓'}</span>
  }

  function openAdd() { setForm(EMPTY_FORM); setFormErr(''); setModal('add') }
  function openEdit(u) { setForm({ ...u }); setFormErr(''); setModalUser(u); setModal('edit') }
  function openDelete(u) { setModalUser(u); setFormErr(''); setModal('delete') }
  function closeModal() { setModal(null); setModalUser(null) }

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
      setFormErr('Vui lòng điền đầy đủ username, họ tên, password'); return
    }
    setSaving(true); setFormErr('')
    try {
      const res = await axios.post('/api/User', form)
      setUsers((prev) => [...prev, res.data])
      closeModal()
    } catch (err) { setFormErr(err.message || 'Không thể thêm nhân viên') }
    finally { setSaving(false) }
  }

  async function handleSaveEdit() {
    if (!form.username || !form.fullName) {
      setFormErr('Username và họ tên không được để trống'); return
    }
    setSaving(true); setFormErr('')
    try {
      await updateUser(form.id, form)
      setUsers((prev) => prev.map((u) => (u.id === form.id ? { ...u, ...form } : u)))
      if (form.id === user.id) onUserUpdated({ ...user, ...form })
      closeModal()
    } catch (err) { setFormErr(err.message || 'Không thể cập nhật') }
    finally { setSaving(false) }
  }

  async function handleDelete() {
    setSaving(true)
    try {
      await axios.delete(`/api/User/${modalUser.id}`)
      setUsers((prev) => prev.filter((u) => u.id !== modalUser.id))
      closeModal()
    } catch (err) { setFormErr(err.message || 'Không thể xóa') }
    finally { setSaving(false) }
  }

  const countByRole = (r) => users.filter((u) => u.roleName?.toUpperCase() === r).length

  const getHeaderInfo = () => {
    switch (activeTab) {
      case 'overview': return { eyebrow: 'Hệ thống', title: 'Tổng quan' }
      case 'users':    return { eyebrow: 'Quản lý', title: 'Nhân sự' }
      case 'account':  return { eyebrow: 'Cài đặt', title: 'Tài khoản' }
      default:         return { eyebrow: '', title: '' }
    }
  }
  const headerInfo = getHeaderInfo()

  const NAV_ITEMS = [
    { id: 'overview', icon: '⬡', label: 'Tổng quan' },
    { id: 'users',    icon: '◈', label: 'Nhân viên' },
    { id: 'account',  icon: '◎', label: 'Tài khoản' },
  ]

  return (
    <div className="sd-root sd-root--left-nav">
      {/* Topbar */}
      <header className="sd-topbar">
        <div className="sd-brand">
          <span className="sd-brand-icon">CT</span>
          <span className="sd-brand-name">Canteen Admin</span>
        </div>
        <button className="sd-logout-btn" onClick={onLogout}>
          <span>Đăng xuất</span> ↩
        </button>
      </header>

      <div className="sd-layout">
        {/* LEFT SIDE NAV */}
        <nav className="sd-left-nav">
          <div className="sd-left-nav-user">
            <div className="sd-info-avatar sd-avatar-sm">
              {getInitials(user.fullName || user.username)}
            </div>
            <span className="sd-left-nav-name">{user.fullName || user.username}</span>
          </div>
          <div className="sd-left-nav-items">
            {NAV_ITEMS.map((item) => (
              <button
                key={item.id}
                className={`sd-left-nav-item ${activeTab === item.id ? 'active' : ''}`}
                onClick={() => setActiveTab(item.id)}
                type="button"
              >
                <span className="sd-nav-icon">{item.icon}</span>
                <span className="sd-nav-label">{item.label}</span>
              </button>
            ))}
          </div>
          <button className="sd-left-nav-logout" onClick={onLogout}>↩ Đăng xuất</button>
        </nav>

        {/* Main content */}
        <main className="sd-main">
          <div className="sd-page-header">
            <div>
              <p className="sd-eyebrow">{headerInfo.eyebrow}</p>
              <h1>{headerInfo.title}</h1>
            </div>
            <div className="sd-branch-badge">Quyền Quản Trị</div>
          </div>

          <div className="sd-content">

            {/* ── OVERVIEW ── */}
            {activeTab === 'overview' && (
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
                  {roles.filter((r) => r.roleName !== 'ADMIN').map((r) => {
                    const cnt = countByRole(r.roleName)
                    const pct = users.length ? Math.round((cnt / users.length) * 100) : 0
                    return (
                      <div key={r.id} className="sd-role-bar">
                        <div className="sd-role-bar-head">
                          <strong>{r.roleName}</strong>
                          <span>{cnt} người · {pct}%</span>
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

            {/* ── USERS — Full-width table ── */}
            {activeTab === 'users' && (
              <div className="sd-users-page">
                {/* Toolbar */}
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
                        <button className="sd-search-clear" onClick={() => setSearch('')}>✕</button>
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
                          {r !== 'ALL' && (
                            <span className="sd-chip-count">{countByRole(r)}</span>
                          )}
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

                {/* Table */}
                <div className="sd-table-wrap">
                  <table className="sd-table">
                    <thead>
                      <tr>
                        <th className="sd-th sd-th-avatar" style={{ width: 48 }}></th>
                        <th className="sd-th sd-th-sortable" onClick={() => toggleSort('fullName')}>
                          Họ và tên <SortIcon col="fullName" />
                        </th>
                        <th className="sd-th sd-th-sortable" onClick={() => toggleSort('username')}>
                          Username <SortIcon col="username" />
                        </th>
                        <th className="sd-th sd-th-sortable" onClick={() => toggleSort('roleName')}>
                          Chức vụ <SortIcon col="roleName" />
                        </th>
                        <th className="sd-th sd-th-sortable" onClick={() => toggleSort('branchName')}>
                          Chi nhánh <SortIcon col="branchName" />
                        </th>
                        <th className="sd-th sd-th-sortable" onClick={() => toggleSort('hireDate')}>
                          Ngày vào làm <SortIcon col="hireDate" />
                        </th>
                        <th className="sd-th sd-th-actions">Thao tác</th>
                      </tr>
                    </thead>
                    <tbody>
                      {displayed.length === 0 && (
                        <tr>
                          <td colSpan={7} className="sd-td-empty">
                            <div className="sd-empty-state">
                              <span className="sd-empty-icon">◈</span>
                              <p>Không tìm thấy nhân sự phù hợp</p>
                            </div>
                          </td>
                        </tr>
                      )}
                      {displayed.map((u, idx) => {
                        const roleColor = ROLE_COLORS[u.roleName?.toUpperCase()] || { bg: '#f1f5f9', color: '#475569' }
                        return (
                          <tr key={u.id} className="sd-tr" style={{ animationDelay: `${idx * 30}ms` }}>
                            <td className="sd-td sd-td-avatar">
                              <div className="sd-info-avatar sd-avatar-sm">
                                {getInitials(u.fullName || u.username)}
                              </div>
                            </td>
                            <td className="sd-td">
                              <span className="sd-td-name">{u.fullName || '—'}</span>
                            </td>
                            <td className="sd-td">
                              <span className="sd-td-username">@{u.username}</span>
                            </td>
                            <td className="sd-td">
                              <span
                                className="sd-role-pill"
                                style={{ background: roleColor.bg, color: roleColor.color }}
                              >
                                {u.roleName || '—'}
                              </span>
                            </td>
                            <td className="sd-td">
                              <span className="sd-td-branch">{u.branchName || <em className="sd-muted">Chưa gán</em>}</span>
                            </td>
                            <td className="sd-td">
                              <span className="sd-td-date">{formatDate(u.hireDate)}</span>
                            </td>
                            <td className="sd-td sd-td-actions">
                              <button
                                className="sd-action-btn sd-action-edit"
                                onClick={() => openEdit(u)}
                                title="Chỉnh sửa"
                              >
                                ✎
                              </button>
                              {u.id !== user.id && (
                                <button
                                  className="sd-action-btn sd-action-delete"
                                  onClick={() => openDelete(u)}
                                  title="Xoá"
                                >
                                  ✕
                                </button>
                              )}
                            </td>
                          </tr>
                        )
                      })}
                    </tbody>
                  </table>
                </div>
              </div>
            )}

            {/* ── ACCOUNT ── */}
            {activeTab === 'account' && (
              <div className="sd-profile-layout">
                <div className="sd-card">
                  <div className="sd-card-header">
                    <p className="sd-eyebrow">Chi tiết</p>
                    <h2>Hồ sơ Admin</h2>
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

      {/* MODALS */}
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
                  <input name="fullName" value={form.fullName} onChange={handleFormChange} placeholder="Nguyễn Văn A" />
                </div>
                <div className="sd-field">
                  <label>Username *</label>
                  <input name="username" value={form.username} onChange={handleFormChange} placeholder="nguyenvana" />
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
                    {roles.map((r) => <option key={r.id} value={r.id}>{r.roleName}</option>)}
                  </select>
                </div>
                <div className="sd-field">
                  <label>Chi nhánh</label>
                  <select name="branchId" value={form.branchId || ''} onChange={handleFormChange}>
                    <option value="">-- Chọn chi nhánh --</option>
                    {branches.map((b) => <option key={b.id} value={b.id}>{b.name || b.branchName}</option>)}
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
              <p style={{ fontSize: 14, color: '#475569', lineHeight: 1.6 }}>
                Bạn có chắc muốn xoá nhân viên <strong>{modalUser?.fullName}</strong>?
                Hành động này không thể hoàn tác.
              </p>
              {formErr && <p className="sd-status sd-status-error">{formErr}</p>}
            </div>
            <div className="sd-modal-footer">
              <button className="sd-btn-ghost" onClick={closeModal}>Huỷ</button>
              <button
                className="sd-btn-primary"
                style={{ background: '#ef4444' }}
                disabled={saving}
                onClick={handleDelete}
              >
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
    const { name, value } = e.target
    setForm((f) => ({ ...f, [name]: value }))
  }

  async function handleSubmit(e) {
    e.preventDefault()
    setStatus(null)
    if (form.currentPassword !== user.password) {
      setStatus({ type: 'error', msg: 'Mật khẩu hiện tại không đúng' }); return
    }
    if (form.newPassword.length < 4) {
      setStatus({ type: 'error', msg: 'Mật khẩu mới cần tối thiểu 4 ký tự' }); return
    }
    if (form.newPassword !== form.confirmPassword) {
      setStatus({ type: 'error', msg: 'Nhập lại mật khẩu chưa khớp' }); return
    }
    try {
      setIsSaving(true)
      const updatedUser = { ...user, password: form.newPassword }
      await updateUser(user.id, updatedUser)
      onUserUpdated(updatedUser)
      setForm({ currentPassword: '', newPassword: '', confirmPassword: '' })
      setStatus({ type: 'success', msg: 'Đã cập nhật mật khẩu' })
    } catch (err) {
      setStatus({ type: 'error', msg: err.message || 'Lỗi cập nhật' })
    } finally { setIsSaving(false) }
  }

  return (
    <form className="sd-pw-form" onSubmit={handleSubmit}>
      {['currentPassword', 'newPassword', 'confirmPassword'].map((field) => (
        <div key={field} className="sd-field">
          <label>
            {field === 'currentPassword' ? 'Mật khẩu hiện tại'
              : field === 'newPassword' ? 'Mật khẩu mới'
              : 'Nhập lại mật khẩu'}
          </label>
          <input name={field} onChange={handleChange} type="password" value={form[field]} />
        </div>
      ))}
      {status && <p className={`sd-status sd-status-${status.type}`}>{status.msg}</p>}
      <button className="sd-btn-primary" disabled={isSaving} type="submit">
        {isSaving ? 'Đang lưu…' : 'Cập nhật mật khẩu'}
      </button>
    </form>
  )
}