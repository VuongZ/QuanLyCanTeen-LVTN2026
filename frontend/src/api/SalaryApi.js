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

export async function getSalaryRuleAdjustments(month, year, branchId) {
  const response = await axios.get(`${BASE_URL}/rule-adjustments`, {
    params: { month, year, branchId: branchId || undefined },
  });
  return response.data;
}

export async function updateSalaryRule(payload) {
  const response = await axios.put(`${BASE_URL}/rule`, payload);
  return response.data;
}

export async function applySalaryRuleAdjustment(payload, branchId) {
  const response = await axios.put(`${BASE_URL}/rule-adjustments/apply`, payload, {
    params: { branchId: branchId || undefined },
  });
  return response.data;
}

export async function addManualSalaryAdjustment(payload, branchId) {
  const response = await axios.put(`${BASE_URL}/rule-adjustments/manual`, payload, {
    params: { branchId: branchId || undefined },
  });
  return response.data;
}
