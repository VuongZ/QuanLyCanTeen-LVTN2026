import {
  useCallback,
  useEffect,
  useMemo,
  useState
} from 'react';

import {
  cancelSocialInsuranceContribution,
  createSocialInsuranceProfile,
  createSocialInsuranceRate,
  deactivateSocialInsuranceRate,
  generateSocialInsuranceContributions,
  getAllSocialInsuranceProfiles,
  getAllSocialInsuranceRates,
  getFullTimeEmployees,
  getSocialInsuranceContributionsByPeriod,
  markSocialInsuranceContributionPaid,
  updateSocialInsuranceProfile,
  updateSocialInsuranceProfileStatus,
  updateSocialInsuranceRate
} from '../../api/SocialInsuranceApi';

import '../css/SocialInsuranceTab.css';


// ============================================================
// TRẠNG THÁI
// ============================================================

const PROFILE_STATUS = {
  PENDING: 'PENDING',
  ACTIVE: 'ACTIVE',
  SUSPENDED: 'SUSPENDED',
  STOPPED: 'STOPPED'
};

const STAFF_CONFIRMATION_STATUS = {
  PENDING: 'PENDING',
  CONFIRMED: 'CONFIRMED',
  CHANGE_REQUESTED: 'CHANGE_REQUESTED'
};

const CONTRIBUTION_STATUS = {
  DRAFT: 'DRAFT',
  CONFIRMED: 'CONFIRMED',
  PAID: 'PAID',
  CANCELLED: 'CANCELLED'
};

const PROFILE_STATUS_DISPLAY = {
  PENDING: {
    label: 'Chờ hoàn tất',
    className: 'bhxh-badge--warning'
  },
  ACTIVE: {
    label: 'Đang tham gia',
    className: 'bhxh-badge--success'
  },
  SUSPENDED: {
    label: 'Tạm ngừng',
    className: 'bhxh-badge--orange'
  },
  STOPPED: {
    label: 'Đã kết thúc',
    className: 'bhxh-badge--neutral'
  }
};

const STAFF_CONFIRMATION_DISPLAY = {
  PENDING: {
    label: 'Chờ Staff xác nhận',
    className: 'bhxh-badge--warning'
  },
  CONFIRMED: {
    label: 'Staff đã xác nhận',
    className: 'bhxh-badge--success'
  },
  CHANGE_REQUESTED: {
    label: 'Yêu cầu chỉnh sửa',
    className: 'bhxh-badge--danger'
  }
};

const CONTRIBUTION_STATUS_DISPLAY = {
  DRAFT: {
    label: 'Dự kiến',
    className: 'bhxh-badge--warning'
  },
  CONFIRMED: {
    label: 'Chờ nộp',
    className: 'bhxh-badge--blue'
  },
  PAID: {
    label: 'Đã nộp',
    className: 'bhxh-badge--success'
  },
  CANCELLED: {
    label: 'Đã hủy',
    className: 'bhxh-badge--neutral'
  }
};


// ============================================================
// HÀM HỖ TRỢ
// ============================================================

function normalizeStatus(value) {
  return String(value || '')
    .trim()
    .toUpperCase();
}

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

function formatMoney(value) {
  const numberValue =
    Number(value);

  if (!Number.isFinite(numberValue)) {
    return '—';
  }

  return new Intl.NumberFormat(
    'vi-VN',
    {
      style: 'currency',
      currency: 'VND',
      maximumFractionDigits: 0
    }
  ).format(numberValue);
}

function formatPercent(value) {
  const numberValue =
    Number(value);

  if (!Number.isFinite(numberValue)) {
    return '—';
  }

  return (
    new Intl.NumberFormat(
      'vi-VN',
      {
        minimumFractionDigits: 0,
        maximumFractionDigits: 2
      }
    ).format(numberValue) + '%'
  );
}

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

function formatDateTime(value) {
  if (!value) {
    return '—';
  }

  const date =
    new Date(value);

  if (Number.isNaN(date.getTime())) {
    return '—';
  }

  return new Intl.DateTimeFormat(
    'vi-VN',
    {
      dateStyle: 'short',
      timeStyle: 'short'
    }
  ).format(date);
}

function getVietnamDateParts() {
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

  return Object.fromEntries(
    parts.map((part) => [
      part.type,
      part.value
    ])
  );
}

function getVietnamToday() {
  const parts =
    getVietnamDateParts();

  return (
    `${parts.year}-` +
    `${parts.month}-` +
    `${parts.day}`
  );
}

function getVietnamCurrentPeriod() {
  const parts =
    getVietnamDateParts();

  return {
    month: Number(parts.month),
    year: Number(parts.year)
  };
}

function formatEmploymentType(value) {
  return normalizeStatus(value) ===
    'FULL TIME'
    ? 'Full-time'
    : value || '—';
}

function getProfileDisplay(status) {
  return (
    PROFILE_STATUS_DISPLAY[
      normalizeStatus(status)
    ] || {
      label: 'Chưa có hồ sơ',
      className: 'bhxh-badge--neutral'
    }
  );
}

function getStaffConfirmationDisplay(status) {
  return (
    STAFF_CONFIRMATION_DISPLAY[
      normalizeStatus(status)
    ] || {
      label: 'Chưa có hồ sơ',
      className: 'bhxh-badge--neutral'
    }
  );
}

function getContributionDisplay(status) {
  return (
    CONTRIBUTION_STATUS_DISPLAY[
      normalizeStatus(status)
    ] || {
      label: status || 'Không xác định',
      className: 'bhxh-badge--neutral'
    }
  );
}


// ============================================================
// COMPONENT DÙNG CHUNG
// ============================================================

function Badge({
  display
}) {
  return (
    <span
      className={
        `bhxh-badge ${display.className}`
      }
    >
      {display.label}
    </span>
  );
}

function ProfileStatusBadge({
  status
}) {
  return (
    <Badge
      display={
        getProfileDisplay(status)
      }
    />
  );
}

function StaffConfirmationBadge({
  status
}) {
  return (
    <Badge
      display={
        getStaffConfirmationDisplay(
          status
        )
      }
    />
  );
}

function ContributionStatusBadge({
  status
}) {
  return (
    <Badge
      display={
        getContributionDisplay(status)
      }
    />
  );
}

function EmptyState({
  icon,
  title,
  description
}) {
  return (
    <div className="bhxh-empty-state">
      <span className="bhxh-empty-icon">
        {icon}
      </span>

      <strong>{title}</strong>

      {description && (
        <p>{description}</p>
      )}
    </div>
  );
}

function Modal({
  title,
  subtitle,
  disabled,
  onClose,
  children,
  footer
}) {
  return (
    <div
      className="bhxh-modal-overlay"
      onMouseDown={onClose}
      role="presentation"
    >
      <div
        className="bhxh-modal"
        onMouseDown={(event) => {
          event.stopPropagation();
        }}
        role="dialog"
        aria-modal="true"
      >
        <div className="bhxh-modal-header">
          <div>
            <h2>{title}</h2>

            {subtitle && (
              <p>{subtitle}</p>
            )}
          </div>

          <button
            type="button"
            className="bhxh-modal-close"
            disabled={disabled}
            onClick={onClose}
          >
            ✕
          </button>
        </div>

        <div className="bhxh-modal-body">
          {children}
        </div>

        {footer && (
          <div className="bhxh-modal-footer">
            {footer}
          </div>
        )}
      </div>
    </div>
  );
}


// ============================================================
// COMPONENT CHÍNH
// ============================================================

export function AdminSocialInsuranceTab() {
  const currentPeriod =
    useMemo(
      () => getVietnamCurrentPeriod(),
      []
    );

  const [activeSection, setActiveSection] =
    useState('profiles');

  const [employees, setEmployees] =
    useState([]);

  const [profiles, setProfiles] =
    useState([]);

  const [rates, setRates] =
    useState([]);

  const [
    contributions,
    setContributions
  ] = useState([]);

  const [
    loadingOverview,
    setLoadingOverview
  ] = useState(true);

  const [
    loadingContributions,
    setLoadingContributions
  ] = useState(false);

  const [savingKey, setSavingKey] =
    useState('');

  const [message, setMessage] =
    useState(null);

  const [searchText, setSearchText] =
    useState('');

  const [
    profileStatusFilter,
    setProfileStatusFilter
  ] = useState('ALL');

  const [
    confirmationFilter,
    setConfirmationFilter
  ] = useState('ALL');

  const [selectedMonth, setSelectedMonth] =
    useState(currentPeriod.month);

  const [selectedYear, setSelectedYear] =
    useState(currentPeriod.year);


  // ==========================================================
  // STATE HỒ SƠ
  // ==========================================================

  const [profileModal, setProfileModal] =
    useState(null);

  const [
    selectedEmployee,
    setSelectedEmployee
  ] = useState(null);

  const [
    selectedProfile,
    setSelectedProfile
  ] = useState(null);

  const [
    profileFormError,
    setProfileFormError
  ] = useState('');

  const [profileForm, setProfileForm] =
    useState({
      socialInsuranceNumber: '',
      insuranceSalaryBasis: '',
      startDate: '',
      endDate: '',
      note: ''
    });

  const [statusModal, setStatusModal] =
    useState(null);

  const [
    statusFormError,
    setStatusFormError
  ] = useState('');

  const [statusNote, setStatusNote] =
    useState('');


  // ==========================================================
  // STATE CẤU HÌNH TỶ LỆ
  // ==========================================================

  const [rateModalOpen, setRateModalOpen] =
    useState(false);

  const [selectedRate, setSelectedRate] =
    useState(null);

  const [
    rateFormError,
    setRateFormError
  ] = useState('');

  const [rateForm, setRateForm] =
    useState({
      employeeRate: '8',
      employerRate: '17.5',
      effectiveFrom: '',
      effectiveTo: ''
    });

  const [
    deactivateRateModal,
    setDeactivateRateModal
  ] = useState(null);

  const [
    deactivateEffectiveTo,
    setDeactivateEffectiveTo
  ] = useState('');

  const [
    deactivateRateError,
    setDeactivateRateError
  ] = useState('');


  // ==========================================================
  // STATE KHOẢN ĐÓNG
  // ==========================================================

  const [
    cancelContributionModal,
    setCancelContributionModal
  ] = useState(null);

  const [
    cancelReason,
    setCancelReason
  ] = useState('');

  const [
    cancelContributionError,
    setCancelContributionError
  ] = useState('');


  const isSaving =
    Boolean(savingKey);


  // ==========================================================
  // TẢI DỮ LIỆU
  // ==========================================================

  const loadOverview =
    useCallback(
      async () => {
        setLoadingOverview(true);

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
              'Không thể tải dữ liệu BHXH.'
            )
          });
        } finally {
          setLoadingOverview(false);
        }
      },
      []
    );

  const loadContributions =
    useCallback(
      async (
        month,
        year
      ) => {
        const normalizedMonth =
          Number(month);

        const normalizedYear =
          Number(year);

        if (
          normalizedMonth < 1 ||
          normalizedMonth > 12 ||
          normalizedYear < 2000 ||
          normalizedYear > 2100
        ) {
          setMessage({
            type: 'error',
            text:
              'Tháng hoặc năm được chọn không hợp lệ.'
          });

          return;
        }

        setLoadingContributions(true);

        try {
          const data =
            await getSocialInsuranceContributionsByPeriod(
              normalizedMonth,
              normalizedYear
            );

          setContributions(
            Array.isArray(data)
              ? data
              : []
          );
        } catch (error) {
          setContributions([]);

          setMessage({
            type: 'error',
            text: getApiErrorMessage(
              error,
              'Không thể tải khoản đóng BHXH.'
            )
          });
        } finally {
          setLoadingContributions(false);
        }
      },
      []
    );

  useEffect(() => {
    loadOverview();
  }, [
    loadOverview
  ]);

  useEffect(() => {
    if (
      activeSection ===
      'contributions'
    ) {
      loadContributions(
        selectedMonth,
        selectedYear
      );
    }
  }, [
    activeSection,
    selectedMonth,
    selectedYear,
    loadContributions
  ]);


  // ==========================================================
  // DỮ LIỆU TÍNH TOÁN
  // ==========================================================

  const profileByUserId =
    useMemo(() => {
      const map =
        new Map();

      profiles.forEach((profile) => {
        map.set(
          String(profile.userId),
          profile
        );
      });

      return map;
    }, [
      profiles
    ]);

  const employeeRows =
    useMemo(() => {
      const normalizedSearch =
        searchText
          .trim()
          .toLowerCase();

      return employees
        .map((employee) => ({
          ...employee,
          profile:
            profileByUserId.get(
              String(employee.userId)
            ) || null
        }))
        .filter((row) => {
          const profileStatus =
            normalizeStatus(
              row.profile?.status
            );

          const confirmationStatus =
            normalizeStatus(
              row.profile
                ?.staffConfirmationStatus
            );

          if (
            profileStatusFilter !==
              'ALL' &&
            profileStatus !==
              profileStatusFilter
          ) {
            return false;
          }

          if (
            confirmationFilter !==
              'ALL' &&
            confirmationStatus !==
              confirmationFilter
          ) {
            return false;
          }

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
            row.profile?.status,
            row.profile
              ?.staffConfirmationStatus,
            row.profile
              ?.staffConfirmationNote
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
      profileStatusFilter,
      confirmationFilter,
      searchText
    ]);

  const profileStatistics =
    useMemo(() => {
      return {
        employees:
          employees.length,

        profiles:
          profiles.length,

        active:
          profiles.filter(
            (profile) =>
              normalizeStatus(
                profile.status
              ) ===
              PROFILE_STATUS.ACTIVE
          ).length,

        waitingConfirmation:
          profiles.filter(
            (profile) =>
              normalizeStatus(
                profile
                  .staffConfirmationStatus
              ) ===
              STAFF_CONFIRMATION_STATUS
                .PENDING
          ).length,

        changeRequested:
          profiles.filter(
            (profile) =>
              normalizeStatus(
                profile
                  .staffConfirmationStatus
              ) ===
              STAFF_CONFIRMATION_STATUS
                .CHANGE_REQUESTED
          ).length
      };
    }, [
      employees,
      profiles
    ]);

  const sortedRates =
    useMemo(() => {
      return [...rates].sort(
        (
          firstRate,
          secondRate
        ) =>
          String(
            secondRate.effectiveFrom ||
            ''
          ).localeCompare(
            String(
              firstRate.effectiveFrom ||
              ''
            )
          )
      );
    }, [
      rates
    ]);

  const activeRate =
    useMemo(() => {
      const today =
        getVietnamToday();

      return (
        sortedRates.find((rate) => {
          const from =
            String(
              rate.effectiveFrom || ''
            ).slice(0, 10);

          const to =
            rate.effectiveTo
              ? String(
                  rate.effectiveTo
                ).slice(0, 10)
              : null;

          return (
            Boolean(rate.isActive) &&
            from <= today &&
            (
              !to ||
              to >= today
            )
          );
        }) || null
      );
    }, [
      sortedRates
    ]);

  const selectedPeriodRate =
    useMemo(() => {
      const targetDate =
        `${selectedYear}-` +
        `${String(selectedMonth)
          .padStart(2, '0')}-01`;

      return (
        sortedRates.find((rate) => {
          const from =
            String(
              rate.effectiveFrom || ''
            ).slice(0, 10);

          const to =
            rate.effectiveTo
              ? String(
                  rate.effectiveTo
                ).slice(0, 10)
              : null;

          return (
            from <= targetDate &&
            (
              !to ||
              to >= targetDate
            )
          );
        }) || null
      );
    }, [
      sortedRates,
      selectedMonth,
      selectedYear
    ]);

  const contributionStatistics =
    useMemo(() => {
      const result = {
        totalCount:
          contributions.length,

        draftCount:
          0,

        confirmedCount:
          0,

        paidCount:
          0,

        employeeAmount:
          0,

        employeeDeductedAmount:
          0,

        employeeOutstandingAmount:
          0,

        employerAmount:
          0,

        totalAmount:
          0
      };

      contributions.forEach(
        (contribution) => {
          const status =
            normalizeStatus(
              contribution.status
            );

          if (
            status ===
            CONTRIBUTION_STATUS.DRAFT
          ) {
            result.draftCount += 1;
          }

          if (
            status ===
            CONTRIBUTION_STATUS.CONFIRMED
          ) {
            result.confirmedCount += 1;
          }

          if (
            status ===
            CONTRIBUTION_STATUS.PAID
          ) {
            result.paidCount += 1;
          }

          if (
            status !==
            CONTRIBUTION_STATUS.CANCELLED
          ) {
            result.employeeAmount +=
              Number(
                contribution.employeeAmount ||
                0
              );

            result.employeeDeductedAmount +=
              Number(
                contribution.employeeDeductedAmount ||
                0
              );

            result.employeeOutstandingAmount +=
              Number(
                contribution.employeeOutstandingAmount ||
                0
              );

            result.employerAmount +=
              Number(
                contribution.employerAmount ||
                0
              );

            result.totalAmount +=
              Number(
                contribution.totalAmount ||
                0
              );
          }
        }
      );

      return result;
    }, [
      contributions
    ]);


  // ==========================================================
  // HỒ SƠ BHXH
  // ==========================================================

  function openCreateProfile(employee) {
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

  function openEditProfile(profile) {
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
        profile.note || ''
    });

    setProfileFormError('');
    setMessage(null);
    setProfileModal('edit');
  }

  function closeProfileModal() {
    if (isSaving) {
      return;
    }

    setProfileModal(null);
    setSelectedEmployee(null);
    setSelectedProfile(null);
    setProfileFormError('');
  }

  function validateProfileForm() {
    const salaryBasis =
      Number(
        profileForm.insuranceSalaryBasis
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
        'Ngày bắt đầu tham gia là bắt buộc.'
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

    setSavingKey('profile-save');
    setProfileFormError('');

    try {
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
            'Không xác định được hồ sơ.'
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

      await loadOverview();

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
          'Không thể lưu hồ sơ BHXH.'
        )
      );
    } finally {
      setSavingKey('');
    }
  }

  function openStatusModal(
    profile,
    targetStatus
  ) {
    const normalizedTarget =
      normalizeStatus(targetStatus);

    const confirmationStatus =
      normalizeStatus(
        profile?.staffConfirmationStatus
      );

    if (
      normalizedTarget ===
        PROFILE_STATUS.ACTIVE &&
      confirmationStatus !==
        STAFF_CONFIRMATION_STATUS
          .CONFIRMED
    ) {
      setMessage({
        type: 'error',

        text:
          confirmationStatus ===
            STAFF_CONFIRMATION_STATUS
              .CHANGE_REQUESTED
            ? (
                'Staff đang yêu cầu chỉnh sửa. ' +
                'Admin cần cập nhật hồ sơ trước ' +
                'khi kích hoạt.'
              )
            : (
                'Phải chờ Staff xác nhận hồ sơ ' +
                'trước khi kích hoạt.'
              )
      });

      return;
    }

    setStatusModal({
      profile,
      targetStatus:
        normalizedTarget
    });

    setStatusNote('');
    setStatusFormError('');
    setMessage(null);
  }

  function closeStatusModal() {
    if (isSaving) {
      return;
    }

    setStatusModal(null);
    setStatusNote('');
    setStatusFormError('');
  }

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
        'Không xác định được hồ sơ hoặc trạng thái.'
      );

      return;
    }

    setSavingKey('profile-status');
    setStatusFormError('');

    try {
      const response =
        await updateSocialInsuranceProfileStatus(
          profile.id,
          {
            status:
              targetStatus,

            note:
              statusNote.trim() ||
              null
          }
        );

      setStatusModal(null);
      setStatusNote('');

      await loadOverview();

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
          'Không thể cập nhật trạng thái.'
        )
      );
    } finally {
      setSavingKey('');
    }
  }


  // ==========================================================
  // CẤU HÌNH TỶ LỆ
  // ==========================================================

  function openCreateRateModal() {
    setSelectedRate(null);

    setRateForm({
      employeeRate: '8',
      employerRate: '17.5',
      effectiveFrom:
        getVietnamToday(),
      effectiveTo: ''
    });

    setRateFormError('');
    setMessage(null);
    setRateModalOpen(true);
  }

  function openEditRateModal(rate) {
    if (!rate?.id || !rate.canEdit) {
      setMessage({
        type: 'error',
        text:
          'Cấu hình này không đủ điều kiện ' +
          'để chỉnh sửa trực tiếp.'
      });

      return;
    }

    setSelectedRate(rate);

    setRateForm({
      employeeRate:
        String(
          rate.employeeRate ?? ''
        ),

      employerRate:
        String(
          rate.employerRate ?? ''
        ),

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
    setRateModalOpen(true);
  }

  function closeRateModal() {
    if (isSaving) {
      return;
    }

    setRateModalOpen(false);
    setSelectedRate(null);
    setRateFormError('');
  }

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
      return (
        'Tỷ lệ nhân viên đóng là bắt buộc.'
      );
    }

    if (!employerRateText) {
      return (
        'Tỷ lệ doanh nghiệp đóng là bắt buộc.'
      );
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

    if (
      selectedRate &&
      rateForm.effectiveFrom <=
        getVietnamToday()
    ) {
      return (
        'Ngày bắt đầu khi chỉnh sửa ' +
        'phải nằm trong tương lai.'
      );
    }

    if (
      rateForm.effectiveTo &&
      rateForm.effectiveTo <
        rateForm.effectiveFrom
    ) {
      return (
        'Ngày kết thúc không được ' +
        'trước ngày bắt đầu.'
      );
    }

    return '';
  }

  async function handleSaveRate() {
    const validationError =
      validateRateForm();

    if (validationError) {
      setRateFormError(
        validationError
      );

      return;
    }

    const isEditing =
      Boolean(selectedRate?.id);

    const payload = {
      employeeRate:
        Number(
          rateForm.employeeRate
        ),

      employerRate:
        Number(
          rateForm.employerRate
        ),

      effectiveFrom:
        rateForm.effectiveFrom,

      effectiveTo:
        rateForm.effectiveTo ||
        null
    };

    setSavingKey('rate-save');
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

      setRateModalOpen(false);
      setSelectedRate(null);

      await loadOverview();

      setMessage({
        type: 'success',

        text:
          response?.message ||
          (
            isEditing
              ? 'Đã cập nhật cấu hình tỷ lệ.'
              : 'Đã tạo cấu hình tỷ lệ.'
          )
      });
    } catch (error) {
      setRateFormError(
        getApiErrorMessage(
          error,
          isEditing
            ? 'Không thể cập nhật cấu hình.'
            : 'Không thể tạo cấu hình.'
        )
      );
    } finally {
      setSavingKey('');
    }
  }

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

  function closeDeactivateRateModal() {
    if (isSaving) {
      return;
    }

    setDeactivateRateModal(null);
    setDeactivateEffectiveTo('');
    setDeactivateRateError('');
  }

  async function handleDeactivateRate() {
    const rate =
      deactivateRateModal;

    if (!rate?.id) {
      setDeactivateRateError(
        'Không xác định được cấu hình.'
      );

      return;
    }

    if (!deactivateEffectiveTo) {
      setDeactivateRateError(
        'Ngày kết thúc là bắt buộc.'
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
        'Ngày kết thúc không được trước ngày bắt đầu.'
      );

      return;
    }

    setSavingKey('rate-deactivate');
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

      await loadOverview();

      setMessage({
        type: 'success',
        text:
          response?.message ||
          'Đã ngừng áp dụng cấu hình.'
      });
    } catch (error) {
      setDeactivateRateError(
        getApiErrorMessage(
          error,
          'Không thể ngừng cấu hình.'
        )
      );
    } finally {
      setSavingKey('');
    }
  }


  // ==========================================================
  // KHOẢN ĐÓNG HẰNG THÁNG
  // ==========================================================

  async function handleGenerateContributions() {
    if (!selectedPeriodRate) {
      setMessage({
        type: 'error',
        text:
          `Không tìm thấy cấu hình tỷ lệ ` +
          `có hiệu lực cho tháng ` +
          `${selectedMonth}/${selectedYear}.`
      });

      return;
    }

    const accepted =
      window.confirm(
        `Sinh khoản đóng BHXH tháng ` +
        `${selectedMonth}/${selectedYear}?\n\n` +
        `Chỉ hồ sơ ACTIVE, Staff đã xác nhận ` +
        `và nhân viên FULL TIME mới được tạo.`
      );

    if (!accepted) {
      return;
    }

    setSavingKey('contribution-generate');
    setMessage(null);

    try {
      const response =
        await generateSocialInsuranceContributions(
          {
            month:
              Number(selectedMonth),

            year:
              Number(selectedYear)
          }
        );

      await loadContributions(
        selectedMonth,
        selectedYear
      );

      setMessage({
        type: 'success',

        text:
          response?.message ||
          (
            `Đã tạo ` +
            `${response?.createdCount || 0} ` +
            `khoản đóng BHXH.`
          )
      });
    } catch (error) {
      setMessage({
        type: 'error',
        text: getApiErrorMessage(
          error,
          'Không thể sinh khoản đóng BHXH.'
        )
      });
    } finally {
      setSavingKey('');
    }
  }

  async function handleMarkPaid(
    contribution
  ) {
    const accepted =
      window.confirm(
        `Xác nhận doanh nghiệp đã nộp khoản BHXH của ` +
        `${contribution.fullName} ` +
        `tháng ${contribution.month}/` +
        `${contribution.year} cho cơ quan BHXH?`
      );

    if (!accepted) {
      return;
    }

    setSavingKey(
      `contribution-paid-${contribution.id}`
    );

    try {
      const response =
        await markSocialInsuranceContributionPaid(
          contribution.id
        );

      await loadContributions(
        selectedMonth,
        selectedYear
      );

      setMessage({
        type: 'success',
        text:
          response?.message ||
          'Đã xác nhận doanh nghiệp đã nộp BHXH.'
      });
    } catch (error) {
      setMessage({
        type: 'error',
        text: getApiErrorMessage(
          error,
          'Không thể xác nhận khoản BHXH đã nộp.'
        )
      });
    } finally {
      setSavingKey('');
    }
  }

  function openCancelContributionModal(
    contribution
  ) {
    setCancelContributionModal(
      contribution
    );

    setCancelReason('');
    setCancelContributionError('');
    setMessage(null);
  }

  function closeCancelContributionModal() {
    if (isSaving) {
      return;
    }

    setCancelContributionModal(null);
    setCancelReason('');
    setCancelContributionError('');
  }

  async function handleCancelContribution() {
    const contribution =
      cancelContributionModal;

    const normalizedReason =
      cancelReason.trim();

    if (!contribution?.id) {
      setCancelContributionError(
        'Không xác định được khoản đóng.'
      );

      return;
    }

    if (!normalizedReason) {
      setCancelContributionError(
        'Vui lòng nhập lý do hủy.'
      );

      return;
    }

    if (normalizedReason.length > 500) {
      setCancelContributionError(
        'Lý do hủy không được vượt quá 500 ký tự.'
      );

      return;
    }

    setSavingKey('contribution-cancel');
    setCancelContributionError('');

    try {
      const response =
        await cancelSocialInsuranceContribution(
          contribution.id,
          {
            reason:
              normalizedReason
          }
        );

      setCancelContributionModal(null);
      setCancelReason('');

      await loadContributions(
        selectedMonth,
        selectedYear
      );

      setMessage({
        type: 'success',
        text:
          response?.message ||
          'Đã hủy khoản đóng BHXH.'
      });
    } catch (error) {
      setCancelContributionError(
        getApiErrorMessage(
          error,
          'Không thể hủy khoản đóng.'
        )
      );
    } finally {
      setSavingKey('');
    }
  }


  // ==========================================================
  // RENDER HỒ SƠ
  // ==========================================================

  function renderProfilesSection() {
    return (
      <>
        <div className="bhxh-stat-grid bhxh-stat-grid--five">
          <div className="bhxh-stat">
            <span className="bhxh-stat-icon">
              👥
            </span>

            <div>
              <p>Nhân viên FULL TIME</p>
              <strong>
                {profileStatistics.employees}
              </strong>
            </div>
          </div>

          <div className="bhxh-stat">
            <span className="bhxh-stat-icon">
              📁
            </span>

            <div>
              <p>Đã có hồ sơ</p>
              <strong>
                {profileStatistics.profiles}
              </strong>
            </div>
          </div>

          <div className="bhxh-stat">
            <span className="bhxh-stat-icon bhxh-stat-icon--success">
              ✓
            </span>

            <div>
              <p>Đang tham gia</p>
              <strong>
                {profileStatistics.active}
              </strong>
            </div>
          </div>

          <div className="bhxh-stat">
            <span className="bhxh-stat-icon bhxh-stat-icon--warning">
              ⏳
            </span>

            <div>
              <p>Chờ Staff xác nhận</p>
              <strong>
                {
                  profileStatistics
                    .waitingConfirmation
                }
              </strong>
            </div>
          </div>

          <div className="bhxh-stat">
            <span className="bhxh-stat-icon bhxh-stat-icon--danger">
              !
            </span>

            <div>
              <p>Yêu cầu chỉnh sửa</p>
              <strong>
                {
                  profileStatistics
                    .changeRequested
                }
              </strong>
            </div>
          </div>
        </div>

        <section className="bhxh-panel">
          <div className="bhxh-panel-header">
            <div>
              <p className="bhxh-kicker">
                Hồ sơ tham gia
              </p>

              <h2>
                Danh sách nhân viên
              </h2>

              <p>
                Tạo hồ sơ, theo dõi xác nhận của
                Staff và quản lý trạng thái tham gia.
              </p>
            </div>

            <button
              type="button"
              className="bhxh-btn bhxh-btn--light"
              disabled={loadingOverview}
              onClick={() => {
                loadOverview();
              }}
            >
              ↻ Làm mới
            </button>
          </div>

          <div className="bhxh-toolbar">
            <div className="bhxh-search-box">
              <span>⌕</span>

              <input
                type="search"
                value={searchText}
                placeholder={
                  'Tìm tên, email, mã số BHXH hoặc phản hồi...'
                }
                onChange={(event) => {
                  setSearchText(
                    event.target.value
                  );
                }}
              />

              {searchText && (
                <button
                  type="button"
                  onClick={() => {
                    setSearchText('');
                  }}
                >
                  ✕
                </button>
              )}
            </div>

            <select
              value={profileStatusFilter}
              onChange={(event) => {
                setProfileStatusFilter(
                  event.target.value
                );
              }}
            >
              <option value="ALL">
                Tất cả trạng thái hồ sơ
              </option>

              <option value="PENDING">
                Chờ hoàn tất
              </option>

              <option value="ACTIVE">
                Đang tham gia
              </option>

              <option value="SUSPENDED">
                Tạm ngừng
              </option>

              <option value="STOPPED">
                Đã kết thúc
              </option>
            </select>

            <select
              value={confirmationFilter}
              onChange={(event) => {
                setConfirmationFilter(
                  event.target.value
                );
              }}
            >
              <option value="ALL">
                Tất cả xác nhận Staff
              </option>

              <option value="PENDING">
                Chờ Staff xác nhận
              </option>

              <option value="CONFIRMED">
                Staff đã xác nhận
              </option>

              <option value="CHANGE_REQUESTED">
                Yêu cầu chỉnh sửa
              </option>
            </select>

            <span className="bhxh-result-count">
              {employeeRows.length} nhân viên
            </span>
          </div>

          {loadingOverview ? (
            <div className="bhxh-loading">
              Đang tải danh sách hồ sơ...
            </div>
          ) : employeeRows.length === 0 ? (
            <EmptyState
              icon="👤"
              title="Không tìm thấy nhân viên"
              description={
                'Hãy thay đổi từ khóa hoặc bộ lọc.'
              }
            />
          ) : (
            <div className="bhxh-table-wrap">
              <table className="bhxh-table bhxh-profile-table">
                <thead>
                  <tr>
                    <th>Nhân viên</th>
                    <th>Hồ sơ</th>
                    <th>Xác nhận Staff</th>
                    <th>Thông tin đóng</th>
                    <th>Thời gian tham gia</th>
                    <th className="bhxh-align-right">
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

                      const confirmationStatus =
                        normalizeStatus(
                          profile
                            ?.staffConfirmationStatus
                        );

                      const canActivate =
                        Boolean(profile) &&
                        confirmationStatus ===
                          STAFF_CONFIRMATION_STATUS
                            .CONFIRMED;

                      return (
                        <tr
                          key={employee.userId}
                        >
                          <td>
                            <div className="bhxh-person">
                              <div className="bhxh-person-avatar">
                                {String(
                                  employee.fullName ||
                                  'NV'
                                )
                                  .split(' ')
                                  .filter(Boolean)
                                  .slice(-2)
                                  .map(
                                    (part) =>
                                      part[0]
                                  )
                                  .join('')
                                  .toUpperCase()}
                              </div>

                              <div>
                                <strong>
                                  {
                                    employee.fullName ||
                                    'Chưa có tên'
                                  }
                                </strong>

                                <span>
                                  {
                                    employee.email ||
                                    'Chưa có email'
                                  }
                                </span>

                                <small>
                                  {
                                    formatEmploymentType(
                                      employee
                                        .employmentType
                                    )
                                  }
                                </small>
                              </div>
                            </div>
                          </td>

                          <td>
                            {profile ? (
                              <div className="bhxh-cell-stack">
                                <ProfileStatusBadge
                                  status={
                                    profile.status
                                  }
                                />

                                <span>
                                  Mã hồ sơ #{profile.id}
                                </span>
                              </div>
                            ) : (
                              <Badge
                                display={{
                                  label:
                                    'Chưa có hồ sơ',
                                  className:
                                    'bhxh-badge--neutral'
                                }}
                              />
                            )}
                          </td>

                          <td>
                            {profile ? (
                              <div className="bhxh-cell-stack bhxh-confirmation-cell">
                                <StaffConfirmationBadge
                                  status={
                                    profile
                                      .staffConfirmationStatus
                                  }
                                />

                                {profile.staffConfirmedAt && (
                                  <span>
                                    {
                                      formatDateTime(
                                        profile
                                          .staffConfirmedAt
                                      )
                                    }
                                  </span>
                                )}

                                {profile.staffConfirmationNote && (
                                  <div className="bhxh-staff-note">
                                    <strong>
                                      Staff phản hồi:
                                    </strong>

                                    <p>
                                      {
                                        profile
                                          .staffConfirmationNote
                                      }
                                    </p>
                                  </div>
                                )}
                              </div>
                            ) : (
                              <span className="bhxh-muted">
                                —
                              </span>
                            )}
                          </td>

                          <td>
                            {profile ? (
                              <div className="bhxh-cell-stack">
                                <strong className="bhxh-code">
                                  {
                                    profile
                                      .socialInsuranceNumber ||
                                    'Chưa có mã BHXH'
                                  }
                                </strong>

                                <span className="bhxh-money">
                                  {
                                    formatMoney(
                                      profile
                                        .insuranceSalaryBasis
                                    )
                                  }
                                </span>
                              </div>
                            ) : (
                              <span className="bhxh-muted">
                                —
                              </span>
                            )}
                          </td>

                          <td>
                            {profile ? (
                              <div className="bhxh-cell-stack">
                                <span>
                                  Từ{' '}
                                  <strong>
                                    {
                                      formatDate(
                                        profile
                                          .startDate
                                      )
                                    }
                                  </strong>
                                </span>

                                <span>
                                  Đến{' '}
                                  <strong>
                                    {
                                      profile.endDate
                                        ? formatDate(
                                            profile
                                              .endDate
                                          )
                                        : 'Chưa xác định'
                                    }
                                  </strong>
                                </span>
                              </div>
                            ) : (
                              <span className="bhxh-muted">
                                —
                              </span>
                            )}
                          </td>

                          <td>
                            <div className="bhxh-row-actions">
                              {!profile && (
                                <button
                                  type="button"
                                  className="bhxh-btn bhxh-btn--primary"
                                  disabled={isSaving}
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
                                  className="bhxh-btn bhxh-btn--light"
                                  disabled={isSaving}
                                  onClick={() => {
                                    openEditProfile(
                                      profile
                                    );
                                  }}
                                >
                                  ✎ Chỉnh sửa
                                </button>
                              )}

                              {status ===
                                PROFILE_STATUS.PENDING && (
                                canActivate ? (
                                  <button
                                    type="button"
                                    className="bhxh-btn bhxh-btn--success"
                                    disabled={isSaving}
                                    onClick={() => {
                                      openStatusModal(
                                        profile,
                                        PROFILE_STATUS
                                          .ACTIVE
                                      );
                                    }}
                                  >
                                    ✓ Kích hoạt
                                  </button>
                                ) : (
                                  <span className="bhxh-action-hint">
                                    {
                                      confirmationStatus ===
                                      STAFF_CONFIRMATION_STATUS
                                        .CHANGE_REQUESTED
                                        ? 'Cần chỉnh sửa hồ sơ'
                                        : 'Chờ Staff xác nhận'
                                    }
                                  </span>
                                )
                              )}

                              {status ===
                                PROFILE_STATUS.ACTIVE && (
                                <>
                                  <button
                                    type="button"
                                    className="bhxh-btn bhxh-btn--warning"
                                    disabled={isSaving}
                                    onClick={() => {
                                      openStatusModal(
                                        profile,
                                        PROFILE_STATUS
                                          .SUSPENDED
                                      );
                                    }}
                                  >
                                    Tạm ngừng
                                  </button>

                                  <button
                                    type="button"
                                    className="bhxh-btn bhxh-btn--danger-light"
                                    disabled={isSaving}
                                    onClick={() => {
                                      openStatusModal(
                                        profile,
                                        PROFILE_STATUS
                                          .STOPPED
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
                                  {canActivate ? (
                                    <button
                                      type="button"
                                      className="bhxh-btn bhxh-btn--success"
                                      disabled={isSaving}
                                      onClick={() => {
                                        openStatusModal(
                                          profile,
                                          PROFILE_STATUS
                                            .ACTIVE
                                        );
                                      }}
                                    >
                                      Kích hoạt lại
                                    </button>
                                  ) : (
                                    <span className="bhxh-action-hint">
                                      Chưa được Staff xác nhận
                                    </span>
                                  )}

                                  <button
                                    type="button"
                                    className="bhxh-btn bhxh-btn--danger-light"
                                    disabled={isSaving}
                                    onClick={() => {
                                      openStatusModal(
                                        profile,
                                        PROFILE_STATUS
                                          .STOPPED
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
                </tbody>
              </table>
            </div>
          )}
        </section>
      </>
    );
  }


  // ==========================================================
  // RENDER KHOẢN ĐÓNG
  // ==========================================================

  function renderContributionsSection() {
    return (
      <>
        <section className="bhxh-panel">
          <div className="bhxh-panel-header bhxh-panel-header--period">
            <div>
              <p className="bhxh-kicker">
                Kỳ đóng BHXH
              </p>

              <h2>
                Tháng {selectedMonth}/{selectedYear}
              </h2>

              <p>
                Sinh và xử lý khoản đóng cho các
                hồ sơ đủ điều kiện.
              </p>
            </div>

            <div className="bhxh-period-controls">
              <label>
                <span>Tháng</span>

                <select
                  value={selectedMonth}
                  onChange={(event) => {
                    setSelectedMonth(
                      Number(
                        event.target.value
                      )
                    );
                  }}
                >
                  {Array.from(
                    {
                      length: 12
                    },
                    (
                      _,
                      index
                    ) => index + 1
                  ).map((month) => (
                    <option
                      key={month}
                      value={month}
                    >
                      Tháng {month}
                    </option>
                  ))}
                </select>
              </label>

              <label>
                <span>Năm</span>

                <input
                  type="number"
                  min="2000"
                  max="2100"
                  value={selectedYear}
                  onChange={(event) => {
                    setSelectedYear(
                      Number(
                        event.target.value
                      )
                    );
                  }}
                />
              </label>

              <button
                type="button"
                className="bhxh-btn bhxh-btn--light"
                disabled={
                  loadingContributions
                }
                onClick={() => {
                  loadContributions(
                    selectedMonth,
                    selectedYear
                  );
                }}
              >
                ↻ Tải lại
              </button>

              <button
                type="button"
                className="bhxh-btn bhxh-btn--primary"
                disabled={isSaving}
                onClick={
                  handleGenerateContributions
                }
              >
                {savingKey ===
                'contribution-generate'
                  ? 'Đang sinh...'
                  : '＋ Sinh khoản đóng'}
              </button>
            </div>
          </div>

          {selectedPeriodRate ? (
            <div className="bhxh-period-rate">
              <div>
                <span>
                  Tỷ lệ áp dụng
                </span>

                <strong>
                  {
                    formatPercent(
                      selectedPeriodRate
                        .employeeRate
                    )
                  }
                  {' + '}
                  {
                    formatPercent(
                      selectedPeriodRate
                        .employerRate
                    )
                  }
                </strong>
              </div>

              <div>
                <span>
                  Nhân viên đóng
                </span>

                <strong>
                  {
                    formatPercent(
                      selectedPeriodRate
                        .employeeRate
                    )
                  }
                </strong>
              </div>

              <div>
                <span>
                  Doanh nghiệp đóng
                </span>

                <strong>
                  {
                    formatPercent(
                      selectedPeriodRate
                        .employerRate
                    )
                  }
                </strong>
              </div>

              <div>
                <span>
                  Hiệu lực từ
                </span>

                <strong>
                  {
                    formatDate(
                      selectedPeriodRate
                        .effectiveFrom
                    )
                  }
                </strong>
              </div>
            </div>
          ) : (
            <div className="bhxh-inline-alert bhxh-inline-alert--warning">
              Chưa có cấu hình tỷ lệ phù hợp cho
              kỳ {selectedMonth}/{selectedYear}.
              Không thể sinh khoản đóng mới.
            </div>
          )}
        </section>

        <div className="bhxh-stat-grid">
          <div className="bhxh-stat">
            <span className="bhxh-stat-icon">
              📄
            </span>

            <div>
              <p>Tổng khoản đóng</p>
              <strong>
                {
                  contributionStatistics
                    .totalCount
                }
              </strong>
            </div>
          </div>

          <div className="bhxh-stat">
            <span className="bhxh-stat-icon bhxh-stat-icon--warning">
              D
            </span>

            <div>
              <p>Dự kiến</p>
              <strong>
                {
                  contributionStatistics
                    .draftCount
                }
              </strong>
            </div>
          </div>

          <div className="bhxh-stat">
            <span className="bhxh-stat-icon bhxh-stat-icon--blue">
              C
            </span>

            <div>
              <p>Chờ nộp</p>
              <strong>
                {
                  contributionStatistics
                    .confirmedCount
                }
              </strong>
            </div>
          </div>

          <div className="bhxh-stat">
            <span className="bhxh-stat-icon bhxh-stat-icon--success">
              ✓
            </span>

            <div>
              <p>Đã nộp</p>
              <strong>
                {
                  contributionStatistics
                    .paidCount
                }
              </strong>
            </div>
          </div>
        </div>

        <section className="bhxh-panel">
          <div className="bhxh-money-summary">
            <div>
              <span>
                Tổng nhân viên đóng
              </span>

              <strong>
                {
                  formatMoney(
                    contributionStatistics
                      .employeeAmount
                  )
                }
              </strong>
            </div>

            <div>
              <span>
                Đã khấu trừ từ lương
              </span>

              <strong>
                {
                  formatMoney(
                    contributionStatistics
                      .employeeDeductedAmount
                  )
                }
              </strong>
            </div>

            <div>
              <span>
                Doanh nghiệp tạm ứng
              </span>

              <strong>
                {
                  formatMoney(
                    contributionStatistics
                      .employeeOutstandingAmount
                  )
                }
              </strong>
            </div>

            <div>
              <span>
                Tổng doanh nghiệp đóng
              </span>

              <strong>
                {
                  formatMoney(
                    contributionStatistics
                      .employerAmount
                  )
                }
              </strong>
            </div>

            <div className="bhxh-money-summary-total">
              <span>Tổng cộng</span>

              <strong>
                {
                  formatMoney(
                    contributionStatistics
                      .totalAmount
                  )
                }
              </strong>
            </div>
          </div>

          {loadingContributions ? (
            <div className="bhxh-loading">
              Đang tải khoản đóng BHXH...
            </div>
          ) : contributions.length === 0 ? (
            <EmptyState
              icon="📄"
              title={
                `Chưa có khoản đóng tháng ` +
                `${selectedMonth}/${selectedYear}`
              }
              description={
                'Nhấn “Sinh khoản đóng” để tạo các bản ghi DRAFT.'
              }
            />
          ) : (
            <div className="bhxh-table-wrap">
              <table className="bhxh-table bhxh-contribution-table">
                <thead>
                  <tr>
                    <th>Nhân viên</th>
                    <th>Lương căn cứ</th>
                    <th>Nhân viên đóng</th>
                    <th>Doanh nghiệp đóng</th>
                    <th>Tổng cộng</th>
                    <th>Trạng thái</th>
                    <th>Xử lý</th>
                    <th className="bhxh-align-right">
                      Thao tác
                    </th>
                  </tr>
                </thead>

                <tbody>
                  {contributions.map(
                    (contribution) => {
                      const status =
                        normalizeStatus(
                          contribution.status
                        );

                      return (
                        <tr
                          key={contribution.id}
                        >
                          <td>
                            <div className="bhxh-cell-stack">
                              <strong>
                                {
                                  contribution
                                    .fullName ||
                                  'Nhân viên'
                                }
                              </strong>

                              <span>
                                Mã khoản #
                                {contribution.id}
                              </span>
                            </div>
                          </td>

                          <td>
                            <strong className="bhxh-money">
                              {
                                formatMoney(
                                  contribution
                                    .insuranceSalaryBasis
                                )
                              }
                            </strong>
                          </td>

                          <td>
                            <div className="bhxh-cell-stack">
                              <strong>
                                {
                                  formatMoney(
                                    contribution
                                      .employeeAmount
                                  )
                                }
                              </strong>

                              <span>
                                Tỷ lệ:{' '}
                                {
                                  formatPercent(
                                    contribution
                                      .employeeRate
                                  )
                                }
                              </span>

                              <span>
                                Đã trừ:{' '}
                                {
                                  formatMoney(
                                    contribution
                                      .employeeDeductedAmount
                                  )
                                }
                              </span>

                              {Number(
                                contribution
                                  .employeeOutstandingAmount ||
                                0
                              ) > 0 && (
                                <span className="bhxh-note-text">
                                  Doanh nghiệp tạm ứng:{' '}
                                  {
                                    formatMoney(
                                      contribution
                                        .employeeOutstandingAmount
                                    )
                                  }
                                </span>
                              )}

                              <span>
                                {
                                  normalizeStatus(
                                    contribution
                                      .deductionStatus
                                  ) === 'FULL'
                                    ? 'Đã khấu trừ đủ'
                                    : normalizeStatus(
                                        contribution
                                          .deductionStatus
                                      ) === 'PARTIAL'
                                      ? 'Khấu trừ một phần'
                                      : 'Chưa khấu trừ'
                                }
                              </span>
                            </div>
                          </td>

                          <td>
                            <div className="bhxh-cell-stack">
                              <strong>
                                {
                                  formatMoney(
                                    contribution
                                      .employerAmount
                                  )
                                }
                              </strong>

                              <span>
                                {
                                  formatPercent(
                                    contribution
                                      .employerRate
                                  )
                                }
                              </span>
                            </div>
                          </td>

                          <td>
                            <strong className="bhxh-total-money">
                              {
                                formatMoney(
                                  contribution
                                    .totalAmount
                                )
                              }
                            </strong>
                          </td>

                          <td>
                            <ContributionStatusBadge
                              status={
                                contribution.status
                              }
                            />
                          </td>

                          <td>
                            <div className="bhxh-cell-stack">
                              {contribution.confirmedAt && (
                                <span>
                                  Xác nhận:{' '}
                                  {
                                    formatDateTime(
                                      contribution
                                        .confirmedAt
                                    )
                                  }
                                </span>
                              )}

                              {contribution.paidAt && (
                                <span>
                                  Đã nộp:{' '}
                                  {
                                    formatDateTime(
                                      contribution
                                        .paidAt
                                    )
                                  }
                                </span>
                              )}

                              {contribution.note && (
                                <span className="bhxh-note-text">
                                  {
                                    contribution.note
                                  }
                                </span>
                              )}

                              {!contribution.confirmedAt &&
                                !contribution.paidAt &&
                                !contribution.note && (
                                <span>
                                  Chưa xử lý
                                </span>
                              )}
                            </div>
                          </td>

                          <td>
                            <div className="bhxh-row-actions">
                              {status ===
                                CONTRIBUTION_STATUS
                                  .DRAFT && (
                                <span className="bhxh-action-complete">
                                  Chờ chốt bảng lương
                                </span>
                              )}

                              {status ===
                                CONTRIBUTION_STATUS
                                  .CONFIRMED && (
                                <button
                                  type="button"
                                  className="bhxh-btn bhxh-btn--success"
                                  disabled={isSaving}
                                  onClick={() => {
                                    handleMarkPaid(
                                      contribution
                                    );
                                  }}
                                >
                                  {
                                    savingKey ===
                                    `contribution-paid-${contribution.id}`
                                      ? 'Đang cập nhật...'
                                      : 'Xác nhận đã nộp'
                                  }
                                </button>
                              )}

                              {status !==
                                CONTRIBUTION_STATUS
                                  .PAID &&
                                status !==
                                CONTRIBUTION_STATUS
                                  .CANCELLED && (
                                <button
                                  type="button"
                                  className="bhxh-btn bhxh-btn--danger-light"
                                  disabled={isSaving}
                                  onClick={() => {
                                    openCancelContributionModal(
                                      contribution
                                    );
                                  }}
                                >
                                  Hủy khoản
                                </button>
                              )}

                              {(status ===
                                CONTRIBUTION_STATUS
                                  .PAID ||
                                status ===
                                CONTRIBUTION_STATUS
                                  .CANCELLED) && (
                                <span className="bhxh-action-complete">
                                  Không còn thao tác
                                </span>
                              )}
                            </div>
                          </td>
                        </tr>
                      );
                    }
                  )}
                </tbody>
              </table>
            </div>
          )}
        </section>
      </>
    );
  }


  // ==========================================================
  // RENDER TỶ LỆ
  // ==========================================================

  function renderRatesSection() {
    return (
      <>
        <section className="bhxh-panel">
          <div className="bhxh-panel-header">
            <div>
              <p className="bhxh-kicker">
                Cấu hình đóng BHXH
              </p>

              <h2>
                Tỷ lệ đóng theo thời gian
              </h2>

              <p>
                Cấu hình được lưu theo khoảng hiệu
                lực để không làm thay đổi dữ liệu cũ.
              </p>
            </div>

            <button
              type="button"
              className="bhxh-btn bhxh-btn--primary"
              disabled={isSaving}
              onClick={openCreateRateModal}
            >
              ＋ Tạo cấu hình mới
            </button>
          </div>

          {activeRate ? (
            <div className="bhxh-active-rate">
              <div className="bhxh-active-rate-main">
                <span>
                  Tỷ lệ đang áp dụng
                </span>

                <strong>
                  {
                    formatPercent(
                      Number(
                        activeRate.employeeRate ||
                        0
                      ) +
                      Number(
                        activeRate.employerRate ||
                        0
                      )
                    )
                  }
                </strong>
              </div>

              <div>
                <span>Nhân viên</span>

                <strong>
                  {
                    formatPercent(
                      activeRate.employeeRate
                    )
                  }
                </strong>
              </div>

              <div>
                <span>Doanh nghiệp</span>

                <strong>
                  {
                    formatPercent(
                      activeRate.employerRate
                    )
                  }
                </strong>
              </div>

              <div>
                <span>Hiệu lực từ</span>

                <strong>
                  {
                    formatDate(
                      activeRate.effectiveFrom
                    )
                  }
                </strong>
              </div>
            </div>
          ) : (
            <div className="bhxh-inline-alert bhxh-inline-alert--warning">
              Hiện chưa có cấu hình tỷ lệ đang
              áp dụng tại ngày hôm nay.
            </div>
          )}
        </section>

        <section className="bhxh-panel">
          <div className="bhxh-panel-header">
            <div>
              <p className="bhxh-kicker">
                Lịch sử cấu hình
              </p>

              <h2>
                Danh sách tỷ lệ
              </h2>
            </div>

            <button
              type="button"
              className="bhxh-btn bhxh-btn--light"
              disabled={loadingOverview}
              onClick={() => {
                loadOverview();
              }}
            >
              ↻ Làm mới
            </button>
          </div>

          {loadingOverview ? (
            <div className="bhxh-loading">
              Đang tải cấu hình tỷ lệ...
            </div>
          ) : sortedRates.length === 0 ? (
            <EmptyState
              icon="⚙️"
              title="Chưa có cấu hình tỷ lệ"
              description={
                'Tạo cấu hình đầu tiên để có thể sinh khoản đóng.'
              }
            />
          ) : (
            <div className="bhxh-table-wrap">
              <table className="bhxh-table bhxh-rate-table">
                <thead>
                  <tr>
                    <th>Nhân viên</th>
                    <th>Doanh nghiệp</th>
                    <th>Tổng tỷ lệ</th>
                    <th>Khoảng hiệu lực</th>
                    <th>Trạng thái</th>
                    <th>Thông tin</th>
                    <th className="bhxh-align-right">
                      Thao tác
                    </th>
                  </tr>
                </thead>

                <tbody>
                  {sortedRates.map(
                    (rate) => {
                      const totalRate =
                        Number(
                          rate.employeeRate ||
                          0
                        ) +
                        Number(
                          rate.employerRate ||
                          0
                        );

                      return (
                        <tr key={rate.id}>
                          <td>
                            <strong className="bhxh-rate-employee">
                              {
                                formatPercent(
                                  rate.employeeRate
                                )
                              }
                            </strong>
                          </td>

                          <td>
                            <strong className="bhxh-rate-employer">
                              {
                                formatPercent(
                                  rate.employerRate
                                )
                              }
                            </strong>
                          </td>

                          <td>
                            <strong>
                              {
                                formatPercent(
                                  totalRate
                                )
                              }
                            </strong>
                          </td>

                          <td>
                            <div className="bhxh-cell-stack">
                              <span>
                                Từ{' '}
                                <strong>
                                  {
                                    formatDate(
                                      rate.effectiveFrom
                                    )
                                  }
                                </strong>
                              </span>

                              <span>
                                Đến{' '}
                                <strong>
                                  {
                                    rate.effectiveTo
                                      ? formatDate(
                                          rate.effectiveTo
                                        )
                                      : 'Chưa xác định'
                                  }
                                </strong>
                              </span>
                            </div>
                          </td>

                          <td>
                            <Badge
                              display={{
                                label:
                                  rate.isActive
                                    ? 'Đang hoạt động'
                                    : 'Đã ngừng',

                                className:
                                  rate.isActive
                                    ? 'bhxh-badge--success'
                                    : 'bhxh-badge--neutral'
                              }}
                            />
                          </td>

                          <td>
                            <div className="bhxh-cell-stack">
                              <span>
                                Tạo bởi:{' '}
                                <strong>
                                  {
                                    rate.createdByUserName ||
                                    'Admin'
                                  }
                                </strong>
                              </span>

                              <span>
                                {
                                  rate.hasBeenUsed
                                    ? 'Đã được sử dụng'
                                    : 'Chưa được sử dụng'
                                }
                              </span>
                            </div>
                          </td>

                          <td>
                            <div className="bhxh-row-actions">
                              {rate.canEdit && (
                                <button
                                  type="button"
                                  className="bhxh-btn bhxh-btn--light"
                                  disabled={isSaving}
                                  onClick={() => {
                                    openEditRateModal(
                                      rate
                                    );
                                  }}
                                >
                                  ✎ Chỉnh sửa
                                </button>
                              )}

                              {rate.isActive && (
                                <button
                                  type="button"
                                  className="bhxh-btn bhxh-btn--danger-light"
                                  disabled={isSaving}
                                  onClick={() => {
                                    openDeactivateRateModal(
                                      rate
                                    );
                                  }}
                                >
                                  Ngừng áp dụng
                                </button>
                              )}

                              {!rate.canEdit &&
                                !rate.isActive && (
                                <span className="bhxh-action-complete">
                                  Không còn thao tác
                                </span>
                              )}
                            </div>
                          </td>
                        </tr>
                      );
                    }
                  )}
                </tbody>
              </table>
            </div>
          )}
        </section>
      </>
    );
  }


  // ==========================================================
  // GIAO DIỆN CHÍNH
  // ==========================================================

  return (
    <div className="bhxh-shell">
      <section className="bhxh-hero">
        <div className="bhxh-hero-content">
          <div className="bhxh-hero-icon">
            🛡️
          </div>

          <div>
            <p className="bhxh-hero-kicker">
              Quản trị phúc lợi nhân viên
            </p>

            <h1>
              Bảo hiểm xã hội
            </h1>

            <p>
              Quản lý hồ sơ, xác nhận của Staff,
              cấu hình tỷ lệ và khoản đóng hằng tháng
              trong cùng một quy trình.
            </p>
          </div>
        </div>

        <div className="bhxh-flow">
          <span>Admin tạo hồ sơ</span>
          <b>→</b>
          <span>Staff xác nhận</span>
          <b>→</b>
          <span>Admin kích hoạt</span>
          <b>→</b>
          <span>Sinh khoản đóng</span>
        </div>
      </section>

      <nav className="bhxh-tabs">
        <button
          type="button"
          className={
            activeSection === 'profiles'
              ? 'active'
              : ''
          }
          onClick={() => {
            setActiveSection('profiles');
            setMessage(null);
          }}
        >
          <span>👥</span>

          <div>
            <strong>Hồ sơ nhân viên</strong>
            <small>
              {profiles.length} hồ sơ
            </small>
          </div>
        </button>

        <button
          type="button"
          className={
            activeSection ===
            'contributions'
              ? 'active'
              : ''
          }
          onClick={() => {
            setActiveSection(
              'contributions'
            );

            setMessage(null);
          }}
        >
          <span>💳</span>

          <div>
            <strong>Khoản đóng</strong>
            <small>
              Tháng {selectedMonth}/
              {selectedYear}
            </small>
          </div>
        </button>

        <button
          type="button"
          className={
            activeSection === 'rates'
              ? 'active'
              : ''
          }
          onClick={() => {
            setActiveSection('rates');
            setMessage(null);
          }}
        >
          <span>⚙️</span>

          <div>
            <strong>Cấu hình tỷ lệ</strong>
            <small>
              {rates.length} cấu hình
            </small>
          </div>
        </button>
      </nav>

      {message && (
        <div
          className={
            `bhxh-page-message ` +
            `bhxh-page-message--${message.type}`
          }
          role="alert"
        >
          <span>
            {message.type === 'success'
              ? '✓'
              : '!'}
          </span>

          <p>{message.text}</p>

          <button
            type="button"
            onClick={() => {
              setMessage(null);
            }}
          >
            ✕
          </button>
        </div>
      )}

      <main className="bhxh-section-content">
        {activeSection === 'profiles' &&
          renderProfilesSection()}

        {activeSection ===
          'contributions' &&
          renderContributionsSection()}

        {activeSection === 'rates' &&
          renderRatesSection()}
      </main>


      {/* ================================================== */}
      {/* MODAL TẠO / SỬA HỒ SƠ */}
      {/* ================================================== */}

      {profileModal && (
        <Modal
          title={
            profileModal === 'create'
              ? 'Tạo hồ sơ BHXH'
              : 'Cập nhật hồ sơ BHXH'
          }
          subtitle={
            profileModal === 'create'
              ? selectedEmployee?.fullName
              : selectedProfile?.fullName
          }
          disabled={isSaving}
          onClose={closeProfileModal}
          footer={
            <>
              <button
                type="button"
                className="bhxh-btn bhxh-btn--light"
                disabled={isSaving}
                onClick={closeProfileModal}
              >
                Hủy
              </button>

              <button
                type="button"
                className="bhxh-btn bhxh-btn--primary"
                disabled={isSaving}
                onClick={handleSaveProfile}
              >
                {savingKey ===
                'profile-save'
                  ? 'Đang lưu...'
                  : profileModal ===
                    'create'
                    ? 'Tạo hồ sơ'
                    : 'Lưu thay đổi'}
              </button>
            </>
          }
        >
          <div className="bhxh-form-grid">
            <label className="bhxh-field">
              <span>Mã số BHXH</span>

              <input
                type="text"
                maxLength={20}
                value={
                  profileForm
                    .socialInsuranceNumber
                }
                placeholder="Ví dụ: BHXH0000123"
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
            </label>

            <label className="bhxh-field">
              <span>
                Mức lương làm căn cứ *
              </span>

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
            </label>

            <label className="bhxh-field">
              <span>Ngày bắt đầu *</span>

              <input
                type="date"
                value={profileForm.startDate}
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
            </label>

            <label className="bhxh-field">
              <span>Ngày kết thúc</span>

              <input
                type="date"
                min={
                  profileForm.startDate ||
                  undefined
                }
                value={profileForm.endDate}
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
            </label>
          </div>

          <label className="bhxh-field">
            <span>Ghi chú nội bộ Admin</span>

            <textarea
              rows={4}
              maxLength={500}
              value={profileForm.note}
              placeholder={
                'Thông tin bổ sung dành cho Admin...'
              }
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
          </label>

          {profileModal === 'edit' && (
            <div className="bhxh-inline-alert bhxh-inline-alert--warning">
              Thay đổi mã số BHXH, lương căn cứ,
              ngày bắt đầu hoặc ngày kết thúc sẽ đưa
              hồ sơ về PENDING và Staff phải xác
              nhận lại. Chỉ sửa ghi chú thì không
              reset xác nhận.
            </div>
          )}

          {profileFormError && (
            <div className="bhxh-form-error">
              {profileFormError}
            </div>
          )}
        </Modal>
      )}


      {/* ================================================== */}
      {/* MODAL ĐỔI TRẠNG THÁI HỒ SƠ */}
      {/* ================================================== */}

      {statusModal && (
        <Modal
          title="Cập nhật trạng thái hồ sơ"
          subtitle={
            statusModal.profile.fullName
          }
          disabled={isSaving}
          onClose={closeStatusModal}
          footer={
            <>
              <button
                type="button"
                className="bhxh-btn bhxh-btn--light"
                disabled={isSaving}
                onClick={closeStatusModal}
              >
                Hủy
              </button>

              <button
                type="button"
                className="bhxh-btn bhxh-btn--primary"
                disabled={isSaving}
                onClick={handleUpdateStatus}
              >
                {savingKey ===
                'profile-status'
                  ? 'Đang cập nhật...'
                  : 'Xác nhận'}
              </button>
            </>
          }
        >
          <div className="bhxh-status-change">
            <span>Trạng thái mới</span>

            <ProfileStatusBadge
              status={
                statusModal.targetStatus
              }
            />
          </div>

          {statusModal.targetStatus ===
            PROFILE_STATUS.ACTIVE && (
            <div className="bhxh-requirement-card">
              <h3>
                Điều kiện kích hoạt
              </h3>

              <ul>
                <li>
                  Nhân viên vẫn là FULL TIME.
                </li>

                <li>
                  Staff đã xác nhận thông tin.
                </li>

                <li>
                  Có mã số BHXH.
                </li>

                <li>
                  Lương căn cứ lớn hơn 0.
                </li>
              </ul>

              <StaffConfirmationBadge
                status={
                  statusModal.profile
                    .staffConfirmationStatus
                }
              />
            </div>
          )}

          {statusModal.targetStatus ===
            PROFILE_STATUS.SUSPENDED && (
            <div className="bhxh-inline-alert bhxh-inline-alert--warning">
              Hồ sơ sẽ tạm ngừng sinh khoản đóng,
              nhưng toàn bộ dữ liệu vẫn được giữ lại.
            </div>
          )}

          {statusModal.targetStatus ===
            PROFILE_STATUS.STOPPED && (
            <div className="bhxh-inline-alert bhxh-inline-alert--danger">
              Hồ sơ sẽ kết thúc tham gia. Hệ thống
              không xóa lịch sử đã phát sinh.
            </div>
          )}

          <label className="bhxh-field">
            <span>Ghi chú</span>

            <textarea
              rows={4}
              maxLength={500}
              value={statusNote}
              placeholder={
                'Nhập lý do hoặc nội dung ghi chú...'
              }
              onChange={(event) => {
                setStatusNote(
                  event.target.value
                );
              }}
            />
          </label>

          {statusFormError && (
            <div className="bhxh-form-error">
              {statusFormError}
            </div>
          )}
        </Modal>
      )}


      {/* ================================================== */}
      {/* MODAL TẠO / SỬA TỶ LỆ */}
      {/* ================================================== */}

      {rateModalOpen && (
        <Modal
          title={
            selectedRate
              ? 'Chỉnh sửa cấu hình tỷ lệ'
              : 'Tạo cấu hình tỷ lệ'
          }
          subtitle={
            'Tỷ lệ nhập theo đơn vị phần trăm'
          }
          disabled={isSaving}
          onClose={closeRateModal}
          footer={
            <>
              <button
                type="button"
                className="bhxh-btn bhxh-btn--light"
                disabled={isSaving}
                onClick={closeRateModal}
              >
                Hủy
              </button>

              <button
                type="button"
                className="bhxh-btn bhxh-btn--primary"
                disabled={isSaving}
                onClick={handleSaveRate}
              >
                {savingKey === 'rate-save'
                  ? 'Đang lưu...'
                  : selectedRate
                    ? 'Lưu thay đổi'
                    : 'Tạo cấu hình'}
              </button>
            </>
          }
        >
          <div className="bhxh-form-grid">
            <label className="bhxh-field">
              <span>
                Nhân viên đóng (%) *
              </span>

              <input
                type="number"
                min="0"
                max="100"
                step="0.01"
                value={
                  rateForm.employeeRate
                }
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
            </label>

            <label className="bhxh-field">
              <span>
                Doanh nghiệp đóng (%) *
              </span>

              <input
                type="number"
                min="0"
                max="100"
                step="0.01"
                value={
                  rateForm.employerRate
                }
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
            </label>

            <label className="bhxh-field">
              <span>
                Ngày bắt đầu hiệu lực *
              </span>

              <input
                type="date"
                value={
                  rateForm.effectiveFrom
                }
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
            </label>

            <label className="bhxh-field">
              <span>
                Ngày kết thúc hiệu lực
              </span>

              <input
                type="date"
                min={
                  rateForm.effectiveFrom ||
                  undefined
                }
                value={
                  rateForm.effectiveTo
                }
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
            </label>
          </div>

          <div className="bhxh-rate-preview">
            <div>
              <span>Nhân viên</span>

              <strong>
                {
                  formatPercent(
                    rateForm.employeeRate
                  )
                }
              </strong>
            </div>

            <div>
              <span>Doanh nghiệp</span>

              <strong>
                {
                  formatPercent(
                    rateForm.employerRate
                  )
                }
              </strong>
            </div>

            <div>
              <span>Tổng tỷ lệ</span>

              <strong>
                {
                  formatPercent(
                    Number(
                      rateForm.employeeRate ||
                      0
                    ) +
                    Number(
                      rateForm.employerRate ||
                      0
                    )
                  )
                }
              </strong>
            </div>
          </div>

          <div className="bhxh-inline-alert bhxh-inline-alert--info">
            Các khoản đóng đã tạo sẽ giữ nguyên
            tỷ lệ snapshot cũ. Cấu hình mới chỉ
            áp dụng cho khoản được sinh sau đó.
          </div>

          {rateFormError && (
            <div className="bhxh-form-error">
              {rateFormError}
            </div>
          )}
        </Modal>
      )}


      {/* ================================================== */}
      {/* MODAL NGỪNG TỶ LỆ */}
      {/* ================================================== */}

      {deactivateRateModal && (
        <Modal
          title="Ngừng áp dụng cấu hình"
          subtitle={
            `Nhân viên ` +
            `${formatPercent(
              deactivateRateModal
                .employeeRate
            )} · Doanh nghiệp ` +
            `${formatPercent(
              deactivateRateModal
                .employerRate
            )}`
          }
          disabled={isSaving}
          onClose={
            closeDeactivateRateModal
          }
          footer={
            <>
              <button
                type="button"
                className="bhxh-btn bhxh-btn--light"
                disabled={isSaving}
                onClick={
                  closeDeactivateRateModal
                }
              >
                Hủy
              </button>

              <button
                type="button"
                className="bhxh-btn bhxh-btn--danger"
                disabled={isSaving}
                onClick={
                  handleDeactivateRate
                }
              >
                {savingKey ===
                'rate-deactivate'
                  ? 'Đang cập nhật...'
                  : 'Xác nhận ngừng'}
              </button>
            </>
          }
        >
          <label className="bhxh-field">
            <span>
              Ngày cuối cùng còn hiệu lực *
            </span>

            <input
              type="date"
              min={
                String(
                  deactivateRateModal
                    .effectiveFrom || ''
                ).slice(0, 10)
              }
              value={
                deactivateEffectiveTo
              }
              onChange={(event) => {
                setDeactivateEffectiveTo(
                  event.target.value
                );
              }}
            />
          </label>

          <div className="bhxh-inline-alert bhxh-inline-alert--warning">
            Cấu hình không bị xóa. Các khoản đóng
            đã sử dụng cấu hình này vẫn được giữ
            nguyên.
          </div>

          {deactivateRateError && (
            <div className="bhxh-form-error">
              {deactivateRateError}
            </div>
          )}
        </Modal>
      )}


      {/* ================================================== */}
      {/* MODAL HỦY KHOẢN ĐÓNG */}
      {/* ================================================== */}

      {cancelContributionModal && (
        <Modal
          title="Hủy khoản đóng BHXH"
          subtitle={
            `${cancelContributionModal.fullName} · ` +
            `Tháng ${cancelContributionModal.month}/` +
            `${cancelContributionModal.year}`
          }
          disabled={isSaving}
          onClose={
            closeCancelContributionModal
          }
          footer={
            <>
              <button
                type="button"
                className="bhxh-btn bhxh-btn--light"
                disabled={isSaving}
                onClick={
                  closeCancelContributionModal
                }
              >
                Đóng
              </button>

              <button
                type="button"
                className="bhxh-btn bhxh-btn--danger"
                disabled={
                  isSaving ||
                  !cancelReason.trim()
                }
                onClick={
                  handleCancelContribution
                }
              >
                {savingKey ===
                'contribution-cancel'
                  ? 'Đang hủy...'
                  : 'Xác nhận hủy'}
              </button>
            </>
          }
        >
          <div className="bhxh-contribution-preview">
            <div>
              <span>Lương căn cứ</span>

              <strong>
                {
                  formatMoney(
                    cancelContributionModal
                      .insuranceSalaryBasis
                  )
                }
              </strong>
            </div>

            <div>
              <span>Tổng khoản đóng</span>

              <strong>
                {
                  formatMoney(
                    cancelContributionModal
                      .totalAmount
                  )
                }
              </strong>
            </div>
          </div>

          <label className="bhxh-field">
            <span>Lý do hủy *</span>

            <textarea
              rows={5}
              maxLength={500}
              value={cancelReason}
              placeholder={
                'Ví dụ: Khoản đóng được tạo sai mức lương căn cứ.'
              }
              onChange={(event) => {
                setCancelReason(
                  event.target.value
                );
              }}
            />

            <small className="bhxh-character-count">
              {cancelReason.length}/500
            </small>
          </label>

          <div className="bhxh-inline-alert bhxh-inline-alert--danger">
            Khoản đóng sẽ chuyển sang CANCELLED và
            được giữ lại để truy vết. Khoản đã PAID
            không thể hủy.
          </div>

          {cancelContributionError && (
            <div className="bhxh-form-error">
              {cancelContributionError}
            </div>
          )}
        </Modal>
      )}
    </div>
  );
}