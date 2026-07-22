import { useEffect, useMemo, useState } from 'react';
import {
  adminFinalizeSalary,
  finalizeSalary,
  getAllSalaries,
  getBranchSalaries,
  getSalaryAdjustmentHistory,
  getSalaryWorkDetails,
  markSalaryPaid,
} from '../../api/SalaryApi';

const money = (value) => new Intl.NumberFormat('vi-VN', { style: 'currency', currency: 'VND', maximumFractionDigits: 0 }).format(Number(value || 0));
const number = (value) => new Intl.NumberFormat('vi-VN', { maximumFractionDigits: 2 }).format(Number(value || 0));
const date = (value) => value ? new Intl.DateTimeFormat('vi-VN', { hour: '2-digit', minute: '2-digit', day: '2-digit', month: '2-digit', year: 'numeric' }).format(new Date(value)) : '—';
const periodKey = (item) => `${item.year}-${String(item.month).padStart(2, '0')}`;

function statusLabel(status) {
  if ((status || '').toUpperCase() === 'PAID') return 'Đã thanh toán';
  if ((status || '').toUpperCase() === 'ADMIN_FINALIZED') return 'Admin đã chốt - chờ trả';
  if ((status || '').toUpperCase() === 'FINALIZED') return 'Manager đã chốt - chờ admin';
  if ((status || '').toUpperCase() === 'CANCELLED') return 'Đã huỷ';
  return 'Chưa thanh toán';
}

function statusClass(status) {
  const normalized = (status || '').toUpperCase();
  if (normalized === 'PAID') return 'paid';
  if (normalized === 'ADMIN_FINALIZED') return 'approved';
  if (normalized === 'FINALIZED') return 'finalized';
  return 'pending';
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
  const [detailLoading, setDetailLoading] = useState(false);
  const [workDetails, setWorkDetails] = useState([]);
  const [adjustmentHistory, setAdjustmentHistory] = useState([]);
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
      const normalizedStatus = (item.status || '').toUpperCase();
      const history = isAdmin
        ? normalizedStatus === 'ADMIN_FINALIZED' || normalizedStatus === 'PAID'
        : normalizedStatus === 'PAID';
      if ((viewMode === 'HISTORY') !== history) return false;
      const values = [item.fullName, item.username, item.branchName, item.bankName, item.bankAccountNumber, `${item.month}/${item.year}`];
      return !keyword || values.some((value) => String(value || '').toLowerCase().includes(keyword));
    });
  }, [items, query, viewMode, isAdmin, selectedPeriod]);

  const summary = useMemo(() => items
    .filter((item) => selectedPeriod === 'ALL' || periodKey(item) === selectedPeriod)
    .reduce((total, item) => {
    const normalizedStatus = (item.status || '').toUpperCase();
    const completed = isAdmin
      ? normalizedStatus === 'ADMIN_FINALIZED' || normalizedStatus === 'PAID'
      : normalizedStatus === 'PAID';
    total.count += 1;
    total[completed ? 'paid' : 'pending'] += Number(item.totalSalary || 0);
    return total;
  }, { count: 0, pending: 0, paid: 0 }), [items, isAdmin, selectedPeriod]);

  async function confirmPayment() {
    if (!selected) return;
    setSaving(true);
    setMessage(null);
    try {
      const updated = await markSalaryPaid(selected.id);
      setItems((current) => current.map((item) => item.id === updated.id ? updated : item));
      setMessage({ type: 'success', text: 'Đã xác nhận trả lương cho nhân viên.' });
      setSelected(null);
    } catch (err) {
      setMessage({ type: 'error', text: err.response?.data?.message || 'Không thể cập nhật thanh toán.' });
    } finally {
      setSaving(false);
    }
  }

  async function confirmAdminFinalization() {
    if (!selected || !isAdmin) return;
    setSaving(true);
    setMessage(null);
    try {
      const updated = await adminFinalizeSalary(selected.id);
      setItems((current) => current.map((item) => item.id === updated.id ? updated : item));
      setSelected(null);
      setMessage({ type: 'success', text: 'Admin đã chốt bảng lương của nhân viên.' });
    } catch (err) {
      setMessage({ type: 'error', text: err.response?.data?.message || 'Không thể chốt bảng lương.' });
    } finally {
      setSaving(false);
    }
  }

  async function confirmFinalization() {
    if (!selected || isAdmin) return;
    setSaving(true);
    setMessage(null);
    try {
      const updated = await finalizeSalary(selected.id);
      setItems((current) => current.map((item) => item.id === updated.id ? updated : item));
      setSelected(updated);
      setMessage({ type: 'success', text: 'Đã chốt bảng lương. Các số liệu của kỳ này đã được khóa.' });
    } catch (err) {
      setMessage({ type: 'error', text: err.response?.data?.message || 'Không thể chốt bảng lương.' });
    } finally {
      setSaving(false);
    }
  }

  async function openSalaryDetail(item) {
    setSelected(item);
    setDetailLoading(true);
    setWorkDetails([]);
    setAdjustmentHistory([]);
    try {
      const [details, history] = await Promise.all([
        getSalaryWorkDetails(item.userId, item.month, item.year),
        getSalaryAdjustmentHistory(item.userId, item.month, item.year),
      ]);
      setWorkDetails(Array.isArray(details) ? details : []);
      setAdjustmentHistory(Array.isArray(history) ? history : []);
    } catch (err) {
      setMessage({ type: 'error', text: err.response?.data?.message || 'Không tải được chi tiết bảng lương.' });
    } finally {
      setDetailLoading(false);
    }
  }

  return (
    <div className={`sd-salary-admin-page ${isAdmin ? 'sd-salary-admin-page--admin' : ''}`}>
      <div className="sd-stat-grid sd-salary-admin-stats">
        <div className="sd-stat-card"><span className="sd-stat-icon">∑</span><h3>{summary.count}</h3><p>{isAdmin ? 'Bảng lương manager đã gửi' : 'Bảng lương cơ sở'}</p></div>
        <div className="sd-stat-card"><span className="sd-stat-icon">₫</span><h3>{money(summary.pending)}</h3><p>{isAdmin ? 'Chờ admin chốt' : 'Chưa thanh toán'}</p></div>
        <div className="sd-stat-card"><span className="sd-stat-icon">✓</span><h3>{money(summary.paid)}</h3><p>{isAdmin ? 'Admin đã chốt' : 'Đã thanh toán'}</p></div>
      </div>

      <div className="sd-users-toolbar">
        <div className="sd-users-toolbar-left">
          <div className="sd-search-wrap">
            <span className="sd-search-icon">⌕</span>
            <input className="sd-input-search" value={query} onChange={(event) => setQuery(event.target.value)} placeholder="Tìm nhân viên, cơ sở, ngân hàng hoặc kỳ lương..." />
            {query && <button className="sd-search-clear" onClick={() => setQuery('')} type="button">✕</button>}
          </div>
          <div className="sd-filter-chips">
            <button className={`sd-filter-chip ${viewMode === 'CURRENT' ? 'active' : ''}`} onClick={() => setViewMode('CURRENT')} type="button">{isAdmin ? 'Chờ admin chốt' : 'Chờ trả'}</button>
            <button className={`sd-filter-chip ${viewMode === 'HISTORY' ? 'active' : ''}`} onClick={() => setViewMode('HISTORY')} type="button">{isAdmin ? 'Admin đã chốt' : 'Lịch sử trả lương'}</button>
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
      <SalaryEmployeeTable history={viewMode === 'HISTORY'} isAdmin={isAdmin} items={filtered} loading={loading} onSelect={openSalaryDetail} />

      {selected && (
        <div className="sd-overlay" onClick={() => setSelected(null)}>
          <div className="sd-modal sd-salary-pay-modal sd-modal--wide" onClick={(event) => event.stopPropagation()}>
            <div className="sd-modal-header"><h2>{isAdmin ? 'Chi tiết và chốt lương nhân viên' : 'Chi tiết và chốt lương'}</h2><button onClick={() => setSelected(null)} type="button">✕</button></div>
            <div className="sd-modal-body">
              <dl className="sd-dl">
                <InfoRow label="Nhân viên" value={selected.fullName || selected.username} />
                <InfoRow label="Kỳ lương" value={`Tháng ${selected.month}/${selected.year}`} />
                <InfoRow label="Cơ sở" value={selected.branchName} />
                <InfoRow label="Trạng thái" value={statusLabel(selected.status)} />
                <InfoRow label="Ngân hàng" value={selected.bankName} />
                <InfoRow label="Số tài khoản" value={selected.bankAccountNumber} />
                <InfoRow label="Tên tài khoản" value={selected.bankAccountName} />
              </dl>

              <>
                  <div className="sd-salary-detail-summary">
                    <div><span>Tổng giờ</span><strong>{number(selected.totalHours)} giờ</strong></div>
                    <div><span>Lương/giờ</span><strong>{money(selected.hourlyWageAtTime)}</strong></div>
                    <div><span>Lương cơ bản</span><strong>{money(Number(selected.totalHours) * Number(selected.hourlyWageAtTime))}</strong></div>
                    <div><span>Thưởng</span><strong>{money(selected.totalBonus)}</strong></div>
                    <div><span>Phạt</span><strong>{money(selected.totalPenalty)}</strong></div>
                    <div className="total"><span>Thực nhận</span><strong>{money(selected.totalSalary)}</strong></div>
                  </div>

                  {selected.finalizedAt && <p className="sd-salary-finalized-note">Chốt lúc {date(selected.finalizedAt)} bởi {selected.finalizedByName || 'Manager'}</p>}
                  {selected.adminFinalizedAt && <p className="sd-salary-finalized-note sd-salary-finalized-note--admin">Admin chốt lúc {date(selected.adminFinalizedAt)} bởi {selected.adminFinalizedByName || 'Admin'}</p>}

                  {detailLoading ? <p className="sd-salary-empty">Đang tải chi tiết lương...</p> : (
                    <>
                      <h3 className="sd-salary-detail-title">Chi tiết giờ làm</h3>
                      <div className="sd-table-wrap sd-salary-detail-table-wrap">
                        <table className="sd-table sd-salary-detail-table"><thead><tr><th>Ngày</th><th>Ca</th><th>Vào</th><th>Ra</th><th>Số giờ</th></tr></thead><tbody>
                          {workDetails.length === 0 ? <tr><td colSpan={5} className="sd-td-empty">Chưa có dữ liệu chấm công.</td></tr> : workDetails.map((item) => <tr key={item.attendanceId}>
                            <td>{new Intl.DateTimeFormat('vi-VN').format(new Date(`${item.workDate}T00:00:00`))}</td><td>{item.shiftName}</td><td>{date(item.checkInTime)}</td><td>{date(item.checkOutTime)}</td><td>{number(item.workedHours)} giờ</td>
                          </tr>)}
                        </tbody></table>
                      </div>

                      <h3 className="sd-salary-detail-title">Chi tiết thưởng/phạt</h3>
                      <div className="sd-table-wrap sd-salary-detail-table-wrap">
                        <table className="sd-table sd-salary-detail-table"><thead><tr><th>Thời gian</th><th>Thưởng</th><th>Phạt</th><th>Lý do</th></tr></thead><tbody>
                          {adjustmentHistory.length === 0 ? <tr><td colSpan={4} className="sd-td-empty">Chưa có thưởng/phạt thủ công.</td></tr> : adjustmentHistory.map((item) => <tr key={item.id}>
                            <td>{date(item.createdAt)}</td><td>{money(item.bonusAmount)}</td><td>{money(item.penaltyAmount)}</td><td>{item.reason}</td>
                          </tr>)}
                        </tbody></table>
                      </div>
                    </>
                  )}
              </>
            </div>
            <div className="sd-modal-footer">
              <button className="sd-btn-ghost" onClick={() => setSelected(null)} type="button">Đóng</button>
              {!isAdmin && (selected.status || '').toUpperCase() === 'ADMIN_FINALIZED' && <button className="sd-btn-primary" disabled={saving || detailLoading} onClick={confirmPayment} type="button">{saving ? 'Đang cập nhật...' : 'Xác nhận đã trả'}</button>}
              {!isAdmin && (selected.status || 'PENDING').toUpperCase() === 'PENDING' && <button className="sd-btn-primary" disabled={saving || detailLoading} onClick={confirmFinalization} type="button">{saving ? 'Đang chốt...' : 'Chốt lương'}</button>}
              {isAdmin && (selected.status || '').toUpperCase() === 'FINALIZED' && <button className="sd-btn-primary" disabled={saving || detailLoading} onClick={confirmAdminFinalization} type="button">{saving ? 'Đang chốt...' : 'Admin chốt lương'}</button>}
            </div>
          </div>
        </div>
      )}
    </div>
  );
}

function SalaryEmployeeTable({ history, isAdmin, items, loading, onSelect }) {
  return <div className="sd-table-wrap"><table className="sd-table sd-salary-employee-table"><thead><tr>
    <th>Nhân viên</th><th>Cơ sở</th><th>Tháng</th><th>Giờ làm</th><th>Thực nhận</th><th>Ngân hàng</th><th>Trạng thái</th><th>Thao tác</th>
  </tr></thead><tbody>
    {loading ? <tr><td colSpan={8} className="sd-td-empty">Đang tải danh sách lương...</td></tr> : items.length === 0 ? <tr><td colSpan={8} className="sd-td-empty">{isAdmin ? (history ? 'Chưa có bảng lương admin đã chốt.' : 'Chưa có bảng lương nào được manager chốt gửi lên.') : (history ? 'Chưa có lịch sử trả lương.' : 'Không có bảng lương đang chờ xử lý.')}</td></tr> : items.map((item) => <tr key={item.id}>
      <td><strong>{item.fullName || item.username}</strong></td>
      <td>{item.branchName || 'Chưa gán cơ sở'}</td>
      <td>{item.month}/{item.year}</td><td>{number(item.totalHours)} giờ</td><td className="sd-salary-admin-total">{money(item.totalSalary)}</td>
      <td><strong>{item.bankName || 'Chưa có'}</strong><span className="sd-subline">{item.bankAccountNumber || 'Chưa có STK'}</span></td>
      <td><span className={`sd-status-pill ${statusClass(item.status)}`}>{statusLabel(item.status)}</span></td>
      <td><button className={history ? 'sd-btn-ghost' : 'sd-btn-primary'} onClick={() => onSelect(item)} type="button">{history ? 'Xem chi tiết' : 'Chi tiết lương'}</button></td>
    </tr>)}
  </tbody></table></div>;
}
