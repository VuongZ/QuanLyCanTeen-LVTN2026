import { useState, useEffect } from 'react'
import {
  getAllPeriods,
  createPeriod,
  updatePeriod,
  updatePeriodStatus,
  deletePeriod
} from '../../api/PeriodApi'

import {
  getRegistrationsByPeriod,
  getFinalScheduleByPeriod,
  publishSchedule
} from '../../api/StaffRegistrationApi'

import { getAllShifts } from '../../api/ShiftApi'

const DAY_NAMES = ['Chủ nhật', 'Thứ 2', 'Thứ 3', 'Thứ 4', 'Thứ 5', 'Thứ 6', 'Thứ 7']

function getApiErrorMessage(error, fallbackMessage) {
  const responseData = error?.response?.data

  if (typeof responseData === 'string' && responseData.trim()) {
    return responseData
  }

  return responseData?.message || fallbackMessage
}

function getVietnamDateString(date = new Date()) {
  const parts = new Intl.DateTimeFormat('en-US', {
    timeZone: 'Asia/Ho_Chi_Minh',
    year: 'numeric',
    month: '2-digit',
    day: '2-digit'
  }).formatToParts(date)

  const values = Object.fromEntries(parts.map((part) => [part.type, part.value]))

  return `${values.year}-${values.month}-${values.day}`
}

function addDaysToDateString(dateString, days) {
  const [year, month, day] = dateString.split('-').map(Number)
  const date = new Date(Date.UTC(year, month - 1, day))

  date.setUTCDate(date.getUTCDate() + days)

  return date.toISOString().slice(0, 10)
}

function hasPeriodStarted(startDate) {
  const normalizedStartDate = String(startDate || '').slice(0, 10)

  return Boolean(normalizedStartDate) &&
    normalizedStartDate <= getVietnamDateString()
}

function formatDate(value) {
  if (!value) return '—'

  const normalizedDate = String(value).slice(0, 10)
  const [year, month, day] = normalizedDate.split('-').map(Number)

  if (!year || !month || !day) return '—'

  return new Intl.DateTimeFormat('vi-VN').format(
    new Date(year, month - 1, day)
  )
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

  useEffect(() => {
    loadPeriods()

    const intervalId = window.setInterval(loadPeriods, 10000)

    return () => window.clearInterval(intervalId)
  }, [user.branchId])

  async function loadPeriods() {
    try {
      const data = await getAllPeriods()
      setPeriods(Array.isArray(data) ? data : [])
    } catch (err) {
      console.error('Lỗi tải danh sách đợt đăng ký:', err)
    }
  }

  function handleStartDateChange(e) {
    const selectedDateStr = e.target.value

    if (!selectedDateStr) {
      setForm((previous) => ({
        ...previous,
        startDate: '',
        endDate: ''
      }))
      return
    }

    const endDateStr = addDaysToDateString(selectedDateStr, 6)

    setForm((previous) => ({
      ...previous,
      startDate: selectedDateStr,
      endDate: endDateStr
    }))
  }

  const minimumStartDate = addDaysToDateString(
    getVietnamDateString(),
    1
  )

  const filteredPeriods = periods
    .filter((p) => {
      const matchBranch = String(p.branchId) === String(user.branchId)
      const dateRangeStr = `${formatDate(p.startDate)} ${formatDate(p.endDate)}`.toLowerCase()
      return matchBranch && dateRangeStr.includes(search.toLowerCase())
    })
    .sort((a, b) => new Date(b.startDate) - new Date(a.startDate))

  function openAdd() {
    setForm({ startDate: '', endDate: '', status: 'OPEN' })
    setError('')
    setModal('add')
  }
  function openEdit(period) {
    const status = String(period.status || '').toUpperCase()

    if (status === 'PUBLISHED') {
      setError('Không thể chỉnh sửa đợt đăng ký đã được công bố.')
      return
    }

    if (hasPeriodStarted(period.startDate)) {
      setError('Không thể chỉnh sửa đợt đăng ký đã đến ngày bắt đầu.')
      return
    }

    setForm({
      id: period.id,
      startDate: period.startDate?.slice(0, 10) || '',
      endDate: period.endDate?.slice(0, 10) || '',
      status: period.status || 'OPEN'
    })
    setError('')
    setModal('edit')
  }

  function openDelete(period) {
    const status = String(period.status || '').toUpperCase()

    if (status === 'PUBLISHED') {
      setError('Không thể xóa đợt đăng ký đã được công bố.')
      return
    }

    setSelectedPeriod(period)
    setError('')
    setModal('delete')
  }

  async function handleSave() {
    if (!form.startDate || !form.endDate) {
      setError('Vui lòng chọn ngày bắt đầu đợt đăng ký.')
      return
    }

    if (form.startDate < minimumStartDate) {
      setError('Ngày bắt đầu phải lớn hơn ngày hiện tại.')
      return
    }

    const startDate = new Date(`${form.startDate}T00:00:00`)

    if (startDate.getDay() !== 1) {
      setError('Ngày bắt đầu đợt đăng ký bắt buộc phải là Thứ Hai.')
      return
    }

    setSaving(true)
    setError('')

    try {
      const payload = {
        ...form,
        branchId: user.branchId
      }

      if (modal === 'add') {
        await createPeriod(payload)
      } else {
        await updatePeriod(form.id, payload)
      }

      await loadPeriods()
      setModal(null)
    } catch (err) {
      setError(getApiErrorMessage(err, 'Không thể lưu đợt đăng ký.'))
    } finally {
      setSaving(false)
    }
  }

  async function handleDelete() {
    if (!selectedPeriod) return

    setSaving(true)
    setError('')

    try {
      await deletePeriod(selectedPeriod.id)
      await loadPeriods()
      setModal(null)
    } catch (err) {
      setError(
        getApiErrorMessage(
          err,
          'Không thể xóa đợt đăng ký này.'
        )
      )
    } finally {
      setSaving(false)
    }
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

      {error && !modal && (
        <p className="sd-status sd-status-error">
          {error}
        </p>
      )}

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
            {filteredPeriods.map((period) => {
              const status = String(period.status || '').toUpperCase()
              const isOpen = status === 'OPEN'
              const isPublished = status === 'PUBLISHED'
              const isOverdue =
                !isPublished &&
                hasPeriodStarted(period.startDate)

              const cannotEdit =
                isPublished ||
                hasPeriodStarted(period.startDate)

              return (
                <tr
                  key={period.id}
                  className="sd-tr"
                  style={{ cursor: 'pointer' }}
                  onClick={() => setReviewingPeriodId(period.id)}
                >
                  <td className="sd-td sd-td-name-col">
                    <strong style={{ color: '#1e293b' }}>
                      Từ {formatDate(period.startDate)} đến {formatDate(period.endDate)}
                    </strong>
                  </td>

                  <td className="sd-td sd-text-center sd-td-info-col">
                    {isPublished ? (
                      <span className="sd-status-pill sd-status-pill--published">
                        Đã công bố
                      </span>
                    ) : isOverdue ? (
                      <span className="sd-status-pill sd-status-pill--overdue">
                        Quá hạn - Chưa công bố
                      </span>
                    ) : isOpen ? (
                      <span className="sd-status-pill sd-status-pill--open">
                        Đang mở
                      </span>
                    ) : (
                      <span className="sd-status-pill sd-status-pill--closed">
                        Đã khóa
                      </span>
                    )}
                  </td>

                  <td
                    className="sd-td sd-text-right"
                    style={{ whiteSpace: 'nowrap' }}
                  >
                    <button
                      className="sd-action-btn sd-action-edit"
                      title={
                        cannotEdit
                          ? 'Đợt đã bắt đầu hoặc đã công bố nên không thể chỉnh sửa'
                          : 'Chỉnh sửa đợt đăng ký'
                      }
                      disabled={cannotEdit}
                      onClick={(event) => {
                        event.stopPropagation()
                        openEdit(period)
                      }}
                    >
                      ✎
                    </button>

                    <button
                      className="sd-action-btn sd-action-delete"
                      title={
                        isPublished
                          ? 'Không thể xóa đợt đã công bố'
                          : 'Xóa đợt đăng ký'
                      }
                      disabled={isPublished}
                      onClick={(event) => {
                        event.stopPropagation()
                        openDelete(period)
                      }}
                    >
                      ✕
                    </button>
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
                  <input type="date" min={minimumStartDate} value={form.startDate} onChange={handleStartDateChange} />
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
                    Khóa đăng ký
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
  const [reviewError, setReviewError] = useState('')
  const [currentStatus, setCurrentStatus] = useState(
    (period?.status || 'OPEN').toUpperCase()
  )

  useEffect(() => {
    setCurrentStatus(
      String(period?.status || 'OPEN').toUpperCase()
    )
  }, [period?.status])

  useEffect(() => {
    async function loadBoardData() {
      setLoading(true)

      try {
        const isPublishedPeriod =
          String(currentStatus || period.status || '').toUpperCase() === 'PUBLISHED'

        const schedulePromise = isPublishedPeriod
  ? getFinalScheduleByPeriod(period.id)
  : getRegistrationsByPeriod(period.id)

const [scheduleRows, shiftRows] = await Promise.all([
  schedulePromise,
  getAllShifts()
])

const branchShifts = (shiftRows || []).filter(
  (shift) =>
    String(shift.branchId) === String(period.branchId)
)

setRegistrations(
  Array.isArray(scheduleRows) ? scheduleRows : []
)

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
        setReviewError(
          getApiErrorMessage(
            error,
            'Không thể tải dữ liệu đăng ký ca.'
          )
        )
      } finally {
        setLoading(false)
      }
    }

    loadBoardData()
  }, [
    period.id,
    period.branchId,
    period.startDate,
    period.endDate,
    currentStatus
  ])

  function toDateString(dateObj) {
    const offset = dateObj.getTimezoneOffset()
    const d = new Date(dateObj.getTime() - offset * 60 * 1000)

    return d.toISOString().split('T')[0]
  }

  function isActiveRegistration(reg) {
    const status = String(reg.status || '').toUpperCase()

    return ![
      'CANCELLED',
      'TỪ CHỐI',
      'REJECTED'
    ].includes(status)
  }

  function isManagerRow(row) {
    const roleName = String(row?.user?.roleName || '')
      .normalize('NFD')
      .replace(/[\u0300-\u036f]/g, '')
      .toUpperCase()

    return roleName.includes('MANAGER') || roleName.includes('QUAN LY')
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
  if (!window.confirm('Bạn có chắc muốn khóa đăng ký đợt này?')) {
    return
  }

  try {
    await updatePeriodStatus(period.id, 'CLOSED')

    alert('Đã khóa đăng ký thành công!')
    setCurrentStatus('CLOSED')
  } catch (error) {
    alert(
      getApiErrorMessage(
        error,
        'Không thể khóa đợt đăng ký.'
      )
    )
  }
}

 const handleReopenPeriod = async () => {
  if (hasPeriodStarted(period.startDate)) {
    alert(
      'Không thể mở lại đợt đăng ký khi đã đến ngày bắt đầu lịch làm.'
    )
    return
  }

  if (!window.confirm('Bạn muốn mở lại đợt đăng ký này?')) {
    return
  }

  try {
    await updatePeriodStatus(period.id, 'OPEN')

    alert('Đã mở lại đợt đăng ký!')
    setCurrentStatus('OPEN')
  } catch (error) {
    alert(
      getApiErrorMessage(
        error,
        'Không thể mở lại đợt đăng ký.'
      )
    )
  }
}

 const handlePublish = async () => {
  if (!window.confirm('Bạn có chắc chắn muốn công bố lịch làm?')) {
    return
  }

  try {
    await publishSchedule(period.id)

    alert('Đã công bố lịch làm việc thành công!')
    setCurrentStatus('PUBLISHED')
    await onBack()
  } catch (error) {
    alert(
      getApiErrorMessage(
        error,
        'Không thể công bố lịch.'
      )
    )
  }
}

  const isOpen = currentStatus === 'OPEN'
  const isClosed = [
    'CLOSED',
    'REVIEWING',
    'DRAFT'
  ].includes(currentStatus)
  const isPublished = currentStatus === 'PUBLISHED'
  const isOverdue =
    !isPublished &&
    hasPeriodStarted(period.startDate)
  const canReopen = isClosed && !isOverdue

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
              {isPublished
                ? 'Đã công bố'
                : isOverdue
                  ? 'Quá hạn - Chưa công bố'
                  : isOpen
                    ? 'Đang mở'
                    : isClosed
                      ? 'Đã khóa'
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

          {canReopen && (
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

      {isOverdue && !isPublished && (
        <div className="sd-period-message sd-period-message--overdue">
          <strong>Lịch làm đã đến ngày bắt đầu nhưng chưa được công bố.</strong>
          {' '}Đợt đăng ký không thể mở lại; Quản lý cần kiểm tra và công bố lịch.
        </div>
      )}

      {reviewError && (
        <p className="sd-status sd-status-error">
          {reviewError}
        </p>
      )}

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
                      const max = Number(shift.maxStaff || 0)

                      // Trước công bố: danh sách chỉ gồm Staff đăng ký, Manager được hiển thị riêng.
                      // Sau công bố: cellRegs lấy trực tiếp từ ca_final_schedule và đã gồm cả Manager.
                      const occupiedCount = isPublished
                        ? cellRegs.length
                        : cellRegs.length + (max > 0 ? 1 : 0)

                      const remainingSlots = Math.max(max - occupiedCount, 0)
                      const isFull = max > 0 && remainingSlots === 0

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
                              {!isPublished && (
                                <div className="sd-slot-person sd-slot-manager">
                                  <span className="sd-slot-name">
                                    {user.fullName || user.username || 'Quản lý'}
                                  </span>

                                  <span className="sd-slot-role">
                                    Quản lý
                                  </span>
                                </div>
                              )}

                              {cellRegs.map((row) => {
                                const managerRow = isPublished && isManagerRow(row)

                                return (
                                  <div
                                    key={row.id}
                                    className={`sd-slot-person ${
                                      managerRow
                                        ? 'sd-slot-manager'
                                        : 'sd-slot-staff'
                                    }`}
                                  >
                                    <span className="sd-slot-name">
                                      {row.user?.fullName || row.user?.email || 'Nhân viên'}
                                    </span>

                                    <span className="sd-slot-role">
                                      {isPublished
                                        ? managerRow
                                          ? 'Quản lý'
                                          : 'Lịch chính thức'
                                        : getRegistrationStatusLabel(row.status)}
                                    </span>
                                  </div>
                                )
                              })}

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