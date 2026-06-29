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

function formatStatus(status) {
  const map = {
    PENDING: 'Chưa Thanh Toán',
    PAID: 'Đã Thanh Toán',
    CANCELLED: 'Đã Huỷ',
  };
  return map[status] || status;
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
        setSelectedMonthKey((current) => current || (nextSalaries[0] ? `${nextSalaries[0].year}-${String(nextSalaries[0].month).padStart(2, '0')}` : ''));
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
      key: `${item.year}-${String(item.month).padStart(2, '0')}`,
      label: `Tháng ${item.month}/${item.year}`,
    }));
  }, [salaries]);

  const filteredSalaries = useMemo(() => {
    if (!selectedMonthKey) return [];
    return salaries.filter((item) => `${item.year}-${String(item.month).padStart(2, '0')}` === selectedMonthKey);
  }, [salaries, selectedMonthKey]);

  const summary = useMemo(() => {
    return filteredSalaries.reduce(
      (total, item) => ({
        hours: total.hours + Number(item.totalHours || 0),
        salary: total.salary + Number(item.totalSalary || 0),
        bonus: total.bonus + Number(item.totalBonus || 0),
        penalty: total.penalty + Number(item.totalPenalty || 0),
      }),
      { hours: 0, salary: 0, bonus: 0, penalty: 0 }
    );
  }, [filteredSalaries]);

  const selectedSalary = filteredSalaries[0] || salaries[0];

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
              <label>Tháng</label>
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
        ) : filteredSalaries.length === 0 ? (
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
                {filteredSalaries.map((item) => (
                  <tr key={item.id}>
                    <td><strong>{item.month}/{item.year}</strong></td>
                    <td>{formatNumber(item.totalHours)} giờ</td>
                    <td>{formatMoney(item.hourlyWageAtTime)}</td>
                    <td>{formatMoney(item.totalBonus)}</td>
                    <td>{formatMoney(item.totalPenalty)}</td>
                    <td className="sd-salary-total">{formatMoney(item.totalSalary)}</td>
                    <td><span className="sd-salary-status">{formatStatus(item.status || 'PENDING')}</span></td>
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
