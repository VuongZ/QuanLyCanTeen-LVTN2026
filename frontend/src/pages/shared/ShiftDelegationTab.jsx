import { Fragment, useEffect, useMemo, useState } from 'react'
import {
  createShiftDelegation,
  getShiftDelegations,
  markDelegatedAttendance,
  respondShiftDelegation,
  revokeShiftDelegation,
} from '../../api/ShiftDelegationApi'
import { getAllShifts } from '../../api/ShiftApi'
import { ManagerQrAttendanceTab } from '../manager/ManagerQrAttendanceTab'

function todayInVietnam() {
  return new Intl.DateTimeFormat('en-CA', {
    timeZone: 'Asia/Ho_Chi_Minh',
    year: 'numeric',
    month: '2-digit',
    day: '2-digit',
  }).format(new Date())
}

function formatDateTime(value) {
  if (!value) return '—'
  return new Intl.DateTimeFormat('vi-VN', {
    timeZone: 'Asia/Ho_Chi_Minh',
    dateStyle: 'short',
    timeStyle: 'short',
  }).format(parseUtcDate(value))
}

function parseUtcDate(value) {
  const text = String(value || '')
  return new Date(text.endsWith('Z') ? text : `${text}Z`)
}

function statusLabel(item, currentTime) {
  if (item.status === 'ACCEPTED' && item.isPermissionActive) {
    return 'Đang có quyền'
  }
  if (
    item.status === 'ACCEPTED' &&
    parseUtcDate(item.startsAtUtc).getTime() > currentTime
  ) {
    return 'Đã nhận · Chờ đến giờ ca'
  }
  return {
    PENDING: 'Chờ xác nhận',
    ACCEPTED: 'Đã nhận',
    REJECTED: 'Đã từ chối',
    REVOKED: 'Đã thu hồi',
    EXPIRED: 'Đã hết ca',
  }[item.status] || item.status
}

export function ShiftDelegationTab({
  branches = [],
  isManagement = false,
  user,
  users = [],
}) {
  const [items, setItems] = useState([])
  const [shifts, setShifts] = useState([])
  const [loading, setLoading] = useState(true)
  const [currentTime, setCurrentTime] = useState(() => Date.now())
  const [saving, setSaving] = useState(false)
  const [message, setMessage] = useState(null)
  const [expandedId, setExpandedId] = useState(null)
  const [form, setForm] = useState({
    branchId: user.branchId || '',
    shiftId: '',
    workDate: todayInVietnam(),
    delegateUserId: '',
    reason: '',
  })
  const [attendanceForm, setAttendanceForm] = useState({
    employeeId: '',
    status: 'LATE',
    note: '',
  })

  const selectedBranchId = Number(form.branchId || user.branchId || 0)
  const branchShifts = useMemo(
    () => shifts.filter((shift) => Number(shift.branchId) === selectedBranchId),
    [selectedBranchId, shifts],
  )
  const eligibleUsers = useMemo(
    () => users.filter((candidate) => {
      const role = String(candidate.roleName || '').toUpperCase()
      return Number(candidate.branchId) === selectedBranchId &&
        role.includes('STAFF') &&
        candidate.id !== user.id
    }),
    [selectedBranchId, user.id, users],
  )
  const effectiveShiftId =
    form.shiftId || String(branchShifts[0]?.id || '')
  const activeDelegation = items.find(
    (item) => item.delegateUserId === user.id && item.isPermissionActive,
  )
  const pendingForMe = items.filter(
    (item) => item.delegateUserId === user.id && item.status === 'PENDING',
  )
  const upcomingDelegations = items.filter(
    (item) =>
      item.delegateUserId === user.id &&
      item.status === 'ACCEPTED' &&
      !item.isPermissionActive &&
      parseUtcDate(item.startsAtUtc).getTime() > currentTime,
  )

  async function loadData() {
    setLoading(true)
    try {
      const [delegations, shiftData] = await Promise.all([
        getShiftDelegations(isManagement ? selectedBranchId || undefined : undefined),
        getAllShifts(),
      ])
      setItems(Array.isArray(delegations) ? delegations : [])
      setShifts(Array.isArray(shiftData) ? shiftData : [])
    } catch (error) {
      setMessage({
        type: 'error',
        text: error.response?.data?.message || 'Không tải được dữ liệu ủy quyền.',
      })
    } finally {
      setLoading(false)
    }
  }

  useEffect(() => {
    let cancelled = false
    function refresh() {
      return Promise.all([
        getShiftDelegations(isManagement ? selectedBranchId || undefined : undefined),
        getAllShifts(),
      ])
        .then(([delegations, shiftData]) => {
        if (cancelled) return
        setItems(Array.isArray(delegations) ? delegations : [])
        setShifts(Array.isArray(shiftData) ? shiftData : [])
        setCurrentTime(Date.now())
      })
        .catch((error) => {
        if (!cancelled) {
          setMessage({
            type: 'error',
            text: error.response?.data?.message || 'Không tải được dữ liệu ủy quyền.',
          })
        }
      })
        .finally(() => {
        if (!cancelled) setLoading(false)
      })
    }

    queueMicrotask(refresh)
    const timer = window.setInterval(refresh, 30000)
    return () => {
      cancelled = true
      window.clearInterval(timer)
    }
  }, [isManagement, selectedBranchId])

  async function submitDelegation(event) {
    event.preventDefault()
    setSaving(true)
    setMessage(null)
    try {
      await createShiftDelegation({
        ...form,
        branchId: selectedBranchId,
        shiftId: Number(effectiveShiftId),
        delegateUserId: Number(form.delegateUserId),
      })
      setMessage({ type: 'success', text: 'Đã gửi yêu cầu ủy quyền cho nhân viên.' })
      setForm((current) => ({ ...current, delegateUserId: '', reason: '' }))
      await loadData()
    } catch (error) {
      setMessage({
        type: 'error',
        text: error.response?.data?.message || 'Không thể tạo ủy quyền.',
      })
    } finally {
      setSaving(false)
    }
  }

  async function respond(id, accept) {
    setSaving(true)
    try {
      await respondShiftDelegation(id, accept)
      setMessage({
        type: 'success',
        text: accept ? 'Bạn đã nhận quyền trưởng ca tạm thời.' : 'Bạn đã từ chối yêu cầu.',
      })
      await loadData()
    } catch (error) {
      setMessage({ type: 'error', text: error.response?.data?.message || 'Không thể xử lý yêu cầu.' })
    } finally {
      setSaving(false)
    }
  }

  async function revoke(id) {
    setSaving(true)
    try {
      await revokeShiftDelegation(id)
      setMessage({ type: 'success', text: 'Đã thu hồi quyền trưởng ca tạm thời.' })
      await loadData()
    } catch (error) {
      setMessage({ type: 'error', text: error.response?.data?.message || 'Không thể thu hồi.' })
    } finally {
      setSaving(false)
    }
  }

  async function markAttendance(event) {
    event.preventDefault()
    if (!activeDelegation) return
    setSaving(true)
    try {
      await markDelegatedAttendance({
        employeeId: Number(attendanceForm.employeeId),
        shiftId: activeDelegation.shiftId,
        workDate: activeDelegation.workDate,
        status: attendanceForm.status,
        note: attendanceForm.note,
      })
      setMessage({ type: 'success', text: 'Đã ghi nhận trạng thái chấm công.' })
      setAttendanceForm({ employeeId: '', status: 'LATE', note: '' })
      await loadData()
    } catch (error) {
      setMessage({ type: 'error', text: error.response?.data?.message || 'Không thể ghi nhận.' })
    } finally {
      setSaving(false)
    }
  }

  return (
    <div className="sd-users-page">
      {message && <p className={`sd-status sd-status-${message.type}`}>{message.text}</p>}

      {isManagement && (
        <div className="sd-card">
          <div className="sd-card-header">
            <p className="sd-eyebrow">Phân quyền có kiểm soát</p>
            <h2>Ủy quyền trưởng ca tạm thời</h2>
          </div>
          <form className="sd-modal-grid" onSubmit={submitDelegation}>
            <div className="sd-field">
              <label>Chi nhánh</label>
              <select
                disabled={!String(user.roleName || user.role).toUpperCase().includes('ADMIN')}
                value={form.branchId}
                onChange={(event) => setForm((current) => ({
                  ...current,
                  branchId: event.target.value,
                  shiftId: '',
                  delegateUserId: '',
                }))}
              >
                <option value="">-- Chọn chi nhánh --</option>
                {branches.map((branch) => (
                  <option key={branch.id} value={branch.id}>{branch.name}</option>
                ))}
              </select>
            </div>
            <div className="sd-field">
              <label>Ngày làm việc</label>
              <input
                min={todayInVietnam()}
                type="date"
                value={form.workDate}
                onChange={(event) => setForm((current) => ({ ...current, workDate: event.target.value }))}
              />
            </div>
            <div className="sd-field">
              <label>Ca cần ủy quyền</label>
              <select
                required
                value={effectiveShiftId}
                onChange={(event) => setForm((current) => ({ ...current, shiftId: event.target.value }))}
              >
                <option value="">-- Chọn ca --</option>
                {branchShifts.map((shift) => (
                  <option key={shift.id} value={shift.id}>
                    {shift.shiftName} ({shift.startTime?.slice(0, 5)}–{shift.endTime?.slice(0, 5)})
                  </option>
                ))}
              </select>
            </div>
            <div className="sd-field">
              <label>Người được giao thay</label>
              <select
                required
                value={form.delegateUserId}
                onChange={(event) => setForm((current) => ({ ...current, delegateUserId: event.target.value }))}
              >
                <option value="">-- Chọn nhân viên --</option>
                {eligibleUsers.map((candidate) => (
                  <option key={candidate.id} value={candidate.id}>{candidate.fullName}</option>
                ))}
              </select>
            </div>
            <div className="sd-field" style={{ gridColumn: '1 / -1' }}>
              <label>Lý do ủy quyền</label>
              <textarea
                maxLength={500}
                required
                value={form.reason}
                onChange={(event) => setForm((current) => ({ ...current, reason: event.target.value }))}
              />
            </div>
            <button className="sd-btn-primary" disabled={saving} type="submit">
              {saving ? 'Đang gửi...' : 'Gửi yêu cầu ủy quyền'}
            </button>
          </form>
        </div>
      )}

      {!isManagement && pendingForMe.length > 0 && (
        <div className="sd-card">
          <div className="sd-card-header">
            <p className="sd-eyebrow">Thông báo mới</p>
            <h2>Yêu cầu nhận quyền trưởng ca</h2>
          </div>
          {pendingForMe.map((item) => (
            <div className="sd-info-row" key={item.id}>
              <div>
                <strong>{item.shiftName} · {item.workDate}</strong>
                <p>{item.delegatedByName}: {item.reason}</p>
              </div>
              <div className="sd-flex-center">
                <button className="sd-btn-ghost" disabled={saving} onClick={() => respond(item.id, false)} type="button">Từ chối</button>
                <button className="sd-btn-primary" disabled={saving} onClick={() => respond(item.id, true)} type="button">Xác nhận</button>
              </div>
            </div>
          ))}
        </div>
      )}

      {!isManagement && upcomingDelegations.length > 0 && (
        <div className="sd-card">
          <div className="sd-card-header">
            <p className="sd-eyebrow">Quyền đã xác nhận</p>
            <h2>Ca trưởng tạm thời sắp tới</h2>
          </div>
          {upcomingDelegations.map((item) => (
            <div className="sd-info-row" key={item.id}>
              <div>
                <strong>{item.shiftName} · {item.workDate}</strong>
                <p>
                  Quyền quét QR bắt đầu lúc {formatDateTime(item.startsAtUtc)}
                  {' '}và kết thúc lúc {formatDateTime(item.endsAtUtc)}.
                </p>
                <p>
                  Khu vực quét QR sẽ tự động xuất hiện khi ca bắt đầu;
                  hệ thống không cho phép chấm công trước giờ.
                </p>
              </div>
            </div>
          ))}
        </div>
      )}

      {!isManagement && activeDelegation && (
        <>
          <div className="sd-card">
            <div className="sd-card-header">
              <p className="sd-eyebrow">Quyền đang hiệu lực</p>
              <h2>Trưởng ca tạm thời · {activeDelegation.shiftName}</h2>
            </div>
            <p>Quyền tự hết hạn lúc {formatDateTime(activeDelegation.endsAtUtc)}.</p>
          </div>
          <ManagerQrAttendanceTab
            delegatedShiftId={activeDelegation.shiftId}
            user={user}
          />
          <div className="sd-card">
            <div className="sd-card-header"><h2>Ghi nhận đi muộn / vắng mặt</h2></div>
            <form className="sd-modal-grid" onSubmit={markAttendance}>
              <div className="sd-field">
                <label>Nhân viên</label>
                <select
                  required
                  value={attendanceForm.employeeId}
                  onChange={(event) => setAttendanceForm((current) => ({ ...current, employeeId: event.target.value }))}
                >
                  <option value="">-- Chọn nhân viên --</option>
                  {eligibleUsers.concat(users.filter((candidate) => candidate.id === user.id)).map((candidate) => (
                    <option key={candidate.id} value={candidate.id}>{candidate.fullName}</option>
                  ))}
                </select>
              </div>
              <div className="sd-field">
                <label>Trạng thái</label>
                <select
                  value={attendanceForm.status}
                  onChange={(event) => setAttendanceForm((current) => ({ ...current, status: event.target.value }))}
                >
                  <option value="LATE">Đi muộn</option>
                  <option value="ABSENT">Vắng mặt</option>
                </select>
              </div>
              <div className="sd-field">
                <label>Ghi chú</label>
                <input
                  value={attendanceForm.note}
                  onChange={(event) => setAttendanceForm((current) => ({ ...current, note: event.target.value }))}
                />
              </div>
              <button className="sd-btn-primary" disabled={saving} type="submit">Ghi nhận</button>
            </form>
          </div>
        </>
      )}

      <div className="sd-card">
        <div className="sd-card-header">
          <p className="sd-eyebrow">Kiểm soát & truy vết</p>
          <h2>Lịch sử ủy quyền</h2>
        </div>
        {loading ? <p>Đang tải...</p> : items.length === 0 ? <p>Chưa có dữ liệu ủy quyền.</p> : (
          <div className="sd-salary-table-wrap">
            <table className="sd-salary-table">
              <thead><tr><th>Ca</th><th>Người ủy quyền</th><th>Người nhận</th><th>Thời gian</th><th>Trạng thái</th><th>Thao tác</th></tr></thead>
              <tbody>
                {items.map((item) => (
                  <Fragment key={item.id}>
                    <tr>
                      <td><strong>{item.shiftName}</strong><br />{item.workDate}<br />{item.branchName}</td>
                      <td>{item.delegatedByName}</td>
                      <td>{item.delegateUserName}</td>
                      <td>{formatDateTime(item.startsAtUtc)}<br />→ {formatDateTime(item.endsAtUtc)}</td>
                      <td>{statusLabel(item, currentTime)}</td>
                      <td>
                        <button className="sd-btn-ghost" onClick={() => setExpandedId(expandedId === item.id ? null : item.id)} type="button">Nhật ký</button>
                        {isManagement && ['PENDING', 'ACCEPTED'].includes(item.status) && (
                          <button className="sd-btn-ghost btn-delete" disabled={saving} onClick={() => revoke(item.id)} type="button">Thu hồi</button>
                        )}
                      </td>
                    </tr>
                    {expandedId === item.id && (
                      <tr key={`${item.id}-audit`}>
                        <td colSpan="6">
                          <strong>Lý do:</strong> {item.reason}
                          {(item.audits || []).map((audit) => (
                            <p key={audit.id}>
                              {formatDateTime(audit.occurredAtUtc)} · {audit.actorName} · {audit.actionType}: {audit.details}
                            </p>
                          ))}
                        </td>
                      </tr>
                    )}
                  </Fragment>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </div>
    </div>
  )
}
