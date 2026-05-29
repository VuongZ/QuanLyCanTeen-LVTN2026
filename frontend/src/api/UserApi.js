const BASE_URL = '/api/User'

export async function getUserPageData() {
  const response = await fetch(BASE_URL)

  if (!response.ok) {
    throw new Error(`Khong the lay du lieu User: ${response.status}`)
  }

  return response.json()
}

export async function getUserById(id) {
  const response = await fetch(`${BASE_URL}/${id}`)

  if (!response.ok) {
    throw new Error(`Khong the lay User id ${id}: ${response.status}`)
  }

  return response.json()
}

export async function updateUser(id, user) {
  const response = await fetch(`${BASE_URL}/${id}`, {
    method: 'PUT',
    headers: {
      'Content-Type': 'application/json',
    },
    body: JSON.stringify(user),
  })

  if (!response.ok) {
    throw new Error(`Khong the cap nhat User id ${id}: ${response.status}`)
  }
}
