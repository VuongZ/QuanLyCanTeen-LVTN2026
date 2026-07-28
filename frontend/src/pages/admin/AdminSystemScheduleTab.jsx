import { useState, useEffect } from 'react'
import { getAllPeriods } from '../../api/PeriodApi'
import { getAllShifts } from '../../api/ShiftApi'

import {
  getFinalScheduleByPeriod
} from '../../api/StaffRegistrationApi'

import {
  getScheduleUserName,
  isManagerScheduleRow
} from '../../utils/scheduleRoleUtils'

const DAY_NAMES = ['Chủ nhật', 'Thứ 2', 'Thứ 3', 'Thứ 4', 'Thứ 5', 'Thứ 6', 'Thứ 7']

function formatDate(value) {
  if (!value) return '—'
  return new Intl.DateTimeFormat('vi-VN').format(new Date(value))
}

export function AdminSystemScheduleTab({ branches }) {
  const [periods, setPeriods] = useState([])
  const [shifts, setShifts] = useState([])
  const [dates, setDates] = useState([])
  const [registrations, setRegistrations] = useState([])
  const [selectedBranchId, setSelectedBranchId] = useState('')
  const [selectedPeriodId, setSelectedPeriodId] = useState('')
  const [loading, setLoading] = useState(false)

  useEffect(() => { if (branches.length > 0) setSelectedBranchId(branches[0].id.toString()) }, [branches])

  useEffect(() => {
    if (!selectedBranchId) return
    async function loadBranchPeriods() {
      try {
        const allPeriods = await getAllPeriods()
        const pPeriods = allPeriods.filter((p) => String(p.branchId) === String(selectedBranchId) && p.status === 'PUBLISHED').sort((a, b) => new Date(b.startDate) - new Date(a.startDate))
        setPeriods(pPeriods)
        if (pPeriods.length > 0) setSelectedPeriodId(pPeriods[0].id.toString())
        else { setSelectedPeriodId(''); setRegistrations([]); setDates([]) }
      } catch (e) { console.error(e) }
    }
    loadBranchPeriods()
  }, [selectedBranchId])

  useEffect(() => {
    if (!selectedBranchId || !selectedPeriodId) return
    async function loadOfficialSchedule() {
      setLoading(true)
      try {
        const period = periods.find((p) => p.id.toString() === selectedPeriodId)
        if (!period) return
        // Đợt đã PUBLISHED nên phải lấy dữ liệu
        // từ lịch làm chính thức.
        const [scheduleRows, shiftRows] =
          await Promise.all([
            getFinalScheduleByPeriod(period.id),
            getAllShifts()
          ])

        // Danh sách này đã gồm cả Manager
        // và các Staff thuộc lịch chính thức.
        setRegistrations(
          Array.isArray(scheduleRows)
            ? scheduleRows
            : []
        )

        setShifts(
          (shiftRows || []).filter(
            (shift) =>
              String(shift.branchId) ===
              String(selectedBranchId)
          )
        )
        const dArray = []
        let curr = new Date(period.startDate)
        const end = new Date(period.endDate)
        while (curr <= end) { dArray.push(new Date(curr)); curr.setDate(curr.getDate() + 1) }
        setDates(dArray)
      } catch (e) { console.error(e) } finally { setLoading(false) }
    }
    loadOfficialSchedule()
  }, [selectedPeriodId, selectedBranchId, periods])

  function toDateString(dateObj) {
    const offset = dateObj.getTimezoneOffset()
    const d = new Date(dateObj.getTime() - (offset * 60 * 1000))
    return d.toISOString().split('T')[0]
  }

 // Tạo ma trận lịch theo ngày và ca.
const boardMatrix = {}

dates.forEach((dateObj) => {
  const dStr = toDateString(dateObj)

  boardMatrix[dStr] = {}

  shifts.forEach((shift) => {
    boardMatrix[dStr][shift.id] =
      registrations.filter((row) => {
        return (
          row.workDate?.slice(0, 10) === dStr &&
          Number(row.shiftId) ===
            Number(shift.id)
        )
      })
  })
})


  return (
    <div className="sd-card" style={{ padding: '20px 0' }}>
      <div style={{ padding: '0 20px 16px', display: 'flex', flexDirection: 'column', gap: 12, borderBottom: '1px solid #f1f5f9', marginBottom: 16 }}>
        <div className="sd-field" style={{ marginBottom: 0 }}>
          <label>1. Chọn cơ sở canteen giám sát:</label>
          <select value={selectedBranchId} onChange={(e) => setSelectedBranchId(e.target.value)}>
            {branches.map((b) => <option key={b.id} value={b.id}>{b.name}</option>)}
          </select>
        </div>
        <div className="sd-field" style={{ marginBottom: 0 }}>
          <label>2. Chọn tuần làm việc đã chốt sổ:</label>
          <select value={selectedPeriodId} onChange={(e) => setSelectedPeriodId(e.target.value)} disabled={periods.length === 0}>
            {periods.length === 0 ? <option value="">-- Canteen này chưa có lịch chốt chính thức --</option> : periods.map((p) => <option key={p.id} value={p.id}>Từ {formatDate(p.startDate)} đến {formatDate(p.endDate)}</option>)}
          </select>
        </div>
      </div>
      <div style={{ padding: '0 20px' }}>
        {loading ? <p>Đang tải dữ liệu lịch làm việc...</p> : periods.length === 0 ? (
          <div className="sd-empty-state" style={{ padding: '30px 0' }}><span className="sd-empty-icon">🗓️</span><p>Cơ sở này hiện chưa được Quản lý xuất bản (Publish) lịch làm việc.</p></div>
        ) : (
          <div className="sd-board-wrap" style={{ borderRadius: 12 }}>
            <table className="sd-schedule-board">
              <thead>
                <tr>
                  <th style={{ width: 90 }}>NGÀY</th>
                  {shifts.map((s) => <th key={s.id}>{s.shiftName}<br /><span style={{ fontWeight: 500, fontSize: 11 }}>{s.startTime?.slice(0, 5)} - {s.endTime?.slice(0, 5)}</span></th>)}
                </tr>
              </thead>
              <tbody>
                {dates.map((dateObj) => {
                  const dStr = toDateString(dateObj)
                  const dayOfWeek = DAY_NAMES[dateObj.getDay()]
                  return (
                    <tr key={dStr}>
                      <td className="sd-board-date-col"><strong>{dayOfWeek}</strong><small>{dateObj.getDate()}/{dateObj.getMonth() + 1}</small></td>
                      {shifts.map((shift) => {
                        const cellRows =
                          boardMatrix[dStr][shift.id] || []

                        // Manager vẫn nằm trong lịch chính thức
                        // để chiếm một slot.
                        const managerRow =
                          cellRows.find(isManagerScheduleRow)

                        // Chỉ lấy những người không phải Manager
                        // để hiển thị trong danh sách Staff.
                        const staffRows =
                          cellRows.filter(
                            (row) => !isManagerScheduleRow(row)
                          )

                        // Lịch đã công bố và không có dòng nào
                        // thì xem như ô không có ca làm.
                        const isShiftClosed =
                          cellRows.length === 0

                        const managerName =
                          managerRow
                            ? getScheduleUserName(managerRow)
                            : 'Quản lý ca'
                        return (
                          <td key={shift.id}>
                            {!isShiftClosed ? (
                              <>
                                {/* Manager được hiển thị riêng */}
                                <div
                                  className="sd-reg-card"
                                  style={{
                                    background: '#ffedd5',
                                    borderColor: '#fdba74',
                                    color: '#9a3412',
                                    padding: '6px 8px',
                                    borderRadius: 6,
                                    marginBottom: 6,
                                    fontSize: 12,
                                    fontWeight: 600
                                  }}
                                >
                                  <span className="sd-reg-name">
                                    {managerName}
                                  </span>

                                  <span
                                    style={{
                                      marginLeft: 6,
                                      fontSize: 11,
                                      fontWeight: 500
                                    }}
                                  >
                                    Quản lý
                                  </span>
                                </div>

                                {/* Danh sách này chỉ còn Staff */}
                                {staffRows.map((row) => (
                                  <div
                                    key={row.id}
                                    className="sd-reg-cardapproved"
                                    style={{
                                      background: '#f8fafc',
                                      borderColor: '#e2e8f0',
                                      color: '#475569',
                                      padding: '6px 8px',
                                      borderRadius: 6,
                                      marginBottom: 6,
                                      fontSize: 12,
                                      fontWeight: 600
                                    }}
                                  >
                                    <span>
                                      {getScheduleUserName(row)}
                                    </span>
                                  </div>
                                ))}
                              </>
                            ) : (
                              <div
                                style={{
                                  textAlign: 'center',
                                  padding: '16px 0',
                                  color: '#cbd5e1',
                                  fontSize: 12,
                                  fontWeight: 600
                                }}
                              >
                                KHÔNG CÓ CA LÀM
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
    </div>
  )
}