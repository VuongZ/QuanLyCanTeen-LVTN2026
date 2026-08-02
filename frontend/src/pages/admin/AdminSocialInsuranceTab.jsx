import {
  useEffect,
  useMemo,
  useState
} from 'react';

import {
  // Nhân viên và hồ sơ BHXH.
  getFullTimeEmployees,
  getAllSocialInsuranceProfiles,
  createSocialInsuranceProfile,
  updateSocialInsuranceProfile,
  updateSocialInsuranceProfileStatus,

  // Cấu hình tỷ lệ BHXH.
  getAllSocialInsuranceRates,
  createSocialInsuranceRate,
  updateSocialInsuranceRate,
  deactivateSocialInsuranceRate
} from '../../api/SocialInsuranceApi';

import '../css/SocialInsuranceTab.css';



// ============================================================
// CÁC GIÁ TRỊ TRẠNG THÁI
// ============================================================

const PROFILE_STATUS = {
  PENDING: 'PENDING',
  ACTIVE: 'ACTIVE',
  SUSPENDED: 'SUSPENDED',
  STOPPED: 'STOPPED'
};

const STATUS_DISPLAY = {
  PENDING: {
    label: 'Chờ hoàn tất',
    className: 'bhxh-status--pending'
  },

  ACTIVE: {
    label: 'Đang tham gia',
    className: 'bhxh-status--active'
  },

  SUSPENDED: {
    label: 'Tạm ngừng',
    className: 'bhxh-status--suspended'
  },

  STOPPED: {
    label: 'Đã kết thúc',
    className: 'bhxh-status--stopped'
  }
};


// ============================================================
// CÁC HÀM HỖ TRỢ
// ============================================================

/*
  Lấy nội dung lỗi do Backend trả về.

  Backend thường trả:

  {
    message: "Nội dung lỗi"
  }
*/
function getApiErrorMessage(
  error,
  fallbackMessage
) {
  const responseData =
    error?.response?.data;

  if (
    typeof responseData === 'string' &&
    responseData.trim()
  ) {
    return responseData;
  }

  return (
    responseData?.message ||
    error?.message ||
    fallbackMessage
  );
}


/*
  Chuẩn hóa trạng thái thành chữ in hoa.
*/
function normalizeStatus(status) {
  return String(status || '')
    .trim()
    .toUpperCase();
}


/*
  Hiển thị tiền theo định dạng Việt Nam.

  Ví dụ:

  6000000
  → 6.000.000 ₫
*/
function formatMoney(value) {
  return new Intl.NumberFormat(
    'vi-VN',
    {
      style: 'currency',
      currency: 'VND',
      maximumFractionDigits: 0
    }
  ).format(
    Number(value || 0)
  );
}

/*
  Hiển thị tỷ lệ phần trăm.

  Ví dụ:
  8    → 8%
  17.5 → 17,5%
*/
function formatPercent(value) {
  const numericValue =
    Number(value || 0);

  return (
    new Intl.NumberFormat(
      'vi-VN',
      {
        minimumFractionDigits: 0,
        maximumFractionDigits: 2
      }
    ).format(numericValue) + '%'
  );
}

/*
  Chuyển EmploymentType sang tên dễ đọc.
*/
function formatEmploymentType(value) {
  const normalizedValue =
    String(value || '')
      .trim()
      .toUpperCase();

  if (normalizedValue === 'FULL_TIME') {
    return 'Full-time';
  }

  if (normalizedValue === 'PART_TIME') {
    return 'Part-time';
  }

  return value || '—';
}


/*
  Chuyển ngày:

  2026-08-01
  → 01/08/2026
*/
function formatDate(value) {
  if (!value) {
    return '—';
  }

  const normalizedValue =
    String(value).slice(0, 10);

  const [
    year,
    month,
    day
  ] = normalizedValue
    .split('-')
    .map(Number);

  if (!year || !month || !day) {
    return '—';
  }

  return new Intl.DateTimeFormat(
    'vi-VN'
  ).format(
    new Date(
      year,
      month - 1,
      day
    )
  );
}


/*
  Lấy ngày hiện tại theo múi giờ Việt Nam.

  Kết quả:

  YYYY-MM-DD
*/
function getVietnamToday() {
  const parts =
    new Intl.DateTimeFormat(
      'en-US',
      {
        timeZone: 'Asia/Ho_Chi_Minh',
        year: 'numeric',
        month: '2-digit',
        day: '2-digit'
      }
    ).formatToParts(
      new Date()
    );

  const values =
    Object.fromEntries(
      parts.map((part) => [
        part.type,
        part.value
      ])
    );

  return (
    `${values.year}-` +
    `${values.month}-` +
    `${values.day}`
  );
}


/*
  Component hiển thị trạng thái hồ sơ.
*/
function StatusBadge({
  status
}) {
  const normalizedStatus =
    normalizeStatus(status);

  const display =
    STATUS_DISPLAY[normalizedStatus];

  return (
    <span
      className={
        `bhxh-status ` +
        (
          display?.className ||
          'bhxh-status--none'
        )
      }
    >
      {display?.label || 'Chưa có hồ sơ'}
    </span>
  );
}


// ============================================================
// COMPONENT CHÍNH
// ============================================================

export function AdminSocialInsuranceTab() {
  // Danh sách nhân viên FULL_TIME.
  const [
    employees,
    setEmployees
  ] = useState([]);

  // Danh sách hồ sơ BHXH.
  const [
    profiles,
    setProfiles
  ] = useState([]);

  // Danh sách cấu hình tỷ lệ BHXH.
const [
  rates,
  setRates
] = useState([]);

/*
  true:
  → đang mở modal tạo cấu hình tỷ lệ.

  false:
  → modal đang đóng.
*/
const [
  rateModal,
  setRateModal
] = useState(false);

/*
  Cấu hình tỷ lệ đang được chỉnh sửa.

  null:
  → Modal đang ở chế độ tạo mới.

  Có dữ liệu:
  → Modal đang ở chế độ chỉnh sửa.
*/
const [
  selectedRate,
  setSelectedRate
] = useState(null);

/*
  Cấu hình đang được chọn để ngừng sử dụng.

  null:
  → không mở modal.
*/
const [
  deactivateRateModal,
  setDeactivateRateModal
] = useState(null);

// Lỗi trong form tạo tỷ lệ.
const [
  rateFormError,
  setRateFormError
] = useState('');

// Lỗi trong modal ngừng cấu hình.
const [
  deactivateRateError,
  setDeactivateRateError
] = useState('');

// Form tạo cấu hình tỷ lệ mới.
const [
  rateForm,
  setRateForm
] = useState({
  employeeRate: '8',
  employerRate: '17.5',
  effectiveFrom: '',
  effectiveTo: ''
});

// Ngày kết thúc của cấu hình bị ngừng.
const [
  deactivateEffectiveTo,
  setDeactivateEffectiveTo
] = useState('');

  // Từ khóa tìm kiếm.
  const [
    searchText,
    setSearchText
  ] = useState('');

  // Trạng thái tải dữ liệu.
  const [
    loading,
    setLoading
  ] = useState(true);

  // Trạng thái đang gửi request lưu.
  const [
    saving,
    setSaving
  ] = useState(false);

  /*
    Giá trị của profileModal:

    null
    → không mở modal.

    create
    → tạo hồ sơ mới.

    edit
    → cập nhật hồ sơ.
  */
  const [
    profileModal,
    setProfileModal
  ] = useState(null);

  // Nhân viên đang được tạo hồ sơ.
  const [
    selectedEmployee,
    setSelectedEmployee
  ] = useState(null);

  // Hồ sơ đang được chỉnh sửa.
  const [
    selectedProfile,
    setSelectedProfile
  ] = useState(null);

  /*
    Modal chuyển trạng thái có dạng:

    {
      profile: {...},
      targetStatus: "ACTIVE"
    }
  */
  const [
    statusModal,
    setStatusModal
  ] = useState(null);

  // Thông báo chung của trang.
  const [
    message,
    setMessage
  ] = useState(null);

  // Lỗi hiển thị trong modal hồ sơ.
  const [
    profileFormError,
    setProfileFormError
  ] = useState('');

  // Lỗi hiển thị trong modal trạng thái.
  const [
    statusFormError,
    setStatusFormError
  ] = useState('');

  // Form tạo hoặc cập nhật hồ sơ.
  const [
    profileForm,
    setProfileForm
  ] = useState({
    socialInsuranceNumber: '',
    insuranceSalaryBasis: '',
    startDate: '',
    endDate: '',
    note: ''
  });

  // Ghi chú khi đổi trạng thái.
  const [
    statusNote,
    setStatusNote
  ] = useState('');


  // ==========================================================
  // TẢI DỮ LIỆU
  // ==========================================================

  /*
    Tải đồng thời:
  - Nhân viên FULL_TIME.
  - Hồ sơ BHXH.
  - Lịch sử cấu hình tỷ lệ BHXH.
*/
async function loadData() {
  setLoading(true);
  setMessage(null);

  try {
    const [
      employeeData,
      profileData,
      rateData
    ] = await Promise.all([
      getFullTimeEmployees(),

      getAllSocialInsuranceProfiles(),

      getAllSocialInsuranceRates()
    ]);

    setEmployees(
      Array.isArray(employeeData)
        ? employeeData
        : []
    );

    setProfiles(
      Array.isArray(profileData)
        ? profileData
        : []
    );

    setRates(
      Array.isArray(rateData)
        ? rateData
        : []
    );
  } catch (error) {
    setEmployees([]);
    setProfiles([]);
    setRates([]);

    setMessage({
      type: 'error',

      text: getApiErrorMessage(
        error,
        'Không tải được dữ liệu BHXH.'
      )
    });
  } finally {
    setLoading(false);
  }
}


  /*
    Tự động tải dữ liệu khi Admin mở tab.
  */
  useEffect(() => {
    loadData();
  }, []);


  // ==========================================================
  // XỬ LÝ DANH SÁCH
  // ==========================================================

  /*
    Chuyển danh sách hồ sơ thành Map:

    UserId → Profile
  */
  const profileByUserId =
    useMemo(() => {
      const result =
        new Map();

      profiles.forEach(
        (profile) => {
          result.set(
            String(profile.userId),
            profile
          );
        }
      );

      return result;
    }, [
      profiles
    ]);


  /*
    Ghép từng nhân viên với hồ sơ BHXH tương ứng.
  */
  const employeeRows =
    useMemo(() => {
      const normalizedSearch =
        searchText
          .trim()
          .toLowerCase();

      return employees
        .map((employee) => {
          const profile =
            profileByUserId.get(
              String(employee.userId)
            ) || null;

          return {
            ...employee,
            profile
          };
        })
        .filter((row) => {
          if (!normalizedSearch) {
            return true;
          }

          const searchableText = [
            row.fullName,
            row.email,
            row.phoneNumber,
            row.employmentType,
            row.profile
              ?.socialInsuranceNumber,
            row.profile
              ?.status
          ]
            .filter(Boolean)
            .join(' ')
            .toLowerCase();

          return searchableText.includes(
            normalizedSearch
          );
        });
    }, [
      employees,
      profileByUserId,
      searchText
    ]);


  /*
  Tính số liệu thống kê hồ sơ BHXH.
*/
const statistics =
  useMemo(() => {
    const activeCount =
      profiles.filter(
        (profile) =>
          normalizeStatus(
            profile.status
          ) ===
          PROFILE_STATUS.ACTIVE
      ).length;

    const pendingCount =
      profiles.filter(
        (profile) =>
          normalizeStatus(
            profile.status
          ) ===
          PROFILE_STATUS.PENDING
      ).length;

    return {
      fullTimeCount:
        employees.length,

      profileCount:
        profiles.length,

      activeCount,

      pendingCount
    };
  }, [
    employees,
    profiles
  ]);


/*
  Sắp xếp cấu hình có ngày hiệu lực
  mới nhất lên đầu bảng.
*/
const sortedRates =
  useMemo(() => {
    return [...rates].sort(
      (
        firstRate,
        secondRate
      ) => {
        return String(
          secondRate.effectiveFrom || ''
        ).localeCompare(
          String(
            firstRate.effectiveFrom || ''
          )
        );
      }
    );
  }, [
    rates
  ]);


/*
  Tìm cấu hình thực sự có hiệu lực
  tại ngày hiện tại.

  Không chỉ dựa vào isActive vì có thể tồn tại
  cấu hình đã được lập lịch cho tương lai.
*/
const activeRate =
  useMemo(() => {
    const today =
      getVietnamToday();

    return (
      sortedRates.find((rate) => {
        const effectiveFrom =
          String(
            rate.effectiveFrom || ''
          ).slice(0, 10);

        const effectiveTo =
          rate.effectiveTo
            ? String(
                rate.effectiveTo
              ).slice(0, 10)
            : null;

        return (
          Boolean(rate.isActive) &&
          effectiveFrom <= today &&
          (
            !effectiveTo ||
            effectiveTo >= today
          )
        );
      }) || null
    );
  }, [
    sortedRates
  ]);

// ==========================================================
// CẤU HÌNH TỶ LỆ BHXH
// ==========================================================

/*
  Mở modal ở chế độ tạo cấu hình mới.
*/
function openCreateRateModal() {
  /*
    Xóa cấu hình đang chọn để modal
    chuyển về chế độ tạo mới.
  */
  setSelectedRate(null);

  setRateForm({
    employeeRate: '8',
    employerRate: '17.5',
    effectiveFrom: getVietnamToday(),
    effectiveTo: ''
  });

  setRateFormError('');
  setMessage(null);
  setRateModal(true);
}


/*
  Mở modal ở chế độ chỉnh sửa cấu hình.
*/
function openEditRateModal(rate) {
  /*
    Frontend chỉ cho mở modal sửa khi
    Backend trả về canEdit = true.
  */
  if (!rate?.id || !rate.canEdit) {
    setMessage({
      type: 'error',
      text:
        'Cấu hình này không đủ điều kiện ' +
        'để chỉnh sửa trực tiếp.'
    });

    return;
  }

  // Ghi nhớ cấu hình đang sửa.
  setSelectedRate(rate);

  // Đưa dữ liệu hiện tại lên form.
  setRateForm({
    employeeRate:
      String(rate.employeeRate ?? ''),

    employerRate:
      String(rate.employerRate ?? ''),

    effectiveFrom:
      rate.effectiveFrom
        ? String(
            rate.effectiveFrom
          ).slice(0, 10)
        : '',

    effectiveTo:
      rate.effectiveTo
        ? String(
            rate.effectiveTo
          ).slice(0, 10)
        : ''
  });

  setRateFormError('');
  setMessage(null);
  setRateModal(true);
}


/*
  Đóng modal tạo hoặc chỉnh sửa cấu hình.
*/
function closeRateModal() {
  if (saving) {
    return;
  }

  setRateModal(false);

  // Xóa cấu hình đang được chỉnh sửa.
  setSelectedRate(null);

  setRateFormError('');
}


/*
  Kiểm tra form tạo cấu hình tỷ lệ.
*/
function validateRateForm() {
  const employeeRateText =
  String(
    rateForm.employeeRate ?? ''
  ).trim();

const employerRateText =
  String(
    rateForm.employerRate ?? ''
  ).trim();

if (!employeeRateText) {
  return 'Tỷ lệ nhân viên đóng là bắt buộc.';
}

if (!employerRateText) {
  return 'Tỷ lệ doanh nghiệp đóng là bắt buộc.';
}

const employeeRate =
  Number(employeeRateText);

const employerRate =
  Number(employerRateText);

  if (
    !Number.isFinite(employeeRate) ||
    employeeRate < 0 ||
    employeeRate > 100
  ) {
    return (
      'Tỷ lệ nhân viên phải nằm ' +
      'trong khoảng từ 0 đến 100.'
    );
  }

  if (
    !Number.isFinite(employerRate) ||
    employerRate < 0 ||
    employerRate > 100
  ) {
    return (
      'Tỷ lệ doanh nghiệp phải nằm ' +
      'trong khoảng từ 0 đến 100.'
    );
  }

  if (!rateForm.effectiveFrom) {
    return (
      'Ngày bắt đầu hiệu lực là bắt buộc.'
    );
  }
  /*
  Khi chỉnh sửa, ngày bắt đầu mới
  vẫn phải nằm trong tương lai.

  Backend cũng kiểm tra lại điều kiện này.
*/
if (
  selectedRate &&
  rateForm.effectiveFrom <=
    getVietnamToday()
) {
  return (
    'Ngày bắt đầu hiệu lực khi chỉnh sửa ' +
    'phải là một ngày trong tương lai.'
  );
}

  if (
    rateForm.effectiveTo &&
    rateForm.effectiveTo <
      rateForm.effectiveFrom
  ) {
    return (
      'Ngày kết thúc hiệu lực không được ' +
      'trước ngày bắt đầu hiệu lực.'
    );
  }

  return '';
}


/*
  Lưu cấu hình tỷ lệ.

  selectedRate có dữ liệu:
  → Cập nhật cấu hình.

  selectedRate là null:
  → Tạo cấu hình mới.
*/
async function handleSaveRate() {
  const validationError =
    validateRateForm();

  if (validationError) {
    setRateFormError(validationError);
    return;
  }

  const isEditing =
    Boolean(selectedRate?.id);

  const payload = {
    employeeRate:
      Number(rateForm.employeeRate),

    employerRate:
      Number(rateForm.employerRate),

    effectiveFrom:
      rateForm.effectiveFrom,

    effectiveTo:
      rateForm.effectiveTo || null
  };

  setSaving(true);
  setRateFormError('');

  try {
    let response;

    if (isEditing) {
      response =
        await updateSocialInsuranceRate(
          selectedRate.id,
          payload
        );
    } else {
      response =
        await createSocialInsuranceRate(
          payload
        );
    }

    setRateModal(false);
    setSelectedRate(null);

    await loadData();

    setMessage({
      type: 'success',

      text:
        response?.message ||
        (
          isEditing
            ? 'Đã cập nhật cấu hình tỷ lệ BHXH.'
            : 'Đã tạo cấu hình tỷ lệ BHXH.'
        )
    });
  } catch (error) {
    setRateFormError(
      getApiErrorMessage(
        error,
        isEditing
          ? (
              'Không cập nhật được cấu hình ' +
              'tỷ lệ BHXH.'
            )
          : (
              'Không tạo được cấu hình ' +
              'tỷ lệ BHXH.'
            )
      )
    );
  } finally {
    setSaving(false);
  }
}


/*
  Mở modal ngừng sử dụng một cấu hình.

  Mặc định lấy ngày hiện tại làm ngày kết thúc.
*/
function openDeactivateRateModal(rate) {
  const effectiveFrom =
    String(
      rate?.effectiveFrom || ''
    ).slice(0, 10);

  const today =
    getVietnamToday();

  setDeactivateEffectiveTo(
    today < effectiveFrom
      ? effectiveFrom
      : today
  );

  setDeactivateRateError('');
  setMessage(null);
  setDeactivateRateModal(rate);
}


/*
  Đóng modal ngừng cấu hình.
*/
function closeDeactivateRateModal() {
  if (saving) {
    return;
  }

  setDeactivateRateModal(null);
  setDeactivateEffectiveTo('');
  setDeactivateRateError('');
}


/*
  Ngừng sử dụng cấu hình tỷ lệ.

  Hệ thống không xóa bản ghi.
  Chỉ cập nhật:
  - isActive = false.
  - effectiveTo.
*/
async function handleDeactivateRate() {
  const rate =
    deactivateRateModal;

  if (!rate?.id) {
    setDeactivateRateError(
      'Không xác định được cấu hình tỷ lệ.'
    );

    return;
  }

  if (!deactivateEffectiveTo) {
    setDeactivateRateError(
      'Ngày kết thúc hiệu lực là bắt buộc.'
    );

    return;
  }

  const effectiveFrom =
    String(
      rate.effectiveFrom || ''
    ).slice(0, 10);

  if (
    deactivateEffectiveTo <
    effectiveFrom
  ) {
    setDeactivateRateError(
      'Ngày kết thúc không được trước ' +
      'ngày bắt đầu hiệu lực.'
    );

    return;
  }

  setSaving(true);
  setDeactivateRateError('');

  try {
    const response =
      await deactivateSocialInsuranceRate(
        rate.id,
        {
          effectiveTo:
            deactivateEffectiveTo
        }
      );

    setDeactivateRateModal(null);
    setDeactivateEffectiveTo('');

    await loadData();

    setMessage({
      type: 'success',

      text:
        response?.message ||
        'Đã ngừng sử dụng cấu hình tỷ lệ.'
    });
  } catch (error) {
    setDeactivateRateError(
      getApiErrorMessage(
        error,
        'Không ngừng được cấu hình tỷ lệ.'
      )
    );
  } finally {
    setSaving(false);
  }
}

  // ==========================================================
  // MODAL TẠO / CẬP NHẬT HỒ SƠ
  // ==========================================================

  /*
    Mở form tạo hồ sơ.

    Ngày bắt đầu mặc định:
    - Ngày tuyển dụng nếu có.
    - Nếu không có thì lấy ngày hiện tại.
  */
  function openCreateProfile(
    employee
  ) {
    setSelectedEmployee(employee);
    setSelectedProfile(null);

    setProfileForm({
      socialInsuranceNumber: '',
      insuranceSalaryBasis: '',

      startDate:
        employee.hireDate
          ? String(
              employee.hireDate
            ).slice(0, 10)
          : getVietnamToday(),

      endDate: '',
      note: ''
    });

    setProfileFormError('');
    setMessage(null);
    setProfileModal('create');
  }


  /*
    Mở form cập nhật hồ sơ.
  */
  function openEditProfile(
    profile
  ) {
    setSelectedEmployee(null);
    setSelectedProfile(profile);

    setProfileForm({
      socialInsuranceNumber:
        profile.socialInsuranceNumber ||
        '',

      insuranceSalaryBasis:
        String(
          profile.insuranceSalaryBasis ??
          ''
        ),

      startDate:
        profile.startDate
          ? String(
              profile.startDate
            ).slice(0, 10)
          : '',

      endDate:
        profile.endDate
          ? String(
              profile.endDate
            ).slice(0, 10)
          : '',

      note:
        profile.note ||
        ''
    });

    setProfileFormError('');
    setMessage(null);
    setProfileModal('edit');
  }


  /*
    Đóng modal hồ sơ.
  */
  function closeProfileModal() {
    if (saving) {
      return;
    }

    setProfileModal(null);
    setSelectedEmployee(null);
    setSelectedProfile(null);
    setProfileFormError('');
  }


  /*
    Kiểm tra dữ liệu form trước khi gửi Backend.
  */
  function validateProfileForm() {
    const salaryBasis =
      Number(
        profileForm
          .insuranceSalaryBasis
      );

    if (
      !Number.isFinite(salaryBasis) ||
      salaryBasis <= 0
    ) {
      return (
        'Mức lương làm căn cứ đóng ' +
        'phải lớn hơn 0.'
      );
    }

    if (!profileForm.startDate) {
      return (
        'Ngày bắt đầu tham gia ' +
        'là bắt buộc.'
      );
    }

    if (
      profileForm.endDate &&
      profileForm.endDate <
        profileForm.startDate
    ) {
      return (
        'Ngày kết thúc không được ' +
        'trước ngày bắt đầu.'
      );
    }

    return '';
  }


  /*
    Tạo mới hoặc cập nhật hồ sơ.
  */
  async function handleSaveProfile() {
    const validationError =
      validateProfileForm();

    if (validationError) {
      setProfileFormError(
        validationError
      );

      return;
    }

    const isCreating =
      profileModal === 'create';

    setSaving(true);
    setProfileFormError('');

    try {
      const commonPayload = {
        socialInsuranceNumber:
          profileForm
            .socialInsuranceNumber
            .trim() || null,

        insuranceSalaryBasis:
          Number(
            profileForm
              .insuranceSalaryBasis
          ),

        startDate:
          profileForm.startDate,

        endDate:
          profileForm.endDate ||
          null,

        note:
          profileForm.note
            .trim() || null
      };

      let response;

      if (isCreating) {
        if (!selectedEmployee?.userId) {
          throw new Error(
            'Không xác định được nhân viên.'
          );
        }

        response =
          await createSocialInsuranceProfile({
            userId:
              selectedEmployee.userId,

            ...commonPayload
          });
      } else {
        if (!selectedProfile?.id) {
          throw new Error(
            'Không xác định được hồ sơ BHXH.'
          );
        }

        response =
          await updateSocialInsuranceProfile(
            selectedProfile.id,
            commonPayload
          );
      }

      setProfileModal(null);
      setSelectedEmployee(null);
      setSelectedProfile(null);

      await loadData();

      setMessage({
        type: 'success',

        text:
          response?.message ||
          (
            isCreating
              ? 'Đã tạo hồ sơ BHXH.'
              : 'Đã cập nhật hồ sơ BHXH.'
          )
      });
    } catch (error) {
      setProfileFormError(
        getApiErrorMessage(
          error,
          'Không lưu được hồ sơ BHXH.'
        )
      );
    } finally {
      setSaving(false);
    }
  }


  // ==========================================================
  // MODAL ĐỔI TRẠNG THÁI
  // ==========================================================

  /*
    Mở modal đổi trạng thái.
  */
  function openStatusModal(
    profile,
    targetStatus
  ) {
    setStatusModal({
      profile,
      targetStatus
    });

    setStatusNote('');
    setStatusFormError('');
    setMessage(null);
  }


  /*
    Đóng modal đổi trạng thái.
  */
  function closeStatusModal() {
    if (saving) {
      return;
    }

    setStatusModal(null);
    setStatusNote('');
    setStatusFormError('');
  }


  /*
    Cập nhật trạng thái hồ sơ.

    Không xóa cứng hồ sơ khỏi database.
  */
  async function handleUpdateStatus() {
    const profile =
      statusModal?.profile;

    const targetStatus =
      statusModal?.targetStatus;

    if (
      !profile?.id ||
      !targetStatus
    ) {
      setStatusFormError(
        'Không xác định được hồ sơ hoặc trạng thái mới.'
      );

      return;
    }

    setSaving(true);
    setStatusFormError('');

    try {
      const response =
        await updateSocialInsuranceProfileStatus(
          profile.id,
          {
            status:
              targetStatus,

            note:
              statusNote
                .trim() ||
              null
          }
        );

      setStatusModal(null);
      setStatusNote('');

      await loadData();

      setMessage({
        type: 'success',

        text:
          response?.message ||
          'Đã cập nhật trạng thái hồ sơ.'
      });
    } catch (error) {
      setStatusFormError(
        getApiErrorMessage(
          error,
          'Không cập nhật được trạng thái hồ sơ.'
        )
      );
    } finally {
      setSaving(false);
    }
  }


  // ==========================================================
  // GIAO DIỆN
  // ==========================================================

  return (
    <div className="bhxh-page">
      {/* ================================================== */}
      {/* GIỚI THIỆU */}
      {/* ================================================== */}

      <div className="sd-card bhxh-hero-card">
        <div className="sd-card-header bhxh-hero-header">
          <div>
            <p className="sd-eyebrow">
              Bảo hiểm xã hội
            </p>

            <h2>
              Quản lý hồ sơ nhân viên
            </h2>

            <p className="bhxh-description">
              Chỉ quản lý hồ sơ BHXH của nhân viên FULL_TIME.
            </p>
          </div>

          <button
            type="button"
            className="sd-btn-ghost"
            disabled={loading}
            onClick={loadData}
          >
            {loading
              ? 'Đang tải...'
              : '↻ Làm mới'}
          </button>
        </div>
      </div>


      {/* ================================================== */}
      {/* THÔNG BÁO */}
      {/* ================================================== */}

      {message && (
        <div
          role="alert"
          className={
            `bhxh-alert ` +
            `bhxh-alert--${message.type}`
          }
        >
          {message.text}
        </div>
      )}


      {/* ================================================== */}
      {/* THỐNG KÊ */}
      {/* ================================================== */}

      <div className="bhxh-stats">
        <div className="sd-card bhxh-stat-card">
          <p className="bhxh-stat-label">
            Nhân viên FULL_TIME
          </p>

          <strong className="bhxh-stat-value">
            {statistics.fullTimeCount}
          </strong>
        </div>

        <div className="sd-card bhxh-stat-card">
          <p className="bhxh-stat-label">
            Đã có hồ sơ
          </p>

          <strong className="bhxh-stat-value">
            {statistics.profileCount}
          </strong>
        </div>

        <div className="sd-card bhxh-stat-card">
          <p className="bhxh-stat-label">
            Đang tham gia
          </p>

          <strong
            className={
              'bhxh-stat-value ' +
              'bhxh-stat-value--active'
            }
          >
            {statistics.activeCount}
          </strong>
        </div>

        <div className="sd-card bhxh-stat-card">
          <p className="bhxh-stat-label">
            Chờ hoàn tất
          </p>

          <strong
            className={
              'bhxh-stat-value ' +
              'bhxh-stat-value--pending'
            }
          >
            {statistics.pendingCount}
          </strong>
        </div>
      </div>


      {/* ================================================== */}
      {/* DANH SÁCH NHÂN VIÊN */}
      {/* ================================================== */}

      <div className="sd-card bhxh-list-card">
        <div className="sd-card-header bhxh-list-header">
          <div>
            <p className="sd-eyebrow">
              Hồ sơ tham gia
            </p>

            <h2>
              Danh sách nhân viên
            </h2>
          </div>

          <input
            className="bhxh-search"
            type="search"
            value={searchText}
            placeholder="Tìm tên, email, mã số BHXH..."
            onChange={(event) => {
              setSearchText(
                event.target.value
              );
            }}
          />
        </div>

        {loading ? (
          <p className="bhxh-loading">
            Đang tải dữ liệu BHXH...
          </p>
        ) : (
          <div className="sd-table-wrap">
            <table className="sd-table bhxh-table">
              <thead>
                <tr>
                  <th>
                    Nhân viên
                  </th>

                  <th>
                    Loại Nhân viên
                  </th>

                  <th>
                    Trạng thái
                  </th>

                  <th>
                    Mã số BHXH
                  </th>

                  <th>
                    Lương căn cứ
                  </th>

                  <th>
                    Thời gian tham gia
                  </th>

                  <th className="sd-text-right">
                    Thao tác
                  </th>
                </tr>
              </thead>

              <tbody>
                {employeeRows.map(
                  (employee) => {
                    const profile =
                      employee.profile;

                    const status =
                      normalizeStatus(
                        profile?.status
                      );

                    return (
                      <tr
                        key={
                          employee.userId
                        }
                      >
                        {/* NHÂN VIÊN */}
                        <td>
                          <strong className="bhxh-employee-name">
                            {employee.fullName ||
                              'Chưa có tên'}
                          </strong>

                          <span className="bhxh-secondary-text">
                            {employee.email ||
                              'Chưa có email'}
                          </span>

                          {employee.phoneNumber && (
                            <span className="bhxh-secondary-text">
                              {employee.phoneNumber}
                            </span>
                          )}
                        </td>

                        {/* LOẠI NHÂN VIÊN */}
                        <td>
                          <strong>
                            {formatEmploymentType(
                              employee.employmentType
                            )}
                          </strong>

                          <span className="bhxh-secondary-text">
                            Tuyển dụng:{' '}
                            {formatDate(
                              employee.hireDate
                            )}
                          </span>
                        </td>

                        {/* TRẠNG THÁI */}
                        <td>
                          <StatusBadge
                            status={
                              profile?.status
                            }
                          />
                        </td>

                        {/* MÃ SỐ BHXH */}
                        <td>
                          <span className="bhxh-insurance-number">
                            {profile
                              ?.socialInsuranceNumber ||
                              '—'}
                          </span>
                        </td>

                        {/* LƯƠNG CĂN CỨ */}
                        <td>
                          <span className="bhxh-salary-basis">
                            {profile
                              ? formatMoney(
                                  profile
                                    .insuranceSalaryBasis
                                )
                              : '—'}
                          </span>
                        </td>

                        {/* THỜI GIAN */}
                        <td>
                          {profile ? (
                            <>
                              <div>
                                Từ:{' '}

                                <strong>
                                  {formatDate(
                                    profile.startDate
                                  )}
                                </strong>
                              </div>

                              <span className="bhxh-secondary-text">
                                Đến:{' '}

                                {profile.endDate
                                  ? formatDate(
                                      profile.endDate
                                    )
                                  : 'Chưa xác định'}
                              </span>
                            </>
                          ) : (
                            '—'
                          )}
                        </td>

                        {/* THAO TÁC */}
                        <td>
                          <div className="bhxh-actions">
                            {!profile && (
                              <button
                                type="button"
                                className="sd-btn-primary"
                                disabled={saving}
                                onClick={() => {
                                  openCreateProfile(
                                    employee
                                  );
                                }}
                              >
                                Tạo hồ sơ
                              </button>
                            )}

                            {profile && (
                              <button
                                type="button"
                                className={
                                  'sd-action-btn ' +
                                  'sd-action-edit'
                                }
                                title="Chỉnh sửa hồ sơ"
                                disabled={saving}
                                onClick={() => {
                                  openEditProfile(
                                    profile
                                  );
                                }}
                              >
                                ✎
                              </button>
                            )}

                            {status ===
                              PROFILE_STATUS.PENDING && (
                              <button
                                type="button"
                                className="sd-btn-primary"
                                disabled={saving}
                                onClick={() => {
                                  openStatusModal(
                                    profile,
                                    PROFILE_STATUS.ACTIVE
                                  );
                                }}
                              >
                                Kích hoạt
                              </button>
                            )}

                            {status ===
                              PROFILE_STATUS.ACTIVE && (
                              <>
                                <button
                                  type="button"
                                  className="sd-btn-ghost"
                                  disabled={saving}
                                  onClick={() => {
                                    openStatusModal(
                                      profile,
                                      PROFILE_STATUS.SUSPENDED
                                    );
                                  }}
                                >
                                  Tạm ngừng
                                </button>

                                <button
                                  type="button"
                                  className="sd-btn-ghost"
                                  disabled={saving}
                                  onClick={() => {
                                    openStatusModal(
                                      profile,
                                      PROFILE_STATUS.STOPPED
                                    );
                                  }}
                                >
                                  Kết thúc
                                </button>
                              </>
                            )}

                            {status ===
                              PROFILE_STATUS.SUSPENDED && (
                              <>
                                <button
                                  type="button"
                                  className="sd-btn-primary"
                                  disabled={saving}
                                  onClick={() => {
                                    openStatusModal(
                                      profile,
                                      PROFILE_STATUS.ACTIVE
                                    );
                                  }}
                                >
                                  Kích hoạt lại
                                </button>

                                <button
                                  type="button"
                                  className="sd-btn-ghost"
                                  disabled={saving}
                                  onClick={() => {
                                    openStatusModal(
                                      profile,
                                      PROFILE_STATUS.STOPPED
                                    );
                                  }}
                                >
                                  Kết thúc
                                </button>
                              </>
                            )}
                          </div>
                        </td>
                      </tr>
                    );
                  }
                )}

                {employeeRows.length === 0 && (
                  <tr>
                    <td
                      colSpan={7}
                      className="bhxh-empty"
                    >
                      Không tìm thấy nhân viên FULL_TIME phù hợp.
                    </td>
                  </tr>
                )}
              </tbody>
            </table>
          </div>
        )}
      </div>

      {/* ================================================== */}
{/* CẤU HÌNH TỶ LỆ BHXH */}
{/* ================================================== */}

<div className="sd-card bhxh-rate-card">
  <div className="sd-card-header bhxh-rate-header">
    <div>
      <p className="sd-eyebrow">
        Cấu hình đóng BHXH
      </p>

      <h2>
        Tỷ lệ đóng theo thời gian
      </h2>

      <p className="bhxh-description">
        Tỷ lệ được sử dụng khi sinh khoản
        đóng BHXH hằng tháng.
      </p>
    </div>

    <button
      type="button"
      className={
        'sd-btn-primary ' +
        'bhxh-rate-create-button'
      }
      disabled={saving}
      onClick={openCreateRateModal}
    >
      ＋ Tạo cấu hình mới
    </button>
  </div>

  {activeRate && (
    <div className="bhxh-current-rate">
      <div>
        <span>
          Nhân viên đóng
        </span>

        <strong>
          {formatPercent(
            activeRate.employeeRate
          )}
        </strong>
      </div>

      <div>
        <span>
          Doanh nghiệp đóng
        </span>

        <strong>
          {formatPercent(
            activeRate.employerRate
          )}
        </strong>
      </div>

      <div>
        <span>
          Tổng tỷ lệ
        </span>

        <strong>
          {formatPercent(
            Number(
              activeRate.employeeRate || 0
            ) +
            Number(
              activeRate.employerRate || 0
            )
          )}
        </strong>
      </div>

      <div>
        <span>
          Hiệu lực từ
        </span>

        <strong>
          {formatDate(
            activeRate.effectiveFrom
          )}
        </strong>
      </div>
    </div>
  )}

  {!activeRate && !loading && (
    <div className="bhxh-rate-warning">
      Hiện chưa có cấu hình tỷ lệ BHXH
      đang hoạt động. Hệ thống sẽ không thể
      sinh khoản đóng mới nếu không tìm thấy
      cấu hình phù hợp.
    </div>
  )}

  {loading ? (
    <p className="bhxh-loading">
      Đang tải cấu hình tỷ lệ...
    </p>
  ) : (
    <div className="sd-table-wrap">
      <table className="sd-table bhxh-rate-table">
        <thead>
          <tr>
            <th>
              Nhân viên đóng
            </th>

            <th>
              Doanh nghiệp đóng
            </th>

            <th>
              Tổng tỷ lệ
            </th>

            <th>
              Hiệu lực từ
            </th>

            <th>
              Hiệu lực đến
            </th>

            <th>
              Trạng thái
            </th>

            <th>
              Người tạo
            </th>

            <th className="sd-text-right">
              Thao tác
            </th>
          </tr>
        </thead>

        <tbody>
          {sortedRates.map((rate) => {
            const totalRate =
              Number(
                rate.employeeRate || 0
              ) +
              Number(
                rate.employerRate || 0
              );

            return (
              <tr key={rate.id}>
                <td>
                  <strong className="bhxh-rate-employee">
                    {formatPercent(
                      rate.employeeRate
                    )}
                  </strong>
                </td>

                <td>
                  <strong className="bhxh-rate-employer">
                    {formatPercent(
                      rate.employerRate
                    )}
                  </strong>
                </td>

                <td>
                  <strong>
                    {formatPercent(totalRate)}
                  </strong>
                </td>

                <td>
                  {formatDate(
                    rate.effectiveFrom
                  )}
                </td>

                <td>
                  {rate.effectiveTo
                    ? formatDate(
                        rate.effectiveTo
                      )
                    : 'Chưa xác định'}
                </td>

                <td>
                  <span
                    className={
                      rate.isActive
                        ? (
                            'bhxh-rate-status ' +
                            'bhxh-rate-status--active'
                          )
                        : (
                            'bhxh-rate-status ' +
                            'bhxh-rate-status--inactive'
                          )
                    }
                  >
                    {rate.isActive
                      ? 'Đang áp dụng'
                      : 'Đã ngừng'}
                  </span>
                </td>

                <td>
                  {rate.createdByUserName ||
                    'Admin'}

                  <span className="bhxh-secondary-text">
                    Tạo lúc:{' '}
                    {formatDate(rate.createdAt)}
                  </span>
                </td>

                <td>
  <div className="bhxh-rate-actions">
    {/* Chỉ cấu hình đủ điều kiện mới có nút sửa. */}
    {rate.canEdit && (
      <button
        type="button"
        className="sd-btn-ghost"
        disabled={saving}
        onClick={() => {
          openEditRateModal(rate);
        }}
      >
        Chỉnh sửa
      </button>
    )}

    {/* Cấu hình đang hoạt động có thể ngừng áp dụng. */}
    {rate.isActive ? (
      <button
        type="button"
        className="sd-btn-ghost"
        disabled={saving}
        onClick={() => {
          openDeactivateRateModal(rate);
        }}
      >
        Ngừng áp dụng
      </button>
    ) : (
      <span className="bhxh-secondary-text">
        Không có thao tác
      </span>
    )}
  </div>
</td>
              </tr>
            );
          })}

          {sortedRates.length === 0 && (
            <tr>
              <td
                colSpan={8}
                className="bhxh-empty"
              >
                Chưa có cấu hình tỷ lệ BHXH.
              </td>
            </tr>
          )}
        </tbody>
      </table>
    </div>
  )}
</div>


{/* ================================================== */}
{/* MODAL TẠO CẤU HÌNH TỶ LỆ */}
{/* ================================================== */}

{rateModal && (
  <div
    className="sd-overlay"
    onClick={closeRateModal}
  >
    <div
      className="sd-modal"
      onClick={(event) => {
        event.stopPropagation();
      }}
    >
      <div className="sd-modal-header">
        <div>
         <h2>
  {selectedRate
    ? 'Chỉnh sửa cấu hình tỷ lệ BHXH'
    : 'Tạo cấu hình tỷ lệ BHXH'}
</h2>

          <span className="bhxh-secondary-text">
            Tỷ lệ nhập theo đơn vị phần trăm
          </span>
        </div>

        <button
          type="button"
          disabled={saving}
          onClick={closeRateModal}
        >
          ✕
        </button>
      </div>

      <div className="sd-modal-body">
        <div className="sd-modal-grid">
          <div className="sd-field">
            <label>
              Tỷ lệ nhân viên đóng (%) *
            </label>

            <input
              type="number"
              min="0"
              max="100"
              step="0.01"
              value={rateForm.employeeRate}
              placeholder="Ví dụ: 8"
              onChange={(event) => {
                setRateForm(
                  (current) => ({
                    ...current,

                    employeeRate:
                      event.target.value
                  })
                );
              }}
            />
          </div>

          <div className="sd-field">
            <label>
              Tỷ lệ doanh nghiệp đóng (%) *
            </label>

            <input
              type="number"
              min="0"
              max="100"
              step="0.01"
              value={rateForm.employerRate}
              placeholder="Ví dụ: 17.5"
              onChange={(event) => {
                setRateForm(
                  (current) => ({
                    ...current,

                    employerRate:
                      event.target.value
                  })
                );
              }}
            />
          </div>

          <div className="sd-field">
            <label>
              Ngày bắt đầu hiệu lực *
            </label>

            <input
              type="date"
              value={rateForm.effectiveFrom}
              onChange={(event) => {
                setRateForm(
                  (current) => ({
                    ...current,

                    effectiveFrom:
                      event.target.value
                  })
                );
              }}
            />
          </div>

          <div className="sd-field">
            <label>
              Ngày kết thúc hiệu lực
            </label>

            <input
              type="date"
              min={
                rateForm.effectiveFrom ||
                undefined
              }
              value={rateForm.effectiveTo}
              onChange={(event) => {
                setRateForm(
                  (current) => ({
                    ...current,

                    effectiveTo:
                      event.target.value
                  })
                );
              }}
            />
          </div>
        </div>

        <div className="bhxh-rate-preview">
          <div>
            <span>
              Nhân viên đóng
            </span>

            <strong>
              {formatPercent(
                rateForm.employeeRate
              )}
            </strong>
          </div>

          <div>
            <span>
              Doanh nghiệp đóng
            </span>

            <strong>
              {formatPercent(
                rateForm.employerRate
              )}
            </strong>
          </div>

          <div>
            <span>
              Tổng tỷ lệ
            </span>

            <strong>
              {formatPercent(
                Number(
                  rateForm.employeeRate || 0
                ) +
                Number(
                  rateForm.employerRate || 0
                )
              )}
            </strong>
          </div>
        </div>

        <p className="bhxh-modal-note bhxh-modal-note--info">
  {selectedRate
    ? (
        <>
          Chỉ cấu hình chưa đến ngày hiệu lực
          và chưa được dùng để sinh khoản đóng
          mới được chỉnh sửa.
        </>
      )
    : (
        <>
          Cấu hình mới chỉ ảnh hưởng đến các
          khoản đóng được sinh sau đó. Các khoản
          đóng đã tạo vẫn giữ tỷ lệ đã lưu trước đây.
        </>
      )}
</p>

        {rateFormError && (
          <p className="sd-status sd-status-error">
            {rateFormError}
          </p>
        )}
      </div>

      <div className="sd-modal-footer">
        <button
          type="button"
          className="sd-btn-ghost"
          disabled={saving}
          onClick={closeRateModal}
        >
          Hủy
        </button>

        <button
  type="button"
  className="sd-btn-primary"
  disabled={saving}
  onClick={handleSaveRate}
>
  {saving
    ? (
        selectedRate
          ? 'Đang cập nhật...'
          : 'Đang tạo...'
      )
    : (
        selectedRate
          ? 'Lưu thay đổi'
          : 'Tạo cấu hình'
      )}
</button>
      </div>
    </div>
  </div>
)}

{/* ================================================== */}
{/* MODAL NGỪNG CẤU HÌNH TỶ LỆ */}
{/* ================================================== */}

{deactivateRateModal && (
  <div
    className="sd-overlay"
    onClick={closeDeactivateRateModal}
  >
    <div
      className="sd-modal"
      onClick={(event) => {
        event.stopPropagation();
      }}
    >
      <div className="sd-modal-header">
        <div>
          <h2>
            Ngừng áp dụng cấu hình
          </h2>

          <span className="bhxh-secondary-text">
            Nhân viên{' '}
            {formatPercent(
              deactivateRateModal.employeeRate
            )}
            {' · '}
            Doanh nghiệp{' '}
            {formatPercent(
              deactivateRateModal.employerRate
            )}
          </span>
        </div>

        <button
          type="button"
          disabled={saving}
          onClick={
            closeDeactivateRateModal
          }
        >
          ✕
        </button>
      </div>

      <div className="sd-modal-body">
        <div className="sd-field">
          <label>
            Ngày cuối cùng còn hiệu lực *
          </label>

          <input
            type="date"
            min={
              String(
                deactivateRateModal
                  .effectiveFrom || ''
              ).slice(0, 10)
            }
            value={deactivateEffectiveTo}
            onChange={(event) => {
              setDeactivateEffectiveTo(
                event.target.value
              );
            }}
          />
        </div>

        <p className="bhxh-modal-note bhxh-modal-note--warning">
          Cấu hình sẽ không bị xóa khỏi hệ thống.
          Các khoản đóng đã sử dụng cấu hình này
          vẫn được giữ nguyên.
        </p>

        {deactivateRateError && (
          <p className="sd-status sd-status-error">
            {deactivateRateError}
          </p>
        )}
      </div>

      <div className="sd-modal-footer">
        <button
          type="button"
          className="sd-btn-ghost"
          disabled={saving}
          onClick={
            closeDeactivateRateModal
          }
        >
          Hủy
        </button>

        <button
          type="button"
          className={
            'sd-btn-primary ' +
            'bhxh-danger-button'
          }
          disabled={saving}
          onClick={handleDeactivateRate}
        >
          {saving
            ? 'Đang cập nhật...'
            : 'Xác nhận ngừng'}
        </button>
      </div>
    </div>
  </div>
)}


      {/* ================================================== */}
      {/* MODAL TẠO / CẬP NHẬT HỒ SƠ */}
      {/* ================================================== */}

      {profileModal && (
        <div
          className="sd-overlay"
          onClick={
            closeProfileModal
          }
        >
          <div
            className="sd-modal"
            onClick={(event) => {
              event.stopPropagation();
            }}
          >
            <div className="sd-modal-header">
              <div>
                <h2>
                  {profileModal ===
                  'create'
                    ? 'Tạo hồ sơ BHXH'
                    : 'Cập nhật hồ sơ BHXH'}
                </h2>

                <span className="bhxh-secondary-text">
                  {profileModal ===
                  'create'
                    ? selectedEmployee
                        ?.fullName
                    : selectedProfile
                        ?.fullName}
                </span>
              </div>

              <button
                type="button"
                disabled={saving}
                onClick={
                  closeProfileModal
                }
              >
                ✕
              </button>
            </div>

            <div className="sd-modal-body">
              <div className="sd-modal-grid">
                <div className="sd-field">
                  <label>
                    Mã số BHXH
                  </label>

                  <input
                    type="text"
                    maxLength={20}
                    value={
                      profileForm
                        .socialInsuranceNumber
                    }
                    placeholder="Có thể để trống khi PENDING"
                    onChange={(event) => {
                      setProfileForm(
                        (current) => ({
                          ...current,

                          socialInsuranceNumber:
                            event.target.value
                        })
                      );
                    }}
                  />
                </div>

                <div className="sd-field">
                  <label>
                    Mức lương làm căn cứ *
                  </label>

                  <input
                    type="number"
                    min="1"
                    step="1000"
                    value={
                      profileForm
                        .insuranceSalaryBasis
                    }
                    placeholder="Ví dụ: 6000000"
                    onChange={(event) => {
                      setProfileForm(
                        (current) => ({
                          ...current,

                          insuranceSalaryBasis:
                            event.target.value
                        })
                      );
                    }}
                  />
                </div>

                <div className="sd-field">
                  <label>
                    Ngày bắt đầu *
                  </label>

                  <input
                    type="date"
                    value={
                      profileForm.startDate
                    }
                    onChange={(event) => {
                      setProfileForm(
                        (current) => ({
                          ...current,

                          startDate:
                            event.target.value
                        })
                      );
                    }}
                  />
                </div>

                <div className="sd-field">
                  <label>
                    Ngày kết thúc
                  </label>

                  <input
                    type="date"
                    min={
                      profileForm.startDate ||
                      undefined
                    }
                    value={
                      profileForm.endDate
                    }
                    onChange={(event) => {
                      setProfileForm(
                        (current) => ({
                          ...current,

                          endDate:
                            event.target.value
                        })
                      );
                    }}
                  />
                </div>
              </div>

              <div className="sd-field">
                <label>
                  Ghi chú
                </label>

                <textarea
                  rows={4}
                  maxLength={500}
                  value={
                    profileForm.note
                  }
                  placeholder="Thông tin bổ sung về hồ sơ..."
                  onChange={(event) => {
                    setProfileForm(
                      (current) => ({
                        ...current,

                        note:
                          event.target.value
                      })
                    );
                  }}
                />
              </div>

              {profileFormError && (
                <p className="sd-status sd-status-error">
                  {profileFormError}
                </p>
              )}
            </div>

            <div className="sd-modal-footer">
              <button
                type="button"
                className="sd-btn-ghost"
                disabled={saving}
                onClick={
                  closeProfileModal
                }
              >
                Hủy
              </button>

              <button
                type="button"
                className="sd-btn-primary"
                disabled={saving}
                onClick={
                  handleSaveProfile
                }
              >
                {saving
                  ? 'Đang lưu...'
                  : profileModal ===
                    'create'
                    ? 'Tạo hồ sơ'
                    : 'Lưu thay đổi'}
              </button>
            </div>
          </div>
        </div>
      )}


      {/* ================================================== */}
      {/* MODAL ĐỔI TRẠNG THÁI */}
      {/* ================================================== */}

      {statusModal && (
        <div
          className="sd-overlay"
          onClick={
            closeStatusModal
          }
        >
          <div
            className="sd-modal"
            onClick={(event) => {
              event.stopPropagation();
            }}
          >
            <div className="sd-modal-header">
              <div>
                <h2>
                  Cập nhật trạng thái hồ sơ
                </h2>

                <span className="bhxh-secondary-text">
                  {statusModal
                    .profile
                    .fullName}
                </span>
              </div>

              <button
                type="button"
                disabled={saving}
                onClick={
                  closeStatusModal
                }
              >
                ✕
              </button>
            </div>

            <div className="sd-modal-body">
              <p>
                Chuyển hồ sơ sang:
              </p>

              <StatusBadge
                status={
                  statusModal.targetStatus
                }
              />

              {statusModal.targetStatus ===
                PROFILE_STATUS.ACTIVE && (
                <p
                  className={
                    'bhxh-modal-note ' +
                    'bhxh-modal-note--info'
                  }
                >
                  Hồ sơ phải có mã số BHXH và mức lương căn cứ hợp lệ trước khi kích hoạt.
                </p>
              )}

              {statusModal.targetStatus ===
                PROFILE_STATUS.SUSPENDED && (
                <p
                  className={
                    'bhxh-modal-note ' +
                    'bhxh-modal-note--warning'
                  }
                >
                  Hồ sơ sẽ tạm ngừng sinh khoản đóng BHXH nhưng vẫn được giữ lại trong hệ thống.
                </p>
              )}

              {statusModal.targetStatus ===
                PROFILE_STATUS.STOPPED && (
                <p
                  className={
                    'bhxh-modal-note ' +
                    'bhxh-modal-note--warning'
                  }
                >
                  Hệ thống sẽ kết thúc hồ sơ nhưng vẫn giữ toàn bộ lịch sử.
                </p>
              )}

              <div className="sd-field">
                <label>
                  Ghi chú
                </label>

                <textarea
                  rows={4}
                  maxLength={500}
                  value={statusNote}
                  placeholder="Nhập lý do hoặc nội dung ghi chú..."
                  onChange={(event) => {
                    setStatusNote(
                      event.target.value
                    );
                  }}
                />
              </div>

              {statusFormError && (
                <p className="sd-status sd-status-error">
                  {statusFormError}
                </p>
              )}
            </div>

            <div className="sd-modal-footer">
              <button
                type="button"
                className="sd-btn-ghost"
                disabled={saving}
                onClick={
                  closeStatusModal
                }
              >
                Hủy
              </button>

              <button
                type="button"
                className="sd-btn-primary"
                disabled={saving}
                onClick={
                  handleUpdateStatus
                }
              >
                {saving
                  ? 'Đang cập nhật...'
                  : 'Xác nhận'}
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}