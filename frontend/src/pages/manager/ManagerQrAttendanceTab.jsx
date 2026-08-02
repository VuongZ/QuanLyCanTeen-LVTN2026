import { useState, useEffect, useRef } from 'react'

import { Html5QrcodeScanner } from 'html5-qrcode'
import { getAllShifts } from '../../api/ShiftApi'
import {
  scanAttendance
} from '../../api/AttendanceApi'

function formatDate(value) {
  if (!value) return '—'
  return new Intl.DateTimeFormat('vi-VN').format(new Date(value))
}

function getVietnamDateString(date = new Date()) {
  const parts = new Intl.DateTimeFormat('en-US', {
    timeZone: 'Asia/Ho_Chi_Minh',
    year: 'numeric',
    month: '2-digit',
    day: '2-digit',
  }).formatToParts(date)
  const values = Object.fromEntries(parts.map((part) => [part.type, part.value]))
  return `${values.year}-${values.month}-${values.day}`
}

function formatVietnamDateTime(value) {
  if (!value) return '---'
  const text = String(value)
  const match = text.match(/^(\d{4})-(\d{2})-(\d{2})T(\d{2}):(\d{2}):(\d{2})/)
  if (match) {
    const [, year, month, day, hour, minute, second] = match
    return `${hour}:${minute}:${second} ${day}/${month}/${year}`
  }

  return new Intl.DateTimeFormat('vi-VN', {
    timeZone: 'Asia/Ho_Chi_Minh',
    hour: '2-digit',
    minute: '2-digit',
    second: '2-digit',
    day: '2-digit',
    month: '2-digit',
    year: 'numeric',
    hour12: false,
  }).format(new Date(value))
}

function InfoRow({ label, value }) {
  return <div className="sd-info-row"><dt>{label}</dt><dd>{value}</dd></div>
}

export function ManagerQrAttendanceTab({ delegatedShiftId, user }) {
  const [shifts, setShifts] = useState([])
  const [shiftId, setShiftId] = useState('')
  const [scanAction, setScanAction] = useState('CHECKIN')
  const [manualQr, setManualQr] = useState('')
  const [status, setStatus] = useState(null)
  const [notification, setNotification] = useState(null)
  const [scanResult, setScanResult] = useState(null)
  const [isSubmitting, setIsSubmitting] = useState(false)
  const [isScannerOpen, setIsScannerOpen] = useState(false)
  const lastQrTextRef = useRef('')
  const scannerRef = useRef(null)

  useEffect(() => {
    getAllShifts().then((data) => {
      const branchShifts = (Array.isArray(data) ? data : []).filter((s) =>
        String(s.branchId) === String(user.branchId) &&
        (!delegatedShiftId || Number(s.id) === Number(delegatedShiftId)))
      setShifts(branchShifts)
      setShiftId((current) => current || branchShifts[0]?.id?.toString() || '')
    }).catch(() => setShifts([]))
  }, [delegatedShiftId, user.branchId])

  useEffect(() => {
    if (!notification) return undefined

    const timeoutId = window.setTimeout(() => setNotification(null), 4500)
    return () => window.clearTimeout(timeoutId)
  }, [notification])

  useEffect(() => {
    if (status) setNotification(status)
  }, [status])

  useEffect(() => {
    if (!isScannerOpen) return

    let isMounted = true
    const scannerElement = document.getElementById('manager-qr-reader')
    if (scannerElement) scannerElement.innerHTML = ''

    const scanner = new Html5QrcodeScanner('manager-qr-reader', { fps: 8, qrbox: { width: 240, height: 240 } }, false)
    scannerRef.current = scanner
    scanner.render((decodedText) => {
      const scanKey = `${scanAction}|${shiftId}|${getVietnamDateString()}|${decodedText}`
      if (decodedText && scanKey !== lastQrTextRef.current) {
        lastQrTextRef.current = scanKey
        handleQrText(decodedText)
      }
    }, () => { })

    return () => {
      isMounted = false
      scanner.clear()
        .catch(() => { })
        .finally(() => {
          if (!isMounted && scannerRef.current === scanner) scannerRef.current = null
        })
    }
  }, [isScannerOpen, shiftId, scanAction])

  async function closeScanner() {
    setIsScannerOpen(false)
    lastQrTextRef.current = ''
    if (scannerRef.current) {
      const scanner = scannerRef.current
      scannerRef.current = null
      await scanner.clear().catch(() => { })
    }
  }

  function parseEmployeeQr(text) {
    const parsed = JSON.parse(text)
    const employeeId = parsed.id || parsed.employeeId || parsed.userId
    if (!employeeId) throw new Error('QR không có mã nhân viên')
    return { ...parsed, employeeId: Number(employeeId) }
  }

  async function handleQrText(text) {
    if (!shiftId) { setStatus({ type: 'error', msg: 'Vui lòng chọn ca trước khi quét QR.' }); return }
    const workDate = getVietnamDateString()
    setIsSubmitting(true); setStatus(null)
    try {
      const employeeQr = parseEmployeeQr(text)
      const result = await scanAttendance({
  employeeId: employeeQr.employeeId,
  shiftId: Number(shiftId),
  workDate,
  action: scanAction
})

setScanResult(result)
      setStatus({ type: 'success', msg: 'Đã lưu chấm công thành công.' })
    } catch (err) { setStatus({ type: 'error', msg: err.response?.data?.message || err.message || 'Không đọc được mã QR.' }) } finally { setIsSubmitting(false) }
  }

  function handleManualSubmit(e) {
    e.preventDefault()
    if (!manualQr.trim()) return
    lastQrTextRef.current = `${scanAction}|${shiftId}|${getVietnamDateString()}|${manualQr.trim()}`
    handleQrText(manualQr.trim())
  }

  return (
    <div className="sd-users-page">
      {notification && (
        <div className={`sd-scan-notification sd-scan-notification-${notification.type}`} role="alert">
          <strong>{notification.type === 'success' ? 'Quét thành công' : 'Quét thất bại'}</strong>
          <span>{notification.msg}</span>
          <button aria-label="Đóng thông báo" type="button" onClick={() => setNotification(null)}>x</button>
        </div>
      )}

      <div className="sd-card sd-qr-scan-card">
        <div className="sd-card-header"><p className="sd-eyebrow">Chấm công</p><h2>Quét QR nhân viên</h2></div>
        <div className="sd-modal-grid">
          <div className="sd-field">
            <label>Ca làm</label>
            <select disabled={Boolean(delegatedShiftId)} value={shiftId} onChange={(e) => setShiftId(e.target.value)}>
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
        <div className="sd-qr-actions">
          {isScannerOpen ? (
            <button className="sd-btn-ghost" type="button" onClick={closeScanner}>Tắt camera</button>
          ) : (
            <button className="sd-btn-primary" disabled={!shiftId} type="button" onClick={() => setIsScannerOpen(true)}>Mở camera quét QR</button>
          )}
        </div>
        {isScannerOpen && <div id="manager-qr-reader" className="sd-qr-reader"></div>}
        <form className="sd-qr-manual" onSubmit={handleManualSubmit}>
          <div className="sd-field">
            <label>Nhập dữ liệu QR nếu không mở được camera</label>
            <textarea value={manualQr} onChange={(e) => setManualQr(e.target.value)} placeholder='{"type":"EMPLOYEE","id":1,...}' />
          </div>
          <button className="sd-btn-primary" disabled={isSubmitting || !shiftId} type="submit">{isSubmitting ? 'Đang lưu...' : 'Lưu dữ liệu QR'}</button>
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
            <InfoRow label="Check-in" value={formatVietnamDateTime(scanResult.checkInTime)} />
            <InfoRow label="Check-out" value={formatVietnamDateTime(scanResult.checkOutTime)} />
            <InfoRow label="Số giờ làm" value={`${scanResult.workedHours ?? 0} giờ`} />
            <InfoRow label="Mã bảng lương" value={scanResult.salaryId || '---'} />
            <InfoRow label="Trạng thái" value={scanResult.status || '---'} />
          </dl>
        </div>
      )}
    </div>
  )
}
