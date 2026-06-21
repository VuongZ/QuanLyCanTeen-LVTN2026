import axios from 'axios'

// 1. Lấy toàn bộ danh sách đợt đăng ký
export async function getAllPeriods() {
  const res = await axios.get(
    '/api/SchedulePeriod'
  )
  return res.data
}

// 2. Tạo mới một đợt đăng ký lịch làm
export async function createPeriod(payload) {
  const res = await axios.post(
    '/api/SchedulePeriod',
    payload
  )
  return res.data
}

// 3. Cập nhật thông tin đợt đăng ký
export async function updatePeriod(id, payload) {
  const res = await axios.put(
    `/api/SchedulePeriod/${id}`,
    payload
  )
  return res.data
}

// 4. Xóa một đợt đăng ký
export async function deletePeriod(id) {
  const res = await axios.delete(
    `/api/SchedulePeriod/${id}`
  )
  return res.data
}