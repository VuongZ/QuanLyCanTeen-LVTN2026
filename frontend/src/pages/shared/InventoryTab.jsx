import { useEffect, useMemo, useState } from 'react';
import axios from 'axios';
import { TicketHistoryModal } from './TicketHistoryModal';

function normalizeText(value = '') {
  return String(value || '')
    .normalize('NFD')
    .replace(/[\u0300-\u036f]/g, '')
    .trim()
    .toLowerCase();
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

function formatNumber(value) {
  return new Intl.NumberFormat('vi-VN', {
    maximumFractionDigits: 2,
  }).format(Number(value || 0));
}

function getStockStatus(quantity, lowThreshold) {
  const qty = Number(quantity || 0);

  if (qty <= 0) {
    return {
      label: 'Hết hàng',
      className: 'out',
      rank: 0,
    };
  }

  if (qty <= lowThreshold) {
    return {
      label: 'Sắp hết',
      className: 'low',
      rank: 1,
    };
  }

  return {
    label: 'Còn hàng',
    className: 'ok',
    rank: 2,
  };
}

function getRoleName(user) {
  return normalizeText(user?.roleName || user?.role || '');
}

function getUserBranchId(user) {
  return (
    user?.branchId ??
    user?.BranchId ??
    user?.branch_id ??
    user?.branch?.id ??
    ''
  );
}

function normalizeInventoryItem(item, branches, fallbackBranchId = '') {
  const branchId = getValue(
    item,
    ['branchId', 'BranchId', 'branch_id', 'BranchID', 'maChiNhanh', 'coSoId'],
    fallbackBranchId
  );

  const branch = branches?.find((branchItem) => String(branchItem.id) === String(branchId));

  return {
    id: getValue(
      item,
      ['id', 'Id'],
      `${branchId}-${getValue(item, ['productId', 'ProductId', 'product_id'], '')}`
    ),

    branchId,

    branchName:
      getValue(item, ['branchName', 'BranchName', 'branch_name', 'tenChiNhanh', 'coSo'], '') ||
      branch?.name ||
      branch?.branchName ||
      'Cơ sở hiện tại',

    productId: getValue(item, ['productId', 'ProductId', 'product_id'], ''),

    productCode: getValue(item, ['productCode', 'ProductCode', 'product_code'], ''),

    productName:
      getValue(item, ['productName', 'ProductName', 'product_name', 'name', 'Name', 'tenSanPham'], '') ||
      'Chưa rõ mặt hàng',

    unit:
      getValue(item, ['unit', 'Unit', 'donVi', 'unitName'], 'Cái') ||
      'Cái',

    quantity: Number(
      getValue(item, ['quantity', 'Quantity', 'soLuong', 'soLuongTon', 'stockQuantity'], 0) || 0
    ),

    supplierName: getValue(item, ['supplierName', 'SupplierName', 'supplier_name'], ''),
  };
}

async function fetchInventoryByCandidates(urls) {
  let lastError = null;

  for (const url of urls) {
    try {
      const response = await axios.get(url);
      const data = response.data;

      if (Array.isArray(data)) return data;
      if (Array.isArray(data?.data)) return data.data;
      if (Array.isArray(data?.items)) return data.items;

      return [];
    } catch (error) {
      lastError = error;
    }
  }

  throw lastError;
}

export function InventoryTab({ currentUser, branches = [] }) {
  const roleName = getRoleName(currentUser);
  const isAdmin = roleName.includes('admin') || roleName.includes('quan tri');
  const isManager = roleName.includes('manager') || roleName.includes('quan ly');

  const [inventory, setInventory] = useState([]);
  const [selectedBranchId, setSelectedBranchId] = useState(
    isAdmin ? 'ALL' : String(getUserBranchId(currentUser) || '')
  );
  const [search, setSearch] = useState('');
  const [lowThreshold, setLowThreshold] = useState(10);
  const [statusFilter, setStatusFilter] = useState('ALL');
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState('');

  const userBranchId = getUserBranchId(currentUser);
  const [showImportTickets, setShowImportTickets] = useState(false);

  const currentBranch = branches.find(
    (branch) => String(branch.id) === String(userBranchId)
  );

  useEffect(() => {
    if (!isAdmin) {
      setSelectedBranchId(String(userBranchId || ''));
    }
  }, [userBranchId, isAdmin]);

  async function loadInventory() {
    setLoading(true);
    setError('');

    try {
      let rawData = [];

      if (isAdmin && selectedBranchId === 'ALL') {
        rawData = await fetchInventoryByCandidates([
          '/api/Inventory',
        ]);
      } else if (isAdmin) {
        rawData = await fetchInventoryByCandidates([
          `/api/Inventory?branchId=${selectedBranchId}`,
        ]);
      } else {
        rawData = await fetchInventoryByCandidates([
          '/api/Inventory',
        ]);
      }

      const normalized = rawData.map((item) =>
        normalizeInventoryItem(item, branches, userBranchId)
      );



      setInventory(normalized);
    } catch (err) {
      console.error('Lỗi tải tồn kho:', err);
      setError(err.response?.data?.message || 'Không tải được dữ liệu tồn kho.');
      setInventory([]);
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => {
    if (!currentUser?.id && !currentUser?.Id) return;

    loadInventory();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [currentUser?.id, currentUser?.Id, userBranchId, selectedBranchId, branches.length]);

  const filteredInventory = useMemo(() => {
    const keyword = normalizeText(search);

    return inventory
      .filter((item) => {
        if (isAdmin && selectedBranchId !== 'ALL' && String(item.branchId) !== String(selectedBranchId)) {
          return false;
        }

        if (!isAdmin && userBranchId && item.branchId && String(item.branchId) !== String(userBranchId)) {
          return false;
        }

        const status = getStockStatus(item.quantity, lowThreshold);

        if (statusFilter !== 'ALL' && status.className !== statusFilter) {
          return false;
        }

        if (!keyword) return true;

        return [
          item.productCode,
          item.productName,
          item.unit,
          item.branchName,
          item.supplierName,
        ].some((value) => normalizeText(value).includes(keyword));
      })
      .sort((a, b) => {
        const statusA = getStockStatus(a.quantity, lowThreshold);
        const statusB = getStockStatus(b.quantity, lowThreshold);

        if (statusA.rank !== statusB.rank) return statusA.rank - statusB.rank;

        return a.productName.localeCompare(b.productName, 'vi');
      });
  }, [inventory, search, selectedBranchId, isAdmin, currentUser?.branchId, lowThreshold, statusFilter]);

  const stats = useMemo(() => {
    const totalProducts = filteredInventory.length;
    const totalQuantity = filteredInventory.reduce((sum, item) => sum + Number(item.quantity || 0), 0);
    const outOfStock = filteredInventory.filter((item) => Number(item.quantity || 0) <= 0).length;
    const lowStock = filteredInventory.filter((item) => {
      const qty = Number(item.quantity || 0);
      return qty > 0 && qty <= lowThreshold;
    }).length;

    const branchCount = new Set(filteredInventory.map((item) => item.branchId).filter(Boolean)).size;

    return {
      totalProducts,
      totalQuantity,
      outOfStock,
      lowStock,
      branchCount,
    };
  }, [filteredInventory, lowThreshold]);

  const pageTitle = isAdmin
    ? 'Tồn kho toàn hệ thống'
    : 'Tồn kho cơ sở của bạn';

  const pageSubtitle = isAdmin
    ? 'Theo dõi số lượng hàng hóa tại tất cả cơ sở hoặc lọc theo từng chi nhánh.'
    : `Theo dõi số lượng hàng hóa tại ${currentBranch?.name || currentUser?.branchName || 'cơ sở đang quản lý'}.`;

  return (
    <div className="sd-inventory-page">
      <div className="sd-inventory-hero">
        <div className="sd-inventory-hero-main">
          <div className="sd-inventory-icon">📦</div>
          <div>
            <p className="sd-eyebrow">Báo cáo kho</p>
            <h2>{pageTitle}</h2>
            <p>{pageSubtitle}</p>
          </div>
        </div>
        <button
          type="button"
          className="sd-inventory-refresh secondary"
          onClick={() => setShowImportTickets(true)}
        >
          Xem phiếu nhập kho
        </button>

        <button
          type="button"
          className="sd-inventory-refresh"
          onClick={loadInventory}
          disabled={loading}
        >
          {loading ? 'Đang tải...' : 'Làm mới'}
        </button>
      </div>

      <div className="sd-inventory-stats">
        <div className="sd-inventory-stat-card">
          <span className="sd-inventory-stat-icon">📋</span>
          <div>
            <strong>{formatNumber(stats.totalProducts)}</strong>
            <p>Mặt hàng</p>
          </div>
        </div>

        <div className="sd-inventory-stat-card">
          <span className="sd-inventory-stat-icon">🧮</span>
          <div>
            <strong>{formatNumber(stats.totalQuantity)}</strong>
            <p>Tổng số lượng</p>
          </div>
        </div>

        <div className="sd-inventory-stat-card warning">
          <span className="sd-inventory-stat-icon">⚠️</span>
          <div>
            <strong>{formatNumber(stats.lowStock)}</strong>
            <p>Sắp hết hàng</p>
          </div>
        </div>

        <div className="sd-inventory-stat-card danger">
          <span className="sd-inventory-stat-icon">⛔</span>
          <div>
            <strong>{formatNumber(stats.outOfStock)}</strong>
            <p>Hết hàng</p>
          </div>
        </div>

        {isAdmin && (
          <div className="sd-inventory-stat-card">
            <span className="sd-inventory-stat-icon">🏫</span>
            <div>
              <strong>{formatNumber(stats.branchCount)}</strong>
              <p>Cơ sở có hàng</p>
            </div>
          </div>
        )}
      </div>

      <div className="sd-inventory-toolbar">
        <div className="sd-inventory-search">
          <span>⌕</span>
          <input
            type="text"
            value={search}
            placeholder="Tìm tên hàng, mã hàng, đơn vị, cơ sở..."
            onChange={(event) => setSearch(event.target.value)}
          />
          {search && (
            <button type="button" onClick={() => setSearch('')}>
              ✕
            </button>
          )}
        </div>

        <div className="sd-inventory-filters">
          {isAdmin ? (
            <select value={selectedBranchId} onChange={(event) => setSelectedBranchId(event.target.value)}>
              <option value="ALL">Tất cả cơ sở</option>
              {branches.map((branch) => (
                <option key={branch.id} value={branch.id}>
                  {branch.name || branch.branchName}
                </option>
              ))}
            </select>
          ) : (
            <div className="sd-inventory-branch-chip">
              🏫 {currentBranch?.name || currentUser?.branchName || 'Cơ sở hiện tại'}
            </div>
          )}

          <select value={statusFilter} onChange={(event) => setStatusFilter(event.target.value)}>
            <option value="ALL">Tất cả trạng thái</option>
            <option value="ok">Còn hàng</option>
            <option value="low">Sắp hết</option>
            <option value="out">Hết hàng</option>
          </select>

          <select value={lowThreshold} onChange={(event) => setLowThreshold(Number(event.target.value))}>
            <option value={5}>Ngưỡng thấp ≤ 5</option>
            <option value={10}>Ngưỡng thấp ≤ 10</option>
            <option value={20}>Ngưỡng thấp ≤ 20</option>
            <option value={50}>Ngưỡng thấp ≤ 50</option>
          </select>
        </div>
      </div>

      {error && (
        <div className="sd-status sd-status-error sd-inventory-message">
          {error}
        </div>
      )}

      {loading ? (
        <div className="sd-inventory-empty">
          <span>⏳</span>
          <p>Đang tải dữ liệu tồn kho...</p>
        </div>
      ) : filteredInventory.length === 0 ? (
        <div className="sd-inventory-empty">
          <span>📦</span>
          <p>Không có dữ liệu tồn kho phù hợp.</p>
        </div>
      ) : (
        <>
          <div className="sd-inventory-table-wrap">
            <table className="sd-inventory-table">
              <thead>
                <tr>
                  <th>Mặt hàng</th>
                  {isAdmin && <th>Cơ sở</th>}
                  <th>Mã SP</th>
                  <th className="text-right">Số lượng tồn</th>
                  <th>Đơn vị</th>
                  <th>Trạng thái</th>
                </tr>
              </thead>
              <tbody>
                {filteredInventory.map((item) => {
                  const status = getStockStatus(item.quantity, lowThreshold);

                  return (
                    <tr key={`${item.branchId}-${item.productId}-${item.productName}`}>
                      <td>
                        <div className="sd-inventory-product">
                          <div className="sd-inventory-product-icon">
                            {item.productName.charAt(0).toUpperCase()}
                          </div>
                          <div>
                            <strong>{item.productName}</strong>
                            {item.supplierName && <span>{item.supplierName}</span>}
                          </div>
                        </div>
                      </td>

                      {isAdmin && (
                        <td>
                          <span className="sd-inventory-branch-name">{item.branchName}</span>
                        </td>
                      )}

                      <td>
                        <span className="sd-inventory-code">{item.productCode || '—'}</span>
                      </td>

                      <td className="text-right">
                        <span className={`sd-inventory-quantity ${status.className}`}>
                          {formatNumber(item.quantity)}
                        </span>
                      </td>

                      <td>{item.unit}</td>

                      <td>
                        <span className={`sd-inventory-status ${status.className}`}>
                          {status.label}
                        </span>
                      </td>
                    </tr>
                  );
                })}
              </tbody>
            </table>
          </div>

          <div className="sd-inventory-cards">
            {filteredInventory.map((item) => {
              const status = getStockStatus(item.quantity, lowThreshold);

              return (
                <div
                  key={`${item.branchId}-${item.productId}-${item.productName}-card`}
                  className={`sd-inventory-card ${status.className}`}
                >
                  <div className="sd-inventory-card-head">
                    <div>
                      <strong>{item.productName}</strong>
                      <span>{item.productCode || 'Chưa có mã SP'}</span>
                    </div>
                    <span className={`sd-inventory-status ${status.className}`}>
                      {status.label}
                    </span>
                  </div>

                  <div className="sd-inventory-card-body">
                    <div>
                      <span>Số lượng tồn</span>
                      <strong className={status.className}>{formatNumber(item.quantity)}</strong>
                    </div>
                    <div>
                      <span>Đơn vị</span>
                      <strong>{item.unit}</strong>
                    </div>
                    <div>
                      <span>Cơ sở</span>
                      <strong>{item.branchName}</strong>
                    </div>
                  </div>
                </div>
              );
            })}
          </div>
        </>
      )}
      <TicketHistoryModal
  open={showImportTickets}
  onClose={() => setShowImportTickets(false)}
  type="inventoryImport"
  branchId={isAdmin ? selectedBranchId : userBranchId}
/>
    </div>
    
  );
}