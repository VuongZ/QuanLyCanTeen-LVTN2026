import { useEffect, useMemo, useState } from 'react';
import { getAllSalaries, getBranchSalaries, markSalaryPaid } from '../../api/SalaryApi';

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

export function AdminSalaryTab({ isAdmin = true }) {
  const [items, setItems] = useState([]);
  const [query, setQuery] = useState('');
  const [statusFilter, setStatusFilter] = useState('ALL');
  const [selectedSalary, setSelectedSalary] = useState(null);
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [message, setMessage] = useState(null);

  async function loadSalaries() {
    setLoading(true);
    setMessage(null);
    try {
      const data = isAdmin ? await getAllSalaries() : await getBranchSalaries();
      setItems(Array.isArray(data) ? data : []);
    } catch (err) {
      setMessage({ type: 'error', text: err.response?.data?.message || 'Không tải được dữ liệu lương.' });
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => {
    let ignore = false;

    setLoading(true);
    setMessage(null);
    (isAdmin ? getAllSalaries() : getBranchSalaries())
      .then((data) => {
        if (!ignore) setItems(Array.isArray(data) ? data : []);
      })
      .catch((err) => {
        if (!ignore) setMessage({ type: 'error', text: err.response?.data?.message || 'Không tải được dữ liệu lương.' });
      })
      .finally(() => {
        if (!ignore) setLoading(false);
      });

    return () => {
      ignore = true;
    };
  }, [isAdmin]);

  const filteredItems = useMemo(() => {
    const normalizedQuery = query.trim().toLowerCase();
    return items.filter((item) => {
      if (isAdmin) {
        return !normalizedQuery || [
          item.branchName,
          item.managerName,
          item.managerEmail,
          item.managerPhoneNumber,
          item.managerBankName,
          item.managerBankAccountNumber,
          item.managerBankAccountName,
          String(item.branchId || ''),
        ].some((value) => String(value || '').toLowerCase().includes(normalizedQuery));
      }

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
  }, [items, query, statusFilter, isAdmin]);

  const summary = useMemo(() => {
    return filteredItems.reduce(
      (total, item) => {
        if (isAdmin) {
          return {
            count: total.count + Number(item.salaryCount || 0),
            pending: total.pending + Number(item.pendingTotal || 0),
            paid: total.paid + Number(item.paidTotal || 0),
          };
        }

        const isPaid = (item.status || '').toUpperCase() === 'PAID';
        return {
          count: total.count + 1,
          pending: total.pending + (isPaid ? 0 : Number(item.totalSalary || 0)),
          paid: total.paid + (isPaid ? Number(item.totalSalary || 0) : 0),
        };
      },
      { count: 0, pending: 0, paid: 0 }
    );
  }, [filteredItems, isAdmin]);

  async function handleConfirmPaid() {
    if (!selectedSalary) return;

    setSaving(true);
    setMessage(null);
    try {
      const updated = await markSalaryPaid(selectedSalary.id);
      setItems((currentItems) => currentItems.map((item) => (item.id === updated.id ? updated : item)));
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
        <div className="sd-stat-card"><span className="sd-stat-icon">∑</span><h3>{summary.count}</h3><p>{isAdmin ? 'Bảng lương toàn hệ thống' : 'Bảng lương cơ sở'}</p></div>
        <div className="sd-stat-card"><span className="sd-stat-icon">₫</span><h3>{formatMoney(summary.pending)}</h3><p>{isAdmin ? 'Cần chuyển cho manager' : 'Chưa thanh toán'}</p></div>
        <div className="sd-stat-card"><span className="sd-stat-icon">✓</span><h3>{formatMoney(summary.paid)}</h3><p>Đã thanh toán</p></div>
      </div>

      <div className="sd-users-toolbar">
        <div className="sd-users-toolbar-left">
          <div className="sd-search-wrap">
            <span className="sd-search-icon">⌕</span>
            <input
              className="sd-input-search"
              onChange={(event) => setQuery(event.target.value)}
              placeholder={isAdmin ? 'Tìm cơ sở...' : 'Tìm nhân viên, ngân hàng, tháng...'}
              value={query}
            />
            {query && <button className="sd-search-clear" onClick={() => setQuery('')} type="button">✕</button>}
          </div>
          {!isAdmin && (
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
          )}
        </div>
        <button className="sd-btn-ghost" onClick={loadSalaries} type="button">Làm mới</button>
      </div>

      {message && <p className={`sd-status sd-status-${message.type}`}>{message.text}</p>}

      {isAdmin ? (
        <div className="sd-table-wrap">
          <table className="sd-table">
            <thead>
              <tr>
                <th>Cơ sở</th>
                <th>Manager nhận tiền</th>
                <th>Nhân viên có lương</th>
                <th>Bảng lương</th>
                <th>Cần chuyển manager</th>
                <th>Đã trả</th>
                <th>Tổng lương</th>
              </tr>
            </thead>
            <tbody>
              {loading ? (
                <tr><td colSpan={7} className="sd-td-empty">Đang tải tổng lương theo cơ sở...</td></tr>
              ) : filteredItems.length === 0 ? (
                <tr><td colSpan={7} className="sd-td-empty">Chưa có dữ liệu lương theo cơ sở.</td></tr>
              ) : filteredItems.map((item) => (
                <tr key={item.branchId || 'unassigned'}>
                  <td>
                    <strong>{item.branchName || 'Chưa gán cơ sở'}</strong>
                    <span className="sd-subline">Manager cơ sở xác nhận trả lương nhân viên</span>
                  </td>
                  <td>
                    <strong>{item.managerName || item.managerEmail || item.managerPhoneNumber || 'Chưa có manager'}</strong>
                    <span className="sd-subline">{item.managerBankName || 'Chưa có ngân hàng'}</span>
                    <span className="sd-subline">{item.managerBankAccountNumber || 'Chưa có STK'}</span>
                    <span className="sd-subline">{item.managerBankAccountName || 'Chưa có tên tài khoản'}</span>
                  </td>
                  <td>{formatNumber(item.employeeCount)}</td>
                  <td>
                    <strong>{formatNumber(item.salaryCount)}</strong>
                    <span className="sd-subline">{formatNumber(item.pendingCount)} chưa trả / {formatNumber(item.paidCount)} đã trả</span>
                  </td>
                  <td className="sd-salary-admin-total">{formatMoney(item.pendingTotal)}</td>
                  <td>{formatMoney(item.paidTotal)}</td>
                  <td>{formatMoney(item.totalSalary)}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      ) : (
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
              ) : filteredItems.length === 0 ? (
                <tr><td colSpan={7} className="sd-td-empty">Chưa có bảng lương phù hợp.</td></tr>
              ) : filteredItems.map((item) => {
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
      )}

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
