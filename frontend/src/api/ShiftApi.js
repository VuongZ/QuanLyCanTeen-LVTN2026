import axios from 'axios'

const URL_SHIFT = '/api/Shift'

export async function getAllShifts() {
  const response = await axios.get(URL_SHIFT)
  return response.data
}

export async function createShift(shiftData) {
  const response = await axios.post(URL_SHIFT, shiftData)
  return response.data
}

export async function updateShift(id, shiftData) {
  await axios.put(`${URL_SHIFT}/${id}`, shiftData)
}

export async function deleteShift(id) {
  await axios.delete(`${URL_SHIFT}/${id}`)
}