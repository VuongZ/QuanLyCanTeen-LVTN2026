import axios from 'axios'

const URL_SHIFT = '/api/Shift'

export async function getAllShifts(includeInactive = false) {
  const response = await axios.get(URL_SHIFT, {
    params: includeInactive ? { includeInactive: true } : {},
  })

  return response.data
}

export async function createShift(shiftData) {
  const response = await axios.post(URL_SHIFT, shiftData)
  return response.data
}

export async function updateShift(id, shiftData) {
  const response = await axios.put(`${URL_SHIFT}/${id}`, shiftData)
  return response.data
}

export async function deactivateShift(id, reason = '') {
  const response = await axios.patch(
    `${URL_SHIFT}/${id}/deactivate`,
    { reason }
  )

  return response.data
}

export async function restoreShift(id) {
  const response = await axios.patch(
    `${URL_SHIFT}/${id}/restore`
  )

  return response.data
}