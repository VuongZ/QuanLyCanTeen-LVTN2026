import axios from 'axios';

const BASE_URL = '/api/KhoImport';

// Gửi ảnh hóa đơn lên Backend để hệ thống OCR
// và nhận diện nhà cung cấp, mã hóa đơn,
// ngày hóa đơn cùng danh sách sản phẩm.
export async function parseInvoiceImage(file) {
  if (!file) {
    throw new Error('Chưa chọn ảnh hóa đơn.');
  }

  const formData = new FormData();
  formData.append('file', file);

  const response = await axios.post(
    `${BASE_URL}/parse-invoice-image`,
    formData
  );

  return response.data;
}

// Gửi thông tin phiếu nhập kho lên Backend.
//
// Sau khi xử lý thành công, Backend sẽ:
// - Tạo phiếu nhập kho.
// - Tạo chi tiết phiếu nhập.
// - Cộng số lượng sản phẩm vào tồn kho chi nhánh.
export async function submitImportTicket(payload) {
  const response = await axios.post(
    `${BASE_URL}/submit-import`,
    payload
  );

  return response.data;
}

// Lấy danh sách lịch sử phiếu nhập kho.
//
// Admin có thể truyền branchId để lọc theo một cơ sở.
// Manager không cần truyền branchId vì Backend
// sẽ lấy chi nhánh từ thông tin trong token.
export async function getInventoryImportTickets(
  branchId = null
) {
  const params =
    branchId &&
    branchId !== 'ALL' &&
    Number(branchId) > 0
      ? { branchId }
      : {};

  const response = await axios.get(
    `${BASE_URL}/inventory-tickets`,
    {
      params,
    }
  );

  return response.data;
}

// Lấy thông tin chi tiết của một phiếu nhập kho.
//
// ticketId là mã phiếu cần xem.
// branchId dùng khi Admin đang lọc theo cơ sở.
export async function getInventoryImportTicketDetail(
  ticketId,
  branchId = null
) {
  if (!ticketId || Number(ticketId) <= 0) {
    throw new Error(
      'Mã phiếu nhập kho không hợp lệ.'
    );
  }

  const params =
    branchId &&
    branchId !== 'ALL' &&
    Number(branchId) > 0
      ? { branchId }
      : {};

  const response = await axios.get(
    `${BASE_URL}/inventory-tickets/${ticketId}`,
    {
      params,
    }
  );

  return response.data;
}