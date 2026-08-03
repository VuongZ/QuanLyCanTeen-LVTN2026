import axios from 'axios'

/**
 * File API riêng cho nghiệp vụ chấm công.
 *
 * Frontend không cần gửi ManagerId.
 * Backend lấy ManagerId từ JWT.
 */
export async function scanAttendance(payload) {
  const safePayload = {
    employeeId:
      Number(payload.employeeId),

    shiftId:
      Number(payload.shiftId),

    workDate:
      payload.workDate,

    action:
      String(payload.action || '')
        .trim()
        .toUpperCase()
  }

  const response = await axios.post(
    '/api/Attendance/scan',
    safePayload
  )

  return response.data
}

export async function getDailyAttendanceHistory(workDate, shiftId) {
  const response = await axios.get(
    '/api/Attendance/history/daily',
    {
      params: {
        workDate,
        shiftId: shiftId ? Number(shiftId) : undefined
      }
    }
  )

  return response.data
}
