import { useState, useEffect } from 'react'
import axios from 'axios'
import {
  getAllPeriods,
  createPeriod,
  updatePeriod,
  updatePeriodStatus,
  deletePeriod
} from '../../api/PeriodApi'

// Danh sách ca làm dùng để tạo các cột của bảng lịch.
import { getAllShifts } from '../../api/ShiftApi'

// Phiếu đăng ký ca.
import {
  getRegistrationsByPeriod
} from '../../api/StaffRegistrationApi'

// Lịch chính thức và nghiệp vụ thay ca.
import {
  getFinalScheduleByPeriod,
  getAutomaticFullTimeStaff,
  publishSchedule,
  markApprovedLeave,
  markAbsent,
  getReplacementCandidates,
  assignEmergencyReplacement
} from '../../api/FinalScheduleApi'


// CSS riêng của hai tab lịch/đợt, không còn đặt trong CSS dashboard lớn.
import '../css/ScheduleTabs.css'
// Ánh xạ chỉ số getDay() sang tên thứ tiếng Việt.
const DAY_NAMES = [
  'Chủ nhật',
  'Thứ 2',
  'Thứ 3',
  'Thứ 4',
  'Thứ 5',
  'Thứ 6',
  'Thứ 7'
]

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

  // Tải danh sách đợt ngay khi component mở
  // và tự tải lại sau mỗi 10 giây.
  useEffect(() => {
    loadPeriods()

    const intervalId = window.setInterval(loadPeriods, 10000)

    return () => window.clearInterval(intervalId)
  }, [user.branchId])

  // Lấy toàn bộ đợt đăng ký từ Backend.
  async function loadPeriods() {
    try {
      const data = await getAllPeriods()
      setPeriods(Array.isArray(data) ? data : [])
    } catch (err) {
      console.error('Lỗi tải danh sách đợt đăng ký:', err)
    }
  }

  // Khi chọn ngày bắt đầu,
  // tự động tính ngày kết thúc = ngày bắt đầu + 6 ngày.
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

  // Lọc các đợt thuộc đúng chi nhánh và khớp nội dung tìm kiếm.
  // Sau đó sắp xếp tuần mới nhất lên trước.
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
    <div className="sd-users-page schedule-tabs schedule-tabs--manager">
      <div className="sd-users-toolbar">
        <div className="sd-users-toolbar-left">
          <div className="sd-search-wrap">
            <span className="sd-search-icon">⌕</span>
            <input
              className="sd-input-search"
              placeholder="Tìm theo ngày..."
              value={search}
              onChange={(event) => setSearch(event.target.value)}
            />
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
                <div className="sd-empty-state">
                  <span className="sd-empty-icon">
                    📅
                  </span>

                  <p>
                    Chưa có đợt đăng ký lịch làm nào
                  </p>
                </div>
              </td></tr>
            )}
            {/* filteredPeriods.map:
                mỗi đợt đăng ký tạo ra một hàng <tr>. */}
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
            <div className="sd-modal-header">
              <h2>
                {modal === 'add'
                  ? 'Mở đợt đăng ký mới'
                  : 'Chỉnh sửa'}
              </h2>

              <button onClick={() => setModal(null)}>
                ✕
              </button>
            </div>
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
            <div className="sd-modal-header">
              <h2>
                Xác nhận xoá đợt
              </h2>

              <button onClick={() => setModal(null)}>
                ✕
              </button>
            </div>
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

// ==========================================================
// MÀN HÌNH XEM VÀ DUYỆT LỊCH CỦA MỘT ĐỢT
// ==========================================================
function PeriodReviewScreen({ period, onBack, user }) {
  const [registrations, setRegistrations] = useState([])
  const [shifts, setShifts] = useState([])
  const [shiftConfigs, setShiftConfigs] = useState([])
  const [fullTimeStaff, setFullTimeStaff] = useState([])
  const [dates, setDates] = useState([])
  const [loading, setLoading] = useState(true)
  const [reviewError, setReviewError] = useState('')
  const [publishDialog, setPublishDialog] = useState(null)
  const [publishLoading, setPublishLoading] = useState(false)

  const [currentStatus, setCurrentStatus] = useState(
    String(period?.status || 'OPEN').toUpperCase()
  )

  // ========================================================================
  // STATE CỦA NGHIỆP VỤ THAY NHÂN VIÊN
  // ========================================================================

  // Tăng biến này để useEffect tải lại bảng sau mỗi thao tác thành công.
  const [boardReloadKey, setBoardReloadKey] = useState(0)

  // replacementModal có dạng:
  // {
  //   row: dòng lịch của Staff đang nghỉ/vắng,
  //   step: 'absence' | 'candidates'
  // }
  const [replacementModal, setReplacementModal] = useState(null)
  const [absenceReason, setAbsenceReason] = useState('')
  const [replacementCandidates, setReplacementCandidates] = useState([])
  const [selectedRegistrationId, setSelectedRegistrationId] = useState('')
  const [replacementLoading, setReplacementLoading] = useState(false)
  const [replacementError, setReplacementError] = useState('')

  // Đồng bộ trạng thái khi period bên ngoài thay đổi.
  useEffect(() => {
    setCurrentStatus(
      String(period?.status || 'OPEN').toUpperCase()
    )
  }, [period?.status])

  // ========================================================================
  // TẢI BẢNG ĐĂNG KÝ HOẶC LỊCH CHÍNH THỨC
  // ========================================================================
  useEffect(() => {
    let isMounted = true

    async function loadBoardData() {
      setLoading(true)
      setReviewError('')

      try {
        const isPublishedPeriod =
          String(
            currentStatus || period.status || ''
          ).toUpperCase() === 'PUBLISHED'

        const schedulePromise = isPublishedPeriod
          ? getFinalScheduleByPeriod(period.id)
          : getRegistrationsByPeriod(period.id)

        const [scheduleRows, shiftRows, automaticStaff, configRes] = await Promise.all([
          schedulePromise,
          getAllShifts(),
          getAutomaticFullTimeStaff(period.branchId),
          axios.get('/api/BranchShiftConfig')
        ])

        if (!isMounted) return

        const branchShifts = (shiftRows || []).filter((shift) => {
          return String(shift.branchId) === String(period.branchId)
        })

        setRegistrations(
          Array.isArray(scheduleRows)
            ? scheduleRows
            : []
        )
        setShifts(branchShifts)
        const branchShiftIds = new Set(
          branchShifts.map((shift) => Number(shift.id))
        )
        setShiftConfigs(
          (configRes.data || []).filter((config) => {
            return branchShiftIds.has(Number(config.shiftId))
          })
        )
        setFullTimeStaff(
          Array.isArray(automaticStaff)
            ? automaticStaff
            : []
        )

        const dateArray = []
        let currentDate = new Date(period.startDate)
        const endDate = new Date(period.endDate)

        while (currentDate <= endDate) {
          dateArray.push(new Date(currentDate))
          currentDate.setDate(currentDate.getDate() + 1)
        }

        setDates(dateArray)
      } catch (error) {
        console.error('Lỗi tải bảng lịch:', error)

        if (isMounted) {
          setReviewError(
            getApiErrorMessage(
              error,
              'Không thể tải dữ liệu đăng ký ca.'
            )
          )
        }
      } finally {
        if (isMounted) {
          setLoading(false)
        }
      }
    }

    loadBoardData()

    return () => {
      isMounted = false
    }
  }, [
    period.id,
    period.branchId,
    period.startDate,
    period.endDate,
    currentStatus,
    boardReloadKey
  ])

  function toDateString(dateObj) {
    const offset = dateObj.getTimezoneOffset()
    const normalizedDate = new Date(
      dateObj.getTime() - offset * 60 * 1000
    )

    return normalizedDate.toISOString().split('T')[0]
  }

  function normalizeStatus(value) {
    return String(value || '')
      .trim()
      .toUpperCase()
  }

  function getMaxStaffForShiftDate(shift, dateObj) {
    const dayName = dateObj.toLocaleDateString(
      'en-US',
      { weekday: 'long' }
    )

    const config = shiftConfigs.find((item) => {
      return Number(item.shiftId) === Number(shift.id) &&
        String(item.dayOfWeek).toLowerCase() === dayName.toLowerCase()
    })

    return Number(config?.maxStaff ?? shift.maxStaff ?? 0)
  }

  function isActiveRegistration(row) {
    const status = normalizeStatus(row?.status)

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

    return (
      roleName.includes('MANAGER') ||
      roleName.includes('QUAN LY')
    )
  }

  // REGISTERED mới chiếm slot trước khi công bố.
  function isOfficialRegistrationStatus(status) {
    const normalized = normalizeStatus(status)

    return [
      'REGISTERED',
      'APPROVED',
      'ĐÃ DUYỆT',
      'CHỜ DUYỆT'
    ].includes(normalized)
  }

  function getRegistrationStatusLabel(status) {
    const normalized = normalizeStatus(status)

    if (isOfficialRegistrationStatus(normalized)) {
      return 'Đã đăng ký chính thức'
    }

    if (normalized === 'WAITLIST') {
      return 'Danh sách chờ'
    }

    if (normalized === 'REPLACEMENT_SELECTED') {
      return 'Đã được chọn thay'
    }

    if (
      normalized === 'CANCELLED' ||
      normalized === 'REJECTED'
    ) {
      return 'Đã hủy'
    }

    return status || 'Không rõ trạng thái'
  }

  // Nhãn trạng thái sau khi lịch đã công bố.
  function getPublishedScheduleLabel(row, managerRow) {
    if (managerRow) return 'Quản lý'

    const status = normalizeStatus(row?.status)
    const assignmentType = normalizeStatus(row?.assignmentType)

    if (assignmentType === 'EMERGENCY_REPLACEMENT') {
      return 'Thay ca khẩn cấp'
    }

    if (status === 'LEAVE_APPROVED') {
      return 'Nghỉ có phép'
    }

    if (status === 'ABSENT') {
      return 'Vắng không phép'
    }

    return 'Lịch chính thức'
  }

  // Màu của từng loại lịch.
  function getScheduleRowStyle(row, managerRow, isPublished) {
    if (managerRow) return {}

    const status = normalizeStatus(row?.status)
    const assignmentType = normalizeStatus(row?.assignmentType)

    if (!isPublished && status === 'WAITLIST') {
      return {
        background: '#faf5ff',
        borderColor: '#c4b5fd',
        color: '#6d28d9'
      }
    }

    if (assignmentType === 'EMERGENCY_REPLACEMENT') {
      return {
        background: '#dcfce7',
        borderColor: '#86efac',
        color: '#166534'
      }
    }

    if (status === 'LEAVE_APPROVED') {
      return {
        background: '#fef3c7',
        borderColor: '#fcd34d',
        color: '#92400e'
      }
    }

    if (status === 'ABSENT') {
      return {
        background: '#fee2e2',
        borderColor: '#fca5a5',
        color: '#991b1b'
      }
    }

    return {}
  }

  // Loại phiếu hủy/từ chối khỏi ma trận.
  const activeRegistrations = registrations.filter(
    isActiveRegistration
  )

  // boardMatrix[ngày][shiftId] = các dòng của đúng ngày và ca.
  const boardMatrix = {}

  dates.forEach((dateObj) => {
    const dateString = toDateString(dateObj)
    boardMatrix[dateString] = {}

    shifts.forEach((shift) => {
      boardMatrix[dateString][shift.id] =
        activeRegistrations.filter((row) => {
          return (
            row.workDate?.slice(0, 10) === dateString &&
            Number(row.shiftId) === Number(shift.id)
          )
        })
    })
  })

  // ========================================================================
  // QUẢN LÝ TRẠNG THÁI ĐỢT ĐĂNG KÝ
  // ========================================================================
  async function handleLockPeriod() {
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

  async function handleReopenPeriod() {
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

  async function handlePublish() {
    setPublishDialog({
      mode: 'confirm',
      message: 'Bạn có chắc chắn muốn công bố lịch làm?'
    })
  }

  async function confirmPublish() {
    setPublishLoading(true)
    try {
      const result = await publishSchedule(period.id)
      setCurrentStatus('PUBLISHED')
      setPublishDialog({
        mode: 'success',
        message:
          result?.message ||
          'Đã công bố lịch làm việc thành công!'
      })
    } catch (error) {
      setPublishDialog({
        mode: 'error',
        message: getApiErrorMessage(
          error,
          'Không thể công bố lịch.'
        )
      })
    } finally {
      setPublishLoading(false)
    }
  }

  async function closePublishDialog() {
    if (publishLoading) return

    const wasSuccessful = publishDialog?.mode === 'success'
    setPublishDialog(null)
    if (wasSuccessful) await onBack()
  }

  // ========================================================================
  // NGHIỆP VỤ NGHỈ/VẮNG VÀ CHỌN NGƯỜI THAY
  // ========================================================================
  function closeReplacementModal() {
    setReplacementModal(null)
    setAbsenceReason('')
    setReplacementCandidates([])
    setSelectedRegistrationId('')
    setReplacementError('')
    setReplacementLoading(false)
  }

  async function loadCandidates(row) {
    setReplacementLoading(true)
    setReplacementError('')
    setSelectedRegistrationId('')

    try {
      const data = await getReplacementCandidates(row.id)

      setReplacementCandidates(
        Array.isArray(data)
          ? data
          : []
      )

      setReplacementModal({
        row,
        step: 'candidates'
      })
    } catch (error) {
      setReplacementError(
        getApiErrorMessage(
          error,
          'Không thể tải danh sách nhân viên dự phòng.'
        )
      )
    } finally {
      setReplacementLoading(false)
    }
  }

  async function openReplacementFlow(row) {
    const status = normalizeStatus(row?.status)
    const assignmentType = normalizeStatus(row?.assignmentType)

    // Không mở xử lý trên chính dòng người thay.
    if (assignmentType === 'EMERGENCY_REPLACEMENT') {
      return
    }

    setAbsenceReason('')
    setReplacementCandidates([])
    setSelectedRegistrationId('')
    setReplacementError('')

    // Lịch đã được đánh dấu nghỉ/vắng thì chuyển thẳng tới WAITLIST.
    if (
      status === 'LEAVE_APPROVED' ||
      status === 'ABSENT'
    ) {
      setReplacementModal({
        row,
        step: 'candidates'
      })

      await loadCandidates(row)
      return
    }

    // Lịch bình thường sẽ mở bước nhập lý do trước.
    if (status === 'PUBLISHED') {
      setReplacementModal({
        row,
        step: 'absence'
      })
    }
  }

  async function handleMarkAbsence(targetStatus) {
    const row = replacementModal?.row
    const reason = absenceReason.trim()

    if (!row) return

    if (reason.length < 3) {
      setReplacementError(
        'Vui lòng nhập lý do từ 3 ký tự trở lên.'
      )
      return
    }

    setReplacementLoading(true)
    setReplacementError('')

    try {
      if (targetStatus === 'LEAVE_APPROVED') {
        await markApprovedLeave(row.id, reason)
      } else {
        await markAbsent(row.id, reason)
      }

      const updatedRow = {
        ...row,
        status: targetStatus,
        absenceReason: reason
      }

      // Chuyển modal sang bước chọn người thay ngay sau khi ghi nhận thành công.
      setReplacementModal({
        row: updatedRow,
        step: 'candidates'
      })
      setSelectedRegistrationId('')
      setBoardReloadKey((value) => value + 1)

      try {
        const candidates = await getReplacementCandidates(row.id)

        setReplacementCandidates(
          Array.isArray(candidates)
            ? candidates
            : []
        )
      } catch (candidateError) {
        setReplacementCandidates([])
        setReplacementError(
          getApiErrorMessage(
            candidateError,
            'Đã ghi nhận nghỉ/vắng nhưng chưa tải được danh sách dự phòng.'
          )
        )
      }
    } catch (error) {
      setReplacementError(
        getApiErrorMessage(
          error,
          targetStatus === 'ABSENT'
            ? 'Không thể ghi nhận vắng không phép.'
            : 'Không thể ghi nhận nghỉ có phép.'
        )
      )
    } finally {
      setReplacementLoading(false)
    }
  }

  async function handleAssignReplacement() {
    const row = replacementModal?.row
    const registrationId = Number(selectedRegistrationId)

    if (!row) return

    if (!registrationId) {
      setReplacementError(
        'Vui lòng chọn một nhân viên dự phòng.'
      )
      return
    }

    if (
      !window.confirm(
        'Xác nhận nhân viên này đã đồng ý đến thay ca?'
      )
    ) {
      return
    }

    setReplacementLoading(true)
    setReplacementError('')

    try {
      await assignEmergencyReplacement(
        row.id,
        registrationId
      )

      alert('Đã điều động nhân viên thay ca thành công!')
      closeReplacementModal()
      setBoardReloadKey((value) => value + 1)
    } catch (error) {
      setReplacementError(
        getApiErrorMessage(
          error,
          'Không thể điều động nhân viên thay ca.'
        )
      )
    } finally {
      setReplacementLoading(false)
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
    <div className="sd-users-page schedule-tabs schedule-tabs--manager">
      <button
        className="sd-btn-back"
        onClick={onBack}
      >
        ← Quay lại danh sách đợt
      </button>

      {publishDialog && (
        <div className="sd-overlay" onClick={closePublishDialog}>
          <div
            className="sd-modal"
            onClick={(event) => event.stopPropagation()}
          >
            <div className="sd-modal-header">
              <h2>
                {publishDialog.mode === 'confirm'
                  ? 'Xác nhận công bố lịch'
                  : publishDialog.mode === 'success'
                    ? 'Công bố thành công'
                    : 'Không thể công bố lịch'}
              </h2>
              {!publishLoading && (
                <button type="button" onClick={closePublishDialog}>✕</button>
              )}
            </div>
            <div className="sd-modal-body">
              <p className={
                publishDialog.mode === 'error'
                  ? 'sd-status sd-status-error'
                  : publishDialog.mode === 'success'
                    ? 'sd-status sd-status-success'
                    : ''
              }>
                {publishDialog.message}
              </p>
            </div>
            <div className="sd-modal-footer">
              {publishDialog.mode === 'confirm' ? (
                <>
                  <button
                    className="sd-btn-ghost"
                    disabled={publishLoading}
                    onClick={closePublishDialog}
                    type="button"
                  >
                    Huỷ
                  </button>
                  <button
                    className="sd-btn-primary"
                    disabled={publishLoading}
                    onClick={confirmPublish}
                    type="button"
                  >
                    {publishLoading ? 'Đang công bố...' : 'Công bố lịch'}
                  </button>
                </>
              ) : (
                <button
                  className="sd-btn-primary"
                  onClick={closePublishDialog}
                  type="button"
                >
                  Đóng
                </button>
              )}
            </div>
          </div>
        </div>
      )}

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
            Lịch đăng ký: Từ {formatDate(period.startDate)} đến{' '}
            {formatDate(period.endDate)}
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
          <strong>
            Lịch làm đã đến ngày bắt đầu nhưng chưa được công bố.
          </strong>{' '}
          Đợt đăng ký không thể mở lại; Quản lý cần kiểm tra và công bố lịch.
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
                <th style={{ width: 90 }}>NGÀY</th>

                {shifts.map((shift) => (
                  <th key={shift.id}>
                    {shift.shiftName}
                    <br />
                    <span
                      style={{
                        fontWeight: 500,
                        fontSize: 11
                      }}
                    >
                      {shift.startTime?.slice(0, 5)} -{' '}
                      {shift.endTime?.slice(0, 5)}
                    </span>
                  </th>
                ))}
              </tr>
            </thead>

            <tbody>
              {dates.map((dateObj) => {
                const dateString = toDateString(dateObj)
                const dayOfWeek = DAY_NAMES[dateObj.getDay()]

                return (
                  <tr key={dateString}>
                    <td className="sd-board-date-col">
                      <strong>{dayOfWeek}</strong>
                      <small>
                        {dateObj.getDate()}/{dateObj.getMonth() + 1}
                      </small>
                    </td>

                    {shifts.map((shift) => {
                      const cellRows =
                        boardMatrix[dateString][shift.id] || []

                      const visibleCellRows = isPublished
                        ? cellRows
                        : cellRows.filter((row) => {
                            return !fullTimeStaff.some((staff) => {
                              return String(staff.id) === String(row.userId)
                            })
                          })

                      const max = getMaxStaffForShiftDate(shift, dateObj)

                      // Sau công bố:
                      // - PUBLISHED mới là người đang thực sự chiếm vị trí.
                      // - LEAVE_APPROVED và ABSENT vẫn hiển thị nhưng không chiếm vị trí.
                      //
                      // Trước công bố:
                      // - REGISTERED chiếm vị trí.
                      // - WAITLIST không chiếm vị trí.
                      const occupiedCount = isPublished
                        ? visibleCellRows.filter((row) => {
                            return normalizeStatus(row.status) === 'PUBLISHED'
                          }).length
                        : visibleCellRows.filter((row) => {
                            return isOfficialRegistrationStatus(row.status)
                          }).length + (max > 0 ? 1 + fullTimeStaff.length : 0)

                      const remainingSlots = Math.max(
                        max - occupiedCount,
                        0
                      )

                      const isFull =
                        max > 0 &&
                        remainingSlots === 0

                      return (
                        <td
                          key={shift.id}
                          className={`sd-schedule-cell ${
                            isFull ? 'is-full' : ''
                          }`}
                        >
                          {max <= 0 ? (
                            <div className="sd-schedule-closed">
                              Không có ca làm
                            </div>
                          ) : (
                            <div className="sd-slot-list">
                              {/* Trước công bố, Manager chưa có dòng ca_final_schedule nên hiển thị tạm. */}
                              {!isPublished && (
                                <div className="sd-slot-person sd-slot-manager">
                                  <span className="sd-slot-name">
                                    {user.fullName ||
                                      user.username ||
                                      'Quản lý'}
                                  </span>

                                  <span className="sd-slot-role">
                                    Quản lý
                                  </span>
                                </div>
                              )}

                              {!isPublished && fullTimeStaff.map((staff) => (
                                <div
                                  key={`full-time-${staff.id}`}
                                  className="sd-slot-person sd-slot-staff"
                                >
                                  <span className="sd-slot-name">
                                    {staff.fullName || staff.username || 'Nhân viên'}
                                  </span>

                                  <span className="sd-slot-role">
                                    Full-time
                                  </span>
                                </div>
                              ))}

                              {visibleCellRows.map((row) => {
                                const managerRow =
                                  isPublished && isManagerRow(row)

                                const rowStatus = normalizeStatus(row.status)
                                const assignmentType = normalizeStatus(
                                  row.assignmentType
                                )

                                const hasReplacement = registrations.some(
                                  (candidateRow) => {
                                    return Number(
                                      candidateRow.replacesScheduleId
                                    ) === Number(row.id)
                                  }
                                )

                                const canHandleReplacement =
                                  isPublished &&
                                  !managerRow &&
                                  !hasReplacement &&
                                  assignmentType !== 'EMERGENCY_REPLACEMENT' &&
                                  [
                                    'PUBLISHED',
                                    'LEAVE_APPROVED',
                                    'ABSENT'
                                  ].includes(rowStatus)

                                const rowStyle = getScheduleRowStyle(
                                  row,
                                  managerRow,
                                  isPublished
                                )

                                return (
                                  <div
                                    key={row.id}
                                    className={`sd-slot-person ${
                                      managerRow
                                        ? 'sd-slot-manager'
                                        : 'sd-slot-staff'
                                    }`}
                                    style={{
                                      ...rowStyle,
                                      alignItems: 'flex-start',
                                      gap: 8
                                    }}
                                  >
                                    <div
                                      style={{
                                        display: 'grid',
                                        gap: 3,
                                        minWidth: 0,
                                        flex: 1
                                      }}
                                    >
                                      <span className="sd-slot-name">
                                        {row.user?.fullName ||
                                          row.user?.email ||
                                          'Nhân viên'}
                                      </span>

                                      <span className="sd-slot-role">
                                        {isPublished
                                          ? getPublishedScheduleLabel(
                                              row,
                                              managerRow
                                            )
                                          : getRegistrationStatusLabel(
                                              row.status
                                            )}
                                      </span>

                                      {row.absenceReason && (
                                        <small
                                          title={row.absenceReason}
                                          style={{
                                            fontSize: 10,
                                            opacity: 0.85
                                          }}
                                        >
                                          Lý do: {row.absenceReason}
                                        </small>
                                      )}
                                    </div>

                                    {canHandleReplacement && (
                                      <button
                                        type="button"
                                        onClick={() =>
                                          openReplacementFlow(row)
                                        }
                                        style={{
                                          border: '1px solid #cbd5e1',
                                          background: '#ffffff',
                                          borderRadius: 6,
                                          padding: '4px 7px',
                                          fontSize: 10,
                                          fontWeight: 700,
                                          cursor: 'pointer',
                                          whiteSpace: 'nowrap'
                                        }}
                                      >
                                        {rowStatus === 'PUBLISHED'
                                          ? 'Xử lý'
                                          : 'Chọn người thay'}
                                      </button>
                                    )}

                                    {isPublished &&
                                      !managerRow &&
                                      hasReplacement && (
                                        <span
                                          style={{
                                            fontSize: 10,
                                            fontWeight: 700,
                                            color: '#166534',
                                            whiteSpace: 'nowrap'
                                          }}
                                        >
                                          Đã có người thay
                                        </span>
                                      )}
                                  </div>
                                )
                              })}

                              {Array.from({
                                length: remainingSlots
                              }).map((_, index) => (
                                <div
                                  key={`empty-${dateString}-${shift.id}-${index}`}
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

      {/* ================================================================
          MODAL XỬ LÝ NGHỈ/VẮNG VÀ CHỌN NGƯỜI THAY
          ================================================================ */}
      {replacementModal && (
        <div
          className="sd-overlay"
          onClick={closeReplacementModal}
        >
          <div
            className="sd-modal"
            onClick={(event) => event.stopPropagation()}
            style={{
              width: 'min(680px, 94vw)'
            }}
          >
            <div className="sd-modal-header">
              <h2>Xử lý nhân viên nghỉ/vắng</h2>

              <button
                type="button"
                onClick={closeReplacementModal}
              >
                ✕
              </button>
            </div>

            <div className="sd-modal-body">
              <div
                style={{
                  padding: 12,
                  marginBottom: 16,
                  border: '1px solid #e2e8f0',
                  borderRadius: 10,
                  background: '#f8fafc'
                }}
              >
                <strong>
                  {replacementModal.row?.user?.fullName ||
                    replacementModal.row?.user?.email ||
                    'Nhân viên'}
                </strong>

                <div
                  style={{
                    marginTop: 5,
                    fontSize: 13,
                    color: '#64748b'
                  }}
                >
                  Ngày làm:{' '}
                  {formatDate(replacementModal.row?.workDate)}
                </div>

                <div
                  style={{
                    marginTop: 3,
                    fontSize: 13,
                    color: '#64748b'
                  }}
                >
                  Ca:{' '}
                  {replacementModal.row?.shift?.shiftName ||
                    'Chưa có thông tin ca'}
                </div>
              </div>

              {replacementModal.step === 'absence' && (
                <>
                  <div className="sd-field">
                    <label>Lý do nghỉ hoặc vắng *</label>

                    <textarea
                      value={absenceReason}
                      onChange={(event) =>
                        setAbsenceReason(event.target.value)
                      }
                      rows={4}
                      maxLength={500}
                      placeholder="Nhập lý do Manager đã xác minh..."
                      style={{
                        width: '100%',
                        resize: 'vertical'
                      }}
                    />
                  </div>

                  <div
                    style={{
                      display: 'flex',
                      gap: 10,
                      flexWrap: 'wrap',
                      marginTop: 16
                    }}
                  >
                    <button
                      type="button"
                      disabled={replacementLoading}
                      onClick={() =>
                        handleMarkAbsence('LEAVE_APPROVED')
                      }
                      style={{
                        padding: '9px 14px',
                        border: '1px solid #fcd34d',
                        background: '#fef3c7',
                        color: '#92400e',
                        borderRadius: 7,
                        fontWeight: 700,
                        cursor: replacementLoading
                          ? 'not-allowed'
                          : 'pointer'
                      }}
                    >
                      Ghi nhận nghỉ có phép
                    </button>

                    <button
                      type="button"
                      disabled={replacementLoading}
                      onClick={() =>
                        handleMarkAbsence('ABSENT')
                      }
                      style={{
                        padding: '9px 14px',
                        border: '1px solid #fca5a5',
                        background: '#fee2e2',
                        color: '#991b1b',
                        borderRadius: 7,
                        fontWeight: 700,
                        cursor: replacementLoading
                          ? 'not-allowed'
                          : 'pointer'
                      }}
                    >
                      Ghi nhận vắng không phép
                    </button>
                  </div>
                </>
              )}

              {replacementModal.step === 'candidates' && (
                <>
                  <div
                    style={{
                      marginBottom: 12,
                      fontSize: 14,
                      color: '#475569'
                    }}
                  >
                    Chọn nhân viên trong danh sách chờ sau khi đã gọi điện
                    và nhận được xác nhận.
                  </div>

                  {replacementLoading ? (
                    <p>Đang tải danh sách dự phòng...</p>
                  ) : replacementCandidates.length === 0 ? (
                    <div
                      style={{
                        padding: 16,
                        borderRadius: 8,
                        background: '#f8fafc',
                        color: '#64748b',
                        textAlign: 'center'
                      }}
                    >
                      Không có nhân viên phù hợp trong danh sách chờ.
                    </div>
                  ) : (
                    <div
                      style={{
                        display: 'grid',
                        gap: 10
                      }}
                    >
                      {replacementCandidates.map((candidate) => (
                        <label
                          key={candidate.registrationId}
                          style={{
                            display: 'flex',
                            gap: 10,
                            alignItems: 'flex-start',
                            padding: 12,
                            border: '1px solid #e2e8f0',
                            borderRadius: 9,
                            cursor: 'pointer',
                            background:
                              String(selectedRegistrationId) ===
                              String(candidate.registrationId)
                                ? '#eff6ff'
                                : '#ffffff'
                          }}
                        >
                          <input
                            type="radio"
                            name="replacementCandidate"
                            value={candidate.registrationId}
                            checked={
                              String(selectedRegistrationId) ===
                              String(candidate.registrationId)
                            }
                            onChange={(event) =>
                              setSelectedRegistrationId(
                                event.target.value
                              )
                            }
                          />

                          <div
                            style={{
                              display: 'grid',
                              gap: 4
                            }}
                          >
                            <strong>
                              #{candidate.queuePosition}{' '}
                              {candidate.fullName || 'Nhân viên'}
                            </strong>

                            <span
                              style={{
                                fontSize: 13,
                                color: '#475569'
                              }}
                            >
                              Điện thoại:{' '}
                              {candidate.phoneNumber ? (
                                <a href={`tel:${candidate.phoneNumber}`}>
                                  {candidate.phoneNumber}
                                </a>
                              ) : (
                                'Chưa có'
                              )}
                            </span>

                            <span
                              style={{
                                fontSize: 13,
                                color: '#475569'
                              }}
                            >
                              Email: {candidate.email || 'Chưa có'}
                            </span>
                          </div>
                        </label>
                      ))}
                    </div>
                  )}
                </>
              )}

              {replacementError && (
                <p className="sd-status sd-status-error">
                  {replacementError}
                </p>
              )}
            </div>

            <div className="sd-modal-footer">
              <button
                type="button"
                className="sd-btn-ghost"
                onClick={closeReplacementModal}
              >
                Đóng
              </button>

              {replacementModal.step === 'candidates' && (
                <button
                  type="button"
                  className="sd-btn-primary"
                  disabled={
                    replacementLoading ||
                    !selectedRegistrationId
                  }
                  onClick={handleAssignReplacement}
                >
                  {replacementLoading
                    ? 'Đang xử lý...'
                    : 'Xác nhận chọn người thay'}
                </button>
              )}
            </div>
          </div>
        </div>
      )}
    </div>
  )
}
