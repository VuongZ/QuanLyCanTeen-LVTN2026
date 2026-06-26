import axios from 'axios';

// Đường dẫn đã được sửa khớp với Route C# [Route("api/KhoSupplier")]
const BASE_URL = '/api/Supplier';

// 1. Lấy danh sách nhà cung cấp (Manager xem, Admin quản lý)
export const getAllSuppliers = async () => {
  const response = await axios.get(BASE_URL);
  return response.data;
};

// 2. Admin thêm mới nhà cung cấp
export const createSupplier = async (supplierData) => {
  const response = await axios.post(BASE_URL, supplierData);
  return response.data;
};

// 3. Admin cập nhật nhà cung cấp
export const updateSupplier = async (id, supplierData) => {
  const response = await axios.put(`${BASE_URL}/${id}`, supplierData);
  return response.data;
};

// 4. Admin xóa nhà cung cấp
export const deleteSupplier = async (id) => {
  const response = await axios.delete(`${BASE_URL}/${id}`);
  return response.data;
};