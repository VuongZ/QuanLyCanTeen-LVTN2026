import { useEffect, useMemo, useState } from 'react';
import {
  createSalaryComplaint,
  getMySalaryComplaints,
  getSalaryAdjustmentHistory,
  getSalaryByUser,
  getSalaryWorkDetails,
} from '../../api/SalaryApi';
import { formatVietnamTime } from '../../utils/vietnamDateTime';

function formatMoney(value) {
  return new Intl.NumberFormat('vi-VN', {
    style: 'currency',
    currency: 'VND',
    maximumFractionDigits: 0,
  }).format(Number(value || 0));
}

function formatNumber(value) {
  return new Intl.NumberFormat('vi-VN', {
    maximumFractionDigits: 2,
  }).format(Number(value || 0));
}

function formatDate(value) {
  if (!value) return '—';

  const [year, month, day] = String(value).split('-');

  if (!year || !month || !day) return value;

  return `${day}/${month}/${year}`;
}

function formatTime(value) {
  return formatVietnamTime(value);
}

function formatWorkStatus(status) {
  const statusMap = {
    COMPLETED: 'Đã hoàn thành',
    WORKING: 'Đang trong ca',
    NOT_STARTED: 'Chưa bắt đầu',
  };

  return statusMap[(status || '').toUpperCase()] || status || '—';
}

function getWorkStatusClass(status) {
  const normalized = (status || '').toUpperCase();

  if (normalized === 'COMPLETED') return 'completed';
  if (normalized === 'WORKING') return 'working';

  return 'not-started';
}

function getAdjustmentStatus(status) {
  const normalized = (status || 'PENDING').toUpperCase();
  if (normalized === 'APPROVED') return { label: 'Đã duyệt', className: 'approved' };
  if (normalized === 'REJECTED') return { label: 'Từ chối', className: 'rejected' };
  return { label: 'Chờ Admin', className: 'pending' };
}

function SalaryMetric({ label, value }) {
  return (
    <div className="sd-salary-metric">
      <span>{label}</span>
      <strong>{value}</strong>
    </div>
  );
}

export function SalaryTab({ user }) {
  const [salaries, setSalaries] = useState([]);
  const [selectedYear, setSelectedYear] = useState('');
const [selectedMonth, setSelectedMonth] = useState('');

  const [workDetails, setWorkDetails] = useState([]);
  const [adjustmentHistory, setAdjustmentHistory] = useState([]);
  const [complaints, setComplaints] = useState([]);
  const [complaintContent, setComplaintContent] = useState('');
  const [complaintSaving, setComplaintSaving] = useState(false);
  const [complaintError, setComplaintError] = useState('');

  const [salaryLoading, setSalaryLoading] = useState(false);
  const [detailLoading, setDetailLoading] = useState(false);
  const [historyLoading, setHistoryLoading] = useState(false);

  const [salaryError, setSalaryError] = useState('');
  const [detailError, setDetailError] = useState('');
  const [historyError, setHistoryError] = useState('');

  useEffect(() => {
    async function loadSalary() {
      if (!user?.id) return;

      setSalaryLoading(true);
      setSalaryError('');

      try {
        const [data, complaintData] = await Promise.all([
          getSalaryByUser(user.id),
          getMySalaryComplaints(),
        ]);
        const nextSalaries = Array.isArray(data) ? data : [];

        setSalaries(nextSalaries);
        setComplaints(Array.isArray(complaintData) ? complaintData : []);

       if (nextSalaries[0]) {
  setSelectedYear(String(nextSalaries[0].year));
  setSelectedMonth(String(nextSalaries[0].month));
} else {
  setSelectedYear('');
  setSelectedMonth('');
}
      } catch (error) {
        setSalaryError(
          error.response?.data?.message ||
          'Không tải được dữ liệu lương.',
        );
      } finally {
        setSalaryLoading(false);
      }
    }

    loadSalary();
  }, [user?.id]);

const yearOptions = useMemo(() => {
  return [...new Set(salaries.map((item) => item.year))]
    .sort((a, b) => b - a);
}, [salaries]);

const monthOptions = useMemo(() => {
  if (!selectedYear) return [];

  return salaries
    .filter((item) => item.year === Number(selectedYear))
    .map((item) => item.month)
    .filter((month, index, array) => array.indexOf(month) === index)
    .sort((a, b) => b - a);
}, [salaries, selectedYear]);

useEffect(() => {
  if (monthOptions.length === 0) {
    setSelectedMonth('');
    return;
  }

  const currentMonthExists = monthOptions.includes(
    Number(selectedMonth),
  );

  if (!currentMonthExists) {
    setSelectedMonth(String(monthOptions[0]));
  }
}, [monthOptions, selectedMonth]);

const selectedSalary = useMemo(() => {
  return salaries.find(
    (item) =>
      item.year === Number(selectedYear) &&
      item.month === Number(selectedMonth),
  ) || null;
}, [salaries, selectedYear, selectedMonth]);

  const isTemporarySalary =
    (selectedSalary?.status || 'PENDING').toUpperCase() === 'PENDING';

  const selectedComplaint = useMemo(
    () => complaints.find((item) => item.salaryId === selectedSalary?.id) || null,
    [complaints, selectedSalary?.id],
  );

  async function submitComplaint(event) {
    event.preventDefault();
    if (!selectedSalary || !complaintContent.trim()) return;

    setComplaintSaving(true);
    setComplaintError('');
    try {
      const created = await createSalaryComplaint(
        selectedSalary.id,
        complaintContent.trim(),
      );
      setComplaints((current) => [created, ...current]);
      setComplaintContent('');
    } catch (error) {
      setComplaintError(
        error.response?.data?.message || 'Không thể gửi khiếu nại lương.',
      );
    } finally {
      setComplaintSaving(false);
    }
  }

  useEffect(() => {
    async function loadWorkDetails() {
      if (!user?.id || !selectedSalary) {
        setWorkDetails([]);
        return;
      }

      setDetailLoading(true);
      setDetailError('');

      try {
        const data = await getSalaryWorkDetails(
          user.id,
          selectedSalary.month,
          selectedSalary.year,
        );

        setWorkDetails(Array.isArray(data) ? data : []);
      } catch (error) {
        setWorkDetails([]);

        setDetailError(
          error.response?.data?.message ||
          'Không tải được chi tiết ngày làm.',
        );
      } finally {
        setDetailLoading(false);
      }
    }

    loadWorkDetails();
  }, [user?.id, selectedSalary]);

  useEffect(() => {
    async function loadAdjustmentHistory() {
      if (!user?.id || !selectedSalary) {
        setAdjustmentHistory([]);
        return;
      }

      setHistoryLoading(true);
      setHistoryError('');
      try {
        const data = await getSalaryAdjustmentHistory(
          user.id,
          selectedSalary.month,
          selectedSalary.year,
        );
        setAdjustmentHistory(Array.isArray(data) ? data : []);
      } catch (error) {
        setAdjustmentHistory([]);
        setHistoryError(error.response?.data?.message || 'Không tải được lịch sử thưởng/phạt.');
      } finally {
        setHistoryLoading(false);
      }
    }

    loadAdjustmentHistory();
  }, [user?.id, selectedSalary]);

  const summary = useMemo(() => {
  const salary =
    Number(selectedSalary?.totalSalary || 0);

  const currentBhxh =
    Number(
      selectedSalary?.currentBhxhDeduction || 0
    );

  const previousBhxhRecovery =
    Number(
      selectedSalary?.previousBhxhRecovery || 0
    );

  const totalBhxh =
    Number(
      selectedSalary?.socialInsuranceDeduction || 0
    );

  const backendNetSalary =
    Number(selectedSalary?.netSalary);

  const netSalary =
    Number.isFinite(backendNetSalary)
      ? backendNetSalary
      : Math.max(
          0,
          salary - totalBhxh
        );

  return {
    hours:
      Number(selectedSalary?.totalHours || 0),

    salary,

    bonus:
      Number(selectedSalary?.totalBonus || 0),

    penalty:
      Number(selectedSalary?.totalPenalty || 0),

    currentBhxh,

    previousBhxhRecovery,

    totalBhxh,

    netSalary,
  };
}, [selectedSalary]);

  return (
    <div className="sd-profile-layout sd-salary-layout">
      <div className="sd-salary-summary">
  <SalaryMetric
    label="Tổng giờ làm"
    value={`${formatNumber(summary.hours)} giờ`}
  />

  <SalaryMetric
    label="Lương trước BHXH"
    value={formatMoney(summary.salary)}
  />

  <SalaryMetric
    label="Thưởng"
    value={formatMoney(summary.bonus)}
  />

  <SalaryMetric
    label="Phạt"
    value={formatMoney(summary.penalty)}
  />

  <SalaryMetric
    label="BHXH tháng hiện tại"
    value={`− ${formatMoney(summary.currentBhxh)}`}
  />

  <SalaryMetric
    label="Thu hồi khoản ứng cũ"
    value={`− ${formatMoney(
      summary.previousBhxhRecovery
    )}`}
  />

  <SalaryMetric
    label="Tổng khấu trừ BHXH"
    value={`− ${formatMoney(summary.totalBhxh)}`}
  />

  <SalaryMetric
    label="Lương thực nhận"
    value={formatMoney(summary.netSalary)}
  />
</div>

      {selectedSalary && isTemporarySalary && (
        <p className="sd-status">
          Đây là bảng lương tạm tính. Tổng giờ, hệ số lương theo ngày,
          thưởng, phạt và số tiền có thể thay đổi trước khi Manager chốt lương.
        </p>
      )}

      <div className="sd-card sd-work-detail-card">
        <div className="sd-card-header sd-salary-header">
          <div>
            <p className="sd-eyebrow">Chi tiết chấm công</p>

            <h2>
              {selectedSalary
                ? `Ngày làm trong tháng ${selectedSalary.month}/${selectedSalary.year}`
                : 'Chi tiết ngày làm'}
            </h2>
          </div>

         {yearOptions.length > 0 && (
  <div className="sd-salary-period-filter">
    

    <div className="sd-field sd-salary-filter">
      <label>Tháng</label>

      <select
        value={selectedMonth}
        onChange={(event) =>
          setSelectedMonth(event.target.value)
        }
      >
        {monthOptions.map((month) => (
          <option key={month} value={month}>
            Tháng {month}
          </option>
        ))}
      </select>
    </div>

    <div className="sd-field sd-salary-filter">
      <label>Năm</label>

      <select
        value={selectedYear}
        onChange={(event) =>
          setSelectedYear(event.target.value)
        }
      >
        {yearOptions.map((year) => (
          <option key={year} value={year}>
            {year}
          </option>
        ))}
      </select>
    </div>
  </div>
)}
        </div>

        {salaryError && (
          <p className="sd-status sd-status-error">
            {salaryError}
          </p>
        )}

        {detailError && (
          <p className="sd-status sd-status-error">
            {detailError}
          </p>
        )}

        {salaryLoading || detailLoading ? (
          <p className="sd-salary-empty">
            Đang tải dữ liệu ngày làm...
          </p>
        ) : !selectedSalary ? (
          <p className="sd-salary-empty">
            Chưa có bảng lương tạm hoặc bảng lương đã chốt.
          </p>
        ) : workDetails.length === 0 ? (
          <p className="sd-salary-empty">
            Chưa có dữ liệu điểm danh trong kỳ lương này.
          </p>
        ) : (
          <div className="sd-salary-table-wrap">
            <table className="sd-salary-table sd-work-detail-table">
              <thead>
                <tr>
                  <th>Ngày làm</th>
                  <th>Ca làm</th>
                  <th>Giờ ca</th>
                  <th>Vào ca</th>
                  <th>Ra ca</th>
                  <th>Số giờ làm</th>
                  <th>Hệ số lương</th>
                  <th>Loại ngày</th>
                  <th>Lương ca</th>
                  <th>Trạng thái</th>
                </tr>
              </thead>

              <tbody>
                {workDetails.map((item) => (
                  <tr key={item.attendanceId}>
                    <td>
                      <strong>{formatDate(item.workDate)}</strong>
                    </td>

                    <td>{item.shiftName}</td>

                    <td>
                      {item.startTime} – {item.endTime}
                    </td>

                    <td>{formatTime(item.checkInTime)}</td>

                    <td>{formatTime(item.checkOutTime)}</td>

                    <td className="sd-worked-hours">
                      {formatNumber(item.workedHours)} giờ
                    </td>

                    <td className="sd-salary-coefficient">
                      {formatNumber(item.salaryCoefficient)}
                    </td>

                    <td>
                      <span
                        className={
                          `sd-day-type ${
                            item.isWeekend ? 'weekend' : 'weekday'
                          }`
                        }
                      >
                        {item.isWeekend ? 'Cuối tuần' : 'Ngày thường'}
                      </span>
                    </td>

                    <td className="sd-daily-salary">
                      {formatMoney(item.totalSalary)}
                    </td>

                    <td>
                      <span
                        className={
                          `sd-work-status ${
                            getWorkStatusClass(item.status)
                          }`
                        }
                      >
                        {formatWorkStatus(item.status)}
                      </span>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </div>

      {selectedSalary && (
        <div className="sd-card sd-salary-complaint-card">
          <div className="sd-card-header">
            <div>
              <p className="sd-eyebrow">Phản hồi bảng lương</p>
              <h2>Khiếu nại lương tháng {selectedSalary.month}/{selectedSalary.year}</h2>
            </div>
          </div>

          {selectedComplaint ? (
            <div className="sd-complaint-detail">
              <p><strong>Nội dung đã gửi:</strong> {selectedComplaint.content}</p>
              <p>
                <strong>Trạng thái:</strong>{' '}
                {(selectedComplaint.status || 'PENDING').toUpperCase() === 'RESOLVED'
                  ? 'Manager đã phản hồi'
                  : 'Đang chờ Manager xử lý'}
              </p>
              {selectedComplaint.managerResponse && (
                <p className="sd-complaint-response">
                  <strong>Phản hồi của Manager:</strong> {selectedComplaint.managerResponse}
                </p>
              )}
            </div>
          ) : (selectedSalary.status || '').toUpperCase() === 'FINALIZED' ? (
            <form className="sd-complaint-form" onSubmit={submitComplaint}>
              <div className="sd-field">
                <label>Nội dung khiếu nại</label>
                <textarea
                  maxLength="1000"
                  onChange={(event) => setComplaintContent(event.target.value)}
                  placeholder="Mô tả khoản lương, giờ làm, thưởng hoặc phạt cần Manager kiểm tra..."
                  required
                  rows="4"
                  value={complaintContent}
                />
              </div>
              {complaintError && <p className="sd-status sd-status-error">{complaintError}</p>}
              <button className="sd-btn-primary" disabled={complaintSaving || !complaintContent.trim()} type="submit">
                {complaintSaving ? 'Đang gửi...' : 'Gửi khiếu nại cho Manager'}
              </button>
            </form>
          ) : (
            <p className="sd-salary-empty">
              {isTemporarySalary
                ? 'Đây là bảng lương tạm. Bạn có thể gửi khiếu nại sau khi Manager chốt lương.'
                : 'Kỳ lương này đã được Admin chốt hoặc đã thanh toán nên không còn nhận khiếu nại mới.'}
            </p>
          )}
        </div>
      )}

      <div className="sd-card sd-work-detail-card">
        <div className="sd-card-header">
          <div>
            <p className="sd-eyebrow">Minh bạch thu nhập</p>
            <h2>Lịch sử thưởng/phạt</h2>
          </div>
        </div>

        {historyError && <p className="sd-status sd-status-error">{historyError}</p>}
        {historyLoading ? (
          <p className="sd-salary-empty">Đang tải lịch sử thưởng/phạt...</p>
        ) : adjustmentHistory.length === 0 ? (
          <p className="sd-salary-empty">Chưa có lần thưởng/phạt thủ công nào trong kỳ lương này.</p>
        ) : (
          <div className="sd-salary-table-wrap">
            <table className="sd-salary-table sd-adjustment-history-table">
              <thead>
                <tr><th>Thời gian</th><th>Thưởng</th><th>Phạt</th><th>Lý do</th><th>Người tạo</th><th>Trạng thái</th></tr>
              </thead>
              <tbody>
                {adjustmentHistory.map((item) => {
                  const status = getAdjustmentStatus(item.status);
                  return (
                  <tr key={item.id}>
                    <td>{new Date(item.createdAt).toLocaleString('vi-VN')}</td>
                    <td>{formatMoney(item.bonusAmount)}</td>
                    <td>{formatMoney(item.penaltyAmount)}</td>
                    <td>{item.reason}</td>
                    <td>{item.createdByName || 'Quản lý'}</td>
                    <td><span className={`sd-status-pill ${status.className}`}>{status.label}</span></td>
                  </tr>
                  );
                })}
              </tbody>
            </table>
          </div>
        )}
      </div>
    </div>
  );
}
