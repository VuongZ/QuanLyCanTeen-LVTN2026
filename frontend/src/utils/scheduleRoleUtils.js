/**
 * Chuẩn hóa tên vai trò:
 * - Xóa dấu tiếng Việt.
 * - Xóa khoảng trắng thừa.
 * - Chuyển thành chữ in hoa.
 */
export function normalizeRoleName(value) {
  return String(value ?? '')
    .normalize('NFD')
    .replace(/[\u0300-\u036f]/g, '')
    .trim()
    .toUpperCase()
}

/**
 * Kiểm tra một dòng lịch có thuộc
 * tài khoản Manager hay không.
 *
 * Hỗ trợ nhiều cấu trúc JSON khác nhau
 * mà Backend có thể trả về.
 */
export function isManagerScheduleRow(row) {
  const roleName = normalizeRoleName(
    row?.user?.roleName ??
    row?.user?.role?.roleName ??
    row?.roleName ??
    row?.role?.roleName
  )

  return (
    roleName === 'MANAGER' ||
    roleName === 'QUAN LY' ||
    roleName.includes('MANAGER') ||
    roleName.includes('QUAN LY')
  )
}

/**
 * Lấy tên hiển thị của người dùng
 * trong một dòng lịch chính thức.
 */
export function getScheduleUserName(row) {
  return (
    row?.user?.fullName ||
    row?.user?.username ||
    row?.user?.email ||
    'Chưa rõ người dùng'
  )
}