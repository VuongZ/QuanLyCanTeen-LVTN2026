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
  const [form, setForm] = useState({ startDate: '', endDate: '', status: 'OPEN' })

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

  function openAdd() {
    setForm({ startDate: '', endDate: '', status: 'OPEN' })
    setError('')
    setModal('add')
  }
  function openEdit(p) {
    setForm({
      id: p.id,
      startDate: p.startDate?.slice(0, 10) || '',
      endDate: p.endDate?.slice(0, 10) || '',
      status: p.status || 'OPEN'
    })
    setError('')
    setModal('edit')
  }
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
              const st = String(p.status || '').toUpperCase()

              const isOpen = st === 'OPEN'

              const isClosed =
                st === 'CLOSED' ||
                st === 'REVIEWING' ||
                st === 'DRAFT'

              const isPublished = st === 'PUBLISHED'
              return (
                // ✅ FIX: setReviewingPeriodId(p.id) thay vì setReviewingPeriod(p)
                <tr key={p.id} className="sd-tr" style={{ cursor: 'pointer' }} onClick={() => setReviewingPeriodId(p.id)}>
                  <td className="sd-td sd-td-name-col">
                    <strong style={{ color: '#1e293b' }}>Từ {formatDate(p.startDate)} đến {formatDate(p.endDate)}</strong>
                  </td>
                  <td className="sd-td sd-text-center sd-td-info-col">
                    {isPublished ? (
                      <span
                        className="sd-status-pill"
                        style={{
                          background: '#e0e7ff',
                          color: '#1d4ed8',
                          borderColor: '#bfdbfe'
                        }}
                      >
                        Đã công bố
                      </span>
                    ) : isOpen ? (
                      <span className="sd-status-pill sd-status-pill--open">
                        Đang mở
                      </span>
                    ) : (
                      <span className="sd-status-pill sd-status-pill--closed">
                        Đã đóng
                      </span>
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
                <select
                  value={form.status}
                  onChange={(e) => setForm({ ...form, status: e.target.value })}
                >
                  <option value="OPEN">
                    Mở đăng ký
                  </option>

                  <option value="CLOSED">
                    Đóng đăng ký
                  </option>
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
  const [currentStatus, setCurrentStatus] = useState((period?.status || 'OPEN').toUpperCase())

  useEffect(() => {
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

        while (curr <= end) {
          dArray.push(new Date(curr))
          curr.setDate(curr.getDate() + 1)
        }

        setDates(dArray)
      } catch (error) {
        console.error(error)
      } finally {
        setLoading(false)
      }
    }

    loadBoardData()
  }, [period])

  function toDateString(dateObj) {
    const offset = dateObj.getTimezoneOffset()
    const d = new Date(dateObj.getTime() - offset * 60 * 1000)

    return d.toISOString().split('T')[0]
  }

  function isActiveRegistration(reg) {
    const status = reg.status || ''

    return ![
      'CANCELLED',
      'Từ Chối',
      'REJECTED',
      'Tá»« Chá»‘i'
    ].includes(status)
  }

  function getRegistrationStatusLabel(status) {
    if (
      status === 'REGISTERED' ||
      status === 'Chờ Duyệt' ||
      status === 'Đã Duyệt' ||
      status === 'APPROVED'
    ) {
      return 'Đã đăng ký'
    }

    if (status === 'CANCELLED') {
      return 'Đã hủy'
    }

    return status || 'Đã đăng ký'
  }

  const activeRegistrations = registrations.filter(isActiveRegistration)

  const boardMatrix = {}

  dates.forEach((dObj) => {
    const dStr = toDateString(dObj)

    boardMatrix[dStr] = {}

    shifts.forEach((s) => {
      boardMatrix[dStr][s.id] = activeRegistrations.filter((r) => {
        return (
          r.workDate?.slice(0, 10) === dStr &&
          r.shiftId === s.id
        )
      })
    })
  })

  const handleLockPeriod = async () => {
    if (!window.confirm('Bạn có chắc muốn khóa đăng ký đợt này?')) return

    try {
      await axios.patch(`/api/SchedulePeriod/${period.id}/status`, {
        status: 'CLOSED'
      })

      alert('Đã khóa đăng ký thành công!')
      setCurrentStatus('CLOSED')
    } catch (error) {
      alert(error?.response?.data?.message || 'Lỗi khi khóa đăng ký')
    }
  }

  const handleReopenPeriod = async () => {
    if (!window.confirm('Bạn muốn mở lại đợt đăng ký này?')) return

    try {
      await axios.patch(`/api/SchedulePeriod/${period.id}/status`, {
        status: 'OPEN'
      })

      alert('Đã mở lại đợt đăng ký!')
      setCurrentStatus('OPEN')
    } catch (error) {
      alert(error?.response?.data?.message || 'Lỗi khi mở lại đợt')
    }
  }

  const handlePublish = async () => {
    if (!window.confirm('Bạn có chắc chắn muốn công bố lịch làm?')) return

    try {
      const payload = {
        periodId: period.id,
        approvedRegistrationIds: []
      }

      await axios.post('/api/StaffRegistration/publish', payload)

      alert('Đã công bố lịch làm việc thành công!')
      setCurrentStatus('PUBLISHED')
      onBack()
    } catch (error) {
      alert(error?.response?.data?.message || 'Lỗi công bố lịch')
    }
  }

  const isOpen = currentStatus === 'OPEN'
  const isClosed = currentStatus === 'CLOSED'
  const isPublished = currentStatus === 'PUBLISHED'

  return (
    <div className="sd-users-page">
      <button
        className="sd-btn-back"
        onClick={onBack}
      >
        ← Quay lại danh sách đợt
      </button>

      <div
        className="sd-publish-banner"
        style={{
          display: 'flex',
          justifyContent: 'space-between',
          alignItems: 'center',
          flexWrap: 'wrap',
          gap: '10px'
        }}
      >
        <div>
          <h2
            style={{
              margin: '0 0 4px',
              fontSize: 18
            }}
          >
            Lịch đăng ký: Từ {formatDate(period.startDate)} đến {formatDate(period.endDate)}
          </h2>

          <p
            style={{
              margin: 0,
              fontSize: 14,
              color: '#64748b'
            }}
          >
            Trạng thái hiện tại:{' '}

            <strong style={{ color: '#ea580c' }}>
              {isOpen
                ? 'Đang mở'
                : isClosed
                  ? 'Đã đóng'
                  : isPublished
                    ? 'Đã công bố'
                    : currentStatus}
            </strong>
          </p>
        </div>

        <div
          style={{
            display: 'flex',
            gap: '10px'
          }}
        >
          {isOpen && (
            <button
              style={{
                padding: '8px 16px',
                background: '#fef08a',
                color: '#854d0e',
                border: '1px solid #fde047',
                borderRadius: '6px',
                fontWeight: '600',
                cursor: 'pointer'
              }}
              onClick={handleLockPeriod}
            >
              Khóa đăng ký
            </button>
          )}

          {isClosed && (
            <button
              style={{
                padding: '8px 16px',
                background: '#dcfce7',
                color: '#166534',
                border: '1px solid #bbf7d0',
                borderRadius: '6px',
                fontWeight: '600',
                cursor: 'pointer'
              }}
              onClick={handleReopenPeriod}
            >
              Mở lại đăng ký
            </button>
          )}

          {isClosed && (
  <button
    className="sd-btn-primary"
    style={{
      width: 'auto',
      marginTop: 0,
      background: '#ea580c',
      cursor: 'pointer'
    }}
    onClick={handlePublish}
  >
    Công bố lịch
  </button>
)}

{isPublished && (
  <span
    style={{
      padding: '8px 16px',
      background: '#e0e7ff',
      color: '#1d4ed8',
      border: '1px solid #bfdbfe',
      borderRadius: '6px',
      fontWeight: '600'
    }}
  >
    Đã công bố lịch
  </span>
)}
        </div>
      </div>

      {loading ? (
        <p>Đang tải...</p>
      ) : (
        <div className="sd-board-wrap">
          <table className="sd-schedule-board">
            <thead>
              <tr>
                <th style={{ width: 90 }}>
                  NGÀY
                </th>

                {shifts.map((s) => (
                  <th key={s.id}>
                    {s.shiftName}

                    <br />

                    <span
                      style={{
                        fontWeight: 500,
                        fontSize: 11
                      }}
                    >
                      {s.startTime?.slice(0, 5)} - {s.endTime?.slice(0, 5)}
                    </span>
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
                      <strong>
                        {dayOfWeek}
                      </strong>

                      <small>
                        {dateObj.getDate()}/{dateObj.getMonth() + 1}
                      </small>
                    </td>

                    {shifts.map((shift) => {
                      const cellRegs = boardMatrix[dStr][shift.id] || []
                      const max = shift.maxStaff || 0
                      const staffSlot = Math.max(max - 1, 0)
                      const remainingSlots = Math.max(staffSlot - cellRegs.length, 0)
                      const isFull = staffSlot > 0 && remainingSlots === 0

                      return (
                        <td
                          key={shift.id}
                          className={`sd-schedule-cell ${isFull ? 'is-full' : ''}`}
                        >
                          {max <= 0 ? (
                            <div className="sd-schedule-closed">
                              Không có ca làm
                            </div>
                          ) : (
                            <div className="sd-slot-list">
                              <div className="sd-slot-person sd-slot-manager">
                                <span className="sd-slot-name">
                                  {user.fullName || user.username || 'Quản lý'}
                                </span>

                                <span className="sd-slot-role">
                                  Quản lý
                                </span>
                              </div>

                              {cellRegs.map((r) => (
                                <div
                                  key={r.id}
                                  className="sd-slot-person sd-slot-staff"
                                >
                                  <span className="sd-slot-name">
                                    {r.user?.fullName || 'Nhân viên'}
                                  </span>

                                  <span className="sd-slot-role">
                                    {getRegistrationStatusLabel(r.status)}
                                  </span>
                                </div>
                              ))}

                              {Array.from({ length: remainingSlots }).map((_, index) => (
                                <div
                                  key={`empty-${dStr}-${shift.id}-${index}`}
                                  className="sd-slot-empty"
                                >
                                  Còn trống
                                </div>
                              ))}



                              {isFull && (
                                <div className="sd-slot-full-text">
                                  Ca đã đủ người
                                </div>
                              )}
                            </div>
                          )}
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
