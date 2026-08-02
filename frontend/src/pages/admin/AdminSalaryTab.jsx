import {
  useEffect,
  useMemo,
  useState
} from 'react';

import {
  finalizeBranchSalaryPeriod,
  getAllSalaries,
  getBranchSalaryComplaints,
  getBranchSalaries,
  getPendingSalaryAdjustments,
  getSalaryAdjustmentHistory,
  getSalaryWorkDetails,
  markSalaryPaid,
  reviewSalaryAdjustment,
  resolveSalaryComplaint
} from '../../api/SalaryApi';

import '../css/SalaryInsurance.css';

const money = (value) => new Intl.NumberFormat('vi-VN', { style: 'currency', currency: 'VND', maximumFractionDigits: 0 }).format(Number(value || 0));
const number = (value) => new Intl.NumberFormat('vi-VN', { maximumFractionDigits: 2 }).format(Number(value || 0));
const date = (value) => value ? new Intl.DateTimeFormat('vi-VN', { hour: '2-digit', minute: '2-digit', day: '2-digit', month: '2-digit', year: 'numeric' }).format(new Date(value)) : '—';
const periodKey = (item) => `${item.year}-${String(item.month).padStart(2, '0')}`;
/*
  Lấy phần BHXH do nhân viên đóng.

  Backend đã tính và lưu giá trị này,
  Frontend không tự nhân tỷ lệ BHXH.
*/
function getInsuranceDeduction(item) {
  return Number(
    item?.socialInsuranceDeduction || 0
  );
}


/*
  Lấy lương thực nhận.

  Ưu tiên sử dụng netSalary do Backend trả về.

  Phép trừ phía sau chỉ là dự phòng cho
  những dữ liệu cũ chưa có trường netSalary.
*/
function getNetSalary(item) {
  const backendNetSalary =
    Number(item?.netSalary);

  if (
    Number.isFinite(backendNetSalary)
  ) {
    return backendNetSalary;
  }

  const grossSalary =
    Number(item?.totalSalary || 0);

  const insuranceDeduction =
    getInsuranceDeduction(item);

  return Math.max(
    0,
    grossSalary -
      insuranceDeduction
  );
}

function statusLabel(status) {
  if ((status || '').toUpperCase() === 'PAID') return 'Đã thanh toán';
  if ((status || '').toUpperCase() === 'FINALIZED') return 'Manager đã chốt - chờ trả';
  if ((status || '').toUpperCase() === 'CANCELLED') return 'Đã huỷ';
  return 'Chưa thanh toán';
}

function statusClass(status) {
  const normalized = (status || '').toUpperCase();
  if (normalized === 'PAID') return 'paid';
  if (normalized === 'FINALIZED') return 'finalized';
  return 'pending';
}

function adjustmentStatus(status) {
  const normalized = (status || 'PENDING').toUpperCase();
  if (normalized === 'APPROVED') return { label: 'Đã duyệt', className: 'approved' };
  if (normalized === 'REJECTED') return { label: 'Từ chối', className: 'rejected' };
  return { label: 'Chờ duyệt', className: 'pending' };
}

function complaintStatus(status) {
  return (status || 'PENDING').toUpperCase() === 'RESOLVED'
    ? { label: 'Đã phản hồi', className: 'approved' }
    : { label: 'Chờ xử lý', className: 'pending' };
}

function InfoRow({ label, value }) {
  return <div className="sd-info-row"><dt>{label}</dt><dd>{value || 'Chưa có'}</dd></div>;
}

export function AdminSalaryTab({ isAdmin = true }) {
  const [items, setItems] = useState([]);
  const [query, setQuery] = useState('');
  const [viewMode, setViewMode] = useState('CURRENT');
  const [selectedBranch, setSelectedBranch] = useState('ALL');
  const [selectedPeriod, setSelectedPeriod] = useState('ALL');
  const [selected, setSelected] = useState(null);
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [detailLoading, setDetailLoading] = useState(false);
  const [workDetails, setWorkDetails] = useState([]);
  const [adjustmentHistory, setAdjustmentHistory] = useState([]);
  const [pendingAdjustments, setPendingAdjustments] = useState([]);
  const [reviewingId, setReviewingId] = useState(null);
  const [complaints, setComplaints] = useState([]);
  const [complaintPeriod, setComplaintPeriod] = useState('ALL');
  const [complaintTarget, setComplaintTarget] = useState(null);
  const [complaintResponse, setComplaintResponse] = useState('');
  const [bulkFinalizing, setBulkFinalizing] = useState(false);
  const [message, setMessage] = useState(null);

  async function fetchItems() {
    return isAdmin ? getAllSalaries() : getBranchSalaries();
  }

  async function reload() {
    setLoading(true);
    setMessage(null);
    try {
      const [data, requests, complaintData] = await Promise.all([
        fetchItems(),
        isAdmin ? getPendingSalaryAdjustments() : Promise.resolve([]),
        isAdmin ? Promise.resolve([]) : getBranchSalaryComplaints(),
      ]);
      const nextItems = Array.isArray(data) ? data : [];
      setItems(nextItems);
      setPendingAdjustments(Array.isArray(requests) ? requests : []);
      setComplaints(Array.isArray(complaintData) ? complaintData : []);
      if (selectedPeriod !== 'ALL' && !nextItems.some((item) => periodKey(item) === selectedPeriod)) {
        setSelectedPeriod(nextItems[0] ? periodKey(nextItems[0]) : 'ALL');
      }
      if (selectedBranch !== 'ALL' && !nextItems.some((item) => String(item.branchId) === selectedBranch)) {
        setSelectedBranch('ALL');
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
        const [data, requests, complaintData] = await Promise.all([
          isAdmin ? getAllSalaries() : getBranchSalaries(),
          isAdmin ? getPendingSalaryAdjustments() : Promise.resolve([]),
          isAdmin ? Promise.resolve([]) : getBranchSalaryComplaints(),
        ]);
        if (!ignore) {
          setItems(Array.isArray(data) ? data : []);
          setPendingAdjustments(Array.isArray(requests) ? requests : []);
          setComplaints(Array.isArray(complaintData) ? complaintData : []);
        }
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

  const branchOptions = useMemo(() => {
    const uniqueBranches = new Map();
    items.forEach((item) => {
      if (item.branchId != null && !uniqueBranches.has(String(item.branchId))) {
        uniqueBranches.set(String(item.branchId), {
          id: String(item.branchId),
          name: item.branchName || `Cơ sở ${item.branchId}`,
        });
      }
    });
    return Array.from(uniqueBranches.values())
      .sort((a, b) => a.name.localeCompare(b.name, 'vi'));
  }, [items]);

  const complaintPeriodOptions = useMemo(() => {
    const uniquePeriods = new Map();
    complaints.forEach((item) => {
      const key = periodKey(item);
      if (!uniquePeriods.has(key)) {
        uniquePeriods.set(key, {
          key,
          month: item.month,
          year: item.year,
        });
      }
    });
    return Array.from(uniquePeriods.values())
      .sort((a, b) => b.key.localeCompare(a.key));
  }, [complaints]);

  const filteredComplaints = useMemo(
    () => complaints.filter(
      (item) => complaintPeriod === 'ALL'
        || periodKey(item) === complaintPeriod,
    ),
    [complaints, complaintPeriod],
  );

  const pendingComplaintCount = useMemo(
    () => filteredComplaints.filter(
      (item) => (item.status || 'PENDING').toUpperCase() === 'PENDING',
    ).length,
    [filteredComplaints],
  );

  const filtered = useMemo(() => {
    const keyword = query.trim().toLowerCase();
    return items.filter((item) => {
      if (isAdmin && selectedBranch !== 'ALL' && String(item.branchId) !== selectedBranch) return false;
      if (selectedPeriod !== 'ALL' && periodKey(item) !== selectedPeriod) return false;
      const normalizedStatus = (item.status || '').toUpperCase();
      const history = normalizedStatus === 'PAID';
      if ((viewMode === 'HISTORY') !== history) return false;
      const values = [item.fullName, item.username, item.branchName, item.bankName, item.bankAccountNumber, `${item.month}/${item.year}`];
      return !keyword || values.some((value) => String(value || '').toLowerCase().includes(keyword));
    });
  }, [items, query, viewMode, isAdmin, selectedBranch, selectedPeriod]);

  const summary =
  useMemo(() => {
    return items
      .filter((item) => {
        const matchesBranch =
          !isAdmin ||
          selectedBranch === 'ALL' ||
          String(item.branchId) ===
            selectedBranch;

        const matchesPeriod =
          selectedPeriod === 'ALL' ||
          periodKey(item) ===
            selectedPeriod;

        return (
          matchesBranch &&
          matchesPeriod
        );
      })
      .reduce(
        (total, item) => {
          const normalizedStatus =
            String(
              item.status || ''
            ).toUpperCase();

          const isPaid =
            normalizedStatus ===
            'PAID';

          const grossSalary =
            Number(
              item.totalSalary || 0
            );

          const insuranceDeduction =
            getInsuranceDeduction(
              item
            );

          const netSalary =
            getNetSalary(item);

          total.count += 1;

          total.gross +=
            grossSalary;

          total.insurance +=
            insuranceDeduction;

          if (isPaid) {
            total.paid +=
              netSalary;
          } else {
            total.pending +=
              netSalary;
          }

          return total;
        },
        {
          count: 0,
          gross: 0,
          insurance: 0,
          pending: 0,
          paid: 0
        }
      );
  }, [
    items,
    isAdmin,
    selectedBranch,
    selectedPeriod
  ]);

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

  async function confirmBranchFinalization() {
    if (isAdmin || selectedPeriod === 'ALL') {
      setMessage({ type: 'error', text: 'Vui lòng chọn một kỳ lương cụ thể trước khi chốt.' });
      return;
    }
    const [year, month] = selectedPeriod.split('-').map(Number);
    setBulkFinalizing(true);
    setMessage(null);
    try {
      const updated = await finalizeBranchSalaryPeriod(month, year);
      const updatedMap = new Map((Array.isArray(updated) ? updated : []).map((item) => [item.id, item]));
      setItems((current) => current.map((item) => updatedMap.get(item.id) || item));
      setMessage({ type: 'success', text: `Đã chốt lương toàn bộ nhân viên cơ sở tháng ${month}/${year}.` });
    } catch (err) {
      setMessage({ type: 'error', text: err.response?.data?.message || 'Không thể chốt lương toàn cơ sở.' });
    } finally {
      setBulkFinalizing(false);
    }
  }

  async function submitComplaintResponse(event) {
    event.preventDefault();
    if (!complaintTarget || !complaintResponse.trim()) return;
    setSaving(true);
    setMessage(null);
    try {
      const updated = await resolveSalaryComplaint(complaintTarget.id, complaintResponse.trim());
      setComplaints((current) => current.map((item) => item.id === updated.id ? updated : item));
      setComplaintTarget(null);
      setComplaintResponse('');
      setMessage({ type: 'success', text: 'Đã gửi phản hồi khiếu nại cho nhân viên.' });
    } catch (err) {
      setMessage({ type: 'error', text: err.response?.data?.message || 'Không thể phản hồi khiếu nại.' });
    } finally {
      setSaving(false);
    }
  }

  async function reviewAdjustment(item, isApproved) {
    if (!isAdmin) return;
    setReviewingId(item.id);
    setMessage(null);
    try {
      await reviewSalaryAdjustment(item.id, isApproved);
      setPendingAdjustments((current) => current.filter((request) => request.id !== item.id));
      setMessage({
        type: 'success',
        text: isApproved
          ? 'Đã duyệt và cập nhật khoản thưởng/phạt vào lương.'
          : 'Đã từ chối yêu cầu thưởng/phạt.',
      });
      const salaryData = await getAllSalaries();
      setItems(Array.isArray(salaryData) ? salaryData : []);
    } catch (err) {
      setMessage({ type: 'error', text: err.response?.data?.message || 'Không thể xử lý yêu cầu thưởng/phạt.' });
    } finally {
      setReviewingId(null);
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
  <div className="sd-stat-card">
    <span className="sd-stat-icon">
      ∑
    </span>

    <h3>
      {summary.count}
    </h3>

    <p>
      {isAdmin
        ? 'Bảng lương toàn hệ thống'
        : 'Bảng lương cơ sở'}
    </p>
  </div>

  <div className="sd-stat-card">
    <span className="sd-stat-icon">
      ₫
    </span>

    <h3>
      {money(summary.gross)}
    </h3>

    <p>
      Lương trước BHXH
    </p>
  </div>

  <div className="sd-stat-card sd-salary-insurance-stat">
    <span className="sd-stat-icon">
      −
    </span>

    <h3>
      {money(summary.insurance)}
    </h3>

    <p>
      BHXH nhân viên
    </p>
  </div>

  <div className="sd-stat-card">
    <span className="sd-stat-icon">
      …
    </span>

    <h3>
      {money(summary.pending)}
    </h3>

    <p>
      Thực nhận chưa trả
    </p>
  </div>

  <div className="sd-stat-card sd-salary-paid-stat">
    <span className="sd-stat-icon">
      ✓
    </span>

    <h3>
      {money(summary.paid)}
    </h3>

    <p>
      Thực nhận đã trả
    </p>
  </div>
</div>

      {isAdmin && (
        <div className="sd-card sd-adjustment-approval-card">
          <div className="sd-card-header">
            <div>
              <p className="sd-eyebrow">Phê duyệt</p>
              <h2>Yêu cầu thưởng/phạt đang chờ ({pendingAdjustments.length})</h2>
            </div>
          </div>
          <div className="sd-table-wrap">
            <table className="sd-table sd-adjustment-approval-table">
              <thead>
                <tr><th>Nhân viên</th><th>Cơ sở</th><th>Kỳ lương</th><th>Thưởng</th><th>Phạt</th><th>Lý do</th><th>Người gửi</th><th>Thao tác</th></tr>
              </thead>
              <tbody>
                {pendingAdjustments.length === 0 ? (
                  <tr><td className="sd-td-empty" colSpan={8}>Không có yêu cầu thưởng/phạt đang chờ duyệt.</td></tr>
                ) : pendingAdjustments.map((item) => (
                  <tr key={item.id}>
                    <td><strong>{item.employeeName}</strong></td>
                    <td>{item.branchName || 'Chưa gán'}</td>
                    <td>{item.month}/{item.year}</td>
                    <td>{money(item.bonusAmount)}</td>
                    <td>{money(item.penaltyAmount)}</td>
                    <td>{item.reason}</td>
                    <td>{item.createdByName || 'Quản lý'}<span className="sd-subline">{date(item.createdAt)}</span></td>
                    <td>
                      <div className="sd-salary-actions">
                        <button className="sd-btn-primary" disabled={reviewingId === item.id} onClick={() => reviewAdjustment(item, true)} type="button">
                          {reviewingId === item.id ? 'Đang xử lý...' : 'Duyệt'}
                        </button>
                        <button className="sd-btn-primary btn-danger" disabled={reviewingId === item.id} onClick={() => reviewAdjustment(item, false)} type="button">Từ chối</button>
                      </div>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </div>
      )}

      {!isAdmin && (
        <div className="sd-card sd-salary-complaint-card">
          <div className="sd-card-header sd-salary-complaint-header">
            <div>
              <p className="sd-eyebrow">Phản hồi nhân viên</p>
              <h2>Khiếu nại lương ({pendingComplaintCount} chờ xử lý)</h2>
            </div>
            <div className="sd-salary-period-filter sd-complaint-period-filter">
              <label htmlFor="complaint-period">Tháng khiếu nại</label>
              <select
                id="complaint-period"
                onChange={(event) => setComplaintPeriod(event.target.value)}
                value={complaintPeriod}
              >
                <option value="ALL">Tất cả các tháng</option>
                {complaintPeriodOptions.map((period) => (
                  <option key={period.key} value={period.key}>
                    Tháng {period.month}/{period.year}
                  </option>
                ))}
              </select>
            </div>
          </div>
          <div className="sd-table-wrap">
            <table className="sd-table sd-salary-complaint-table">
              <thead><tr><th>Nhân viên</th><th>Kỳ lương</th><th>Nội dung</th><th>Thời gian gửi</th><th>Trạng thái</th><th>Thao tác</th></tr></thead>
              <tbody>
                {filteredComplaints.length === 0 ? (
                  <tr>
                    <td className="sd-td-empty" colSpan={6}>
                      {complaintPeriod === 'ALL'
                        ? 'Chưa có khiếu nại lương từ nhân viên.'
                        : 'Không có khiếu nại trong tháng đã chọn.'}
                    </td>
                  </tr>
                ) : filteredComplaints.map((item) => {
                  const status = complaintStatus(item.status);
                  return (
                    <tr key={item.id}>
                      <td><strong>{item.employeeName}</strong></td>
                      <td>{item.month}/{item.year}</td>
                      <td>{item.content}{item.managerResponse && <span className="sd-subline">Phản hồi: {item.managerResponse}</span>}</td>
                      <td>{date(item.createdAt)}</td>
                      <td><span className={`sd-status-pill ${status.className}`}>{status.label}</span></td>
                      <td>
                        {(item.status || 'PENDING').toUpperCase() === 'PENDING'
                          ? <button className="sd-btn-primary" onClick={() => { setComplaintTarget(item); setComplaintResponse(''); }} type="button">Phản hồi</button>
                          : 'Đã xử lý'}
                      </td>
                    </tr>
                  );
                })}
              </tbody>
            </table>
          </div>
        </div>
      )}

      <div className="sd-users-toolbar">
        <div className="sd-users-toolbar-left">
          <div className="sd-search-wrap">
            <span className="sd-search-icon">⌕</span>
            <input className="sd-input-search" value={query} onChange={(event) => setQuery(event.target.value)} placeholder="Tìm nhân viên, cơ sở, ngân hàng hoặc kỳ lương..." />
            {query && <button className="sd-search-clear" onClick={() => setQuery('')} type="button">✕</button>}
          </div>
          <div className="sd-filter-chips">
            <button className={`sd-filter-chip ${viewMode === 'CURRENT' ? 'active' : ''}`} onClick={() => setViewMode('CURRENT')} type="button">Chờ trả</button>
            <button className={`sd-filter-chip ${viewMode === 'HISTORY' ? 'active' : ''}`} onClick={() => setViewMode('HISTORY')} type="button">Lịch sử trả lương</button>
          </div>
          {isAdmin && branchOptions.length > 0 && (
            <div className="sd-salary-period-filter sd-salary-branch-filter">
              <label htmlFor="salary-branch">Cơ sở</label>
              <select
                id="salary-branch"
                onChange={(event) => setSelectedBranch(event.target.value)}
                value={selectedBranch}
              >
                <option value="ALL">Tất cả cơ sở</option>
                {branchOptions.map((branch) => (
                  <option key={branch.id} value={branch.id}>{branch.name}</option>
                ))}
              </select>
            </div>
          )}
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
        <div className="sd-salary-toolbar-actions">
          {!isAdmin && (
            <button
              className="sd-btn-primary"
              disabled={bulkFinalizing || selectedPeriod === 'ALL'}
              onClick={confirmBranchFinalization}
              type="button"
            >
              {bulkFinalizing ? 'Đang chốt...' : 'Chốt lương cả cơ sở'}
            </button>
          )}
          <button className="sd-btn-ghost" onClick={reload} type="button">Làm mới</button>
        </div>
      </div>

      {message && <p className={`sd-status sd-status-${message.type}`}>{message.text}</p>}
      <SalaryEmployeeTable history={viewMode === 'HISTORY'} isAdmin={isAdmin} items={filtered} loading={loading} onSelect={openSalaryDetail} />

      {selected && (
        <div className="sd-overlay" onClick={() => setSelected(null)}>
          <div className="sd-modal sd-salary-pay-modal sd-modal--wide" onClick={(event) => event.stopPropagation()}>
            <div className="sd-modal-header"><h2>Chi tiết bảng lương</h2><button onClick={() => setSelected(null)} type="button">✕</button></div>
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
  <div>
    <span>
      Tổng giờ
    </span>

    <strong>
      {number(
        selected.totalHours
      )}{' '}
      giờ
    </strong>
  </div>

  <div>
    <span>
      Lương/giờ
    </span>

    <strong>
      {money(
        selected.hourlyWageAtTime
      )}
    </strong>
  </div>

  <div>
    <span>
      Lương cơ bản
    </span>

    <strong>
      {money(
        Number(
          selected.totalHours || 0
        ) *
        Number(
          selected.hourlyWageAtTime || 0
        )
      )}
    </strong>
  </div>

  <div>
    <span>
      Thưởng
    </span>

    <strong>
      {money(
        selected.totalBonus
      )}
    </strong>
  </div>

  <div>
    <span>
      Phạt
    </span>

    <strong>
      {money(
        selected.totalPenalty
      )}
    </strong>
  </div>

  <div className="salary-gross">
    <span>
      Lương trước BHXH
    </span>

    <strong>
      {money(
        selected.totalSalary
      )}
    </strong>
  </div>

  <div className="salary-insurance-deduction">
    <span>
      BHXH nhân viên đóng
    </span>

    <strong>
      − {money(
        getInsuranceDeduction(
          selected
        )
      )}
    </strong>
  </div>

  <div className="total salary-net">
    <span>
      Lương thực nhận
    </span>

    <strong>
      {money(
        getNetSalary(selected)
      )}
    </strong>
  </div>
</div>

{selected.bhxhContributionId ? (
  <p className="salary-insurance-note">
    Bảng lương đã liên kết với khoản đóng
    BHXH #{selected.bhxhContributionId}.
    Chỉ phần BHXH của nhân viên được khấu trừ
    khỏi lương.
  </p>
) : (
  <p className="salary-insurance-note salary-insurance-note--empty">
    Bảng lương này không có khoản khấu trừ
    BHXH. Trường hợp này phù hợp với nhân viên
    PART_TIME hoặc bảng lương chưa được chốt.
  </p>
)}

                  {selected.finalizedAt && <p className="sd-salary-finalized-note">Chốt lúc {date(selected.finalizedAt)} bởi {selected.finalizedByName || 'Manager'}</p>}

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
                        <table className="sd-table sd-salary-detail-table"><thead><tr><th>Thời gian</th><th>Thưởng</th><th>Phạt</th><th>Lý do</th><th>Trạng thái</th><th>Admin duyệt</th></tr></thead><tbody>
                          {adjustmentHistory.length === 0 ? <tr><td colSpan={6} className="sd-td-empty">Chưa có thưởng/phạt thủ công.</td></tr> : adjustmentHistory.map((item) => {
                            const status = adjustmentStatus(item.status);
                            return <tr key={item.id}>
                              <td>{date(item.createdAt)}</td><td>{money(item.bonusAmount)}</td><td>{money(item.penaltyAmount)}</td><td>{item.reason}</td>
                              <td><span className={`sd-status-pill ${status.className}`}>{status.label}</span></td><td>{item.reviewedByName || '—'}</td>
                            </tr>;
                          })}
                        </tbody></table>
                      </div>
                    </>
                  )}
              </>
            </div>
            <div className="sd-modal-footer">
              <button className="sd-btn-ghost" onClick={() => setSelected(null)} type="button">Đóng</button>
              {!isAdmin && (selected.status || '').toUpperCase() === 'FINALIZED' && <button className="sd-btn-primary" disabled={saving || detailLoading} onClick={confirmPayment} type="button">{saving ? 'Đang cập nhật...' : 'Xác nhận đã trả'}</button>}
            </div>
          </div>
        </div>
      )}

      {complaintTarget && (
        <div className="sd-overlay" onClick={() => setComplaintTarget(null)}>
          <div className="sd-modal" onClick={(event) => event.stopPropagation()}>
            <div className="sd-modal-header">
              <h2>Phản hồi khiếu nại lương</h2>
              <button onClick={() => setComplaintTarget(null)} type="button">✕</button>
            </div>
            <form onSubmit={submitComplaintResponse}>
              <div className="sd-modal-body">
                <dl className="sd-dl">
                  <InfoRow label="Nhân viên" value={complaintTarget.employeeName} />
                  <InfoRow label="Kỳ lương" value={`${complaintTarget.month}/${complaintTarget.year}`} />
                  <InfoRow label="Nội dung" value={complaintTarget.content} />
                </dl>
                <div className="sd-field">
                  <label>Phản hồi của Manager</label>
                  <textarea
                    maxLength="1000"
                    onChange={(event) => setComplaintResponse(event.target.value)}
                    required
                    rows="5"
                    value={complaintResponse}
                  />
                </div>
              </div>
              <div className="sd-modal-footer">
                <button className="sd-btn-ghost" onClick={() => setComplaintTarget(null)} type="button">Hủy</button>
                <button className="sd-btn-primary" disabled={saving || !complaintResponse.trim()} type="submit">{saving ? 'Đang gửi...' : 'Gửi phản hồi'}</button>
              </div>
            </form>
          </div>
        </div>
      )}
    </div>
  );
}

function SalaryEmployeeTable({
  history,
  isAdmin,
  items,
  loading,
  onSelect
}) {
  return (
   <div className="sd-table-wrap sd-salary-insurance-table-wrap">
      <table
        className={
          'sd-table ' +
          'sd-salary-employee-table ' +
          'sd-salary-insurance-table'
        }
      >
        <thead>
          <tr>
            <th>
              Nhân viên
            </th>

            <th>
              Cơ sở
            </th>

            <th>
              Tháng
            </th>

            <th>
              Giờ làm
            </th>

            <th>
              Lương trước BHXH
            </th>

            <th>
              Khấu trừ BHXH
            </th>

            <th>
              Thực nhận
            </th>

            <th>
              Ngân hàng
            </th>

            <th>
              Trạng thái
            </th>
          </tr>
        </thead>

        <tbody>
          {loading ? (
            <tr>
              <td
                colSpan={9}
                className="sd-td-empty"
              >
                Đang tải danh sách lương...
              </td>
            </tr>
          ) : items.length === 0 ? (
            <tr>
              <td
                colSpan={9}
                className="sd-td-empty"
              >
                {history
                  ? 'Chưa có lịch sử trả lương.'
                  : (
                      isAdmin
                        ? 'Chưa có bảng lương nào được Manager chốt.'
                        : 'Không có bảng lương đang chờ trả.'
                    )}
              </td>
            </tr>
          ) : (
            items.map((item) => {
              const insuranceDeduction =
                getInsuranceDeduction(
                  item
                );

              const netSalary =
                getNetSalary(item);

              return (
                <tr
                  className="sd-tr"
                  key={item.id}
                  role="button"
                  tabIndex={0}
                  onClick={() => {
                    onSelect(item);
                  }}
                  onKeyDown={(event) => {
                    if (
                      event.key === 'Enter' ||
                      event.key === ' '
                    ) {
                      event.preventDefault();
                      onSelect(item);
                    }
                  }}
                >
                  <td>
                    <strong>
                      {item.fullName ||
                        item.username}
                    </strong>
                  </td>

                  <td>
                    {item.branchName ||
                      'Chưa gán cơ sở'}
                  </td>

                  <td>
                    {item.month}/{item.year}
                  </td>

                  <td>
                    {number(
                      item.totalHours
                    )}{' '}
                    giờ
                  </td>

                  <td className="sd-salary-gross-value">
                    {money(
                      item.totalSalary
                    )}
                  </td>

                  <td className="sd-salary-insurance-value">
                    {insuranceDeduction > 0
                      ? `− ${money(
                          insuranceDeduction
                        )}`
                      : money(0)}
                  </td>

                  <td className="sd-salary-net-value">
                    {money(netSalary)}
                  </td>

                  <td>
                    <strong>
                      {item.bankName ||
                        'Chưa có'}
                    </strong>

                    <span className="sd-subline">
                      {item.bankAccountNumber ||
                        'Chưa có STK'}
                    </span>
                  </td>

                  <td>
                    <span
                      className={
                        `sd-status-pill ` +
                        statusClass(
                          item.status
                        )
                      }
                    >
                      {statusLabel(
                        item.status
                      )}
                    </span>
                  </td>
                </tr>
              );
            })
          )}
        </tbody>
      </table>
    </div>
  );
}
