import axios from 'axios'

const BASE_URL = '/api/StaffRegistration'

// Lấy danh sách Nhân viên đăng ký trong một đợt
export async function getRegistrationsByPeriod(periodId) {
  const response = await axios.get(
    `${BASE_URL}/period/${periodId}`
  )

  return response.data
}

// Lấy lịch chính thức của một đợt đã công bố
export async function getFinalScheduleByPeriod(periodId) {
  const response = await axios.get(
    `${BASE_URL}/final-schedule/period/${periodId}`
  )

  return response.data
}

// Công bố lịch làm chính thức
export async function publishSchedule(
  periodId,
  approvedRegistrationIds = []
) {
  const response = await axios.post(
    `${BASE_URL}/publish`,
    {
      periodId,
      approvedRegistrationIds
    }
  )

  return response.data
}