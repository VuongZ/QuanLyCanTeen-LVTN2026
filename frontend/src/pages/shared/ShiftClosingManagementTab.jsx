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

function normalizeReport(report) {
  const rawItems = getValue(report, ['items', 'Items'], []);

  return {
    id: Number(getValue(report, ['id', 'Id'], 0)),
    branchId: Number(getValue(report, ['branchId', 'BranchId'], 0)),
    branchName: getValue(report, ['branchName', 'BranchName'], 'Chưa rõ cơ sở'),
    userId: Number(getValue(report, ['userId', 'UserId'], 0)),
    staffName: getValue(report, ['staffName', 'StaffName'], 'Chưa rõ nhân viên'),
    scheduleId: Number(getValue(report, ['scheduleId', 'ScheduleId'], 0)),
    shiftName: getValue(report, ['shiftName', 'ShiftName'], 'Ca làm'),
    workDate: getValue(report, ['workDate', 'WorkDate'], ''),
    reportDate: getValue(report, ['reportDate', 'ReportDate'], ''),
    itemCount: Number(getValue(report, ['itemCount', 'ItemCount'], 0)),
    totalSystemCount: Number(getValue(report, ['totalSystemCount', 'TotalSystemCount'], 0)),
    totalActualCount: Number(getValue(report, ['totalActualCount', 'TotalActualCount'], 0)),
    totalDifference: Number(getValue(report, ['totalDifference', 'TotalDifference'], 0)),
    note: getValue(report, ['note', 'Note'], ''),
    items: Array.isArray(rawItems)
      ? rawItems.map((item) => ({
          productId: Number(getValue(item, ['productId', 'ProductId'], 0)),
          productCode: getValue(item, ['productCode', 'ProductCode'], ''),
          productName: getValue(item, ['productName', 'ProductName'], 'Chưa rõ sản phẩm'),
          unit: getValue(item, ['unit', 'Unit'], 'Cái') || 'Cái',
          systemCount: Number(getValue(item, ['systemCount', 'SystemCount'], 0)),
          actualCount: Number(getValue(item, ['actualCount', 'ActualCount'], 0)),
          difference: Number(getValue(item, ['difference', 'Difference'], 0)),
        }))
      : [],
  };
}

export function ShiftClosingManagementTab({ currentUser, branches = [] }) {
  const role = String(currentUser?.role || currentUser?.roleName || '').toUpperCase();
  const isAdmin = role.includes('ADMIN') || role.includes('QUẢN TRỊ') || role.includes('QUAN TRI');

  const [selectedBranchId, setSelectedBranchId] = useState(isAdmin ? 'ALL' : String(currentUser?.branchId || ''));
  const [reports, setReports] = useState([]);
  const [selectedReport, setSelectedReport] = useState(null);
  const [keyword, setKeyword] = useState('');
  const [loading, setLoading] = useState(false);
  const [loadingDetail, setLoadingDetail] = useState(false);
  const [error, setError] = useState('');

  const filteredReports = useMemo(() => {
    const searchText = keyword.trim().toLowerCase();

    if (!searchText) return reports;

    return reports.filter((report) =>
      `${report.staffName} ${report.branchName} ${report.shiftName} ${report.reportDate}`
        .toLowerCase()
        .includes(searchText)
    );
  }, [reports, keyword]);

  async function loadReports() {
    setLoading(true);
    setError('');
    setSelectedReport(null);

    try {
      const params = {};

      if (selectedBranchId && selectedBranchId !== 'ALL') {
        params.branchId = selectedBranchId;
      }

      const response = await axios.get('/api/ShiftClosing/reports', { params });
      const data = Array.isArray(response.data) ? response.data : [];

      setReports(data.map(normalizeReport));
    } catch (err) {
      setError(err.response?.data?.message || 'Không tải được danh sách báo cáo kết ca.');
      setReports([]);
    } finally {
      setLoading(false);
    }
  }

  async function loadReportDetail(reportId) {
    setLoadingDetail(true);
    setError('');

    try {
      const params = {};

      if (selectedBranchId && selectedBranchId !== 'ALL') {
        params.branchId = selectedBranchId;
      }

      const response = await axios.get(`/api/ShiftClosing/reports/${reportId}`, { params });
      setSelectedReport(normalizeReport(response.data));
    } catch (err) {
      setError(err.response?.data?.message || 'Không tải được chi tiết báo cáo.');
      setSelectedReport(null);
    } finally {
      setLoadingDetail(false);
    }
  }

  useEffect(() => {
    loadReports();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [selectedBranchId]);

  const totalReports = filteredReports.length;
  const totalSystem = filteredReports.reduce((sum, report) => sum + report.totalSystemCount, 0);
  const totalActual = filteredReports.reduce((sum, report) => sum + report.totalActualCount, 0);
  const totalDifference = filteredReports.reduce((sum, report) => sum + report.totalDifference, 0);

  return (
    <div className="sd-closing-page">
      <div className="sd-closing-hero">
        <div>
          <p className="sd-eyebrow">Báo cáo kết ca</p>
          <h2>{isAdmin ? 'Báo cáo kết ca toàn hệ thống' : 'Báo cáo kết ca cơ sở'}</h2>
          <span>Theo dõi số lượng hệ thống, số lượng thực tế và chênh lệch cuối ca.</span>
        </div>

        <button type="button" onClick={loadReports} disabled={loading}>
          {loading ? 'Đang tải...' : 'Làm mới'}
        </button>
      </div>

      {error && <div className="sd-status sd-status-error">{error}</div>}

      <div className="sd-closing-summary">
        <div>
          <span>Số báo cáo</span>
          <strong>{formatNumber(totalReports)}</strong>
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

      <div className="sd-closing-filter">
        <input
          value={keyword}
          onChange={(event) => setKeyword(event.target.value)}
          placeholder="Tìm nhân viên, cơ sở, ca làm..."
        />

        {isAdmin ? (
          <select
            value={selectedBranchId}
            onChange={(event) => setSelectedBranchId(event.target.value)}
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

      <div className="sd-closing-layout">
        <section className="sd-closing-card">
          <div className="sd-closing-card-head">
            <h3>Danh sách báo cáo</h3>
          </div>

          {loading ? (
            <div className="sd-closing-empty">Đang tải báo cáo...</div>
          ) : filteredReports.length === 0 ? (
            <div className="sd-closing-empty">Chưa có báo cáo kết ca nào.</div>
          ) : (
            <div className="sd-closing-list">
              {filteredReports.map((report) => (
                <button
                  key={report.id}
                  type="button"
                  className={`sd-closing-report ${selectedReport?.id === report.id ? 'active' : ''}`}
                  onClick={() => loadReportDetail(report.id)}
                >
                  <div>
                    <strong>#{report.id} · {report.staffName}</strong>
                    <span>{report.reportDate}</span>
                  </div>

                  <p>{report.branchName}</p>

                  <small>
                    {report.shiftName || 'Ca làm'} · Hệ thống {formatNumber(report.totalSystemCount)} · Thực tế {formatNumber(report.totalActualCount)} · Lệch {formatNumber(report.totalDifference)}
                  </small>
                </button>
              ))}
            </div>
          )}
        </section>

        <section className="sd-closing-card">
          <div className="sd-closing-card-head">
            <h3>Chi tiết báo cáo</h3>
          </div>

          {loadingDetail ? (
            <div className="sd-closing-empty">Đang tải chi tiết...</div>
          ) : !selectedReport ? (
            <div className="sd-closing-empty">Chọn một báo cáo để xem chi tiết.</div>
          ) : (
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
                  <span>Ngày báo cáo</span>
                  <strong>{selectedReport.reportDate}</strong>
                </div>

                <div>
                  <span>Ghi chú</span>
                  <strong>{selectedReport.note || '—'}</strong>
                </div>
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
                        <td className="text-right">{formatNumber(item.systemCount)}</td>
                        <td className="text-right">{formatNumber(item.actualCount)}</td>
                        <td className="text-right">
                          <strong>{formatNumber(item.difference)}</strong>
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            </>
          )}
        </section>
      </div>
    </div>
  );
}