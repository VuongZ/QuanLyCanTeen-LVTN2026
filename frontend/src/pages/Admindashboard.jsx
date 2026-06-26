import { useState, useEffect } from 'react'
import axios from 'axios'
import { updateUser } from '../api/UserApi'
import { getAllBranches } from '../api/BranchApi'
import './css/admindashboard.css'

// 1. IMPORT CÁC COMPONENT ĐÃ ĐƯỢC CHIA TÁCH
import { PasswordForm } from './shared/PasswordForm'
import { AdminBranchTab } from './admin/AdminBranchTab'
import { AdminSystemScheduleTab } from './admin/AdminSystemScheduleTab'
import { ManagerPeriodTab } from './manager/ManagerPeriodTab'
import { ManagerQrAttendanceTab } from './manager/ManagerQrAttendanceTab'
import {ManagerImportTab} from './manager/ManagerImportTab'
import { AdminSupplierTab } from './admin/AdminSupplierTab';
import { InventoryTab } from './shared/InventoryTab';

// --- CÁC HÀM TIỆN ÍCH DÙNG CHUNG TRONG LAYOUT ---
function getInitials(name = '') {
  return name.split(' ').filter(Boolean).slice(-2).map((p) => p[0]).join('').toUpperCase()
}

function formatDate(value) {
  if (!value) return '—'
  return new Intl.DateTimeFormat('vi-VN').format(new Date(value))
}

function normalizeText(value = '') {
  return value.normalize('NFD').replace(/[\u0300-\u036f]/g, '').toUpperCase()
}

function InfoRow({ label, value }) {
  return <div className="sd-info-row"><dt>{label}</dt><dd>{value}</dd></div>
}

const EMPTY_FORM = {
  username: '', fullName: '', password: '', branchId: '', branchName: '', roleId: '', roleName: '', hireDate: '',
}

const ROLE_COLORS = {
  ADMIN: { bg: '#fef3c7', color: '#92400e' },
  MANAGER: { bg: '#dbeafe', color: '#1e40af' },
  STAFF: { bg: '#dcfce7', color: '#166534' },
}

export function AdminDashboard({ onLogout, onUserUpdated, roles, user, users: initUsers }) {
const rawRoleName = normalizeText(user.roleName || user.role || '')
  const isAdmin = rawRoleName.includes('ADMIN') || rawRoleName.includes('QUAN TRI')
  const isManager = rawRoleName.includes('MANAGER') || rawRoleName.includes('QUAN LY')

  const [activeTab, setActiveTab] = useState(isAdmin ? 'overview' : 'periods')
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
    getAllBranches().then((data) => setBranches(Array.isArray(data) ? data : [])).catch(() => setBranches([]))
  }, [])

  const branch = branches.find((b) => b.id === user.branchId)

  // LOGIC TÌM KIẾM NHÂN VIÊN
  const displayed = users
    .filter((u) => {
      const matchSearch = [u.fullName, u.username, u.branchName].some((v) => v?.toLowerCase().includes(search.toLowerCase()))
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
      if (name === 'branchId') { const b = branches.find((b) => String(b.id) === value); next.branchName = b?.name || b?.branchName || '' }
      if (name === 'roleId') { const r = roles.find((r) => String(r.id) === value); next.roleName = r?.roleName || '' }
      return next
    })
  }

  async function handleSaveAdd() {
    if (!form.username || !form.fullName || !form.password) return setFormErr('Vui lòng điền đầy đủ username, họ tên, password')
    setSaving(true); setFormErr('')
    try {
      const res = await axios.post('/api/User', form)
      setUsers((prev) => [...prev, res.data]); closeModal()
    } catch (err) { setFormErr(err.message || 'Không thể thêm nhân viên') } finally { setSaving(false) }
  }

  async function handleSaveEdit() {
    if (!form.username || !form.fullName) return setFormErr('Username và họ tên không được để trống')
    setSaving(true); setFormErr('')
    try {
      await updateUser(form.id, form)
      setUsers((prev) => prev.map((u) => (u.id === form.id ? { ...u, ...form } : u)))
      if (selectedUser && selectedUser.id === form.id) setSelectedUser({ ...selectedUser, ...form })
      if (form.id === user.id) onUserUpdated({ ...user, ...form })
      closeModal()
    } catch (err) { setFormErr(err.message || 'Không thể cập nhật') } finally { setSaving(false) }
  }

  async function handleDelete() {
    setSaving(true)
    try {
      await axios.delete(`/api/User/${modalUser.id}`)
      setUsers((prev) => prev.filter((u) => u.id !== modalUser.id))
      if (selectedUser && selectedUser.id === modalUser.id) setSelectedUser(null)
      closeModal()
    } catch (err) { setFormErr(err.message || 'Không thể xóa') } finally { setSaving(false) }
  }

  const countByRole = (r) => users.filter((u) => u.roleName?.toUpperCase() === r).length

  const getHeaderInfo = () => {
    switch (activeTab) {
      case 'overview': return { eyebrow: 'Hệ thống', title: 'Tổng quan' }
      case 'users': return { eyebrow: 'Quản lý', title: selectedUser ? 'Hồ sơ nhân viên' : 'Nhân sự' }
      case 'account': return { eyebrow: 'Cài đặt', title: 'Tài khoản' }
      case 'branches': return { eyebrow: 'Hệ thống', title: 'Quản lý Cơ sở' }
      case 'periods': return { eyebrow: 'Lịch trình', title: 'Đợt đăng ký ca' }
      case 'scanQr': return { eyebrow: 'Chấm công', title: 'Quét QR nhân viên' }
      case 'systemSchedule': return { eyebrow: 'Giám sát', title: 'Lịch làm các cơ sở' }
      case 'inventory':return{eyebrow:"Kho hàng", title:'Nhập kho hàng hóa '}
      case 'suppliers': return { eyebrow: 'Quản trị', title: 'Danh mục Nhà cung cấp' }
      default: return { eyebrow: '', title: '' }
    }
  }

  const headerInfo = getHeaderInfo()

 const NAV_ITEMS = []
if (isAdmin) {
  NAV_ITEMS.push({ id: 'overview', icon: '⬡', label: 'Tổng quan' })
  NAV_ITEMS.push({ id: 'users', icon: '◈', label: 'Nhân viên' })
  NAV_ITEMS.push({ id: 'branches', icon: '🏢', label: 'Cơ sở' })
  NAV_ITEMS.push({ id: 'systemSchedule', icon: '🗓️', label: 'Lịch các cơ sở' })
  NAV_ITEMS.push({ id: 'suppliers', icon: '📇', label: 'Nhà cung cấp' })
  NAV_ITEMS.push({ id: 'inventoryReport', icon: '📦', label: 'Tồn kho toàn cục' }) // 👈 Admin xem tồn kho toàn hệ thống
}
if (isManager) {
  NAV_ITEMS.push({ id: 'periods', icon: '📅', label: 'Đợt đăng ký' })
  NAV_ITEMS.push({ id: 'scanQr', icon: 'QR', label: 'Quét QR' })
  NAV_ITEMS.push({ id: 'inventory', icon: '[]', label: 'Nhập kho hàng' })
  NAV_ITEMS.push({ id: 'inventoryReport', icon: '📦', label: 'Tồn kho cơ sở' }) // 👈 Manager xem tồn kho cơ sở của mình
}
NAV_ITEMS.push({ id: 'account', icon: '◎', label: 'Tài khoản' })

  return (
    <div className="sd-root sd-root--left-nav">
      {/* ── HEADER ── */}
      <header className="sd-topbar">
        <div className="sd-brand">
          <button className="sd-hamburger" onClick={() => setIsMenuOpen(true)}>☰</button>
          <span className="sd-brand-icon">CT</span>
          <span className="sd-brand-name">Canteen Admin</span>
        </div>
        <div className="sd-flex-center">
          <div className="sd-branch-badge" style={{ marginRight: 12 }}>{user.roleName}</div>
          <button className="sd-logout-btn" onClick={onLogout}><span>Đăng xuất</span> ↩</button>
        </div>
      </header>

      <div className="sd-layout">
        {isMenuOpen && <div className="sd-menu-overlay" onClick={() => setIsMenuOpen(false)}></div>}

        {/* ── SIDEBAR MENU ── */}
        <nav className={`sd-left-nav ${isMenuOpen ? 'open' : ''}`}>
          <div className="sd-left-nav-user">
            <div className="sd-info-avatar sd-avatar-sm">{getInitials(user.fullName || user.username)}</div>
            <span className="sd-left-nav-name">{user.fullName || user.username}</span>
          </div>
          <div className="sd-left-nav-items">
            {NAV_ITEMS.map((item) => (
              <button
                key={item.id}
                className={`sd-left-nav-item ${activeTab === item.id ? 'active' : ''}`}
                onClick={() => { setActiveTab(item.id); setSelectedUser(null); setIsMenuOpen(false) }}
                type="button"
              >
                <span className="sd-nav-icon">{item.icon}</span>
                <span className="sd-nav-label">{item.label}</span>
              </button>
            ))}
          </div>
          <button className="sd-left-nav-logout" onClick={onLogout}>↩ Đăng xuất</button>
        </nav>

        {/* ── MAIN CONTENT ── */}
        <main className="sd-main">
          <div className="sd-page-header">
            <div>
              <p className="sd-eyebrow">{headerInfo.eyebrow}</p>
              <h1>{headerInfo.title}</h1>
            </div>
          </div>

          <div className="sd-content">
            {/* THỐNG KÊ (Giữ lại trong file chính vì liên kết với user data trực tiếp) */}
            {activeTab === 'overview' && isAdmin && (
              <div className="sd-profile-layout">
                <div className="sd-stat-grid">
                  <div className="sd-stat-card"><span className="sd-stat-icon">◈</span><h3>{users.length}</h3><p>Tổng nhân viên</p></div>
                  <div className="sd-stat-card"><span className="sd-stat-icon">⊞</span><h3>{branches.length}</h3><p>Chi nhánh</p></div>
                </div>
                <div className="sd-card">
                  <div className="sd-card-header"><p className="sd-eyebrow">Thống kê</p><h2>Phân bổ chức vụ</h2></div>
                  {roles.filter((r) => r.roleName !== 'ADMIN').map((r) => {
                    const cnt = countByRole(r.roleName)
                    const pct = users.length ? Math.round((cnt / users.length) * 100) : 0
                    return (
                      <div key={r.id} className="sd-role-bar">
                        <div className="sd-role-bar-head"><strong>{r.roleName}</strong><span>{cnt} người · {pct}%</span></div>
                        <div className="sd-bar-track"><div className="sd-bar-fill" style={{ width: `${pct}%` }} /></div>
                      </div>
                    )
                  })}
                </div>
              </div>
            )}

            {/* QUẢN LÝ NHÂN VIÊN */}
            {activeTab === 'users' && isAdmin && (
              <>
                {!selectedUser ? (
                  <div className="sd-users-page">
                    <div className="sd-users-toolbar">
                      <div className="sd-users-toolbar-left">
                        <div className="sd-search-wrap">
                          <span className="sd-search-icon">⌕</span>
                          <input className="sd-input-search" placeholder="Tìm tên, username, chi nhánh..." value={search} onChange={(e) => setSearch(e.target.value)} />
                          {search && <button className="sd-search-clear" onClick={() => setSearch('')}>✕</button>}
                        </div>
                        <div className="sd-filter-chips">
                          {['ALL', 'ADMIN', 'MANAGER', 'STAFF'].map((r) => (
                            <button key={r} className={`sd-filter-chip ${filterRole === r ? 'active' : ''}`} onClick={() => setFilterRole(r)}>
                              {r === 'ALL' ? 'Tất cả' : r}
                              {r !== 'ALL' && <span className="sd-chip-count">{countByRole(r)}</span>}
                            </button>
                          ))}
                        </div>
                      </div>
                      <div className="sd-users-toolbar-right">
                        <span className="sd-result-count">{displayed.length} nhân viên</span>
                        <button className="sd-btn-add" onClick={openAdd}><span>＋</span> Thêm nhân viên</button>
                      </div>
                    </div>
                    <div className="sd-table-wrap">
                      <table className="sd-table">
                        <thead>
                          <tr>
                            <th className="sd-th sd-th-avatar" style={{ width: 48 }}></th>
                            <th className="sd-th sd-th-sortable sd-td-name-col" onClick={() => toggleSort('fullName')}>Họ và tên <SortIcon col="fullName" /></th>
                            <th className="sd-th sd-th-sortable sd-hide-mobile" onClick={() => toggleSort('username')}>Username <SortIcon col="username" /></th>
                            <th className="sd-th sd-th-sortable sd-hide-mobile" onClick={() => toggleSort('roleName')}>Chức vụ <SortIcon col="roleName" /></th>
                            <th className="sd-th sd-th-sortable sd-td-info-col" onClick={() => toggleSort('branchName')}>Chi nhánh <SortIcon col="branchName" /></th>
                            <th className="sd-th sd-th-sortable sd-hide-mobile" onClick={() => toggleSort('hireDate')}>Ngày vào làm <SortIcon col="hireDate" /></th>
                          </tr>
                        </thead>
                        <tbody>
                          {displayed.length === 0 && (
                            <tr><td colSpan={6} className="sd-td-empty"><div className="sd-empty-state"><span className="sd-empty-icon">◈</span><p>Không tìm thấy nhân sự</p></div></td></tr>
                          )}
                          {displayed.map((u, idx) => {
                            const roleColor = ROLE_COLORS[u.roleName?.toUpperCase()] || { bg: '#f1f5f9', color: '#475569' }
                            return (
                              <tr key={u.id} className="sd-tr" style={{ animationDelay: `${idx * 30}ms`, cursor: 'pointer' }} onClick={() => setSelectedUser(u)}>
                                <td className="sd-td sd-td-avatar sd-hide-mobile"><div className="sd-info-avatar sd-avatar-sm">{getInitials(u.fullName || u.username)}</div></td>
                                <td className="sd-td sd-td-name-col"><span className="sd-td-name">{u.fullName || '—'}</span></td>
                                <td className="sd-td sd-hide-mobile"><span className="sd-td-username">@{u.username}</span></td>
                                <td className="sd-td sd-hide-mobile"><span className="sd-role-pill" style={{ background: roleColor.bg, color: roleColor.color }}>{u.roleName || '—'}</span></td>
                                <td className="sd-td sd-td-info-col"><span className="sd-td-branch">{u.branchName || <em className="sd-muted">Chưa gán</em>}</span></td>
                                <td className="sd-td sd-hide-mobile"><span className="sd-td-date">{formatDate(u.hireDate)}</span></td>
                              </tr>
                            )
                          })}
                        </tbody>
                      </table>
                    </div>
                  </div>
                ) : (
                  <div className="sd-user-detail-page">
                    <button className="sd-btn-back" onClick={() => setSelectedUser(null)}>← Quay lại danh sách</button>
                    <div className="sd-profile-layout">
                      <div className="sd-card">
                        <div className="sd-info-hero">
                          <div className="sd-info-avatar">{getInitials(selectedUser.fullName || selectedUser.username)}</div>
                          <div>
                            <h3>{selectedUser.fullName || selectedUser.username}</h3>
                            <span className="sd-role-badge" style={{ background: ROLE_COLORS[selectedUser.roleName?.toUpperCase()]?.bg || '#ea580c', color: ROLE_COLORS[selectedUser.roleName?.toUpperCase()]?.color || '#fff' }}>
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
                          <button className="sd-btn-ghost btn-edit" onClick={() => openEdit(selectedUser)}>✎ Chỉnh sửa</button>
                          {selectedUser.id !== user.id && <button className="sd-btn-ghost btn-delete" onClick={() => openDelete(selectedUser)}>✕ Xóa nhân sự</button>}
                        </div>
                      </div>
                    </div>
                  </div>
                )}
              </>
            )}

            {/* 👉 IMPORT CÁC COMPONENT CON VÀO ĐÂY */}
            {activeTab === 'branches' && isAdmin && <AdminBranchTab branches={branches} setBranches={setBranches} />}
            {activeTab === 'periods' && isManager && <ManagerPeriodTab user={user} isManager={isManager} branches={branches} />}
            {activeTab === 'scanQr' && isManager && <ManagerQrAttendanceTab user={user} />}
            {activeTab === 'inventory' && isManager && <ManagerImportTab user={user} branches={branches}/>}
            {activeTab === 'systemSchedule' && isAdmin && <AdminSystemScheduleTab branches={branches} />}
            {activeTab === 'suppliers' && isAdmin && <AdminSupplierTab />}
            {activeTab === 'inventoryReport' && <InventoryTab currentUser={user} branches={branches} />}

            {/* TÀI KHOẢN VÀ BẢO MẬT */}
            {activeTab === 'account' && (
              <div className="sd-profile-layout">
                <div className="sd-card">
                  <div className="sd-card-header"><p className="sd-eyebrow">Chi tiết</p><h2>Hồ sơ cá nhân</h2></div>
                  <div className="sd-info-hero">
                    <div className="sd-info-avatar">{getInitials(user.fullName || user.username)}</div>
                    <div><h3>{user.fullName || user.username}</h3><span className="sd-role-badge">{user.roleName}</span></div>
                  </div>
                  <dl className="sd-dl">
                    <InfoRow label="Tên đăng nhập" value={user.username} />
                    <InfoRow label="Chi nhánh" value={branch?.name || user.branchName || 'Chưa có'} />
                    <InfoRow label="Ngày vào làm" value={formatDate(user.hireDate)} />
                  </dl>
                </div>
                <div className="sd-card">
                  <div className="sd-card-header"><p className="sd-eyebrow">Bảo mật</p><h2>Đổi mật khẩu</h2></div>
                  <PasswordForm onUserUpdated={onUserUpdated} user={user} />
                </div>
              </div>
            )}
          </div>
        </main>
      </div>

      {/* MODAL CỦA PHẦN QUẢN LÝ NHÂN VIÊN */}
      {(modal === 'add' || modal === 'edit') && (
        <div className="sd-overlay" onClick={closeModal}>
          <div className="sd-modal" onClick={(e) => e.stopPropagation()}>
            <div className="sd-modal-header"><h2>{modal === 'add' ? 'Thêm nhân viên' : 'Chỉnh sửa'}</h2><button onClick={closeModal}>✕</button></div>
            <div className="sd-modal-body">
              <div className="sd-modal-grid">
                <div className="sd-field"><label>Họ và tên *</label><input name="fullName" value={form.fullName} onChange={handleFormChange} /></div>
                <div className="sd-field"><label>Username *</label><input name="username" value={form.username} onChange={handleFormChange} /></div>
                <div className="sd-field"><label>Password *</label><input type="password" name="password" value={form.password} onChange={handleFormChange} placeholder="••••••" /></div>
                <div className="sd-field"><label>Ngày vào làm</label><input type="date" name="hireDate" value={form.hireDate?.slice(0, 10) || ''} onChange={handleFormChange} /></div>
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
                    {branches.map((b) => <option key={b.id} value={b.id}>{b.name}</option>)}
                  </select>
                </div>
              </div>
              {formErr && <p className="sd-status sd-status-error">{formErr}</p>}
            </div>
            <div className="sd-modal-footer">
              <button className="sd-btn-ghost" onClick={closeModal}>Huỷ</button>
              <button className="sd-btn-primary" disabled={saving} onClick={modal === 'add' ? handleSaveAdd : handleSaveEdit}>{saving ? 'Đang lưu...' : 'Lưu lại'}</button>
            </div>
          </div>
        </div>
      )}

      {modal === 'delete' && (
        <div className="sd-overlay" onClick={closeModal}>
          <div className="sd-modal" onClick={(e) => e.stopPropagation()}>
            <div className="sd-modal-header"><h2>Xác nhận xoá</h2><button onClick={closeModal}>✕</button></div>
            <div className="sd-modal-body">
              <p>Bạn có chắc muốn xoá nhân viên <strong>{modalUser?.fullName}</strong>?</p>
              {formErr && <p className="sd-status sd-status-error">{formErr}</p>}
            </div>
            <div className="sd-modal-footer">
              <button className="sd-btn-ghost" onClick={closeModal}>Huỷ</button>
              <button className="sd-btn-primary btn-danger" disabled={saving} onClick={handleDelete}>{saving ? 'Đang xoá...' : 'Xoá ngay'}</button>
            </div>
          </div>
        </div>
      )}
    </div>
  )
}