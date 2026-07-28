import axios from 'axios';

const BASE_URL = '/api/Supplier';

// Lấy danh sách các nhà phân phối đang hoạt động.
export async function getAllSuppliers() {
  const response = await axios.get(BASE_URL);

  return response.data;
}

// Lấy danh sách các nhà phân phối đã bị xóa mềm.
export async function getDeletedSuppliers() {
  const response = await axios.get(
    `${BASE_URL}/deleted`
  );

  return response.data;
}

// Tạo mới một nhà phân phối.
export async function createSupplier(supplierData) {
  const response = await axios.post(
    BASE_URL,
    supplierData
  );

  return response.data;
}

// Cập nhật thông tin của một nhà phân phối theo ID.
export async function updateSupplier(
  id,
  supplierData
) {
  const response = await axios.put(
    `${BASE_URL}/${id}`,
    supplierData
  );

  return response.data;
}

// Xóa mềm một nhà phân phối theo ID.
export async function deleteSupplier(id) {
  const response = await axios.delete(
    `${BASE_URL}/${id}`
  );

  return response.data;
}

// Khôi phục một nhà phân phối đã bị xóa mềm.
export async function restoreSupplier(id) {
  const response = await axios.patch(
    `${BASE_URL}/${id}/restore`
  );

  return response.data;
}