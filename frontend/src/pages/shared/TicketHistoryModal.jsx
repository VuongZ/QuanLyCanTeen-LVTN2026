import { useEffect, useMemo, useState } from 'react';
import axios from 'axios';


function normalizeSearchText(value = '') {
  return String(value ?? '')
    .normalize('NFD')
    .replace(/[\u0300-\u036f]/g, '')
    .trim()
    .toLowerCase();
}

function formatMoney(value) {
  return new Intl.NumberFormat('vi-VN').format(Number(value || 0));
}

function formatNumber(value) {
  return new Intl.NumberFormat('vi-VN', {
    maximumFractionDigits: 2,
  }).format(Number(value || 0));
}

function getValue(item, keys, fallback = '') {
  if (!item) return fallback;

  for (const key of keys) {
    if (item[key] !== undefined && item[key] !== null) {
      return item[key];
    }

    const realKey = Object.keys(item).find(
      (itemKey) => itemKey.toLowerCase() === key.toLowerCase()
    );

    if (realKey && item[realKey] !== undefined && item[realKey] !== null) {
      return item[realKey];
    }
  }

  return fallback;
}

function normalizeTicket(item, type) {
  return {
    id: getValue(item, ['id', 'Id'], ''),
    branchId: getValue(item, ['branchId', 'BranchId'], ''),
    branchName: getValue(item, ['branchName', 'BranchName'], 'Chưa rõ cơ sở'),
    managerName: getValue(item, ['managerName', 'ManagerName'], 'Chưa rõ người thực hiện'),
    supplierName: getValue(item, ['supplierName', 'SupplierName'], ''),
    invoiceCode: getValue(item, ['invoiceCode', 'InvoiceCode'], ''),
    invoiceDate: getValue(item, ['invoiceDate', 'InvoiceDate'], ''),
    importDate: getValue(item, ['importDate', 'ImportDate'], ''),
    exportDate: getValue(item, ['exportDate', 'ExportDate'], ''),
    totalAmount: Number(getValue(item, ['totalAmount', 'TotalAmount'], 0) || 0),
    totalQuantity: Number(getValue(item, ['totalQuantity', 'TotalQuantity'], 0) || 0),
    itemCount: Number(getValue(item, ['itemCount', 'ItemCount'], 0) || 0),
    scheduleId: getValue(item, ['scheduleId', 'ScheduleId'], ''),
    shiftName: getValue(item, ['shiftName', 'ShiftName'], ''),
    workDate: getValue(item, ['workDate', 'WorkDate'], ''),
    shiftTime: getValue(item, ['shiftTime', 'ShiftTime'], ''),
    note: getValue(item, ['note', 'Note'], ''),
    type,
  };
}

function normalizeDetail(item, type) {
  const ticket = normalizeTicket(item, type);
  const rawItems = getValue(item, ['items', 'Items'], []);

  return {
    ...ticket,
    items: Array.isArray(rawItems)
      ? rawItems.map((detail) => ({
          productId: getValue(detail, ['productId', 'ProductId'], ''),
          productCode: getValue(detail, ['productCode', 'ProductCode'], ''),
          productName: getValue(detail, ['productName', 'ProductName'], 'Chưa rõ sản phẩm'),
          unit: getValue(detail, ['unit', 'Unit'], 'Cái') || 'Cái',
          quantity: Number(getValue(detail, ['quantity', 'Quantity'], 0) || 0),
          unitPrice: Number(getValue(detail, ['unitPrice', 'UnitPrice'], 0) || 0),
          lineTotal: Number(getValue(detail, ['lineTotal', 'LineTotal'], 0) || 0),
        }))
      : [],
  };
}

export function TicketHistoryModal({
  open,
  onClose,
  type,
  branchId,
}) {
  const [tickets, setTickets] = useState([]);
  const [selectedTicket, setSelectedTicket] = useState(null);
  const [loadingList, setLoadingList] = useState(false);
  const [loadingDetail, setLoadingDetail] = useState(false);
  const [error, setError] = useState('');
  const [search, setSearch] = useState('');

  const config = useMemo(() => {
    if (type === 'inventoryImport') {
      return {
        title: 'Phiếu nhập kho',
        listUrl: '/api/KhoImport/inventory-tickets',
        detailUrl: (id) => `/api/KhoImport/inventory-tickets/${id}`,
        emptyText: 'Chưa có phiếu nhập kho nào.',
      };
    }

    return {
      title: 'Phiếu xuất ra quầy',
      listUrl: '/api/KhoExport/front-stock-tickets',
      detailUrl: (id) => `/api/KhoExport/front-stock-tickets/${id}`,
      emptyText: 'Chưa có phiếu xuất ra quầy nào.',
    };
  }, [type]);

  async function loadTickets() {
    if (!open) return;

    setLoadingList(true);
    setError('');
    setSelectedTicket(null);

    try {
      const params = {};

      if (branchId && branchId !== 'ALL') {
        params.branchId = branchId;
      }

      const response = await axios.get(config.listUrl, { params });
      const data = Array.isArray(response.data) ? response.data : [];

      setTickets(data.map((item) => normalizeTicket(item, type)));
    } catch (err) {
      setError(err.response?.data?.message || `Không tải được danh sách ${config.title.toLowerCase()}.`);
      setTickets([]);
    } finally {
      setLoadingList(false);
    }
  }

  async function loadDetail(ticketId) {
    setLoadingDetail(true);
    setError('');

    try {
      const params = {};

      if (branchId && branchId !== 'ALL') {
        params.branchId = branchId;
      }

      const response = await axios.get(config.detailUrl(ticketId), { params });
      setSelectedTicket(normalizeDetail(response.data, type));
    } catch (err) {
      setError(err.response?.data?.message || 'Không tải được chi tiết phiếu.');
      setSelectedTicket(null);
    } finally {
      setLoadingDetail(false);
    }
  }


  const filteredTickets = useMemo(() => {
    const keyword = normalizeSearchText(search);

    if (!keyword) {
      return tickets;
    }

    return tickets.filter((ticket) => {
      const searchableText = [
        ticket.id,
        `#${ticket.id}`,
        ticket.branchName,
        ticket.managerName,
        ticket.supplierName,
        ticket.invoiceCode,
        ticket.invoiceDate,
        ticket.importDate,
        ticket.exportDate,
        ticket.shiftName,
        ticket.workDate,
        ticket.shiftTime,
        ticket.note,
        ticket.itemCount,
        ticket.totalQuantity,
        ticket.totalAmount,
      ]
        .filter((value) => value !== null && value !== undefined)
        .join(' ');

      return normalizeSearchText(searchableText).includes(keyword);
    });
  }, [tickets, search]);

  useEffect(() => {
    if (open) {
      loadTickets();
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [open, type, branchId]);


  useEffect(() => {
    if (open) {
      setSearch('');
    }
  }, [open, type, branchId]);

  if (!open) return null;

  const isImport = type === 'inventoryImport';

  return (
    <div className="sd-ticket-overlay">
      <div className="sd-ticket-modal">
        <div className="sd-ticket-header">
          <div>
            <p className="sd-eyebrow">Lịch sử chứng từ</p>
            <h2>{config.title}</h2>
          </div>

          <button type="button" className="sd-ticket-close" onClick={onClose}>
            ✕
          </button>
        </div>

        {error && (
          <div className="sd-status sd-status-error sd-ticket-message">
            {error}
          </div>
        )}

        <div className="sd-ticket-layout">
          <div className="sd-ticket-list">
            <div className="sd-ticket-list-head">
              <strong>Danh sách phiếu</strong>
              <button type="button" onClick={loadTickets} disabled={loadingList}>
                {loadingList ? 'Đang tải...' : 'Làm mới'}
              </button>
            </div>

            <div className="sd-ticket-search">
              <span className="sd-ticket-search-icon">⌕</span>

              <input
                type="text"
                value={search}
                onChange={(event) => setSearch(event.target.value)}
                placeholder={
                  isImport
                    ? 'Tìm mã phiếu, nhà cung cấp, ngày nhập...'
                    : 'Tìm mã phiếu, cơ sở, ngày xuất...'
                }
              />

              {search && (
                <button
                  type="button"
                  className="sd-ticket-search-clear"
                  onClick={() => setSearch('')}
                  aria-label="Xóa nội dung tìm kiếm"
                  title="Xóa tìm kiếm"
                >
                  ×
                </button>
              )}
            </div>

            <div className="sd-ticket-result-count">
              Hiển thị {filteredTickets.length} / {tickets.length} phiếu
            </div>

            {loadingList ? (
              <div className="sd-ticket-empty">Đang tải danh sách phiếu...</div>
            ) : filteredTickets.length === 0 ? (
              <div className="sd-ticket-empty">
                {search ? 'Không tìm thấy phiếu phù hợp.' : config.emptyText}
              </div>
            ) : (
              filteredTickets.map((ticket) => (
                <button
                  key={ticket.id}
                  type="button"
                  className={`sd-ticket-card ${selectedTicket?.id === ticket.id ? 'active' : ''}`}
                  onClick={() => loadDetail(ticket.id)}
                >
                  <div>
                    <strong>#{ticket.id}</strong>
                    <span>{isImport ? ticket.importDate : ticket.exportDate}</span>
                  </div>

                  <p>{ticket.branchName}</p>

                  <small>
                    {ticket.itemCount} mặt hàng · Tổng SL {formatNumber(ticket.totalQuantity)}
                  </small>
                </button>
              ))
            )}
          </div>

          <div className="sd-ticket-detail">
            {!selectedTicket ? (
              <div className="sd-ticket-empty large">
                {loadingDetail ? 'Đang tải chi tiết...' : 'Chọn một phiếu để xem chi tiết.'}
              </div>
            ) : (
              <>
                <div className="sd-ticket-detail-head">
                  <div>
                    <p className="sd-eyebrow">
                      {isImport ? 'Chi tiết phiếu nhập' : 'Chi tiết phiếu xuất'}
                    </p>
                    <h3>Phiếu #{selectedTicket.id}</h3>
                  </div>

                  <span className="sd-ticket-pill">
                    {selectedTicket.itemCount} mặt hàng
                  </span>
                </div>

                <div className="sd-ticket-info-grid">
                  <div>
                    <span>Cơ sở</span>
                    <strong>{selectedTicket.branchName}</strong>
                  </div>

                  <div>
                    <span>Người thực hiện</span>
                    <strong>{selectedTicket.managerName}</strong>
                  </div>

                  {isImport ? (
                    <>
                      <div>
                        <span>Nhà cung cấp</span>
                        <strong>{selectedTicket.supplierName || '—'}</strong>
                      </div>

                      <div>
                        <span>Mã hóa đơn</span>
                        <strong>{selectedTicket.invoiceCode || '—'}</strong>
                      </div>

                      <div>
                        <span>Ngày hóa đơn</span>
                        <strong>{selectedTicket.invoiceDate || '—'}</strong>
                      </div>

                      <div>
                        <span>Tổng tiền</span>
                        <strong>{formatMoney(selectedTicket.totalAmount)} đ</strong>
                      </div>
                    </>
                  ) : (
                    <>
                      <div>
                        <span>Ca làm</span>
                        <strong>{selectedTicket.shiftName || '—'}</strong>
                      </div>

                      <div>
                        <span>Ngày làm</span>
                        <strong>{selectedTicket.workDate || '—'}</strong>
                      </div>

                      <div>
                        <span>Giờ ca</span>
                        <strong>{selectedTicket.shiftTime || '—'}</strong>
                      </div>

                    </>
                  )}

                  <div>
                    <span>Tổng số lượng</span>
                    <strong>{formatNumber(selectedTicket.totalQuantity)}</strong>
                  </div>

                  <div>
                    <span>Ghi chú</span>
                    <strong>{selectedTicket.note || '—'}</strong>
                  </div>
                </div>

                <div className="sd-ticket-table-wrap">
                  <table className="sd-ticket-table">
                    <thead>
                      <tr>
                        <th>Sản phẩm</th>
                        <th>Mã SP</th>
                        <th className="text-right">Số lượng</th>
                        <th>Đơn vị</th>
                        {isImport && <th className="text-right">Đơn giá</th>}
                        {isImport && <th className="text-right">Thành tiền</th>}
                      </tr>
                    </thead>

                    <tbody>
                      {selectedTicket.items.map((item, index) => (
                        <tr key={`${item.productId}-${index}`}>
                          <td>
                            <strong>{item.productName}</strong>
                          </td>
                          <td>{item.productCode || '—'}</td>
                          <td className="text-right">{formatNumber(item.quantity)}</td>
                          <td>{item.unit}</td>
                          {isImport && <td className="text-right">{formatMoney(item.unitPrice)} đ</td>}
                          {isImport && <td className="text-right">{formatMoney(item.lineTotal)} đ</td>}
                        </tr>
                      ))}
                    </tbody>
                  </table>
                </div>
              </>
            )}
          </div>
        </div>
      </div>
    </div>
  );
}