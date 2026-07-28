import axios from 'axios';

const BASE_URL = '/api/FrontStock';

// Lấy danh sách tồn quầy.
//
// Admin có thể truyền branchId để xem một cơ sở cụ thể.
// Manager không cần truyền branchId vì Backend
// sẽ lấy cơ sở từ thông tin trong token.
export async function getFrontStock(
  branchId = null
) {
  const params =
    branchId && Number(branchId) > 0
      ? { branchId }
      : {};

  const response = await axios.get(
    BASE_URL,
    {
      params,
    }
  );

  return response.data;
}