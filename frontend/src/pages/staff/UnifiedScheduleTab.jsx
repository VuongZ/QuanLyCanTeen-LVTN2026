import { useState, useEffect } from 'react';
import axios from 'axios';
import { getAllPeriods } from '../../api/PeriodApi';
import { getAllShifts } from '../../api/ShiftApi';
// Lịch đã công bố thuộc FinalScheduleApi.
import {
  getFinalScheduleByPeriod,
  getAutomaticFullTimeStaff
} from '../../api/FinalScheduleApi'
import {
  getScheduleUserName,
  isManagerScheduleRow
} from '../../utils/scheduleRoleUtils';


// CSS riêng của hai tab lịch/đợt, không còn đặt trong CSS dashboard lớn.
import '../css/ScheduleTabs.css'
// Mảng ánh xạ kết quả getDay() sang tên thứ bằng tiếng Việt.
// getDay(): 0 = Chủ nhật, 1 = Thứ 2, ..., 6 = Thứ 7.
const DAY_NAMES = [
  'Chủ nhật',
  'Thứ 2',
  'Thứ 3',
  'Thứ 4',
  'Thứ 5',
  'Thứ 6',
  'Thứ 7',
];

function getApiErrorMessage(error, fallbackMessage) {
  const responseData = error?.response?.data;

  if (typeof responseData === 'string' && responseData.trim()) {
    return responseData;
  }

  return responseData?.message || fallbackMessage;
}

function getVietnamDateString(date = new Date()) {
  const parts = new Intl.DateTimeFormat('en-US', {
    timeZone: 'Asia/Ho_Chi_Minh',
    year: 'numeric',
    month: '2-digit',
    day: '2-digit',
  }).formatToParts(date);

  const values = Object.fromEntries(
    parts.map((part) => [part.type, part.value])
  );

  return `${values.year}-${values.month}-${values.day}`;
}

function hasPeriodStarted(startDate) {
  const normalizedStartDate = String(startDate || '').slice(0, 10);

  return Boolean(normalizedStartDate) &&
    normalizedStartDate <= getVietnamDateString();
}

function isPeriodOpenForRegistration(period) {
  return (
    String(period?.status || '').toUpperCase() === 'OPEN' &&
    !hasPeriodStarted(period?.startDate)
  );
}

function formatDate(value) {
  if (!value) return 'Chưa có';

  const normalizedDate = String(value).slice(0, 10);
  const [year, month, day] = normalizedDate.split('-').map(Number);

  if (!year || !month || !day) return 'Chưa có';

  return new Intl.DateTimeFormat('vi-VN').format(
    new Date(year, month - 1, day)
  );
}

// ==========================================
// 👉 COMPONENT MỚI: MÀN HÌNH GỘP (LỊCH & ĐĂNG KÝ)
// ==========================================
export function UnifiedScheduleTab({ user }) {
  const [periods, setPeriods] = useState([])
  const [selectedPeriodId, setSelectedPeriodId] = useState('')
  const [loading, setLoading] = useState(true)
  const [periodError, setPeriodError] = useState('')

  // useEffect này tải danh sách các tuần của đúng chi nhánh Staff.
  // Sau đó chọn tuần phù hợp nhất để hiển thị mặc định.
  useEffect(() => {
    let isMounted = true

    async function loadPeriods() {
      try {
        const allPeriods = await getAllPeriods()

        const today = getVietnamDateString()

        // Lọc các đợt thuộc đúng chi nhánh,
        // chỉ giữ những trạng thái còn cần hiển thị,
        // rồi sắp xếp tuần hiện tại và tuần gần nhất lên trước.
        const branchPeriods = (allPeriods || [])
          .filter((period) => {
            return String(period.branchId) === String(user.branchId)
          })
          .filter((period) => {
            const status = String(
              period.status || ''
            ).toUpperCase()

            return [
              'OPEN',
              'CLOSED',
              'PUBLISHED',
              'REVIEWING',
              'DRAFT',
            ].includes(status)
          })
          .sort((first, second) => {
  const firstStart =
    String(first.startDate || '').slice(0, 10)

  const secondStart =
    String(second.startDate || '').slice(0, 10)

  // Đợt có ngày bắt đầu mới nhất nằm trên cùng.
  // Chuỗi yyyy-MM-dd có thể so sánh trực tiếp.
  return secondStart.localeCompare(firstStart)
})

        if (!isMounted) return

        setPeriods(branchPeriods)
        setPeriodError('')

        // Chọn tuần mặc định theo thứ tự ưu tiên:
        // 1. Giữ nguyên tuần đang chọn nếu nó vẫn tồn tại.
        // 2. Tuần đang diễn ra.
        // 3. Tuần đang mở đăng ký.
        // 4. Tuần đã công bố.
        // 5. Phần tử đầu tiên.
        setSelectedPeriodId((currentId) => {
          const currentStillExists = branchPeriods.some(
            (period) => String(period.id) === String(currentId)
          )

          if (currentId && currentStillExists) {
            return currentId
          }

          /**
 * Tìm đợt đang diễn ra theo ngày hiện tại.
 */
          const currentPeriod = branchPeriods.find((period) => {
            const startDate =
              String(period.startDate || '').slice(0, 10)

            const endDate =
              String(period.endDate || '').slice(0, 10)

            return (
              startDate <= today &&
              today <= endDate
            )
          })

          const openPeriod = branchPeriods.find(
            isPeriodOpenForRegistration
          )

          const publishedPeriod = branchPeriods.find((period) => {
            return (
              String(period.status || '').toUpperCase() ===
              'PUBLISHED'
            )
          })

          const firstPeriod =
            currentPeriod ||
            openPeriod ||
            publishedPeriod ||
            branchPeriods[0]

          return firstPeriod ? String(firstPeriod.id) : ''
        })
      } catch (error) {
        console.error('Lỗi lấy danh sách đợt:', error)

        if (isMounted) {
          setPeriodError(
            getApiErrorMessage(
              error,
              'Không thể tải danh sách đợt đăng ký.'
            )
          )
        }
      } finally {
        if (isMounted) {
          setLoading(false)
        }
      }
    }

    loadPeriods()

    const intervalId = window.setInterval(loadPeriods, 10000)

    return () => {
      isMounted = false
      window.clearInterval(intervalId)
    }
  }, [user.branchId])

  const selectedPeriod = periods.find((p) => {
    return p.id.toString() === selectedPeriodId
  })

  const selectedStatus = String(
    selectedPeriod?.status || ''
  ).toUpperCase()

  const isPublished = selectedStatus === 'PUBLISHED'
  const registrationIsOpen =
    isPeriodOpenForRegistration(selectedPeriod)
  const isWaitingForPublication =
    Boolean(selectedPeriod) &&
    !isPublished &&
    !registrationIsOpen

  function handleChangePeriod(e) {
    setSelectedPeriodId(e.target.value)
  }

  function getPeriodStatusText(period) {
    const status = String(period?.status || '').toUpperCase()

    if (status === 'PUBLISHED') {
      return 'Đã công bố lịch'
    }

    if (hasPeriodStarted(period?.startDate)) {
      return 'Quá hạn - Chưa công bố'
    }

    if (status === 'OPEN') {
      return 'Đang mở đăng ký'
    }

    if (
      status === 'CLOSED' ||
      status === 'REVIEWING' ||
      status === 'DRAFT'
    ) {
      return 'Đã khóa đăng ký'
    }

    return period?.status || 'Không rõ trạng thái'
  }

  if (loading) {
    return (
      <div className="sd-card schedule-tabs schedule-tabs--staff">
        <p>Đang tải dữ liệu...</p>
      </div>
    )
  }

  if (periods.length === 0) {
    return (
      <div className="sd-card schedule-tabs schedule-tabs--staff">
        <div
          className="sd-empty-state"
          style={{
            padding: '40px 20px'
          }}
        >
          <span className="sd-empty-icon">🗓️</span>

          <h3
            style={{
              color: '#1e293b',
              marginTop: 10
            }}
          >
            Chưa có dữ liệu lịch làm
          </h3>

          <p>
            {periodError
              ? periodError
              : 'Hiện tại cơ sở của bạn chưa có lịch làm chính thức hoặc đợt đăng ký ca nào được mở.'}
          </p>
        </div>
      </div>
    )
  }

  return (
    <div
      className="sd-card schedule-tabs schedule-tabs--staff"
      style={{
        padding: '20px 0'
      }}
    >
      <div
        style={{
          padding: '0 20px 16px',
          borderBottom: '1px solid #f1f5f9',
          marginBottom: 16
        }}
      >
        <div
          style={{
            display: 'grid',
            gap: 10
          }}
        >
          <div
            style={{
              display: 'flex',
              gap: 12,
              alignItems: 'center',
              flexWrap: 'wrap'
            }}
          >
            <span
              style={{
                fontSize: 14,
                fontWeight: 600,
                color: '#475569',
                whiteSpace: 'nowrap'
              }}
            >
              Chọn tuần:
            </span>

            <select
              className="sd-input-search"
              style={{
                width: '100%',
                maxWidth: 520
              }}
              value={selectedPeriodId}
              onChange={handleChangePeriod}
            >
              {periods.map((p) => (
                <option
                  key={p.id}
                  value={p.id}
                >
                  Từ {formatDate(p.startDate)} đến {formatDate(p.endDate)} - {getPeriodStatusText(p)}
                </option>
              ))}
            </select>
          </div>

          <div
            style={{
              fontSize: 13,
              color: '#64748b'
            }}
          >
            Trạng thái tuần đang chọn:{' '}

            <strong
              style={{
                color: isPublished ? '#1d4ed8' : isWaitingForPublication ? '#b45309' : '#ea580c'
              }}
            >
              {getPeriodStatusText(selectedPeriod)}
            </strong>
          </div>

          <div
            style={{
              fontSize: 13,
              color: '#64748b'
            }}
          >
            {isPublished
              ? 'Tuần này đã công bố lịch. Hệ thống đang hiển thị lịch làm chính thức.'
              : registrationIsOpen
                ? 'Đợt đăng ký đang mở. Bạn có thể chọn hoặc hủy các ca trước ngày bắt đầu.'
                : 'Đợt đăng ký đã khóa hoặc đã đến hạn. Bạn chỉ có thể xem các ca đã đăng ký và chờ Quản lý công bố lịch.'}
          </div>
        </div>
      </div>

      {periodError && (
        <div
          className="sd-period-message sd-period-message--error"
          style={{ margin: '0 20px 16px' }}
        >
          {periodError}
        </div>
      )}

      <div
        style={{
          padding: '0 20px'
        }}
      >
        {isPublished ? (
          <PublishedScheduleView
            period={selectedPeriod}
            user={user}
          />
        ) : (
          <RegistrationView
            period={selectedPeriod}
            user={user}
          />
        )}
      </div>
    </div>
  )
}

// ==========================================
// 2A. CHẾ ĐỘ: XEM LỊCH ĐÃ CHỐT
// ==========================================
// ==========================================================
// MÀN HÌNH LỊCH CHÍNH THỨC
// ==========================================================
function PublishedScheduleView({ period, user }) {
  const [registrations, setRegistrations] = useState([]);
  const [shifts, setShifts] = useState([]);
  const [shiftConfigs, setShiftConfigs] = useState([]);
  const [dates, setDates] = useState([]);
  const [loading, setLoading] = useState(true);
  const [scheduleError, setScheduleError] = useState('');

  // ========================================================================
  // TẢI LỊCH CHÍNH THỨC
  //
  // Backend trả về cả:
  // - PUBLISHED: lịch đang làm bình thường.
  // - LEAVE_APPROVED: nhân viên nghỉ có phép.
  // - ABSENT: nhân viên vắng không phép.
  // - EMERGENCY_REPLACEMENT: lịch của người được điều động thay ca.
  // ========================================================================
  useEffect(() => {
    let isMounted = true;

    async function loadBoard() {
      setLoading(true);
      setScheduleError('');

      try {
        const [scheduleRows, shiftRows, configRes] = await Promise.all([
          getFinalScheduleByPeriod(period.id),
          getAllShifts(),
          axios.get('/api/BranchShiftConfig'),
        ]);

        if (!isMounted) return;

        setRegistrations(
          Array.isArray(scheduleRows)
            ? scheduleRows
            : []
        );

        const branchShifts = (shiftRows || []).filter((shift) => {
          return String(shift.branchId) === String(user.branchId);
        });

        const branchShiftIds = new Set(
          branchShifts.map((shift) => shift.id)
        );

        setShifts(branchShifts);
        setShiftConfigs(
          (configRes.data || []).filter((config) => {
            return branchShiftIds.has(config.shiftId);
          })
        );

        // Tạo mảng ngày từ ngày bắt đầu đến ngày kết thúc của đợt.
        const dateArray = [];
        let currentDate = new Date(period.startDate);
        const endDate = new Date(period.endDate);

        while (currentDate <= endDate) {
          dateArray.push(new Date(currentDate));
          currentDate.setDate(currentDate.getDate() + 1);
        }

        setDates(dateArray);
      } catch (error) {
        console.error('Lỗi tải lịch chính thức:', error);

        if (isMounted) {
          setScheduleError(
            getApiErrorMessage(
              error,
              'Không thể tải lịch làm việc chính thức.'
            )
          );
        }
      } finally {
        if (isMounted) {
          setLoading(false);
        }
      }
    }

    loadBoard();

    return () => {
      isMounted = false;
    };
  }, [period.id, period.startDate, period.endDate, user.branchId]);

  // Chuyển Date thành yyyy-MM-dd để dùng làm khóa cho ma trận lịch.
  function toDateString(dateObj) {
    const offset = dateObj.getTimezoneOffset();
    const normalizedDate = new Date(
      dateObj.getTime() - offset * 60 * 1000
    );

    return normalizedDate.toISOString().split('T')[0];
  }

  // Kiểm tra ca có được mở vào đúng ngày hay không.
  function isShiftOpenOnDate(shiftId, dateObj) {
    const dayName = dateObj.toLocaleDateString(
      'en-US',
      { weekday: 'long' }
    );

    const config = shiftConfigs.find((item) => {
      return (
        Number(item.shiftId) === Number(shiftId) &&
        String(item.dayOfWeek).toLowerCase() ===
          dayName.toLowerCase()
      );
    });

    return Number(config?.maxStaff ?? 0) > 0;
  }

  function normalizeScheduleStatus(value) {
    return String(value || '')
      .trim()
      .toUpperCase();
  }

  /**
   * Xác định nhãn và màu của một lịch Staff.
   *
   * Màu xanh lá: người thay ca khẩn cấp.
   * Màu vàng: nghỉ có phép.
   * Màu đỏ nhạt: vắng không phép.
   * Màu xanh dương: lịch của chính người đang đăng nhập.
   */
  function getStaffScheduleVisual(row, isMe) {
    const status = normalizeScheduleStatus(row?.status);
    const assignmentType = normalizeScheduleStatus(
      row?.assignmentType
    );

    if (assignmentType === 'EMERGENCY_REPLACEMENT') {
      return {
        label: isMe
          ? 'Bạn được điều động thay ca'
          : 'Nhân viên thay ca',
        background: '#dcfce7',
        borderColor: '#86efac',
        color: '#166534',
      };
    }

    if (status === 'LEAVE_APPROVED') {
      return {
        label: 'Nghỉ có phép',
        background: '#fef3c7',
        borderColor: '#fcd34d',
        color: '#92400e',
      };
    }

    if (status === 'ABSENT') {
      return {
        label: 'Vắng không phép',
        background: '#fee2e2',
        borderColor: '#fca5a5',
        color: '#991b1b',
      };
    }

    return {
      label: isMe ? 'Lịch của bạn' : 'Lịch chính thức',
      background: isMe ? '#dbeafe' : '#f8fafc',
      borderColor: isMe ? '#93c5fd' : '#e2e8f0',
      color: isMe ? '#1e3a8a' : '#475569',
    };
  }

  // boardMatrix[ngày][shiftId] = danh sách lịch trong ô đó.
  const boardMatrix = {};

  dates.forEach((dateObj) => {
    const dateString = toDateString(dateObj);
    boardMatrix[dateString] = {};

    shifts.forEach((shift) => {
      boardMatrix[dateString][shift.id] = registrations.filter((row) => {
        return (
          row.workDate?.slice(0, 10) === dateString &&
          Number(row.shiftId) === Number(shift.id)
        );
      });
    });
  });

  if (loading) {
    return <p>Đang tải bảng lịch làm việc...</p>;
  }

  return (
    <>
      <div style={{ marginBottom: 16 }}>
        <h2
          style={{
            color: '#1d4ed8',
            margin: '0 0 4px',
          }}
        >
          Lịch làm việc chính thức
        </h2>

        <p
          style={{
            margin: 0,
            color: '#64748b',
            fontSize: 13,
          }}
        >
          Lịch nghỉ/vắng vẫn được giữ lại để mọi người biết ai đã được
          điều động thay ca.
        </p>
      </div>

      {scheduleError && (
        <div className="sd-period-message sd-period-message--error">
          {scheduleError}
        </div>
      )}

      <div
        className="sd-board-wrap"
        style={{ borderRadius: 12 }}
      >
        <table className="sd-schedule-board">
          <thead>
            <tr>
              <th style={{ width: 90 }}>NGÀY</th>

              {shifts.map((shift) => (
                <th key={shift.id}>
                  {shift.shiftName}
                  <br />
                  <span
                    style={{
                      fontWeight: 500,
                      fontSize: 11,
                    }}
                  >
                    {shift.startTime?.slice(0, 5)} -{' '}
                    {shift.endTime?.slice(0, 5)}
                  </span>
                </th>
              ))}
            </tr>
          </thead>

          <tbody>
            {dates.map((dateObj) => {
              const dateString = toDateString(dateObj);
              const dayOfWeek = DAY_NAMES[dateObj.getDay()];
              const shortDate =
                `${dateObj.getDate()}/${dateObj.getMonth() + 1}`;

              return (
                <tr key={dateString}>
                  <td className="sd-board-date-col">
                    <strong>{dayOfWeek}</strong>
                    <small>{shortDate}</small>
                  </td>

                  {shifts.map((shift) => {
                    const cellRows =
                      boardMatrix[dateString][shift.id] || [];

                    const isShiftOpen = isShiftOpenOnDate(
                      shift.id,
                      dateObj
                    );

                    // Manager vẫn là một dòng lịch chính thức riêng.
                    const managerRow = cellRows.find(
                      isManagerScheduleRow
                    );

                    const staffRows = cellRows.filter(
                      (row) => !isManagerScheduleRow(row)
                    );

                    const managerName = managerRow
                      ? getScheduleUserName(managerRow)
                      : 'Quản lý ca';

                    return (
                      <td key={shift.id}>
                        {isShiftOpen ? (
                          <>
                            {/* Dòng Manager */}
                            <div
                              className="sd-reg-card"
                              style={{
                                background: '#ffedd5',
                                borderColor: '#fdba74',
                                color: '#9a3412',
                              }}
                            >
                              <span
                                className="sd-reg-name"
                                title={managerName}
                              >
                                {managerName}
                              </span>

                              <span
                                style={{
                                  marginLeft: 6,
                                  fontSize: 11,
                                  fontWeight: 500,
                                }}
                              >
                                Quản lý
                              </span>
                            </div>

                            {/* Các dòng Staff, gồm cả người nghỉ và người thay */}
                            {staffRows.map((row) => {
                              const staffName =
                                getScheduleUserName(row);

                              const isMe =
                                String(row.userId) ===
                                String(user.id);

                              const visual =
                                getStaffScheduleVisual(row, isMe);

                              return (
                                <div
                                  key={row.id}
                                  className="sd-reg-card"
                                  style={{
                                    background: visual.background,
                                    borderColor: visual.borderColor,
                                    color: visual.color,
                                    fontWeight: isMe ? 700 : 500,
                                    display: 'grid',
                                    gap: 3,
                                  }}
                                >
                                  <span
                                    className="sd-reg-name"
                                    title={staffName}
                                  >
                                    {staffName}
                                  </span>

                                  <span
                                    style={{
                                      fontSize: 10,
                                      fontWeight: 700,
                                    }}
                                  >
                                    {visual.label}
                                  </span>

                                  {row.absenceReason && (
                                    <small
                                      title={row.absenceReason}
                                      style={{
                                        fontSize: 10,
                                        opacity: 0.85,
                                      }}
                                    >
                                      Lý do: {row.absenceReason}
                                    </small>
                                  )}
                                </div>
                              );
                            })}
                          </>
                        ) : (
                          <div
                            style={{
                              textAlign: 'center',
                              padding: '16px 0',
                              color: '#cbd5e1',
                              fontSize: 12,
                              fontWeight: 600,
                            }}
                          >
                            KHÔNG CÓ CA LÀM
                          </div>
                        )}
                      </td>
                    );
                  })}
                </tr>
              );
            })}
          </tbody>
        </table>
      </div>
    </>
  );
}

// ==========================================
// 2B. CHẾ ĐỘ: ĐĂNG KÝ CA LÀM
// ==========================================
function RegistrationView({ period, user }) {
  const [shifts, setShifts] = useState([]);
  const [shiftConfigs, setShiftConfigs] = useState([]);
  const [fullTimeStaff, setFullTimeStaff] = useState([]);
  const [allRegistrations, setAllRegistrations] = useState([]);
  const [dates, setDates] = useState([]);

  // registered[ngày][shiftId] lưu trạng thái đang được chọn trên giao diện.
  const [registered, setRegistered] = useState({});

  // dbRegistrations[ngày][shiftId] lưu phiếu thật đang có trong CSDL.
  const [dbRegistrations, setDbRegistrations] = useState({});

  const [capacityMessage, setCapacityMessage] = useState('');
  const [saved, setSaved] = useState(false);
  const [saving, setSaving] = useState(false);
  const [loading, setLoading] = useState(true);
  const [registrationError, setRegistrationError] = useState('');

  const periodStatus = String(period.status || '').toUpperCase();

  const isClosed = [
    'CLOSED',
    'REVIEWING',
    'DRAFT',
  ].includes(periodStatus);

  const isPublished = periodStatus === 'PUBLISHED';
  const isDeadlineReached = hasPeriodStarted(period.startDate);

  const isLocked =
    isClosed ||
    isPublished ||
    isDeadlineReached;

  const normalizedEmploymentType =
    String(user.employmentType || '').toUpperCase();
  const isFullTimeUser =
    normalizedEmploymentType === 'FULL_TIME' ||
    normalizedEmploymentType === 'MATERNITY';

  const isRegistrationDisabled =
    isLocked ||
    isFullTimeUser;

  const isOverdue =
    !isPublished &&
    isDeadlineReached;

  // ========================================================================
  // CÁC HÀM CHUẨN HÓA TRẠNG THÁI
  // ========================================================================
  function normalizeRegistrationStatus(status) {
    return String(status || '')
      .trim()
      .toUpperCase();
  }

  function isRejectedStatus(status = '') {
    const normalized = String(status).toLowerCase();

    return (
      normalized === 'cancelled' ||
      normalized === 'rejected' ||
      normalized.includes('từ chối')
    );
  }

  // Chỉ các trạng thái này mới chiếm một vị trí chính thức của ca.
  function isOfficialRegistrationStatus(status) {
    const normalized = normalizeRegistrationStatus(status);

    return [
      'REGISTERED',
      'APPROVED',
      'ĐÃ DUYỆT',
      'CHỜ DUYỆT',
    ].includes(normalized);
  }

  function isWaitlistStatus(status) {
    return (
      normalizeRegistrationStatus(status) === 'WAITLIST'
    );
  }

  // Staff được phép tự hủy REGISTERED hoặc WAITLIST khi đợt còn OPEN.
  function isCancellableRegistrationStatus(status) {
    const normalized = normalizeRegistrationStatus(status);

    return [
      'REGISTERED',
      'WAITLIST',
      'CHỜ DUYỆT',
    ].includes(normalized);
  }

  // ========================================================================
  // TẢI DỮ LIỆU ĐĂNG KÝ
  // ========================================================================
  useEffect(() => {
    let isMounted = true;

    async function loadData() {
      setLoading(true);
      setRegistrationError('');

      try {
        const [allShifts, configRes, periodRegRes, myRegRes, automaticStaff] =
          await Promise.all([
            getAllShifts(),
            axios.get('/api/BranchShiftConfig'),
            axios.get(
              `/api/StaffRegistration/period/${period.id}`
            ),
            axios.get(
              `/api/StaffRegistration/my-schedule/${user.id}/${period.id}`
            ),
            getAutomaticFullTimeStaff(user.branchId),
          ]);

        if (!isMounted) return;

        const branchShifts = (allShifts || []).filter((shift) => {
          return String(shift.branchId) === String(user.branchId);
        });

        const branchShiftIds = new Set(
          branchShifts.map((shift) => shift.id)
        );

        const branchFullTimeStaff = Array.isArray(automaticStaff)
          ? automaticStaff
          : [];

        setShifts(branchShifts);
        setFullTimeStaff(branchFullTimeStaff);
        setShiftConfigs(
          (configRes.data || []).filter((config) => {
            return branchShiftIds.has(config.shiftId);
          })
        );
        setAllRegistrations(periodRegRes.data || []);

        const dateArray = [];
        let currentDate = new Date(period.startDate);
        const endDate = new Date(period.endDate);

        while (currentDate <= endDate) {
          dateArray.push(new Date(currentDate));
          currentDate.setDate(currentDate.getDate() + 1);
        }

        setDates(dateArray);

        // Chỉ giữ các phiếu chưa bị hủy hoặc từ chối.
        const myRegistrations = (myRegRes.data || []).filter(
          (registration) => !isRejectedStatus(registration.status)
        );

        const dbMap = {};
        const initialSelectedMap = {};

        myRegistrations.forEach((registration) => {
          const dateString = registration.workDate.slice(0, 10);

          if (!dbMap[dateString]) {
            dbMap[dateString] = {};
            initialSelectedMap[dateString] = {};
          }

          dbMap[dateString][registration.shiftId] = {
            id: registration.id,
            status: registration.status,
          };

          initialSelectedMap[dateString][registration.shiftId] = true;
        });

        setDbRegistrations(dbMap);
        setRegistered(initialSelectedMap);
      } catch (error) {
        console.error('Lỗi tải dữ liệu đăng ký:', error);

        if (isMounted) {
          setRegistrationError(
            getApiErrorMessage(
              error,
              'Không thể tải dữ liệu đăng ký ca.'
            )
          );
        }
      } finally {
        if (isMounted) {
          setLoading(false);
        }
      }
    }

    loadData();

    return () => {
      isMounted = false;
    };
  }, [period.id, period.startDate, period.endDate, user.id, user.branchId]);

  // Khi đợt bị khóa, trả giao diện về đúng dữ liệu đang lưu trong CSDL.
  useEffect(() => {
    if (!isLocked) return;

    const savedRegistrations = {};

    Object.entries(dbRegistrations).forEach(
      ([dateString, shiftsInfo]) => {
        savedRegistrations[dateString] = {};

        Object.keys(shiftsInfo).forEach((shiftId) => {
          savedRegistrations[dateString][shiftId] = true;
        });
      }
    );

    setRegistered(savedRegistrations);
    setSaved(false);
    setCapacityMessage('');
  }, [isLocked, dbRegistrations]);

  function toDateString(dateObj) {
    const offset = dateObj.getTimezoneOffset();
    const normalizedDate = new Date(
      dateObj.getTime() - offset * 60 * 1000
    );

    return normalizedDate.toISOString().split('T')[0];
  }

  // Lấy MaxStaff đúng theo ca và thứ trong tuần.
  function getTotalMaxStaffForShiftDate(shiftId, dateObj) {
    const dayName = dateObj.toLocaleDateString(
      'en-US',
      { weekday: 'long' }
    );

    const config = shiftConfigs.find((item) => {
      return (
        Number(item.shiftId) === Number(shiftId) &&
        String(item.dayOfWeek).toLowerCase() ===
          dayName.toLowerCase()
      );
    });

    const shift = shifts.find((item) => {
      return Number(item.id) === Number(shiftId);
    });

    return Number(config?.maxStaff ?? shift?.maxStaff ?? 0);
  }

  // Manager và FULL_TIME được tự động xếp lịch nên giữ chỗ trước.
  function getStaffSlotForShiftDate(shiftId, dateObj) {
    const totalMaxStaff = getTotalMaxStaffForShiftDate(
      shiftId,
      dateObj
    );

    return Math.max(
      totalMaxStaff - 1 - fullTimeStaff.length,
      0
    );
  }

  // Chỉ đếm REGISTERED; WAITLIST không chiếm vị trí chính thức.
  function getRegisteredCount(dateString, shiftId) {
    return allRegistrations.filter((item) => {
      return (
        item.workDate?.slice(0, 10) === dateString &&
        Number(item.shiftId) === Number(shiftId) &&
        isOfficialRegistrationStatus(item.status) &&
        !fullTimeStaff.some((staff) => {
          return String(staff.id) === String(item.userId);
        })
      );
    }).length;
  }

  function isShiftFull(dateString, shiftId, dateObj) {
    const staffSlot = getStaffSlotForShiftDate(
      shiftId,
      dateObj
    );

    if (staffSlot <= 0) return true;

    return (
      getRegisteredCount(dateString, shiftId) >= staffSlot
    );
  }

  // ========================================================================
  // STAFF CHỌN HOẶC BỎ CHỌN MỘT CA
  //
  // Điểm quan trọng:
  // - Ca chưa đầy: Backend sẽ tạo REGISTERED.
  // - Ca đã đầy: Frontend vẫn cho chọn, Backend sẽ tạo WAITLIST.
  // ========================================================================
  function toggle(dateString, shiftId, dateObj) {
    const dbItem = dbRegistrations[dateString]?.[shiftId];

    if (isLocked) return;

    if (
      dbItem &&
      !isCancellableRegistrationStatus(dbItem.status)
    ) {
      return;
    }

    if (
      !dbItem &&
      isShiftFull(dateString, shiftId, dateObj)
    ) {
      setCapacityMessage(
        'Ca đã đủ vị trí chính thức. Khi lưu, đăng ký của bạn sẽ được đưa vào danh sách chờ.'
      );
    } else {
      setCapacityMessage('');
    }

    setSaved(false);
    setRegistered((previous) => {
      const dayRegistrations = previous[dateString] || {};

      return {
        ...previous,
        [dateString]: {
          ...dayRegistrations,
          [shiftId]: !dayRegistrations[shiftId],
        },
      };
    });
  }

  // So sánh state giao diện với dữ liệu CSDL để tìm các ca cần thêm/hủy.
  function getChanges() {
    const adds = [];
    const deletes = [];

    Object.entries(registered).forEach(
      ([dateString, shiftsInfo]) => {
        Object.entries(shiftsInfo).forEach(
          ([shiftId, isSelected]) => {
            if (
              isSelected &&
              !dbRegistrations[dateString]?.[shiftId]
            ) {
              adds.push({
                // UserId vẫn được gửi để tương thích giao diện cũ,
                // nhưng Backend sẽ ưu tiên ID lấy từ JWT.
                userId: user.id,
                periodId: period.id,
                shiftId: Number(shiftId),
                workDate: dateString,
              });
            }
          }
        );
      }
    );

    Object.entries(dbRegistrations).forEach(
      ([dateString, shiftsInfo]) => {
        Object.entries(shiftsInfo).forEach(
          ([shiftId, dbItem]) => {
            const isSelectedNow =
              registered[dateString]?.[shiftId];

            if (
              !isSelectedNow &&
              isCancellableRegistrationStatus(dbItem.status)
            ) {
              deletes.push(dbItem.id);
            }
          }
        );
      }
    );

    return { adds, deletes };
  }

  // Lưu toàn bộ thay đổi đăng ký.
  async function handleSave() {
    if (isLocked) {
      alert(
        isOverdue
          ? 'Đợt đăng ký đã đến hạn nên không thể thay đổi ca.'
          : 'Đợt đăng ký đã khóa nên không thể thay đổi ca.'
      );
      return;
    }

    const { adds, deletes } = getChanges();

    if (adds.length === 0 && deletes.length === 0) {
      alert('Không có thay đổi nào để lưu!');
      return;
    }

    setSaving(true);
    setRegistrationError('');

    // Hai biến này dùng để thông báo có bao nhiêu ca chính thức
    // và bao nhiêu ca được đưa vào WAITLIST.
    let registeredCreatedCount = 0;
    let waitlistCreatedCount = 0;

    try {
      // Hủy các phiếu bị bỏ chọn trước.
      for (const registrationId of deletes) {
        await axios.delete(
          `/api/StaffRegistration/${registrationId}/user/${user.id}`
        );
      }

      // Gửi lần lượt để giữ đúng nguyên tắc ai đăng ký trước được nhận trước.
      for (const payload of adds) {
        const response = await axios.post(
          '/api/StaffRegistration',
          payload
        );

        const createdStatus = normalizeRegistrationStatus(
          response.data?.status
        );

        if (createdStatus === 'WAITLIST') {
          waitlistCreatedCount += 1;
        } else if (createdStatus === 'REGISTERED') {
          registeredCreatedCount += 1;
        }
      }

      // Tải lại dữ liệu thật sau khi Backend đã lưu xong.
      const [myRegRes, periodRegRes] = await Promise.all([
        axios.get(
          `/api/StaffRegistration/my-schedule/${user.id}/${period.id}`
        ),
        axios.get(
          `/api/StaffRegistration/period/${period.id}`
        ),
      ]);

      setAllRegistrations(periodRegRes.data || []);

      const dbMap = {};
      const selectedMap = {};

      (myRegRes.data || [])
        .filter((registration) => {
          return !isRejectedStatus(registration.status);
        })
        .forEach((registration) => {
          const dateString = registration.workDate.slice(0, 10);

          if (!dbMap[dateString]) {
            dbMap[dateString] = {};
            selectedMap[dateString] = {};
          }

          dbMap[dateString][registration.shiftId] = {
            id: registration.id,
            status: registration.status,
          };

          selectedMap[dateString][registration.shiftId] = true;
        });

      setDbRegistrations(dbMap);
      setRegistered(selectedMap);
      setSaved(true);
      setCapacityMessage('');

      if (waitlistCreatedCount > 0) {
        alert(
          `Đã lưu đăng ký. ${registeredCreatedCount} ca chính thức, ` +
          `${waitlistCreatedCount} ca trong danh sách chờ.`
        );
      } else {
        alert('✅ Đã lưu đăng ký ca thành công!');
      }
    } catch (error) {
      console.error('Lỗi lưu đăng ký:', error);

      const message = getApiErrorMessage(
        error,
        'Có lỗi xảy ra khi lưu đăng ký.'
      );

      setRegistrationError(message);
      alert(`❌ Lỗi: ${message}`);
    } finally {
      setSaving(false);
    }
  }

  // Hoàn tác các thay đổi chưa lưu trên giao diện.
  function handleReset() {
    const resetRegistrations = {};

    Object.keys(dbRegistrations).forEach((dateString) => {
      resetRegistrations[dateString] = {};

      Object.keys(dbRegistrations[dateString]).forEach((shiftId) => {
        resetRegistrations[dateString][shiftId] = true;
      });
    });

    setRegistered(resetRegistrations);
    setSaved(false);
    setCapacityMessage('');
  }

  if (loading) {
    return <p>Đang tải form đăng ký...</p>;
  }

  const { adds, deletes } = getChanges();
  const totalChanges = adds.length + deletes.length;

  return (
    <>
      <div style={{ marginBottom: 16 }}>
        <h2
          style={{
            color: '#ea580c',
            margin: '0 0 4px',
          }}
        >
          Đăng ký ca làm việc
        </h2>

        {isLocked && (
          <div
            className={`sd-period-message ${
              isOverdue
                ? 'sd-period-message--overdue'
                : 'sd-period-message--locked'
            }`}
          >
            <strong>
              {isOverdue
                ? 'Đợt đăng ký đã đến hạn nhưng lịch chưa được công bố.'
                : 'Đợt đăng ký đã được khóa.'}
            </strong>{' '}
            Bạn chỉ có thể xem các ca đã đăng ký, không thể thêm hoặc hủy ca.
          </div>
        )}


        <p
          style={{
            fontSize: 13,
            color: '#64748b',
            margin: 0,
          }}
        >
          {isLocked
            ? 'Bạn đang xem lại các ca đã đăng ký và chờ Quản lý công bố lịch làm chính thức.'
            : 'Ca còn chỗ sẽ nhận REGISTERED; ca đã đủ người vẫn có thể đăng ký vào WAITLIST.'}
        </p>
      </div>

      {registrationError && (
        <div className="sd-period-message sd-period-message--error">
          {registrationError}
        </div>
      )}

      {capacityMessage && (
        <div
          style={{
            background: '#faf5ff',
            color: '#6d28d9',
            padding: '10px 14px',
            borderRadius: 8,
            margin: '-4px 0 16px',
            border: '1px solid #c4b5fd',
            fontWeight: 700,
          }}
        >
          {capacityMessage}
        </div>
      )}

      {shifts.length > 0 && dates.length > 0 && (
        <div
          className="sd-board-wrap"
          style={{ borderRadius: 12 }}
        >
          <table className="sd-schedule-board sd-registration-board">
            <thead>
              <tr>
                <th style={{ width: 90 }}>NGÀY</th>

                {shifts.map((shift) => (
                  <th key={shift.id}>
                    {shift.shiftName}
                    <br />
                    <span
                      style={{
                        fontWeight: 500,
                        fontSize: 11,
                      }}
                    >
                      {shift.startTime?.slice(0, 5)} -{' '}
                      {shift.endTime?.slice(0, 5)}
                    </span>
                  </th>
                ))}
              </tr>
            </thead>

            <tbody>
              {dates.map((dateObj) => {
                const dateString = toDateString(dateObj);
                const dayOfWeek = DAY_NAMES[dateObj.getDay()];
                const shortDate =
                  `${dateObj.getDate()}/${dateObj.getMonth() + 1}`;

                return (
                  <tr key={dateString}>
                    <td className="sd-board-date-col">
                      <strong>{dayOfWeek}</strong>
                      <small>{shortDate}</small>
                    </td>

                    {shifts.map((shift) => {
                      const isSelected =
                        registered[dateString]?.[shift.id] || false;

                      const dbItem =
                        dbRegistrations[dateString]?.[shift.id];

                      const isCellLocked =
                        isRegistrationDisabled ||
                        (
                          dbItem &&
                          !isCancellableRegistrationStatus(
                            dbItem.status
                          )
                        );

                      const totalMaxStaff =
                        getTotalMaxStaffForShiftDate(
                          shift.id,
                          dateObj
                        );

                      const staffSlot =
                        getStaffSlotForShiftDate(
                          shift.id,
                          dateObj
                        );

                      const hasSavedRegistration = Boolean(dbItem);
                      const isPendingCancel =
                        hasSavedRegistration && !isSelected;

                      // isFull chỉ dùng để báo Staff sẽ vào WAITLIST.
                      // Không dùng isFull để disabled nút.
                      const isFull =
                        !isSelected &&
                        !hasSavedRegistration &&
                        isShiftFull(
                          dateString,
                          shift.id,
                          dateObj
                        );

                      const savedRows = allRegistrations.filter((item) => {
                        return (
                          item.workDate?.slice(0, 10) === dateString &&
                          Number(item.shiftId) === Number(shift.id) &&
                          !isRejectedStatus(item.status)
                        );
                      });

                      const savedOfficialRows = savedRows.filter((item) => {
                        return isOfficialRegistrationStatus(item.status) &&
                          !fullTimeStaff.some((staff) => {
                            return String(staff.id) === String(item.userId);
                          });
                      });

                      const savedWaitlistRows = savedRows
                        .filter((item) => {
                          return isWaitlistStatus(item.status);
                        })
                        .sort((first, second) => {
                          const firstTime = String(first.registeredAt || '');
                          const secondTime = String(second.registeredAt || '');

                          return (
                            firstTime.localeCompare(secondTime) ||
                            Number(first.id) - Number(second.id)
                          );
                        });

                      const hasMeInSavedList = savedRows.some((item) => {
                        return String(item.userId) === String(user.id);
                      });

                      // Danh sách chính thức: chỉ REGISTERED chiếm slot.
                      const displayStaffList = savedOfficialRows
                        .filter((item) => {
                          const isMe =
                            String(item.userId) === String(user.id);

                          return !isMe || isSelected;
                        })
                        .map((item) => {
                          const isMe =
                            String(item.userId) === String(user.id);

                          return {
                            id: item.id,
                            name: isMe
                              ? user.fullName || 'Bạn'
                              : item.user?.fullName ||
                                item.user?.username ||
                                'Nhân viên',
                            isMe,
                            isPending: false,
                          };
                        });

                      // Danh sách chờ được hiển thị riêng và không trừ slot.
                      const displayWaitlist = savedWaitlistRows
                        .filter((item) => {
                          const isMe =
                            String(item.userId) === String(user.id);

                          return !isMe || isSelected;
                        })
                        .map((item, index) => {
                          const isMe =
                            String(item.userId) === String(user.id);

                          return {
                            id: item.id,
                            name: isMe
                              ? user.fullName || 'Bạn'
                              : item.user?.fullName ||
                                item.user?.username ||
                                'Nhân viên',
                            isMe,
                            queuePosition: index + 1,
                            isPending: false,
                          };
                        });

                      // Ca mới được chọn nhưng chưa lưu:
                      // - Ca đầy: hiển thị tạm ở WAITLIST.
                      // - Ca còn chỗ: hiển thị tạm trong danh sách chính thức.
                      if (isSelected && !hasMeInSavedList) {
                        const pendingItem = {
                          id: `new-${dateString}-${shift.id}`,
                          name: user.fullName || 'Bạn',
                          isMe: true,
                          isPending: true,
                        };

                        const shiftIsFull = isShiftFull(
                          dateString,
                          shift.id,
                          dateObj
                        );

                        if (shiftIsFull) {
                          displayWaitlist.push({
                            ...pendingItem,
                            queuePosition: displayWaitlist.length + 1,
                          });
                        } else {
                          displayStaffList.push(pendingItem);
                        }
                      }

                      const emptySlotCount = Math.max(
                        staffSlot - displayStaffList.length,
                        0
                      );

                      return (
                        <td
                          key={shift.id}
                          className="sd-registration-cell"
                        >
                          <button
                            className={`sd-shift-cell-v sd-shift-cell-slots ${
                              isSelected ? 'selected' : ''
                            } ${
                              isPendingCancel ? 'pending-cancel' : ''
                            } ${
                              isCellLocked ? 'disabled' : ''
                            }`}
                            onClick={() =>
                              toggle(
                                dateString,
                                shift.id,
                                dateObj
                              )
                            }
                            type="button"
                            disabled={isCellLocked}
                            aria-pressed={isSelected}
                            title={
                              isPendingCancel
                                ? 'Ca đã được bỏ chọn. Bấm lại để hoàn tác hoặc lưu để hủy ca.'
                                : isSelected
                                  ? isWaitlistStatus(dbItem?.status)
                                    ? 'Bạn đang ở danh sách chờ. Bấm để bỏ đăng ký.'
                                    : 'Ca đang được chọn. Bấm để bỏ chọn.'
                                  : isFull
                                    ? 'Ca đã đủ vị trí chính thức. Bấm để đăng ký vào danh sách chờ.'
                                    : 'Bấm để chọn ca.'
                            }
                          >
                            <div className="sd-slot-list">
                              {totalMaxStaff > 0 && (
                                <div className="sd-slot-person sd-slot-manager">
                                  <span className="sd-slot-name">
                                    Quản lý
                                  </span>
                                </div>
                              )}

                              {totalMaxStaff > 0 && fullTimeStaff.map((staff) => (
                                <div
                                  key={`full-time-${staff.id}`}
                                  className={`sd-slot-person ${
                                    String(staff.id) === String(user.id)
                                      ? 'sd-slot-me'
                                      : 'sd-slot-staff'
                                  }`}
                                >
                                  <span className="sd-slot-name">
                                    {String(staff.id) === String(user.id)
                                      ? user.fullName || 'Bạn'
                                      : staff.fullName || staff.username || 'Nhân viên'}
                                  </span>
                                  <span className="sd-slot-saved">
                                    Full-time
                                  </span>
                                </div>
                              ))}

                              {/* Các Staff đang giữ vị trí chính thức */}
                              {displayStaffList.map((staff) => (
                                <div
                                  key={staff.id}
                                  className={`sd-slot-person ${
                                    staff.isMe
                                      ? 'sd-slot-me'
                                      : 'sd-slot-staff'
                                  }`}
                                >
                                  <span className="sd-slot-name">
                                    {staff.name}
                                  </span>

                                  {staff.isPending ? (
                                    <span className="sd-slot-pending">
                                      Chưa lưu
                                    </span>
                                  ) : staff.isMe ? (
                                    <span className="sd-slot-saved">
                                      Đã đăng ký chính thức
                                    </span>
                                  ) : null}
                                </div>
                              ))}

                              {/* WAITLIST hiển thị riêng bên dưới */}
                              {displayWaitlist.length > 0 && (
                                <div
                                  style={{
                                    marginTop: 7,
                                    paddingTop: 7,
                                    borderTop: '1px dashed #cbd5e1',
                                    display: 'grid',
                                    gap: 5,
                                  }}
                                >
                                  <div
                                    style={{
                                      fontSize: 10,
                                      fontWeight: 800,
                                      color: '#7c3aed',
                                      textTransform: 'uppercase',
                                    }}
                                  >
                                    Danh sách chờ
                                  </div>

                                  {displayWaitlist.map((staff) => (
                                    <div
                                      key={`waitlist-${staff.id}`}
                                      className={`sd-slot-person ${
                                        staff.isMe
                                          ? 'sd-slot-me'
                                          : 'sd-slot-staff'
                                      }`}
                                      style={{
                                        background: staff.isMe
                                          ? '#ede9fe'
                                          : '#faf5ff',
                                        borderColor: '#c4b5fd',
                                        color: '#6d28d9',
                                      }}
                                    >
                                      <span className="sd-slot-name">
                                        #{staff.queuePosition}{' '}
                                        {staff.name}
                                      </span>

                                      <span
                                        style={{
                                          fontSize: 10,
                                          fontWeight: 700,
                                        }}
                                      >
                                        {staff.isPending
                                          ? 'Sẽ vào danh sách chờ'
                                          : 'Danh sách chờ'}
                                      </span>
                                    </div>
                                  ))}
                                </div>
                              )}

                              {isPendingCancel && (
                                <div className="sd-slot-cancel-pending">
                                  <span>Đã bỏ chọn</span>
                                  <small>Chưa lưu</small>
                                </div>
                              )}

                              {/* Còn trống chỉ dựa trên số REGISTERED */}
                              {Array.from({
                                length: emptySlotCount,
                              }).map((_, index) => (
                                <div
                                  key={`empty-${dateString}-${shift.id}-${index}`}
                                  className="sd-slot-empty"
                                >
                                  Còn trống
                                </div>
                              ))}

                              {staffSlot <= 0 && (
                                <div className="sd-slot-empty">
                                  Không có slot nhân viên
                                </div>
                              )}

                              {isFull && (
                                <div className="sd-slot-full-text">
                                  Ca đã đủ người — vẫn có thể đăng ký chờ
                                </div>
                              )}
                            </div>
                          </button>
                        </td>
                      );
                    })}
                  </tr>
                );
              })}
            </tbody>
          </table>
        </div>
      )}

      <div className="sd-shift-actions">
        <button
          className="sd-btn-ghost"
          onClick={handleReset}
          type="button"
          disabled={totalChanges === 0}
        >
          Hoàn tác thay đổi
        </button>

        <button
          className="sd-btn-primary"
          disabled={saving || totalChanges === 0 || isRegistrationDisabled}
          onClick={handleSave}
          type="button"
        >
          {saving
            ? 'Đang lưu…'
            : `Xác nhận lưu thay đổi (${totalChanges} ca)`}
        </button>
      </div>

      {saved && totalChanges === 0 && (
        <p
          className="sd-save-notice"
          style={{
            color: '#15803d',
            fontSize: 13,
            marginTop: 12,
            textAlign: 'center',
          }}
        >
          Đăng Ký Ca Thành Công
        </p>
      )}
    </>
  );
}
