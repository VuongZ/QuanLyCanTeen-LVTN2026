import axios from 'axios'

const BASE_URL = '/api/Supplier'

export async function getAllSuppliers() {
  const response = await axios.get(BASE_URL)
  return response.data
}

export async function getDeletedSuppliers() {
  const response = await axios.get(`${BASE_URL}/deleted`)
  return response.data
}

export async function createSupplier(supplierData) {
  const response = await axios.post(BASE_URL, supplierData)
  return response.data
}

export async function updateSupplier(id, supplierData) {
  const response = await axios.put(`${BASE_URL}/${id}`, supplierData)
  return response.data
}

export async function deleteSupplier(id) {
  const response = await axios.delete(`${BASE_URL}/${id}`)
  return response.data
}

export async function restoreSupplier(id) {
  const response = await axios.patch(`${BASE_URL}/${id}/restore`)
  return response.data
}
