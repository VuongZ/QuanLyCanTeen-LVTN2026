import { useEffect, useMemo, useState } from 'react';
import { getAllSalaries, markSalaryPaid } from '../../api/SalaryApi';

function formatMoney(value) {
  return new Intl.NumberFormat('vi-VN', {
    style: 'currency',
    currency: 'VND',
    maximumFractionDigits: 0,
  }).format(Number(value || 0));
}

function formatNumber(value) {
  return new Intl.NumberFormat('vi-VN', { maximumFractionDigits: 2 }).format(Number(value || 0));
}

function formatDate(value) {
  if (!value) return '---';
  return new Intl.DateTimeFormat('vi-VN', {
    hour: '2-digit',
    minute: '2-digit',
    day: '2-digit',
    month: '2-digit',
    year: 'numeric',
  }).format(new Date(value));
}

function formatStatus(status) {
  const normalized = (status || 'PENDING').toUpperCase();
  if (normalized === 'PAID') return 'Đã thanh toán';
  if (normalized === 'CANCELLED') return 'Đã hủy';
  return 'Chưa thanh toán';
}

function InfoRow({ label, value }) {
  return (
    <div className="sd-info-row">
      <dt>{label}</dt>
      <dd>{value || 'Chưa có'}</dd>
    </div>
  );
}

export function AdminSalaryTab() {
  const [salaries, setSalaries] = useState([]);
  const [query, setQuery] = useState('');
  const [statusFilter, setStatusFilter] = useState('ALL');
  const [selectedSalary, setSelectedSalary] = useState(null);
  const [loading, setLoading] = useState(false);
  const [saving, setSaving] = useState(false);
  const [message, setMessage] = useState(null);

  async function loadSalaries() {
    setLoading(true);
    setMessage(null);
    try {
      const data = await getAllSalaries();
      setSalaries(Array.isArray(data) ? data : []);
    } catch (err) {
      setMessage({ type: 'error', text: err.response?.data?.message || 'Không tải được danh sách lương.' });
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => {
    loadSalaries();
  }, []);

  const filteredSalaries = useMemo(() => {
    const normalizedQuery = query.trim().toLowerCase();
    return salaries.filter((item) => {
      const status = (item.status || 'PENDING').toUpperCase();
      const matchesStatus = statusFilter === 'ALL' || status === statusFilter;
      const matchesQuery = !normalizedQuery || [
        item.fullName,
        item.username,
        item.branchName,
        item.bankName,
        item.bankAccountNumber,
        item.bankAccountName,
        `${item.month}/${item.year}`,
      ].some((value) => String(value || '').toLowerCase().includes(normalizedQuery));

      return matchesStatus && matchesQuery;
    });
  }, [salaries, query, statusFilter]);

  const summary = useMemo(() => {
    return filteredSalaries.reduce(
      (total, item) => {
        const isPaid = (item.status || '').toUpperCase() === 'PAID';
        return {
          count: total.count + 1,
          pending: total.pending + (isPaid ? 0 : Number(item.totalSalary || 0)),
          paid: total.paid + (isPaid ? Number(item.totalSalary || 0) : 0),
        };
      },
      { count: 0, pending: 0, paid: 0 }
    );
  }, [filteredSalaries]);

  async function handleConfirmPaid() {
    if (!selectedSalary) return;

    setSaving(true);
    setMessage(null);
    try {
      const updated = await markSalaryPaid(selectedSalary.id);
      setSalaries((items) => items.map((item) => (item.id === updated.id ? updated : item)));
      setSelectedSalary(null);
      setMessage({ type: 'success', text: 'Đã cập nhật trạng thái thành đã thanh toán.' });
    } catch (err) {
      setMessage({ type: 'error', text: err.response?.data?.message || 'Không thể cập nhật thanh toán.' });
    } finally {
      setSaving(false);
    }
  }

  return (
    <div className="sd-salary-admin-page">
      <div className="sd-stat-grid sd-salary-admin-stats">
        <div className="sd-stat-card"><span className="sd-stat-icon">∑</span><h3>{summary.count}</h3><p>Bảng lương</p></div>
        <div className="sd-stat-card"><span className="sd-stat-icon">₫</span><h3>{formatMoney(summary.pending)}</h3><p>Chưa thanh toán</p></div>
        <div className="sd-stat-card"><span className="sd-stat-icon">✓</span><h3>{formatMoney(summary.paid)}</h3><p>Đã thanh toán</p></div>
      </div>

      <div className="sd-users-toolbar">
        <div className="sd-users-toolbar-left">
          <div className="sd-search-wrap">
            <span className="sd-search-icon">⌕</span>
            <input
              className="sd-input-search"
              onChange={(event) => setQuery(event.target.value)}
              placeholder="Tìm nhân viên, ngân hàng, tháng..."
              value={query}
            />
            {query && <button className="sd-search-clear" onClick={() => setQuery('')}>✕</button>}
          </div>
          <div className="sd-filter-chips">
            {[
              ['ALL', 'Tất cả'],
              ['PENDING', 'Chưa thanh toán'],
              ['PAID', 'Đã thanh toán'],
            ].map(([value, label]) => (
              <button
                className={`sd-filter-chip ${statusFilter === value ? 'active' : ''}`}
                key={value}
                onClick={() => setStatusFilter(value)}
                type="button"
              >
                {label}
              </button>
            ))}
          </div>
        </div>
        <button className="sd-btn-ghost" onClick={loadSalaries} type="button">Làm mới</button>
      </div>

      {message && <p className={`sd-status sd-status-${message.type}`}>{message.text}</p>}

      <div className="sd-table-wrap">
        <table className="sd-table">
          <thead>
            <tr>
              <th>Nhân viên</th>
              <th>Tháng</th>
              <th>Giờ làm</th>
              <th>Thực nhận</th>
              <th>Ngân hàng</th>
              <th>Trạng thái</th>
              <th>Thao tác</th>
            </tr>
          </thead>
          <tbody>
            {loading ? (
              <tr><td colSpan={7} className="sd-td-empty">Đang tải danh sách lương...</td></tr>
            ) : filteredSalaries.length === 0 ? (
              <tr><td colSpan={7} className="sd-td-empty">Chưa có bảng lương phù hợp.</td></tr>
            ) : filteredSalaries.map((item) => {
              const isPaid = (item.status || '').toUpperCase() === 'PAID';
              return (
                <tr key={item.id}>
                  <td>
                    <strong>{item.fullName || item.username}</strong>
                    <span className="sd-subline">{item.branchName || 'Chưa gán cơ sở'}</span>
                  </td>
                  <td>{item.month}/{item.year}</td>
                  <td>{formatNumber(item.totalHours)} giờ</td>
                  <td className="sd-salary-admin-total">{formatMoney(item.totalSalary)}</td>
                  <td>
                    <strong>{item.bankName || 'Chưa có'}</strong>
                    <span className="sd-subline">{item.bankAccountNumber || 'Chưa có STK'}</span>
                  </td>
                  <td><span className={`sd-status-pill ${isPaid ? 'paid' : 'pending'}`}>{formatStatus(item.status)}</span></td>
                  <td>
                    <button
                      className={isPaid ? 'sd-btn-ghost' : 'sd-btn-primary'}
                      disabled={isPaid}
                      onClick={() => setSelectedSalary(item)}
                      type="button"
                    >
                      {isPaid ? 'Đã trả' : 'Trả lương'}
                    </button>
                  </td>
                </tr>
              );
            })}
          </tbody>
        </table>
      </div>

      {selectedSalary && (
        <div className="sd-overlay" onClick={() => setSelectedSalary(null)}>
          <div className="sd-modal sd-salary-pay-modal" onClick={(event) => event.stopPropagation()}>
            <div className="sd-modal-header">
              <h2>Xác nhận trả lương</h2>
              <button onClick={() => setSelectedSalary(null)} type="button">✕</button>
            </div>
            <div className="sd-modal-body">
              <dl className="sd-dl">
                <InfoRow label="Nhân viên" value={selectedSalary.fullName || selectedSalary.username} />
                <InfoRow label="Kỳ lương" value={`Tháng ${selectedSalary.month}/${selectedSalary.year}`} />
                <InfoRow label="Số tiền" value={formatMoney(selectedSalary.totalSalary)} />
                <InfoRow label="Ngân hàng" value={selectedSalary.bankName} />
                <InfoRow label="Số tài khoản" value={selectedSalary.bankAccountNumber} />
                <InfoRow label="Tên tài khoản" value={selectedSalary.bankAccountName} />
                <InfoRow label="Thanh toán lúc" value={formatDate(selectedSalary.paidAt)} />
              </dl>
            </div>
            <div className="sd-modal-footer">
              <button className="sd-btn-ghost" onClick={() => setSelectedSalary(null)} type="button">Hủy</button>
              <button className="sd-btn-primary" disabled={saving} onClick={handleConfirmPaid} type="button">
                {saving ? 'Đang cập nhật...' : 'Đã trả lương thành công'}
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}
