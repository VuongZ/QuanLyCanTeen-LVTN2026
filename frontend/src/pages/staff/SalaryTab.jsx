import { useEffect, useMemo, useState } from 'react';
import {
  getSalaryByUser,
  getSalaryWorkDetails,
} from '../../api/SalaryApi';

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
  if (!value) return '—';

  const date = new Date(value);

  if (Number.isNaN(date.getTime())) return '—';

  return date.toLocaleTimeString('vi-VN', {
    hour: '2-digit',
    minute: '2-digit',
    hour12: false,
  });
}

function getMonthKey(item) {
  return `${item.year}-${String(item.month).padStart(2, '0')}`;
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

  const [salaryLoading, setSalaryLoading] = useState(false);
  const [detailLoading, setDetailLoading] = useState(false);

  const [salaryError, setSalaryError] = useState('');
  const [detailError, setDetailError] = useState('');

  useEffect(() => {
    async function loadSalary() {
      if (!user?.id) return;

      setSalaryLoading(true);
      setSalaryError('');

      try {
        const data = await getSalaryByUser(user.id);
        const nextSalaries = Array.isArray(data) ? data : [];

        setSalaries(nextSalaries);

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
  }, [
    user?.id,
    selectedSalary?.month,
    selectedSalary?.year,
  ]);

  const summary = useMemo(() => ({
    hours: Number(selectedSalary?.totalHours || 0),
    salary: Number(selectedSalary?.totalSalary || 0),
    bonus: Number(selectedSalary?.totalBonus || 0),
    penalty: Number(selectedSalary?.totalPenalty || 0),
  }), [selectedSalary]);

  return (
    <div className="sd-profile-layout sd-salary-layout">
      <div className="sd-salary-summary">
        <SalaryMetric
          label="Tổng giờ làm"
          value={`${formatNumber(summary.hours)} giờ`}
        />

        <SalaryMetric
          label="Tổng lương"
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
      </div>

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
            Chưa có dữ liệu lương. Bảng lương sẽ được tạo
            sau khi Nhân viên hoàn tất điểm danh ra ca.
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
    </div>
  );
}