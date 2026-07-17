import { useEffect, useMemo, useState } from 'react';
import { getSalaryByUser } from '../../api/SalaryApi';

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

function getMonthKey(item) {
  return `${item.year}-${String(item.month).padStart(2, '0')}`;
}

function formatStatus(status) {
  const map = {
    PENDING: 'Chưa thanh toán',
    PAID: 'Đã thanh toán',
    CANCELLED: 'Đã huỷ',
  };
  return map[(status || 'PENDING').toUpperCase()] || status;
}

function getStatusClass(status) {
  const normalized = (status || 'PENDING').toUpperCase();
  if (normalized === 'PAID') return 'paid';
  if (normalized === 'CANCELLED') return 'cancelled';
  return 'pending';
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
  const [selectedMonthKey, setSelectedMonthKey] = useState('');
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState('');

  useEffect(() => {
    async function loadSalary() {
      if (!user?.id) return;

      setLoading(true);
      setError('');
      try {
        const data = await getSalaryByUser(user.id);
        const nextSalaries = Array.isArray(data) ? data : [];
        setSalaries(nextSalaries);
        setSelectedMonthKey(nextSalaries[0] ? getMonthKey(nextSalaries[0]) : '');
      } catch (err) {
        setError(err.response?.data?.message || 'Không tải được dữ liệu lương.');
      } finally {
        setLoading(false);
      }
    }

    loadSalary();
  }, [user?.id]);

  const monthOptions = useMemo(() => {
    return salaries.map((item) => ({
      key: getMonthKey(item),
      label: `Tháng ${item.month}/${item.year}`,
    }));
  }, [salaries]);

  const selectedSalary = useMemo(() => {
    return salaries.find((item) => getMonthKey(item) === selectedMonthKey) || null;
  }, [salaries, selectedMonthKey]);

  const summary = useMemo(() => ({
    hours: Number(selectedSalary?.totalHours || 0),
    salary: Number(selectedSalary?.totalSalary || 0),
    bonus: Number(selectedSalary?.totalBonus || 0),
    penalty: Number(selectedSalary?.totalPenalty || 0),
  }), [selectedSalary]);

  return (
    <div className="sd-profile-layout">
      <div className="sd-salary-summary">
        <SalaryMetric label="Tổng giờ làm" value={`${formatNumber(summary.hours)} giờ`} />
        <SalaryMetric label="Tổng lương" value={formatMoney(summary.salary)} />
        <SalaryMetric label="Thưởng" value={formatMoney(summary.bonus)} />
        <SalaryMetric label="Phạt" value={formatMoney(summary.penalty)} />
      </div>

      <div className="sd-card">
        <div className="sd-card-header sd-salary-header">
          <div>
            <p className="sd-eyebrow">Lương nhân viên</p>
            <h2>{selectedSalary ? `Tháng ${selectedSalary.month}/${selectedSalary.year}` : 'Bảng lương của tôi'}</h2>
          </div>
          {monthOptions.length > 0 && (
            <div className="sd-field sd-salary-filter">
              <label>Chọn kỳ lương</label>
              <select value={selectedMonthKey} onChange={(event) => setSelectedMonthKey(event.target.value)}>
                {monthOptions.map((month) => (
                  <option key={month.key} value={month.key}>{month.label}</option>
                ))}
              </select>
            </div>
          )}
        </div>

        {error && <p className="sd-status sd-status-error">{error}</p>}

        {loading ? (
          <p className="sd-salary-empty">Đang tải dữ liệu lương...</p>
        ) : !selectedSalary ? (
          <p className="sd-salary-empty">Chưa có dữ liệu lương. Bảng lương sẽ được tạo sau khi ca làm được check-out.</p>
        ) : (
          <div className="sd-salary-table-wrap">
            <table className="sd-salary-table">
              <thead>
                <tr>
                  <th>Tháng</th>
                  <th>Giờ làm</th>
                  <th>Lương/giờ</th>
                  <th>Thưởng</th>
                  <th>Phạt</th>
                  <th>Thực nhận</th>
                  <th>Trạng thái</th>
                </tr>
              </thead>
              <tbody>
                <tr>
                  <td><strong>{selectedSalary.month}/{selectedSalary.year}</strong></td>
                  <td>{formatNumber(selectedSalary.totalHours)} giờ</td>
                  <td>{formatMoney(selectedSalary.hourlyWageAtTime)}</td>
                  <td>{formatMoney(selectedSalary.totalBonus)}</td>
                  <td>{formatMoney(selectedSalary.totalPenalty)}</td>
                  <td className="sd-salary-total">{formatMoney(selectedSalary.totalSalary)}</td>
                  <td>
                    <span className={`sd-salary-status ${getStatusClass(selectedSalary.status)}`}>
                      {formatStatus(selectedSalary.status)}
                    </span>
                  </td>
                </tr>
              </tbody>
            </table>
          </div>
        )}
      </div>

    </div>
  );
}
