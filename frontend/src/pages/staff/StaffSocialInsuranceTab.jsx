import {
  useCallback,
  useEffect,
  useState
} from 'react';

import {
  getMySocialInsuranceContributions,
  getMySocialInsuranceProfile,
  updateMySocialInsuranceProfileConfirmation
} from '../../api/SocialInsuranceApi';

import '../css/StaffSocialInsuranceTab.css';


// ============================================================
// HÀM HỖ TRỢ
// ============================================================

function normalizeStatus(value) {
  return String(value || '')
    .trim()
    .toUpperCase();
}


function getErrorMessage(
  error,
  fallbackMessage
) {
  return (
    error?.response?.data?.message ||
    error?.message ||
    fallbackMessage
  );
}


function formatMoney(value) {
  const amount =
    Number(value);

  if (!Number.isFinite(amount)) {
    return '—';
  }

  return new Intl.NumberFormat(
    'vi-VN',
    {
      style: 'currency',
      currency: 'VND',
      maximumFractionDigits: 0
    }
  ).format(amount);
}


// Xử lý riêng kiểu DateOnly YYYY-MM-DD
// để tránh sai lệch ngày do múi giờ trình duyệt.
function formatDate(value) {
  if (!value) {
    return 'Chưa xác định';
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
    return normalizedValue;
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
    return String(value);
  }

  return new Intl.DateTimeFormat(
    'vi-VN',
    {
      dateStyle: 'short',
      timeStyle: 'short'
    }
  ).format(date);
}


function getInitials(name = '') {
  const initials =
    String(name)
      .split(' ')
      .filter(Boolean)
      .slice(-2)
      .map((part) => part[0])
      .join('')
      .toUpperCase();

  return initials || 'BH';
}


function getProfileStatusMeta(status) {
  switch (normalizeStatus(status)) {
    case 'PENDING':
      return {
        label: 'Chờ xử lý',
        className:
          'staff-bhxh-badge--warning'
      };

    case 'ACTIVE':
      return {
        label: 'Đang tham gia',
        className:
          'staff-bhxh-badge--success'
      };

    case 'SUSPENDED':
      return {
        label: 'Tạm ngừng',
        className:
          'staff-bhxh-badge--orange'
      };

    case 'STOPPED':
      return {
        label: 'Đã ngừng',
        className:
          'staff-bhxh-badge--neutral'
      };

    default:
      return {
        label:
          status || 'Chưa xác định',
        className:
          'staff-bhxh-badge--neutral'
      };
  }
}


function getConfirmationStatusMeta(status) {
  switch (normalizeStatus(status)) {
    case 'PENDING':
      return {
        label: 'Chờ bạn xác nhận',
        className:
          'staff-bhxh-badge--warning'
      };

    case 'CONFIRMED':
      return {
        label: 'Đã xác nhận',
        className:
          'staff-bhxh-badge--success'
      };

    case 'CHANGE_REQUESTED':
      return {
        label: 'Đã yêu cầu chỉnh sửa',
        className:
          'staff-bhxh-badge--danger'
      };

    default:
      return {
        label:
          status || 'Chưa xác định',
        className:
          'staff-bhxh-badge--neutral'
      };
  }
}


function getContributionStatusMeta(status) {
  switch (normalizeStatus(status)) {
    case 'DRAFT':
      return {
        label: 'Tạm tính',
        className:
          'staff-bhxh-badge--warning'
      };

    case 'CONFIRMED':
      return {
        label: 'Đã xác nhận',
        className:
          'staff-bhxh-badge--blue'
      };

    case 'PAID':
      return {
        label: 'Đã nộp',
        className:
          'staff-bhxh-badge--success'
      };

    case 'CANCELLED':
      return {
        label: 'Đã hủy',
        className:
          'staff-bhxh-badge--neutral'
      };

    default:
      return {
        label:
          status || 'Chưa xác định',
        className:
          'staff-bhxh-badge--neutral'
      };
  }
}


// ============================================================
// COMPONENT DÙNG CHUNG
// ============================================================

function StatusBadge({
  meta
}) {
  return (
    <span
      className={
        `staff-bhxh-badge ${meta.className}`
      }
    >
      {meta.label}
    </span>
  );
}


function ProfileInfoRow({
  label,
  value
}) {
  return (
    <div className="staff-bhxh-info-row">
      <dt>{label}</dt>
      <dd>{value}</dd>
    </div>
  );
}


function EmptyState({
  icon,
  title,
  description
}) {
  return (
    <div className="staff-bhxh-empty">
      <span className="staff-bhxh-empty-icon">
        {icon}
      </span>

      <strong>{title}</strong>

      {description && (
        <p>{description}</p>
      )}
    </div>
  );
}


// ============================================================
// COMPONENT CHÍNH
// ============================================================

export function StaffSocialInsuranceTab() {
  const [profile, setProfile] =
    useState(null);

  const [
    contributions,
    setContributions
  ] = useState([]);

  const [loading, setLoading] =
    useState(true);

  const [refreshing, setRefreshing] =
    useState(false);

  const [saving, setSaving] =
    useState(false);

  const [
    errorMessage,
    setErrorMessage
  ] = useState('');

  const [
    successMessage,
    setSuccessMessage
  ] = useState('');

  const [
    isChangeModalOpen,
    setIsChangeModalOpen
  ] = useState(false);

  const [
    changeNote,
    setChangeNote
  ] = useState('');

  const [
    changeRequestError,
    setChangeRequestError
  ] = useState('');


  // ==========================================================
  // TẢI DỮ LIỆU
  // ==========================================================

  const loadData =
    useCallback(
      async ({
        initial = false
      } = {}) => {
        if (initial) {
          setLoading(true);
        } else {
          setRefreshing(true);
        }

        setErrorMessage('');

        const [
          profileResult,
          contributionsResult
        ] = await Promise.allSettled([
          getMySocialInsuranceProfile(),
          getMySocialInsuranceContributions()
        ]);

        let nextErrorMessage = '';

        if (
          profileResult.status ===
          'fulfilled'
        ) {
          setProfile(
            profileResult.value || null
          );
        } else {
          const profileError =
            profileResult.reason;

          if (
            profileError?.response?.status ===
            404
          ) {
            setProfile(null);
          } else {
            setProfile(null);

            nextErrorMessage =
              getErrorMessage(
                profileError,
                'Không thể tải hồ sơ BHXH.'
              );
          }
        }

        if (
          contributionsResult.status ===
          'fulfilled'
        ) {
          setContributions(
            Array.isArray(
              contributionsResult.value
            )
              ? contributionsResult.value
              : []
          );
        } else {
          setContributions([]);

          if (!nextErrorMessage) {
            nextErrorMessage =
              getErrorMessage(
                contributionsResult.reason,
                'Không thể tải lịch sử đóng BHXH.'
              );
          }
        }

        setErrorMessage(
          nextErrorMessage
        );

        setLoading(false);
        setRefreshing(false);
      },
      []
    );


  useEffect(() => {
    loadData({
      initial: true
    });
  }, [
    loadData
  ]);


  // ==========================================================
  // TRẠNG THÁI GIAO DIỆN
  // ==========================================================

  const profileStatus =
    normalizeStatus(
      profile?.status
    );

  const confirmationStatus =
    normalizeStatus(
      profile?.staffConfirmationStatus
    );

  const canReviewProfile =
    profileStatus === 'PENDING' &&
    confirmationStatus === 'PENDING';


  // ==========================================================
  // STAFF XÁC NHẬN HỒ SƠ
  // ==========================================================

  async function handleConfirmProfile() {
    const accepted =
      window.confirm(
        'Bạn xác nhận mã số BHXH, mức lương ' +
        'làm căn cứ và thời gian tham gia ' +
        'đang hiển thị là chính xác?'
      );

    if (!accepted) {
      return;
    }

    setSaving(true);
    setErrorMessage('');
    setSuccessMessage('');

    try {
      const response =
        await updateMySocialInsuranceProfileConfirmation(
          {
            confirmationStatus:
              'CONFIRMED',

            note: null
          }
        );

      if (response?.data) {
        setProfile(response.data);
      } else {
        await loadData();
      }

      setSuccessMessage(
        response?.message ||
        'Bạn đã xác nhận thông tin hồ sơ BHXH.'
      );
    } catch (error) {
      setErrorMessage(
        getErrorMessage(
          error,
          'Không thể xác nhận hồ sơ BHXH.'
        )
      );
    } finally {
      setSaving(false);
    }
  }


  // ==========================================================
  // STAFF YÊU CẦU CHỈNH SỬA
  // ==========================================================

  function openChangeRequestModal() {
    setChangeNote('');
    setChangeRequestError('');
    setErrorMessage('');
    setSuccessMessage('');
    setIsChangeModalOpen(true);
  }


  function closeChangeRequestModal() {
    if (saving) {
      return;
    }

    setIsChangeModalOpen(false);
    setChangeNote('');
    setChangeRequestError('');
  }


  async function handleRequestChange(
    event
  ) {
    event.preventDefault();

    const normalizedNote =
      changeNote.trim();

    if (!normalizedNote) {
      setChangeRequestError(
        'Vui lòng nhập nội dung cần Admin chỉnh sửa.'
      );

      return;
    }

    if (normalizedNote.length > 500) {
      setChangeRequestError(
        'Nội dung yêu cầu không được vượt quá 500 ký tự.'
      );

      return;
    }

    setSaving(true);
    setChangeRequestError('');

    try {
      const response =
        await updateMySocialInsuranceProfileConfirmation(
          {
            confirmationStatus:
              'CHANGE_REQUESTED',

            note:
              normalizedNote
          }
        );

      if (response?.data) {
        setProfile(response.data);
      } else {
        await loadData();
      }

      setIsChangeModalOpen(false);
      setChangeNote('');

      setSuccessMessage(
        response?.message ||
        'Đã gửi yêu cầu chỉnh sửa hồ sơ BHXH.'
      );
    } catch (error) {
      setChangeRequestError(
        getErrorMessage(
          error,
          'Không thể gửi yêu cầu chỉnh sửa.'
        )
      );
    } finally {
      setSaving(false);
    }
  }


  // ==========================================================
  // LOADING
  // ==========================================================

  if (loading) {
    return (
      <div className="staff-bhxh-loading-card">
        <span className="staff-bhxh-spinner" />

        <p>
          Đang tải thông tin bảo hiểm xã hội...
        </p>
      </div>
    );
  }


  // ==========================================================
  // GIAO DIỆN
  // ==========================================================

  return (
    <div className="staff-bhxh-page">
      {/* Đầu trang */}
      <section className="staff-bhxh-hero">
        <div className="staff-bhxh-hero-main">
          <div className="staff-bhxh-hero-icon">
            🛡️
          </div>

          <div>
            <p className="staff-bhxh-kicker">
              Phúc lợi nhân viên
            </p>

            <h2>
              Bảo hiểm xã hội của tôi
            </h2>

            <p>
              Kiểm tra hồ sơ do Admin tạo,
              xác nhận thông tin và theo dõi
              các khoản đóng BHXH hằng tháng.
            </p>
          </div>
        </div>

        <button
          className="staff-bhxh-btn staff-bhxh-btn--light"
          disabled={refreshing || saving}
          onClick={() => {
            setSuccessMessage('');

            loadData();
          }}
          type="button"
        >
          {refreshing
            ? 'Đang tải...'
            : '↻ Làm mới'}
        </button>
      </section>


      {/* Thông báo lỗi */}
      {errorMessage && (
        <div
          className={
            'staff-bhxh-message ' +
            'staff-bhxh-message--error'
          }
          role="alert"
        >
          <span>!</span>

          <p>{errorMessage}</p>

          <button
            onClick={() => {
              setErrorMessage('');
            }}
            type="button"
          >
            ✕
          </button>
        </div>
      )}


      {/* Thông báo thành công */}
      {successMessage && (
        <div
          className={
            'staff-bhxh-message ' +
            'staff-bhxh-message--success'
          }
          role="status"
        >
          <span>✓</span>

          <p>{successMessage}</p>

          <button
            onClick={() => {
              setSuccessMessage('');
            }}
            type="button"
          >
            ✕
          </button>
        </div>
      )}


      {/* Chưa có hồ sơ */}
      {!profile && (
        <section className="staff-bhxh-card">
          <EmptyState
            icon="🛡️"
            title="Bạn chưa có hồ sơ BHXH"
            description={
              'Admin chưa tạo hồ sơ BHXH cho tài khoản này.'
            }
          />
        </section>
      )}


      {/* Hồ sơ và xác nhận */}
      {profile && (
        <div className="staff-bhxh-main-grid">
          {/* Thông tin hồ sơ */}
          <section className="staff-bhxh-card">
            <header className="staff-bhxh-card-header">
              <div>
                <p className="staff-bhxh-kicker">
                  Thông tin tham gia
                </p>

                <h2>
                  Hồ sơ BHXH của tôi
                </h2>
              </div>

              <StatusBadge
                meta={
                  getProfileStatusMeta(
                    profile.status
                  )
                }
              />
            </header>

            <div className="staff-bhxh-identity">
              <div className="staff-bhxh-avatar">
                {getInitials(
                  profile.fullName
                )}
              </div>

              <div>
                <h3>
                  {profile.fullName ||
                    'Nhân viên'}
                </h3>

                <p>
                  {profile.email ||
                    'Chưa có email'}
                </p>

                <span>
                  {profile.employmentType ||
                    'FULL_TIME'}
                </span>
              </div>
            </div>

            <dl className="staff-bhxh-info-list">
              <ProfileInfoRow
                label="Mã số BHXH"
                value={
                  <strong className="staff-bhxh-code">
                    {profile
                      .socialInsuranceNumber ||
                      'Chưa cập nhật'}
                  </strong>
                }
              />

              <ProfileInfoRow
                label="Mức lương làm căn cứ"
                value={
                  <strong className="staff-bhxh-money">
                    {formatMoney(
                      profile
                        .insuranceSalaryBasis
                    )}
                  </strong>
                }
              />

              <ProfileInfoRow
                label="Ngày bắt đầu"
                value={formatDate(
                  profile.startDate
                )}
              />

              <ProfileInfoRow
                label="Ngày kết thúc"
                value={formatDate(
                  profile.endDate
                )}
              />
            </dl>
          </section>


          {/* Xác nhận Staff */}
          <section className="staff-bhxh-card">
            <header className="staff-bhxh-card-header">
              <div>
                <p className="staff-bhxh-kicker">
                  Kiểm tra thông tin
                </p>

                <h2>
                  Xác nhận hồ sơ
                </h2>
              </div>
            </header>

            <div className="staff-bhxh-confirmation-summary">
              <div>
                <span>
                  Trạng thái xác nhận
                </span>

                <StatusBadge
                  meta={
                    getConfirmationStatusMeta(
                      profile
                        .staffConfirmationStatus
                    )
                  }
                />
              </div>

              <div>
                <span>
                  Thời điểm xác nhận
                </span>

                <strong>
                  {formatDateTime(
                    profile.staffConfirmedAt
                  )}
                </strong>
              </div>
            </div>


            {/* Hồ sơ đang chờ Staff xác nhận */}
            {canReviewProfile && (
              <>
                <div
                  className={
                    'staff-bhxh-callout ' +
                    'staff-bhxh-callout--info'
                  }
                >
                  <strong>
                    Hãy kiểm tra kỹ trước khi xác nhận
                  </strong>

                  <p>
                    Vui lòng kiểm tra mã số BHXH,
                    mức lương làm căn cứ, ngày bắt đầu
                    và ngày kết thúc tham gia.
                  </p>
                </div>

                <div className="staff-bhxh-actions">
                  <button
                    className={
                      'staff-bhxh-btn ' +
                      'staff-bhxh-btn--primary'
                    }
                    disabled={saving}
                    onClick={
                      handleConfirmProfile
                    }
                    type="button"
                  >
                    {saving
                      ? 'Đang xử lý...'
                      : '✓ Xác nhận thông tin'}
                  </button>

                  <button
                    className={
                      'staff-bhxh-btn ' +
                      'staff-bhxh-btn--secondary'
                    }
                    disabled={saving}
                    onClick={
                      openChangeRequestModal
                    }
                    type="button"
                  >
                    ✎ Yêu cầu chỉnh sửa
                  </button>
                </div>
              </>
            )}


            {/* Staff đã xác nhận, chờ Admin kích hoạt */}
            {confirmationStatus ===
              'CONFIRMED' &&
              profileStatus ===
              'PENDING' && (
              <div
                className={
                  'staff-bhxh-callout ' +
                  'staff-bhxh-callout--success'
                }
              >
                <strong>
                  Bạn đã xác nhận hồ sơ
                </strong>

                <p>
                  Hồ sơ đang chờ Admin kiểm tra
                  và chuyển sang trạng thái hoạt động.
                </p>
              </div>
            )}


            {/* Hồ sơ đã được Admin kích hoạt */}
            {confirmationStatus ===
              'CONFIRMED' &&
              profileStatus ===
              'ACTIVE' && (
              <div
                className={
                  'staff-bhxh-callout ' +
                  'staff-bhxh-callout--success'
                }
              >
                <strong>
                  Hồ sơ đang hoạt động
                </strong>

                <p>
                  Bạn đã xác nhận thông tin và
                  Admin đã kích hoạt hồ sơ BHXH.
                </p>
              </div>
            )}


            {/* Hồ sơ tạm ngừng hoặc đã kết thúc */}
            {confirmationStatus ===
              'CONFIRMED' &&
              (
                profileStatus ===
                  'SUSPENDED' ||
                profileStatus ===
                  'STOPPED'
              ) && (
              <div
                className={
                  'staff-bhxh-callout ' +
                  'staff-bhxh-callout--neutral'
                }
              >
                <strong>
                  Hồ sơ đã được Admin xử lý
                </strong>

                <p>
                  Bạn chỉ có thể xem thông tin
                  và không thể thay đổi xác nhận.
                </p>
              </div>
            )}


            {/* Staff đã yêu cầu chỉnh sửa */}
            {confirmationStatus ===
              'CHANGE_REQUESTED' && (
              <>
                <div
                  className={
                    'staff-bhxh-callout ' +
                    'staff-bhxh-callout--warning'
                  }
                >
                  <strong>
                    Đang chờ Admin chỉnh sửa
                  </strong>

                  <p>
                    Bạn đã gửi yêu cầu chỉnh sửa.
                    Sau khi Admin cập nhật thông tin,
                    hồ sơ sẽ trở lại trạng thái chờ
                    xác nhận.
                  </p>
                </div>

                <div className="staff-bhxh-request-note">
                  <span>
                    Nội dung đã gửi
                  </span>

                  <p>
                    {profile
                      .staffConfirmationNote ||
                      '—'}
                  </p>
                </div>
              </>
            )}
          </section>
        </div>
      )}


      {/* Lịch sử đóng BHXH */}
      <section className="staff-bhxh-card staff-bhxh-history-card">
        <header className="staff-bhxh-card-header">
          <div>
            <p className="staff-bhxh-kicker">
              Lịch sử đóng
            </p>

            <h2>
              Khoản đóng BHXH hằng tháng
            </h2>

            <p>
              Theo dõi mức đóng của nhân viên
              và doanh nghiệp theo từng tháng.
            </p>
          </div>

          <span className="staff-bhxh-history-count">
            {contributions.length} khoản
          </span>
        </header>

        {contributions.length === 0 ? (
          <EmptyState
            icon="📄"
            title="Chưa có khoản đóng BHXH"
            description={
              'Các khoản đóng sẽ xuất hiện sau khi Admin sinh dữ liệu hằng tháng.'
            }
          />
        ) : (
          <div className="staff-bhxh-table-wrap">
            <table className="staff-bhxh-table">
              <thead>
                <tr>
                  <th>Kỳ đóng</th>
                  <th>Lương căn cứ</th>
                  <th>Nhân viên đóng</th>
                  <th>Doanh nghiệp đóng</th>
                  <th>Tổng cộng</th>
                  <th>Trạng thái</th>
                </tr>
              </thead>

              <tbody>
                {contributions.map(
                  (item) => (
                    <tr key={item.id}>
                      <td>
                        <strong>
                          Tháng {item.month}/
                          {item.year}
                        </strong>

                        <span>
                          Mã khoản #{item.id}
                        </span>
                      </td>

                      <td>
                        <strong>
                          {formatMoney(
                            item
                              .insuranceSalaryBasis
                          )}
                        </strong>
                      </td>

                      <td>
                        <strong>
                          {formatMoney(
                            item.employeeAmount
                          )}
                        </strong>

                        <span>
                          {Number(
                            item.employeeRate
                          ) || 0}%
                        </span>
                      </td>

                      <td>
                        <strong>
                          {formatMoney(
                            item.employerAmount
                          )}
                        </strong>

                        <span>
                          {Number(
                            item.employerRate
                          ) || 0}%
                        </span>
                      </td>

                      <td>
                        <strong className="staff-bhxh-total-money">
                          {formatMoney(
                            item.totalAmount
                          )}
                        </strong>
                      </td>

                      <td>
                        <StatusBadge
                          meta={
                            getContributionStatusMeta(
                              item.status
                            )
                          }
                        />
                      </td>
                    </tr>
                  )
                )}
              </tbody>
            </table>
          </div>
        )}
      </section>


      {/* Modal yêu cầu chỉnh sửa */}
      {isChangeModalOpen && (
        <div
          className="staff-bhxh-modal-overlay"
          onMouseDown={
            closeChangeRequestModal
          }
          role="presentation"
        >
          <div
            className="staff-bhxh-modal"
            onMouseDown={(event) => {
              event.stopPropagation();
            }}
            role="dialog"
            aria-modal="true"
            aria-labelledby={
              'staff-bhxh-change-title'
            }
          >
            <form
              onSubmit={
                handleRequestChange
              }
            >
              <header className="staff-bhxh-modal-header">
                <div>
                  <p className="staff-bhxh-kicker">
                    Phản hồi hồ sơ
                  </p>

                  <h2
                    id="staff-bhxh-change-title"
                  >
                    Yêu cầu Admin chỉnh sửa
                  </h2>

                  <p>
                    Mô tả rõ thông tin chưa chính xác
                    để Admin dễ kiểm tra.
                  </p>
                </div>

                <button
                  className="staff-bhxh-modal-close"
                  disabled={saving}
                  onClick={
                    closeChangeRequestModal
                  }
                  type="button"
                >
                  ✕
                </button>
              </header>

              <div className="staff-bhxh-modal-body">
                <label className="staff-bhxh-field">
                  <span>
                    Nội dung cần chỉnh sửa *
                  </span>

                  <textarea
                    autoFocus
                    id="staff-bhxh-change-note"
                    maxLength={500}
                    onChange={(event) => {
                      setChangeNote(
                        event.target.value
                      );

                      setChangeRequestError('');
                    }}
                    placeholder={
                      'Ví dụ: Mã số BHXH của tôi chưa chính xác.'
                    }
                    required
                    rows={6}
                    value={changeNote}
                  />

                  <small>
                    {changeNote.length}/500
                  </small>
                </label>

                <div
                  className={
                    'staff-bhxh-callout ' +
                    'staff-bhxh-callout--warning'
                  }
                >
                  <strong>
                    Lưu ý
                  </strong>

                  <p>
                    Sau khi gửi, bạn phải chờ
                    Admin cập nhật hồ sơ trước
                    khi có thể xác nhận lại.
                  </p>
                </div>

                {changeRequestError && (
                  <div className="staff-bhxh-form-error">
                    {changeRequestError}
                  </div>
                )}
              </div>

              <footer className="staff-bhxh-modal-footer">
                <button
                  className={
                    'staff-bhxh-btn ' +
                    'staff-bhxh-btn--light'
                  }
                  disabled={saving}
                  onClick={
                    closeChangeRequestModal
                  }
                  type="button"
                >
                  Hủy
                </button>

                <button
                  className={
                    'staff-bhxh-btn ' +
                    'staff-bhxh-btn--primary'
                  }
                  disabled={
                    saving ||
                    !changeNote.trim()
                  }
                  type="submit"
                >
                  {saving
                    ? 'Đang gửi...'
                    : 'Gửi yêu cầu'}
                </button>
              </footer>
            </form>
          </div>
        </div>
      )}
    </div>
  );
}