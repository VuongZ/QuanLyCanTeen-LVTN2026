import axios from 'axios'

const BASE_URL = '/api/supplemental-attendance'

export async function getSupplementalCandidates(workDate) {
  const response = await axios.get(`${BASE_URL}/candidates`, { params: { workDate } })
  return response.data
}

export async function submitSupplementalAttendance(payload) {
  const response = await axios.post(BASE_URL, payload)
  return response.data
}

export async function getMySupplementalRequests() {
  const response = await axios.get(`${BASE_URL}/mine`)
  return response.data
}

export async function getSupplementalRequestsForReview() {
  const response = await axios.get(`${BASE_URL}/review`)
  return response.data
}

export async function approveSupplementalRequest(id) {
  const response = await axios.put(`${BASE_URL}/${id}/approve`)
  return response.data
}

export async function rejectSupplementalRequest(id, reason) {
  const response = await axios.put(`${BASE_URL}/${id}/reject`, { reason })
  return response.data
}
