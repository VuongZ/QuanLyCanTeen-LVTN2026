import axios from 'axios';

const BASE_URL = '/api/KhoExport';

// Lấy danh sách ca làm chính thức của Manager
// có thể sử dụng để xuất hàng từ kho ra quầy.
export async function getAvailableExportSchedules(
  managerId
) {
  const params =
    managerId && Number(managerId) > 0
      ? { managerId }
      : {};

  const response = await axios.get(
    `${BASE_URL}/available-schedules`,
    {
      params,
    }
  );

  return response.data;
}

// Gửi phiếu xuất hàng từ kho ra quầy.
//
// Sau khi xử lý thành công, Backend sẽ:
// - Trừ số lượng trong kho chi nhánh.
// - Cộng số lượng vào tồn quầy.
// - Lưu phiếu xuất và chi tiết phiếu xuất.
export async function submitExportTicket(payload) {
  const response = await axios.post(
    `${BASE_URL}/submit-export`,
    payload
  );

  return response.data;
}

// Lấy danh sách lịch sử phiếu xuất hàng ra quầy.
//
// Admin có thể truyền branchId để lọc theo một cơ sở.
// Manager không cần truyền branchId vì Backend
// sẽ lấy chi nhánh từ thông tin trong token.
export async function getFrontStockExportTickets(
  branchId = null
) {
  const params =
    branchId &&
    branchId !== 'ALL' &&
    Number(branchId) > 0
      ? { branchId }
      : {};

  const response = await axios.get(
    `${BASE_URL}/front-stock-tickets`,
    {
      params,
    }
  );

  return response.data;
}

// Lấy thông tin chi tiết của một phiếu xuất ra quầy.
//
// ticketId là mã phiếu cần xem.
// branchId dùng khi Admin đang lọc theo cơ sở.
export async function getFrontStockExportTicketDetail(
  ticketId,
  branchId = null
) {
  if (!ticketId || Number(ticketId) <= 0) {
    throw new Error(
      'Mã phiếu xuất kho không hợp lệ.'
    );
  }

  const params =
    branchId &&
    branchId !== 'ALL' &&
    Number(branchId) > 0
      ? { branchId }
      : {};

  const response = await axios.get(
    `${BASE_URL}/front-stock-tickets/${ticketId}`,
    {
      params,
    }
  );

  return response.data;
}