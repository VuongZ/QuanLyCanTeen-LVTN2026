import { useEffect, useMemo, useState } from 'react';
import { getWorkHoursRanking } from '../../api/DashboardApi';
import '../css/adminworkhoursranking.css';

const currentDate = new Date();
const currentYear = currentDate.getFullYear();
const currentMonth = currentDate.getMonth() + 1;
const monthNames = Array.from(
  { length: 12 },
  (_, index) => `Tháng ${index + 1}`,
);

const formatHours = (value) => new Intl.NumberFormat('vi-VN', {
  maximumFractionDigits: 2,
}).format(Number(value || 0));

export function AdminWorkHoursRanking({ branches }) {
  const [mode, setMode] = useState('MONTH');
  const [selectedBranch, setSelectedBranch] = useState('ALL');
  const [selectedMonth, setSelectedMonth] = useState(currentMonth);
  const [selectedYear, setSelectedYear] = useState(currentYear);
  const [ranking, setRanking] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');

  const yearOptions = useMemo(
    () => Array.from({ length: 7 }, (_, index) => currentYear - index),
    [],
  );

  useEffect(() => {
    let ignore = false;

    async function loadRanking() {
      setLoading(true);
      setError('');
      try {
        const data = await getWorkHoursRanking({
          branchId: selectedBranch,
          month: mode === 'MONTH' ? selectedMonth : null,
          year: selectedYear,
        });
        if (!ignore) setRanking(Array.isArray(data) ? data : []);
      } catch (err) {
        if (!ignore) {
          setRanking([]);
          setError(
            err.response?.data?.message
              || 'Không tải được thống kê giờ làm.',
          );
        }
      } finally {
        if (!ignore) setLoading(false);
      }
    }

    loadRanking();
    return () => { ignore = true; };
  }, [mode, selectedBranch, selectedMonth, selectedYear]);

  const topEmployee = ranking[0] || null;
  const periodLabel = mode === 'MONTH'
    ? `tháng ${selectedMonth}/${selectedYear}`
    : `năm ${selectedYear}`;

  return (
    <section className="sd-card sd-hours-ranking-card">
      <div className="sd-hours-ranking-header">
        <div>
          <p className="sd-eyebrow">Hiệu suất làm việc</p>
          <h2>Nhân viên làm nhiều giờ nhất</h2>
          <p className="sd-hours-ranking-subtitle">
            Xếp hạng theo tổng giờ của các ca đã có đủ giờ vào và giờ ra.
          </p>
        </div>

        <div className="sd-hours-ranking-filters">
          <div className="sd-hours-filter-field sd-hours-filter-branch">
            <label htmlFor="hours-ranking-branch">Phạm vi</label>
            <select
              id="hours-ranking-branch"
              onChange={(event) => setSelectedBranch(event.target.value)}
              value={selectedBranch}
            >
              <option value="ALL">Toàn căn tin</option>
              {branches.map((branch) => (
                <option key={branch.id} value={branch.id}>{branch.name}</option>
              ))}
            </select>
          </div>

          <div className="sd-hours-period-mode" aria-label="Kiểu thời gian">
            <button
              className={mode === 'MONTH' ? 'active' : ''}
              onClick={() => setMode('MONTH')}
              type="button"
            >
              Theo tháng
            </button>
            <button
              className={mode === 'YEAR' ? 'active' : ''}
              onClick={() => setMode('YEAR')}
              type="button"
            >
              Theo năm
            </button>
          </div>

          {mode === 'MONTH' && (
            <div className="sd-hours-filter-field">
              <label htmlFor="hours-ranking-month">Tháng</label>
              <select
                id="hours-ranking-month"
                onChange={(event) => setSelectedMonth(Number(event.target.value))}
                value={selectedMonth}
              >
                {monthNames.map((name, index) => (
                  <option key={name} value={index + 1}>{name}</option>
                ))}
              </select>
            </div>
          )}

          <div className="sd-hours-filter-field">
            <label htmlFor="hours-ranking-year">Năm</label>
            <select
              id="hours-ranking-year"
              onChange={(event) => setSelectedYear(Number(event.target.value))}
              value={selectedYear}
            >
              {yearOptions.map((year) => (
                <option key={year} value={year}>{year}</option>
              ))}
            </select>
          </div>
        </div>
      </div>

      {error && <p className="sd-status sd-status-error">{error}</p>}

      {loading ? (
        <div className="sd-hours-ranking-empty">Đang tải thống kê giờ làm...</div>
      ) : ranking.length === 0 ? (
        <div className="sd-hours-ranking-empty">
          Chưa có ca làm hoàn thành trong {periodLabel}.
        </div>
      ) : (
        <div className="sd-hours-ranking-content">
          <div className="sd-hours-top-employee">
            <span className="sd-hours-trophy">★</span>
            <div className="sd-hours-top-copy">
              <span>Dẫn đầu {periodLabel}</span>
              <strong>{topEmployee.employeeName}</strong>
              <small>{topEmployee.branchName || 'Chưa gán cơ sở'}</small>
            </div>
            <div className="sd-hours-top-value">
              <strong>{formatHours(topEmployee.totalHours)}</strong>
              <span>giờ · {topEmployee.shiftCount} ca</span>
            </div>
          </div>

          <div className="sd-table-wrap sd-hours-ranking-table-wrap">
            <table className="sd-table sd-hours-ranking-table">
              <thead>
                <tr>
                  <th>Hạng</th>
                  <th>Nhân viên</th>
                  <th>Cơ sở</th>
                  <th>Số ca</th>
                  <th>Tổng giờ</th>
                </tr>
              </thead>
              <tbody>
                {ranking.map((item) => (
                  <tr key={item.userId}>
                    <td>
                      <span className={`sd-hours-rank sd-hours-rank-${item.rank}`}>
                        {item.rank}
                      </span>
                    </td>
                    <td><strong>{item.employeeName}</strong></td>
                    <td>{item.branchName || 'Chưa gán cơ sở'}</td>
                    <td>{item.shiftCount}</td>
                    <td className="sd-hours-total">{formatHours(item.totalHours)} giờ</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </div>
      )}
    </section>
  );
}
