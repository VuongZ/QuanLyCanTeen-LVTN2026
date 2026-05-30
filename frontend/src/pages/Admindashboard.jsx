import { useState, useEffect } from 'react'
import { updateUser, GetALLBranh } from '../api/UserApi'
import axios from 'axios'

/* ─── helpers ─────────────────────────────────────────── */
function getInitials(name = '') {
  return name.split(' ').filter(Boolean).slice(-2).map((p) => p[0]).join('').toUpperCase()
}
function formatDate(value) {
  if (!value) return '—'
  return new Intl.DateTimeFormat('vi-VN').format(new Date(value))
}

const ROLE_COLOR = { ADMIN: '#1d4ed8', MANAGER: '#0891b2', STAFF: '#059669', default: '#7c3aed' }
function roleColor(name = '') { return ROLE_COLOR[name.toUpperCase()] || ROLE_COLOR.default }

const NAV_ITEMS = [
  { id: 'overview', label: 'Tổng quan',   icon: '⬡' },
  { id: 'users',    label: 'Nhân viên',   icon: '◈' },
  { id: 'account',  label: 'Tài khoản',   icon: '◉' },
]

const EMPTY_FORM = {
  username: '', fullName: '', password: '',
  branchId: '', branchName: '', roleId: '', roleName: '', hireDate: '',
}

/* ─── main ─────────────────────────────────────────────── */
export function AdminDashboard({ onLogout, onUserUpdated, roles, user, users: initUsers }) {
  const [activeNav, setActiveNav]   = useState('overview')
  const [users, setUsers]           = useState(initUsers)
  const [branches, setBranches]     = useState([])

  /* modal state */
  const [modal, setModal]           = useState(null)
  const [modalUser, setModalUser]   = useState(null)
  const [form, setForm]             = useState(EMPTY_FORM)
  const [formErr, setFormErr]       = useState('')
  const [saving, setSaving]         = useState(false)

  /* search/filter */
  const [search, setSearch]         = useState('')
  const [filterRole, setFilterRole] = useState('ALL')

  /* fetch branches on mount */
  useEffect(() => {
    GetALLBranh()
      .then((data) => setBranches(Array.isArray(data) ? data : []))
      .catch(() => setBranches([]))
  }, [])

  const branch = branches.find((b) => b.id === user.branchId)

  /* ── filter users ── */
  const displayed = users.filter((u) => {
    const matchSearch = [u.fullName, u.username, u.branchName].some((v) =>
      v?.toLowerCase().includes(search.toLowerCase()))
    const matchRole = filterRole === 'ALL' || u.roleName?.toUpperCase() === filterRole
    return matchSearch && matchRole
  })

  /* ── modal helpers ── */
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

  return (
    <>
      <style>{CSS}</style>
      <div className="ad-root">
        {/* sidebar */}
        <aside className="ad-sidebar">
          <div className="ad-logo">
            <span className="ad-logo-mark">CT</span>
            <div className="ad-logo-text">
              <span className="ad-logo-name">Canteen</span>
              <span className="ad-logo-sub">Admin Portal</span>
            </div>
          </div>
          <nav className="ad-nav">
            {NAV_ITEMS.map((item) => (
              <button
                key={item.id}
                className={`ad-nav-item ${activeNav === item.id ? 'active' : ''}`}
                onClick={() => setActiveNav(item.id)}
              >
                <span className="ad-nav-icon">{item.icon}</span>
                <span>{item.label}</span>
              </button>
            ))}
          </nav>
          <div className="ad-sidebar-user">
            <span className="ad-avatar sm">{getInitials(user.fullName || user.username)}</span>
            <div className="ad-sidebar-user-info">
              <strong>{user.fullName || user.username}</strong>
              <span>{user.roleName}</span>
            </div>
            <button className="ad-logout-btn" onClick={onLogout} title="Đăng xuất">↩</button>
          </div>
        </aside>

        {/* main content */}
        <div className="ad-main">

          {/* ── OVERVIEW ── */}
          {activeNav === 'overview' && (
            <div className="ad-page">
              <header className="ad-page-header">
                <div>
                  <p className="ad-eyebrow">Canteen Management System</p>
                  <h1 className="ad-title">Tổng Quan Hệ Thống</h1>
                </div>
                <span className="ad-badge admin">ADMIN</span>
              </header>

              <div className="ad-stat-grid">
                <StatCard icon="◈" label="Tổng nhân viên" value={users.length}     accent="#2563eb" />
                <StatCard icon="⬡" label="Manager"        value={countByRole('MANAGER')} accent="#0891b2" />
                <StatCard icon="◉" label="Staff"           value={countByRole('STAFF')}   accent="#059669" />
                <StatCard icon="⊞" label="Chi nhánh"       value={branches.length}        accent="#7c3aed" />
              </div>

              <div className="ad-overview-bottom">
                <div className="ad-card">
                  <p className="ad-card-label">Phân bổ theo role</p>
                  {roles.filter((r) => r.roleName !== 'ADMIN').map((r) => {
                    const cnt = countByRole(r.roleName)
                    const pct = users.length ? Math.round((cnt / users.length) * 100) : 0
                    return (
                      <div key={r.id} className="ad-role-bar">
                        <div className="ad-role-bar-head">
                          <span style={{ color: roleColor(r.roleName), fontWeight: 600 }}>{r.roleName}</span>
                          <span>{cnt} người · {pct}%</span>
                        </div>
                        <div className="ad-bar-track">
                          <div className="ad-bar-fill" style={{ width: `${pct}%`, background: roleColor(r.roleName) }} />
                        </div>
                      </div>
                    )
                  })}
                </div>

                <div className="ad-card">
                  <p className="ad-card-label">Thông tin Admin</p>
                  <div className="ad-profile-block">
                    <span className="ad-avatar lg">{getInitials(user.fullName || user.username)}</span>
                    <div>
                      <h3>{user.fullName || user.username}</h3>
                      <p className="ad-sub">@{user.username}</p>
                      <p className="ad-sub">{branch?.name || branch?.branchName || user.branchName || '—'}</p>
                      <p className="ad-sub">Vào làm: {formatDate(user.hireDate)}</p>
                    </div>
                  </div>
                </div>
              </div>
            </div>
          )}

          {/* ── USERS TABLE ── */}
          {activeNav === 'users' && (
            <div className="ad-page">
              <header className="ad-page-header">
                <div>
                  <p className="ad-eyebrow">Quản lý</p>
                  <h1 className="ad-title">Danh Sách Nhân Viên</h1>
                </div>
                <button className="ad-btn primary" onClick={openAdd}>+ Thêm nhân viên</button>
              </header>

              <div className="ad-toolbar">
                <input
                  className="ad-search"
                  placeholder="🔍  Tìm theo tên, username, chi nhánh..."
                  value={search}
                  onChange={(e) => setSearch(e.target.value)}
                />
                <div className="ad-filter-tabs">
                  {['ALL', 'ADMIN', 'MANAGER', 'STAFF'].map((r) => (
                    <button
                      key={r}
                      className={`ad-filter-tab ${filterRole === r ? 'active' : ''}`}
                      onClick={() => setFilterRole(r)}
                    >{r === 'ALL' ? 'Tất cả' : r}</button>
                  ))}
                </div>
              </div>

              <div className="ad-table-wrap">
                <table className="ad-table">
                  <thead>
                    <tr>
                      <th>Nhân viên</th>
                      <th>Username</th>
                      <th>Role</th>
                      <th>Chi nhánh</th>
                      <th>Vào làm</th>
                      <th></th>
                    </tr>
                  </thead>
                  <tbody>
                    {displayed.length === 0 && (
                      <tr><td colSpan={6} className="ad-empty">Không tìm thấy nhân viên nào</td></tr>
                    )}
                    {displayed.map((u) => (
                      <tr key={u.id} className="ad-tr">
                        <td>
                          <div className="ad-user-cell">
                            <span className="ad-avatar sm" style={{ background: roleColor(u.roleName) }}>
                              {getInitials(u.fullName || u.username)}
                            </span>
                            <span className="ad-fullname">{u.fullName || '—'}</span>
                          </div>
                        </td>
                        <td className="ad-muted">@{u.username}</td>
                        <td>
                          <span className="ad-role-chip" style={{ '--rc': roleColor(u.roleName) }}>
                            {u.roleName}
                          </span>
                        </td>
                        <td className="ad-muted">{u.branchName || '—'}</td>
                        <td className="ad-muted">{formatDate(u.hireDate)}</td>
                        <td>
                          <div className="ad-actions">
                            <button className="ad-icon-btn edit" onClick={() => openEdit(u)} title="Sửa">✎</button>
                            {u.id !== user.id && (
                              <button className="ad-icon-btn del" onClick={() => openDelete(u)} title="Xóa">✕</button>
                            )}
                          </div>
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
              <p className="ad-count">Hiển thị {displayed.length} / {users.length} nhân viên</p>
            </div>
          )}

          {/* ── ACCOUNT ── */}
          {activeNav === 'account' && (
            <div className="ad-page">
              <header className="ad-page-header">
                <div>
                  <p className="ad-eyebrow">Cài đặt</p>
                  <h1 className="ad-title">Tài Khoản Của Tôi</h1>
                </div>
              </header>
              <div className="ad-account-grid">
                <div className="ad-card">
                  <p className="ad-card-label">Thông tin cá nhân</p>
                  <div className="ad-profile-block">
                    <span className="ad-avatar xl">{getInitials(user.fullName || user.username)}</span>
                    <div>
                      <h2 style={{ fontFamily: "'Playfair Display', serif", fontWeight: 600, fontSize: 20, color: '#111827' }}>{user.fullName || user.username}</h2>
                      <p className="ad-sub">@{user.username}</p>
                    </div>
                  </div>
                  <dl className="ad-dl">
                    <div><dt>Role</dt><dd>{user.roleName}</dd></div>
                    <div><dt>Chi nhánh</dt><dd>{branch?.name || branch?.branchName || user.branchName || '—'}</dd></div>
                    <div><dt>Ngày vào làm</dt><dd>{formatDate(user.hireDate)}</dd></div>
                  </dl>
                </div>
                <div className="ad-card">
                  <PasswordForm user={user} onUserUpdated={onUserUpdated} />
                </div>
              </div>
            </div>
          )}
        </div>
      </div>

      {/* ── ADD / EDIT MODAL ── */}
      {(modal === 'add' || modal === 'edit') && (
        <div className="ad-overlay" onClick={closeModal}>
          <div className="ad-modal" onClick={(e) => e.stopPropagation()}>
            <div className="ad-modal-header">
              <h2>{modal === 'add' ? '+ Thêm nhân viên' : '✎ Chỉnh sửa nhân viên'}</h2>
              <button className="ad-modal-close" onClick={closeModal}>✕</button>
            </div>
            <div className="ad-modal-body">
              <div className="ad-form-row">
                <FormField label="Họ và tên *" name="fullName" value={form.fullName} onChange={handleFormChange} placeholder="Nguyễn Văn A" />
                <FormField label="Username *" name="username" value={form.username} onChange={handleFormChange} placeholder="nguyenvana" />
              </div>
              <div className="ad-form-row">
                <FormField label="Password *" name="password" value={form.password} onChange={handleFormChange} type="password" placeholder="••••••" />
                <FormField label="Ngày vào làm" name="hireDate" value={form.hireDate?.slice(0, 10) || ''} onChange={handleFormChange} type="date" />
              </div>
              <div className="ad-form-row">
                <div className="ad-form-field">
                  <label>Role</label>
                  <select name="roleId" value={form.roleId || ''} onChange={handleFormChange}>
                    <option value="">-- Chọn role --</option>
                    {roles.map((r) => <option key={r.id} value={r.id}>{r.roleName}</option>)}
                  </select>
                </div>
                <div className="ad-form-field">
                  <label>Chi nhánh</label>
                  <select name="branchId" value={form.branchId || ''} onChange={handleFormChange}>
                    <option value="">-- Chọn chi nhánh --</option>
                    {branches.map((b) => <option key={b.id} value={b.id}>{b.name || b.branchName}</option>)}
                  </select>
                </div>
              </div>
              {formErr && <p className="ad-form-err">{formErr}</p>}
            </div>
            <div className="ad-modal-footer">
              <button className="ad-btn ghost" onClick={closeModal}>Huỷ</button>
              <button className="ad-btn primary" disabled={saving} onClick={modal === 'add' ? handleSaveAdd : handleSaveEdit}>
                {saving ? 'Đang lưu...' : 'Lưu'}
              </button>
            </div>
          </div>
        </div>
      )}

      {/* ── DELETE MODAL ── */}
      {modal === 'delete' && (
        <div className="ad-overlay" onClick={closeModal}>
          <div className="ad-modal sm" onClick={(e) => e.stopPropagation()}>
            <div className="ad-modal-header">
              <h2>Xác nhận xoá</h2>
              <button className="ad-modal-close" onClick={closeModal}>✕</button>
            </div>
            <div className="ad-modal-body">
              <p style={{ color: '#6b7280', fontSize: 14, lineHeight: 1.6 }}>
                Bạn có chắc muốn xoá nhân viên <strong style={{ color: '#111827' }}>{modalUser?.fullName}</strong>?<br />
                Hành động này không thể hoàn tác.
              </p>
              {formErr && <p className="ad-form-err">{formErr}</p>}
            </div>
            <div className="ad-modal-footer">
              <button className="ad-btn ghost" onClick={closeModal}>Huỷ</button>
              <button className="ad-btn danger" disabled={saving} onClick={handleDelete}>
                {saving ? 'Đang xoá...' : 'Xoá nhân viên'}
              </button>
            </div>
          </div>
        </div>
      )}
    </>
  )
}

/* ─── sub-components ───────────────────────────────────── */
function StatCard({ icon, label, value, accent }) {
  return (
    <div className="ad-stat-card" style={{ '--ac': accent }}>
      <div className="ad-stat-icon-wrap">
        <span className="ad-stat-icon">{icon}</span>
      </div>
      <div>
        <p className="ad-stat-label">{label}</p>
        <p className="ad-stat-value">{value}</p>
      </div>
    </div>
  )
}

function FormField({ label, name, value, onChange, type = 'text', placeholder }) {
  return (
    <div className="ad-form-field">
      <label>{label}</label>
      <input type={type} name={name} value={value} onChange={onChange} placeholder={placeholder} />
    </div>
  )
}

function PasswordForm({ user, onUserUpdated }) {
  const [form, setForm]     = useState({ currentPassword: '', newPassword: '', confirmPassword: '' })
  const [status, setStatus] = useState('')
  const [isSaving, setIsSaving] = useState(false)

  async function handleSubmit(e) {
    e.preventDefault(); setStatus('')
    if (form.currentPassword !== user.password) { setStatus('Password hiện tại không đúng'); return }
    if (form.newPassword.length < 4) { setStatus('Password mới cần tối thiểu 4 ký tự'); return }
    if (form.newPassword !== form.confirmPassword) { setStatus('Nhập lại password chưa khớp'); return }
    setIsSaving(true)
    try {
      const updated = { ...user, password: form.newPassword }
      await updateUser(user.id, updated)
      onUserUpdated(updated)
      setForm({ currentPassword: '', newPassword: '', confirmPassword: '' })
      setStatus('✓ Đã cập nhật password')
    } catch (err) { setStatus(err.message || 'Không thể cập nhật') }
    finally { setIsSaving(false) }
  }

  return (
    <form onSubmit={handleSubmit}>
      <p className="ad-card-label">Đổi mật khẩu</p>
      <div className="ad-pw-fields">
        {[['currentPassword', 'Password hiện tại'], ['newPassword', 'Password mới'], ['confirmPassword', 'Nhập lại password mới']].map(([name, ph]) => (
          <input key={name} type="password" name={name} placeholder={ph}
            value={form[name]} onChange={(e) => setForm((f) => ({ ...f, [name]: e.target.value }))} />
        ))}
      </div>
      {status && <p className={`ad-form-status ${status.startsWith('✓') ? 'ok' : 'err'}`}>{status}</p>}
      <button className="ad-btn primary" disabled={isSaving} type="submit">
        {isSaving ? 'Đang lưu...' : 'Cập nhật mật khẩu'}
      </button>
    </form>
  )
}

/* ─── styles ───────────────────────────────────────────── */
const CSS = `
@import url('https://fonts.googleapis.com/css2?family=Playfair+Display:wght@600;700&family=Plus+Jakarta+Sans:wght@300;400;500;600&display=swap');

*, *::before, *::after { box-sizing: border-box; margin: 0; padding: 0; }

:root {
  --blue:      #2563eb;
  --blue-dark: #1d4ed8;
  --blue-soft: #eff6ff;
  --blue-mid:  #dbeafe;
  --text:      #111827;
  --text-sub:  #6b7280;
  --text-mute: #9ca3af;
  --border:    #e5e7eb;
  --bg:        #f8fafc;
  --card:      #ffffff;
  --sidebar:   #1e3a5f;
}

.ad-root {
  display: flex; min-height: 100vh;
  background: var(--bg);
  color: var(--text);
  font-family: 'Plus Jakarta Sans', sans-serif;
}

/* ── SIDEBAR ── */
.ad-sidebar {
  width: 230px; flex-shrink: 0;
  background: var(--sidebar);
  display: flex; flex-direction: column;
  padding: 0 0 20px;
  box-shadow: 2px 0 12px rgba(37,99,235,.12);
}
.ad-logo {
  display: flex; align-items: center; gap: 12px;
  padding: 24px 20px 22px;
  border-bottom: 1px solid rgba(255,255,255,.08);
}
.ad-logo-mark {
  width: 40px; height: 40px; flex-shrink: 0;
  background: var(--blue);
  border-radius: 10px; display: grid; place-items: center;
  font-family: 'Playfair Display', serif;
  font-size: 15px; font-weight: 700; color: #fff;
  box-shadow: 0 4px 12px rgba(37,99,235,.4);
}
.ad-logo-name  { display: block; font-size: 14px; font-weight: 600; color: #fff; }
.ad-logo-sub   { display: block; font-size: 10px; color: rgba(255,255,255,.4); letter-spacing:.05em; text-transform: uppercase; margin-top: 2px; }

.ad-nav { flex: 1; padding: 16px 12px; display: flex; flex-direction: column; gap: 3px; }
.ad-nav-item {
  display: flex; align-items: center; gap: 10px;
  padding: 10px 13px; border-radius: 9px;
  border: none; background: transparent;
  color: rgba(255,255,255,.45); font-size: 13.5px;
  font-family: 'Plus Jakarta Sans', sans-serif;
  font-weight: 500;
  cursor: pointer; text-align: left; transition: all .15s;
}
.ad-nav-item:hover { background: rgba(255,255,255,.07); color: rgba(255,255,255,.8); }
.ad-nav-item.active { background: var(--blue); color: #fff; box-shadow: 0 4px 12px rgba(37,99,235,.35); }
.ad-nav-icon { font-size: 15px; width: 18px; text-align: center; }

.ad-sidebar-user {
  display: flex; align-items: center; gap: 9px;
  padding: 14px 14px 0;
  border-top: 1px solid rgba(255,255,255,.08);
}
.ad-sidebar-user-info { flex: 1; min-width: 0; }
.ad-sidebar-user-info strong { display: block; font-size: 12.5px; white-space: nowrap; overflow: hidden; text-overflow: ellipsis; color: #fff; }
.ad-sidebar-user-info span { font-size: 10.5px; color: rgba(255,255,255,.45); }

.ad-logout-btn {
  background: none; border: 1px solid rgba(255,255,255,.15); border-radius: 8px;
  color: rgba(255,255,255,.4); font-size: 14px; width: 28px; height: 28px;
  cursor: pointer; transition: all .15s; flex-shrink: 0;
  display: grid; place-items: center;
}
.ad-logout-btn:hover { color: #fca5a5; border-color: #fca5a5; }

/* ── AVATARS ── */
.ad-avatar {
  border-radius: 50%; display: grid; place-items: center;
  font-family: 'Playfair Display', serif; font-weight: 700;
  background: var(--blue); color: #fff; flex-shrink: 0;
}
.ad-avatar.sm  { width: 32px; height: 32px; font-size: 12px; }
.ad-avatar.lg  { width: 52px; height: 52px; font-size: 18px; }
.ad-avatar.xl  { width: 68px; height: 68px; font-size: 24px; }

/* ── MAIN ── */
.ad-main { flex: 1; overflow: auto; background: var(--bg); }
.ad-page { padding: 36px 40px; max-width: 1100px; animation: adFadeUp .25s ease; }
@keyframes adFadeUp { from { opacity: 0; transform: translateY(10px); } to { opacity: 1; transform: none; } }

.ad-page-header { display: flex; justify-content: space-between; align-items: flex-start; margin-bottom: 28px; }
.ad-eyebrow { font-size: 11px; text-transform: uppercase; letter-spacing: .1em; color: var(--blue); margin-bottom: 4px; font-weight: 600; }
.ad-title { font-family: 'Playfair Display', serif; font-size: 28px; font-weight: 700; color: var(--text); }

.ad-badge { padding: 5px 13px; border-radius: 20px; font-size: 11px; font-weight: 700; letter-spacing: .08em; }
.ad-badge.admin { background: var(--blue-mid); color: var(--blue-dark); border: 1px solid #bfdbfe; }

/* ── STAT CARDS ── */
.ad-stat-grid { display: grid; grid-template-columns: repeat(4, 1fr); gap: 14px; margin-bottom: 20px; }
.ad-stat-card {
  background: var(--card); border: 1px solid var(--border);
  border-radius: 14px; padding: 20px;
  display: flex; align-items: center; gap: 16px;
  transition: transform .15s, box-shadow .15s;
  box-shadow: 0 1px 3px rgba(0,0,0,.05);
}
.ad-stat-card:hover { transform: translateY(-2px); box-shadow: 0 8px 24px rgba(0,0,0,.08); }
.ad-stat-icon-wrap {
  width: 46px; height: 46px; flex-shrink: 0;
  background: color-mix(in srgb, var(--ac) 10%, transparent);
  border-radius: 12px; display: grid; place-items: center;
}
.ad-stat-icon { font-size: 20px; color: var(--ac); }
.ad-stat-label { font-size: 11px; color: var(--text-sub); text-transform: uppercase; letter-spacing: .05em; margin-bottom: 3px; font-weight: 500; }
.ad-stat-value { font-family: 'Playfair Display', serif; font-size: 26px; font-weight: 700; color: var(--text); }

/* ── CARDS ── */
.ad-overview-bottom { display: grid; grid-template-columns: 1fr 1fr; gap: 14px; }
.ad-card {
  background: var(--card); border: 1px solid var(--border);
  border-radius: 14px; padding: 22px;
  box-shadow: 0 1px 3px rgba(0,0,0,.05);
}
.ad-card-label { font-size: 10.5px; text-transform: uppercase; letter-spacing: .08em; color: var(--text-mute); margin-bottom: 16px; font-weight: 600; }
.ad-role-bar { margin-bottom: 14px; }
.ad-role-bar-head { display: flex; justify-content: space-between; font-size: 12.5px; margin-bottom: 7px; color: var(--text-sub); }
.ad-bar-track { height: 5px; background: var(--border); border-radius: 99px; overflow: hidden; }
.ad-bar-fill { height: 100%; border-radius: 99px; transition: width .7s cubic-bezier(.4,0,.2,1); }
.ad-profile-block { display: flex; align-items: center; gap: 14px; margin-bottom: 18px; }
.ad-profile-block h3 { font-size: 15px; font-weight: 600; color: var(--text); }
.ad-sub { font-size: 12px; color: var(--text-sub); margin-top: 3px; }

.ad-dl { display: grid; gap: 0; }
.ad-dl > div { display: flex; justify-content: space-between; font-size: 13px; padding: 10px 0; border-bottom: 1px solid var(--border); }
.ad-dl > div:last-child { border-bottom: none; }
.ad-dl dt { color: var(--text-sub); }
.ad-dl dd { color: var(--text); font-weight: 500; }

/* ── TOOLBAR ── */
.ad-toolbar { display: flex; gap: 10px; align-items: center; margin-bottom: 14px; flex-wrap: wrap; }
.ad-search {
  flex: 1; min-width: 200px; padding: 9px 14px;
  background: var(--card); border: 1.5px solid var(--border);
  border-radius: 9px; color: var(--text); font-family: inherit; font-size: 13px;
  transition: border-color .15s;
}
.ad-search:focus { outline: none; border-color: var(--blue); box-shadow: 0 0 0 3px rgba(37,99,235,.1); }
.ad-search::placeholder { color: var(--text-mute); }

.ad-filter-tabs { display: flex; gap: 5px; }
.ad-filter-tab {
  padding: 8px 13px; border-radius: 8px; font-size: 11.5px;
  font-family: inherit; font-weight: 600;
  border: 1.5px solid var(--border); background: var(--card);
  color: var(--text-sub); cursor: pointer; transition: all .15s;
}
.ad-filter-tab:hover:not(.active) { border-color: var(--blue); color: var(--blue); background: var(--blue-soft); }
.ad-filter-tab.active { background: var(--blue); border-color: var(--blue); color: #fff; }

/* ── TABLE ── */
.ad-table-wrap {
  background: var(--card); border: 1px solid var(--border);
  border-radius: 14px; overflow: auto;
  box-shadow: 0 1px 3px rgba(0,0,0,.05);
}
.ad-table { width: 100%; border-collapse: collapse; font-size: 13px; }
.ad-table th {
  padding: 12px 16px; text-align: left;
  font-size: 10.5px; text-transform: uppercase; letter-spacing: .06em;
  color: var(--text-mute); border-bottom: 1px solid var(--border);
  white-space: nowrap; font-weight: 600; background: #f9fafb;
}
.ad-tr { transition: background .1s; }
.ad-tr:hover { background: var(--blue-soft); }
.ad-table td { padding: 12px 16px; border-bottom: 1px solid #f3f4f6; vertical-align: middle; }
.ad-tr:last-child td { border-bottom: none; }
.ad-user-cell { display: flex; align-items: center; gap: 10px; }
.ad-fullname { font-weight: 600; color: var(--text); }
.ad-muted { color: var(--text-sub); }

.ad-role-chip {
  display: inline-block; padding: 3px 9px; border-radius: 20px;
  font-size: 10.5px; font-weight: 700; letter-spacing: .05em;
  background: color-mix(in srgb, var(--rc) 10%, transparent);
  color: var(--rc);
  border: 1px solid color-mix(in srgb, var(--rc) 25%, transparent);
}
.ad-actions { display: flex; gap: 5px; }
.ad-icon-btn {
  width: 30px; height: 30px; border-radius: 8px;
  border: 1.5px solid var(--border); background: transparent;
  cursor: pointer; font-size: 14px;
  display: grid; place-items: center; transition: all .15s;
}
.ad-icon-btn.edit { color: var(--blue); }
.ad-icon-btn.edit:hover { background: var(--blue-soft); border-color: var(--blue); }
.ad-icon-btn.del { color: #ef4444; }
.ad-icon-btn.del:hover { background: #fef2f2; border-color: #fca5a5; }
.ad-empty { text-align: center; color: var(--text-mute); padding: 48px; font-size: 13px; }
.ad-count { font-size: 11.5px; color: var(--text-mute); margin-top: 10px; text-align: right; }

/* ── BUTTONS ── */
.ad-btn {
  padding: 9px 18px; border-radius: 9px; font-family: inherit;
  font-size: 13px; font-weight: 600; cursor: pointer;
  border: none; transition: all .15s; white-space: nowrap;
}
.ad-btn.primary { background: var(--blue); color: #fff; box-shadow: 0 2px 8px rgba(37,99,235,.25); }
.ad-btn.primary:hover:not(:disabled) { background: var(--blue-dark); box-shadow: 0 4px 14px rgba(37,99,235,.35); }
.ad-btn.primary:disabled { opacity: .45; cursor: not-allowed; }
.ad-btn.ghost { background: transparent; border: 1.5px solid var(--border); color: var(--text-sub); }
.ad-btn.ghost:hover { border-color: var(--blue); color: var(--blue); background: var(--blue-soft); }
.ad-btn.danger { background: #ef4444; color: #fff; }
.ad-btn.danger:hover:not(:disabled) { background: #dc2626; }
.ad-btn.danger:disabled { opacity: .45; cursor: not-allowed; }

/* ── MODAL ── */
.ad-overlay {
  position: fixed; inset: 0;
  background: rgba(0,0,0,.3); backdrop-filter: blur(4px);
  display: grid; place-items: center; z-index: 200;
  animation: adFadeIn .15s ease;
}
@keyframes adFadeIn { from { opacity: 0; } to { opacity: 1; } }
.ad-modal {
  background: var(--card); border: 1px solid var(--border);
  border-radius: 16px; width: 560px; max-width: 95vw;
  box-shadow: 0 20px 60px rgba(0,0,0,.15);
  animation: adSlideUp .2s ease;
}
.ad-modal.sm { width: 400px; }
@keyframes adSlideUp { from { opacity:0; transform:translateY(16px); } to { opacity:1; transform:none; } }

.ad-modal-header {
  display: flex; justify-content: space-between; align-items: center;
  padding: 20px 24px; border-bottom: 1px solid var(--border);
}
.ad-modal-header h2 {
  font-family: 'Playfair Display', serif; font-size: 17px;
  font-weight: 700; color: var(--text);
}
.ad-modal-close { background: none; border: none; color: var(--text-mute); font-size: 17px; cursor: pointer; transition: color .15s; }
.ad-modal-close:hover { color: var(--text); }
.ad-modal-body { padding: 22px 24px; display: flex; flex-direction: column; gap: 14px; }
.ad-modal-footer { padding: 14px 24px; border-top: 1px solid var(--border); display: flex; gap: 8px; justify-content: flex-end; }

/* ── FORM ── */
.ad-form-row { display: grid; grid-template-columns: 1fr 1fr; gap: 14px; }
.ad-form-field { display: flex; flex-direction: column; gap: 5px; }
.ad-form-field label { font-size: 11.5px; color: var(--text-sub); font-weight: 600; letter-spacing: .03em; }
.ad-form-field input,
.ad-form-field select {
  padding: 9px 12px;
  background: #f9fafb; border: 1.5px solid var(--border);
  border-radius: 8px; color: var(--text);
  font-family: inherit; font-size: 13px;
  transition: border-color .15s, box-shadow .15s;
}
.ad-form-field input:focus,
.ad-form-field select:focus {
  outline: none; border-color: var(--blue);
  box-shadow: 0 0 0 3px rgba(37,99,235,.1);
  background: #fff;
}
.ad-form-field select { cursor: pointer; }
.ad-form-err {
  font-size: 12px; color: #b91c1c;
  background: #fef2f2; padding: 8px 12px;
  border-radius: 8px; border: 1px solid #fecaca;
}

/* ── ACCOUNT ── */
.ad-account-grid { display: grid; grid-template-columns: 1fr 1fr; gap: 16px; }
.ad-pw-fields { display: flex; flex-direction: column; gap: 9px; margin: 14px 0; }
.ad-pw-fields input {
  padding: 9px 12px; background: #f9fafb;
  border: 1.5px solid var(--border); border-radius: 8px;
  color: var(--text); font-family: inherit; font-size: 13px;
  transition: border-color .15s, box-shadow .15s;
}
.ad-pw-fields input:focus {
  outline: none; border-color: var(--blue);
  box-shadow: 0 0 0 3px rgba(37,99,235,.1);
  background: #fff;
}
.ad-form-status { font-size: 12px; padding: 8px 12px; border-radius: 8px; margin-bottom: 12px; font-weight: 500; }
.ad-form-status.ok { color: #065f46; background: #ecfdf5; border: 1px solid #a7f3d0; }
.ad-form-status.err { color: #b91c1c; background: #fef2f2; border: 1px solid #fecaca; }
`