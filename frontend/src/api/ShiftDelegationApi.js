import axios from 'axios'

const BASE_URL = '/api/ShiftDelegation'

export async function getShiftDelegations(branchId) {
  const response = await axios.get(BASE_URL, {
    params: branchId ? { branchId } : undefined,
  })
  return response.data
}

export async function createShiftDelegation(payload) {
  const response = await axios.post(BASE_URL, payload)
  return response.data
}

export async function respondShiftDelegation(id, accept) {
  const response = await axios.put(`${BASE_URL}/${id}/respond`, { accept })
  return response.data
}

export async function revokeShiftDelegation(id) {
  const response = await axios.put(`${BASE_URL}/${id}/revoke`)
  return response.data
}

export async function markDelegatedAttendance(payload) {
  const response = await axios.post(`${BASE_URL}/attendance-status`, payload)
  return response.data
}
