// File: src/api/BranchApi.js
import axios from 'axios'

const URL_BRANCH = '/api/Branch'

export async function getAllBranches() {
  const response = await axios.get(URL_BRANCH)
  return response.data
}

export async function createBranch(branchData) {
  const response = await axios.post(URL_BRANCH, branchData)
  return response.data
}

export async function updateBranch(id, branchData) {
  await axios.put(`${URL_BRANCH}/${id}`, branchData)
}

export async function deleteBranch(id) {
  await axios.delete(`${URL_BRANCH}/${id}`)
}