import axios from 'axios';

const BASE_URL = '/api/Inventory';

export async function getInventory(branchId = null) {
  const params =
    branchId && Number(branchId) > 0
      ? { branchId }
      : {};

  const response = await axios.get(BASE_URL, {
    params,
  });

  return response.data;
}