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
      return 'Chờ duyệt';
    case 'APPROVED':
      return 'Đã duyệt';
    case 'REJECTED':
      return 'Đã từ chối';
    default:
      return 'Không xác định';
  }
}

const REPORTS_PER_PAGE = 6;

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

function normalizeReport(report) {
  const rawItems = getValue(report, ['items', 'Items'], []);

  return {
    id: Number(getValue(report, ['id', 'Id'], 0)),
    branchId: Number(getValue(report, ['branchId', 'BranchId'], 0)),
    branchName: getValue(
      report,
      ['branchName', 'BranchName'],
      'Chưa rõ cơ sở'
    ),
    userId: Number(getValue(report, ['userId', 'UserId'], 0)),
    staffName: getValue(
      report,
      ['staffName', 'StaffName'],
      'Chưa rõ nhân viên'
    ),
    scheduleId: Number(
      getValue(report, ['scheduleId', 'ScheduleId'], 0)
    ),
    shiftName: getValue(report, ['shiftName', 'ShiftName'], 'Ca làm'),
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
      ? rawItems.map((item) => ({
          productId: Number(
            getValue(item, ['productId', 'ProductId'], 0)
          ),
          productCode: getValue(
            item,
            ['productCode', 'ProductCode'],
            ''
          ),
          productName: getValue(
            item,
            ['productName', 'ProductName'],
            'Chưa rõ sản phẩm'
          ),
          unit: getValue(item, ['unit', 'Unit'], 'Cái') || 'Cái',
          systemCount: Number(
            getValue(item, ['systemCount', 'SystemCount'], 0)
          ),
          actualCount: Number(
            getValue(item, ['actualCount', 'ActualCount'], 0)
          ),
          difference: Number(
            getValue(item, ['difference', 'Difference'], 0)
          ),
        }))
      : [],
  };
}

export function ShiftClosingManagementTab({
  currentUser,
  branches = [],
}) {
  const role = String(
    currentUser?.role || currentUser?.roleName || ''
  ).toUpperCase();

  const isAdmin =
    role.includes('ADMIN') ||
    role.includes('QUẢN TRỊ') ||
    role.includes('QUAN TRI');

  const isManager =
    role.includes('MANAGER') ||
    role.includes('QUẢN LÝ') ||
    role.includes('QUAN LY');

  const [selectedBranchId, setSelectedBranchId] = useState(
    isAdmin ? 'ALL' : String(currentUser?.branchId || '')
  );
  const [reports, setReports] = useState([]);
  const [selectedReport, setSelectedReport] = useState(null);
  const [isDetailOpen, setIsDetailOpen] = useState(false);
  const [keyword, setKeyword] = useState('');
  const [statusFilter, setStatusFilter] = useState('ALL');
  const [currentPage, setCurrentPage] = useState(1);
  const [loading, setLoading] = useState(false);
  const [loadingDetail, setLoadingDetail] = useState(false);
  const [processing, setProcessing] = useState(false);
  const [error, setError] = useState('');
  const [message, setMessage] = useState('');

const filteredReports = useMemo(() => {
  const searchText = keyword.trim().toLowerCase();

  return reports
    .filter((report) => {
      const matchesStatus =
        statusFilter === 'ALL' || report.status === statusFilter;

      if (!matchesStatus) return false;
      if (!searchText) return true;

      return `#${report.id} ${report.id} ${report.staffName} ${report.branchName} ${report.shiftName} ${report.workDate} ${report.reportDate} ${getStatusLabel(report.status)}`
        .toLowerCase()
        .includes(searchText);
    })
    .sort((first, second) => second.id - first.id);
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

  async function loadReports() {
    setLoading(true);
    setError('');
    setSelectedReport(null);
    setIsDetailOpen(false);

    try {
      const params = {};

      if (selectedBranchId && selectedBranchId !== 'ALL') {
        params.branchId = selectedBranchId;
      }

      const response = await axios.get(
        '/api/ShiftClosing/reports',
        { params }
      );
      const data = Array.isArray(response.data)
        ? response.data
        : [];

      setReports(data.map(normalizeReport));
    } catch (err) {
      setError(
        err.response?.data?.message ||
          'Không tải được danh sách báo cáo kết ca.'
      );
      setReports([]);
    } finally {
      setLoading(false);
    }
  }

  async function loadReportDetail(reportId) {
    setLoadingDetail(true);
    setError('');
    setSelectedReport(null);
    setIsDetailOpen(true);

    try {
      const params = {};

      if (selectedBranchId && selectedBranchId !== 'ALL') {
        params.branchId = selectedBranchId;
      }

      const response = await axios.get(
        `/api/ShiftClosing/reports/${reportId}`,
        { params }
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
      setLoadingDetail(false);
    }
  }

  function closeDetailModal() {
    if (processing || loadingDetail) return;
    setIsDetailOpen(false);
    setSelectedReport(null);
  }

  async function approveReport() {
    if (!selectedReport || selectedReport.status !== 'PENDING') return;

    const confirmed = window.confirm(
      `Duyệt báo cáo #${selectedReport.id} của ${selectedReport.staffName}?\nSau khi duyệt, tồn quầy sẽ được cập nhật theo số lượng thực tế.`
    );

    if (!confirmed) return;

    setProcessing(true);
    setError('');
    setMessage('');

    try {
      const response = await axios.put(
        `/api/ShiftClosing/reports/${selectedReport.id}/approve`
      );

      setIsDetailOpen(false);
      setSelectedReport(null);
      await loadReports();
      setMessage(
        response.data?.message ||
          'Duyệt báo cáo thành công.'
      );
    } catch (err) {
      setError(
        err.response?.data?.message ||
          'Không duyệt được báo cáo.'
      );
    } finally {
      setProcessing(false);
    }
  }

  async function rejectReport() {
    if (!selectedReport || selectedReport.status !== 'PENDING') return;

    const reason = window.prompt(
      `Nhập lý do từ chối báo cáo #${selectedReport.id}:`
    );

    if (reason === null) return;

    if (!reason.trim()) {
      setError('Vui lòng nhập lý do từ chối.');
      return;
    }

    setProcessing(true);
    setError('');
    setMessage('');

    try {
      const response = await axios.put(
        `/api/ShiftClosing/reports/${selectedReport.id}/reject`,
        { reason: reason.trim() }
      );

      setIsDetailOpen(false);
      setSelectedReport(null);
      await loadReports();
      setMessage(
        response.data?.message ||
          'Đã từ chối báo cáo.'
      );
    } catch (err) {
      setError(
        err.response?.data?.message ||
          'Không từ chối được báo cáo.'
      );
    } finally {
      setProcessing(false);
    }
  }

  useEffect(() => {
    loadReports();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [selectedBranchId]);

  useEffect(() => {
    setCurrentPage(1);
  }, [keyword, statusFilter, selectedBranchId]);

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
  }, [isDetailOpen, loadingDetail, processing]);

  const totalReports = filteredReports.length;
  const pendingReports = filteredReports.filter(
    (report) => report.status === 'PENDING'
  ).length;
  const totalSystem = filteredReports.reduce(
    (sum, report) => sum + report.totalSystemCount,
    0
  );
  const totalActual = filteredReports.reduce(
    (sum, report) => sum + report.totalActualCount,
    0
  );
  const totalDifference = filteredReports.reduce(
    (sum, report) => sum + report.totalDifference,
    0
  );

  return (
    <div className="sd-closing-page">
      <div className="sd-closing-hero">
        <div>
          <p className="sd-eyebrow">Báo cáo kết ca</p>
          <h2>
            {isAdmin
              ? 'Báo cáo kết ca toàn hệ thống'
              : 'Duyệt báo cáo kết ca cơ sở'}
          </h2>
          <span>
            
          </span>
        </div>

        <button type="button" onClick={loadReports} disabled={loading}>
          {loading ? 'Đang tải...' : 'Làm mới'}
        </button>
      </div>

      {error && (
        <div className="sd-status sd-status-error">{error}</div>
      )}
      {message && (
        <div className="sd-status sd-status-success">{message}</div>
      )}

      <div className="sd-closing-summary">
        <div>
          <span>Số báo cáo</span>
          <strong>{formatNumber(totalReports)}</strong>
        </div>

        <div>
          <span>Đang chờ duyệt</span>
          <strong>{formatNumber(pendingReports)}</strong>
        </div>

        <div>
          <span>Tổng hệ thống</span>
          <strong>{formatNumber(totalSystem)}</strong>
        </div>

        <div>
          <span>Tổng thực tế</span>
          <strong>{formatNumber(totalActual)}</strong>
        </div>

        <div>
          <span>Tổng lệch</span>
          <strong>{formatNumber(totalDifference)}</strong>
        </div>
      </div>

      <div className="sd-closing-filter sd-closing-filter-advanced">
        <div className="sd-closing-search-wrap">
          <span aria-hidden="true">⌕</span>
          <input
            type="search"
            value={keyword}
            onChange={(event) => setKeyword(event.target.value)}
            placeholder="Tìm mã báo cáo, nhân viên, ca làm..."
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
          <option value="REJECTED">Đã từ chối</option>
        </select>

        {isAdmin ? (
          <select
            value={selectedBranchId}
            onChange={(event) =>
              setSelectedBranchId(event.target.value)
            }
            aria-label="Lọc báo cáo theo cơ sở"
          >
            <option value="ALL">Tất cả cơ sở</option>
            {branches.map((branch) => (
              <option key={branch.id} value={branch.id}>
                {branch.name}
              </option>
            ))}
          </select>
        ) : (
          <span className="sd-closing-branch">
            📍 {currentUser?.branchName || 'Cơ sở của tôi'}
          </span>
        )}
      </div>

      <div className="sd-closing-layout sd-closing-layout-list-only">
        <section className="sd-closing-card">
          <div className="sd-closing-card-head sd-closing-list-head">
            <div>
              <h3>Danh sách báo cáo</h3>
              <span>
                Báo cáo chờ duyệt được ưu tiên hiển thị trước.
              </span>
            </div>

            <strong>{formatNumber(filteredReports.length)} báo cáo</strong>
          </div>

          {loading ? (
            <div className="sd-closing-empty">
              Đang tải báo cáo...
            </div>
          ) : reports.length === 0 ? (
            <div className="sd-closing-empty">
              Chưa có báo cáo kết ca nào.
            </div>
          ) : filteredReports.length === 0 ? (
            <div className="sd-closing-empty">
              Không tìm thấy báo cáo phù hợp với điều kiện lọc.
            </div>
          ) : (
            <>
              <div className="sd-closing-list sd-closing-list-grid">
                {paginatedReports.map((report) => (
                  <button
                    key={report.id}
                    type="button"
                    className={`sd-closing-report status-${getStatusClass(
                      report.status
                    )}`}
                    onClick={() => loadReportDetail(report.id)}
                  >
                    <div>
                      <strong>
                        #{report.id} · {report.staffName}
                      </strong>
                      <span>{report.reportDate}</span>
                    </div>

                    <p>
                      {report.branchName} ·{' '}
                      <strong
                        className={`sd-closing-status ${getStatusClass(
                          report.status
                        )}`}
                      >
                        {getStatusLabel(report.status)}
                      </strong>
                    </p>

                    <small>
                      {report.shiftName || 'Ca làm'} · Hệ thống{' '}
                      {formatNumber(report.totalSystemCount)} · Thực tế{' '}
                      {formatNumber(report.totalActualCount)} · Lệch{' '}
                      {formatNumber(report.totalDifference)}
                    </small>

                    <span className="sd-closing-view-link">
                      Xem chi tiết →
                    </span>
                  </button>
                ))}
              </div>

              <div className="sd-closing-pagination">
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
      </div>

      {isDetailOpen && (
        <div
          className="sd-closing-modal-overlay"
          role="presentation"
          onMouseDown={(event) => {
            if (event.target === event.currentTarget) {
              closeDetailModal();
            }
          }}
        >
          <section
            className="sd-closing-modal"
            role="dialog"
            aria-modal="true"
            aria-labelledby="sd-closing-modal-title"
          >
            <header className="sd-closing-modal-header">
              <div>
                <p className="sd-eyebrow">Chi tiết báo cáo kết ca</p>
                <h3 id="sd-closing-modal-title">
                  {selectedReport
                    ? `Báo cáo #${selectedReport.id}`
                    : 'Đang tải báo cáo'}
                </h3>
              </div>

              <button
                type="button"
                onClick={closeDetailModal}
                disabled={loadingDetail || processing}
                aria-label="Đóng chi tiết báo cáo"
              >
                ×
              </button>
            </header>

            <div className="sd-closing-modal-body">
              {loadingDetail ? (
                <div className="sd-closing-empty">
                  Đang tải chi tiết...
                </div>
              ) : selectedReport ? (
                <>
                  <div className="sd-closing-info-grid">
                    <div>
                      <span>Nhân viên</span>
                      <strong>{selectedReport.staffName}</strong>
                    </div>

                    <div>
                      <span>Cơ sở</span>
                      <strong>{selectedReport.branchName}</strong>
                    </div>

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
                      <strong>{selectedReport.reportDate}</strong>
                    </div>

                    <div
                      className={`sd-closing-status-box status-${getStatusClass(
                        selectedReport.status
                      )}`}
                    >
                      <span>Trạng thái</span>
                      <strong
                        className={`sd-closing-status ${getStatusClass(
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

                  <div className="sd-closing-modal-summary">
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
                    <div className="sd-status sd-status-error">
                      Lý do từ chối:{' '}
                      {selectedReport.rejectReason || 'Không có lý do.'}
                    </div>
                  )}

                  <div className="sd-closing-modal-note">
                    <span>Ghi chú</span>
                    <p>{selectedReport.note || 'Không có ghi chú.'}</p>
                  </div>

                  <div className="sd-closing-table-wrap">
                    <table className="sd-closing-table">
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
                              <strong>
                                {formatNumber(item.difference)}
                              </strong>
                            </td>
                          </tr>
                        ))}
                      </tbody>
                    </table>
                  </div>
                </>
              ) : null}
            </div>

            <footer className="sd-closing-modal-footer">
              {isManager && selectedReport?.status === 'PENDING' ? (
                <>
                  <button
                    type="button"
                    className="sd-closing-approve-btn"
                    onClick={approveReport}
                    disabled={processing}
                  >
                    {processing ? 'Đang xử lý...' : 'Duyệt báo cáo'}
                  </button>

                  <button
                    type="button"
                    className="sd-closing-reject-btn"
                    onClick={rejectReport}
                    disabled={processing}
                  >
                    Từ chối
                  </button>
                </>
              ) : (
                <button
                  type="button"
                  className="sd-closing-close-btn"
                  onClick={closeDetailModal}
                >
                  Đóng
                </button>
              )}
            </footer>

            {isAdmin && selectedReport && (
              <div className="sd-closing-admin-note">
                Admin chỉ theo dõi; quyền duyệt thuộc về Manager của cơ sở.
              </div>
            )}
          </section>
        </div>
      )}

    </div>
  );
}