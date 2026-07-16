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

function getStatusLabel(status) {
  switch (String(status || '').toUpperCase()) {
    case 'PENDING':
      return 'Chờ Quản lý duyệt';
    case 'APPROVED':
      return 'Đã duyệt';
    case 'REJECTED':
      return 'Bị từ chối';
    default:
      return 'Chưa báo cáo';
  }
}

const REPORTS_PER_PAGE = 5;

function getStatusClass(status) {
  switch (String(status || '').toUpperCase()) {
    case 'PENDING':
      return 'pending';
    case 'APPROVED':
      return 'approved';
    case 'REJECTED':
      return 'rejected';
    default:
      return 'none';
  }
}

export function ShiftClosingReportTab() {
  const [shiftInfo, setShiftInfo] = useState(null);
  const [items, setItems] = useState([]);
  const [note, setNote] = useState('');
  const [reports, setReports] = useState([]);
  const [selectedReport, setSelectedReport] = useState(null);
  const [isDetailOpen, setIsDetailOpen] = useState(false);
  const [keyword, setKeyword] = useState('');
  const [statusFilter, setStatusFilter] = useState('ALL');
  const [currentPage, setCurrentPage] = useState(1);

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

    return (
      reports.find(
        (report) => Number(report.scheduleId) === Number(shiftInfo.scheduleId)
      ) || null
    );
  }, [reports, shiftInfo]);

  const shouldDisplaySubmittedTotals =
    ['PENDING', 'APPROVED'].includes(shiftInfo?.reportStatus) &&
    submittedReport;

  const displayTotalSystemCount = shouldDisplaySubmittedTotals
    ? submittedReport.totalSystemCount
    : totalSystemCount;

  const displayTotalActualCount = shouldDisplaySubmittedTotals
    ? submittedReport.totalActualCount
    : totalActualCount;

  const displayTotalDifference = shouldDisplaySubmittedTotals
    ? submittedReport.totalDifference
    : totalDifference;


  const filteredReports = useMemo(() => {
    const searchText = keyword.trim().toLowerCase();

    return reports.filter((report) => {
      const matchesStatus =
        statusFilter === 'ALL' || report.status === statusFilter;

      if (!matchesStatus) return false;
      if (!searchText) return true;

      return `#${report.id} ${report.id} ${report.shiftName} ${report.workDate} ${report.reportDate} ${report.branchName} ${getStatusLabel(report.status)}`
        .toLowerCase()
        .includes(searchText);
    });
  }, [reports, keyword, statusFilter]);

  const totalPages = Math.max(
    1,
    Math.ceil(filteredReports.length / REPORTS_PER_PAGE)
  );

  const safeCurrentPage = Math.min(currentPage, totalPages);

  const paginatedReports = useMemo(() => {
    const startIndex = (safeCurrentPage - 1) * REPORTS_PER_PAGE;
    return filteredReports.slice(
      startIndex,
      startIndex + REPORTS_PER_PAGE
    );
  }, [filteredReports, safeCurrentPage]);

  const firstVisibleReport =
    filteredReports.length === 0
      ? 0
      : (safeCurrentPage - 1) * REPORTS_PER_PAGE + 1;

  const lastVisibleReport = Math.min(
    safeCurrentPage * REPORTS_PER_PAGE,
    filteredReports.length
  );

  function normalizeShift(data) {
    if (!data) return null;

    return {
      scheduleId: Number(getValue(data, ['scheduleId', 'ScheduleId'], 0)),
      shiftId: Number(getValue(data, ['shiftId', 'ShiftId'], 0)),
      shiftName: getValue(data, ['shiftName', 'ShiftName'], 'Ca làm'),
      workDate: getValue(data, ['workDate', 'WorkDate'], ''),
      startTime: getValue(data, ['startTime', 'StartTime'], ''),
      endTime: getValue(data, ['endTime', 'EndTime'], ''),
      reportId: Number(getValue(data, ['reportId', 'ReportId'], 0)) || null,
      reportStatus: String(
        getValue(data, ['reportStatus', 'ReportStatus'], 'NONE')
      ).toUpperCase(),
      rejectReason: getValue(data, ['rejectReason', 'RejectReason'], ''),
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
      canSubmit: Boolean(getValue(data, ['canSubmit', 'CanSubmit'], false)),
      submitBlockReason: getValue(
        data,
        ['submitBlockReason', 'SubmitBlockReason'],
        ''
      ),
    };
  }

  function normalizeItem(item) {
    const systemCount = Number(
      getValue(item, ['systemCount', 'SystemCount'], 0) || 0
    );
    const actualCount = Number(
      getValue(item, ['actualCount', 'ActualCount'], systemCount) || 0
    );

    return {
      productId: Number(getValue(item, ['productId', 'ProductId'], 0)),
      productCode: getValue(item, ['productCode', 'ProductCode'], ''),
      productName: getValue(
        item,
        ['productName', 'ProductName'],
        'Chưa rõ sản phẩm'
      ),
      unit: getValue(item, ['unit', 'Unit'], 'Cái') || 'Cái',
      systemCount,
      actualCount,
    };
  }

  function normalizeReport(report) {
    const rawItems = getValue(report, ['items', 'Items'], []);

    return {
      id: Number(getValue(report, ['id', 'Id'], 0)),
      scheduleId: Number(
        getValue(report, ['scheduleId', 'ScheduleId'], 0)
      ),
      branchName: getValue(report, ['branchName', 'BranchName'], ''),
      staffName: getValue(report, ['staffName', 'StaffName'], ''),
      shiftName: getValue(report, ['shiftName', 'ShiftName'], ''),
      workDate: getValue(report, ['workDate', 'WorkDate'], ''),
      reportDate: getValue(report, ['reportDate', 'ReportDate'], ''),
      itemCount: Number(getValue(report, ['itemCount', 'ItemCount'], 0)),
      totalSystemCount: Number(
        getValue(report, ['totalSystemCount', 'TotalSystemCount'], 0)
      ),
      totalActualCount: Number(
        getValue(report, ['totalActualCount', 'TotalActualCount'], 0)
      ),
      totalDifference: Number(
        getValue(report, ['totalDifference', 'TotalDifference'], 0)
      ),
      note: getValue(report, ['note', 'Note'], ''),
      status: String(
        getValue(report, ['status', 'Status'], 'PENDING')
      ).toUpperCase(),
      reviewedBy: Number(
        getValue(report, ['reviewedBy', 'ReviewedBy'], 0)
      ) || null,
      reviewerName: getValue(
        report,
        ['reviewerName', 'ReviewerName'],
        ''
      ),
      reviewedAt: getValue(report, ['reviewedAt', 'ReviewedAt'], ''),
      rejectReason: getValue(
        report,
        ['rejectReason', 'RejectReason'],
        ''
      ),
      items: Array.isArray(rawItems)
        ? rawItems.map((detail) => ({
            productId: Number(
              getValue(detail, ['productId', 'ProductId'], 0)
            ),
            productCode: getValue(
              detail,
              ['productCode', 'ProductCode'],
              ''
            ),
            productName: getValue(
              detail,
              ['productName', 'ProductName'],
              'Chưa rõ sản phẩm'
            ),
            unit: getValue(detail, ['unit', 'Unit'], 'Cái') || 'Cái',
            systemCount: Number(
              getValue(detail, ['systemCount', 'SystemCount'], 0)
            ),
            actualCount: Number(
              getValue(detail, ['actualCount', 'ActualCount'], 0)
            ),
            difference: Number(
              getValue(detail, ['difference', 'Difference'], 0)
            ),
          }))
        : [],
    };
  }

  async function loadData() {
    setLoading(true);
    setError('');
    setSelectedReport(null);
    setIsDetailOpen(false);

    try {
      const [shiftRes, stockRes, reportRes] = await Promise.allSettled([
        axios.get('/api/ShiftClosing/today-shift'),
        axios.get('/api/ShiftClosing/front-stock'),
        axios.get('/api/ShiftClosing/my-reports'),
      ]);

      let nextShiftInfo = null;
      let nextItems = [];
      let nextReports = [];

      if (shiftRes.status === 'fulfilled') {
        nextShiftInfo = normalizeShift(shiftRes.value.data);
      } else {
        setError(
          shiftRes.reason?.response?.data?.message ||
            'Không tìm thấy ca làm hôm nay.'
        );
      }

      if (stockRes.status === 'fulfilled') {
        const stockData = Array.isArray(stockRes.value.data)
          ? stockRes.value.data
          : [];

        nextItems = stockData.map(normalizeItem);
      }

      if (reportRes.status === 'fulfilled') {
        const reportData = Array.isArray(reportRes.value.data)
          ? reportRes.value.data
          : [];

        nextReports = reportData.map(normalizeReport);
      }

      // Báo cáo bị từ chối được nạp lại số thực tế cũ để Staff sửa và gửi lại.
      if (
        nextShiftInfo?.reportStatus === 'REJECTED' &&
        nextShiftInfo?.reportId
      ) {
        try {
          const detailResponse = await axios.get(
            `/api/ShiftClosing/my-reports/${nextShiftInfo.reportId}`
          );
          const rejectedReport = normalizeReport(detailResponse.data);
          const oldActualCounts = new Map(
            rejectedReport.items.map((item) => [
              Number(item.productId),
              Number(item.actualCount),
            ])
          );

          nextItems = nextItems.map((item) => ({
            ...item,
            actualCount: oldActualCounts.has(item.productId)
              ? oldActualCounts.get(item.productId)
              : item.systemCount,
          }));

          setNote(rejectedReport.note || '');
        } catch {
          setNote('');
        }
      } else {
        setNote('');
      }

      setShiftInfo(nextShiftInfo);
      setItems(nextItems);
      setReports(nextReports);
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

    for (const item of items) {
      if (
        item.actualCount === '' ||
        Number.isNaN(Number(item.actualCount))
      ) {
        setError(
          `Vui lòng nhập số lượng thực tế cho sản phẩm "${item.productName}".`
        );
        return;
      }

      if (Number(item.actualCount) < 0) {
        setError(
          `Số lượng thực tế của "${item.productName}" không được âm.`
        );
        return;
      }

      if (Number(item.actualCount) > Number(item.systemCount)) {
        setError(
          `Số lượng thực tế của "${item.productName}" không được lớn hơn số lượng hệ thống.`
        );
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

      await loadData();
      setMessage('Đã gửi báo cáo và đang chờ Quản lý duyệt.');
    } catch (err) {
      setError(
        err.response?.data?.message ||
          'Không gửi được báo cáo kết ca.'
      );
    } finally {
      setSubmitting(false);
    }
  }

  async function loadReportDetail(reportId) {
    setLoadingReport(true);
    setError('');
    setSelectedReport(null);
    setIsDetailOpen(true);

    try {
      const response = await axios.get(
        `/api/ShiftClosing/my-reports/${reportId}`
      );
      setSelectedReport(normalizeReport(response.data));
    } catch (err) {
      setError(
        err.response?.data?.message ||
          'Không tải được chi tiết báo cáo.'
      );
      setSelectedReport(null);
      setIsDetailOpen(false);
    } finally {
      setLoadingReport(false);
    }
  }

  function closeDetailModal() {
    if (loadingReport) return;
    setIsDetailOpen(false);
    setSelectedReport(null);
  }

  useEffect(() => {
    loadData();
  }, []);

  useEffect(() => {
    setCurrentPage(1);
  }, [keyword, statusFilter]);

  useEffect(() => {
    if (currentPage > totalPages) {
      setCurrentPage(totalPages);
    }
  }, [currentPage, totalPages]);

  useEffect(() => {
    if (!isDetailOpen) return undefined;

    const previousOverflow = document.body.style.overflow;
    document.body.style.overflow = 'hidden';

    const handleKeyDown = (event) => {
      if (event.key === 'Escape') closeDetailModal();
    };

    window.addEventListener('keydown', handleKeyDown);

    return () => {
      document.body.style.overflow = previousOverflow;
      window.removeEventListener('keydown', handleKeyDown);
    };
  }, [isDetailOpen, loadingReport]);

  const canSubmit = Boolean(shiftInfo?.canSubmit) && items.length > 0;
  const isRejected = shiftInfo?.reportStatus === 'REJECTED';

  return (
    <div className="staff-closing-page">
      <div className="staff-closing-hero">
        <div>
          <p className="staff-eyebrow">Báo cáo cuối ca</p>
          <h2>Báo cáo kết ca</h2>
          <span>
            Kiểm đếm số lượng hàng còn lại tại quầy sau ca làm.
          </span>
        </div>

        <button type="button" onClick={loadData} disabled={loading}>
          {loading ? 'Đang tải...' : 'Làm mới'}
        </button>
      </div>

      {error && <div className="staff-status error">{error}</div>}
      {message && <div className="staff-status success">{message}</div>}

      {isRejected && (
        <div className="staff-status error">
          Báo cáo đã bị từ chối
          {shiftInfo?.rejectReason
            ? `: ${shiftInfo.rejectReason}`
            : '. Vui lòng kiểm tra và gửi lại.'}
        </div>
      )}

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
                <strong>
                  {shiftInfo.startTime} - {shiftInfo.endTime}
                </strong>
              </div>

              <div
                className={`staff-status-box status-${getStatusClass(
                  shiftInfo.reportStatus
                )}`}
              >
                <span>Trạng thái báo cáo</span>
                <strong
                  className={`staff-closing-status ${getStatusClass(
                    shiftInfo.reportStatus
                  )}`}
                >
                  {getStatusLabel(shiftInfo.reportStatus)}
                </strong>
              </div>
            </div>
          ) : (
            <div className="staff-empty">
              Hôm nay chưa có ca làm cần báo cáo.
            </div>
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
              {isRejected
                ? 'Chỉnh sửa và gửi lại báo cáo'
                : shiftInfo?.alreadyReported
                  ? 'Ca này đã gửi báo cáo'
                  : 'Nhập số lượng thực tế cuối ca'}
            </h3>
          </div>
        </div>

        {shiftInfo?.alreadyReported ? (
          <div className="staff-empty">
            {shiftInfo.submitBlockReason ||
              'Ca này đã gửi báo cáo kết ca.'}
          </div>
        ) : !shiftInfo?.canSubmit ? (
          <div className="staff-empty">
            {shiftInfo?.submitBlockReason ||
              'Bạn chưa đủ điều kiện báo cáo kết ca.'}
          </div>
        ) : (
          <>
            {items.length === 0 ? (
              <div className="staff-empty">
                Quầy hiện chưa có mặt hàng đang kinh doanh.
              </div>
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
                      const difference =
                        Number(item.systemCount || 0) -
                        Number(item.actualCount || 0);

                      return (
                        <tr key={item.productId}>
                          <td>
                            <strong>{item.productName}</strong>
                          </td>
                          <td>{item.productCode || '—'}</td>
                          <td>{item.unit}</td>
                          <td className="text-right">
                            {formatNumber(item.systemCount)}
                          </td>
                          <td className="text-right">
                            <input
                              type="number"
                              min="0"
                              max={item.systemCount}
                              value={item.actualCount}
                              onChange={(event) =>
                                updateActualCount(
                                  item.productId,
                                  event.target.value
                                )
                              }
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
                maxLength={255}
              />
            </div>

            <div className="staff-closing-actions">
              <button
                type="button"
                onClick={submitReport}
                disabled={!canSubmit || submitting}
              >
                {submitting
                  ? 'Đang gửi...'
                  : isRejected
                    ? 'Gửi lại báo cáo'
                    : 'Gửi báo cáo kết ca'}
              </button>
            </div>
          </>
        )}
      </section>

      <section className="staff-closing-card">
        <div className="staff-section-head staff-report-history-head">
          <div>
            <p className="staff-eyebrow">Lịch sử</p>
            <h3>Báo cáo đã gửi</h3>
          </div>

          <span className="staff-report-count">
            {formatNumber(filteredReports.length)} báo cáo
          </span>
        </div>

        <div className="staff-report-toolbar">
          <div className="staff-report-search-wrap">
            <span aria-hidden="true">⌕</span>
            <input
              type="search"
              value={keyword}
              onChange={(event) => setKeyword(event.target.value)}
              placeholder="Tìm mã báo cáo, ca làm, ngày gửi..."
              aria-label="Tìm kiếm báo cáo kết ca"
            />

            {keyword && (
              <button
                type="button"
                onClick={() => setKeyword('')}
                aria-label="Xóa nội dung tìm kiếm"
              >
                ×
              </button>
            )}
          </div>

          <select
            value={statusFilter}
            onChange={(event) => setStatusFilter(event.target.value)}
            aria-label="Lọc báo cáo theo trạng thái"
          >
            <option value="ALL">Tất cả trạng thái</option>
            <option value="PENDING">Chờ duyệt</option>
            <option value="APPROVED">Đã duyệt</option>
            <option value="REJECTED">Bị từ chối</option>
          </select>
        </div>

        {reports.length === 0 ? (
          <div className="staff-empty">
            Chưa có báo cáo kết ca nào.
          </div>
        ) : filteredReports.length === 0 ? (
          <div className="staff-empty">
            Không tìm thấy báo cáo phù hợp với điều kiện lọc.
          </div>
        ) : (
          <>
            <div className="staff-report-list">
              {paginatedReports.map((report) => (
                <button
                  type="button"
                  key={report.id}
                  className={`staff-report-card status-${getStatusClass(
                    report.status
                  )}`}
                  onClick={() => loadReportDetail(report.id)}
                >
                  <div>
                    <strong>#{report.id}</strong>
                    <span>{report.reportDate}</span>
                  </div>

                  <p>
                    {report.shiftName || 'Ca làm'} ·{' '}
                    <span
                      className={`staff-report-status ${getStatusClass(
                        report.status
                      )}`}
                    >
                      {getStatusLabel(report.status)}
                    </span>
                  </p>

                  <small>
                    Hệ thống {formatNumber(report.totalSystemCount)} ·
                    Thực tế {formatNumber(report.totalActualCount)} ·
                    Lệch {formatNumber(report.totalDifference)}
                  </small>

                  <span className="staff-report-view-link">
                    Xem chi tiết →
                  </span>
                </button>
              ))}
            </div>

            <div className="staff-report-pagination">
              <span>
                Hiển thị {firstVisibleReport}–{lastVisibleReport} trong{' '}
                {filteredReports.length} báo cáo
              </span>

              <div>
                <button
                  type="button"
                  onClick={() =>
                    setCurrentPage((page) => Math.max(1, page - 1))
                  }
                  disabled={safeCurrentPage === 1}
                >
                  ← Trước
                </button>

                <strong>
                  Trang {safeCurrentPage}/{totalPages}
                </strong>

                <button
                  type="button"
                  onClick={() =>
                    setCurrentPage((page) =>
                      Math.min(totalPages, page + 1)
                    )
                  }
                  disabled={safeCurrentPage === totalPages}
                >
                  Sau →
                </button>
              </div>
            </div>
          </>
        )}
      </section>

      {isDetailOpen && (
        <div
          className="staff-report-modal-overlay"
          role="presentation"
          onMouseDown={(event) => {
            if (event.target === event.currentTarget) {
              closeDetailModal();
            }
          }}
        >
          <section
            className="staff-report-modal"
            role="dialog"
            aria-modal="true"
            aria-labelledby="staff-report-modal-title"
          >
            <header className="staff-report-modal-header">
              <div>
                <p className="staff-eyebrow">Chi tiết báo cáo</p>
                <h3 id="staff-report-modal-title">
                  {selectedReport
                    ? `Báo cáo #${selectedReport.id}`
                    : 'Đang tải báo cáo'}
                </h3>
              </div>

              <button
                type="button"
                onClick={closeDetailModal}
                disabled={loadingReport}
                aria-label="Đóng chi tiết báo cáo"
              >
                ×
              </button>
            </header>

            <div className="staff-report-modal-body">
              {loadingReport ? (
                <div className="staff-empty">
                  Đang tải chi tiết báo cáo...
                </div>
              ) : selectedReport ? (
                <>
                  <div className="staff-report-modal-info">
                    <div>
                      <span>Ca làm</span>
                      <strong>{selectedReport.shiftName || '—'}</strong>
                    </div>
                    <div>
                      <span>Ngày làm</span>
                      <strong>{selectedReport.workDate || '—'}</strong>
                    </div>
                    <div>
                      <span>Ngày gửi</span>
                      <strong>{selectedReport.reportDate || '—'}</strong>
                    </div>
                    <div>
                      <span>Trạng thái</span>
                      <strong
                        className={`staff-closing-status ${getStatusClass(
                          selectedReport.status
                        )}`}
                      >
                        {getStatusLabel(selectedReport.status)}
                      </strong>
                    </div>
                    <div>
                      <span>Người xử lý</span>
                      <strong>
                        {selectedReport.reviewerName || 'Chưa xử lý'}
                      </strong>
                    </div>
                    <div>
                      <span>Thời gian xử lý</span>
                      <strong>{selectedReport.reviewedAt || '—'}</strong>
                    </div>
                  </div>

                  <div className="staff-report-modal-summary">
                    <div>
                      <span>Hệ thống</span>
                      <strong>
                        {formatNumber(selectedReport.totalSystemCount)}
                      </strong>
                    </div>
                    <div>
                      <span>Thực tế</span>
                      <strong>
                        {formatNumber(selectedReport.totalActualCount)}
                      </strong>
                    </div>
                    <div>
                      <span>Chênh lệch</span>
                      <strong>
                        {formatNumber(selectedReport.totalDifference)}
                      </strong>
                    </div>
                  </div>

                  {selectedReport.status === 'REJECTED' && (
                    <div className="staff-status error">
                      Lý do từ chối:{' '}
                      {selectedReport.rejectReason || 'Không có lý do.'}
                    </div>
                  )}

                  <div className="staff-report-modal-note">
                    <span>Ghi chú</span>
                    <p>{selectedReport.note || 'Không có ghi chú.'}</p>
                  </div>

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
                        {selectedReport.items.map((item) => (
                          <tr key={item.productId}>
                            <td>
                              <strong>{item.productName}</strong>
                            </td>
                            <td>{item.productCode || '—'}</td>
                            <td>{item.unit}</td>
                            <td className="text-right">
                              {formatNumber(item.systemCount)}
                            </td>
                            <td className="text-right">
                              {formatNumber(item.actualCount)}
                            </td>
                            <td className="text-right">
                              {formatNumber(item.difference)}
                            </td>
                          </tr>
                        ))}
                      </tbody>
                    </table>
                  </div>
                </>
              ) : null}
            </div>

            <footer className="staff-report-modal-footer">
              <button type="button" onClick={closeDetailModal}>
                Đóng
              </button>
            </footer>
          </section>
        </div>
      )}

    </div>
  );
}