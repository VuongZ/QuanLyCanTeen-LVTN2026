import { useEffect, useMemo, useState } from 'react';
import { getAllSalaries, getBranchSalaries, markBranchSalaryTransferred, markSalaryPaid } from '../../api/SalaryApi';

const money = (value) => new Intl.NumberFormat('vi-VN', { style: 'currency', currency: 'VND', maximumFractionDigits: 0 }).format(Number(value || 0));
const number = (value) => new Intl.NumberFormat('vi-VN', { maximumFractionDigits: 2 }).format(Number(value || 0));
const date = (value) => value ? new Intl.DateTimeFormat('vi-VN', { hour: '2-digit', minute: '2-digit', day: '2-digit', month: '2-digit', year: 'numeric' }).format(new Date(value)) : '—';
const periodKey = (item) => `${item.year}-${String(item.month).padStart(2, '0')}`;

function statusLabel(status) {
  if ((status || '').toUpperCase() === 'PAID') return 'Đã thanh toán';
  if ((status || '').toUpperCase() === 'CANCELLED') return 'Đã huỷ';
  return 'Chưa thanh toán';
}

function InfoRow({ label, value }) {
  return <div className="sd-info-row"><dt>{label}</dt><dd>{value || 'Chưa có'}</dd></div>;
}

export function AdminSalaryTab({ isAdmin = true }) {
  const [items, setItems] = useState([]);
  const [query, setQuery] = useState('');
  const [viewMode, setViewMode] = useState('CURRENT');
  const [selectedPeriod, setSelectedPeriod] = useState('ALL');
  const [selected, setSelected] = useState(null);
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [message, setMessage] = useState(null);

  async function fetchItems() {
    return isAdmin ? getAllSalaries() : getBranchSalaries();
  }

  async function reload() {
    setLoading(true);
    setMessage(null);
    try {
      const data = await fetchItems();
      const nextItems = Array.isArray(data) ? data : [];
      setItems(nextItems);
      if (selectedPeriod !== 'ALL' && !nextItems.some((item) => periodKey(item) === selectedPeriod)) {
        setSelectedPeriod(nextItems[0] ? periodKey(nextItems[0]) : 'ALL');
      }
    } catch (err) {
      setMessage({ type: 'error', text: err.response?.data?.message || 'Không tải được dữ liệu lương.' });
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => {
    let ignore = false;
    async function load() {
      try {
        const data = isAdmin ? await getAllSalaries() : await getBranchSalaries();
        if (!ignore) setItems(Array.isArray(data) ? data : []);
      } catch (err) {
        if (!ignore) setMessage({ type: 'error', text: err.response?.data?.message || 'Không tải được dữ liệu lương.' });
      } finally {
        if (!ignore) setLoading(false);
      }
    }
    load();
    return () => { ignore = true; };
  }, [isAdmin]);

  const periodOptions = useMemo(() => {
    const uniquePeriods = new Map();
    items.forEach((item) => {
      const key = periodKey(item);
      if (!uniquePeriods.has(key)) uniquePeriods.set(key, { key, month: item.month, year: item.year });
    });
    return Array.from(uniquePeriods.values()).sort((a, b) => b.key.localeCompare(a.key));
  }, [items]);

  const filtered = useMemo(() => {
    const keyword = query.trim().toLowerCase();
    return items.filter((item) => {
      if (selectedPeriod !== 'ALL' && periodKey(item) !== selectedPeriod) return false;
      const history = isAdmin ? Boolean(item.isTransferred) : (item.status || '').toUpperCase() === 'PAID';
      if ((viewMode === 'HISTORY') !== history) return false;
      const values = isAdmin
        ? [item.branchName, item.managerName, item.managerEmail, item.managerBankName, item.managerBankAccountNumber, `${item.month}/${item.year}`]
        : [item.fullName, item.username, item.bankName, item.bankAccountNumber, `${item.month}/${item.year}`];
      return !keyword || values.some((value) => String(value || '').toLowerCase().includes(keyword));
    });
  }, [items, query, viewMode, isAdmin, selectedPeriod]);

  const summary = useMemo(() => items
    .filter((item) => selectedPeriod === 'ALL' || periodKey(item) === selectedPeriod)
    .reduce((total, item) => {
    if (isAdmin) {
      total.count += Number(item.salaryCount || 0);
      if (item.isTransferred) total.paid += Number(item.transferredAmount || item.totalSalary || 0);
      else total.pending += Number(item.totalSalary || 0);
    } else {
      const paid = (item.status || '').toUpperCase() === 'PAID';
      total.count += 1;
      total[paid ? 'paid' : 'pending'] += Number(item.totalSalary || 0);
    }
    return total;
  }, { count: 0, pending: 0, paid: 0 }), [items, isAdmin, selectedPeriod]);

  async function confirmPayment() {
    if (!selected) return;
    setSaving(true);
    setMessage(null);
    try {
      if (isAdmin) {
        const updated = await markBranchSalaryTransferred(selected.branchId, selected.month, selected.year);
        setItems((current) => current.map((item) => item.branchId === updated.branchId && item.month === updated.month && item.year === updated.year ? updated : item));
        setMessage({ type: 'success', text: 'Đã xác nhận chuyển lương cho quản lý.' });
      } else {
        const updated = await markSalaryPaid(selected.id);
        setItems((current) => current.map((item) => item.id === updated.id ? updated : item));
        setMessage({ type: 'success', text: 'Đã xác nhận trả lương cho nhân viên.' });
      }
      setSelected(null);
    } catch (err) {
      setMessage({ type: 'error', text: err.response?.data?.message || 'Không thể cập nhật thanh toán.' });
    } finally {
      setSaving(false);
    }
  }

  return (
    <div className={`sd-salary-admin-page ${isAdmin ? 'sd-salary-admin-page--admin' : ''}`}>
      <div className="sd-stat-grid sd-salary-admin-stats">
        <div className="sd-stat-card"><span className="sd-stat-icon">∑</span><h3>{summary.count}</h3><p>{isAdmin ? 'Bảng lương toàn hệ thống' : 'Bảng lương cơ sở'}</p></div>
        <div className="sd-stat-card"><span className="sd-stat-icon">₫</span><h3>{money(summary.pending)}</h3><p>{isAdmin ? 'Chờ chuyển cho quản lý' : 'Chưa thanh toán'}</p></div>
        <div className="sd-stat-card"><span className="sd-stat-icon">✓</span><h3>{money(summary.paid)}</h3><p>{isAdmin ? 'Đã chuyển cho quản lý' : 'Đã thanh toán'}</p></div>
      </div>

      <div className="sd-users-toolbar">
        <div className="sd-users-toolbar-left">
          <div className="sd-search-wrap">
            <span className="sd-search-icon">⌕</span>
            <input className="sd-input-search" value={query} onChange={(event) => setQuery(event.target.value)} placeholder={isAdmin ? 'Tìm cơ sở, quản lý hoặc kỳ lương...' : 'Tìm nhân viên, ngân hàng hoặc kỳ lương...'} />
            {query && <button className="sd-search-clear" onClick={() => setQuery('')} type="button">✕</button>}
          </div>
          <div className="sd-filter-chips">
            <button className={`sd-filter-chip ${viewMode === 'CURRENT' ? 'active' : ''}`} onClick={() => setViewMode('CURRENT')} type="button">{isAdmin ? 'Chờ chuyển' : 'Chờ trả'}</button>
            <button className={`sd-filter-chip ${viewMode === 'HISTORY' ? 'active' : ''}`} onClick={() => setViewMode('HISTORY')} type="button">{isAdmin ? 'Lịch sử chuyển lương' : 'Lịch sử trả lương'}</button>
          </div>
          {periodOptions.length > 0 && (
            <div className="sd-salary-period-filter">
              <label htmlFor="salary-period">Kỳ lương</label>
              <select id="salary-period" value={selectedPeriod} onChange={(event) => setSelectedPeriod(event.target.value)}>
                <option value="ALL">Tất cả các tháng</option>
                {periodOptions.map((period) => <option key={period.key} value={period.key}>Tháng {period.month}/{period.year}</option>)}
              </select>
            </div>
          )}
        </div>
        <button className="sd-btn-ghost" onClick={reload} type="button">Làm mới</button>
      </div>

      {message && <p className={`sd-status sd-status-${message.type}`}>{message.text}</p>}
      {isAdmin ? <AdminTable history={viewMode === 'HISTORY'} items={filtered} loading={loading} onSelect={setSelected} /> : <ManagerTable history={viewMode === 'HISTORY'} items={filtered} loading={loading} onSelect={setSelected} />}

      {selected && (
        <div className="sd-overlay" onClick={() => setSelected(null)}>
          <div className="sd-modal sd-salary-pay-modal" onClick={(event) => event.stopPropagation()}>
            <div className="sd-modal-header"><h2>{isAdmin ? 'Xác nhận chuyển lương cho quản lý' : 'Xác nhận trả lương'}</h2><button onClick={() => setSelected(null)} type="button">✕</button></div>
            <div className="sd-modal-body"><dl className="sd-dl">
              <InfoRow label={isAdmin ? 'Quản lý nhận' : 'Nhân viên'} value={isAdmin ? selected.managerName || selected.managerEmail : selected.fullName || selected.username} />
              <InfoRow label="Kỳ lương" value={`Tháng ${selected.month}/${selected.year}`} />
              <InfoRow label="Số tiền" value={money(selected.totalSalary)} />
              <InfoRow label="Ngân hàng" value={isAdmin ? selected.managerBankName : selected.bankName} />
              <InfoRow label="Số tài khoản" value={isAdmin ? selected.managerBankAccountNumber : selected.bankAccountNumber} />
              <InfoRow label="Tên tài khoản" value={isAdmin ? selected.managerBankAccountName : selected.bankAccountName} />
            </dl></div>
            <div className="sd-modal-footer">
              <button className="sd-btn-ghost" onClick={() => setSelected(null)} type="button">Hủy</button>
              <button className="sd-btn-primary" disabled={saving} onClick={confirmPayment} type="button">{saving ? 'Đang cập nhật...' : isAdmin ? 'Xác nhận đã chuyển' : 'Xác nhận đã trả'}</button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}

function AdminTable({ history, items, loading, onSelect }) {
  return <div className="sd-table-wrap"><table className="sd-table"><thead><tr>
    <th>Cơ sở</th><th>Kỳ lương</th><th>Quản lý nhận tiền</th><th>Nhân viên</th><th>Bảng lương</th><th>Tổng cần chuyển</th><th>Đã trả nhân viên</th><th>Trạng thái</th><th>Thao tác</th>
  </tr></thead><tbody>
    {loading ? <tr><td colSpan={9} className="sd-td-empty">Đang tải dữ liệu lương...</td></tr> : items.length === 0 ? <tr><td colSpan={9} className="sd-td-empty">{history ? 'Chưa có lịch sử chuyển lương.' : 'Không có kỳ lương đang chờ chuyển.'}</td></tr> : items.map((item) => <tr key={`${item.branchId}-${item.year}-${item.month}`}>
      <td><strong>{item.branchName || 'Chưa gán cơ sở'}</strong></td>
      <td><strong>{item.month}/{item.year}</strong></td>
      <td><strong>{item.managerName || item.managerEmail || 'Chưa có quản lý'}</strong><span className="sd-subline">{item.managerBankName || 'Chưa có ngân hàng'}</span><span className="sd-subline">{item.managerBankAccountNumber || 'Chưa có STK'}</span></td>
      <td>{number(item.employeeCount)}</td>
      <td><strong>{number(item.salaryCount)}</strong><span className="sd-subline">{number(item.pendingCount)} chưa trả / {number(item.paidCount)} đã trả</span></td>
      <td className="sd-salary-admin-total">{money(item.isTransferred ? item.transferredAmount : item.totalSalary)}</td>
      <td>{money(item.paidTotal)}</td>
      <td>{item.isTransferred ? <><span className="sd-status-pill paid">Đã chuyển</span><span className="sd-subline">{date(item.transferredAt)}</span><span className="sd-subline">Bởi {item.transferredByName || 'Admin'}</span></> : <span className="sd-status-pill pending">Chờ chuyển</span>}</td>
      <td>{history ? <span className="sd-status-pill paid">Hoàn tất</span> : <button className="sd-btn-primary sd-salary-transfer-btn" disabled={!item.managerId} onClick={() => onSelect(item)} type="button">Đã chuyển lương cho quản lý</button>}</td>
    </tr>)}
  </tbody></table></div>;
}

function ManagerTable({ history, items, loading, onSelect }) {
  return <div className="sd-table-wrap"><table className="sd-table"><thead><tr>
    <th>Nhân viên</th><th>Tháng</th><th>Giờ làm</th><th>Thực nhận</th><th>Ngân hàng</th><th>Trạng thái</th><th>{history ? 'Thời gian trả' : 'Thao tác'}</th>
  </tr></thead><tbody>
    {loading ? <tr><td colSpan={7} className="sd-td-empty">Đang tải danh sách lương...</td></tr> : items.length === 0 ? <tr><td colSpan={7} className="sd-td-empty">{history ? 'Chưa có lịch sử trả lương.' : 'Không có bảng lương đang chờ trả.'}</td></tr> : items.map((item) => <tr key={item.id}>
      <td><strong>{item.fullName || item.username}</strong><span className="sd-subline">{item.branchName || 'Chưa gán cơ sở'}</span></td>
      <td>{item.month}/{item.year}</td><td>{number(item.totalHours)} giờ</td><td className="sd-salary-admin-total">{money(item.totalSalary)}</td>
      <td><strong>{item.bankName || 'Chưa có'}</strong><span className="sd-subline">{item.bankAccountNumber || 'Chưa có STK'}</span></td>
      <td><span className={`sd-status-pill ${history ? 'paid' : 'pending'}`}>{statusLabel(item.status)}</span></td>
      <td>{history ? <strong>{date(item.paidAt)}</strong> : <button className="sd-btn-primary" onClick={() => onSelect(item)} type="button">Trả lương</button>}</td>
    </tr>)}
  </tbody></table></div>;
}
