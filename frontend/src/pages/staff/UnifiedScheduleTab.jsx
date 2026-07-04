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
  const [periods, setPeriods] = useState([])
  const [selectedPeriodId, setSelectedPeriodId] = useState('')
  const [loading, setLoading] = useState(true)

  useEffect(() => {
    async function init() {
      try {
        const allPeriods = await getAllPeriods()

        const branchPeriods = allPeriods
          .filter((p) => String(p.branchId) === String(user.branchId))
          .filter((p) => {
            const st = String(p.status || '').toUpperCase()

            return (
              st === 'OPEN' ||
              st === 'CLOSED' ||
              st === 'PUBLISHED' ||
              st === 'REVIEWING' ||
              st === 'DRAFT'
            )
          })
          .sort((a, b) => new Date(b.startDate) - new Date(a.startDate))

        setPeriods(branchPeriods)

        if (branchPeriods.length > 0) {
          const firstOpenPeriod = branchPeriods.find((p) => {
            return String(p.status || '').toUpperCase() === 'OPEN'
          })

          const firstPeriod = firstOpenPeriod || branchPeriods[0]

          setSelectedPeriodId(firstPeriod.id.toString())
        }
      } catch (e) {
        console.error('Lỗi lấy danh sách đợt:', e)
      } finally {
        setLoading(false)
      }
    }

    init()
  }, [user.branchId])

  const selectedPeriod = periods.find((p) => {
    return p.id.toString() === selectedPeriodId
  })

  const selectedStatus = String(selectedPeriod?.status || '').toUpperCase()

  const isPublished = selectedStatus === 'PUBLISHED'

  function handleChangePeriod(e) {
    setSelectedPeriodId(e.target.value)
  }

  function getPeriodStatusText(status) {
    const st = String(status || '').toUpperCase()

    if (st === 'OPEN') return 'Đang mở đăng ký'

    if (
      st === 'CLOSED' ||
      st === 'REVIEWING' ||
      st === 'DRAFT'
    ) {
      return 'Đã đóng đăng ký'
    }

    if (st === 'PUBLISHED') return 'Đã công bố lịch'

    return status || 'Không rõ trạng thái'
  }

  if (loading) {
    return (
      <div className="sd-card">
        <p>Đang tải dữ liệu...</p>
      </div>
    )
  }

  if (periods.length === 0) {
    return (
      <div className="sd-card">
        <div
          className="sd-empty-state"
          style={{
            padding: '40px 20px'
          }}
        >
          <span className="sd-empty-icon">🗓️</span>

          <h3
            style={{
              color: '#1e293b',
              marginTop: 10
            }}
          >
            Chưa có dữ liệu lịch làm
          </h3>

          <p>
            Hiện tại chi nhánh của bạn chưa có lịch làm chính thức hoặc đợt đăng ký ca nào được mở.
          </p>
        </div>
      </div>
    )
  }

  return (
    <div
      className="sd-card"
      style={{
        padding: '20px 0'
      }}
    >
      <div
        style={{
          padding: '0 20px 16px',
          borderBottom: '1px solid #f1f5f9',
          marginBottom: 16
        }}
      >
        <div
          style={{
            display: 'grid',
            gap: 10
          }}
        >
          <div
            style={{
              display: 'flex',
              gap: 12,
              alignItems: 'center',
              flexWrap: 'wrap'
            }}
          >
            <span
              style={{
                fontSize: 14,
                fontWeight: 600,
                color: '#475569',
                whiteSpace: 'nowrap'
              }}
            >
              Chọn tuần:
            </span>

            <select
              className="sd-input-search"
              style={{
                width: '100%',
                maxWidth: 520
              }}
              value={selectedPeriodId}
              onChange={handleChangePeriod}
            >
              {periods.map((p) => (
                <option
                  key={p.id}
                  value={p.id}
                >
                  Từ {formatDate(p.startDate)} đến {formatDate(p.endDate)} - {getPeriodStatusText(p.status)}
                </option>
              ))}
            </select>
          </div>

          <div
            style={{
              fontSize: 13,
              color: '#64748b'
            }}
          >
            Trạng thái tuần đang chọn:{' '}

            <strong
              style={{
                color: isPublished ? '#1d4ed8' : '#ea580c'
              }}
            >
              {getPeriodStatusText(selectedPeriod?.status)}
            </strong>
          </div>

          <div
            style={{
              fontSize: 13,
              color: '#64748b'
            }}
          >
            {isPublished
              ? 'Tuần này đã công bố lịch. Hệ thống đang hiển thị lịch làm chính thức.'
              : 'Tuần này chưa công bố lịch. Hệ thống đang hiển thị màn hình đăng ký ca làm.'}
          </div>
        </div>
      </div>

      <div
        style={{
          padding: '0 20px'
        }}
      >
        {isPublished ? (
          <PublishedScheduleView
            period={selectedPeriod}
            user={user}
          />
        ) : (
          <RegistrationView
            period={selectedPeriod}
            user={user}
          />
        )}
      </div>
    </div>
  )
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

        const approvedRegs = (regRes.data || []).filter((r) => {
          return (
            r.status === 'REGISTERED' ||
            r.status === 'Đã Duyệt' ||
            r.status === 'APPROVED'
          )
        })
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
  const periodStatus = String(period.status || '').toUpperCase()

  const isClosed =
    periodStatus === 'CLOSED' ||
    periodStatus === 'REVIEWING' ||
    periodStatus === 'DRAFT'

  const isPublished = periodStatus === 'PUBLISHED'

  const isLocked = isClosed || isPublished

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

  function getTotalMaxStaffForShiftDate(shiftId, dateObj) {
    const dayName = dateObj.toLocaleDateString('en-US', { weekday: 'long' })

    const config = shiftConfigs.find((cfg) => {
      return (
        cfg.shiftId === shiftId &&
        String(cfg.dayOfWeek).toLowerCase() === dayName.toLowerCase()
      )
    })

    const shift = shifts.find((item) => item.id === shiftId)

    return Number(config?.maxStaff ?? shift?.maxStaff ?? 0)
  }

  function getStaffSlotForShiftDate(shiftId, dateObj) {
    const totalMaxStaff = getTotalMaxStaffForShiftDate(shiftId, dateObj)

    return Math.max(totalMaxStaff - 1, 0)
  }

  function isRejectedStatus(status = '') {
    const normalized = String(status).toLowerCase()

    return (
      normalized === 'cancelled' ||
      normalized === 'rejected' ||
      normalized.includes('từ chối') ||
      normalized.includes('tá»« chá»‘i')
    )
  }

  function getRegisteredCount(dateStr, shiftId) {
    return allRegistrations.filter((item) =>
      item.workDate?.slice(0, 10) === dateStr &&
      item.shiftId === shiftId &&
      !isRejectedStatus(item.status)
    ).length;
  }

  function isShiftFull(dateStr, shiftId, dateObj) {
    const staffSlot = getStaffSlotForShiftDate(shiftId, dateObj)

    if (staffSlot <= 0) return true

    return getRegisteredCount(dateStr, shiftId) >= staffSlot
  }

  function toggle(dateStr, shiftId, dateObj) {
    const dbItem = dbRegistrations[dateStr]?.[shiftId]

    if (isLocked) return

    if (dbItem && dbItem.status !== 'REGISTERED' && dbItem.status !== 'Chờ Duyệt') {
      return
    }

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
          adds.push({
            userId: user.id,
            periodId: period.id,
            shiftId: parseInt(sId),
            workDate: dStr,
            status: 'REGISTERED'
          })
        }
      });
    });

    Object.entries(dbRegistrations).forEach(([dStr, shiftsInfo]) => {
      Object.entries(shiftsInfo).forEach(([sId, dbItem]) => {
        const isSelectedNow = registered[dStr]?.[sId];
        if (
          !isSelectedNow &&
          (dbItem.status === 'REGISTERED' || dbItem.status === 'Chờ Duyệt')
        ) {
          deletes.push(dbItem.id)
        }
      });
    });

    return { adds, deletes };
  }

  async function handleSave() {
    const { adds, deletes } = getChanges()

    if (adds.length === 0 && deletes.length === 0) {
      return alert('Không có thay đổi nào để lưu!')
    }

    setSaving(true)

    try {
      // Xóa/hủy ca trước nếu có
      for (const regId of deletes) {
        await axios.delete(`/api/StaffRegistration/${regId}/user/${user.id}`)
      }

      // Đăng ký ca lần lượt, không gửi nhiều request cùng lúc
      for (const payload of adds) {
        await axios.post('/api/StaffRegistration', payload)
      }

      // Load lại dữ liệu sau khi lưu
      const [regRes, periodRegRes] = await Promise.all([
        axios.get(`/api/StaffRegistration/my-schedule/${user.id}/${period.id}`),
        axios.get(`/api/StaffRegistration/period/${period.id}`)
      ])

      setAllRegistrations(periodRegRes.data || [])

      const dbMap = {}
      const initRegs = {}

        ; (regRes.data || []).forEach((r) => {
          const dStr = r.workDate.slice(0, 10)

          if (!dbMap[dStr]) {
            dbMap[dStr] = {}
            initRegs[dStr] = {}
          }

          dbMap[dStr][r.shiftId] = {
            id: r.id,
            status: r.status
          }

          initRegs[dStr][r.shiftId] = true
        })

      setDbRegistrations(dbMap)
      setRegistered(initRegs)
      setSaved(true)

      alert('✅ Đã lưu đăng ký ca thành công!')
    } catch (err) {
      console.error(err)

      alert(
        '❌ Lỗi: ' +
        (
          err.response?.data?.message ||
          err.response?.data ||
          'Có lỗi xảy ra khi lưu đăng ký!'
        )
      )
    } finally {
      setSaving(false)
    }
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
        {isLocked && (
          <div style={{ background: '#fef9c3', color: '#854d0e', padding: '12px 16px', borderRadius: 8, marginBottom: 16, border: '1px solid #fde047' }}>
            <strong>Đợt đăng ký đã đóng!</strong> Bạn chỉ có thể xem các ca đã đăng ký, không thể thêm hoặc hủy ca trong tuần này.
          </div>
        )}
        <p style={{ fontSize: 13, color: '#64748b', margin: 0 }}>
          {isLocked
            ? 'Đợt đăng ký này đã đóng. Bạn chỉ có thể xem lại các ca đã đăng ký.'
            : 'Quản lý đang mở đăng ký cho tuần này. Hãy chọn các ca bạn có thể làm.'}
        </p>
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
                  const isCellLocked =
                    isLocked ||
                    (
                      dbItem &&
                      dbItem.status !== 'REGISTERED' &&
                      dbItem.status !== 'Chờ Duyệt'
                    )

                  const totalMaxStaff = getTotalMaxStaffForShiftDate(shift.id, dateObj)
                  const staffSlot = getStaffSlotForShiftDate(shift.id, dateObj)
                  const isFull = !isOn && isShiftFull(dateStr, shift.id, dateObj)

                  const savedStaffList = allRegistrations.filter((item) => {
                    return (
                      item.workDate?.slice(0, 10) === dateStr &&
                      item.shiftId === shift.id &&
                      !isRejectedStatus(item.status)
                    )
                  })

                  const hasMeInSavedList = savedStaffList.some((item) => item.userId === user.id)

                  const displayStaffList = savedStaffList.map((item) => ({
                    id: item.id,
                    name:
                      item.userId === user.id
                        ? user.fullName || 'Bạn'
                        : item.user?.fullName || item.user?.username || 'Nhân viên',
                    isMe: item.userId === user.id,
                    isPending: false
                  }))

                  if (isOn && !hasMeInSavedList) {
                    displayStaffList.push({
                      id: `new-${dateStr}-${shift.id}`,
                      name: user.fullName || 'Bạn',
                      isMe: true,
                      isPending: true
                    })
                  }

                  const emptySlotCount = Math.max(staffSlot - displayStaffList.length, 0)

                  return (
                    <button
                     key={shift.id}
  className={`sd-shift-cell-v sd-shift-cell-slots ${isOn ? 'selected' : ''} ${(isFull || isCellLocked) ? 'disabled' : ''}`}
  onClick={() => toggle(dateStr, shift.id, dateObj)}
  type="button"
  disabled={isCellLocked || isFull}
                    >
                      <div className="sd-slot-list">
  {totalMaxStaff > 0 && (
    <div className="sd-slot-person sd-slot-manager">
      <span className="sd-slot-name">
        Quản lý
      </span>
    </div>
  )}

  {displayStaffList.map((staff) => (
    <div
      key={staff.id}
      className={`sd-slot-person ${staff.isMe ? 'sd-slot-me' : 'sd-slot-staff'}`}
    >
      <span className="sd-slot-name">
        {staff.name}
      </span>

      {staff.isPending && (
        <span className="sd-slot-pending">
          Chưa lưu
        </span>
      )}
    </div>
  ))}

  {Array.from({ length: emptySlotCount }).map((_, index) => (
    <div
      key={`empty-${dateStr}-${shift.id}-${index}`}
      className="sd-slot-empty"
    >
      Còn trống
    </div>
  ))}

  {staffSlot <= 0 && (
    <div className="sd-slot-empty">
      Không có slot nhân viên
    </div>
  )}

  {isFull && (
    <div className="sd-slot-full-text">
      Ca đã đủ người
    </div>
  )}
</div>
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
        <button
          className="sd-btn-primary"
          disabled={saving || totalChanges === 0 || isLocked}
          onClick={handleSave}
          type="button"
        >
          {saving ? 'Đang lưu…' : `Xác nhận lưu thay đổi (${totalChanges} ca)`}
        </button>
      </div>

      {saved && totalChanges === 0 && (
        <p className="sd-save-notice" style={{ color: '#15803d', fontSize: 13, marginTop: 12, textAlign: 'center' }}>
          ✅ Dữ liệu đã được đồng bộ. Các ca đăng ký sẽ có biểu tượng (🔒) nếu đợt đăng ký đã đóng hoặc đã công bố lịch.
        </p>
      )}
    </>
  );
}
