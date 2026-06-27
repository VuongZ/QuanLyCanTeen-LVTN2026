import { useState, useEffect } from 'react'
import axios from 'axios'
import { getAllPeriods, createPeriod, updatePeriod, deletePeriod } from '../../api/PeriodApi'
import { getAllShifts } from '../../api/ShiftApi'

const DAY_NAMES = ['Chủ nhật', 'Thứ 2', 'Thứ 3', 'Thứ 4', 'Thứ 5', 'Thứ 6', 'Thứ 7']

function formatDate(value) {
  if (!value) return '—'
  return new Intl.DateTimeFormat('vi-VN').format(new Date(value))
}

export function ManagerPeriodTab({ user, isManager, branches }) {
  const [periods, setPeriods] = useState([])
  const [modal, setModal] = useState(null)
  const [selectedPeriod, setSelectedPeriod] = useState(null)
  const [search, setSearch] = useState('')
  const [saving, setSaving] = useState(false)
  const [error, setError] = useState('')
  // ✅ FIX: Chỉ lưu ID, không lưu cả object
  const [reviewingPeriodId, setReviewingPeriodId] = useState(null)
  const [form, setForm] = useState({ startDate: '', endDate: '', status: 'Mở' })

  useEffect(() => { loadPeriods() }, [])

  async function loadPeriods() {
    try { const data = await getAllPeriods(); setPeriods(Array.isArray(data) ? data : []) } catch (err) { console.error(err) }
  }

  function handleStartDateChange(e) {
    const selectedDateStr = e.target.value;
    if (!selectedDateStr) { setForm({ ...form, startDate: '', endDate: '' }); return; }
    const startDateObj = new Date(selectedDateStr);
    const endDateObj = new Date(startDateObj);
    endDateObj.setDate(startDateObj.getDate() + 6);
    const endDateStr = endDateObj.toISOString().slice(0, 10);
    setForm({ ...form, startDate: selectedDateStr, endDate: endDateStr });
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
      if (modal === 'add') await createPeriod(payload); else await updatePeriod(form.id, payload)
      await loadPeriods(); setModal(null)
    } catch (err) { setError('Lỗi khi lưu đợt.') } finally { setSaving(false) }
  }

  async function handleDelete() {
    setSaving(true); setError('')
    try { await deletePeriod(selectedPeriod.id); await loadPeriods(); setModal(null) }
    catch (err) { setError('Không thể xóa đợt đăng ký này!') } finally { setSaving(false) }
  }

  // ✅ FIX: Tìm period object từ periods state (luôn mới nhất sau loadPeriods)
  const reviewingPeriod = reviewingPeriodId ? periods.find(p => p.id === reviewingPeriodId) : null

  if (reviewingPeriod) {
    return (
      <PeriodReviewScreen
        period={reviewingPeriod}
        user={user}
        onBack={async () => {
          await loadPeriods()       // fetch DB trước
          setReviewingPeriodId(null) // rồi mới ra ngoài
        }}
      />
    )
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
              <tr><td colSpan={3} className="sd-td-empty">
                <div className="sd-empty-state"><span className="sd-empty-icon">📅</span><p>Chưa có đợt đăng ký lịch làm nào</p></div>
              </td></tr>
            )}
            {filteredPeriods.map((p) => {
              const st = p.status?.toUpperCase() || ''
              const isOpen = st === 'MỞ' || st === 'OPEN'
              const isReviewing = st === 'REVIEWING'
              const isPublished = st === 'PUBLISHED'
              return (
                // ✅ FIX: setReviewingPeriodId(p.id) thay vì setReviewingPeriod(p)
                <tr key={p.id} className="sd-tr" style={{ cursor: 'pointer' }} onClick={() => setReviewingPeriodId(p.id)}>
                  <td className="sd-td sd-td-name-col">
                    <strong style={{ color: '#1e293b' }}>Từ {formatDate(p.startDate)} đến {formatDate(p.endDate)}</strong>
                  </td>
                  <td className="sd-td sd-text-center sd-td-info-col">
                    {isPublished
                      ? <span className="sd-status-pill" style={{ background: '#e0e7ff', color: '#1d4ed8', borderColor: '#bfdbfe' }}>Đã Chốt</span>
                      : isReviewing
                      ? <span className="sd-status-pill" style={{ background: '#fef9c3', color: '#854d0e', borderColor: '#fde047' }}>Đang Xét Duyệt</span>
                      : <span className={`sd-status-pill ${isOpen ? 'sd-status-pill--open' : 'sd-status-pill--closed'}`}>
                          {isOpen ? 'Đang Mở' : 'Đã Đóng'}
                        </span>
                    }
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

      {(modal === 'add' || modal === 'edit') && (
        <div className="sd-overlay" onClick={() => setModal(null)}>
          <div className="sd-modal" onClick={(e) => e.stopPropagation()}>
            <div className="sd-modal-header"><h2>{modal === 'add' ? 'Mở đợt đăng ký mới' : 'Chỉnh sửa'}</h2><button onClick={() => setModal(null)}>✕</button></div>
            <div className="sd-modal-body">
              <div className="sd-modal-grid">
                <div className="sd-field">
                  <label>Ngày bắt đầu đợt (Bắt buộc Thứ 2) *</label>
                  <input type="date" value={form.startDate} onChange={handleStartDateChange} />
                </div>
              </div>
              <div className="sd-field">
                <label>Trạng thái đợt đăng ký</label>
                <select value={form.status} onChange={(e) => setForm({ ...form, status: e.target.value })}>
                  <option value="Mở">Mở (Cho phép nhân viên đăng ký)</option>
                  <option value="Đóng">Đóng (Khóa đăng ký)</option>
                </select>
              </div>
              {error && <p className="sd-status sd-status-error">{error}</p>}
            </div>
            <div className="sd-modal-footer">
              <button className="sd-btn-ghost" onClick={() => setModal(null)}>Huỷ</button>
              <button className="sd-btn-primary" disabled={saving} onClick={handleSave}>Lưu đợt</button>
            </div>
          </div>
        </div>
      )}

      {modal === 'delete' && (
        <div className="sd-overlay" onClick={() => setModal(null)}>
          <div className="sd-modal" onClick={(e) => e.stopPropagation()}>
            <div className="sd-modal-header"><h2>Xác nhận xoá đợt</h2><button onClick={() => setModal(null)}>✕</button></div>
            <div className="sd-modal-body">
              <p>Bạn có chắc chắn muốn xoá đợt từ <strong>{formatDate(selectedPeriod?.startDate)}</strong>?</p>
              {error && <p className="sd-status sd-status-error">{error}</p>}
            </div>
            <div className="sd-modal-footer">
              <button className="sd-btn-ghost" onClick={() => setModal(null)}>Huỷ</button>
              <button className="sd-btn-primary btn-danger" disabled={saving} onClick={handleDelete}>Xoá ngay</button>
            </div>
          </div>
        </div>
      )}
    </div>
  )
}

function PeriodReviewScreen({ period, onBack, user }) {
  const [registrations, setRegistrations] = useState([])
  const [shifts, setShifts] = useState([])
  const [dates, setDates] = useState([])
  const [loading, setLoading] = useState(true)
  const [draftApproved, setDraftApproved] = useState(new Set())
  const [activeSwapId, setActiveSwapId] = useState(null)
  // ✅ FIX CHÍNH: Lưu status vào state riêng, không đọc từ prop
  const [currentStatus, setCurrentStatus] = useState((period?.status || 'OPEN').toUpperCase())

  useEffect(() => {
    // Khi period prop thay đổi (lần đầu vào), sync lại status
    setCurrentStatus((period?.status || 'OPEN').toUpperCase())

    async function loadBoardData() {
      setLoading(true)
      try {
        
        const [regRes, shiftRes] = await Promise.all([
          axios.get(`/api/StaffRegistration/period/${period.id}`),
          getAllShifts()
        ])
        const allRegs = regRes.data || []
        const branchShifts = shiftRes.filter((s) => s.branchId === period.branchId)
        setRegistrations(allRegs)
        setShifts(branchShifts)

        const dArray = []
        let curr = new Date(period.startDate)
        const end = new Date(period.endDate)
        while (curr <= end) { dArray.push(new Date(curr)); curr.setDate(curr.getDate() + 1) }
        setDates(dArray)

        const newDraft = new Set()
        if (period.status === 'PUBLISHED') {
          allRegs.forEach(r => { if (r.status === 'Đã Duyệt') newDraft.add(r.id) })
        } else {
          const grouped = {}
          allRegs.forEach((r) => {
            const key = r.workDate.slice(0, 10) + '_' + r.shiftId
            if (!grouped[key]) grouped[key] = []
            grouped[key].push(r)
          })
          Object.keys(grouped).forEach((key) => {
            const shiftId = parseInt(key.split('_')[1])
            const shift = branchShifts.find((s) => s.id === shiftId)
            const max = shift?.maxStaff || 0
            const allowedStaff = max > 0 ? max - 1 : 999
            const sorted = grouped[key]
            for (let i = 0; i < Math.min(allowedStaff, sorted.length); i++) newDraft.add(sorted[i].id)
          })
        }
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
  dates.forEach((dObj) => {
    const dStr = toDateString(dObj)
    boardMatrix[dStr] = {}
    shifts.forEach((s) => {
      boardMatrix[dStr][s.id] = registrations.filter((r) => r.workDate.slice(0, 10) === dStr && r.shiftId === s.id)
    })
  })

const handleLockPeriod = async () => {
  if (!window.confirm('Bạn có chắc muốn KHÓA SỔ đợt này?')) return
  try {
    // ✅ Đổi từ updatePeriod(...) sang axios.patch
    await axios.patch(`/api/SchedulePeriod/${period.id}/status`, { status: 'REVIEWING' })
    alert('⏸️ Đã khóa sổ đăng ký thành công!')
    setCurrentStatus('REVIEWING')
  } catch (error) {
    alert('Lỗi khi khóa sổ')
  }
}

 const handleReopenPeriod = async () => {
  if (!window.confirm('Bạn muốn MỞ LẠI đợt này?')) return
  try {
    // ✅ Đổi từ updatePeriod(...) sang axios.patch
    await axios.patch(`/api/SchedulePeriod/${period.id}/status`, { status: 'OPEN' })
    alert('▶️ Đã mở lại đợt đăng ký!')
    setCurrentStatus('OPEN')
  } catch (error) {
    alert('Lỗi khi mở lại đợt')
  }
}

  const handlePublish = async () => {
    if (!window.confirm('Bạn có chắc chắn muốn CHỐT LỊCH?')) return
    try {
      const payload = { periodId: period.id, approvedRegistrationIds: Array.from(draftApproved) }
      await axios.post('/api/StaffRegistration/publish', payload)
      alert('✅ Đã chốt/cập nhật lịch làm việc thành công!')
      setCurrentStatus('PUBLISHED')
      onBack()
    } catch (error) { alert('Lỗi chốt lịch') }
  }

  // ✅ FIX: Đọc từ state local, không phải prop
  const isOpen = currentStatus === 'MỞ' || currentStatus === 'OPEN'
  const isReviewing = currentStatus === 'REVIEWING' || currentStatus === 'ĐANG DUYỆT' || currentStatus === 'ĐÓNG' || currentStatus === 'CLOSED'
  const isPublished = currentStatus === 'PUBLISHED'

  return (
    <div className="sd-users-page">
      <button className="sd-btn-back" onClick={onBack}>← Quay lại danh sách đợt</button>

      <div className="sd-publish-banner" style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', flexWrap: 'wrap', gap: '10px' }}>
        <div>
          <h2 style={{ margin: '0 0 4px', fontSize: 18 }}>Bảng xếp lịch: Từ {formatDate(period.startDate)} đến {formatDate(period.endDate)}</h2>
          <p style={{ margin: 0, fontSize: 14, color: '#64748b' }}>
            Trạng thái hiện tại: <strong style={{ color: '#ea580c' }}>{currentStatus}</strong>
          </p>
        </div>

        <div style={{ display: 'flex', gap: '10px' }}>
          {isOpen && (
            <button
              style={{ padding: '8px 16px', background: '#fef08a', color: '#854d0e', border: '1px solid #fde047', borderRadius: '6px', fontWeight: '600', cursor: 'pointer' }}
              onClick={handleLockPeriod}
            >
              ⏸️ Khóa Đăng Ký
            </button>
          )}

          {isReviewing && (
            <button
              style={{ padding: '8px 16px', background: '#dcfce7', color: '#166534', border: '1px solid #bbf7d0', borderRadius: '6px', fontWeight: '600', cursor: 'pointer' }}
              onClick={handleReopenPeriod}
            >
              ▶️ Mở Lại Đăng Ký
            </button>
          )}

          <button
            className="sd-btn-primary"
            style={{ width: 'auto', marginTop: 0, background: isPublished ? '#0284c7' : '#ea580c' }}
            onClick={handlePublish}
          >
            {isPublished ? '🔄 Cập nhật thay đổi' : '🔒 Chốt Lịch chính thức'}
          </button>
        </div>
      </div>

      {loading ? <p>Đang tải...</p> : (
        <div className="sd-board-wrap">
          <table className="sd-schedule-board">
            <thead>
              <tr>
                <th style={{ width: 90 }}>NGÀY</th>
                {shifts.map((s) => (
                  <th key={s.id}>{s.shiftName}<br />
                    <span style={{ fontWeight: 500, fontSize: 11 }}>{s.startTime?.slice(0, 5)} - {s.endTime?.slice(0, 5)}</span>
                  </th>
                ))}
              </tr>
            </thead>
            <tbody>
              {dates.map((dateObj) => {
                const dStr = toDateString(dateObj)
                const dayOfWeek = DAY_NAMES[dateObj.getDay()]
                return (
                  <tr key={dStr}>
                    <td className="sd-board-date-col">
                      <strong>{dayOfWeek}</strong>
                      <small>{dateObj.getDate()}/{dateObj.getMonth() + 1}</small>
                    </td>
                    {shifts.map((shift) => {
                      const cellRegs = boardMatrix[dStr][shift.id] || []
                      const max = shift.maxStaff || 0
                      const allowedStaff = max > 0 ? max - 1 : 0
                      const assignedRegs = cellRegs.filter((r) => draftApproved.has(r.id))
                      const backupRegs = cellRegs.filter((r) => !draftApproved.has(r.id))
                      const slots = []
                      if (max > 0) { for (let i = 0; i < allowedStaff; i++) slots.push(assignedRegs[i] || null) }
                      else { slots.push(...assignedRegs) }
                      const isWeekend = dayOfWeek === 'Thứ 7' || dayOfWeek === 'Chủ nhật'
                      const isShiftClosed = isWeekend && cellRegs.length === 0

                      return (
                        <td key={shift.id}>
                          {!isShiftClosed ? (
                            <div className="sd-reg-card" style={{ background: '#ffedd5', borderColor: '#fdba74', color: '#9a3412', cursor: 'default' }}>
                              <span className="sd-reg-name">{user.fullName || user.username}</span>
                              <span style={{ fontSize: 10, fontWeight: 'bold' }}>Quản lý</span>
                            </div>
                          ) : (
                            <div style={{ textAlign: 'center', padding: '16px 0', color: '#cbd5e1', fontSize: 12, fontWeight: 600 }}>CA NGHỈ</div>
                          )}

                          {slots.map((r, idx) => {
                            if (!r) {
                              const emptyId = `empty_${dStr}_${shift.id}_${idx}`
                              return (
                                <div key={emptyId} style={{ position: 'relative' }}>
                                  <div className="sd-reg-card" style={{ background: '#f8fafc', borderColor: '#e2e8f0', color: '#94a3b8', borderStyle: 'dashed', cursor: 'pointer' }}
                                    onClick={() => setActiveSwapId(activeSwapId === emptyId ? null : emptyId)}>
                                    <span>+ Thêm NV</span>
                                  </div>
                                  {activeSwapId === emptyId && (
                                    <div className="sd-swap-dropdown">
                                      {backupRegs.map((backup) => (
                                        <div key={backup.id} className="sd-swap-item"
                                          onClick={() => { const next = new Set(draftApproved); next.add(backup.id); setDraftApproved(next); setActiveSwapId(null) }}>
                                          {backup.user?.fullName}
                                        </div>
                                      ))}
                                    </div>
                                  )}
                                </div>
                              )
                            }
                            return (
                              <div key={r.id} style={{ position: 'relative' }}>
                                <div className="sd-reg-card" style={{ background: '#dcfce7', borderColor: '#bbf7d0', color: '#166534', cursor: 'pointer' }}
                                  onClick={() => setActiveSwapId(activeSwapId === r.id ? null : r.id)}>
                                  <span className="sd-reg-name">{r.user?.fullName}</span>
                                </div>
                                {activeSwapId === r.id && (
                                  <div className="sd-swap-dropdown">
                                    {backupRegs.map((backup) => (
                                      <div key={backup.id} className="sd-swap-item"
                                        onClick={() => { const next = new Set(draftApproved); next.delete(r.id); next.add(backup.id); setDraftApproved(next); setActiveSwapId(null) }}>
                                        {backup.user?.fullName}
                                      </div>
                                    ))}
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
