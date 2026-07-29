import axios from 'axios';

const BASE_URL = '/api/Dashboard';

export async function getWorkHoursRanking({ year, month, branchId }) {
  const response = await axios.get(`${BASE_URL}/work-hours-ranking`, {
    params: {
      year,
      month: month || undefined,
      branchId: branchId && branchId !== 'ALL' ? branchId : undefined,
    },
  });

  return response.data;
}
