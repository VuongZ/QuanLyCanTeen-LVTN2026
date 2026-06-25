import { useState, useEffect } from 'react'
import { updateUser } from '../api/UserApi'
import { getAllPeriods } from '../api/PeriodApi'
import { getAllShifts } from '../api/ShiftApi'
import axios from 'axios'
import QRCode from 'qrcode'
import './css/dashboard.css'

// ==========================================
// HÀM TIỆN ÍCH
// ==========================================
function getInitials(name = '') {
  return name.split(' ').filter(Boolean).slice(-2).map((p) => p[0]).join('').toUpperCase()
}

function formatDate(value) {
  if (!value) return 'Chưa có'
  return new Intl.DateTimeFormat('vi-VN').format(new Date(value))
}

const DAY_NAMES = ['Chủ nhật', 'Thứ 2', 'Thứ 3', 'Thứ 4', 'Thứ 5', 'Thứ 6', 'Thứ 7']

// ==========================================
// COMPONENT CHÍNH: STAFF DASHBOARD
// ==========================================
function buildEmployeeQrPayload(user) {
  return JSON.stringify({
    type: 'EMPLOYEE',
    id: user.id,
    username: user.username,
    fullName: user.fullName,
    roleName: user.roleName,
    branchId: user.branchId,
    branchName: user.branchName,
    hireDate: user.hireDate,
  })
}

export function EmployeeQrCard({ user }) {
  const [qrUrl, setQrUrl] = useState('')
  const qrPayload = buildEmployeeQrPayload(user)

  useEffect(() => {
    let isMounted = true
    QRCode.toDataURL(qrPayload, {
      errorCorrectionLevel: 'M',
      margin: 2,
      width: 220,
      color: {
        dark: '#1e293b',
        light: '#ffffff',
      },
    })
      .then((url) => {
        if (isMounted) setQrUrl(url)
      })
      .catch(() => {
        if (isMounted) setQrUrl('')
      })

    return () => {
      isMounted = false
    }
  }, [qrPayload])

  function downloadQr() {
    if (!qrUrl) return
    const link = document.createElement('a')
    link.href = qrUrl
    link.download = `employee-${user.username || user.id}-qr.png`
    link.click()
  }

  return (
    <div className="sd-card sd-employee-qr-card">
      <div className="sd-employee-qr-info">
        <div className="sd-info-avatar">{getInitials(user.fullName || user.username)}</div>
        <div>
          <p className="sd-eyebrow">Mã QR nhân viên</p>
          <h2>{user.fullName || user.username}</h2>
          <dl className="sd-employee-qr-list">
            <InfoRow label="Username" value={user.username || '---'} />
            <InfoRow label="Chức vụ" value={user.roleName || '---'} />
            <InfoRow label="Chi nhánh" value={user.branchName || 'Chưa gán'} />
          </dl>
        </div>
      </div>

      <div className="sd-employee-qr-box">
        {qrUrl ? (
          <img alt="Mã QR nhân viên" src={qrUrl} />
        ) : (
          <div className="sd-employee-qr-placeholder">Đang tạo QR...</div>
        )}
        <button className="sd-btn-primary sd-employee-qr-download" disabled={!qrUrl} onClick={downloadQr} type="button">
          Tải QR
        </button>
      </div>
    </div>
  )
}

export function StaffDashboard({ branches, onLogout, onUserUpdated, user }) {
  const [activeTab, setActiveTab] = useState('schedule')
  const [isMenuOpen, setIsMenuOpen] = useState(false) 

  const branch = branches?.find((b) => b.id === user.branchId)

  const getHeaderInfo = () => {
    switch (activeTab) {
      case 'profile': return { eyebrow: 'Tài khoản', title: 'Hồ sơ của tôi' }
      // 👉 Gộp chung thành 1 tiêu đề duy nhất
      case 'schedule': return { eyebrow: 'Công việc', title: 'Lịch & Đăng ký ca' }
      case 'security': return { eyebrow: 'Cài đặt', title: 'Bảo mật tài khoản' }
      default: return { eyebrow: '', title: '' }
    }
  }
  const headerInfo = getHeaderInfo()

  // 👉 Menu giờ chỉ còn 3 Tab gọn gàng
  const NAV_ITEMS = [
    { id: 'schedule', icon: '🗓️', label: 'Lịch & Đăng ký' },
    { id: 'profile', icon: '◎', label: 'Tài khoản' },
    { id: 'security', icon: '🔒', label: 'Bảo mật' },
  ]

  return (
    <div className="sd-root sd-root--left-nav">
      <header className="sd-topbar">
        <div className="sd-brand">
          <button className="sd-hamburger" onClick={() => setIsMenuOpen(true)}>☰</button>
          <span className="sd-brand-icon">CT</span>
          <span className="sd-brand-name">Canteen</span>
        </div>
        <button className="sd-logout-btn" onClick={onLogout}>
          <span>Đăng xuất</span> ↩
        </button>
      </header>

      <div className="sd-layout">
        {isMenuOpen && <div className="sd-menu-overlay" onClick={() => setIsMenuOpen(false)}></div>}

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
                onClick={() => { setActiveTab(item.id); setIsMenuOpen(false) }}
                type="button"
              >
                <span className="sd-nav-icon">{item.icon}</span>
                <span className="sd-nav-label">{item.label}</span>
              </button>
            ))}
          </div>
          <button className="sd-left-nav-logout" onClick={onLogout}>↩ Đăng xuất</button>
        </nav>

        <main className="sd-main">
          <div className="sd-page-header">
            <div>
              <p className="sd-eyebrow">{headerInfo.eyebrow}</p>
              <h1>{headerInfo.title}</h1>
            </div>
            <div className="sd-branch-badge">📍 {branch?.name || user.branchName || 'Chưa gán'}</div>
          </div>

          <div className="sd-content">
            {/* 👉 GỌI COMPONENT GỘP VÀO ĐÂY */}
            {activeTab === 'schedule' && <UnifiedScheduleTab user={user} />}
            
            {activeTab === 'profile' && <ProfileTab branch={branch} user={user} />}
            {activeTab === 'security' && <SecurityTab onUserUpdated={onUserUpdated} user={user} />}
          </div>
        </main>
      </div>
    </div>
  )
}

// ==========================================
// 👉 COMPONENT MỚI: MÀN HÌNH GỘP (LỊCH & ĐĂNG KÝ)
// ==========================================
export function UnifiedScheduleTab({ user }) {
  const [periods, setPeriods] = useState([])
  const [selectedPeriodId, setSelectedPeriodId] = useState('')
  const [loading, setLoading] = useState(true)

  useEffect(() => {
    async function init() {
      try {
        const allPeriods = await getAllPeriods()
        
        // Lấy tất cả đợt của cơ sở này (Bao gồm cả Đang Mở và Đã Chốt)
        const branchPeriods = allPeriods
          .filter(p => String(p.branchId) === String(user.branchId))
          .filter(p => {
            const st = p.status?.trim().toLowerCase()
            return st === 'mở' || st === 'open' || st === 'published'
          })
          .sort((a, b) => new Date(b.startDate) - new Date(a.startDate)) // Gần nhất lên đầu
        
        setPeriods(branchPeriods)
        if (branchPeriods.length > 0) {
          setSelectedPeriodId(branchPeriods[0].id.toString())
        }
      } catch (e) {
        console.error("Lỗi lấy danh sách đợt:", e)
      } finally {
        setLoading(false)
      }
    }
    init()
  }, [user.branchId])

  if (loading) return <div className="sd-card"><p>Đang tải dữ liệu...</p></div>

  if (periods.length === 0) {
    return (
      <div className="sd-card">
        <div className="sd-empty-state" style={{ padding: '40px 20px' }}>
          <span className="sd-empty-icon">🗓️</span>
          <h3 style={{ color: '#1e293b', marginTop: 10 }}>Chưa có dữ liệu lịch làm</h3>
          <p>Hiện tại cơ sở của bạn chưa có lịch làm chính thức cũng như đợt đăng ký ca nào được mở.</p>
        </div>
      </div>
    )
  }

  // Tìm ra đợt đang được chọn trong Dropdown
  const selectedPeriod = periods.find(p => p.id.toString() === selectedPeriodId)
  const isPublished = selectedPeriod?.status === 'PUBLISHED'

  return (
    <div className="sd-card" style={{ padding: '20px 0' }}>
      {/* 1. BỘ LỌC CHỌN TUẦN LÀM VIỆC */}
      <div style={{ padding: '0 20px 16px', display: 'flex', gap: 12, alignItems: 'center', borderBottom: '1px solid #f1f5f9', marginBottom: 16 }}>
        <span style={{ fontSize: 14, fontWeight: 600, color: '#475569', whiteSpace: 'nowrap' }}>Chọn tuần:</span>
        <select 
          className="sd-input-search" 
          style={{ width: '100%', maxWidth: 400 }}
          value={selectedPeriodId}
          onChange={(e) => setSelectedPeriodId(e.target.value)}
        >
          {periods.map(p => {
            const st = p.status === 'PUBLISHED' ? '(Đã chốt lịch)' : '(Đang mở đăng ký)'
            return (
              <option key={p.id} value={p.id}>
                Từ {formatDate(p.startDate)} đến {formatDate(p.endDate)} {st}
              </option>
            )
          })}
        </select>
      </div>

      {/* 2. ĐIỀU HƯỚNG GIAO DIỆN DỰA THEO TRẠNG THÁI */}
      <div style={{ padding: '0 20px' }}>
        {isPublished 
          ? <PublishedScheduleView period={selectedPeriod} user={user} /> 
          : <RegistrationView period={selectedPeriod} user={user} />
        }
      </div>
    </div>
  )
}

// ==========================================
// 2A. CHẾ ĐỘ: XEM LỊCH ĐÃ CHỐT (READ-ONLY)
// ==========================================
function PublishedScheduleView({ period, user }) {
  const [registrations, setRegistrations] = useState([])
  const [shifts, setShifts] = useState([])
  const [dates, setDates] = useState([])
  const [loading, setLoading] = useState(true)

  useEffect(() => {
    async function loadBoard() {
      setLoading(true)
      try {
        const [regRes, shiftRes] = await Promise.all([
          axios.get(`/api/StaffRegistration/period/${period.id}`),
          getAllShifts()
        ])

        const approvedRegs = (regRes.data || []).filter(r => r.status === 'Đã Duyệt')
        setRegistrations(approvedRegs)
        setShifts(shiftRes.filter(s => String(s.branchId) === String(user.branchId)))

        const dArray = []
        let curr = new Date(period.startDate)
        const end = new Date(period.endDate)
        while (curr <= end) {
          dArray.push(new Date(curr))
          curr.setDate(curr.getDate() + 1)
        }
        setDates(dArray)
      } catch (e) { console.error("Lỗi:", e) } finally { setLoading(false) }
    }
    loadBoard()
  }, [period.id, user.branchId]) // Chú ý dependency: Khi đổi period trong menu, nó sẽ tự động chạy lại!

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

  if (loading) return <p>Đang tải bảng lịch làm việc...</p>

  return (
    <>
      <div style={{ marginBottom: 16 }}>
        <h2 style={{ color: '#1d4ed8', margin: '0 0 4px' }}>Lịch làm việc chính thức</h2>
       
      </div>

      <div className="sd-board-wrap" style={{ borderRadius: 12 }}>
        <table className="sd-schedule-board">
          <thead>
            <tr>
              <th style={{ width: 90 }}>NGÀY</th>
              {shifts.map(s => (
                <th key={s.id}>
                  {s.shiftName}<br/>
                  <span style={{fontWeight: 500, fontSize: 11}}>{s.startTime?.slice(0, 5)} - {s.endTime?.slice(0, 5)}</span>
                </th>
              ))}
            </tr>
          </thead>
          <tbody>
            {dates.map((dateObj) => {
              const dStr = toDateString(dateObj)
              const dayOfWeek = DAY_NAMES[dateObj.getDay()]
              const shortDate = `${dateObj.getDate()}/${dateObj.getMonth() + 1}`

              return (
                <tr key={dStr}>
                  <td className="sd-board-date-col">
                    <strong>{dayOfWeek}</strong>
                    <small>{shortDate}</small>
                  </td>

                  {shifts.map(shift => {
                    const cellRegs = boardMatrix[dStr][shift.id] || []
                    
                    // 👉 MẸO LOGIC CHO NHÂN VIÊN
                    const isWeekend = dayOfWeek === 'Thứ 7' || dayOfWeek === 'Chủ nhật'
                    const isShiftClosed = isWeekend && cellRegs.length === 0

                    return (
                      <td key={shift.id}>
                        {!isShiftClosed ? (
                          <div className="sd-reg-card" style={{ background: '#ffedd5', borderColor: '#fdba74', color: '#9a3412' }}>
                            <span className="sd-reg-name"> Quản lý ca</span>
                          </div>
                        ) : (
                          <div style={{ textAlign: 'center', padding: '16px 0', color: '#cbd5e1', fontSize: 12, fontWeight: 600 }}>
                         KHÔNG CÓ CA LÀM  
                          </div>
                        )}
                        
                        {cellRegs.map(r => {
                          const staffName = r.user?.fullName || r.user?.username || 'Nhân viên'
                          const isMe = r.userId === user.id
                          return (
                            <div 
                              key={r.id} 
                              className="sd-reg-card" 
                              style={{ 
                                background: isMe ? '#dbeafe' : '#f8fafc', 
                                borderColor: isMe ? '#93c5fd' : '#e2e8f0', 
                                color: isMe ? '#1e3a8a' : '#475569',
                                fontWeight: isMe ? 700 : 500
                              }}
                            >
                              <span className="sd-reg-name" title={staffName}>{isMe ? ' ' + staffName : staffName}</span>
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
    </>
  )
}

// ==========================================
// 2B. CHẾ ĐỘ: ĐĂNG KÝ CA LÀM
// ==========================================
function RegistrationView({ period, user }) {
  const [shifts, setShifts] = useState([])
  const [dates, setDates] = useState([])
  const [registered, setRegistered] = useState({})
  const [dbRegistrations, setDbRegistrations] = useState({}) 
  const [saved, setSaved] = useState(false)
  const [saving, setSaving] = useState(false)
  const [loading, setLoading] = useState(true)

  useEffect(() => {
    async function loadData() {
      setLoading(true)
      try {
        const allShifts = await getAllShifts()
        setShifts(allShifts.filter((s) => String(s.branchId) === String(user.branchId)))

        // Sinh mảng ngày
        const dArray = []
        let curr = new Date(period.startDate)
        const end = new Date(period.endDate)
        while (curr <= end) {
          dArray.push(new Date(curr))
          curr.setDate(curr.getDate() + 1)
        }
        setDates(dArray)

        // Tải lịch sử đăng ký của nhân viên này
        const regRes = await axios.get(`/api/StaffRegistration/my-schedule/${user.id}/${period.id}`)
        const myRegs = regRes.data || []
        
        const dbMap = {}
        const initRegs = {}

        myRegs.forEach(r => {
          const dStr = r.workDate.slice(0, 10) 
          if (!dbMap[dStr]) { dbMap[dStr] = {}; initRegs[dStr] = {} }
          dbMap[dStr][r.shiftId] = { id: r.id, status: r.status } 
          initRegs[dStr][r.shiftId] = true 
        })

        setDbRegistrations(dbMap)
        setRegistered(initRegs)
      } catch (err) { console.error('Lỗi:', err) } finally { setLoading(false) }
    }
    loadData()
  }, [period.id, user.id, user.branchId]) // Tự động load lại nếu Manager đổi tuần khác

  function toDateString(dateObj) {
    const offset = dateObj.getTimezoneOffset()
    const d = new Date(dateObj.getTime() - (offset * 60 * 1000))
    return d.toISOString().split('T')[0]
  }

  function toggle(dateStr, shiftId) {
    const dbItem = dbRegistrations[dateStr]?.[shiftId]
    if (dbItem && dbItem.status !== "Chờ Duyệt") return 

    setSaved(false)
    setRegistered((prev) => {
      const dayRegs = prev[dateStr] || {}
      return { ...prev, [dateStr]: { ...dayRegs, [shiftId]: !dayRegs[shiftId] } }
    })
  }

  function getChanges() {
    const adds = []
    const deletes = []

    Object.entries(registered).forEach(([dStr, shiftsInfo]) => {
      Object.entries(shiftsInfo).forEach(([sId, isSelected]) => {
        if (isSelected && !dbRegistrations[dStr]?.[sId]) {
          adds.push({ userId: user.id, periodId: period.id, shiftId: parseInt(sId), workDate: dStr, status: "Chờ Duyệt" })
        }
      })
    })

    Object.entries(dbRegistrations).forEach(([dStr, shiftsInfo]) => {
      Object.entries(shiftsInfo).forEach(([sId, dbItem]) => {
        const isSelectedNow = registered[dStr]?.[sId]
        if (!isSelectedNow && dbItem.status === "Chờ Duyệt") deletes.push(dbItem.id) 
      })
    })

    return { adds, deletes }
  }

  async function handleSave() {
    const { adds, deletes } = getChanges()
    if (adds.length === 0 && deletes.length === 0) return alert("Không có thay đổi nào để lưu!")
    setSaving(true)

    try {
      const apiCalls = [
        ...adds.map(payload => axios.post('/api/StaffRegistration', payload)),
        ...deletes.map(regId => axios.delete(`/api/StaffRegistration/${regId}/user/${user.id}`))
      ]
      await Promise.all(apiCalls)
      
      // Load lại để đồng bộ state DB
      const regRes = await axios.get(`/api/StaffRegistration/my-schedule/${user.id}/${period.id}`)
      const dbMap = {}; const initRegs = {}
      ;(regRes.data || []).forEach(r => {
        const dStr = r.workDate.slice(0, 10) 
        if (!dbMap[dStr]) { dbMap[dStr] = {}; initRegs[dStr] = {} }
        dbMap[dStr][r.shiftId] = { id: r.id, status: r.status } 
        initRegs[dStr][r.shiftId] = true 
      })
      setDbRegistrations(dbMap)
      setRegistered(initRegs)
      setSaved(true)

    } catch (err) { alert("❌ Lỗi: " + (err.response?.data?.message || 'Có lỗi xảy ra!')) } 
    finally { setSaving(false) }
  }

  function handleReset() {
    const resetRegs = {}
    Object.keys(dbRegistrations).forEach(d => {
      resetRegs[d] = {}
      Object.keys(dbRegistrations[d]).forEach(sId => { resetRegs[d][sId] = true })
    })
    setRegistered(resetRegs)
    setSaved(false)
  }

  if (loading) return <p>Đang tải form đăng ký...</p>

  const { adds, deletes } = getChanges()
  const totalChanges = adds.length + deletes.length

  return (
    <>
      <div style={{ marginBottom: 16 }}>
        <h2 style={{ color: '#ea580c', margin: '0 0 4px' }}>Đăng ký ca làm việc</h2>
        <p style={{ fontSize: 13, color: '#64748b', margin: 0 }}>Quản lý đang mở đăng ký cho tuần này. Hãy chọn các ca bạn có thể làm.</p>
      </div>

      <div className="sd-shift-legend" style={{ marginLeft: -20, marginRight: -20, paddingLeft: 20 }}>
        {shifts.length === 0 && <p style={{fontSize: 13}}>Chưa cấu hình ca làm việc.</p>}
        {shifts.map((s) => (
          <div key={s.id} className="sd-shift-legend-item">
            <span>⏱️</span>
            <div><strong>{s.shiftName}</strong><small>{s.startTime?.slice(0, 5)} – {s.endTime?.slice(0, 5)}</small></div>
          </div>
        ))}
      </div>

      {shifts.length > 0 && dates.length > 0 && (
        <div className="sd-shift-grid-vertical">
          <div className="sd-grid-row sd-grid-header-row">
            <div className="sd-grid-corner-v" />
            {shifts.map((s) => <div key={s.id} className="sd-grid-shift-col-label">{s.shiftName}</div>)}
          </div>

          {dates.map((dateObj) => {
            const dateStr = toDateString(dateObj)
            const dayOfWeek = DAY_NAMES[dateObj.getDay()]
            const shortDate = `${dateObj.getDate()}/${dateObj.getMonth() + 1}`

            return (
              <div key={dateStr} className="sd-grid-row">
                <div className="sd-grid-day-row-label">
                  <strong>{dayOfWeek}</strong><small>{shortDate}</small>
                </div>
                
                {shifts.map((shift) => {
                  const isOn = registered[dateStr]?.[shift.id] || false
                  const dbItem = dbRegistrations[dateStr]?.[shift.id]
                  const isLocked = dbItem && dbItem.status !== "Chờ Duyệt"

                  return (
                    <button
                      key={shift.id}
                      className={`sd-shift-cell-v ${isOn ? 'selected' : ''}`}
                      onClick={() => toggle(dateStr, shift.id)}
                      type="button"
                      style={isLocked ? { opacity: 0.6, cursor: 'not-allowed', backgroundColor: '#fed7aa', borderColor: '#ea580c' } : {}}
                    >
                      {isOn ? (isLocked ? '🔒' : '✓') : ''}
                    </button>
                  )
                })}
              </div>
            )
          })}
        </div>
      )}

      <div className="sd-shift-actions">
        <button className="sd-btn-ghost" onClick={handleReset} type="button" disabled={totalChanges === 0}>Hoàn tác thay đổi</button>
        <button className="sd-btn-primary" disabled={saving || totalChanges === 0} onClick={handleSave} type="button">
          {saving ? 'Đang lưu…' : `Xác nhận lưu thay đổi (${totalChanges} ca)`}
        </button>
      </div>

      {saved && totalChanges === 0 && (
        <p className="sd-save-notice" style={{ color: '#15803d', fontSize: 13, marginTop: 12, textAlign: 'center' }}>
          ✅ Dữ liệu đã được đồng bộ. Các ca đăng ký sẽ có biểu tượng (🔒) nếu quản lý đã bắt đầu duyệt.
        </p>
      )}
    </>
  )
}

// ==========================================
// COMPONENT PHỤ: HỒ SƠ & MẬT KHẨU
// ==========================================
function ProfileTab({ branch, user }) {
  return (
    <div className="sd-profile-layout">
      <EmployeeQrCard user={user} />
      <div className="sd-card">
        <div className="sd-card-header"><p className="sd-eyebrow">Chi tiết</p><h2>Hồ sơ nhân viên</h2></div>
        <div className="sd-info-hero">
          <div className="sd-info-avatar">{getInitials(user.fullName || user.username)}</div>
          <div><h3>{user.fullName || user.username}</h3><span className="sd-role-badge">{user.roleName || 'Nhân viên'}</span></div>
        </div>
        <dl className="sd-dl">
      
          <InfoRow label="Họ và tên" value={user.fullName || '—'} />
          <InfoRow label="Chi nhánh" value={branch?.name || user.branchName || 'Chưa có'} />
          <InfoRow label="Ngày vào làm" value={formatDate(user.hireDate)} />
        </dl>
      </div>
    </div>
  )
}

function InfoRow({ label, value }) {
  return <div className="sd-info-row"><dt>{label}</dt><dd>{value}</dd></div>
}

function SecurityTab({ onUserUpdated, user }) {
  return (
    <div className="sd-profile-layout">
      <div className="sd-card">
        <div className="sd-card-header"><p className="sd-eyebrow">Bảo mật</p><h2>Đổi mật khẩu</h2></div>
        <PasswordForm onUserUpdated={onUserUpdated} user={user} />
      </div>
    </div>
  )
}

function PasswordForm({ onUserUpdated, user }) {
  const [form, setForm] = useState({ currentPassword: '', newPassword: '', confirmPassword: '' })
  const [status, setStatus] = useState(null)
  const [isSaving, setIsSaving] = useState(false)

  function handleChange(e) { setForm((f) => ({ ...f, [e.target.name]: e.target.value })) }

  async function handleSubmit(e) {
    e.preventDefault(); setStatus(null)
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
    } catch (err) { setStatus({ type: 'error', msg: 'Không thể cập nhật mật khẩu' }) } 
    finally { setIsSaving(false) }
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
