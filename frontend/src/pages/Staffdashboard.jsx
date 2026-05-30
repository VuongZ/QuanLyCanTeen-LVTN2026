import { useState } from 'react'
import { updateUser } from '../api/UserApi'

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
  { id: 'morning', label: 'Ca Sáng', time: '06:00 – 14:00', icon: '🌅' },
  { id: 'afternoon', label: 'Ca Chiều', time: '14:00 – 22:00', icon: '🌤' },
  { id: 'night', label: 'Ca Đêm', time: '22:00 – 06:00', icon: '🌙' },
]

const DAYS = ['T2', 'T3', 'T4', 'T5', 'T6', 'T7', 'CN']
const DAY_LABELS = ['Thứ 2', 'Thứ 3', 'Thứ 4', 'Thứ 5', 'Thứ 6', 'Thứ 7', 'Chủ nhật']

// ─── Main Component ──────────────────────────────────────────────────────────

export function StaffDashboard({ branches, onLogout, onUserUpdated, roles, user, users }) {
  const [activeTab, setActiveTab] = useState('profile')
  const branch = branches?.find((b) => b.id === user.branchId)

  return (
    <>
      <style>{styles}</style>
      <div className="sd-root">
        {/* Sidebar */}
        <aside className="sd-sidebar">
          <div className="sd-brand">
            <span className="sd-brand-icon">⬡</span>
            <span className="sd-brand-name">WorkFlow</span>
          </div>

          <div className="sd-avatar-block">
            <div className="sd-avatar">{getInitials(user.fullName || user.username)}</div>
            <div className="sd-avatar-info">
              <strong>{user.fullName || user.username}</strong>
              <span>{user.roleName || 'Nhân viên'}</span>
            </div>
          </div>

          <nav className="sd-nav">
            {[
              { id: 'profile', icon: '◎', label: 'Thông tin cá nhân' },
              { id: 'shifts', icon: '⊞', label: 'Đăng ký ca làm' },
            ].map((item) => (
              <button
                key={item.id}
                className={`sd-nav-item ${activeTab === item.id ? 'active' : ''}`}
                onClick={() => setActiveTab(item.id)}
                type="button"
              >
                <span className="sd-nav-icon">{item.icon}</span>
                {item.label}
              </button>
            ))}
          </nav>

          <button className="sd-logout" onClick={onLogout} type="button">
            <span>↩</span> Đăng xuất
          </button>
        </aside>

        {/* Main content */}
        <main className="sd-main">
          <div className="sd-topbar">
            <div>
              <p className="sd-eyebrow">
                {activeTab === 'profile' ? 'Tài khoản' : 'Lịch làm việc'}
              </p>
              <h1>{activeTab === 'profile' ? 'Thông tin cá nhân' : 'Đăng ký ca làm'}</h1>
            </div>
            <div className="sd-branch-badge">
              <span>📍</span>
              {branch?.name || user.branchName || 'Chưa gán chi nhánh'}
            </div>
          </div>

          <div className="sd-content">
            {activeTab === 'profile' && (
              <ProfileTab
                branch={branch}
                onUserUpdated={onUserUpdated}
                user={user}
              />
            )}
            {activeTab === 'shifts' && <ShiftsTab user={user} />}
          </div>
        </main>
      </div>
    </>
  )
}

// ─── Profile Tab ─────────────────────────────────────────────────────────────

function ProfileTab({ branch, onUserUpdated, user }) {
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

      {/* Password card */}
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

function InfoRow({ label, value }) {
  return (
    <div className="sd-info-row">
      <dt>{label}</dt>
      <dd>{value}</dd>
    </div>
  )
}

// ─── Shifts Tab ──────────────────────────────────────────────────────────────

function ShiftsTab({ user }) {
  // registered[dayIndex][shiftId] = true/false
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
  const [status, setStatus] = useState(null) // { type: 'error'|'success', msg }
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

// ─── Styles ───────────────────────────────────────────────────────────────────

const styles = `
  @import url('https://fonts.googleapis.com/css2?family=Be+Vietnam+Pro:wght@400;500;600;700&display=swap');

  .sd-root {
    display: flex;
    min-height: 100vh;
    font-family: 'Be Vietnam Pro', sans-serif;
    background: #f0f2f5;
    color: #1a1a2e;
  }

  /* ── Sidebar ── */
  .sd-sidebar {
    width: 240px;
    min-height: 100vh;
    background: #0f172a;
    display: flex;
    flex-direction: column;
    padding: 24px 16px;
    gap: 8px;
    position: sticky;
    top: 0;
    flex-shrink: 0;
  }

  .sd-brand {
    display: flex;
    align-items: center;
    gap: 10px;
    padding: 0 8px 20px;
    border-bottom: 1px solid rgba(255,255,255,0.08);
    margin-bottom: 8px;
  }
  .sd-brand-icon { font-size: 22px; }
  .sd-brand-name {
    font-size: 17px;
    font-weight: 700;
    color: #fff;
    letter-spacing: -0.3px;
  }

  .sd-avatar-block {
    display: flex;
    align-items: center;
    gap: 10px;
    background: rgba(255,255,255,0.06);
    border-radius: 12px;
    padding: 12px;
    margin-bottom: 16px;
  }
  .sd-avatar {
    width: 38px;
    height: 38px;
    border-radius: 10px;
    background: linear-gradient(135deg, #6366f1, #8b5cf6);
    color: #fff;
    display: flex;
    align-items: center;
    justify-content: center;
    font-weight: 700;
    font-size: 14px;
    flex-shrink: 0;
  }
  .sd-avatar-info { overflow: hidden; }
  .sd-avatar-info strong {
    display: block;
    color: #fff;
    font-size: 13px;
    white-space: nowrap;
    overflow: hidden;
    text-overflow: ellipsis;
  }
  .sd-avatar-info span {
    font-size: 11px;
    color: #94a3b8;
  }

  .sd-nav { display: flex; flex-direction: column; gap: 4px; flex: 1; }

  .sd-nav-item {
    display: flex;
    align-items: center;
    gap: 10px;
    padding: 10px 12px;
    border-radius: 10px;
    border: none;
    background: transparent;
    color: #94a3b8;
    font-family: inherit;
    font-size: 13.5px;
    font-weight: 500;
    cursor: pointer;
    text-align: left;
    transition: background 0.15s, color 0.15s;
  }
  .sd-nav-item:hover { background: rgba(255,255,255,0.07); color: #e2e8f0; }
  .sd-nav-item.active {
    background: linear-gradient(135deg, rgba(99,102,241,0.25), rgba(139,92,246,0.15));
    color: #a5b4fc;
    font-weight: 600;
  }
  .sd-nav-icon { font-size: 15px; }

  .sd-logout {
    display: flex;
    align-items: center;
    gap: 8px;
    padding: 10px 12px;
    border-radius: 10px;
    border: 1px solid rgba(255,255,255,0.1);
    background: transparent;
    color: #64748b;
    font-family: inherit;
    font-size: 13px;
    cursor: pointer;
    margin-top: 8px;
    transition: all 0.15s;
  }
  .sd-logout:hover { color: #f87171; border-color: rgba(248,113,113,0.3); }

  /* ── Main ── */
  .sd-main {
    flex: 1;
    display: flex;
    flex-direction: column;
    min-width: 0;
  }

  .sd-topbar {
    background: #fff;
    padding: 20px 32px;
    border-bottom: 1px solid #e8eaed;
    display: flex;
    justify-content: space-between;
    align-items: center;
    position: sticky;
    top: 0;
    z-index: 10;
  }
  .sd-topbar h1 {
    margin: 0;
    font-size: 22px;
    font-weight: 700;
    letter-spacing: -0.4px;
  }
  .sd-eyebrow {
    margin: 0 0 2px;
    font-size: 11px;
    font-weight: 600;
    letter-spacing: 1px;
    text-transform: uppercase;
    color: #6366f1;
  }
  .sd-branch-badge {
    display: flex;
    align-items: center;
    gap: 6px;
    background: #f1f5f9;
    border-radius: 20px;
    padding: 7px 14px;
    font-size: 13px;
    font-weight: 500;
    color: #475569;
  }

  .sd-content { padding: 28px 32px; flex: 1; }

  /* ── Cards ── */
  .sd-card {
    background: #fff;
    border-radius: 16px;
    padding: 28px;
    box-shadow: 0 1px 3px rgba(0,0,0,0.06), 0 4px 16px rgba(0,0,0,0.04);
  }
  .sd-card-header { margin-bottom: 24px; }
  .sd-card-header h2 {
    margin: 2px 0 0;
    font-size: 18px;
    font-weight: 700;
    letter-spacing: -0.3px;
  }

  /* ── Profile layout ── */
  .sd-profile-layout {
    display: grid;
    grid-template-columns: 1fr 1fr;
    gap: 20px;
    align-items: start;
  }
  @media (max-width: 900px) {
    .sd-profile-layout { grid-template-columns: 1fr; }
  }

  .sd-info-hero {
    display: flex;
    align-items: center;
    gap: 16px;
    padding: 18px;
    background: linear-gradient(135deg, #eef2ff, #f5f3ff);
    border-radius: 12px;
    margin-bottom: 20px;
  }
  .sd-info-avatar {
    width: 52px;
    height: 52px;
    border-radius: 14px;
    background: linear-gradient(135deg, #6366f1, #8b5cf6);
    color: #fff;
    display: flex;
    align-items: center;
    justify-content: center;
    font-weight: 700;
    font-size: 18px;
    flex-shrink: 0;
  }
  .sd-info-hero h3 { margin: 0 0 4px; font-size: 16px; font-weight: 700; }
  .sd-role-badge {
    background: linear-gradient(135deg, #6366f1, #8b5cf6);
    color: #fff;
    border-radius: 20px;
    padding: 3px 10px;
    font-size: 11px;
    font-weight: 600;
    letter-spacing: 0.3px;
  }

  .sd-dl { display: flex; flex-direction: column; gap: 0; }
  .sd-info-row {
    display: flex;
    justify-content: space-between;
    align-items: center;
    padding: 11px 0;
    border-bottom: 1px solid #f1f5f9;
    gap: 16px;
  }
  .sd-info-row:last-child { border-bottom: none; }
  .sd-info-row dt { font-size: 13px; color: #64748b; font-weight: 500; flex-shrink: 0; }
  .sd-info-row dd { font-size: 13.5px; font-weight: 600; color: #1e293b; margin: 0; text-align: right; }

  /* ── Password form ── */
  .sd-pw-form { display: flex; flex-direction: column; gap: 14px; }
  .sd-field { display: flex; flex-direction: column; gap: 6px; }
  .sd-field label { font-size: 12.5px; font-weight: 600; color: #475569; letter-spacing: 0.2px; }
  .sd-field input {
    padding: 10px 14px;
    border: 1.5px solid #e2e8f0;
    border-radius: 10px;
    font-family: inherit;
    font-size: 14px;
    color: #1e293b;
    outline: none;
    transition: border-color 0.15s;
    background: #f8fafc;
  }
  .sd-field input:focus { border-color: #6366f1; background: #fff; }

  .sd-status {
    margin: 0;
    padding: 10px 14px;
    border-radius: 8px;
    font-size: 13px;
    font-weight: 500;
  }
  .sd-status-error { background: #fef2f2; color: #dc2626; }
  .sd-status-success { background: #f0fdf4; color: #16a34a; }

  /* ── Buttons ── */
  .sd-btn-primary {
    padding: 11px 20px;
    background: linear-gradient(135deg, #6366f1, #8b5cf6);
    color: #fff;
    border: none;
    border-radius: 10px;
    font-family: inherit;
    font-size: 14px;
    font-weight: 600;
    cursor: pointer;
    transition: opacity 0.15s, transform 0.1s;
  }
  .sd-btn-primary:hover:not(:disabled) { opacity: 0.9; transform: translateY(-1px); }
  .sd-btn-primary:disabled { opacity: 0.55; cursor: not-allowed; }

  .sd-btn-ghost {
    padding: 11px 20px;
    background: transparent;
    color: #64748b;
    border: 1.5px solid #e2e8f0;
    border-radius: 10px;
    font-family: inherit;
    font-size: 14px;
    font-weight: 600;
    cursor: pointer;
    transition: border-color 0.15s, color 0.15s;
  }
  .sd-btn-ghost:hover { border-color: #94a3b8; color: #334155; }

  /* ── Shifts ── */
  .sd-shifts-card { }

  .sd-shift-legend {
    display: flex;
    gap: 12px;
    margin-bottom: 24px;
    flex-wrap: wrap;
  }
  .sd-shift-legend-item {
    display: flex;
    align-items: center;
    gap: 8px;
    background: #f8fafc;
    border: 1px solid #e2e8f0;
    border-radius: 10px;
    padding: 10px 14px;
    flex: 1;
    min-width: 140px;
  }
  .sd-shift-legend-item span { font-size: 20px; }
  .sd-shift-legend-item strong { display: block; font-size: 13px; font-weight: 700; }
  .sd-shift-legend-item small { font-size: 11.5px; color: #64748b; }

  .sd-count-badge {
    background: linear-gradient(135deg, #6366f1, #8b5cf6);
    color: #fff;
    border-radius: 20px;
    padding: 6px 14px;
    font-size: 13px;
    font-weight: 600;
    white-space: nowrap;
    align-self: flex-start;
  }

  .sd-shift-grid {
    display: flex;
    gap: 6px;
    overflow-x: auto;
    padding-bottom: 4px;
    margin-bottom: 20px;
  }

  .sd-grid-col {
    display: flex;
    flex-direction: column;
    gap: 6px;
    flex: 1;
    min-width: 70px;
  }
  .sd-grid-header-col { min-width: 110px; flex: 0 0 110px; }

  .sd-grid-corner { height: 52px; }

  .sd-grid-shift-label {
    height: 52px;
    display: flex;
    flex-direction: column;
    align-items: flex-start;
    justify-content: center;
    padding: 0 8px;
    font-size: 12px;
    font-weight: 600;
    color: #475569;
    gap: 2px;
  }

  .sd-grid-day-label {
    height: 52px;
    display: flex;
    flex-direction: column;
    align-items: center;
    justify-content: center;
    background: #f1f5f9;
    border-radius: 10px;
  }
  .sd-grid-day-label strong { font-size: 14px; font-weight: 700; color: #1e293b; }
  .sd-grid-day-label small { font-size: 10px; color: #64748b; }

  .sd-shift-cell {
    height: 52px;
    border-radius: 10px;
    border: 2px solid #e2e8f0;
    background: #f8fafc;
    cursor: pointer;
    font-size: 18px;
    font-weight: 700;
    color: #6366f1;
    transition: all 0.15s;
    display: flex;
    align-items: center;
    justify-content: center;
  }
  .sd-shift-cell:hover:not(.selected) { border-color: #a5b4fc; background: #eef2ff; }
  .sd-shift-cell.selected {
    border-color: #6366f1;
    background: linear-gradient(135deg, #6366f1, #8b5cf6);
    color: #fff;
    box-shadow: 0 4px 12px rgba(99,102,241,0.3);
  }

  .sd-shift-actions {
    display: flex;
    gap: 12px;
    justify-content: flex-end;
  }

  .sd-save-notice {
    margin-top: 14px;
    padding: 12px 16px;
    background: #f0fdf4;
    color: #15803d;
    border-radius: 10px;
    font-size: 13.5px;
    font-weight: 500;
    text-align: center;
  }

  @media (max-width: 768px) {
    .sd-sidebar { display: none; }
    .sd-content { padding: 16px; }
    .sd-topbar { padding: 16px; }
  }
`