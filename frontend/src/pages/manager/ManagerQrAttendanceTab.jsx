import { useState, useEffect } from 'react'
import axios from 'axios'
import { Html5QrcodeScanner } from 'html5-qrcode'
import { getAllShifts } from '../../api/ShiftApi'

function formatDate(value) {
  if (!value) return '—'
  return new Intl.DateTimeFormat('vi-VN').format(new Date(value))
}

function InfoRow({ label, value }) {
  return <div className="sd-info-row"><dt>{label}</dt><dd>{value}</dd></div>
}

export function ManagerQrAttendanceTab({ user }) {
  const [shifts, setShifts] = useState([])
  const [shiftId, setShiftId] = useState('')
  const [workDate, setWorkDate] = useState(() => new Date().toISOString().slice(0, 10))
  const [scanAction, setScanAction] = useState('CHECKIN')
  const [manualQr, setManualQr] = useState('')
  const [status, setStatus] = useState(null)
  const [scanResult, setScanResult] = useState(null)
  const [isSubmitting, setIsSubmitting] = useState(false)
  const [lastQrText, setLastQrText] = useState('')

  useEffect(() => {
    getAllShifts().then((data) => {
      const branchShifts = (Array.isArray(data) ? data : []).filter((s) => String(s.branchId) === String(user.branchId))
      setShifts(branchShifts)
      setShiftId((current) => current || branchShifts[0]?.id?.toString() || '')
    }).catch(() => setShifts([]))
  }, [user.branchId])

  useEffect(() => {
    const scanner = new Html5QrcodeScanner('manager-qr-reader', { fps: 8, qrbox: { width: 240, height: 240 } }, false)
    scanner.render((decodedText) => { if (decodedText && decodedText !== lastQrText) { setLastQrText(decodedText); handleQrText(decodedText) } }, () => { })
    return () => { scanner.clear().catch(() => { }) }
  }, [lastQrText, shiftId, workDate, scanAction])

  function parseEmployeeQr(text) {
    const parsed = JSON.parse(text)
    const employeeId = parsed.id || parsed.employeeId || parsed.userId
    if (!employeeId) throw new Error('QR không có mã nhân viên')
    return { ...parsed, employeeId: Number(employeeId) }
  }

  async function handleQrText(text) {
    if (!shiftId || !workDate) { setStatus({ type: 'error', msg: 'Vui lòng chọn ngày và ca trước khi quét QR.' }); return }
    setIsSubmitting(true); setStatus(null)
    try {
      const employeeQr = parseEmployeeQr(text)
      const now = new Date().toISOString()
      const res = await axios.post('/api/StaffRegistration/scan-attendance', {
        managerId: user.id, employeeId: employeeQr.employeeId, shiftId: Number(shiftId), workDate, action: scanAction,
        checkInTime: scanAction === 'CHECKIN' ? now : null, checkOutTime: scanAction === 'CHECKOUT' ? now : null,
      })
      setScanResult(res.data)
      setStatus({ type: 'success', msg: 'Đã lưu chấm công thành công.' })
    } catch (err) { setStatus({ type: 'error', msg: err.response?.data?.message || err.message || 'Không đọc được mã QR.' }) } finally { setIsSubmitting(false) }
  }

  function handleManualSubmit(e) {
    e.preventDefault()
    if (!manualQr.trim()) return
    setLastQrText(manualQr.trim()); handleQrText(manualQr.trim())
  }

  return (
    <div className="sd-users-page">
      <div className="sd-card sd-qr-scan-card">
        <div className="sd-card-header"><p className="sd-eyebrow">Chấm công</p><h2>Quét QR nhân viên</h2></div>
        <div className="sd-modal-grid">
          <div className="sd-field"><label>Ngày làm việc</label><input type="date" value={workDate} onChange={(e) => setWorkDate(e.target.value)} /></div>
          <div className="sd-field">
            <label>Ca làm</label>
            <select value={shiftId} onChange={(e) => setShiftId(e.target.value)}>
              <option value="">-- Chọn ca --</option>
              {shifts.map((s) => <option key={s.id} value={s.id}>{s.shiftName} ({s.startTime?.slice(0, 5)} - {s.endTime?.slice(0, 5)})</option>)}
            </select>
          </div>
          <div className="sd-field">
            <label>Hành động</label>
            <select value={scanAction} onChange={(e) => setScanAction(e.target.value)}>
              <option value="CHECKIN">Check-in vào ca</option>
              <option value="CHECKOUT">Check-out kết thúc ca</option>
            </select>
          </div>
        </div>
        <div id="manager-qr-reader" className="sd-qr-reader"></div>
        <form className="sd-qr-manual" onSubmit={handleManualSubmit}>
          <div className="sd-field">
            <label>Nhập dữ liệu QR nếu không mở được camera</label>
            <textarea value={manualQr} onChange={(e) => setManualQr(e.target.value)} placeholder='{"type":"EMPLOYEE","id":1,...}' />
          </div>
          <button className="sd-btn-primary" disabled={isSubmitting || !shiftId || !workDate} type="submit">{isSubmitting ? 'Đang lưu...' : 'Lưu dữ liệu QR'}</button>
        </form>
        {status && <p className={`sd-status sd-status-${status.type}`}>{status.msg}</p>}
      </div>

      {scanResult && (
        <div className="sd-card">
          <div className="sd-card-header"><p className="sd-eyebrow">Kết quả mới nhất</p><h2>{scanResult.employee?.fullName || scanResult.employee?.username}</h2></div>
          <dl className="sd-dl">
            <InfoRow label="Mã lịch làm" value={scanResult.scheduleId} />
            <InfoRow label="Mã chấm công" value={scanResult.attendanceId} />
            <InfoRow label="Ca" value={scanResult.shift?.shiftName || '---'} />
            <InfoRow label="Ngày" value={formatDate(scanResult.workDate)} />
            <InfoRow label="Check-in" value={scanResult.checkInTime ? new Date(scanResult.checkInTime).toLocaleString('vi-VN') : '---'} />
            <InfoRow label="Check-out" value={scanResult.checkOutTime ? new Date(scanResult.checkOutTime).toLocaleString('vi-VN') : '---'} />
            <InfoRow label="Số giờ làm" value={`${scanResult.workedHours || 0} giờ`} />
            <InfoRow label="Mã bảng lương" value={scanResult.salaryId || '---'} />
            <InfoRow label="Trạng thái" value={scanResult.status || '---'} />
          </dl>
        </div>
      )}
    </div>
  )
}