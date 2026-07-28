import { useEffect, useMemo, useState } from 'react';
import { TicketHistoryModal } from './TicketHistoryModal';
import { getFrontStock } from '../../api/FrontStockApi';

// Chuẩn hóa chuỗi để tìm kiếm không phân biệt
// chữ hoa, chữ thường và dấu tiếng Việt.
function normalizeText(value = '') {
  return String(value || '')
    .normalize('NFD')
    .replace(/[\u0300-\u036f]/g, '')
    .trim()
    .toLowerCase();
}

// Lấy giá trị từ nhiều tên thuộc tính khác nhau.
//
// Ví dụ:
// branchId, BranchId và branch_id
// đều được xem là cùng một trường dữ liệu.
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

// Định dạng số theo kiểu hiển thị Việt Nam.
function formatNumber(value) {
  return new Intl.NumberFormat('vi-VN', {
    maximumFractionDigits: 2,
  }).format(Number(value || 0));
}

// Xác định trạng thái tồn quầy dựa trên
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
    user?.roleName ||
      user?.role ||
      ''
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

// Chuẩn hóa một dòng tồn quầy từ Backend
// về cấu trúc thống nhất để giao diện sử dụng.
function normalizeFrontStockItem(
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
        ],
        'Cái'
      ) || 'Cái',

    quantity: Number(
      getValue(
        item,
        [
          'quantity',
          'Quantity',
          'soLuongTon',
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

// Component hiển thị màn hình tồn quầy.
export function FrontStockTab({
  currentUser,
  branches = [],
}) {
  // Xác định người dùng có phải Admin hay không.
  const roleName = getRoleName(currentUser);

  const isAdmin =
    roleName.includes('admin') ||
    roleName.includes('quan tri');

  // Lấy mã chi nhánh của tài khoản hiện tại.
  const userBranchId =
    getUserBranchId(currentUser);

  // Tìm thông tin chi nhánh để hiển thị tên cơ sở.
  const currentBranch = branches.find(
    (branch) =>
      String(branch.id) ===
      String(userBranchId)
  );

  // Danh sách tồn quầy nhận từ Backend.
  const [frontStock, setFrontStock] =
    useState([]);

  // Admin mặc định xem tất cả cơ sở.
  // Manager mặc định xem cơ sở được gán.
  const [
    selectedBranchId,
    setSelectedBranchId,
  ] = useState(
    isAdmin
      ? 'ALL'
      : String(userBranchId || '')
  );

  // Từ khóa tìm kiếm sản phẩm.
  const [search, setSearch] =
    useState('');

  // Ngưỡng xác định sản phẩm sắp hết.
  const [
    lowThreshold,
    setLowThreshold,
  ] = useState(10);

  // Bộ lọc trạng thái tồn quầy.
  const [
    statusFilter,
    setStatusFilter,
  ] = useState('ALL');

  // Trạng thái tải dữ liệu.
  const [loading, setLoading] =
    useState(false);

  // Thông báo lỗi tải dữ liệu.
  const [error, setError] =
    useState('');

  // Điều khiển cửa sổ xem lịch sử phiếu xuất.
  const [
    showExportTickets,
    setShowExportTickets,
  ] = useState(false);

  // Manager luôn sử dụng chi nhánh được gán
  // trong tài khoản và không được tự chọn cơ sở.
  useEffect(() => {
    if (!isAdmin) {
      setSelectedBranchId(
        String(userBranchId || '')
      );
    }
  }, [userBranchId, isAdmin]);

  // Tải dữ liệu tồn quầy theo quyền
  // và chi nhánh đang được chọn.
  async function loadFrontStock() {
    setLoading(true);
    setError('');

    try {
      let data;

      // Admin chọn một chi nhánh cụ thể:
      // truyền branchId lên Backend.
      if (
        isAdmin &&
        selectedBranchId !== 'ALL'
      ) {
        data = await getFrontStock(
          selectedBranchId
        );
      } else {
        // Manager:
        // Backend tự lấy chi nhánh từ token.
        //
        // Admin chọn "Tất cả":
        // không truyền branchId.
        data = await getFrontStock();
      }

      const frontStockData =
        Array.isArray(data) ? data : [];

      // Chuẩn hóa dữ liệu trước khi đưa
      // lên giao diện.
      const normalized =
        frontStockData.map((item) =>
          normalizeFrontStockItem(
            item,
            branches,
            userBranchId
          )
        );

      setFrontStock(normalized);
    } catch (error) {
      console.error(
        'Lỗi tải tồn quầy:',
        error
      );

      setError(
        error.response?.data?.message ||
          'Không tải được dữ liệu tồn quầy.'
      );

      setFrontStock([]);
    } finally {
      setLoading(false);
    }
  }

  // Tải lại tồn quầy khi:
  // - Người dùng thay đổi.
  // - Chi nhánh của người dùng thay đổi.
  // - Admin chọn chi nhánh khác.
  // - Danh sách cơ sở được tải lại.
  useEffect(() => {
    if (
      !currentUser?.id &&
      !currentUser?.Id
    ) {
      return;
    }

    loadFrontStock();

    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [
    currentUser?.id,
    currentUser?.Id,
    userBranchId,
    selectedBranchId,
    branches.length,
  ]);

  // Lọc và sắp xếp danh sách tồn quầy
  // theo cơ sở, trạng thái và từ khóa.
  const filteredFrontStock =
    useMemo(() => {
      const keyword =
        normalizeText(search);

      return frontStock
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

          // Manager chỉ được xem tồn quầy
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

          // Lọc theo trạng thái tồn quầy.
          if (
            statusFilter !== 'ALL' &&
            status.className !==
              statusFilter
          ) {
            return false;
          }

          // Không có từ khóa thì giữ lại sản phẩm.
          if (!keyword) return true;

          // Tìm theo mã, tên, đơn vị,
          // cơ sở hoặc nhà phân phối.
          return [
            item.productCode,
            item.productName,
            item.unit,
            item.branchName,
            item.supplierName,
          ].some((value) =>
            normalizeText(value).includes(
              keyword
            )
          );
        })
        .sort((a, b) => {
          const statusA =
            getStockStatus(
              a.quantity,
              lowThreshold
            );

          const statusB =
            getStockStatus(
              b.quantity,
              lowThreshold
            );

          // Ưu tiên hiển thị:
          // hết hàng → sắp hết → còn hàng.
          if (
            statusA.rank !== statusB.rank
          ) {
            return (
              statusA.rank -
              statusB.rank
            );
          }

          // Nếu cùng trạng thái,
          // sắp xếp theo tên sản phẩm.
          return a.productName.localeCompare(
            b.productName,
            'vi'
          );
        });
    }, [
      frontStock,
      search,
      selectedBranchId,
      isAdmin,
      userBranchId,
      lowThreshold,
      statusFilter,
    ]);

  // Tính các số liệu thống kê
  // dựa trên danh sách đang hiển thị.
  const stats = useMemo(() => {
    const totalProducts =
      filteredFrontStock.length;

    const totalQuantity =
      filteredFrontStock.reduce(
        (sum, item) =>
          sum +
          Number(item.quantity || 0),
        0
      );

    const outOfStock =
      filteredFrontStock.filter(
        (item) =>
          Number(item.quantity || 0) <= 0
      ).length;

    const lowStock =
      filteredFrontStock.filter(
        (item) => {
          const qty = Number(
            item.quantity || 0
          );

          return (
            qty > 0 &&
            qty <= lowThreshold
          );
        }
      ).length;

    const branchCount = new Set(
      filteredFrontStock
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
  }, [
    filteredFrontStock,
    lowThreshold,
  ]);

  return (
    <div className="sd-front-stock-page">
      {/* Phần tiêu đề và các nút chức năng. */}
      <div className="sd-front-stock-hero">
        <div className="sd-front-stock-hero-main">
          <div className="sd-front-stock-icon">
            🛒
          </div>

          <div>
            <p className="sd-eyebrow">
              Tồn quầy
            </p>

            <h2>
              {isAdmin
                ? 'Tồn quầy toàn hệ thống'
                : 'Tồn quầy cơ sở'}
            </h2>

            <p>
              {isAdmin
                ? 'Theo dõi số lượng hàng đang có tại quầy của các cơ sở.'
                : `Theo dõi hàng đang có tại quầy của ${
                    currentBranch?.name ||
                    currentBranch?.branchName ||
                    currentUser?.branchName ||
                    'cơ sở hiện tại'
                  }.`}
            </p>
          </div>
        </div>

        <button
          type="button"
          className="sd-front-stock-refresh secondary"
          onClick={() =>
            setShowExportTickets(true)
          }
        >
          Xem phiếu xuất ra quầy
        </button>

        <button
          type="button"
          className="sd-front-stock-refresh"
          onClick={loadFrontStock}
          disabled={loading}
        >
          {loading
            ? 'Đang tải...'
            : 'Làm mới'}
        </button>
      </div>

      {/* Các thẻ thống kê tồn quầy. */}
      <div className="sd-front-stock-stats">
        <div className="sd-front-stock-stat">
          <span>📦</span>

          <div>
            <strong>
              {formatNumber(
                stats.totalProducts
              )}
            </strong>
            <p>Mặt hàng tại quầy</p>
          </div>
        </div>

        <div className="sd-front-stock-stat">
          <span>🧮</span>

          <div>
            <strong>
              {formatNumber(
                stats.totalQuantity
              )}
            </strong>
            <p>Tổng số lượng tại quầy</p>
          </div>
        </div>

        <div className="sd-front-stock-stat warning">
          <span>⚠️</span>

          <div>
            <strong>
              {formatNumber(stats.lowStock)}
            </strong>
            <p>Sắp hết hàng</p>
          </div>
        </div>

        <div className="sd-front-stock-stat danger">
          <span>⛔</span>

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
          <div className="sd-front-stock-stat">
            <span>🏫</span>

            <div>
              <strong>
                {formatNumber(
                  stats.branchCount
                )}
              </strong>
              <p>Cơ sở có hàng tại quầy</p>
            </div>
          </div>
        )}
      </div>

      {/* Thanh tìm kiếm và bộ lọc. */}
      <div className="sd-front-stock-toolbar">
        <div className="sd-front-stock-search">
          <span>⌕</span>

          <input
            type="text"
            value={search}
            placeholder="Tìm tên hàng, mã hàng, đơn vị..."
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

        <div className="sd-front-stock-filters">
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
            <div className="sd-front-stock-branch-chip">
              🏫{' '}
              {currentBranch?.name ||
                currentBranch?.branchName ||
                currentUser?.branchName ||
                'Cơ sở hiện tại'}
            </div>
          )}

          {/* Lọc theo trạng thái tồn quầy. */}
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

          {/* Chọn ngưỡng xác định sắp hết. */}
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

      {/* Hiển thị thông báo lỗi. */}
      {error && (
        <div className="sd-status sd-status-error sd-front-stock-message">
          {error}
        </div>
      )}

      {/* Hiển thị trạng thái tải hoặc bảng dữ liệu. */}
      {loading ? (
        <div className="sd-front-stock-empty">
          <span>⏳</span>
          <p>
            Đang tải dữ liệu tồn quầy...
          </p>
        </div>
      ) : filteredFrontStock.length === 0 ? (
        <div className="sd-front-stock-empty">
          <span>🛒</span>
          <p>
            Không có dữ liệu tồn quầy phù hợp.
          </p>
        </div>
      ) : (
        <div className="sd-front-stock-table-wrap">
          <table className="sd-front-stock-table">
            <thead>
              <tr>
                <th>Mặt hàng</th>

                {isAdmin && (
                  <th>Cơ sở</th>
                )}

                <th>Mã SP</th>

                <th className="text-right">
                  Số lượng tại quầy
                </th>

                <th>Đơn vị</th>
                <th>Trạng thái</th>
              </tr>
            </thead>

            <tbody>
              {filteredFrontStock.map(
                (item, index) => {
                  const status =
                    getStockStatus(
                      item.quantity,
                      lowThreshold
                    );

                  return (
                    <tr
                      key={`${item.branchId}-${item.productId}-${item.id || index}`}
                    >
                      <td>
                        <div className="sd-front-stock-product">
                          <div className="sd-front-stock-product-icon">
                            {item.productName
                              .charAt(0)
                              .toUpperCase()}
                          </div>

                          <div>
                            <strong>
                              {item.productName}
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
                          <span className="sd-front-stock-branch-name">
                            {item.branchName}
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
                          className={`sd-front-stock-quantity ${status.className}`}
                        >
                          {formatNumber(
                            item.quantity
                          )}
                        </span>
                      </td>

                      <td>{item.unit}</td>

                      <td>
                        <span
                          className={`sd-front-stock-status ${status.className}`}
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
      )}

      {/* Cửa sổ xem lịch sử phiếu xuất ra quầy.
          Admin chọn "Tất cả" thì không truyền branchId. */}
      <TicketHistoryModal
        open={showExportTickets}
        onClose={() =>
          setShowExportTickets(false)
        }
        type="frontStockExport"
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