import { useState, useEffect } from 'react';
import { getInventoryReport } from '../../api/InventoryApi'; // Import API sạch sẽ

export function InventoryTab({ currentUser, branches }) {
  const [inventory, setInventory] = useState([]);
  const [selectedBranch, setSelectedBranch] = useState('');
  const [loading, setLoading] = useState(false);

  // Nhận diện phân quyền
  const isAdmin = currentUser?.roleName === 'ADMIN' || currentUser?.role?.toUpperCase() === 'ADMIN';

  useEffect(() => {
    // Nếu không phải Admin (Manager/Staff), tự động khóa bằng branchId của user đó
    if (!isAdmin && currentUser?.branchId) {
      setSelectedBranch(currentUser.branchId);
    } else {
      setSelectedBranch('');
    }
  }, [currentUser, isAdmin]);

  useEffect(() => {
    loadInventoryData();
  }, [selectedBranch]);

  async function loadInventoryData() {
    setLoading(true);
    try {
      // Nếu chọn lọc chi nhánh (Admin), truyền ID; còn lại bỏ trống API sẽ tự ép lấy theo token
      const branchIdToFetch = selectedBranch || '';
      const data = await getInventoryReport(branchIdToFetch);
      setInventory(data || []);
    } catch (e) {
      console.error("Lỗi tải báo cáo tồn kho:", e);
    } finally {
      setLoading(false);
    }
  }

  return (
    <div className="sd-profile-layout">
      <div className="sd-card">
        <div className="sd-card-header">
          <p className="sd-eyebrow">Báo cáo</p>
          <h2>{isAdmin ? 'Tồn kho toàn hệ thống' : 'Tồn kho cơ sở của bạn'}</h2>
        </div>

        {/* 👉 Ô chọn chi nhánh: Chỉ hiển thị nếu tài khoản đăng nhập là ADMIN */}
        {isAdmin && branches && branches.length > 0 && (
          <div className="sd-modal-grid" style={{ marginBottom: 20 }}>
            <div className="sd-field">
              <label>Lọc theo cơ sở</label>
              <select value={selectedBranch} onChange={(e) => setSelectedBranch(e.target.value)}>
                <option value="">-- Tất cả cơ sở --</option>
                {branches.map(b => <option key={b.id} value={b.id}>{b.branchName || b.name}</option>)}
              </select>
            </div>
          </div>
        )}

        {loading ? (
          <p style={{ textAlign: 'center', padding: 30 }}>Đang tải dữ liệu tồn kho...</p>
        ) : (
          <div className="sd-table-wrap">
            <table className="sd-table">
              <thead>
                <tr>
                  {isAdmin && <th className="sd-th">Cơ sở quản lý</th>}
                  <th className="sd-th">Tên vật tư / hàng hóa</th>
                  <th className="sd-th sd-text-right" style={{ width: 150 }}>Số lượng tồn</th>
                  <th className="sd-th sd-text-center" style={{ width: 120 }}>Đơn vị tính</th>
                </tr>
              </thead>
              <tbody>
                {inventory.length === 0 ? (
                  <tr><td colSpan={isAdmin ? 4 : 3} className="sd-td sd-text-center">Chưa có dữ liệu hàng hóa</td></tr>
                ) : (
                  inventory.map(item => (
                    <tr key={item.id} className="sd-tr">
                      {isAdmin && <td className="sd-td">{item.branchName}</td>}
                      <td className="sd-td"><strong>{item.productName}</strong></td>
                      
                      {/* Bôi đỏ cảnh báo nếu tồn kho dưới định mức (ví dụ: tồn < 10) */}
                      <td className={`sd-td sd-text-right ${item.quantity < 10 ? 'sd-text-bold' : ''}`} 
                          style={{ color: item.quantity < 10 ? '#ef4444' : 'inherit' }}>
                        {item.quantity}
                      </td>
                      <td className="sd-td sd-text-center">{item.unit || 'Cái'}</td>
                    </tr>
                  ))
                )}
              </tbody>
            </table>
          </div>
        )}
      </div>
    </div>
  );
}