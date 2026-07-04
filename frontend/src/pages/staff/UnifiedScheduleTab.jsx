import { useState, useEffect } from 'react';
import axios from 'axios';
import { getAllPeriods } from '../../api/PeriodApi';
import { getAllShifts } from '../../api/ShiftApi';

const DAY_NAMES = ['Chủ nhật', 'Thứ 2', 'Thứ 3', 'Thứ 4', 'Thứ 5', 'Thứ 6', 'Thứ 7'];

function formatDate(value) {
  if (!value) return 'Chưa có';
  return new Intl.DateTimeFormat('vi-VN').format(new Date(value));
}

// ==========================================
// 👉 COMPONENT MỚI: MÀN HÌNH GỘP (LỊCH & ĐĂNG KÝ)
// ==========================================
export function UnifiedScheduleTab({ user }) {
  const [periods, setPeriods] = useState([]);
  const [selectedPeriodId, setSelectedPeriodId] = useState('');
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    async function init() {
      try {
        const allPeriods = await getAllPeriods();
        const branchPeriods = allPeriods
          .filter(p => String(p.branchId) === String(user.branchId))
          .filter(p => {
            const st = p.status?.trim().toLowerCase();
            return st === 'mở' || st === 'open' || st === 'published';
          })
          .sort((a, b) => new Date(b.startDate) - new Date(a.startDate));

        setPeriods(branchPeriods);
        if (branchPeriods.length > 0) {
          setSelectedPeriodId(branchPeriods[0].id.toString());
        }
      } catch (e) {
        console.error("Lỗi lấy danh sách đợt:", e);
      } finally {
        setLoading(false);
      }
    }
    init();
  }, [user.branchId]);

  if (loading) return <div className="sd-card"><p>Đang tải dữ liệu...</p></div>;

  if (periods.length === 0) {
    return (
      <div className="sd-card">
        <div className="sd-empty-state" style={{ padding: '40px 20px' }}>
          <span className="sd-empty-icon">🗓️</span>
          <h3 style={{ color: '#1e293b', marginTop: 10 }}>Chưa có dữ liệu lịch làm</h3>
          <p>Hiện tại cơ sở của bạn chưa có lịch làm chính thức cũng như đợt đăng ký ca nào được mở.</p>
        </div>
      </div>
    );
  }

  const selectedPeriod = periods.find(p => p.id.toString() === selectedPeriodId);
  const isPublished = selectedPeriod?.status === 'PUBLISHED';

  return (
    <div className="sd-card" style={{ padding: '20px 0' }}>
      <div style={{ padding: '0 20px 16px', display: 'flex', gap: 12, alignItems: 'center', borderBottom: '1px solid #f1f5f9', marginBottom: 16 }}>
        <span style={{ fontSize: 14, fontWeight: 600, color: '#475569', whiteSpace: 'nowrap' }}>Chọn tuần:</span>
        <select
          className="sd-input-search"
          style={{ width: '100%', maxWidth: 400 }}
          value={selectedPeriodId}
          onChange={(e) => setSelectedPeriodId(e.target.value)}
        >
          {periods.map(p => {
            const st = p.status === 'PUBLISHED' ? '(Đã chốt lịch)' : '(Đang mở đăng ký)';
            return (
              <option key={p.id} value={p.id}>
                Từ {formatDate(p.startDate)} đến {formatDate(p.endDate)} {st}
              </option>
            );
          })}
        </select>
      </div>

      <div style={{ padding: '0 20px' }}>
        {isPublished
          ? <PublishedScheduleView period={selectedPeriod} user={user} />
          : <RegistrationView period={selectedPeriod} user={user} />
        }
      </div>
    </div>
  );
}

// ==========================================
// 2A. CHẾ ĐỘ: XEM LỊCH ĐÃ CHỐT
// ==========================================
function PublishedScheduleView({ period, user }) {
  const [registrations, setRegistrations] = useState([]);
  const [shifts, setShifts] = useState([]);
  const [dates, setDates] = useState([]);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    async function loadBoard() {
      setLoading(true);
      try {
        const [regRes, shiftRes] = await Promise.all([
          axios.get(`/api/StaffRegistration/period/${period.id}`),
          getAllShifts()
        ]);

        const approvedRegs = (regRes.data || []).filter(r => r.status === 'Đã Duyệt');
        setRegistrations(approvedRegs);
        setShifts(shiftRes.filter(s => String(s.branchId) === String(user.branchId)));

        const dArray = [];
        let curr = new Date(period.startDate);
        const end = new Date(period.endDate);
        while (curr <= end) {
          dArray.push(new Date(curr));
          curr.setDate(curr.getDate() + 1);
        }
        setDates(dArray);
      } catch (e) { console.error("Lỗi:", e); } finally { setLoading(false); }
    }
    loadBoard();
  }, [period.id, user.branchId]);

  function toDateString(dateObj) {
    const offset = dateObj.getTimezoneOffset();
    const d = new Date(dateObj.getTime() - (offset * 60 * 1000));
    return d.toISOString().split('T')[0];
  }

  const boardMatrix = {};
  dates.forEach(dObj => {
    const dStr = toDateString(dObj);
    boardMatrix[dStr] = {};
    shifts.forEach(s => {
      boardMatrix[dStr][s.id] = registrations.filter(r => r.workDate.slice(0, 10) === dStr && r.shiftId === s.id);
    });
  });

  if (loading) return <p>Đang tải bảng lịch làm việc...</p>;

  return (
    <>
      <div style={{ marginBottom: 16 }}>
        <h2 style={{ color: '#1d4ed8', margin: '0 0 4px' }}>Lịch làm việc chính thức</h2>
      </div>

      <div className="sd-board-wrap" style={{ borderRadius: 12 }}>
        <table className="sd-schedule-board">
          <thead>
            <tr>
              <th style={{ width: 90 }}>NGÀY</th>
              {shifts.map(s => (
                <th key={s.id}>
                  {s.shiftName}<br />
                  <span style={{ fontWeight: 500, fontSize: 11 }}>{s.startTime?.slice(0, 5)} - {s.endTime?.slice(0, 5)}</span>
                </th>
              ))}
            </tr>
          </thead>
          <tbody>
            {dates.map((dateObj) => {
              const dStr = toDateString(dateObj);
              const dayOfWeek = DAY_NAMES[dateObj.getDay()];
              const shortDate = `${dateObj.getDate()}/${dateObj.getMonth() + 1}`;

              return (
                <tr key={dStr}>
                  <td className="sd-board-date-col">
                    <strong>{dayOfWeek}</strong>
                    <small>{shortDate}</small>
                  </td>

                  {shifts.map(shift => {
                    const cellRegs = boardMatrix[dStr][shift.id] || [];
                    const isWeekend = dayOfWeek === 'Thứ 7' || dayOfWeek === 'Chủ nhật';
                    const isShiftClosed = isWeekend && cellRegs.length === 0;

                    return (
                      <td key={shift.id}>
                        {!isShiftClosed ? (
                          <div className="sd-reg-card" style={{ background: '#ffedd5', borderColor: '#fdba74', color: '#9a3412' }}>
                            <span className="sd-reg-name"> Quản lý ca</span>
                          </div>
                        ) : (
                          <div style={{ textAlign: 'center', padding: '16px 0', color: '#cbd5e1', fontSize: 12, fontWeight: 600 }}>
                            KHÔNG CÓ CA LÀM
                          </div>
                        )}

                        {cellRegs.map(r => {
                          const staffName = r.user?.fullName || r.user?.username || 'Nhân viên';
                          const isMe = r.userId === user.id;
                          return (
                            <div
                              key={r.id}
                              className="sd-reg-card"
                              style={{
                                background: isMe ? '#dbeafe' : '#f8fafc',
                                borderColor: isMe ? '#93c5fd' : '#e2e8f0',
                                color: isMe ? '#1e3a8a' : '#475569',
                                fontWeight: isMe ? 700 : 500
                              }}
                            >
                              <span className="sd-reg-name" title={staffName}>{isMe ? ' ' + staffName : staffName}</span>
                            </div>
                          );
                        })}
                      </td>
                    );
                  })}
                </tr>
              );
            })}
          </tbody>
        </table>
      </div>
    </>
  );
}

// ==========================================
// 2B. CHẾ ĐỘ: ĐĂNG KÝ CA LÀM
// ==========================================
function RegistrationView({ period, user }) {
  const [shifts, setShifts] = useState([]);
  const [shiftConfigs, setShiftConfigs] = useState([]);
  const [allRegistrations, setAllRegistrations] = useState([]);
  const [dates, setDates] = useState([]);
  const [registered, setRegistered] = useState({});
  const [dbRegistrations, setDbRegistrations] = useState({});
  const [capacityMessage, setCapacityMessage] = useState('');
  const [saved, setSaved] = useState(false);
  const [saving, setSaving] = useState(false);
  const [loading, setLoading] = useState(true);
  const isReviewing = period.status === 'REVIEWING' || period.status === 'Đang duyệt';

  useEffect(() => {
    async function loadData() {
      setLoading(true);
      try {
        const [allShifts, configRes, periodRegRes] = await Promise.all([
          getAllShifts(),
          axios.get('/api/BranchShiftConfig'),
          axios.get(`/api/StaffRegistration/period/${period.id}`),
        ]);
        const branchShifts = allShifts.filter((s) => String(s.branchId) === String(user.branchId));
        const branchShiftIds = new Set(branchShifts.map((s) => s.id));
        setShifts(branchShifts);
        setShiftConfigs((configRes.data || []).filter((cfg) => branchShiftIds.has(cfg.shiftId)));
        setAllRegistrations(periodRegRes.data || []);

        const dArray = [];
        let curr = new Date(period.startDate);
        const end = new Date(period.endDate);
        while (curr <= end) {
          dArray.push(new Date(curr));
          curr.setDate(curr.getDate() + 1);
        }
        setDates(dArray);

        const regRes = await axios.get(`/api/StaffRegistration/my-schedule/${user.id}/${period.id}`);
        const myRegs = regRes.data || [];

        const dbMap = {};
        const initRegs = {};

        myRegs.forEach(r => {
          const dStr = r.workDate.slice(0, 10);
          if (!dbMap[dStr]) { dbMap[dStr] = {}; initRegs[dStr] = {}; }
          dbMap[dStr][r.shiftId] = { id: r.id, status: r.status };
          initRegs[dStr][r.shiftId] = true;
        });

        setDbRegistrations(dbMap);
        setRegistered(initRegs);
      } catch (err) { console.error('Lỗi:', err); } finally { setLoading(false); }
    }
    loadData();
  }, [period.id, user.id, user.branchId]);

  function toDateString(dateObj) {
    const offset = dateObj.getTimezoneOffset();
    const d = new Date(dateObj.getTime() - (offset * 60 * 1000));
    return d.toISOString().split('T')[0];
  }

  function getMaxStaffForShiftDate(shiftId, dateObj) {
    const dayName = dateObj.toLocaleDateString('en-US', { weekday: 'long' });
    const config = shiftConfigs.find((cfg) => cfg.shiftId === shiftId && String(cfg.dayOfWeek).toLowerCase() === dayName.toLowerCase());
    const shift = shifts.find((item) => item.id === shiftId);
    return Number(config?.maxStaff ?? shift?.maxStaff ?? 0);
  }

  function isRejectedStatus(status = '') {
    const normalized = String(status).toLowerCase();
    return normalized.includes('từ chối') || normalized.includes('tá»« chá»‘i');
  }

  function getRegisteredCount(dateStr, shiftId) {
    return allRegistrations.filter((item) =>
      item.workDate?.slice(0, 10) === dateStr &&
      item.shiftId === shiftId &&
      !isRejectedStatus(item.status)
    ).length;
  }

  function isShiftFull(dateStr, shiftId, dateObj) {
    const maxStaff = getMaxStaffForShiftDate(shiftId, dateObj);
    if (maxStaff <= 0) return true;
    return getRegisteredCount(dateStr, shiftId) >= maxStaff;
  }

  function toggle(dateStr, shiftId, dateObj) {
    const dbItem = dbRegistrations[dateStr]?.[shiftId];
    if (dbItem && dbItem.status !== "Chờ Duyệt") return;

    if (!dbItem && isShiftFull(dateStr, shiftId, dateObj)) {
      setCapacityMessage('Ca đã đủ người, bạn không thể đăng ký vào ca này.');
      return;
    }

    setCapacityMessage('');
    setSaved(false);
    setRegistered((prev) => {
      const dayRegs = prev[dateStr] || {};
      return { ...prev, [dateStr]: { ...dayRegs, [shiftId]: !dayRegs[shiftId] } };
    });
  }

  function getChanges() {
    const adds = [];
    const deletes = [];

    Object.entries(registered).forEach(([dStr, shiftsInfo]) => {
      Object.entries(shiftsInfo).forEach(([sId, isSelected]) => {
        if (isSelected && !dbRegistrations[dStr]?.[sId]) {
          adds.push({ userId: user.id, periodId: period.id, shiftId: parseInt(sId), workDate: dStr, status: "Chờ Duyệt" });
        }
      });
    });

    Object.entries(dbRegistrations).forEach(([dStr, shiftsInfo]) => {
      Object.entries(shiftsInfo).forEach(([sId, dbItem]) => {
        const isSelectedNow = registered[dStr]?.[sId];
        if (!isSelectedNow && dbItem.status === "Chờ Duyệt") deletes.push(dbItem.id);
      });
    });

    return { adds, deletes };
  }

  async function handleSave() {
    const { adds, deletes } = getChanges();
    if (adds.length === 0 && deletes.length === 0) return alert("Không có thay đổi nào để lưu!");
    setSaving(true);

    try {
      const apiCalls = [
        ...adds.map(payload => axios.post('/api/StaffRegistration', payload)),
        ...deletes.map(regId => axios.delete(`/api/StaffRegistration/${regId}/user/${user.id}`))
      ];
      await Promise.all(apiCalls);

      const [regRes, periodRegRes] = await Promise.all([
        axios.get(`/api/StaffRegistration/my-schedule/${user.id}/${period.id}`),
        axios.get(`/api/StaffRegistration/period/${period.id}`),
      ]);
      setAllRegistrations(periodRegRes.data || []);
      const dbMap = {}; const initRegs = {};
      (regRes.data || []).forEach(r => {
        const dStr = r.workDate.slice(0, 10);
        if (!dbMap[dStr]) { dbMap[dStr] = {}; initRegs[dStr] = {}; }
        dbMap[dStr][r.shiftId] = { id: r.id, status: r.status };
        initRegs[dStr][r.shiftId] = true;
      });
      setDbRegistrations(dbMap);
      setRegistered(initRegs);
      setSaved(true);

    } catch (err) { alert("❌ Lỗi: " + (err.response?.data?.message || 'Có lỗi xảy ra!')); }
    finally { setSaving(false); }
  }

  function handleReset() {
    const resetRegs = {};
    Object.keys(dbRegistrations).forEach(d => {
      resetRegs[d] = {};
      Object.keys(dbRegistrations[d]).forEach(sId => { resetRegs[d][sId] = true; });
    });
    setRegistered(resetRegs);
    setSaved(false);
  }

  if (loading) return <p>Đang tải form đăng ký...</p>;

  const { adds, deletes } = getChanges();
  const totalChanges = adds.length + deletes.length;

  return (
    <>
      <div style={{ marginBottom: 16 }}>
        <h2 style={{ color: '#ea580c', margin: '0 0 4px' }}>Đăng ký ca làm việc</h2>
        {isReviewing && (
          <div style={{ background: '#fef9c3', color: '#854d0e', padding: '12px 16px', borderRadius: 8, marginBottom: 16, border: '1px solid #fde047' }}>
            <strong>⏳ Đã khóa sổ đăng ký!</strong> Quản lý đang trong quá trình xét duyệt ca làm việc. Bạn không thể thêm hay hủy ca vào lúc này.
          </div>
        )}
        <p style={{ fontSize: 13, color: '#64748b', margin: 0 }}>Quản lý đang mở đăng ký cho tuần này. Hãy chọn các ca bạn có thể làm.</p>
      </div>

      {capacityMessage && (
        <div style={{ background: '#fef2f2', color: '#b91c1c', padding: '10px 14px', borderRadius: 8, margin: '-4px 0 16px', border: '1px solid #fecaca', fontWeight: 700 }}>
          {capacityMessage}
        </div>
      )}

      <div className="sd-shift-legend" style={{ marginLeft: -20, marginRight: -20, paddingLeft: 20 }}>
        {shifts.length === 0 && <p style={{ fontSize: 13 }}>Chưa cấu hình ca làm việc.</p>}
        {shifts.map((s) => (
          <div key={s.id} className="sd-shift-legend-item">
            <span>⏱️</span>
            <div><strong>{s.shiftName}</strong><small>{s.startTime?.slice(0, 5)} – {s.endTime?.slice(0, 5)}</small></div>
          </div>
        ))}
      </div>

      {shifts.length > 0 && dates.length > 0 && (
        <div className="sd-shift-grid-vertical">
          <div className="sd-grid-row sd-grid-header-row">
            <div className="sd-grid-corner-v" />
            {shifts.map((s) => <div key={s.id} className="sd-grid-shift-col-label">{s.shiftName}</div>)}
          </div>

          {dates.map((dateObj) => {
            const dateStr = toDateString(dateObj);
            const dayOfWeek = DAY_NAMES[dateObj.getDay()];
            const shortDate = `${dateObj.getDate()}/${dateObj.getMonth() + 1}`;

            return (
              <div key={dateStr} className="sd-grid-row">
                <div className="sd-grid-day-row-label">
                  <strong>{dayOfWeek}</strong><small>{shortDate}</small>
                </div>

                {shifts.map((shift) => {
                  const isOn = registered[dateStr]?.[shift.id] || false;
                  const dbItem = dbRegistrations[dateStr]?.[shift.id];
                  const isLocked = dbItem && dbItem.status !== "Chờ Duyệt" || isReviewing;

                  const registeredCount = getRegisteredCount(dateStr, shift.id);
                  const maxStaff = getMaxStaffForShiftDate(shift.id, dateObj);
                  const isFull = !isOn && isShiftFull(dateStr, shift.id, dateObj);

                  return (
                    <button
                      key={shift.id}
                      className={`sd-shift-cell-v ${isOn ? 'selected' : ''} ${isFull ? 'full' : ''}`}
                      onClick={() => toggle(dateStr, shift.id, dateObj)}
                      type="button"
                      disabled={isReviewing || isFull}
                      style={isFull
                        ? { cursor: 'not-allowed', backgroundColor: '#fee2e2', borderColor: '#dc2626', color: '#991b1b', fontWeight: 800 }
                        : isLocked ? { opacity: 0.6, cursor: 'not-allowed', backgroundColor: '#fed7aa', borderColor: '#ea580c' } : {}}
                    >
                      {isFull && (
                        <span style={{ display: 'grid', gap: 2, lineHeight: 1.1 }}>
                          <strong>Đã đủ người</strong>
                          <small>{registeredCount}/{maxStaff}</small>
                        </span>
                      )}
                      {!isFull && !isOn && (
                        <small style={{ color: '#94a3b8' }}>{registeredCount}/{maxStaff}</small>
                      )}
                      {isOn ? (isLocked ? '🔒' : '✓') : ''}
                    </button>
                  );
                })}
              </div>
            );
          })}
        </div>
      )}

      <div className="sd-shift-actions">
        <button className="sd-btn-ghost" onClick={handleReset} type="button" disabled={totalChanges === 0}>Hoàn tác thay đổi</button>
        <button className="sd-btn-primary" disabled={saving || totalChanges === 0 || isReviewing} onClick={handleSave} type="button">
          {saving ? 'Đang lưu…' : `Xác nhận lưu thay đổi (${totalChanges} ca)`}
        </button>
      </div>

      {saved && totalChanges === 0 && (
        <p className="sd-save-notice" style={{ color: '#15803d', fontSize: 13, marginTop: 12, textAlign: 'center' }}>
          ✅ Dữ liệu đã được đồng bộ. Các ca đăng ký sẽ có biểu tượng (🔒) nếu quản lý đã bắt đầu duyệt.
        </p>
      )}
    </>
  );
}
