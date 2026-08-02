import axios from 'axios'

/**
 * File này CHỈ gọi API liên quan đến phiếu đăng ký ca.
 *
 * Không đặt các API sau trong file này:
 * - Công bố lịch.
 * - Lấy lịch chính thức.
 * - Nghỉ/vắng và thay ca.
 * - Quét QR chấm công.
 */

/**
 * Staff đăng ký một ca.
 *
 * Backend tự lấy UserId từ JWT và tự quyết định
 * REGISTERED hoặc WAITLIST.
 */
export async function registerShift(payload) {
  const response = await axios.post(
    '/api/StaffRegistration',
    payload
  )

  return response.data
}

/**
 * Lấy các phiếu đăng ký của một đợt.
 */
export async function getRegistrationsByPeriod(
  periodId
) {
  const response = await axios.get(
    `/api/StaffRegistration/period/${periodId}`
  )

  return response.data
}

/**
 * Manager/Admin hủy một phiếu.
 *
 * Backend hiện nhận raw JSON string nên Axios sẽ gửi:
 * "CANCELLED"
 */
export async function updateRegistrationStatus(
  registrationId,
  newStatus
) {
  const response = await axios.put(
    `/api/StaffRegistration/${registrationId}/status`,
    newStatus,
    {
      headers: {
        'Content-Type': 'application/json'
      }
    }
  )

  return response.data
}

/**
 * Staff tự hủy REGISTERED hoặc WAITLIST.
 */
export async function cancelRegistration(
  registrationId,
  userId
) {
  const response = await axios.delete(
    `/api/StaffRegistration/${registrationId}/user/${userId}`
  )

  return response.data
}