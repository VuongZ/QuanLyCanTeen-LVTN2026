import { useState } from 'react'
import { updateUser } from '../api/UserApi'
import './dashboard.css'

// ─── Helpers ────────────────────────────────────────────────────────────────

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
  if (!value) return 'Chưa có'
  return new Intl.DateTimeFormat('vi-VN').format(new Date(value))
}

// ─── Shift Config ────────────────────────────────────────────────────────────

const SHIFTS = [
  { id: 'morning', label: 'Ca Sáng', time: '06:00 – 14:00', icon: '' },
  { id: 'afternoon', label: 'Ca Chiều', time: '14:00 – 22:00', icon: '' },
  { id: 'night', label: 'Ca Đêm', time: '22:00 – 06:00', icon: '' },
]

const DAYS = ['T2', 'T3', 'T4', 'T5', 'T6', 'T7', 'CN']
const DAY_LABELS = ['Thứ 2', 'Thứ 3', 'Thứ 4', 'Thứ 5', 'Thứ 6', 'Thứ 7', 'Chủ nhật']

// ─── Main Component (Giao diện Mobile - 3 Tabs) ──────────────────────────────

export function StaffDashboard({ branches, onLogout, onUserUpdated, roles, user, users }) {
  const [activeTab, setActiveTab] = useState('profile')
  const branch = branches?.find((b) => b.id === user.branchId)

  // Đổi tiêu đề dựa trên Tab đang chọn
  const getHeaderInfo = () => {
    switch(activeTab) {
      case 'profile': return { eyebrow: 'Tài khoản', title: 'Hồ sơ của tôi' };
      case 'shifts': return { eyebrow: 'Lịch làm việc', title: 'Đăng ký ca' };
      case 'security': return { eyebrow: 'Cài đặt', title: 'Bảo mật tài khoản' };
      default: return { eyebrow: '', title: '' };
    }
  }
  const headerInfo = getHeaderInfo();

  return (
    <div className="sd-root">
      {/* Topbar chuẩn Mobile */}
      <header className="sd-topbar">
        <div className="sd-brand">
          <span className="sd-brand-icon">CT</span>
          <span className="sd-brand-name">Canteen</span>
        </div>
        <button className="sd-logout-btn" onClick={onLogout}>
          <span>Đăng xuất</span> ↩
        </button>
      </header>

      {/* Nội dung chính */}
      <main className="sd-main">
        <div className="sd-page-header">
          <div>
            <p className="sd-eyebrow">{headerInfo.eyebrow}</p>
            <h1>{headerInfo.title}</h1>
          </div>
          <div className="sd-branch-badge">
            📍 {branch?.name || user.branchName || 'Chưa gán'}
          </div>
        </div>

        <div className="sd-content">
          {activeTab === 'profile' && (
            <ProfileTab branch={branch} user={user} />
          )}
          {activeTab === 'shifts' && (
            <ShiftsTab user={user} />
          )}
          {activeTab === 'security' && (
            <SecurityTab onUserUpdated={onUserUpdated} user={user} />
          )}
        </div>
      </main>

      {/* Thanh Điều Hướng Dưới Đáy (Thêm Tab Bảo mật) */}
      <nav className="sd-bottom-nav">
        {[
          { id: 'profile', icon: '◎', label: 'Tài khoản' },
          { id: 'shifts', icon: '⊞', label: 'Đăng ký ca' },
          { id: 'security', icon: 'sc', label: 'Bảo mật' }, 
        ].map((item) => (
          <button
            key={item.id}
            className={`sd-nav-item ${activeTab === item.id ? 'active' : ''}`}
            onClick={() => setActiveTab(item.id)}
            type="button"
          >
            <span className="sd-nav-icon">{item.icon}</span>
            <span className="sd-nav-label">{item.label}</span>
          </button>
        ))}
      </nav>
    </div>
  )
}

// ─── Profile Tab (Chỉ còn thông tin cá nhân) ──────────────────────────────────

function ProfileTab({ branch, user }) {
  return (
    <div className="sd-profile-layout">
      {/* Info card */}
      <div className="sd-card">
        <div className="sd-card-header">
          <p className="sd-eyebrow">Chi tiết</p>
          <h2>Hồ sơ nhân viên</h2>
        </div>
        <div className="sd-info-hero">
          <div className="sd-info-avatar">{getInitials(user.fullName || user.username)}</div>
          <div>
            <h3>{user.fullName || user.username}</h3>
            <span className="sd-role-badge">{user.roleName || 'Nhân viên'}</span>
          </div>
        </div>
        <dl className="sd-dl">
          <InfoRow label="Tên đăng nhập" value={user.username} />
          <InfoRow label="Họ và tên" value={user.fullName || '—'} />
          <InfoRow label="Chi nhánh" value={branch?.name || user.branchName || 'Chưa có'} />
          <InfoRow label="Ngày vào làm" value={formatDate(user.hireDate)} />
          <InfoRow label="Email" value={user.email || '—'} />
          <InfoRow label="Số điện thoại" value={user.phone || '—'} />
        </dl>
      </div>
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

// ─── Security Tab (Tab mới dành riêng cho Đổi mật khẩu) ────────────────────────

function SecurityTab({ onUserUpdated, user }) {
  return (
    <div className="sd-profile-layout">
      <div className="sd-card">
        <div className="sd-card-header">
          <p className="sd-eyebrow">Bảo mật</p>
          <h2>Đổi mật khẩu</h2>
        </div>
        <PasswordForm onUserUpdated={onUserUpdated} user={user} />
      </div>
    </div>
  )
}

// ─── Shifts Tab ──────────────────────────────────────────────────────────────

function ShiftsTab({ user }) {
  const [registered, setRegistered] = useState({})
  const [saved, setSaved] = useState(false)
  const [saving, setSaving] = useState(false)

  function toggle(dayIdx, shiftId) {
    setSaved(false)
    setRegistered((prev) => {
      const day = prev[dayIdx] || {}
      return { ...prev, [dayIdx]: { ...day, [shiftId]: !day[shiftId] }  }
    })
  }

  function countSelected() {
    return Object.values(registered).flatMap((d) => Object.values(d)).filter(Boolean).length
  }

  async function handleSave() {
    setSaving(true)
    await new Promise((r) => setTimeout(r, 800))
    setSaving(false)
    setSaved(true)
  }

  function handleReset() {
    setRegistered({})
    setSaved(false)
  }

  return (
    <div className="sd-card sd-shifts-card">
      <div className="sd-card-header">
        <div>
          <p className="sd-eyebrow">Tuần này</p>
          <h2>Chọn ca làm việc</h2>
        </div>
        <span className="sd-count-badge">{countSelected()} ca đã chọn</span>
      </div>

      <div className="sd-shift-legend">
        {SHIFTS.map((s) => (
          <div key={s.id} className="sd-shift-legend-item">
            <span>{s.icon}</span>
            <div>
              <strong>{s.label}</strong>
              <small>{s.time}</small>
            </div>
          </div>
        ))}
      </div>

      <div className="sd-shift-grid">
        {/* Header row */}
        <div className="sd-grid-col sd-grid-header-col">
          <div className="sd-grid-corner" />
          {SHIFTS.map((s) => (
            <div key={s.id} className="sd-grid-shift-label">
              <span>{s.icon}</span>
              <span>{s.label}</span>
            </div>
          ))}
        </div>

        {/* Day columns */}
        {DAYS.map((day, dayIdx) => (
          <div key={day} className="sd-grid-col">
            <div className="sd-grid-day-label">
              <strong>{day}</strong>
              <small>{DAY_LABELS[dayIdx]}</small>
            </div>
            {SHIFTS.map((shift) => {
              const on = registered[dayIdx]?.[shift.id] || false
              return (
                <button
                  key={shift.id}
                  className={`sd-shift-cell ${on ? 'selected' : ''}`}
                  onClick={() => toggle(dayIdx, shift.id)}
                  type="button"
                  aria-label={`${DAY_LABELS[dayIdx]} ${shift.label}`}
                  aria-pressed={on}
                >
                  {on ? '✓' : ''}
                </button>
              )
            })}
          </div>
        ))}
      </div>

      <div className="sd-shift-actions">
        <button className="sd-btn-ghost" onClick={handleReset} type="button">
          Xóa tất cả
        </button>
        <button
          className="sd-btn-primary"
          disabled={saving || countSelected() === 0}
          onClick={handleSave}
          type="button"
        >
          {saving ? 'Đang lưu…' : saved ? '✓ Đã đăng ký' : 'Đăng ký ca làm'}
        </button>
      </div>

      {saved && (
        <p className="sd-save-notice">
          ✅ Đã gửi đăng ký {countSelected()} ca. Quản lý sẽ xác nhận trong vòng 24 giờ.
        </p>
      )}
    </div>
  )
}

// ─── Password Form ────────────────────────────────────────────────────────────

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
      setStatus({ type: 'error', msg: 'Mật khẩu hiện tại không đúng' })
      return
    }
    if (form.newPassword.length < 4) {
      setStatus({ type: 'error', msg: 'Mật khẩu mới cần tối thiểu 4 ký tự' })
      return
    }
    if (form.newPassword !== form.confirmPassword) {
      setStatus({ type: 'error', msg: 'Nhập lại mật khẩu chưa khớp' })
      return
    }

    try {
      setIsSaving(true)
      const updatedUser = { ...user, password: form.newPassword }
      await updateUser(user.id, updatedUser)
      onUserUpdated(updatedUser)
      setForm({ currentPassword: '', newPassword: '', confirmPassword: '' })
      setStatus({ type: 'success', msg: 'Đã cập nhật mật khẩu thành công' })
    } catch (err) {
      setStatus({ type: 'error', msg: err.message || 'Không thể cập nhật mật khẩu' })
    } finally {
      setIsSaving(false)
    }
  }

  return (
    <form className="sd-pw-form" onSubmit={handleSubmit}>
      {['currentPassword', 'newPassword', 'confirmPassword'].map((field) => (
        <div key={field} className="sd-field">
          <label>
            {field === 'currentPassword'
              ? 'Mật khẩu hiện tại'
              : field === 'newPassword'
              ? 'Mật khẩu mới'
              : 'Nhập lại mật khẩu mới'}
          </label>
          <input
            autoComplete={field === 'currentPassword' ? 'current-password' : 'new-password'}
            name={field}
            onChange={handleChange}
            type="password"
            value={form[field]}
          />
        </div>
      ))}
      {status && (
        <p className={`sd-status sd-status-${status.type}`}>{status.msg}</p>
      )}
      <button className="sd-btn-primary" disabled={isSaving} type="submit">
        {isSaving ? 'Đang lưu…' : 'Cập nhật mật khẩu'}
      </button>
    </form>
  )
}