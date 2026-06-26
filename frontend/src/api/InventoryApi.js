import axios from 'axios';

const BASE_URL = '/api/Inventory';

// Lấy báo cáo tồn kho (có thể lọc theo cơ sở nếu truyền branchId)
export const getInventoryReport = async (branchId) => {
  const url = branchId ? `${BASE_URL}?branchId=${branchId}` : BASE_URL;
  const response = await axios.get(url);
  return response.data;
};