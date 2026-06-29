import axios from 'axios';

const BASE_URL = '/api/Salary';

export async function getSalaryByUser(userId) {
  const response = await axios.get(`${BASE_URL}/user/${userId}`);
  return response.data;
}
