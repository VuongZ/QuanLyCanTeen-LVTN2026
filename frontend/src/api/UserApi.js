import axios from 'axios'

const BASE_URL = '/api/User'

export async function getUserPageData() {
  const response = await axios.get(BASE_URL)
  return response.data
}

export async function getUserById(id) {
  const response = await axios.get(`${BASE_URL}/${id}`)
  return response.data
}

export async function updateUser(id, user) {
  await axios.put(`${BASE_URL}/${id}`, user)
}

export async function updateUserProfile(id, payload) {
  const response = await axios.put(`${BASE_URL}/${id}/profile`, payload)
  return response.data
}

export async function changePassword(id, payload) {
  const response = await axios.put(`${BASE_URL}/${id}/password`, payload)
  return response.data
}
