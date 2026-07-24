import { useEffect, useMemo, useState } from 'react';
import {
  addManualSalaryAdjustment,
  getSalaryAdjustmentHistory,
  getSalaryRuleAdjustments,
  updateSalaryRule,
} from '../../api/SalaryApi';

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

function getCurrentPeriod() {
  const now = new Date();
  return {
    month: now.getMonth() + 1,
    year: now.getFullYear(),
  };
}

function formatWorkDate(value) {
  if (!value) return '—';
  const [year, month, day] = String(value).slice(0, 10).split('-').map(Number);
  return new Intl.DateTimeFormat('vi-VN', {
    weekday: 'long',
    day: '2-digit',
    month: '2-digit',
    year: 'numeric',
  }).format(new Date(year, month - 1, day));
}

function SalaryRuleMetric({ label, value }) {
  return (
    <div className="sd-salary-metric">
      <span>{label}</span>
      <strong>{value}</strong>
    </div>
  );
}

const EMPTY_RULE_FORM = {
  bonusThresholdDays: '',
  bonusAmount: '',
  latePenalty: '',
  absentPenalty: '',
  weekendMultiplier: '1',
};

export function ManagerSalaryRuleTab({ user, isAdmin = false, branches = [] }) {
  const currentPeriod = getCurrentPeriod();
  const [period, setPeriod] = useState(currentPeriod);
  const [selectedBranchId, setSelectedBranchId] = useState(user?.branchId || '');
  const [rule, setRule] = useState(null);
  const [ruleForm, setRuleForm] = useState(EMPTY_RULE_FORM);
  const [employees, setEmployees] = useState([]);
  const [query, setQuery] = useState('');
  const [loading, setLoading] = useState(true);
  const [savingRule, setSavingRule] = useState(false);
  const [savingUserId, setSavingUserId] = useState(null);
  const [manualTarget, setManualTarget] = useState(null);
  const [manualForm, setManualForm] = useState({ bonusAmount: '', penaltyAmount: '', reason: '' });
  const [historyTarget, setHistoryTarget] = useState(null);
  const [history, setHistory] = useState([]);
  const [historyLoading, setHistoryLoading] = useState(false);
  const [attendanceTarget, setAttendanceTarget] = useState(null);
  const [employeeDetailTarget, setEmployeeDetailTarget] = useState(null);
  const [message, setMessage] = useState(null);

  useEffect(() => {
    if (!isAdmin) {
      setSelectedBranchId(user?.branchId || '');
      return;
    }

    if (!selectedBranchId && branches.length > 0) {
      setSelectedBranchId(branches[0].id);
    }
  }, [branches, isAdmin, selectedBranchId, user?.branchId]);

  useEffect(() => {
    setRuleForm(rule ? {
      bonusThresholdDays: String(rule.bonusThresholdDays ?? ''),
      bonusAmount: String(rule.bonusAmount ?? ''),
      latePenalty: String(rule.latePenalty ?? ''),
      absentPenalty: String(rule.absentPenalty ?? ''),
      weekendMultiplier: String(rule.weekendMultiplier ?? 1),
    } : EMPTY_RULE_FORM);
  }, [rule]);

  async function loadAdjustments() {
    if (!selectedBranchId) {
      setLoading(false);
      setRule(null);
      setEmployees([]);
      if (isAdmin) setMessage({ type: 'error', text: 'Vui lòng chọn cơ sở.' });
      return;
    }

    setLoading(true);
    setMessage(null);
    try {
      const data = await getSalaryRuleAdjustments(period.month, period.year, selectedBranchId);
      setRule(data.rule || null);
      setEmployees(Array.isArray(data.employees) ? data.employees : []);
    } catch (err) {
      setMessage({ type: 'error', text: err.response?.data?.message || 'Không tải được danh sách thưởng phạt.' });
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => {
    loadAdjustments();
  }, [period.month, period.year, selectedBranchId]);

  const filteredEmployees = useMemo(() => {
    const employeesWithAdjustments = employees.filter((employee) =>
      Number(employee.calculatedBonus || 0) !== 0
      || Number(employee.calculatedPenalty || 0) !== 0
      || Number(employee.currentBonus || 0) !== 0
      || Number(employee.currentPenalty || 0) !== 0
    );
    const normalizedQuery = query.trim().toLowerCase();
    if (!normalizedQuery) return employeesWithAdjustments;

    return employeesWithAdjustments.filter((employee) => [
      employee.fullName,
      employee.email,
      employee.phoneNumber,
      employee.roleName,
    ].some((value) => String(value || '').toLowerCase().includes(normalizedQuery)));
  }, [employees, query]);

  const summary = useMemo(() => {
    return filteredEmployees.reduce(
      (total, employee) => ({
        bonus: total.bonus + Number(employee.calculatedBonus || 0),
        penalty: total.penalty + Number(employee.calculatedPenalty || 0),
        late: total.late + Number(employee.lateCount || 0),
        absent: total.absent + Number(employee.absentCount || 0),
      }),
      { bonus: 0, penalty: 0, late: 0, absent: 0 }
    );
  }, [filteredEmployees]);

  function handleMonthChange(event) {
    const [year, month] = event.target.value.split('-').map(Number);
    setPeriod({ month, year });
  }

  function handleRuleFormChange(event) {
    const { name, value } = event.target;
    setRuleForm((form) => ({ ...form, [name]: value }));
  }

  async function handleRuleSubmit(event) {
    event.preventDefault();
    if (!selectedBranchId) {
      setMessage({ type: 'error', text: 'Vui lòng chọn cơ sở.' });
      return;
    }

    const payload = {
      branchId: Number(selectedBranchId),
      bonusThresholdDays: Number(ruleForm.bonusThresholdDays || 0),
      bonusAmount: Number(ruleForm.bonusAmount || 0),
      latePenalty: Number(ruleForm.latePenalty || 0),
      absentPenalty: Number(ruleForm.absentPenalty || 0),
      weekendMultiplier: Number(ruleForm.weekendMultiplier || 1),
    };

    if (payload.bonusThresholdDays < 0 || payload.bonusAmount < 0 || payload.latePenalty < 0 || payload.absentPenalty < 0) {
      setMessage({ type: 'error', text: 'Các giá trị thưởng/phạt không được âm.' });
      return;
    }

    if (payload.weekendMultiplier <= 0) {
      setMessage({ type: 'error', text: 'Hệ số cuối tuần phải lớn hơn 0.' });
      return;
    }

    setSavingRule(true);
    setMessage(null);
    try {
      const updatedRule = await updateSalaryRule(payload);
      setRule(updatedRule);
      await loadAdjustments();
      setMessage({ type: 'success', text: 'Đã lưu salary rule cho cơ sở.' });
    } catch (err) {
      setMessage({ type: 'error', text: err.response?.data?.message || 'Không thể lưu salary rule.' });
    } finally {
      setSavingRule(false);
    }
  }

  function openManualAdjustment(employee) {
    setManualTarget(employee);
    setManualForm({ bonusAmount: '', penaltyAmount: '', reason: '' });
    setMessage(null);
  }

  async function openHistory(employee) {
    setHistoryTarget(employee);
    setHistory([]);
    setHistoryLoading(true);
    try {
      const data = await getSalaryAdjustmentHistory(employee.userId, period.month, period.year);
      setHistory(Array.isArray(data) ? data : []);
    } catch (err) {
      setMessage({ type: 'error', text: err.response?.data?.message || 'Không tải được lịch sử thưởng/phạt.' });
      setHistoryTarget(null);
    } finally {
      setHistoryLoading(false);
    }
  }

  async function handleManualSubmit(event) {
    event.preventDefault();
    if (!manualTarget) return;

    const bonusAmount = Number(manualForm.bonusAmount || 0);
    const penaltyAmount = Number(manualForm.penaltyAmount || 0);
    if (bonusAmount < 0 || penaltyAmount < 0) {
      setMessage({ type: 'error', text: 'Số tiền thưởng/phạt không được âm.' });
      return;
    }
    if (bonusAmount === 0 && penaltyAmount === 0) {
      setMessage({ type: 'error', text: 'Vui lòng nhập số tiền thưởng hoặc phạt.' });
      return;
    }
    if (!manualForm.reason.trim()) {
      setMessage({ type: 'error', text: 'Vui lòng nhập lý do thưởng/phạt.' });
      return;
    }

    setSavingUserId(manualTarget.userId);
    setMessage(null);
    try {
      const updated = await addManualSalaryAdjustment({
        userId: manualTarget.userId,
        month: period.month,
        year: period.year,
        bonusAmount,
        penaltyAmount,
        reason: manualForm.reason.trim(),
      }, selectedBranchId);
      setEmployees((items) => items.map((item) => (item.userId === updated.userId ? updated : item)));
      setManualTarget(null);
      setMessage({ type: 'success', text: `Đã cộng thưởng/phạt thủ công cho ${updated.fullName || updated.email || 'nhân viên'}.` });
    } catch (err) {
      setMessage({ type: 'error', text: err.response?.data?.message || 'Không thể cộng thưởng/phạt thủ công.' });
    } finally {
      setSavingUserId(null);
    }
  }

  const monthOptions = [
    ...(currentPeriod.month === 1
      ? [{ month: 12, year: currentPeriod.year - 1 }]
      : []),
    ...Array.from({ length: 12 }, (_, index) => ({
      month: index + 1,
      year: currentPeriod.year,
    })),
  ];
  const periodValue = `${period.year}-${String(period.month).padStart(2, '0')}`;

  return (
    <div className="sd-salary-admin-page">
      <div className="sd-stat-grid sd-salary-admin-stats">
        <SalaryRuleMetric label="Tổng thưởng theo rule" value={formatMoney(summary.bonus)} />
        <SalaryRuleMetric label="Tổng phạt theo rule" value={formatMoney(summary.penalty)} />
        <SalaryRuleMetric label="Số lần đi trễ" value={formatNumber(summary.late)} />
        <SalaryRuleMetric label="Số ca vắng" value={formatNumber(summary.absent)} />
      </div>

      <div className="sd-card">
        <div className="sd-card-header">
          <p className="sd-eyebrow">Salary rule</p>
          <h2>Quy định thưởng phạt cơ sở</h2>
        </div>
        {isAdmin && (
          <div className="sd-users-toolbar">
            <div className="sd-field sd-salary-filter">
              <label>Cơ sở</label>
              <select value={selectedBranchId} onChange={(event) => setSelectedBranchId(event.target.value)}>
                <option value="">-- Chọn cơ sở --</option>
                {branches.map((branch) => (
                  <option key={branch.id} value={branch.id}>{branch.name || branch.branchName}</option>
                ))}
              </select>
            </div>
          </div>
        )}
        {rule ? (
          <div className="sd-salary-summary">
            <SalaryRuleMetric label="Ngày công đạt thưởng" value={`${rule.bonusThresholdDays} ngày`} />
            <SalaryRuleMetric label="Mức thưởng" value={formatMoney(rule.bonusAmount)} />
            <SalaryRuleMetric label="Phạt đi trễ" value={formatMoney(rule.latePenalty)} />
            <SalaryRuleMetric label="Phạt vắng ca" value={formatMoney(rule.absentPenalty)} />
          </div>
        ) : (
          <p className="sd-status sd-status-error">Cơ sở này chưa có salary rule.</p>
        )}
        {isAdmin && selectedBranchId && (
          <form className="sd-modal-grid" onSubmit={handleRuleSubmit}>
            <div className="sd-field">
              <label>Ngày công đạt thưởng</label>
              <input min="0" name="bonusThresholdDays" onChange={handleRuleFormChange} type="number" value={ruleForm.bonusThresholdDays} />
            </div>
            <div className="sd-field">
              <label>Mức thưởng</label>
              <input min="0" name="bonusAmount" onChange={handleRuleFormChange} type="number" value={ruleForm.bonusAmount} />
            </div>
            <div className="sd-field">
              <label>Phạt đi trễ</label>
              <input min="0" name="latePenalty" onChange={handleRuleFormChange} type="number" value={ruleForm.latePenalty} />
            </div>
            <div className="sd-field">
              <label>Phạt vắng ca</label>
              <input min="0" name="absentPenalty" onChange={handleRuleFormChange} type="number" value={ruleForm.absentPenalty} />
            </div>
            <div className="sd-field">
              <label>Hệ số cuối tuần</label>
              <input min="0.1" name="weekendMultiplier" onChange={handleRuleFormChange} step="0.1" type="number" value={ruleForm.weekendMultiplier} />
            </div>
            <div className="sd-field">
              <label>&nbsp;</label>
              <button className="sd-btn-primary" disabled={savingRule} type="submit">
                {savingRule ? 'Đang lưu...' : 'Lưu salary rule'}
              </button>
            </div>
          </form>
        )}
      </div>

      <div className="sd-users-toolbar">
        <div className="sd-users-toolbar-left">
          <div className="sd-field sd-salary-filter">
            <label>Xem tháng</label>
            <div className="sd-period-selects">
              <select aria-label="Chọn tháng" onChange={handleMonthChange} value={periodValue}>
                {monthOptions.map((option) => (
                  <option
                    key={`${option.year}-${option.month}`}
                    value={`${option.year}-${String(option.month).padStart(2, '0')}`}
                  >
                    Tháng {option.month}/{option.year}
                  </option>
                ))}
              </select>
            </div>
          </div>
          <div className="sd-search-wrap">
            <span className="sd-search-icon">⌕</span>
            <input
              className="sd-input-search"
              onChange={(event) => setQuery(event.target.value)}
              placeholder="Tìm nhân viên, email, SĐT..."
              value={query}
            />
            {query && <button className="sd-search-clear" onClick={() => setQuery('')} type="button">✕</button>}
          </div>
        </div>
        <button className="sd-btn-ghost" onClick={loadAdjustments} type="button">Làm mới</button>
      </div>

      {message && <p className={`sd-status sd-status-${message.type}`}>{message.text}</p>}

      <div className="sd-table-wrap">
        <table className="sd-table">
          <thead>
            <tr>
              <th>Nhân viên</th>
              <th>Ngày làm</th>
              <th>Trễ</th>
              <th>Vắng</th>
              <th>Thưởng</th>
              <th>Phạt</th>
              <th>Lương hiện tại</th>
              {!isAdmin && <th>Thao tác</th>}
            </tr>
          </thead>
          <tbody>
            {loading ? (
              <tr><td colSpan={isAdmin ? 7 : 8} className="sd-td-empty">Đang tải danh sách thưởng phạt...</td></tr>
            ) : filteredEmployees.length === 0 ? (
              <tr>
                <td colSpan={isAdmin ? 7 : 8} className="sd-td-empty">
                  {query.trim()
                    ? 'Không có nhân viên phù hợp.'
                    : 'Không có nhân viên phát sinh thưởng/phạt trong tháng này.'}
                </td>
              </tr>
            ) : filteredEmployees.map((employee) => {
              const salaryStatus = (employee.status || '').toUpperCase();
              const isLocked = salaryStatus === 'FINALIZED' || salaryStatus === 'ADMIN_FINALIZED' || salaryStatus === 'PAID';
              const isSaving = savingUserId === employee.userId;

              return (
                <tr
                  className="sd-tr"
                  key={employee.userId}
                  onClick={() => setEmployeeDetailTarget(employee)}
                  onKeyDown={(event) => {
                    if (event.currentTarget === event.target && (event.key === 'Enter' || event.key === ' ')) {
                      event.preventDefault();
                      setEmployeeDetailTarget(employee);
                    }
                  }}
                  role="button"
                  style={{ cursor: 'pointer' }}
                  tabIndex={0}
                >
                  <td>
                    <strong>{employee.fullName || employee.email || employee.phoneNumber}</strong>
                    <span className="sd-subline">{employee.roleName || 'Nhân viên'}</span>
                  </td>
                  <td>{employee.workedDays}</td>
                  <td>
                    {employee.lateCount > 0 ? (
                      <button
                        className="sd-attendance-detail-button"
                        onClick={(event) => {
                          event.stopPropagation();
                          setAttendanceTarget({ employee, type: 'late' });
                        }}
                        type="button"
                      >
                        {employee.lateCount} · Xem ngày
                      </button>
                    ) : 0}
                  </td>
                  <td>
                    {employee.absentCount > 0 ? (
                      <button
                        className="sd-attendance-detail-button sd-attendance-detail-button--absent"
                        onClick={(event) => {
                          event.stopPropagation();
                          setAttendanceTarget({ employee, type: 'absent' });
                        }}
                        type="button"
                      >
                        {employee.absentCount} · Xem ngày
                      </button>
                    ) : 0}
                  </td>
                  <td>
                    <strong>{formatMoney(employee.calculatedBonus)}</strong>
                    <span className="sd-subline">Tổng đã cộng vào lương: {formatMoney(employee.currentBonus)}</span>
                  </td>
                  <td>
                    <strong>{formatMoney(employee.calculatedPenalty)}</strong>
                    <span className="sd-subline">Tổng đã trừ vào lương: {formatMoney(employee.currentPenalty)}</span>
                  </td>
                  <td>
                    <strong>{formatMoney(employee.totalSalary)}</strong>
                    <span className="sd-subline">{formatNumber(employee.totalHours)} giờ</span>
                  </td>
                  {!isAdmin && (
                    <td>
                      <div className="sd-salary-actions">
                        <button className="sd-btn-ghost" onClick={(event) => { event.stopPropagation(); openHistory(employee); }} type="button">
                          Lịch sử
                        </button>
                        <button
                          className="sd-btn-primary"
                          disabled={isLocked || isSaving}
                          onClick={(event) => { event.stopPropagation(); openManualAdjustment(employee); }}
                          type="button"
                        >
                          {isLocked ? (salaryStatus === 'PAID' ? 'Đã trả' : 'Đã chốt') : isSaving ? 'Đang lưu...' : 'Thưởng phạt'}
                        </button>
                      </div>
                    </td>
                  )}
                </tr>
              );
            })}
          </tbody>
        </table>
      </div>

      {employeeDetailTarget && (() => {
        const issueDetails = [
          ...(employeeDetailTarget.lateDetails || []).map((detail) => ({ ...detail, issueType: 'Đi trễ' })),
          ...(employeeDetailTarget.absentDetails || []).map((detail) => ({ ...detail, issueType: 'Vắng ca' })),
        ];

        return (
          <div className="sd-overlay" onClick={() => setEmployeeDetailTarget(null)}>
            <div className="sd-modal sd-modal--wide" onClick={(event) => event.stopPropagation()}>
              <div className="sd-modal-header">
                <div>
                  <p className="sd-eyebrow">Tháng {period.month}/{period.year}</p>
                  <h2>Chi tiết thưởng phạt - {employeeDetailTarget.fullName || employeeDetailTarget.email}</h2>
                </div>
                <button onClick={() => setEmployeeDetailTarget(null)} type="button">✕</button>
              </div>
              <div className="sd-modal-body">
                <dl className="sd-dl">
                  <div className="sd-info-row"><dt>Ngày làm</dt><dd>{employeeDetailTarget.workedDays} ngày</dd></div>
                  <div className="sd-info-row"><dt>Đi trễ</dt><dd>{employeeDetailTarget.lateCount} lần</dd></div>
                  <div className="sd-info-row"><dt>Vắng ca</dt><dd>{employeeDetailTarget.absentCount} lần</dd></div>
                  <div className="sd-info-row"><dt>Thưởng theo rule</dt><dd>{formatMoney(employeeDetailTarget.calculatedBonus)}</dd></div>
                  <div className="sd-info-row"><dt>Phạt theo rule</dt><dd>{formatMoney(employeeDetailTarget.calculatedPenalty)}</dd></div>
                  <div className="sd-info-row"><dt>Tổng thưởng hiện tại</dt><dd>{formatMoney(employeeDetailTarget.currentBonus)}</dd></div>
                  <div className="sd-info-row"><dt>Tổng phạt hiện tại</dt><dd>{formatMoney(employeeDetailTarget.currentPenalty)}</dd></div>
                  <div className="sd-info-row"><dt>Lương hiện tại</dt><dd>{formatMoney(employeeDetailTarget.totalSalary)}</dd></div>
                </dl>

                <h3 className="sd-salary-detail-title">Các ngày phát sinh vi phạm</h3>
                {issueDetails.length === 0 ? (
                  <p className="sd-salary-empty">Không có ngày đi trễ hoặc vắng ca trong kỳ này.</p>
                ) : (
                  <div className="sd-table-wrap">
                    <table className="sd-table">
                      <thead><tr><th>Loại</th><th>Ngày</th><th>Ca làm</th><th>Giờ ca</th><th>Giờ vào thực tế</th></tr></thead>
                      <tbody>
                        {issueDetails.map((detail, index) => (
                          <tr key={`${detail.issueType}-${detail.workDate}-${detail.scheduledTime}-${index}`}>
                            <td><strong>{detail.issueType}</strong></td>
                            <td>{formatWorkDate(detail.workDate)}</td>
                            <td>{detail.shiftName || '—'}</td>
                            <td>{detail.scheduledTime || '—'}</td>
                            <td>{detail.actualCheckInTime || 'Không có chấm công hoàn tất'}</td>
                          </tr>
                        ))}
                      </tbody>
                    </table>
                  </div>
                )}
              </div>
            </div>
          </div>
        );
      })()}

      {attendanceTarget && (() => {
        const isLate = attendanceTarget.type === 'late';
        const details = isLate
          ? (attendanceTarget.employee.lateDetails || [])
          : (attendanceTarget.employee.absentDetails || []);

        return (
          <div className="sd-overlay" onClick={() => setAttendanceTarget(null)}>
            <div className="sd-modal sd-modal--wide" onClick={(event) => event.stopPropagation()}>
              <div className="sd-modal-header">
                <div>
                  <p className="sd-eyebrow">Tháng {period.month}/{period.year}</p>
                  <h2>
                    {isLate ? 'Chi tiết đi trễ' : 'Chi tiết vắng ca'} - {' '}
                    {attendanceTarget.employee.fullName || attendanceTarget.employee.email}
                  </h2>
                </div>
                <button onClick={() => setAttendanceTarget(null)} type="button">✕</button>
              </div>
              <div className="sd-modal-body">
                {details.length === 0 ? (
                  <p className="sd-salary-empty">Không có dữ liệu chi tiết trong kỳ này.</p>
                ) : (
                  <div className="sd-table-wrap">
                    <table className="sd-table">
                      <thead>
                        <tr>
                          <th>Ngày</th>
                          <th>Ca làm</th>
                          <th>Giờ ca</th>
                          <th>{isLate ? 'Giờ vào thực tế' : 'Trạng thái chấm công'}</th>
                        </tr>
                      </thead>
                      <tbody>
                        {details.map((detail, index) => (
                          <tr key={`${detail.workDate}-${detail.scheduledTime}-${index}`}>
                            <td><strong>{formatWorkDate(detail.workDate)}</strong></td>
                            <td>{detail.shiftName || '—'}</td>
                            <td>{detail.scheduledTime || '—'}</td>
                            <td>
                              {isLate
                                ? (detail.actualCheckInTime || 'Chưa ghi nhận')
                                : (detail.actualCheckInTime
                                  ? `Đã vào lúc ${detail.actualCheckInTime}, chưa hoàn tất ca`
                                  : 'Không có chấm công hoàn tất')}
                            </td>
                          </tr>
                        ))}
                      </tbody>
                    </table>
                  </div>
                )}
              </div>
            </div>
          </div>
        );
      })()}

      {manualTarget && (
        <div className="sd-overlay" onClick={() => setManualTarget(null)}>
          <div className="sd-modal" onClick={(event) => event.stopPropagation()}>
            <div className="sd-modal-header">
              <h2>Thêm thưởng/phạt</h2>
              <button onClick={() => setManualTarget(null)} type="button">✕</button>
            </div>
            <form onSubmit={handleManualSubmit}>
              <div className="sd-modal-body">
                <div className="sd-info-hero">
                  <div className="sd-info-avatar">{String(manualTarget.fullName || manualTarget.email || 'NV').slice(0, 2).toUpperCase()}</div>
                  <div>
                    <h3>{manualTarget.fullName || manualTarget.email || manualTarget.phoneNumber}</h3>
                    <span className="sd-role-badge">Tháng {period.month}/{period.year}</span>
                  </div>
                </div>
                <div className="sd-modal-grid">
                  <div className="sd-field">
                    <label>Thưởng thêm</label>
                    <input
                      min="0"
                      name="bonusAmount"
                      onChange={(event) => setManualForm((form) => ({ ...form, bonusAmount: event.target.value }))}
                      placeholder="VD: 100000"
                      type="number"
                      value={manualForm.bonusAmount}
                    />
                  </div>
                  <div className="sd-field">
                    <label>Phạt thêm</label>
                    <input
                      min="0"
                      name="penaltyAmount"
                      onChange={(event) => setManualForm((form) => ({ ...form, penaltyAmount: event.target.value }))}
                      placeholder="VD: 50000"
                      type="number"
                      value={manualForm.penaltyAmount}
                    />
                  </div>
                </div>
                <div className="sd-field">
                  <label>Lý do thưởng/phạt</label>
                  <textarea
                    maxLength="500"
                    onChange={(event) => setManualForm((form) => ({ ...form, reason: event.target.value }))}
                    placeholder="Nhập lý do cụ thể để nhân viên có thể tra cứu"
                    required
                    rows="3"
                    value={manualForm.reason}
                  />
                </div>
                <dl className="sd-dl">
                  <div className="sd-info-row"><dt>Thưởng hiện tại</dt><dd>{formatMoney(manualTarget.currentBonus)}</dd></div>
                  <div className="sd-info-row"><dt>Phạt hiện tại</dt><dd>{formatMoney(manualTarget.currentPenalty)}</dd></div>
                  <div className="sd-info-row"><dt>Lương hiện tại</dt><dd>{formatMoney(manualTarget.totalSalary)}</dd></div>
                </dl>
              </div>
              <div className="sd-modal-footer">
                <button className="sd-btn-ghost" onClick={() => setManualTarget(null)} type="button">Hủy</button>
                <button className="sd-btn-primary" disabled={savingUserId === manualTarget.userId} type="submit">
                  {savingUserId === manualTarget.userId ? 'Đang lưu...' : 'Cộng vào lương'}
                </button>
              </div>
            </form>
          </div>
        </div>
      )}

      {historyTarget && (
        <div className="sd-overlay" onClick={() => setHistoryTarget(null)}>
          <div className="sd-modal sd-modal--wide" onClick={(event) => event.stopPropagation()}>
            <div className="sd-modal-header">
              <div>
                <p className="sd-eyebrow">Tháng {period.month}/{period.year}</p>
                <h2>Lịch sử thưởng/phạt - {historyTarget.fullName || historyTarget.email}</h2>
              </div>
              <button onClick={() => setHistoryTarget(null)} type="button">✕</button>
            </div>
            <div className="sd-modal-body">
              {historyLoading ? (
                <p className="sd-salary-empty">Đang tải lịch sử...</p>
              ) : history.length === 0 ? (
                <p className="sd-salary-empty">Chưa có lần thưởng/phạt thủ công nào trong kỳ này.</p>
              ) : (
                <div className="sd-table-wrap">
                  <table className="sd-table sd-adjustment-history-table">
                    <thead><tr><th>Thời gian</th><th>Thưởng</th><th>Phạt</th><th>Lý do</th><th>Người tạo</th></tr></thead>
                    <tbody>
                      {history.map((item) => (
                        <tr key={item.id}>
                          <td>{new Date(item.createdAt).toLocaleString('vi-VN')}</td>
                          <td>{formatMoney(item.bonusAmount)}</td>
                          <td>{formatMoney(item.penaltyAmount)}</td>
                          <td>{item.reason}</td>
                          <td>{item.createdByName || 'Quản lý'}</td>
                        </tr>
                      ))}
                    </tbody>
                  </table>
                </div>
              )}
            </div>
          </div>
        </div>
      )}
    </div>
  );
}
