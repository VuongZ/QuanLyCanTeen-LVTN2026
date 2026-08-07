import axios from 'axios'

const URL_BRANCH = '/api/Branch'

export async function getAllBranches(includeInactive = false) {
  const response = await axios.get(URL_BRANCH, {
    params: includeInactive ? { includeInactive: true } : {},
  })

  return response.data
}

export async function createBranch(branchData) {
  const response = await axios.post(URL_BRANCH, branchData)
  return response.data
}

export async function updateBranch(id, branchData) {
  const response = await axios.put(`${URL_BRANCH}/${id}`, branchData)
  return response.data
}

export async function deactivateBranch(id, reason = '') {
  const response = await axios.patch(
    `${URL_BRANCH}/${id}/deactivate`,
    { reason }
  )

  return response.data
}

export async function restoreBranch(id) {
  const response = await axios.patch(
    `${URL_BRANCH}/${id}/restore`
  )

  return response.data
}