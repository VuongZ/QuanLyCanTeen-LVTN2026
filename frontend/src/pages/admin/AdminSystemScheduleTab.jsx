import { useEffect, useState } from 'react'

// CSS riêng cho ba tab lịch: Staff, Manager và Admin.
import '../css/ScheduleTabs.css'
import { getAllPeriods } from '../../api/PeriodApi'
import { getAllShifts } from '../../api/ShiftApi'

// Admin đọc lịch chính thức qua API lịch.
import {
  getFinalScheduleByPeriod
} from '../../api/FinalScheduleApi'

import {
  getScheduleUserName,
  isManagerScheduleRow
} from '../../utils/scheduleRoleUtils'

// getDay() của JavaScript trả về số từ 0 đến 6.
// 0 là Chủ nhật, 1 là Thứ 2, ..., 6 là Thứ 7.
const DAY_NAMES = [
  'Chủ nhật',
  'Thứ 2',
  'Thứ 3',
  'Thứ 4',
  'Thứ 5',
  'Thứ 6',
  'Thứ 7'
]

/**
 * Chuyển ngày từ dữ liệu API sang định dạng ngày Việt Nam.
 * Ví dụ: 2026-08-03 -> 03/08/2026.
 */
function formatDate(value) {
  if (!value) {
    return '—'
  }

  return new Intl.DateTimeFormat('vi-VN').format(
    new Date(value)
  )
}

export function AdminSystemScheduleTab({ branches }) {
  // Danh sách các đợt đã được công bố của cơ sở đang chọn.
  const [periods, setPeriods] = useState([])

  // Danh sách ca làm thuộc cơ sở đang chọn.
  const [shifts, setShifts] = useState([])

  // Danh sách ngày từ ngày bắt đầu đến ngày kết thúc của đợt.
  const [dates, setDates] = useState([])

  // Danh sách lịch làm chính thức, gồm cả Manager và Staff.
  const [registrations, setRegistrations] = useState([])

  // Id cơ sở đang được Admin chọn.
  const [selectedBranchId, setSelectedBranchId] = useState('')

  // Id đợt lịch đang được Admin chọn.
  const [selectedPeriodId, setSelectedPeriodId] = useState('')

  const [loading, setLoading] = useState(false)

  /**
   * Khi danh sách cơ sở được truyền vào,
   * tự động chọn cơ sở đầu tiên.
   */
  useEffect(() => {
    if (branches.length > 0) {
      setSelectedBranchId(
        branches[0].id.toString()
      )
    }
  }, [branches])

  /**
   * Khi Admin đổi cơ sở:
   * 1. Lấy toàn bộ đợt lịch.
   * 2. Chỉ giữ đợt thuộc cơ sở đang chọn.
   * 3. Chỉ giữ đợt đã PUBLISHED.
   * 4. Sắp xếp đợt mới nhất lên trước.
   */
  useEffect(() => {
    if (!selectedBranchId) {
      return
    }

    async function loadBranchPeriods() {
      try {
        const allPeriods = await getAllPeriods()

        const publishedPeriods = allPeriods
          .filter((period) => {
            const belongsToSelectedBranch =
              String(period.branchId) ===
              String(selectedBranchId)

            const isPublished =
              period.status === 'PUBLISHED'

            return (
              belongsToSelectedBranch &&
              isPublished
            )
          })
          .sort((firstPeriod, secondPeriod) => {
            return (
              new Date(secondPeriod.startDate) -
              new Date(firstPeriod.startDate)
            )
          })

        setPeriods(publishedPeriods)

        // Có đợt thì tự động chọn đợt đầu tiên.
        if (publishedPeriods.length > 0) {
          setSelectedPeriodId(
            publishedPeriods[0].id.toString()
          )
        } else {
          // Không có đợt thì xóa dữ liệu lịch cũ trên giao diện.
          setSelectedPeriodId('')
          setRegistrations([])
          setDates([])
        }
      } catch (error) {
        console.error(
          'Lỗi tải danh sách đợt lịch:',
          error
        )
      }
    }

    loadBranchPeriods()
  }, [selectedBranchId])

  /**
   * Khi Admin đổi cơ sở hoặc đổi đợt lịch:
   * 1. Lấy lịch làm chính thức của đợt.
   * 2. Lấy danh sách ca của cơ sở.
   * 3. Dùng while để tạo mảng ngày từ startDate đến endDate.
   */
  useEffect(() => {
    if (
      !selectedBranchId ||
      !selectedPeriodId
    ) {
      return
    }

    async function loadOfficialSchedule() {
      setLoading(true)

      try {
        const selectedPeriod = periods.find(
          (period) => {
            return (
              period.id.toString() ===
              selectedPeriodId
            )
          }
        )

        if (!selectedPeriod) {
          return
        }

        // Chạy song song hai API:
        // - Lấy lịch chính thức của đợt.
        // - Lấy toàn bộ ca làm.
        const [scheduleRows, shiftRows] =
          await Promise.all([
            getFinalScheduleByPeriod(
              selectedPeriod.id
            ),
            getAllShifts()
          ])

        // Lịch chính thức đã gồm cả Manager và Staff.
        setRegistrations(
          Array.isArray(scheduleRows)
            ? scheduleRows
            : []
        )

        // Chỉ giữ các ca thuộc cơ sở Admin đang chọn.
        const branchShifts = (shiftRows || [])
          .filter((shift) => {
            return (
              String(shift.branchId) ===
              String(selectedBranchId)
            )
          })

        setShifts(branchShifts)

        // =====================================================
        // VÒNG WHILE: TẠO DANH SÁCH CÁC NGÀY CỦA TUẦN
        // =====================================================
        // Ví dụ:
        // startDate = 03/08/2026
        // endDate   = 09/08/2026
        //
        // while sẽ lần lượt thêm:
        // 03/08, 04/08, ..., 09/08 vào dateArray.
        const dateArray = []

        let currentDate = new Date(
          selectedPeriod.startDate
        )

        const endDate = new Date(
          selectedPeriod.endDate
        )

        // Còn chưa vượt qua ngày kết thúc thì tiếp tục lặp.
        while (currentDate <= endDate) {
          // Phải tạo new Date để lưu một bản sao của ngày hiện tại.
          dateArray.push(
            new Date(currentDate)
          )

          // Tăng ngày hiện tại thêm 1 ngày.
          currentDate.setDate(
            currentDate.getDate() + 1
          )
        }

        // Lưu mảng ngày vào state.
        // State này sẽ được dates.map() dùng để tạo các hàng của bảng.
        setDates(dateArray)
      } catch (error) {
        console.error(
          'Lỗi tải lịch làm chính thức:',
          error
        )
      } finally {
        setLoading(false)
      }
    }

    loadOfficialSchedule()
  }, [
    selectedPeriodId,
    selectedBranchId,
    periods
  ])

  /**
   * Chuyển Date thành chuỗi yyyy-MM-dd.
   * Ví dụ: Date của ngày 03/08/2026 -> "2026-08-03".
   */
  function toDateString(dateObj) {
    const offset = dateObj.getTimezoneOffset()

    const normalizedDate = new Date(
      dateObj.getTime() -
      offset * 60 * 1000
    )

    return normalizedDate
      .toISOString()
      .split('T')[0]
  }

  // =====================================================
  // TẠO MA TRẬN DỮ LIỆU THEO NGÀY VÀ CA
  // =====================================================
  // Cấu trúc kết quả:
  // boardMatrix[ngày][shiftId] = danh sách người làm.
  //
  // Ví dụ:
  // boardMatrix['2026-08-03'][1]
  // là danh sách người làm ngày 03/08 ở ca có id = 1.
  const boardMatrix = {}

  // forEach thứ nhất: đi lần lượt qua từng ngày.
  dates.forEach((dateObj) => {
    const dateString = toDateString(dateObj)

    // Tạo một object riêng cho ngày hiện tại.
    boardMatrix[dateString] = {}

    // forEach thứ hai: trong ngày hiện tại,
    // đi lần lượt qua từng ca làm.
    shifts.forEach((shift) => {
      // Lọc danh sách lịch chính thức để lấy đúng:
      // - ngày đang xét;
      // - ca đang xét.
      const rowsOfCurrentCell =
        registrations.filter((row) => {
          const sameDate =
            row.workDate?.slice(0, 10) ===
            dateString

          const sameShift =
            Number(row.shiftId) ===
            Number(shift.id)

          return sameDate && sameShift
        })

      boardMatrix[dateString][shift.id] =
        rowsOfCurrentCell
    })
  })

  return (
    <div
      className="sd-card schedule-tabs schedule-tabs--admin"
      style={{
        padding: '20px 0'
      }}
    >
      <div
        style={{
          padding: '0 20px 16px',
          display: 'flex',
          flexDirection: 'column',
          gap: 12,
          borderBottom: '1px solid #f1f5f9',
          marginBottom: 16
        }}
      >
        <div
          className="sd-field"
          style={{
            marginBottom: 0
          }}
        >
          <label>
            1. Chọn cơ sở canteen giám sát:
          </label>

          <select
            value={selectedBranchId}
            onChange={(event) => {
              setSelectedBranchId(
                event.target.value
              )
            }}
          >
            {/*
              branches.map() là vòng lặp qua các cơ sở.
              Mỗi cơ sở tạo ra một thẻ <option>.
            */}
            {branches.map((branch) => {
              return (
                <option
                  key={branch.id}
                  value={branch.id}
                >
                  {branch.name}
                </option>
              )
            })}
          </select>
        </div>

        <div
          className="sd-field"
          style={{
            marginBottom: 0
          }}
        >
          <label>
            2. Chọn tuần làm việc đã chốt sổ:
          </label>

          <select
            value={selectedPeriodId}
            onChange={(event) => {
              setSelectedPeriodId(
                event.target.value
              )
            }}
            disabled={periods.length === 0}
          >
            {periods.length === 0 ? (
              <option value="">
                -- Canteen này chưa có lịch chốt chính thức --
              </option>
            ) : (
              /*
                periods.map() lặp qua các đợt.
                Mỗi đợt tạo một thẻ <option>.
              */
              periods.map((period) => {
                return (
                  <option
                    key={period.id}
                    value={period.id}
                  >
                    Từ {formatDate(period.startDate)} đến{' '}
                    {formatDate(period.endDate)}
                  </option>
                )
              })
            )}
          </select>
        </div>
      </div>

      <div
        style={{
          padding: '0 20px'
        }}
      >
        {loading ? (
          <p>
            Đang tải dữ liệu lịch làm việc...
          </p>
        ) : periods.length === 0 ? (
          <div
            className="sd-empty-state"
            style={{
              padding: '30px 0'
            }}
          >
            <span className="sd-empty-icon">
              🗓️
            </span>

            <p>
              Cơ sở này hiện chưa được Quản lý xuất bản
              (Publish) lịch làm việc.
            </p>
          </div>
        ) : (
          <div
            className="sd-board-wrap"
            style={{
              borderRadius: 12
            }}
          >
            <table className="sd-schedule-board">
              <thead>
                <tr>
                  {/* Cột cố định đầu tiên là cột ngày. */}
                  <th
                    style={{
                      width: 90
                    }}
                  >
                    NGÀY
                  </th>

                  {/*
                    shifts.map() ở phần thead:
                    - lặp qua từng ca;
                    - mỗi ca tạo ra một cột <th>.

                    Ví dụ có 3 ca thì tạo 3 cột:
                    Ca sáng | Ca chiều | Ca tối.
                  */}
                  {shifts.map((shift) => {
                    return (
                      <th key={shift.id}>
                        {shift.shiftName}

                        <br />

                        <span
                          style={{
                            fontWeight: 500,
                            fontSize: 11
                          }}
                        >
                          {shift.startTime?.slice(0, 5)}
                          {' - '}
                          {shift.endTime?.slice(0, 5)}
                        </span>
                      </th>
                    )
                  })}
                </tr>
              </thead>

              <tbody>
                {/*
                  dates.map() là vòng lặp ngoài:
                  - lặp qua từng ngày;
                  - mỗi ngày tạo ra một hàng <tr>.

                  Ví dụ dates có 7 ngày thì tạo 7 hàng.
                */}
                {dates.map((dateObj) => {
                  const dateString =
                    toDateString(dateObj)

                  const dayOfWeek =
                    DAY_NAMES[dateObj.getDay()]

                  const shortDate =
                    `${dateObj.getDate()}/${dateObj.getMonth() + 1}`

                  return (
                    <tr key={dateString}>
                      {/*
                        Ô đầu tiên của mỗi hàng hiển thị:
                        - thứ trong tuần;
                        - ngày/tháng.
                      */}
                      <td className="sd-board-date-col">
                        <strong>
                          {dayOfWeek}
                        </strong>

                        <small>
                          {shortDate}
                        </small>
                      </td>

                      {/*
                        shifts.map() nằm bên trong dates.map():
                        - với mỗi ngày, tiếp tục lặp qua tất cả ca;
                        - mỗi ca tạo ra một ô <td>.

                        Vì vậy:
                        số ô ca = số ngày × số ca.
                      */}
                      {shifts.map((shift) => {
                        // Lấy danh sách người làm đúng ngày và đúng ca.
                        const cellRows =
                          boardMatrix[dateString]
                            ?.[shift.id] || []

                        // Manager vẫn nằm trong lịch chính thức
                        // và chiếm một slot của ca.
                        const managerRow =
                          cellRows.find(
                            isManagerScheduleRow
                          )

                        // Danh sách Staff là các dòng
                        // không phải Manager.
                        const staffRows =
                          cellRows.filter((row) => {
                            return !isManagerScheduleRow(row)
                          })

                        // Không có ai trong ô thì xem như
                        // ngày đó không có ca làm chính thức.
                        const isShiftClosed =
                          cellRows.length === 0

                        const managerName =
                          managerRow
                            ? getScheduleUserName(
                                managerRow
                              )
                            : 'Quản lý ca'

                        return (
                          <td key={shift.id}>
                            {!isShiftClosed ? (
                              <>
                                {/* Hiển thị Manager trước. */}
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

                                {/*
                                  staffRows.map() lặp qua các Staff
                                  nằm trong đúng ô ngày và ca hiện tại.
                                  Mỗi Staff tạo ra một thẻ <div>.
                                */}
                                {staffRows.map((row) => {
                                  return (
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
                                  )
                                })}
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