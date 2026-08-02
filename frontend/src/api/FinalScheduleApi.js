import axios from 'axios'

/**
 * File này tập trung toàn bộ API của LỊCH CHÍNH THỨC:
 *
 * - Công bố lịch.
 * - Lấy lịch đã công bố.
 * - Nghỉ có phép.
 * - Vắng không phép.
 * - Lấy WAITLIST.
 * - Chọn người thay.
 */

/**
 * Manager công bố lịch.
 *
 * approvedRegistrationIds được giữ trong payload để tương thích
 * DTO cũ. Backend hiện tự lấy tất cả phiếu REGISTERED.
 */
export async function publishSchedule(periodId) {
  const response = await axios.post(
    '/api/FinalSchedule/publish',
    {
      periodId: Number(periodId),
      approvedRegistrationIds: []
    }
  )

  return response.data
}

/**
 * Lấy lịch chính thức của một đợt.
 */
export async function getFinalScheduleByPeriod(
  periodId
) {
  const response = await axios.get(
    `/api/FinalSchedule/period/${periodId}`
  )

  return response.data
}

/**
 * Ghi nhận Staff nghỉ có phép.
 */
export async function markApprovedLeave(
  scheduleId,
  reason
) {
  const response = await axios.put(
    `/api/FinalSchedule/${scheduleId}/approved-leave`,
    {
      reason
    }
  )

  return response.data
}

/**
 * Ghi nhận Staff vắng không phép.
 */
export async function markAbsent(
  scheduleId,
  reason
) {
  const response = await axios.put(
    `/api/FinalSchedule/${scheduleId}/absent`,
    {
      reason
    }
  )

  return response.data
}

/**
 * Lấy Staff WAITLIST phù hợp với lịch cần thay.
 */
export async function getReplacementCandidates(
  scheduleId
) {
  const response = await axios.get(
    `/api/FinalSchedule/${scheduleId}/replacement-candidates`
  )

  return response.data
}

/**
 * Xác nhận chọn một phiếu WAITLIST vào thay ca.
 */
export async function assignEmergencyReplacement(
  scheduleId,
  replacementRegistrationId
) {
  const response = await axios.post(
    `/api/FinalSchedule/${scheduleId}/emergency-replacement`,
    {
      replacementRegistrationId:
        Number(replacementRegistrationId)
    }
  )

  return response.data
}