import axios from 'axios';

/*
  Đường dẫn gốc của toàn bộ API BHXH.

  Controller Backend đang dùng:

  [Route("api/[controller]")]

  Tên Controller:
  SocialInsuranceController

  Vì vậy đường dẫn gốc là:
  /api/SocialInsurance
*/
const BASE_URL = '/api/SocialInsurance';


// ============================================================
// 1. NHÂN VIÊN FULL_TIME
// ============================================================

/*
  Admin lấy danh sách nhân viên FULL_TIME.

  Kết quả còn cho biết:
  - Nhân viên đã có hồ sơ BHXH chưa.
  - Hồ sơ đang ở trạng thái nào.
*/
export async function getFullTimeEmployees() {
  const response = await axios.get(
    `${BASE_URL}/full-time-employees`
  );

  return response.data;
}


// ============================================================
// 2. CẤU HÌNH TỶ LỆ BHXH
// ============================================================

/*
  Admin lấy toàn bộ lịch sử cấu hình tỷ lệ.
*/
export async function getAllSocialInsuranceRates() {
  const response = await axios.get(
    `${BASE_URL}/rates`
  );

  return response.data;
}

/*
  Admin tạo cấu hình tỷ lệ mới.

  payload có dạng:

  {
    employeeRate: 8,
    employerRate: 17.5,
    effectiveFrom: '2026-01-01',
    effectiveTo: null
  }
*/
export async function createSocialInsuranceRate(
  payload
) {
  const response = await axios.post(
    `${BASE_URL}/rates`,
    payload
  );

  return response.data;
}

/**
 * Admin cập nhật một cấu hình tỷ lệ BHXH.
 *
 * Backend chỉ cho phép sửa khi:
 * - Cấu hình vẫn đang hoạt động.
 * - Chưa đến ngày bắt đầu hiệu lực.
 * - Chưa được dùng để sinh khoản đóng.
 *
 * PUT:
 * /api/SocialInsurance/rates/{rateConfigId}
 */
export async function updateSocialInsuranceRate(
  rateConfigId,
  payload
) {
  const normalizedRateConfigId =
    Number(rateConfigId);

  if (
    !Number.isInteger(
      normalizedRateConfigId
    ) ||
    normalizedRateConfigId <= 0
  ) {
    throw new Error(
      'Mã cấu hình tỷ lệ không hợp lệ.'
    );
  }

  const response =
    await axios.put(
      `/api/SocialInsurance/rates/${normalizedRateConfigId}`,
      {
        employeeRate:
          Number(payload.employeeRate),

        employerRate:
          Number(payload.employerRate),

        effectiveFrom:
          payload.effectiveFrom,

        effectiveTo:
          payload.effectiveTo || null
      }
    );

  return response.data;
}

/*
  Admin ngừng sử dụng cấu hình tỷ lệ.

  Không xóa bản ghi khỏi database.

  payload có dạng:

  {
    effectiveTo: '2026-08-31'
  }
*/
export async function deactivateSocialInsuranceRate(
  rateConfigId,
  payload
) {
  const response = await axios.put(
    `${BASE_URL}/rates/${rateConfigId}/deactivate`,
    payload
  );

  return response.data;
}


// ============================================================
// 3. HỒ SƠ BHXH — ADMIN
// ============================================================

/*
  Admin lấy toàn bộ hồ sơ BHXH.
*/
export async function getAllSocialInsuranceProfiles() {
  const response = await axios.get(
    `${BASE_URL}/profiles`
  );

  return response.data;
}

/*
  Admin lấy hồ sơ theo ID của hồ sơ.
*/
export async function getSocialInsuranceProfileById(
  profileId
) {
  const response = await axios.get(
    `${BASE_URL}/profiles/${profileId}`
  );

  return response.data;
}

/*
  Admin lấy hồ sơ theo ID nhân viên.
*/
export async function getSocialInsuranceProfileByUserId(
  userId
) {
  const response = await axios.get(
    `${BASE_URL}/profiles/user/${userId}`
  );

  return response.data;
}

/*
  Admin tạo hồ sơ BHXH cho nhân viên FULL_TIME.

  payload có dạng:

  {
    userId: 13,
    socialInsuranceNumber: 'TEST0000000013',
    insuranceSalaryBasis: 6000000,
    startDate: '2026-08-01',
    endDate: null,
    note: 'Hồ sơ BHXH'
  }

  Hồ sơ mới sẽ có trạng thái PENDING.
*/
export async function createSocialInsuranceProfile(
  payload
) {
  const response = await axios.post(
    `${BASE_URL}/profiles`,
    payload
  );

  return response.data;
}

/*
  Admin cập nhật thông tin hồ sơ.

  Không có userId trong payload vì không được
  chuyển hồ sơ của nhân viên này sang nhân viên khác.

  payload có dạng:

  {
    socialInsuranceNumber: 'TEST0000000013',
    insuranceSalaryBasis: 6500000,
    startDate: '2026-08-01',
    endDate: null,
    note: 'Đã cập nhật mức lương căn cứ'
  }
*/
export async function updateSocialInsuranceProfile(
  profileId,
  payload
) {
  const response = await axios.put(
    `${BASE_URL}/profiles/${profileId}`,
    payload
  );

  return response.data;
}

/*
  Admin cập nhật trạng thái hồ sơ.

  payload có dạng:

  {
    status: 'ACTIVE',
    note: 'Hồ sơ đã được kiểm tra'
  }

  Trạng thái hợp lệ:
  - PENDING
  - ACTIVE
  - SUSPENDED
  - STOPPED
*/
export async function updateSocialInsuranceProfileStatus(
  profileId,
  payload
) {
  const response = await axios.put(
    `${BASE_URL}/profiles/${profileId}/status`,
    payload
  );

  return response.data;
}


// ============================================================
// 4. KHOẢN ĐÓNG BHXH — ADMIN
// ============================================================

/*
  Admin sinh khoản đóng cho một tháng.

  payload có dạng:

  {
    month: 8,
    year: 2026
  }

  Chỉ hồ sơ ACTIVE đủ điều kiện mới được sinh khoản đóng.
*/
export async function generateSocialInsuranceContributions(
  payload
) {
  const response = await axios.post(
    `${BASE_URL}/contributions/generate`,
    payload
  );

  return response.data;
}

/*
  Admin lấy danh sách khoản đóng theo tháng và năm.

  Axios sẽ tạo đường dẫn:

  /api/SocialInsurance/contributions?month=8&year=2026
*/
export async function getSocialInsuranceContributionsByPeriod(
  month,
  year
) {
  const response = await axios.get(
    `${BASE_URL}/contributions`,
    {
      params: {
        month,
        year
      }
    }
  );

  return response.data;
}

/*
  Admin lấy chi tiết một khoản đóng.
*/
export async function getSocialInsuranceContributionById(
  contributionId
) {
  const response = await axios.get(
    `${BASE_URL}/contributions/${contributionId}`
  );

  return response.data;
}

/*
  Admin xem lịch sử đóng BHXH của một nhân viên.
*/
export async function getSocialInsuranceContributionsByUserId(
  userId
) {
  const response = await axios.get(
    `${BASE_URL}/contributions/user/${userId}`
  );

  return response.data;
}

/*
  Admin xác nhận khoản đóng:

  DRAFT → CONFIRMED

  API không cần request body.
*/
export async function confirmSocialInsuranceContribution(
  contributionId
) {
  const response = await axios.put(
    `${BASE_URL}/contributions/${contributionId}/confirm`,
    {}
  );

  return response.data;
}

/*
  Admin đánh dấu khoản đóng đã được nộp:

  CONFIRMED → PAID

  API không cần request body.
*/
export async function markSocialInsuranceContributionPaid(
  contributionId
) {
  const response = await axios.put(
    `${BASE_URL}/contributions/${contributionId}/paid`,
    {}
  );

  return response.data;
}

/*
  Admin hủy khoản đóng bị tạo sai.

  Không xóa cứng bản ghi.

  payload có dạng:

  {
    reason: 'Tạo sai mức lương căn cứ'
  }
*/
export async function cancelSocialInsuranceContribution(
  contributionId,
  payload
) {
  const response = await axios.put(
    `${BASE_URL}/contributions/${contributionId}/cancel`,
    payload
  );

  return response.data;
}


// ============================================================
// 5. BHXH CỦA CHÍNH STAFF
// ============================================================

/*
  Staff xem hồ sơ BHXH của chính mình.

  Không truyền userId vì Backend lấy UserId từ JWT.
*/
export async function getMySocialInsuranceProfile() {
  const response = await axios.get(
    `${BASE_URL}/my-profile`
  );

  return response.data;
}

/*
  Staff xem lịch sử đóng BHXH của chính mình.

  Không truyền userId vì Backend lấy UserId từ JWT.
*/
export async function getMySocialInsuranceContributions() {
  const response = await axios.get(
    `${BASE_URL}/my-contributions`
  );

  return response.data;
}

/*
  Staff xác nhận hoặc yêu cầu Admin chỉnh sửa
  hồ sơ BHXH của chính mình.

  Không truyền userId vì Backend lấy UserId từ JWT.

  payload xác nhận:

  {
    confirmationStatus: 'CONFIRMED',
    note: null
  }

  payload yêu cầu chỉnh sửa:

  {
    confirmationStatus: 'CHANGE_REQUESTED',
    note: 'Mã số BHXH của tôi chưa chính xác.'
  }
*/
export async function
  updateMySocialInsuranceProfileConfirmation(
    payload
  ) {
  const normalizedStatus =
    String(
      payload?.confirmationStatus || ''
    )
      .trim()
      .toUpperCase();

  const allowedStatuses = [
    'CONFIRMED',
    'CHANGE_REQUESTED'
  ];

  if (
    !allowedStatuses.includes(
      normalizedStatus
    )
  ) {
    throw new Error(
      'Trạng thái xác nhận hồ sơ không hợp lệ.'
    );
  }

  const normalizedNote =
    typeof payload?.note === 'string'
      ? payload.note.trim()
      : '';

  // Khi yêu cầu chỉnh sửa,
  // Staff bắt buộc phải nhập nội dung.
  if (
    normalizedStatus ===
      'CHANGE_REQUESTED' &&
    !normalizedNote
  ) {
    throw new Error(
      'Vui lòng nhập nội dung cần Admin chỉnh sửa.'
    );
  }

  const response =
    await axios.put(
      `${BASE_URL}/my-profile/confirmation`,
      {
        confirmationStatus:
          normalizedStatus,

        // CONFIRMED không cần gửi ghi chú.
        note:
          normalizedStatus ===
            'CHANGE_REQUESTED'
            ? normalizedNote
            : null
      }
    );

  return response.data;
}