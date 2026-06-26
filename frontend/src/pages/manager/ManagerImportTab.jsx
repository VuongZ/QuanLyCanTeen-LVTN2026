import { useState, useEffect } from 'react';
import axios from 'axios';
import * as XLSX from 'xlsx';
import { getAllSuppliers } from '../../api/SupplierApi';

export function ManagerImportTab({ user, branches }) {
    const [file, setFile] = useState(null);
    const [previewData, setPreviewData] = useState([]);
    const [suppliers, setSuppliers] = useState([]);
    const [selectedSupplier, setSelectedSupplier] = useState('');
    const [detectedSupplierName, setDetectedSupplierName] = useState('');
    const [isSubmitting, setIsSubmitting] = useState(false);
    const [message, setMessage] = useState(null);

    // Lấy tên Chi nhánh hiện tại của Manager từ props branches gửi từ App.jsx vào
    const currentBranch = branches?.find(b => b.id === user.branchId);

    // 👉 DÁN ĐOẠN NÀY VÀO: Hàm tự động tải danh sách Nhà cung cấp khi mở Tab
  useEffect(() => {
    getAllSuppliers()
      .then(data => {
        setSuppliers(data || []);
      })
      .catch(err => {
        console.error("Lỗi tải nhà cung cấp:", err);
      });
  }, []);

    // Xử lý đọc file Excel hóa đơn hàng hóa
    const handleFileUpload = (e) => {
        const uploadedFile = e.target.files[0];
        setFile(uploadedFile);
        setMessage(null);
        setDetectedSupplierName('');
        setSelectedSupplier('');

        if (!uploadedFile) return;

        const reader = new FileReader();
        reader.onload = (evt) => {
            const bstr = evt.target.result;
            const wb = XLSX.read(bstr, { type: 'binary' });
            const wsname = wb.SheetNames[0];
            const ws = wb.Sheets[wsname];

            // 1. Đọc ô B2 để lấy tên Nhà cung cấp trên hóa đơn
            const supplierCell = ws['B2'];
            const excelSupplierName = supplierCell ? supplierCell.v?.toString().trim() : '';

            if (excelSupplierName) {
                setDetectedSupplierName(excelSupplierName);

                // 🔥 SỬA CHÍNH XÁC Ở ĐÂY: Kiểm tra s.supplier_name hoặc s.supplierName hoặc s.SupplierName
                const match = suppliers.find(s => {
                    const dbName = s.supplier_name || s.supplierName || s.SupplierName || '';
                    return dbName.toLowerCase() === excelSupplierName.toLowerCase();
                });

                if (match) {
                    // Lấy ID tương ứng (phòng hờ viết hoa viết thường)
                    const targetId = match.id || match.Id;
                    setSelectedSupplier(targetId.toString());
                }
            }

            // 2. Khai báo và gán dữ liệu Excel thành JSON cho biến `data` từ dòng 4
            const data = XLSX.utils.sheet_to_json(ws, { range: 3 });

            // 3. Map dữ liệu từ biến `data` 
            const formattedData = data.map((row, index) => ({
                idId: index,
                productId: row['Mã SP'] || row['ProductId'] || 0,
                productName: row['Tên SP'] || row['ProductName'] || 'Chưa rõ',
                quantity: row['Số lượng'] || row['Số Lượng'] || row['Quantity'] || 0,
                unitPrice: row['Đơn giá'] || row['Đơn Giá'] || row['UnitPrice'] || 0
            })).filter(item => item.productName !== 'Chưa rõ');

            // 4. Đổ dữ liệu sạch vào bảng Preview
            setPreviewData(formattedData);
        };
        reader.readAsBinaryString(uploadedFile);
    };

    const handleEditQuantity = (index, newQuantity) => {
        const updatedData = [...previewData];
        updatedData[index].quantity = parseInt(newQuantity) || 0;
        setPreviewData(updatedData);
    };

    const handleSubmit = async () => {
        if (!selectedSupplier) {
            setMessage({ type: 'error', text: 'Vui lòng chọn hoặc kiểm tra lại Nhà phân phối!' });
            return;
        }
        if (previewData.length === 0) {
            setMessage({ type: 'error', text: 'Không có dữ liệu hàng hóa để nhập kho!' });
            return;
        }

        setIsSubmitting(true);
        setMessage(null);

        // Payload sạch sẽ gửi lên C# API
        const payload = {
            managerId: user.id,
            branchId: user.branchId, // Lấy thẳng từ thông tin Manager đăng nhập, chống gian lận dữ liệu
            supplierId: parseInt(selectedSupplier),
            items: previewData.map(item => ({
                productId: parseInt(item.productId),
                quantity: parseInt(item.quantity),
                productName: item.productName,
                unitPrice: parseFloat(item.unitPrice)
            }))
        };

        try {
            await axios.post('/api/KhoImport/submit-import', payload);
            setMessage({ type: 'success', text: '✅ Đã xác nhận phiếu và cộng dồn vào kho của cơ sở thành công!' });
            setPreviewData([]);
            setFile(null);
            setDetectedSupplierName('');
            setSelectedSupplier('');
            document.getElementById('excel-upload').value = '';
        } catch (error) {
console.error("Lỗi chi tiết từ server:", error.response?.data);
    if (error.response?.data?.errors) {
        console.table(error.response.data.errors); // In chi tiết lỗi từng trường ra bảng console
    }
    setMessage({ type: 'error', text: '❌ Lỗi nhập kho: ' + (error.response?.data?.message || error.message) });
        } finally {
            setIsSubmitting(false);
        }
    };

    const totalAmount = previewData.reduce((sum, item) => sum + (item.quantity * item.unitPrice), 0);

    return (
        <div className="sd-card">
            <div className="sd-card-header">
                <p className="sd-eyebrow">Kho chi nhánh</p>
                <h2>Nhập kho hàng hóa</h2>
            </div>

            {/* Thông tin cố định của cơ sở - KHÔNG CHO CHỌN BỪA BÃI */}
            <div className="sd-info-hero" style={{ background: '#f8fafc', border: '1px solid #e2e8f0', marginBottom: 20 }}>
                <div className="sd-info-avatar" style={{ background: '#1e293b' }}>🏢</div>
                <div>
                    <p className="sd-eyebrow" style={{ color: '#64748b' }}>Cơ sở quản lý</p>
                    <h3 style={{ color: '#1e293b', fontWeight: 700 }}>{currentBranch?.name || user.branchName || "Chi nhánh hiện tại"}</h3>
                    <span className="sd-text-muted" style={{ fontSize: 12 }}>📍 {currentBranch?.address || "Hệ thống nội bộ"}</span>
                </div>
            </div>

            <div className="sd-modal-grid" style={{ marginBottom: 20 }}>
                <div className="sd-field">
                    <label>Nhà phân phối hệ thống *</label>
                    <select value={selectedSupplier} onChange={(e) => setSelectedSupplier(e.target.value)}>
                        <option value="">-- Chọn nhà phân phối --</option>
                        {suppliers.map(s => {
                            // Lấy linh hoạt theo đúng thuộc tính database trả về
                            const name = s.supplier_name || s.supplierName || s.SupplierName;
                            const id = s.id || s.Id;
                            return <option key={id} value={id}>{name}</option>;
                        })}
                    </select>
                </div>

                <div className="sd-field">
                    <label>Nhà cung cấp nhận diện từ Excel</label>
                    <input
                        type="text"
                        value={detectedSupplierName || "Chưa tải file hoặc không tìm thấy"}
                        disabled
                        style={{ backgroundColor: '#f1f5f9', color: selectedSupplier ? '#166534' : '#b91c1c', fontWeight: 600 }}
                    />
                </div>
            </div>

            <div className="sd-field" style={{ marginBottom: 24 }}>
                <label>Chọn file Excel hoá đơn giao hàng *</label>
                <input
                    id="excel-upload"
                    type="file"
                    accept=".xlsx, .xls"
                    onChange={handleFileUpload}
                    style={{ padding: '10px', background: '#f8fafc' }}
                />
                {detectedSupplierName && !selectedSupplier && (
                    <p className="sd-status sd-status-error" style={{ marginTop: 6, padding: '4px 8px' }}>
                        ⚠ Hệ thống đọc được tên "{detectedSupplierName}" trong file nhưng không khớp với Nhà cung cấp nào trong cơ sở dữ liệu. Vui lòng chọn thủ công bằng ô bên cạnh!
                    </p>
                )}
            </div>

            {previewData.length > 0 && (
                <>
                    <div className="sd-flex-between" style={{ marginBottom: 10 }}>
                        <h3 style={{ fontSize: 14, color: '#1e293b', fontWeight: 700 }}>Danh sách vật tư hóa đơn đề xuất:</h3>
                    </div>
                    <div className="sd-table-wrap sd-box-bordered" style={{ marginBottom: 20 }}>
                        <table className="sd-table">
                            <thead style={{ background: '#f8fafc' }}>
                                <tr>
                                    <th className="sd-th sd-text-center" style={{ width: 80 }}>Mã SP</th>
                                    <th className="sd-th">Tên Vật Tư / Mặt Hàng</th>
                                    <th className="sd-th sd-text-right" style={{ width: 120 }}>Số Lượng Nhập</th>
                                    <th className="sd-th sd-text-right" style={{ width: 120 }}>Đơn Giá</th>
                                    <th className="sd-th sd-text-right" style={{ width: 140 }}>Thành Tiền</th>
                                </tr>
                            </thead>
                            <tbody>
                                {previewData.map((item, index) => (
                                    <tr key={item.idId} className="sd-tr">
                                        <td className="sd-td sd-text-center sd-text-bold">#{item.productId}</td>
                                        <td className="sd-td" style={{ fontWeight: 500 }}>{item.productName}</td>
                                        <td className="sd-td sd-text-right">
                                            <input
                                                type="number"
                                                value={item.quantity}
                                                onChange={(e) => handleEditQuantity(index, e.target.value)}
                                                style={{ width: 90, padding: '5px', textAlign: 'right', border: '1px solid #cbd5e1', borderRadius: 6 }}
                                                min="0"
                                            />
                                        </td>
                                        <td className="sd-td sd-text-right" style={{ color: '#475569' }}>{item.unitPrice.toLocaleString('vi-VN')} đ</td>
                                        <td className="sd-td sd-text-right sd-text-bold" style={{ color: '#ea580c' }}>
                                            {(item.quantity * item.unitPrice).toLocaleString('vi-VN')} đ
                                        </td>
                                    </tr>
                                ))}
                            </tbody>
                            <tfoot>
                                <tr style={{ background: '#fff7ed', borderTop: '2px solid #fed7aa' }}>
                                    <td colSpan="4" className="sd-td sd-text-right sd-text-bold" style={{ color: '#c2410c' }}>Tổng giá trị đơn nhập:</td>
                                    <td className="sd-td sd-text-right sd-text-bold" style={{ fontSize: 15, color: '#ea580c' }}>
                                        {totalAmount.toLocaleString('vi-VN')} đ
                                    </td>
                                </tr>
                            </tfoot>
                        </table>
                    </div>
                </>
            )}

            {message && (
                <div className={`sd-status sd-status-${message.type}`} style={{ marginBottom: 16 }}>
                    {message.text}
                </div>
            )}

            <div style={{ display: 'flex', justifyContent: 'flex-end', gap: 12 }}>
                <button
                    className="sd-btn-primary"
                    style={{ width: 'auto', padding: '12px 28px', marginTop: 0 }}
                    disabled={previewData.length === 0 || isSubmitting}
                    onClick={handleSubmit}
                >
                    {isSubmitting ? 'Đang kiểm tra & lưu kho...' : '🔒 Xác nhận nhập kho'}
                </button>
            </div>
        </div>
    );
}