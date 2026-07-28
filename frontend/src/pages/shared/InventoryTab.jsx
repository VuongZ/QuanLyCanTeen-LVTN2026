import { useEffect, useMemo, useState } from 'react';
import { TicketHistoryModal } from './TicketHistoryModal';
import { getInventory } from '../../api/InventoryApi';

// Chuẩn hóa chuỗi để tìm kiếm không phân biệt
// chữ hoa, chữ thường và dấu tiếng Việt.
function normalizeText(value = '') {
  return String(value || '')
    .normalize('NFD')
    .replace(/[\u0300-\u036f]/g, '')
    .trim()
    .toLowerCase();
}

// Lấy giá trị từ nhiều tên thuộc tính khác nhau
// của dữ liệu trả về từ Backend.
//
// Ví dụ:
// branchId, BranchId hoặc branch_id đều được hỗ trợ.
function getValue(item, keys, fallback = '') {
  if (!item) return fallback;

  for (const key of keys) {
    if (
      item[key] !== undefined &&
      item[key] !== null
    ) {
      return item[key];
    }

    const realKey = Object.keys(item).find(
      (itemKey) =>
        itemKey.toLowerCase() ===
        key.toLowerCase()
    );

    if (
      realKey &&
      item[realKey] !== undefined &&
      item[realKey] !== null
    ) {
      return item[realKey];
    }
  }

  return fallback;
}

// Định dạng số theo kiểu Việt Nam
// để hiển thị dễ đọc trên giao diện.
function formatNumber(value) {
  return new Intl.NumberFormat('vi-VN', {
    maximumFractionDigits: 2,
  }).format(Number(value || 0));
}

// Xác định trạng thái tồn kho dựa trên
// số lượng hiện tại và ngưỡng sắp hết.
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

// Lấy và chuẩn hóa tên vai trò
// của người dùng hiện tại.
function getRoleName(user) {
  return normalizeText(
    user?.roleName || user?.role || ''
  );
}

// Lấy mã chi nhánh của người dùng
// từ các cách đặt tên thuộc tính khác nhau.
function getUserBranchId(user) {
  return (
    user?.branchId ??
    user?.BranchId ??
    user?.branch_id ??
    user?.branch?.id ??
    ''
  );
}

// Chuẩn hóa một dòng tồn kho về cấu trúc thống nhất
// để Component dễ sử dụng.
function normalizeInventoryItem(
  item,
  branches,
  fallbackBranchId = ''
) {
  const branchId = getValue(
    item,
    [
      'branchId',
      'BranchId',
      'branch_id',
      'BranchID',
      'maChiNhanh',
      'coSoId',
    ],
    fallbackBranchId
  );

  const branch = branches?.find(
    (branchItem) =>
      String(branchItem.id) ===
      String(branchId)
  );

  return {
    id: getValue(
      item,
      ['id', 'Id'],
      `${branchId}-${getValue(
        item,
        [
          'productId',
          'ProductId',
          'product_id',
        ],
        ''
      )}`
    ),

    branchId,

    branchName:
      getValue(
        item,
        [
          'branchName',
          'BranchName',
          'branch_name',
          'tenChiNhanh',
          'coSo',
        ],
        ''
      ) ||
      branch?.name ||
      branch?.branchName ||
      'Cơ sở hiện tại',

    productId: getValue(
      item,
      [
        'productId',
        'ProductId',
        'product_id',
      ],
      ''
    ),

    productCode: getValue(
      item,
      [
        'productCode',
        'ProductCode',
        'product_code',
      ],
      ''
    ),

    productName:
      getValue(
        item,
        [
          'productName',
          'ProductName',
          'product_name',
          'name',
          'Name',
          'tenSanPham',
        ],
        ''
      ) || 'Chưa rõ mặt hàng',

    unit:
      getValue(
        item,
        [
          'unit',
          'Unit',
          'donVi',
          'unitName',
        ],
        'Cái'
      ) || 'Cái',

    quantity: Number(
      getValue(
        item,
        [
          'quantity',
          'Quantity',
          'soLuong',
          'soLuongTon',
          'stockQuantity',
        ],
        0
      ) || 0
    ),

    supplierName: getValue(
      item,
      [
        'supplierName',
        'SupplierName',
        'supplier_name',
      ],
      ''
    ),
  };
}

// Component hiển thị và quản lý
// màn hình tồn kho.
export function InventoryTab({
  currentUser,
  branches = [],
}) {
  // Xác định người dùng hiện tại
  // có phải Admin hay không.
  const roleName = getRoleName(currentUser);

  const isAdmin =
    roleName.includes('admin') ||
    roleName.includes('quan tri');

  // Lưu danh sách tồn kho nhận từ Backend.
  const [inventory, setInventory] =
    useState([]);

  // Admin mặc định xem tất cả cơ sở.
  // Manager mặc định xem cơ sở được gán trong tài khoản.
  const [
    selectedBranchId,
    setSelectedBranchId,
  ] = useState(
    isAdmin
      ? 'ALL'
      : String(
          getUserBranchId(currentUser) || ''
        )
  );

  // Từ khóa tìm kiếm.
  const [search, setSearch] = useState('');

  // Ngưỡng dùng để xác định sản phẩm sắp hết.
  const [lowThreshold, setLowThreshold] =
    useState(10);

  // Trạng thái lọc:
  // ALL, ok, low hoặc out.
  const [statusFilter, setStatusFilter] =
    useState('ALL');

  // Trạng thái tải dữ liệu.
  const [loading, setLoading] =
    useState(false);

  // Nội dung lỗi hiển thị trên giao diện.
  const [error, setError] = useState('');

  // Mã chi nhánh của người dùng hiện tại.
  const userBranchId =
    getUserBranchId(currentUser);

  // Điều khiển cửa sổ lịch sử phiếu nhập kho.
  const [
    showImportTickets,
    setShowImportTickets,
  ] = useState(false);

  // Tìm thông tin chi nhánh hiện tại
  // để hiển thị tên cơ sở.
  const currentBranch = branches.find(
    (branch) =>
      String(branch.id) ===
      String(userBranchId)
  );

  // Manager không được tự chọn chi nhánh.
  // Khi tài khoản thay đổi, luôn đặt lại
  // chi nhánh theo thông tin tài khoản.
  useEffect(() => {
    if (!isAdmin) {
      setSelectedBranchId(
        String(userBranchId || '')
      );
    }
  }, [userBranchId, isAdmin]);

  // Tải danh sách tồn kho theo quyền
  // và chi nhánh đang chọn.
  async function loadInventory() {
    setLoading(true);
    setError('');

    try {
      let data;

      // Admin chọn một cơ sở cụ thể:
      // gửi branchId lên Backend.
      if (
        isAdmin &&
        selectedBranchId !== 'ALL'
      ) {
        data = await getInventory(
          selectedBranchId
        );
      } else {
        // Manager:
        // Backend tự lấy chi nhánh từ token.
        //
        // Admin chọn "Tất cả":
        // không truyền branchId để lấy toàn hệ thống.
        data = await getInventory();
      }

      const inventoryData =
        Array.isArray(data) ? data : [];

      // Chuẩn hóa dữ liệu trước khi đưa
      // lên giao diện.
      const normalized = inventoryData.map(
        (item) =>
          normalizeInventoryItem(
            item,
            branches,
            userBranchId
          )
      );

      setInventory(normalized);
    } catch (error) {
      console.error(
        'Lỗi tải tồn kho:',
        error
      );

      setError(
        error.response?.data?.message ||
          'Không tải được dữ liệu tồn kho.'
      );

      setInventory([]);
    } finally {
      setLoading(false);
    }
  }

  // Tải lại dữ liệu khi:
  // - Người dùng thay đổi.
  // - Chi nhánh thay đổi.
  // - Admin thay đổi bộ lọc chi nhánh.
  // - Danh sách cơ sở được tải lại.
  useEffect(() => {
    if (
      !currentUser?.id &&
      !currentUser?.Id
    ) {
      return;
    }

    loadInventory();

    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [
    currentUser?.id,
    currentUser?.Id,
    userBranchId,
    selectedBranchId,
    branches.length,
  ]);

  // Lọc và sắp xếp danh sách tồn kho
  // theo chi nhánh, trạng thái và từ khóa.
  const filteredInventory = useMemo(() => {
    const keyword = normalizeText(search);

    return inventory
      .filter((item) => {
        // Admin đang chọn một cơ sở cụ thể.
        if (
          isAdmin &&
          selectedBranchId !== 'ALL' &&
          String(item.branchId) !==
            String(selectedBranchId)
        ) {
          return false;
        }

        // Manager chỉ được thấy dữ liệu
        // thuộc chi nhánh của mình.
        if (
          !isAdmin &&
          userBranchId &&
          item.branchId &&
          String(item.branchId) !==
            String(userBranchId)
        ) {
          return false;
        }

        const status = getStockStatus(
          item.quantity,
          lowThreshold
        );

        // Lọc theo trạng thái tồn kho.
        if (
          statusFilter !== 'ALL' &&
          status.className !== statusFilter
        ) {
          return false;
        }

        // Không có từ khóa thì giữ lại sản phẩm.
        if (!keyword) return true;

        // Tìm theo mã sản phẩm, tên sản phẩm,
        // đơn vị, cơ sở hoặc nhà phân phối.
        return [
          item.productCode,
          item.productName,
          item.unit,
          item.branchName,
          item.supplierName,
        ].some((value) =>
          normalizeText(value).includes(keyword)
        );
      })
      .sort((a, b) => {
        const statusA = getStockStatus(
          a.quantity,
          lowThreshold
        );

        const statusB = getStockStatus(
          b.quantity,
          lowThreshold
        );

        // Ưu tiên hiển thị:
        // hết hàng → sắp hết → còn hàng.
        if (statusA.rank !== statusB.rank) {
          return statusA.rank - statusB.rank;
        }

        // Nếu cùng trạng thái,
        // sắp xếp theo tên sản phẩm.
        return a.productName.localeCompare(
          b.productName,
          'vi'
        );
      });
  }, [
    inventory,
    search,
    selectedBranchId,
    isAdmin,
    userBranchId,
    lowThreshold,
    statusFilter,
  ]);

  // Tính các số liệu tổng quan
  // đang hiển thị trên các thẻ thống kê.
  const stats = useMemo(() => {
    const totalProducts =
      filteredInventory.length;

    const totalQuantity =
      filteredInventory.reduce(
        (sum, item) =>
          sum + Number(item.quantity || 0),
        0
      );

    const outOfStock =
      filteredInventory.filter(
        (item) =>
          Number(item.quantity || 0) <= 0
      ).length;

    const lowStock =
      filteredInventory.filter((item) => {
        const qty = Number(
          item.quantity || 0
        );

        return (
          qty > 0 &&
          qty <= lowThreshold
        );
      }).length;

    const branchCount = new Set(
      filteredInventory
        .map((item) => item.branchId)
        .filter(Boolean)
    ).size;

    return {
      totalProducts,
      totalQuantity,
      outOfStock,
      lowStock,
      branchCount,
    };
  }, [filteredInventory, lowThreshold]);

  // Tiêu đề hiển thị theo quyền.
  const pageTitle = isAdmin
    ? 'Tồn kho toàn hệ thống'
    : 'Tồn kho cơ sở của bạn';

  const pageSubtitle = isAdmin
    ? 'Theo dõi số lượng hàng hóa tại tất cả cơ sở hoặc lọc theo từng chi nhánh.'
    : `Theo dõi số lượng hàng hóa tại ${
        currentBranch?.name ||
        currentBranch?.branchName ||
        currentUser?.branchName ||
        'cơ sở đang quản lý'
      }.`;

  return (
    <div className="sd-inventory-page">
      {/* Phần tiêu đề trang và các nút chức năng. */}
      <div className="sd-inventory-hero">
        <div className="sd-inventory-hero-main">
          <div className="sd-inventory-icon">
            📦
          </div>

          <div>
            <h2>{pageTitle}</h2>
            <p>{pageSubtitle}</p>
          </div>
        </div>

        <button
          type="button"
          className="sd-inventory-refresh secondary"
          onClick={() =>
            setShowImportTickets(true)
          }
        >
          Xem phiếu nhập kho
        </button>

        <button
          type="button"
          className="sd-inventory-refresh"
          onClick={loadInventory}
          disabled={loading}
        >
          {loading
            ? 'Đang tải...'
            : 'Làm mới'}
        </button>
      </div>

      {/* Các thẻ thống kê tồn kho. */}
      <div className="sd-inventory-stats">
        <div className="sd-inventory-stat-card">
          <span className="sd-inventory-stat-icon">
            📋
          </span>

          <div>
            <strong>
              {formatNumber(
                stats.totalProducts
              )}
            </strong>
            <p>Mặt hàng</p>
          </div>
        </div>

        <div className="sd-inventory-stat-card">
          <span className="sd-inventory-stat-icon">
            🧮
          </span>

          <div>
            <strong>
              {formatNumber(
                stats.totalQuantity
              )}
            </strong>
            <p>Tổng số lượng</p>
          </div>
        </div>

        <div className="sd-inventory-stat-card warning">
          <span className="sd-inventory-stat-icon">
            ⚠️
          </span>

          <div>
            <strong>
              {formatNumber(stats.lowStock)}
            </strong>
            <p>Sắp hết hàng</p>
          </div>
        </div>

        <div className="sd-inventory-stat-card danger">
          <span className="sd-inventory-stat-icon">
            ⛔
          </span>

          <div>
            <strong>
              {formatNumber(
                stats.outOfStock
              )}
            </strong>
            <p>Hết hàng</p>
          </div>
        </div>

        {isAdmin && (
          <div className="sd-inventory-stat-card">
            <span className="sd-inventory-stat-icon">
              🏫
            </span>

            <div>
              <strong>
                {formatNumber(
                  stats.branchCount
                )}
              </strong>
              <p>Cơ sở có hàng</p>
            </div>
          </div>
        )}
      </div>

      {/* Thanh tìm kiếm và bộ lọc. */}
      <div className="sd-inventory-toolbar">
        <div className="sd-inventory-search">
          <span>⌕</span>

          <input
            type="text"
            value={search}
            placeholder="Tìm tên hàng, mã hàng, đơn vị, cơ sở..."
            onChange={(event) =>
              setSearch(event.target.value)
            }
          />

          {search && (
            <button
              type="button"
              onClick={() => setSearch('')}
            >
              ✕
            </button>
          )}
        </div>

        <div className="sd-inventory-filters">
          {/* Admin được chọn cơ sở.
              Manager chỉ xem cơ sở được gán. */}
          {isAdmin ? (
            <select
              value={selectedBranchId}
              onChange={(event) =>
                setSelectedBranchId(
                  event.target.value
                )
              }
            >
              <option value="ALL">
                Tất cả cơ sở
              </option>

              {branches.map((branch) => (
                <option
                  key={branch.id}
                  value={branch.id}
                >
                  {branch.name ||
                    branch.branchName}
                </option>
              ))}
            </select>
          ) : (
            <select
              value={String(
                userBranchId || ''
              )}
              disabled
            >
              <option
                value={String(
                  userBranchId || ''
                )}
              >
                {currentBranch?.name ||
                  currentBranch?.branchName ||
                  currentUser?.branchName ||
                  'Cơ sở hiện tại'}
              </option>
            </select>
          )}

          {/* Lọc theo trạng thái tồn kho. */}
          <select
            value={statusFilter}
            onChange={(event) =>
              setStatusFilter(
                event.target.value
              )
            }
          >
            <option value="ALL">
              Tất cả trạng thái
            </option>
            <option value="ok">
              Còn hàng
            </option>
            <option value="low">
              Sắp hết
            </option>
            <option value="out">
              Hết hàng
            </option>
          </select>

          {/* Chọn ngưỡng xác định sản phẩm sắp hết. */}
          <select
            value={lowThreshold}
            onChange={(event) =>
              setLowThreshold(
                Number(event.target.value)
              )
            }
          >
            <option value={5}>
              Ngưỡng thấp ≤ 5
            </option>
            <option value={10}>
              Ngưỡng thấp ≤ 10
            </option>
            <option value={20}>
              Ngưỡng thấp ≤ 20
            </option>
            <option value={50}>
              Ngưỡng thấp ≤ 50
            </option>
          </select>
        </div>
      </div>

      {/* Hiển thị thông báo lỗi tải dữ liệu. */}
      {error && (
        <div className="sd-status sd-status-error sd-inventory-message">
          {error}
        </div>
      )}

      {/* Hiển thị trạng thái tải, rỗng hoặc danh sách tồn kho. */}
      {loading ? (
        <div className="sd-inventory-empty">
          <span>⏳</span>
          <p>
            Đang tải dữ liệu tồn kho...
          </p>
        </div>
      ) : filteredInventory.length === 0 ? (
        <div className="sd-inventory-empty">
          <span>📦</span>
          <p>
            Không có dữ liệu tồn kho phù hợp.
          </p>
        </div>
      ) : (
        <>
          {/* Bảng dành cho giao diện màn hình lớn. */}
          <div className="sd-inventory-table-wrap">
            <table className="sd-inventory-table">
              <thead>
                <tr>
                  <th>Mặt hàng</th>

                  {isAdmin && (
                    <th>Cơ sở</th>
                  )}

                  <th>Mã SP</th>

                  <th className="text-right">
                    Số lượng tồn
                  </th>

                  <th>Đơn vị</th>
                  <th>Trạng thái</th>
                </tr>
              </thead>

              <tbody>
                {filteredInventory.map(
                  (item) => {
                    const status =
                      getStockStatus(
                        item.quantity,
                        lowThreshold
                      );

                    return (
                      <tr
                        key={`${item.branchId}-${item.productId}-${item.productName}`}
                      >
                        <td>
                          <div className="sd-inventory-product">
                            <div className="sd-inventory-product-icon">
                              {item.productName
                                .charAt(0)
                                .toUpperCase()}
                            </div>

                            <div>
                              <strong>
                                {
                                  item.productName
                                }
                              </strong>

                              {item.supplierName && (
                                <span>
                                  {
                                    item.supplierName
                                  }
                                </span>
                              )}
                            </div>
                          </div>
                        </td>

                        {isAdmin && (
                          <td>
                            <span className="sd-inventory-branch-name">
                              {
                                item.branchName
                              }
                            </span>
                          </td>
                        )}

                        <td>
                          <span className="sd-inventory-code">
                            {item.productCode ||
                              '—'}
                          </span>
                        </td>

                        <td className="text-right">
                          <span
                            className={`sd-inventory-quantity ${status.className}`}
                          >
                            {formatNumber(
                              item.quantity
                            )}
                          </span>
                        </td>

                        <td>{item.unit}</td>

                        <td>
                          <span
                            className={`sd-inventory-status ${status.className}`}
                          >
                            {status.label}
                          </span>
                        </td>
                      </tr>
                    );
                  }
                )}
              </tbody>
            </table>
          </div>

          {/* Danh sách dạng thẻ dành cho màn hình nhỏ. */}
          <div className="sd-inventory-cards">
            {filteredInventory.map(
              (item) => {
                const status =
                  getStockStatus(
                    item.quantity,
                    lowThreshold
                  );

                return (
                  <div
                    key={`${item.branchId}-${item.productId}-${item.productName}-card`}
                    className={`sd-inventory-card ${status.className}`}
                  >
                    <div className="sd-inventory-card-head">
                      <div>
                        <strong>
                          {item.productName}
                        </strong>

                        <span>
                          {item.productCode ||
                            'Chưa có mã SP'}
                        </span>
                      </div>

                      <span
                        className={`sd-inventory-status ${status.className}`}
                      >
                        {status.label}
                      </span>
                    </div>

                    <div className="sd-inventory-card-body">
                      <div>
                        <span>
                          Số lượng tồn
                        </span>

                        <strong
                          className={
                            status.className
                          }
                        >
                          {formatNumber(
                            item.quantity
                          )}
                        </strong>
                      </div>

                      <div>
                        <span>Đơn vị</span>
                        <strong>
                          {item.unit}
                        </strong>
                      </div>

                      <div>
                        <span>Cơ sở</span>
                        <strong>
                          {item.branchName}
                        </strong>
                      </div>
                    </div>
                  </div>
                );
              }
            )}
          </div>
        </>
      )}

      {/* Cửa sổ xem lịch sử phiếu nhập kho.
          Admin chọn "Tất cả" thì không truyền branchId. */}
      <TicketHistoryModal
        open={showImportTickets}
        onClose={() =>
          setShowImportTickets(false)
        }
        type="inventoryImport"
        branchId={
          isAdmin
            ? selectedBranchId === 'ALL'
              ? null
              : selectedBranchId
            : userBranchId
        }
      />
    </div>
  );
}