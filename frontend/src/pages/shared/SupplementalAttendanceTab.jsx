import { useCallback, useEffect, useState } from 'react'
import {
  approveSupplementalRequest,
  getMySupplementalRequests,
  getSupplementalCandidates,
  getSupplementalRequestsForReview,
  rejectSupplementalRequest,
  submitSupplementalAttendance,
} from '../../api/SupplementalAttendanceApi'
import './supplemental-attendance.css'

const STATUS_LABEL = {
  PENDING: 'Chờ admin duyệt',
  APPROVED: 'Đã duyệt',
  REJECTED: 'Bị từ chối',
}

function localToday() {
  const now = new Date()
  return `${now.getFullYear()}-${String(now.getMonth() + 1).padStart(2, '0')}-${String(now.getDate()).padStart(2, '0')}`
}

function dateTime(value) {
  if (!value) return '—'
  return new Intl.DateTimeFormat('vi-VN', { dateStyle: 'short', timeStyle: 'short' }).format(new Date(value))
}

function RequestRow({ item, reviewing, onChanged }) {
  const [rejectReason, setRejectReason] = useState('')
  const [busy, setBusy] = useState(false)
  const [error, setError] = useState('')

  async function act(action) {
    setBusy(true)
    setError('')
    try {
      if (action === 'approve') await approveSupplementalRequest(item.id)
      else await rejectSupplementalRequest(item.id, rejectReason)
      await onChanged()
    } catch (err) {
      setError(err.response?.data?.message || 'Không thể xử lý yêu cầu.')
    } finally {
      setBusy(false)
    }
  }

  return (
    <article className="sa-request-card">
      <div className="sa-request-head">
        <div>
          <strong>{item.employeeName}</strong>
          <span>{item.workDate} · {item.shiftName} ({item.startTime}–{item.endTime})</span>
        </div>
        <span className={`sa-status sa-status--${item.status.toLowerCase()}`}>{STATUS_LABEL[item.status] || item.status}</span>
      </div>
      <div className="sa-request-info">
        <span>Giờ vào đề nghị <strong>{dateTime(item.proposedCheckInTime)}</strong></span>
        <span>Giờ ra đề nghị <strong>{dateTime(item.proposedCheckOutTime)}</strong></span>
        <span>Tính lương <strong>{item.workedHours} giờ</strong></span>
      </div>
      <p className="sa-meta">{item.branchName || 'Chưa rõ cơ sở'} · Quản lý: {item.managerName || '—'} · {item.reason || 'Không có ghi chú'}</p>
      {item.rejectReason && <p className="sa-reject-note">Lý do từ chối: {item.rejectReason}</p>}
      {reviewing && (
        <div className="sa-review-actions">
          <button className="sa-approve" disabled={busy} onClick={() => act('approve')} type="button">Duyệt và tính lương</button>
          <input maxLength={500} onChange={(event) => setRejectReason(event.target.value)} placeholder="Lý do từ chối" value={rejectReason} />
          <button className="sa-reject" disabled={busy || !rejectReason.trim()} onClick={() => act('reject')} type="button">Từ chối</button>
        </div>
      )}
      {error && <p className="sa-error">{error}</p>}
    </article>
  )
}

export function SupplementalAttendanceTab({ isAdmin }) {
  const [today] = useState(localToday)
  const [workDate, setWorkDate] = useState(today)
  const [candidates, setCandidates] = useState([])
  const [selected, setSelected] = useState({})
  const [requests, setRequests] = useState([])
  const [reason, setReason] = useState('')
  const [loading, setLoading] = useState(true)
  const [saving, setSaving] = useState(false)
  const [message, setMessage] = useState(null)

  const load = useCallback(async () => {
    setLoading(true)
    setMessage(null)
    try {
      if (isAdmin) {
        const data = await getSupplementalRequestsForReview()
        setRequests(Array.isArray(data) ? data : [])
      } else {
        const [candidateData, requestData] = await Promise.all([
          getSupplementalCandidates(workDate),
          getMySupplementalRequests(),
        ])
        const list = Array.isArray(candidateData) ? candidateData : []
        setCandidates(list)
        setRequests(Array.isArray(requestData) ? requestData : [])
        setSelected((current) => {
          const next = {}
          list.forEach((item) => {
            if (current[item.scheduleId]?.checked) next[item.scheduleId] = current[item.scheduleId]
          })
          return next
        })
      }
    } catch (err) {
      setMessage({ type: 'error', text: err.response?.data?.message || 'Không thể tải dữ liệu chấm công bổ sung.' })
    } finally {
      setLoading(false)
    }
  }, [isAdmin, workDate])

  useEffect(() => {
    let cancelled = false
    const request = isAdmin
      ? getSupplementalRequestsForReview().then((requestData) => ({ requestData }))
      : Promise.all([getSupplementalCandidates(workDate), getMySupplementalRequests()])
        .then(([candidateData, requestData]) => ({ candidateData, requestData }))

    request
      .then(({ candidateData, requestData }) => {
        if (cancelled) return
        if (!isAdmin) setCandidates(Array.isArray(candidateData) ? candidateData : [])
        setRequests(Array.isArray(requestData) ? requestData : [])
      })
      .catch((err) => {
        if (!cancelled) setMessage({ type: 'error', text: err.response?.data?.message || 'Không thể tải dữ liệu chấm công bổ sung.' })
      })
      .finally(() => { if (!cancelled) setLoading(false) })

    return () => { cancelled = true }
  }, [isAdmin, workDate])

  function toggleCandidate(item, checked) {
    setSelected((current) => ({
      ...current,
      [item.scheduleId]: {
        checked,
        checkInTime: current[item.scheduleId]?.checkInTime || item.previousCheckInTime?.slice(11, 16) || item.startTime,
        checkOutTime: current[item.scheduleId]?.checkOutTime || item.previousCheckOutTime?.slice(11, 16) || item.endTime,
      },
    }))
  }

  async function submit(event) {
    event.preventDefault()
    const entries = candidates
      .filter((item) => selected[item.scheduleId]?.checked)
      .map((item) => ({
        scheduleId: item.scheduleId,
        checkInTime: `${workDate}T${selected[item.scheduleId].checkInTime}:00`,
        checkOutTime: `${workDate}T${selected[item.scheduleId].checkOutTime}:00`,
      }))
    if (entries.length === 0) {
      setMessage({ type: 'error', text: 'Vui lòng chọn ít nhất một nhân viên.' })
      return
    }

    setSaving(true)
    setMessage(null)
    try {
      const result = await submitSupplementalAttendance({ entries, reason })
      setSelected({})
      setReason('')
      await load()
      setMessage({ type: 'success', text: result.message })
    } catch (err) {
      setMessage({ type: 'error', text: err.response?.data?.message || 'Không thể gửi yêu cầu.' })
    } finally {
      setSaving(false)
    }
  }

  return (
    <section className="sa-page">
      <div className="sa-intro">
        <div>
          <p className="sd-eyebrow">Chấm công</p>
          <h2>{isAdmin ? 'Duyệt chấm công bổ sung' : 'Tạo chấm công bổ sung'}</h2>
          <p>{isAdmin
            ? 'Giờ làm chỉ được cộng vào lương sau khi admin duyệt.'
            : 'Chọn ngày, nhân viên, giờ vào và giờ ra thực tế để gửi admin duyệt.'}</p>
        </div>
        <button className="sa-refresh" disabled={loading} onClick={load} type="button">Làm mới</button>
      </div>

      {message && <p className={`sa-message sa-message--${message.type}`}>{message.text}</p>}

      {!isAdmin && (
        <form className="sa-create" onSubmit={submit}>
          <div className="sa-toolbar">
            <label>Ngày làm<input max={today} onChange={(event) => { setLoading(true); setWorkDate(event.target.value) }} type="date" value={workDate} /></label>
            <label className="sa-reason">Lý do/ghi chú<input maxLength={500} onChange={(event) => setReason(event.target.value)} placeholder="Ví dụ: nhân viên quên quét QR" value={reason} /></label>
          </div>
          <div className="sa-table-wrap">
            <table className="sd-table sa-table">
              <thead><tr><th>Chọn</th><th>Nhân viên</th><th>Ca làm</th><th>Giờ vào bổ sung</th><th>Giờ ra bổ sung</th><th>Ghi chú</th></tr></thead>
              <tbody>
                {loading ? <tr><td colSpan={6}>Đang tải...</td></tr>
                  : candidates.length === 0 ? <tr><td colSpan={6}>Không có ca đủ điều kiện. Ca đã có chấm công hoặc yêu cầu đang chờ sẽ không hiển thị.</td></tr>
                    : candidates.map((item) => (
                      <tr key={item.scheduleId}>
                        <td><input checked={Boolean(selected[item.scheduleId]?.checked)} onChange={(event) => toggleCandidate(item, event.target.checked)} type="checkbox" /></td>
                        <td><strong>{item.employeeName}</strong></td>
                        <td>{item.shiftName}<span className="sa-subline">{item.startTime}–{item.endTime}</span></td>
                        <td><input
                          disabled={!selected[item.scheduleId]?.checked}
                          onChange={(event) => setSelected((current) => ({ ...current, [item.scheduleId]: { ...current[item.scheduleId], checkInTime: event.target.value } }))}
                          required={Boolean(selected[item.scheduleId]?.checked)}
                          type="time"
                          value={selected[item.scheduleId]?.checkInTime || item.previousCheckInTime?.slice(11, 16) || item.startTime}
                        /></td>
                        <td><input
                          disabled={!selected[item.scheduleId]?.checked}
                          onChange={(event) => setSelected((current) => ({ ...current, [item.scheduleId]: { ...current[item.scheduleId], checkOutTime: event.target.value } }))}
                          required={Boolean(selected[item.scheduleId]?.checked)}
                          type="time"
                          value={selected[item.scheduleId]?.checkOutTime || item.previousCheckOutTime?.slice(11, 16) || item.endTime}
                        /></td>
                        <td>{item.previousRejectReason ? <span className="sa-rejected-hint">Gửi lại: {item.previousRejectReason}</span> : 'Chưa có chấm công'}</td>
                      </tr>
                    ))}
              </tbody>
            </table>
          </div>
          <button className="sa-submit" disabled={saving || loading} type="submit">{saving ? 'Đang gửi...' : 'Gửi admin duyệt'}</button>
        </form>
      )}

      <div className="sa-list-section">
        <h3>{isAdmin ? `Chờ duyệt (${requests.length})` : 'Lịch sử yêu cầu của tôi'}</h3>
        {loading ? <p className="sa-empty">Đang tải...</p>
          : requests.length === 0 ? <p className="sa-empty">Không có yêu cầu nào.</p>
            : <div className="sa-list">{requests.map((item) => <RequestRow item={item} key={item.id} onChanged={load} reviewing={isAdmin} />)}</div>}
      </div>
    </section>
  )
}
