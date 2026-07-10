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

function getRoleName(user) {
    return normalizeText(user?.roleName || user?.role || '');
}

function getUserBranchId(user) {
    return user?.branchId ?? user?.BranchId ?? user?.branch_id ?? user?.branch?.id ?? '';
}

function normalizeFrontStockItem(item, branches, fallbackBranchId = '') {
    const branchId = getValue(
        item,
        ['branchId', 'BranchId', 'branch_id'],
        fallbackBranchId
    );

    const branch = branches?.find((branchItem) => String(branchItem.id) === String(branchId));

    return {
        id: getValue(item, ['id', 'Id'], ''),
        branchId,
        branchName:
            getValue(item, ['branchName', 'BranchName', 'branch_name'], '') ||
            branch?.name ||
            branch?.branchName ||
            'Cơ sở hiện tại',
        productId: getValue(item, ['productId', 'ProductId', 'product_id'], ''),
        productCode: getValue(item, ['productCode', 'ProductCode', 'product_code'], ''),
        productName:
            getValue(item, ['productName', 'ProductName', 'product_name', 'name', 'Name'], '') ||
            'Chưa rõ mặt hàng',
        unit: getValue(item, ['unit', 'Unit', 'donVi'], 'Cái') || 'Cái',
        quantity: Number(getValue(item, ['quantity', 'Quantity', 'soLuongTon'], 0) || 0),
        supplierName: getValue(item, ['supplierName', 'SupplierName'], ''),
    };
}

export function FrontStockTab({ currentUser, branches = [] }) {
    const roleName = getRoleName(currentUser);
    const isAdmin = roleName.includes('admin') || roleName.includes('quan tri');

    const userBranchId = getUserBranchId(currentUser);
    const currentBranch = branches.find((branch) => String(branch.id) === String(userBranchId));

    const [frontStock, setFrontStock] = useState([]);
    const [selectedBranchId, setSelectedBranchId] = useState(isAdmin ? 'ALL' : String(userBranchId || ''));
    const [search, setSearch] = useState('');
    const [loading, setLoading] = useState(false);
    const [error, setError] = useState('');
    const [showExportTickets, setShowExportTickets] = useState(false);

    async function loadFrontStock() {
        setLoading(true);
        setError('');

        try {
            let response;

            if (isAdmin && selectedBranchId !== 'ALL') {
                response = await axios.get(`/api/FrontStock?branchId=${selectedBranchId}`);
            } else {
                response = await axios.get('/api/FrontStock');
            }

            const data = Array.isArray(response.data) ? response.data : [];
            setFrontStock(data.map((item) => normalizeFrontStockItem(item, branches, userBranchId)));
        } catch (err) {
            setError(err.response?.data?.message || 'Không tải được dữ liệu tồn quầy.');
            setFrontStock([]);
        } finally {
            setLoading(false);
        }
    }

    useEffect(() => {
        if (!currentUser?.id && !currentUser?.Id) return;

        loadFrontStock();

        // eslint-disable-next-line react-hooks/exhaustive-deps
    }, [currentUser?.id, currentUser?.Id, userBranchId, selectedBranchId, branches.length]);

    const filteredFrontStock = useMemo(() => {
        const keyword = normalizeText(search);

        return frontStock
            .filter((item) => {
                if (isAdmin && selectedBranchId !== 'ALL' && String(item.branchId) !== String(selectedBranchId)) {
                    return false;
                }

                if (!isAdmin && userBranchId && item.branchId && String(item.branchId) !== String(userBranchId)) {
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
            .sort((a, b) => a.productName.localeCompare(b.productName, 'vi'));
    }, [frontStock, search, selectedBranchId, isAdmin, userBranchId]);

    const totalProducts = filteredFrontStock.length;
    const totalQuantity = filteredFrontStock.reduce((sum, item) => sum + Number(item.quantity || 0), 0);

    return (
        <div className="sd-front-stock-page">
            <div className="sd-front-stock-hero">
                <div className="sd-front-stock-hero-main">
                    <div className="sd-front-stock-icon">🛒</div>

                    <div>
                        <p className="sd-eyebrow">Tồn quầy</p>
                        <h2>{isAdmin ? 'Tồn quầy toàn hệ thống' : 'Tồn quầy cơ sở'}</h2>
                        <p>
                            {isAdmin
                                ? 'Theo dõi số lượng hàng đang có tại quầy của các cơ sở.'
                                : `Theo dõi hàng đang có tại quầy của ${currentBranch?.name || currentUser?.branchName || 'cơ sở hiện tại'}.`}
                        </p>
                    </div>
                </div>


                <button
                    type="button"
                    className="sd-front-stock-refresh"
                    onClick={loadFrontStock}
                    disabled={loading}
                >
                    {loading ? 'Đang tải...' : 'Làm mới'}
                </button>

                <button
                    type="button"
                    className="sd-front-stock-refresh secondary"
                    onClick={() => setShowExportTickets(true)}
                >
                    Xem phiếu xuất ra quầy
                </button>

            </div>

            <div className="sd-front-stock-stats">
                <div className="sd-front-stock-stat">
                    <span>📦</span>
                    <div>
                        <strong>{formatNumber(totalProducts)}</strong>
                        <p>Mặt hàng tại quầy</p>
                    </div>
                </div>

                <div className="sd-front-stock-stat">
                    <span>🧮</span>
                    <div>
                        <strong>{formatNumber(totalQuantity)}</strong>
                        <p>Tổng số lượng tại quầy</p>
                    </div>
                </div>
            </div>

            <div className="sd-front-stock-toolbar">
                <div className="sd-front-stock-search">
                    <span>⌕</span>
                    <input
                        type="text"
                        value={search}
                        placeholder="Tìm tên hàng, mã hàng, đơn vị..."
                        onChange={(event) => setSearch(event.target.value)}
                    />

                    {search && (
                        <button type="button" onClick={() => setSearch('')}>
                            ✕
                        </button>
                    )}
                </div>

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
                    <div className="sd-front-stock-branch-chip">
                        🏫 {currentBranch?.name || currentUser?.branchName || 'Cơ sở hiện tại'}
                    </div>
                )}
            </div>

            {error && (
                <div className="sd-status sd-status-error sd-front-stock-message">
                    {error}
                </div>
            )}

            {loading ? (
                <div className="sd-front-stock-empty">
                    <span>⏳</span>
                    <p>Đang tải dữ liệu tồn quầy...</p>
                </div>
            ) : filteredFrontStock.length === 0 ? (
                <div className="sd-front-stock-empty">
                    <span>🛒</span>
                    <p>Quầy hiện chưa có mặt hàng nào.</p>
                </div>
            ) : (
                <div className="sd-front-stock-table-wrap">
                    <table className="sd-front-stock-table">
                        <thead>
                            <tr>
                                <th>Mặt hàng</th>
                                {isAdmin && <th>Cơ sở</th>}
                                <th>Mã SP</th>
                                <th className="text-right">Số lượng tại quầy</th>
                                <th>Đơn vị</th>
                            </tr>
                        </thead>

                        <tbody>
                            {filteredFrontStock.map((item, index) => (
                                <tr key={`${item.branchId}-${item.productId}-${item.id || index}`}>
                                    <td>
                                        <div className="sd-front-stock-product">
                                            <div className="sd-front-stock-product-icon">
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
                                            <span className="sd-front-stock-branch-name">{item.branchName}</span>
                                        </td>
                                    )}

                                    <td>
                                        <span className="sd-inventory-code">{item.productCode || '—'}</span>
                                    </td>

                                    <td className="text-right">
                                        <span className="sd-front-stock-quantity">
                                            {formatNumber(item.quantity)}
                                        </span>
                                    </td>

                                    <td>{item.unit}</td>
                                </tr>
                            ))}
                        </tbody>
                    </table>
                </div>
            )}

            <TicketHistoryModal
  open={showExportTickets}
  onClose={() => setShowExportTickets(false)}
  type="frontStockExport"
  branchId={isAdmin ? selectedBranchId : userBranchId}
/>
        </div>
    );
}