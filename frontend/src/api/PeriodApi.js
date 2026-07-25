import axios from 'axios'

const BASE_URL = '/api/SchedulePeriod'

// Lấy toàn bộ đợt đăng ký
export async function getAllPeriods() {
  const response = await axios.get(BASE_URL)
  return response.data
}

// Lấy các đợt đang mở
export async function getOpenPeriods() {
  const response = await axios.get(`${BASE_URL}/open`)
  return response.data
}

// Tạo đợt đăng ký
export async function createPeriod(payload) {
  const response = await axios.post(BASE_URL, payload)
  return response.data
}

// Cập nhật thông tin đợt
export async function updatePeriod(id, payload) {
  const response = await axios.put(`${BASE_URL}/${id}`, payload)
  return response.data
}

// Cập nhật trạng thái: OPEN hoặc CLOSED
export async function updatePeriodStatus(id, status) {
  const response = await axios.patch(`${BASE_URL}/${id}/status`, {
    status
  })

  return response.data
}

// Xóa đợt đăng ký
export async function deletePeriod(id) {
  const response = await axios.delete(`${BASE_URL}/${id}`)
  return response.data
}