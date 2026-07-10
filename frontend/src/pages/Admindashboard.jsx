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
import { ManagerImportTab } from './manager/ManagerImportTab'
import { ManagerSalaryRuleTab } from './manager/ManagerSalaryRuleTab'
import { AdminSupplierTab } from './admin/AdminSupplierTab';
import { AdminSalaryTab } from './admin/AdminSalaryTab';
import { InventoryTab } from './shared/InventoryTab';
import { ManagerExportTab } from './manager/ManagerExportTab';
import { FrontStockTab } from './shared/FrontStockTab';
import { ShiftClosingManagementTab } from './shared/ShiftClosingManagementTab';

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

function SortIcon({ active, direction }) {
  if (!active) return <span className="sd-sort-icon sd-sort-none">↕</span>
  return <span className="sd-sort-icon">{direction === 'asc' ? '↑' : '↓'}</span>
}

const EMPTY_FORM = {
  email: '', fullName: '', phoneNumber: '', bankName: '', bankAccountNumber: '', bankAccountName: '', password: '', branchId: '', branchName: '', roleId: '', roleName: '', hireDate: '',
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
  const canViewUsers = isAdmin || isManager
  const canManageUsers = isAdmin

  const [activeTab, setActiveTab] = useState(isAdmin ? 'overview' : 'periods')
  const [localUsers, setLocalUsers] = useState([])
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
  const users = localUsers.length > 0 ? localUsers : initUsers
  const visibleUsers = isManager && !isAdmin
    ? users.filter((u) => String(u.branchId || '') === String(user.branchId || ''))
    : users
  const getPhone = (u) => u?.phoneNumber || u?.phone || ''

  // LOGIC TÌM KIẾM NHÂN VIÊN
  const displayed = visibleUsers
    .filter((u) => {
      const matchSearch = [u.fullName, u.email, u.username, getPhone(u), u.bankName, u.bankAccountNumber, u.bankAccountName, u.branchName].some((v) => v?.toLowerCase().includes(search.toLowerCase()))
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

  function openAdd() { setForm(EMPTY_FORM); setFormErr(''); setModal('add') }
  function openEdit(u) { setForm({ ...u, password: '' }); setFormErr(''); setModalUser(u); setModal('edit') }
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
    if (!form.email && !form.phoneNumber && !form.phone) return setFormErr('Vui lòng nhập email hoặc số điện thoại')
    if (!form.fullName || !form.password) return setFormErr('Vui lòng điền đầy đủ họ tên và password')
    setSaving(true); setFormErr('')
    try {
      const res = await axios.post('/api/User', form)
      setLocalUsers((prev) => [...(prev.length > 0 ? prev : users), res.data]); closeModal()
    } catch (err) { setFormErr(err.message || 'Không thể thêm nhân viên') } finally { setSaving(false) }
  }

  async function handleSaveEdit() {
    if (!form.email && !form.phoneNumber && !form.phone) return setFormErr('Vui lòng nhập email hoặc số điện thoại')
    if (!form.fullName) return setFormErr('Họ tên không được để trống')
    setSaving(true); setFormErr('')
    try {
      await updateUser(form.id, form)
      const publicForm = { ...form }
      delete publicForm.password
      setLocalUsers((prev) => (prev.length > 0 ? prev : users).map((u) => (u.id === form.id ? { ...u, ...publicForm } : u)))
      if (selectedUser && selectedUser.id === form.id) setSelectedUser({ ...selectedUser, ...publicForm })
      if (form.id === user.id) onUserUpdated({ ...user, ...publicForm })
      closeModal()
    } catch (err) { setFormErr(err.message || 'Không thể cập nhật') } finally { setSaving(false) }
  }

  async function handleDelete() {
    setSaving(true)
    try {
      await axios.delete(`/api/User/${modalUser.id}`)
      setLocalUsers((prev) => (prev.length > 0 ? prev : users).filter((u) => u.id !== modalUser.id))
      if (selectedUser && selectedUser.id === modalUser.id) setSelectedUser(null)
      closeModal()
    } catch (err) { setFormErr(err.message || 'Không thể xóa') } finally { setSaving(false) }
  }

  const countByRole = (r) => visibleUsers.filter((u) => u.roleName?.toUpperCase() === r).length

  const getHeaderInfo = () => {
    switch (activeTab) {
      case 'overview': return { eyebrow: 'Hệ thống', title: 'Tổng quan' }
      case 'users': return { eyebrow: 'Quản lý', title: selectedUser ? 'Hồ sơ nhân viên' : 'Nhân sự' }
      case 'account': return { eyebrow: 'Cài đặt', title: 'Tài khoản' }
      case 'branches': return { eyebrow: 'Hệ thống', title: 'Quản lý Cơ sở' }
      case 'periods': return { eyebrow: 'Lịch trình', title: 'Đợt đăng ký ca' }
      case 'scanQr': return { eyebrow: 'Chấm công', title: 'Quét QR nhân viên' }
      case 'salaryRules': return { eyebrow: 'Lương', title: 'Thưởng phạt nhân viên' }
      case 'systemSchedule': return { eyebrow: 'Giám sát', title: 'Lịch làm các cơ sở' }
      case 'inventory': return { eyebrow: "Kho hàng", title: 'Nhập kho hàng hóa' }
      case 'inventoryReport': return isAdmin
        ? { eyebrow: 'Báo cáo kho', title: 'Tồn kho toàn hệ thống' }
        : { eyebrow: 'Báo cáo kho', title: 'Tồn kho cơ sở' }
      case 'suppliers': return { eyebrow: 'Quản trị', title: 'Danh mục Nhà cung cấp' }
      case 'salaries': return isAdmin
        ? { eyebrow: 'Tài chính', title: 'Tổng lương theo cơ sở' }
        : { eyebrow: 'Tài chính', title: 'Trả lương nhân viên' }

      case 'frontStock':
        return isAdmin
          ? { eyebrow: 'Tồn quầy', title: 'Tồn quầy toàn hệ thống' }
          : { eyebrow: 'Tồn quầy', title: 'Tồn quầy cơ sở' };

          case 'shiftClosingReports':
  return isAdmin
    ? { eyebrow: 'Báo cáo kết ca', title: 'Báo cáo kết ca toàn hệ thống' }
    : { eyebrow: 'Báo cáo kết ca', title: 'Báo cáo kết ca cơ sở' };
      default: return { eyebrow: '', title: '' }
    }
  }

  const headerInfo = getHeaderInfo()

  const NAV_ITEMS = []
  if (isAdmin) {
    NAV_ITEMS.push({ id: 'overview', icon: '📊', label: 'Tổng quan' })
    NAV_ITEMS.push({ id: 'users', icon: '👥', label: 'Nhân viên' })
    NAV_ITEMS.push({ id: 'branches', icon: '🏢', label: 'Cơ sở' })
    NAV_ITEMS.push({ id: 'systemSchedule', icon: '🗓️', label: 'Lịch các cơ sở' })
    NAV_ITEMS.push({ id: 'salaries', icon: '💵', label: 'Quản lý lương' })
    NAV_ITEMS.push({ id: 'suppliers', icon: '🏭', label: 'Nhà cung cấp' })
    NAV_ITEMS.push({ id: 'inventoryReport', icon: '📦', label: 'Tồn kho toàn cục' })// Admin xem tồn kho toàn hệ thống
    NAV_ITEMS.push({ id: 'frontStock', icon: '🛒', label: 'Tồn quầy toàn cục' })
    NAV_ITEMS.push({ id: 'shiftClosingReports', icon: '📋', label: 'Báo cáo kết ca toàn cục' });
  }
  if (isManager) {
    NAV_ITEMS.push({ id: 'periods', icon: '📅', label: 'Đợt đăng ký' })
    NAV_ITEMS.push({ id: 'users', icon: '👥', label: 'Nhân viên' })
    NAV_ITEMS.push({ id: 'scanQr', icon: '📷', label: 'Quét QR' })
    NAV_ITEMS.push({ id: 'salaryRules', icon: '⚖', label: 'Thưởng phạt' })
    NAV_ITEMS.push({ id: 'salaries', icon: '💵', label: 'Trả lương' })
    NAV_ITEMS.push({ id: 'inventory', icon: '📥', label: 'Nhập kho hàng' })
    NAV_ITEMS.push({ id: 'inventoryReport', icon: '📦', label: 'Tồn kho cơ sở' })// Manager xem tồn kho cơ sở của mình
    NAV_ITEMS.push({ id: 'exportStock', icon: '📤', label: 'Xuất hàng ra quầy' })
    NAV_ITEMS.push({ id: 'frontStock', icon: '🛒', label: 'Tồn quầy cơ sở' })
    NAV_ITEMS.push({ id: 'shiftClosingReports', icon: '📋', label: 'Báo cáo kết ca' });
  }
  NAV_ITEMS.push({ id: 'account', icon: '👤', label: 'Tài khoản' })

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
                  <div className="sd-stat-card"><span className="sd-stat-icon">●</span><h3>{users.length}</h3><p>Tổng nhân viên</p></div>
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

            {activeTab === 'exportStock' && isManager && (
              <ManagerExportTab user={user} branches={branches} />
            )}

            {/* QUẢN LÝ NHÂN VIÊN */}
            {activeTab === 'users' && canViewUsers && (
              <>
                {!selectedUser ? (
                  <div className="sd-users-page">
                    <div className="sd-users-toolbar">
                      <div className="sd-users-toolbar-left">
                        <div className="sd-search-wrap">
                          <span className="sd-search-icon">⌕</span>
                          <input className="sd-input-search" placeholder="Tìm tên, email, SĐT, chi nhánh..." value={search} onChange={(e) => setSearch(e.target.value)} />
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
                        {canManageUsers && <button className="sd-btn-add" onClick={openAdd}><span>＋</span> Thêm nhân viên</button>}
                      </div>
                    </div>
                    <div className="sd-table-wrap">
                      <table className="sd-table">
                        <thead>
                          <tr>
                            <th className="sd-th sd-th-avatar" style={{ width: 48 }}></th>
                            <th className="sd-th sd-th-sortable sd-td-name-col" onClick={() => toggleSort('fullName')}>Họ và tên <SortIcon active={sortCol === 'fullName'} direction={sortDir} /></th>
                            <th className="sd-th sd-th-sortable sd-hide-mobile" onClick={() => toggleSort('email')}>Email <SortIcon active={sortCol === 'email'} direction={sortDir} /></th>
                            <th className="sd-th sd-th-sortable sd-hide-mobile" onClick={() => toggleSort('phoneNumber')}>SĐT <SortIcon active={sortCol === 'phoneNumber'} direction={sortDir} /></th>
                            <th className="sd-th sd-th-sortable sd-hide-mobile" onClick={() => toggleSort('bankName')}>Ngân hàng <SortIcon active={sortCol === 'bankName'} direction={sortDir} /></th>
                            <th className="sd-th sd-th-sortable sd-hide-mobile" onClick={() => toggleSort('roleName')}>Chức vụ <SortIcon active={sortCol === 'roleName'} direction={sortDir} /></th>
                            <th className="sd-th sd-th-sortable sd-td-info-col" onClick={() => toggleSort('branchName')}>Chi nhánh <SortIcon active={sortCol === 'branchName'} direction={sortDir} /></th>
                            <th className="sd-th sd-th-sortable sd-hide-mobile" onClick={() => toggleSort('hireDate')}>Ngày vào làm <SortIcon active={sortCol === 'hireDate'} direction={sortDir} /></th>
                          </tr>
                        </thead>
                        <tbody>
                          {displayed.length === 0 && (
                            <tr><td colSpan={8} className="sd-td-empty"><div className="sd-empty-state"><span className="sd-empty-icon">●</span><p>Không tìm thấy nhân sự</p></div></td></tr>
                          )}
                          {displayed.map((u, idx) => {
                            const roleColor = ROLE_COLORS[u.roleName?.toUpperCase()] || { bg: '#f1f5f9', color: '#475569' }
                            return (
                              <tr key={u.id} className="sd-tr" style={{ animationDelay: `${idx * 30}ms`, cursor: 'pointer' }} onClick={() => setSelectedUser(u)}>
                                <td className="sd-td sd-td-avatar sd-hide-mobile"><div className="sd-info-avatar sd-avatar-sm">{getInitials(u.fullName || u.username)}</div></td>
                                <td className="sd-td sd-td-name-col"><span className="sd-td-name">{u.fullName || '—'}</span></td>
                                <td className="sd-td sd-hide-mobile"><span className="sd-td-username">{u.email || '—'}</span></td>
                                <td className="sd-td sd-hide-mobile"><span className="sd-td-phone">{getPhone(u) || '—'}</span></td>
                                <td className="sd-td sd-hide-mobile"><span>{u.bankName || u.bankAccountNumber || '—'}</span></td>
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
                          <InfoRow label="Email" value={selectedUser.email || 'Chưa có'} />
                          <InfoRow label="SĐT" value={getPhone(selectedUser) || 'Chưa có'} />
                          <InfoRow label="Ngân hàng" value={selectedUser.bankName || 'Chưa có'} />
                          <InfoRow label="Số tài khoản" value={selectedUser.bankAccountNumber || 'Chưa có'} />
                          <InfoRow label="Tên tài khoản" value={selectedUser.bankAccountName || 'Chưa có'} />
                          <InfoRow label="Chi nhánh" value={selectedUser.branchName || 'Chưa gán'} />
                          <InfoRow label="Ngày vào làm" value={formatDate(selectedUser.hireDate)} />
                        </dl>
                        <div className="sd-detail-actions">
                          {canManageUsers && <button className="sd-btn-ghost btn-edit" onClick={() => openEdit(selectedUser)}>✎ Chỉnh sửa</button>}
                          {canManageUsers && selectedUser.id !== user.id && <button className="sd-btn-ghost btn-delete" onClick={() => openDelete(selectedUser)}>✕ Xóa nhân sự</button>}
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
            {activeTab === 'salaryRules' && isManager && (
              <ManagerSalaryRuleTab user={user} isAdmin={isAdmin} branches={branches} />
            )}
            {activeTab === 'inventory' && isManager && <ManagerImportTab user={user} branches={branches} />}
            {activeTab === 'systemSchedule' && isAdmin && <AdminSystemScheduleTab branches={branches} />}
            {activeTab === 'salaries' && (isAdmin || isManager) && <AdminSalaryTab isAdmin={isAdmin} />}
            {activeTab === 'suppliers' && isAdmin && <AdminSupplierTab />}
            {activeTab === 'inventoryReport' && <InventoryTab currentUser={user} branches={branches} />}
            {activeTab === 'frontStock' && (isAdmin || isManager) && (
              <FrontStockTab currentUser={user} branches={branches} />
            )}
            {activeTab === 'shiftClosingReports' && (isAdmin || isManager) && (
  <ShiftClosingManagementTab currentUser={user} branches={branches} />
)}

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
                    <InfoRow label="Email" value={user.email || 'Chưa có'} />
                    <InfoRow label="SĐT" value={getPhone(user) || 'Chưa có'} />
                    <InfoRow label="Ngân hàng" value={user.bankName || 'Chưa có'} />
                    <InfoRow label="Số tài khoản" value={user.bankAccountNumber || 'Chưa có'} />
                    <InfoRow label="Tên tài khoản" value={user.bankAccountName || 'Chưa có'} />
                    <InfoRow label="Chi nhánh" value={branch?.name || user.branchName || 'Chưa có'} />
                    <InfoRow label="Ngày vào làm" value={formatDate(user.hireDate)} />
                  </dl>
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
                <div className="sd-field"><label>Email</label><input name="email" value={form.email || ''} onChange={handleFormChange} /></div>
                <div className="sd-field"><label>SĐT</label><input name="phoneNumber" value={form.phoneNumber || form.phone || ''} onChange={handleFormChange} /></div>
                <div className="sd-field"><label>Ngân hàng</label><input name="bankName" value={form.bankName || ''} onChange={handleFormChange} /></div>
                <div className="sd-field"><label>Số tài khoản</label><input name="bankAccountNumber" value={form.bankAccountNumber || ''} onChange={handleFormChange} /></div>
                <div className="sd-field"><label>Tên tài khoản</label><input name="bankAccountName" value={form.bankAccountName || ''} onChange={handleFormChange} /></div>
                <div className="sd-field"><label>{modal === 'add' ? 'Password *' : 'Password mới'}</label><input type="password" name="password" value={form.password || ''} onChange={handleFormChange} placeholder={modal === 'add' ? '••••••' : 'Để trống nếu giữ nguyên'} /></div>
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
              <button className="sd-btn-ghost" onClick={closeModal}>Hủy</button>
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
              <button className="sd-btn-ghost" onClick={closeModal}>Hủy</button>
              <button className="sd-btn-primary btn-danger" disabled={saving} onClick={handleDelete}>{saving ? 'Đang xoá...' : 'Xoá ngay'}</button>
            </div>
          </div>
        </div>
      )}
    </div>
  )
}



