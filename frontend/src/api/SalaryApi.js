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

export async function getBranchSalaries() {
  const response = await axios.get(`${BASE_URL}/branch`);
  return response.data;
}

export async function markSalaryPaid(salaryId) {
  const response = await axios.put(`${BASE_URL}/${salaryId}/pay`);
  return response.data;
}

export async function markBranchSalaryTransferred(branchId, month, year) {
  const response = await axios.put(`${BASE_URL}/branch/${branchId}/period/${year}/${month}/transfer`);
  return response.data;
}

export async function getSalaryRuleAdjustments(month, year, branchId) {
  const response = await axios.get(`${BASE_URL}/rule-adjustments`, {
    params: { month, year, branchId: branchId || undefined },
  });
  return response.data;
}

export async function getSalaryWorkDetails(userId, month, year) {
  const response = await axios.get(
    `${BASE_URL}/user/${userId}/work-details`,
    {
      params: { month, year },
    },
  );

  return response.data;
}

export async function finalizeSalary(salaryId) {
  const response = await axios.put(`${BASE_URL}/${salaryId}/finalize`);
  return response.data;
}

export async function finalizeBranchSalaryPeriod(month, year) {
  const response = await axios.put(
    `${BASE_URL}/branch/period/${year}/${month}/finalize`,
  );
  return response.data;
}

export async function createSalaryComplaint(salaryId, content) {
  const response = await axios.post(`${BASE_URL}/${salaryId}/complaints`, { content });
  return response.data;
}

export async function getMySalaryComplaints() {
  const response = await axios.get(`${BASE_URL}/complaints/my`);
  return response.data;
}

export async function getBranchSalaryComplaints() {
  const response = await axios.get(`${BASE_URL}/complaints/branch`);
  return response.data;
}

export async function resolveSalaryComplaint(complaintId, responseText) {
  const response = await axios.put(
    `${BASE_URL}/complaints/${complaintId}/resolve`,
    { response: responseText },
  );
  return response.data;
}

export async function getSalaryAdjustmentHistory(userId, month, year) {
  const response = await axios.get(`${BASE_URL}/user/${userId}/adjustment-history`, {
    params: { month: month || undefined, year: year || undefined },
  });
  return response.data;
}

export async function getPendingSalaryAdjustments() {
  const response = await axios.get(`${BASE_URL}/adjustment-requests/pending`);
  return response.data;
}

export async function reviewSalaryAdjustment(adjustmentId, isApproved, reviewNote = '') {
  const response = await axios.put(
    `${BASE_URL}/adjustment-requests/${adjustmentId}/review`,
    { isApproved, reviewNote },
  );
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
