import axios from 'axios';

const BASE_URL = '/api/Salary';

export async function getSalaryByUser(userId) {
  const response = await axios.get(`${BASE_URL}/user/${userId}`);
  return response.data;
}

export async function getAllSalaries() {
  const response = await axios.get(BASE_URL);
  return response.data;
}

export async function markSalaryPaid(salaryId) {
  const response = await axios.put(`${BASE_URL}/${salaryId}/pay`);
  return response.data;
}
