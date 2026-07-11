import { useEffect, useMemo, useState } from 'react';
import axios from 'axios';

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

function getUserId(user) {
  return user?.id ?? user?.Id ?? user?.userId ?? user?.UserId ?? '';
}

function getUserBranchId(user) {
  return user?.branchId ?? user?.BranchId ?? user?.branch_id ?? user?.branch?.id ?? '';
}

function normalizeInventoryItem(item) {
  return {
    id: getValue(item, ['id', 'Id'], ''),
    branchId: getValue(item, ['branchId', 'BranchId', 'branch_id'], ''),
    productId: getValue(item, ['productId', 'ProductId', 'product_id'], ''),
    productCode: getValue(item, ['productCode', 'ProductCode', 'product_code'], ''),
    productName:
      getValue(item, ['productName', 'ProductName', 'product_name', 'name', 'Name'], '') ||
      'Chưa rõ mặt hàng',
    unit: getValue(item, ['unit', 'Unit', 'donVi'], 'Cái') || 'Cái',
    quantity: Number(getValue(item, ['quantity', 'Quantity', 'soLuongTon'], 0) || 0),
  };
}

function normalizeScheduleOption(item) {
  return {
    scheduleId: Number(getValue(item, ['scheduleId', 'ScheduleId'], 0) || 0),
    shiftId: Number(getValue(item, ['shiftId', 'ShiftId'], 0) || 0),
    shiftName: getValue(item, ['shiftName', 'ShiftName'], 'Ca làm'),
    workDate: getValue(item, ['workDate', 'WorkDate'], ''),
    startTime: getValue(item, ['startTime', 'StartTime'], ''),
    endTime: getValue(item, ['endTime', 'EndTime'], ''),
    canExportNow: Boolean(getValue(item, ['canExportNow', 'CanExportNow'], false)),
    statusLabel: getValue(item, ['statusLabel', 'StatusLabel'], ''),
  };
}

export function ManagerExportTab({ user, branches = [] }) {
  const userId = getUserId(user);
  const userBranchId = getUserBranchId(user);

  const [inventory, setInventory] = useState([]);
  const [exportQuantities, setExportQuantities] = useState({});
  const [schedules, setSchedules] = useState([]);
  const [note, setNote] = useState('');
  const [search, setSearch] = useState('');
  const [loading, setLoading] = useState(false);
  const [loadingSchedules, setLoadingSchedules] = useState(false);
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [message, setMessage] = useState(null);

  const currentBranch = branches.find((branch) => String(branch.id) === String(userBranchId));

  const activeSchedule = useMemo(() => {
    return schedules.find((schedule) => schedule.canExportNow) || null;
  }, [schedules]);

  async function loadInventory() {
    setLoading(true);
    setMessage(null);

    try {
      const response = await axios.get('/api/Inventory');
      const data = Array.isArray(response.data) ? response.data : [];
      setInventory(data.map(normalizeInventoryItem));
    } catch (error) {
      setMessage({
        type: 'error',
        text: error.response?.data?.message || 'Không tải được dữ liệu tồn kho.',
      });
      setInventory([]);
    } finally {
      setLoading(false);
    }
  }

  async function loadSchedules() {
    if (!userId) return;

    setLoadingSchedules(true);

    try {
      const response = await axios.get(`/api/KhoExport/available-schedules?managerId=${userId}`);
      const data = Array.isArray(response.data) ? response.data : [];
      setSchedules(data.map(normalizeScheduleOption));
    } catch (error) {
      setSchedules([]);
      setMessage({
        type: 'error',
        text: error.response?.data?.message || 'Không tải được ca làm hôm nay.',
      });
    } finally {
      setLoadingSchedules(false);
    }
  }

  useEffect(() => {
    if (!userId) return;

    loadInventory();
    loadSchedules();

    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [userId]);

  const filteredInventory = useMemo(() => {
    const keyword = search.trim().toLowerCase();

    return inventory
      .filter((item) => Number(item.quantity || 0) > 0)
      .filter((item) => {
        if (!keyword) return true;

        return [
          item.productCode,
          item.productName,
          item.unit,
        ].some((value) => String(value || '').toLowerCase().includes(keyword));
      })
      .sort((a, b) => a.productName.localeCompare(b.productName, 'vi'));
  }, [inventory, search]);

  const selectedItems = useMemo(() => {
    return Object.entries(exportQuantities)
      .map(([productId, quantity]) => ({
        productId: Number(productId),
        quantity: Number(quantity || 0),
      }))
      .filter((item) => item.productId > 0 && item.quantity > 0);
  }, [exportQuantities]);

  const totalExportQuantity = selectedItems.reduce((sum, item) => sum + item.quantity, 0);

  function handleQuantityChange(productId, value, maxQuantity) {
    const parsed = Number(value || 0);
    const nextQuantity = Math.max(0, Math.min(parsed, Number(maxQuantity || 0)));

    setExportQuantities((current) => ({
      ...current,
      [productId]: nextQuantity,
    }));
  }

  async function handleSubmit() {
    if (!activeSchedule) {
      setMessage({
        type: 'error',
        text: 'Hiện tại bạn không ở trong thời gian được phép xuất hàng ra quầy.',
      });
      return;
    }

    if (selectedItems.length === 0) {
      setMessage({
        type: 'error',
        text: 'Vui lòng nhập số lượng cần xuất cho ít nhất một mặt hàng.',
      });
      return;
    }

    setIsSubmitting(true);
    setMessage(null);

    const payload = {
      managerId: Number(userId),
      branchId: Number(userBranchId),
      scheduleId: Number(activeSchedule.scheduleId),
      note: note || null,
      items: selectedItems,
    };

    try {
      const response = await axios.post('/api/KhoExport/submit-export', payload);

      setMessage({
        type: 'success',
        text: response.data?.message || 'Xuất hàng ra quầy thành công!',
      });

      setExportQuantities({});
      setNote('');

      await loadInventory();
      await loadSchedules();
    } catch (error) {
      setMessage({
        type: 'error',
        text: error.response?.data?.message || 'Không thể xuất hàng ra quầy.',
      });
    } finally {
      setIsSubmitting(false);
    }
  }

  return (
    <div className="sd-card sd-export-page">
      <div className="sd-card-header">
        <p className="sd-eyebrow">Kho chi nhánh</p>
        <h2>Xuất hàng từ kho ra quầy</h2>
      </div>

      <div className="sd-export-hero">
        <div className="sd-export-hero-icon">📤</div>

        <div>
          <p className="sd-eyebrow">Cơ sở xuất hàng</p>
          <h3>
            {currentBranch?.name || currentBranch?.branchName || user?.branchName || 'Chi nhánh hiện tại'}
          </h3>
          <span>Hàng xuất sẽ được trừ trong kho và cộng vào tồn quầy.</span>
        </div>
      </div>

      <div className="sd-export-shift-box">
        {loadingSchedules ? (
          <div className="sd-export-shift loading">
            ⏳ Đang kiểm tra ca làm hiện tại...
          </div>
        ) : activeSchedule ? (
          <div className="sd-export-shift success">
            <strong>Ca được phép xuất:</strong>
            <span>
              {activeSchedule.shiftName} · {activeSchedule.startTime} - {activeSchedule.endTime} · {activeSchedule.statusLabel}
            </span>
          </div>
        ) : schedules.length > 0 ? (
          <div className="sd-export-shift error">
            Hiện tại không nằm trong thời gian được phép xuất hàng ra quầy.
          </div>
        ) : (
          <div className="sd-export-shift error">
            Hôm nay bạn chưa có ca làm chính thức, không thể xuất hàng ra quầy.
          </div>
        )}
      </div>

      <div className="sd-export-form-grid">
        <div className="sd-field">
          <label>Tìm mặt hàng</label>
          <input
            type="text"
            value={search}
            placeholder="Nhập tên hàng hoặc mã sản phẩm..."
            onChange={(event) => setSearch(event.target.value)}
          />
        </div>

        <div className="sd-field">
          <label>Ghi chú phiếu xuất</label>
          <input
            type="text"
            value={note}
            placeholder="VD: Bổ sung hàng cho ca chiều"
            onChange={(event) => setNote(event.target.value)}
            disabled={!activeSchedule || isSubmitting}
          />
        </div>
      </div>

      <div className="sd-export-table-title">
        <h3>Danh sách hàng trong kho</h3>

        <span>
          Đã chọn {selectedItems.length} mặt hàng · Tổng xuất {formatNumber(totalExportQuantity)}
        </span>
      </div>

      {message && (
        <div className={`sd-status sd-status-${message.type} sd-export-message`}>
          {message.text}
        </div>
      )}

      {loading ? (
        <p className="sd-text-muted">Đang tải tồn kho...</p>
      ) : filteredInventory.length === 0 ? (
        <div className="sd-td-empty-sm">
          <div className="sd-empty-state">
            <span className="sd-empty-icon">📦</span>
            <p>Không có mặt hàng nào còn tồn kho để xuất ra quầy.</p>
          </div>
        </div>
      ) : (
        <div className="sd-table-wrap sd-box-bordered sd-export-table-wrap">
          <table className="sd-table">
            <thead>
              <tr>
                <th className="sd-th">Mặt hàng</th>
                <th className="sd-th sd-text-center sd-export-code-col">Mã SP</th>
                <th className="sd-th sd-text-right sd-export-stock-col">Tồn kho</th>
                <th className="sd-th sd-export-unit-col">Đơn vị</th>
                <th className="sd-th sd-text-right sd-export-quantity-col">Số lượng xuất</th>
              </tr>
            </thead>

            <tbody>
              {filteredInventory.map((item, index) => (
                <tr key={`${item.branchId}-${item.productId}-${item.id || index}`} className="sd-tr">
                  <td className="sd-td">
                    <strong>{item.productName}</strong>
                  </td>

                  <td className="sd-td sd-text-center">
                    <span className="sd-inventory-code">{item.productCode || '—'}</span>
                  </td>

                  <td className="sd-td sd-text-right sd-text-bold">
                    {formatNumber(item.quantity)}
                  </td>

                  <td className="sd-td">
                    {item.unit}
                  </td>

                  <td className="sd-td sd-text-right">
                    <input
                      className="sd-export-quantity-input"
                      type="number"
                      min="0"
                      max={item.quantity}
                      value={exportQuantities[item.productId] || ''}
                      onChange={(event) =>
                        handleQuantityChange(item.productId, event.target.value, item.quantity)
                      }
                      placeholder="0"
                      disabled={!activeSchedule || isSubmitting}
                    />
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}

      <div className="sd-export-actions">
        <button
          className="sd-btn-secondary sd-export-btn"
          onClick={() => {
            loadInventory();
            loadSchedules();
          }}
          disabled={loading || loadingSchedules || isSubmitting}
        >
          Làm mới
        </button>

        <button
          className="sd-btn-primary sd-export-btn"
          disabled={!activeSchedule || selectedItems.length === 0 || isSubmitting}
          onClick={handleSubmit}
        >
          {isSubmitting ? 'Đang xuất hàng...' : 'Xác nhận xuất ra quầy'}
        </button>
      </div>
    </div>
  );
}