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