import { useEffect, useMemo, useState } from 'react';
import axios from 'axios';

function getValue(item, keys, fallback = '') {
  if (!item) return fallback;

  for (const key of keys) {
    if (item[key] !== undefined && item[key] !== null) return item[key];

    const realKey = Object.keys(item).find(
      (itemKey) => itemKey.toLowerCase() === key.toLowerCase()
    );

    if (realKey && item[realKey] !== undefined && item[realKey] !== null) {
      return item[realKey];
    }
  }

  return fallback;
}

function formatNumber(value) {
  return new Intl.NumberFormat('vi-VN').format(Number(value || 0));
}

export function ShiftClosingReportTab() {
  const [shiftInfo, setShiftInfo] = useState(null);
  const [items, setItems] = useState([]);
  const [note, setNote] = useState('');
  const [reports, setReports] = useState([]);
  const [selectedReport, setSelectedReport] = useState(null);

  const [loading, setLoading] = useState(false);
  const [submitting, setSubmitting] = useState(false);
  const [loadingReport, setLoadingReport] = useState(false);

  const [message, setMessage] = useState('');
  const [error, setError] = useState('');

  const totalSystemCount = useMemo(() => {
    return items.reduce((sum, item) => sum + Number(item.systemCount || 0), 0);
  }, [items]);

  const totalActualCount = useMemo(() => {
    return items.reduce((sum, item) => sum + Number(item.actualCount || 0), 0);
  }, [items]);

  const totalDifference = totalSystemCount - totalActualCount;

  const submittedReport = useMemo(() => {
  if (!shiftInfo?.scheduleId) return null;

  return reports.find(
    (report) => Number(report.scheduleId) === Number(shiftInfo.scheduleId)
  ) || null;
}, [reports, shiftInfo]);

const displayTotalSystemCount =
  shiftInfo?.alreadyReported && submittedReport
    ? submittedReport.totalSystemCount
    : totalSystemCount;

const displayTotalActualCount =
  shiftInfo?.alreadyReported && submittedReport
    ? submittedReport.totalActualCount
    : totalActualCount;

const displayTotalDifference =
  shiftInfo?.alreadyReported && submittedReport
    ? submittedReport.totalDifference
    : totalDifference;

  function normalizeShift(data) {
  if (!data) return null;

  return {
    scheduleId: Number(
      getValue(data, ['scheduleId', 'ScheduleId'], 0)
    ),
    shiftId: Number(
      getValue(data, ['shiftId', 'ShiftId'], 0)
    ),
    shiftName: getValue(
      data,
      ['shiftName', 'ShiftName'],
      'Ca làm'
    ),
    workDate: getValue(
      data,
      ['workDate', 'WorkDate'],
      ''
    ),
    startTime: getValue(
      data,
      ['startTime', 'StartTime'],
      ''
    ),
    endTime: getValue(
      data,
      ['endTime', 'EndTime'],
      ''
    ),
    alreadyReported: Boolean(
      getValue(data, ['alreadyReported', 'AlreadyReported'], false)
    ),

    hasCheckedIn: Boolean(
      getValue(data, ['hasCheckedIn', 'HasCheckedIn'], false)
    ),
    hasCheckedOut: Boolean(
      getValue(data, ['hasCheckedOut', 'HasCheckedOut'], false)
    ),
    isShiftEnded: Boolean(
      getValue(data, ['isShiftEnded', 'IsShiftEnded'], false)
    ),
    canSubmit: Boolean(
      getValue(data, ['canSubmit', 'CanSubmit'], false)
    ),
    submitBlockReason: getValue(
      data,
      ['submitBlockReason', 'SubmitBlockReason'],
      ''
    ),
  };
}

  function normalizeItem(item) {
    const systemCount = Number(getValue(item, ['systemCount', 'SystemCount'], 0) || 0);
    const actualCount = Number(getValue(item, ['actualCount', 'ActualCount'], systemCount) || 0);

    return {
      productId: Number(getValue(item, ['productId', 'ProductId'], 0)),
      productCode: getValue(item, ['productCode', 'ProductCode'], ''),
      productName: getValue(item, ['productName', 'ProductName'], 'Chưa rõ sản phẩm'),
      unit: getValue(item, ['unit', 'Unit'], 'Cái') || 'Cái',
      systemCount,
      actualCount,
    };
  }

  function normalizeReport(report) {
    return {
      id: Number(getValue(report, ['id', 'Id'], 0)),
  scheduleId: Number(getValue(report, ['scheduleId', 'ScheduleId'], 0)),
      branchName: getValue(report, ['branchName', 'BranchName'], ''),
      staffName: getValue(report, ['staffName', 'StaffName'], ''),
      shiftName: getValue(report, ['shiftName', 'ShiftName'], ''),
      workDate: getValue(report, ['workDate', 'WorkDate'], ''),
      reportDate: getValue(report, ['reportDate', 'ReportDate'], ''),
      itemCount: Number(getValue(report, ['itemCount', 'ItemCount'], 0)),
      totalSystemCount: Number(getValue(report, ['totalSystemCount', 'TotalSystemCount'], 0)),
      totalActualCount: Number(getValue(report, ['totalActualCount', 'TotalActualCount'], 0)),
      totalDifference: Number(getValue(report, ['totalDifference', 'TotalDifference'], 0)),
      note: getValue(report, ['note', 'Note'], ''),
      items: Array.isArray(getValue(report, ['items', 'Items'], []))
        ? getValue(report, ['items', 'Items'], []).map((detail) => ({
            productId: Number(getValue(detail, ['productId', 'ProductId'], 0)),
            productCode: getValue(detail, ['productCode', 'ProductCode'], ''),
            productName: getValue(detail, ['productName', 'ProductName'], 'Chưa rõ sản phẩm'),
            unit: getValue(detail, ['unit', 'Unit'], 'Cái') || 'Cái',
            systemCount: Number(getValue(detail, ['systemCount', 'SystemCount'], 0)),
            actualCount: Number(getValue(detail, ['actualCount', 'ActualCount'], 0)),
            difference: Number(getValue(detail, ['difference', 'Difference'], 0)),
          }))
        : [],
    };
  }

  async function loadData() {
    setLoading(true);
    setError('');
    setMessage('');
    setSelectedReport(null);

    try {
      const [shiftRes, stockRes, reportRes] = await Promise.allSettled([
        axios.get('/api/ShiftClosing/today-shift'),
        axios.get('/api/ShiftClosing/front-stock'),
        axios.get('/api/ShiftClosing/my-reports'),
      ]);

      if (shiftRes.status === 'fulfilled') {
        setShiftInfo(normalizeShift(shiftRes.value.data));
      } else {
        setShiftInfo(null);
        setError(shiftRes.reason?.response?.data?.message || 'Không tìm thấy ca làm hôm nay.');
      }

      if (stockRes.status === 'fulfilled') {
        const stockData = Array.isArray(stockRes.value.data) ? stockRes.value.data : [];
        setItems(stockData.map(normalizeItem));
      } else {
        setItems([]);
      }

      if (reportRes.status === 'fulfilled') {
        const reportData = Array.isArray(reportRes.value.data) ? reportRes.value.data : [];
        setReports(reportData.map(normalizeReport));
      } else {
        setReports([]);
      }
    } finally {
      setLoading(false);
    }
  }

  function updateActualCount(productId, value) {
    const numberValue = value === '' ? '' : Number(value);

    setItems((currentItems) =>
      currentItems.map((item) =>
        item.productId === productId
          ? { ...item, actualCount: numberValue }
          : item
      )
    );
  }

  async function submitReport() {
    if (!shiftInfo?.canSubmit) {
  setError(
    shiftInfo?.submitBlockReason ||
      'Bạn chưa đủ điều kiện báo cáo kết ca.'
  );
  return;
}
    if (!shiftInfo?.scheduleId) {
      setError('Không tìm thấy ca cần báo cáo.');
      return;
    }

    if (shiftInfo.alreadyReported) {
      setError('Ca này đã gửi báo cáo kết ca.');
      return;
    }

    for (const item of items) {
      if (item.actualCount === '' || Number.isNaN(Number(item.actualCount))) {
        setError(`Vui lòng nhập số lượng thực tế cho sản phẩm "${item.productName}".`);
        return;
      }

      if (Number(item.actualCount) < 0) {
        setError(`Số lượng thực tế của "${item.productName}" không được âm.`);
        return;
      }

      if (Number(item.actualCount) > Number(item.systemCount)) {
        setError(`Số lượng thực tế của "${item.productName}" không được lớn hơn số lượng hệ thống.`);
        return;
      }
    }

    setSubmitting(true);
    setError('');
    setMessage('');

    try {
      await axios.post('/api/ShiftClosing/submit', {
        scheduleId: shiftInfo.scheduleId,
        note: note || null,
        items: items.map((item) => ({
          productId: item.productId,
          actualCount: Number(item.actualCount),
        })),
      });

      setMessage('Gửi báo cáo kết ca thành công.');
      await loadData();
    } catch (err) {
      setError(err.response?.data?.message || 'Không gửi được báo cáo kết ca.');
    } finally {
      setSubmitting(false);
    }
  }

  async function loadReportDetail(reportId) {
    setLoadingReport(true);
    setError('');

    try {
      const response = await axios.get(`/api/ShiftClosing/my-reports/${reportId}`);
      setSelectedReport(normalizeReport(response.data));
    } catch (err) {
      setError(err.response?.data?.message || 'Không tải được chi tiết báo cáo.');
      setSelectedReport(null);
    } finally {
      setLoadingReport(false);
    }
  }

  useEffect(() => {
    loadData();
  }, []);

const canSubmit =
  Boolean(shiftInfo?.canSubmit) &&
  items.length > 0;

  return (
    <div className="staff-closing-page">
      <div className="staff-closing-hero">
        <div>
          <p className="staff-eyebrow">Báo cáo cuối ca</p>
          <h2>Báo cáo kết ca</h2>
          <span>Kiểm đếm số lượng hàng còn lại tại quầy sau ca làm.</span>
        </div>

        <button type="button" onClick={loadData} disabled={loading}>
          {loading ? 'Đang tải...' : 'Làm mới'}
        </button>
      </div>

      {error && <div className="staff-status error">{error}</div>}
      {message && <div className="staff-status success">{message}</div>}

      <div className="staff-closing-grid">
        <section className="staff-closing-card">
          <div className="staff-section-head">
            <div>
              <p className="staff-eyebrow">Ca làm</p>
              <h3>Thông tin ca làm</h3>
            </div>
          </div>

          {shiftInfo ? (
            <div className="staff-shift-box">
              <div>
                <span>Ca làm</span>
                <strong>{shiftInfo.shiftName}</strong>
              </div>

              <div>
                <span>Ngày làm</span>
                <strong>{shiftInfo.workDate}</strong>
              </div>

              <div>
                <span>Giờ ca</span>
                <strong>{shiftInfo.startTime} - {shiftInfo.endTime}</strong>
              </div>

              <div>
                <span>Trạng thái</span>
                <strong className={shiftInfo.alreadyReported ? 'danger' : 'ok'}>
                  {shiftInfo.alreadyReported ? 'Đã báo cáo' : 'Chưa báo cáo'}
                </strong>
              </div>
            </div>
          ) : (
            <div className="staff-empty">Hôm nay chưa có ca làm cần báo cáo.</div>
          )}
        </section>

        <section className="staff-closing-card">
          <div className="staff-section-head">
            <div>
              <p className="staff-eyebrow">Tổng quan</p>
              <h3>Số liệu kiểm kê</h3>
            </div>
          </div>

          <div className="staff-summary-row">
            <div>
              <span>Số lượng hệ thống</span>
              <strong>{formatNumber(displayTotalSystemCount)}</strong>
            </div>

            <div>
              <span>Số lượng thực tế</span>
              <strong>{formatNumber(displayTotalActualCount)}</strong>
            </div>

            <div>
              <span>Chênh lệch</span>
            <strong>{formatNumber(displayTotalDifference)}</strong>
            </div>
          </div>
        </section>
      </div>

      <section className="staff-closing-card">
  <div className="staff-section-head">
    <div>
      <p className="staff-eyebrow">Tồn quầy</p>
      <h3>
        {shiftInfo?.alreadyReported
          ? 'Ca này đã gửi báo cáo'
          : 'Nhập số lượng thực tế cuối ca'}
      </h3>
    </div>
  </div>

{shiftInfo?.alreadyReported ? (
  <div className="staff-empty">
    Ca này đã gửi báo cáo kết ca. Bạn có thể xem chi tiết
    trong phần lịch sử bên dưới.
  </div>
) : !shiftInfo?.canSubmit ? (
  <div className="staff-empty">
    {shiftInfo?.submitBlockReason ||
      'Bạn chưa đủ điều kiện báo cáo kết ca.'}
  </div>
) : (
    <>
      {items.length === 0 ? (
        <div className="staff-empty">Quầy hiện chưa có mặt hàng nào.</div>
      ) : (
        <div className="staff-closing-table-wrap">
          <table className="staff-closing-table">
            <thead>
              <tr>
                <th>Mặt hàng</th>
                <th>Mã SP</th>
                <th>Đơn vị</th>
                <th className="text-right">Hệ thống</th>
                <th className="text-right">Thực tế</th>
                <th className="text-right">Chênh lệch</th>
              </tr>
            </thead>

            <tbody>
              {items.map((item) => {
                const difference = Number(item.systemCount || 0) - Number(item.actualCount || 0);

                return (
                  <tr key={item.productId}>
                    <td>
                      <strong>{item.productName}</strong>
                    </td>
                    <td>{item.productCode || '—'}</td>
                    <td>{item.unit}</td>
                    <td className="text-right">{formatNumber(item.systemCount)}</td>
                    <td className="text-right">
                      <input
                        type="number"
                        min="0"
                        max={item.systemCount}
                        value={item.actualCount}
                        onChange={(event) => updateActualCount(item.productId, event.target.value)}
                      />
                    </td>
                    <td className="text-right">
                      <strong>{formatNumber(difference)}</strong>
                    </td>
                  </tr>
                );
              })}
            </tbody>
          </table>
        </div>
      )}

      <div className="staff-closing-note">
        <label>Ghi chú</label>
        <textarea
          value={note}
          onChange={(event) => setNote(event.target.value)}
          placeholder="Nhập ghi chú nếu có..."
        />
      </div>

      <div className="staff-closing-actions">
        <button
          type="button"
          onClick={submitReport}
          disabled={!canSubmit || submitting}
        >
          {submitting ? 'Đang gửi...' : 'Gửi báo cáo kết ca'}
        </button>
      </div>
    </>
  )}
</section>

      <section className="staff-closing-card">
        <div className="staff-section-head">
          <div>
            <p className="staff-eyebrow">Lịch sử</p>
            <h3>Báo cáo đã gửi</h3>
          </div>
        </div>

        {reports.length === 0 ? (
          <div className="staff-empty">Chưa có báo cáo kết ca nào.</div>
        ) : (
          <div className="staff-report-list">
            {reports.map((report) => (
              <button
                type="button"
                key={report.id}
                className={`staff-report-card ${selectedReport?.id === report.id ? 'active' : ''}`}
                onClick={() => loadReportDetail(report.id)}
              >
                <div>
                  <strong>#{report.id}</strong>
                  <span>{report.reportDate}</span>
                </div>
                <p>{report.shiftName || 'Ca làm'}</p>
                <small>
                  Hệ thống {formatNumber(report.totalSystemCount)} · Thực tế {formatNumber(report.totalActualCount)} · Lệch {formatNumber(report.totalDifference)}
                </small>
              </button>
            ))}
          </div>
        )}

        {loadingReport && <div className="staff-empty">Đang tải chi tiết báo cáo...</div>}

        {selectedReport && (
          <div className="staff-report-detail">
            <h4>Chi tiết báo cáo #{selectedReport.id}</h4>

            <div className="staff-closing-table-wrap">
              <table className="staff-closing-table">
                <thead>
                  <tr>
                    <th>Mặt hàng</th>
                    <th>Đơn vị</th>
                    <th className="text-right">Hệ thống</th>
                    <th className="text-right">Thực tế</th>
                    <th className="text-right">Chênh lệch</th>
                  </tr>
                </thead>

                <tbody>
                  {selectedReport.items.map((item) => (
                    <tr key={item.productId}>
                      <td>
                        <strong>{item.productName}</strong>
                      </td>
                      <td>{item.unit}</td>
                      <td className="text-right">{formatNumber(item.systemCount)}</td>
                      <td className="text-right">{formatNumber(item.actualCount)}</td>
                      <td className="text-right">{formatNumber(item.difference)}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          </div>
        )}
      </section>
    </div>
  );
}