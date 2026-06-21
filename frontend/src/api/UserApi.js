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