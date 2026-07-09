import { useState, useEffect, useMemo } from 'react';
import axios from 'axios';
import * as XLSX from 'xlsx';
import { getAllSuppliers } from '../../api/SupplierApi';

function formatMoney(value) {
  return new Intl.NumberFormat('vi-VN', {
    style: 'currency',
    currency: 'VND',
    maximumFractionDigits: 0,
  }).format(Number(value || 0));
}

function normalizeText(value = '') {
  return String(value || '')
    .normalize('NFD')
    .replace(/[\u0300-\u036f]/g, '')
    .trim()
    .toLowerCase();
}

function cleanCell(value) {
  return String(value ?? '').trim();
}

function parseNumber(value) {
  if (typeof value === 'number') return value;

  const cleaned = String(value ?? '')
    .replace(/[₫đĐ\s]/g, '')
    .replace(/\./g, '')
    .replace(',', '.');

  const parsed = Number(cleaned);
  return Number.isFinite(parsed) ? parsed : 0;
}

function parseExcelDate(value) {
  if (!value) return '';

  if (value instanceof Date) {
    const yyyy = value.getFullYear();
    const mm = String(value.getMonth() + 1).padStart(2, '0');
    const dd = String(value.getDate()).padStart(2, '0');
    return `${yyyy}-${mm}-${dd}`;
  }

  if (typeof value === 'number') {
    const date = XLSX.SSF.parse_date_code(value);
    if (!date) return '';

    const yyyy = date.y;
    const mm = String(date.m).padStart(2, '0');
    const dd = String(date.d).padStart(2, '0');
    return `${yyyy}-${mm}-${dd}`;
  }

  const raw = String(value).trim();

  if (/^\d{4}-\d{2}-\d{2}$/.test(raw)) {
    return raw;
  }

  const parts = raw.split(/[/-]/).map((item) => item.trim());
  if (parts.length === 3) {
    const [day, month, year] = parts;
    if (year?.length === 4) {
      return `${year}-${String(month).padStart(2, '0')}-${String(day).padStart(2, '0')}`;
    }
  }

  return '';
}

function getSupplierName(supplier) {
  return supplier?.supplier_name || supplier?.supplierName || supplier?.SupplierName || supplier?.name || '';
}

function getSupplierId(supplier) {
  return supplier?.id || supplier?.Id;
}

export function ManagerImportTab({ user, branches }) {
  const [file, setFile] = useState(null);
  const [previewData, setPreviewData] = useState([]);
  const [suppliers, setSuppliers] = useState([]);
  const [selectedSupplier, setSelectedSupplier] = useState('');
  const [detectedSupplierName, setDetectedSupplierName] = useState('');
  const [invoiceCode, setInvoiceCode] = useState('');
  const [invoiceDate, setInvoiceDate] = useState('');
  const [note, setNote] = useState('');
  const [excelTotal, setExcelTotal] = useState(0);
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [message, setMessage] = useState(null);

  const currentBranch = branches?.find((branch) => String(branch.id) === String(user.branchId));

  useEffect(() => {
    getAllSuppliers()
      .then((data) => {
        setSuppliers(Array.isArray(data) ? data : []);
      })
      .catch((err) => {
        console.error('Lỗi tải nhà cung cấp:', err);
        setSuppliers([]);
      });
  }, []);

  useEffect(() => {
    if (!detectedSupplierName || selectedSupplier || suppliers.length === 0) return;

    const matchedSupplier = suppliers.find((supplier) => {
      return normalizeText(getSupplierName(supplier)) === normalizeText(detectedSupplierName);
    });

    if (matchedSupplier) {
      setSelectedSupplier(String(getSupplierId(matchedSupplier)));
    }
  }, [detectedSupplierName, selectedSupplier, suppliers]);

  const totalAmount = useMemo(() => {
    return previewData.reduce(
      (sum, item) => sum + Number(item.quantity || 0) * Number(item.unitPrice || 0),
      0
    );
  }, [previewData]);

  async function handleFileUpload(event) {
    const uploadedFile = event.target.files?.[0];

    setFile(uploadedFile || null);
    setMessage(null);
    setDetectedSupplierName('');
    setSelectedSupplier('');
    setInvoiceCode('');
    setInvoiceDate('');
    setNote('');
    setExcelTotal(0);
    setPreviewData([]);

    if (!uploadedFile) return;

    try {
      const buffer = await uploadedFile.arrayBuffer();

      const workbook = XLSX.read(buffer, {
        type: 'array',
        cellDates: true,
      });

      const sheetName = workbook.SheetNames[0];
      const worksheet = workbook.Sheets[sheetName];

      const rows = XLSX.utils.sheet_to_json(worksheet, {
        header: 1,
        defval: '',
      });

      const supplierName = cleanCell(rows?.[1]?.[1]); // B2
      const nextInvoiceCode = cleanCell(rows?.[2]?.[1]); // B3
      const nextInvoiceDate = parseExcelDate(rows?.[2]?.[3]); // D3

      setDetectedSupplierName(supplierName);
      setInvoiceCode(nextInvoiceCode);
      setInvoiceDate(nextInvoiceDate);

      const matchedSupplier = suppliers.find((supplier) => {
        return normalizeText(getSupplierName(supplier)) === normalizeText(supplierName);
      });

      if (matchedSupplier) {
        setSelectedSupplier(String(getSupplierId(matchedSupplier)));
      }

      const parsedItems = [];

      // Dữ liệu bắt đầu từ dòng 6 trong Excel, index mảng = 5
      for (let i = 5; i < rows.length; i += 1) {
        const row = rows[i] || [];

        const rowText = normalizeText(row.join(' '));
        if (rowText.includes('tong tien')) {
          setExcelTotal(parseNumber(row[6]));
          continue;
        }

        const productCode = cleanCell(row[1]); // B: Mã SP
        const productName = cleanCell(row[2]); // C: Tên SP
        const unit = cleanCell(row[3]); // D: Đơn vị
        const quantity = parseNumber(row[4]); // E: Số lượng
        const unitPrice = parseNumber(row[5]); // F: Đơn giá

        const isEmptyRow = !productCode && !productName && !unit && quantity === 0 && unitPrice === 0;
        if (isEmptyRow) continue;

        if (!productName) continue;

        parsedItems.push({
          idId: `${productCode || productName}-${i}`,
          productId: 0,
          productCode,
          productName,
          unit: unit || 'Cái',
          quantity,
          unitPrice,
        });
      }

      const calculatedTotal = parsedItems.reduce(
        (sum, item) => sum + Number(item.quantity || 0) * Number(item.unitPrice || 0),
        0
      );

      setExcelTotal(calculatedTotal);
      setPreviewData(parsedItems);

      if (parsedItems.length === 0) {
        setMessage({
          type: 'error',
          text: 'File Excel không có dòng sản phẩm hợp lệ. Vui lòng kiểm tra lại mẫu file.',
        });
      }
    } catch (error) {
      console.error('Lỗi đọc Excel:', error);
      setMessage({
        type: 'error',
        text: 'Không đọc được file Excel. Vui lòng kiểm tra lại định dạng file.',
      });
    }
  }

  function handleEditItem(index, field, value) {
    setPreviewData((items) => {
      const updated = [...items];
      const nextItem = { ...updated[index] };

      if (field === 'quantity') {
        nextItem.quantity = parseNumber(value);
      } else if (field === 'unitPrice') {
        nextItem.unitPrice = parseNumber(value);
      } else {
        nextItem[field] = value;
      }

      updated[index] = nextItem;
      return updated;
    });
  }

  async function handleSubmit() {
    if (!selectedSupplier) {
      setMessage({ type: 'error', text: 'Vui lòng chọn hoặc kiểm tra lại Nhà phân phối.' });
      return;
    }

    if (previewData.length === 0) {
      setMessage({ type: 'error', text: 'Không có dữ liệu hàng hóa để nhập kho.' });
      return;
    }

    const invalidItem = previewData.find((item) => {
      return !item.productName || Number(item.quantity || 0) <= 0 || Number(item.unitPrice || 0) < 0;
    });

    if (invalidItem) {
      setMessage({
        type: 'error',
        text: 'Danh sách hàng hóa có sản phẩm thiếu tên, số lượng không hợp lệ hoặc đơn giá âm.',
      });
      return;
    }

    setIsSubmitting(true);
    setMessage(null);

    const payload = {
      managerId: user.id,
      branchId: user.branchId,
      supplierId: Number(selectedSupplier),
      invoiceCode: invoiceCode || null,
      invoiceDate: invoiceDate || null,
      note: note || null,
      items: previewData.map((item) => ({
        productId: Number(item.productId || 0),
        productCode: item.productCode || null,
        productName: item.productName,
        unit: item.unit || 'Cái',
        quantity: Number(item.quantity || 0),
        unitPrice: Number(item.unitPrice || 0),
      })),
    };

    try {
      const response = await axios.post('/api/KhoImport/submit-import', payload);

      setMessage({
        type: 'success',
        text: response.data?.message || 'Đã xác nhận phiếu và cộng dồn vào kho của cơ sở thành công.',
      });

      setPreviewData([]);
      setFile(null);
      setDetectedSupplierName('');
      setSelectedSupplier('');
      setInvoiceCode('');
      setInvoiceDate('');
      setNote('');
      setExcelTotal(0);

      const input = document.getElementById('excel-upload');
      if (input) input.value = '';
    } catch (error) {
      console.error('Lỗi chi tiết từ server:', error.response?.data);

      setMessage({
        type: 'error',
        text: 'Lỗi nhập kho: ' + (error.response?.data?.message || error.message),
      });
    } finally {
      setIsSubmitting(false);
    }
  }

  return (
    <div className="sd-card">
      <div className="sd-card-header">
        <p className="sd-eyebrow">Kho chi nhánh</p>
        <h2>Nhập kho hàng hóa</h2>
      </div>

      <div
        className="sd-info-hero"
        style={{ background: '#f8fafc', border: '1px solid #e2e8f0', marginBottom: 20 }}
      >
        <div className="sd-info-avatar" style={{ background: '#1e293b' }}>🏢</div>
        <div>
          <p className="sd-eyebrow" style={{ color: '#64748b' }}>Cơ sở quản lý</p>
          <h3 style={{ color: '#1e293b', fontWeight: 700 }}>
            {currentBranch?.name || user.branchName || 'Chi nhánh hiện tại'}
          </h3>
          <span className="sd-text-muted" style={{ fontSize: 12 }}>
            📍 {currentBranch?.address || 'Hệ thống nội bộ'}
          </span>
        </div>
      </div>

      <div className="sd-modal-grid" style={{ marginBottom: 20 }}>
        <div className="sd-field">
          <label>Nhà phân phối hệ thống *</label>
          <select value={selectedSupplier} onChange={(event) => setSelectedSupplier(event.target.value)}>
            <option value="">-- Chọn nhà phân phối --</option>
            {suppliers.map((supplier) => {
              const id = getSupplierId(supplier);
              const name = getSupplierName(supplier);

              return (
                <option key={id} value={id}>
                  {name}
                </option>
              );
            })}
          </select>
        </div>

        <div className="sd-field">
          <label>Nhà cung cấp nhận diện từ Excel</label>
          <input
            type="text"
            value={detectedSupplierName || 'Chưa tải file hoặc không tìm thấy'}
            disabled
            style={{
              backgroundColor: '#f1f5f9',
              color: selectedSupplier ? '#166534' : '#b91c1c',
              fontWeight: 600,
            }}
          />
        </div>

        <div className="sd-field">
          <label>Mã hóa đơn</label>
          <input
            type="text"
            value={invoiceCode}
            onChange={(event) => setInvoiceCode(event.target.value)}
            placeholder="VD: HD001"
          />
        </div>

        <div className="sd-field">
          <label>Ngày hóa đơn</label>
          <input
            type="date"
            value={invoiceDate}
            onChange={(event) => setInvoiceDate(event.target.value)}
          />
        </div>

        <div className="sd-field">
          <label>Ghi chú</label>
          <input
            type="text"
            value={note}
            onChange={(event) => setNote(event.target.value)}
            placeholder="VD: Hóa đơn giao hàng buổi sáng"
          />
        </div>

        <div className="sd-field">
          <label>Tổng tiền tạm tính</label>
          <input
            type="text"
            value={formatMoney(totalAmount || excelTotal)}
            disabled
            style={{ backgroundColor: '#f1f5f9', fontWeight: 700 }}
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
        {file && (
          <p className="sd-text-muted" style={{ fontSize: 12, marginTop: 6 }}>
            File đã chọn: {file.name}
          </p>
        )}
        {detectedSupplierName && !selectedSupplier && (
          <p className="sd-status sd-status-error" style={{ marginTop: 6, padding: '4px 8px' }}>
            Hệ thống đọc được tên "{detectedSupplierName}" trong file nhưng không khớp với Nhà cung cấp nào trong cơ sở dữ liệu.
            Vui lòng chọn thủ công bằng ô bên trên.
          </p>
        )}
      </div>

      {previewData.length > 0 && (
        <>
          <div className="sd-flex-between" style={{ marginBottom: 10 }}>
            <h3 style={{ fontSize: 14, color: '#1e293b', fontWeight: 700 }}>
              Danh sách hàng hóa trên hóa đơn:
            </h3>
          </div>

          <div className="sd-table-wrap sd-box-bordered" style={{ marginBottom: 20 }}>
            <table className="sd-table">
              <thead style={{ background: '#f8fafc' }}>
                <tr>
                  <th className="sd-th sd-text-center" style={{ width: 100 }}>Mã SP</th>
                  <th className="sd-th">Tên mặt hàng</th>
                  <th className="sd-th" style={{ width: 110 }}>Đơn vị</th>
                  <th className="sd-th sd-text-right" style={{ width: 130 }}>Số lượng nhập</th>
                  <th className="sd-th sd-text-right" style={{ width: 140 }}>Đơn giá</th>
                  <th className="sd-th sd-text-right" style={{ width: 150 }}>Thành tiền</th>
                </tr>
              </thead>
              <tbody>
                {previewData.map((item, index) => (
                  <tr key={item.idId} className="sd-tr">
                    <td className="sd-td sd-text-center">
                      {item.productCode || '—'}
                    </td>
                    <td className="sd-td" style={{ fontWeight: 500 }}>
                      {item.productName}
                    </td>
                    <td className="sd-td">
                      <input
                        type="text"
                        value={item.unit}
                        onChange={(event) => handleEditItem(index, 'unit', event.target.value)}
                        style={{ width: 90, padding: '5px', border: '1px solid #cbd5e1', borderRadius: 6 }}
                      />
                    </td>
                    <td className="sd-td sd-text-right">
                      <input
                        type="number"
                        value={item.quantity}
                        onChange={(event) => handleEditItem(index, 'quantity', event.target.value)}
                        style={{ width: 90, padding: '5px', textAlign: 'right', border: '1px solid #cbd5e1', borderRadius: 6 }}
                        min="1"
                      />
                    </td>
                    <td className="sd-td sd-text-right">
                      <input
                        type="number"
                        value={item.unitPrice}
                        onChange={(event) => handleEditItem(index, 'unitPrice', event.target.value)}
                        style={{ width: 120, padding: '5px', textAlign: 'right', border: '1px solid #cbd5e1', borderRadius: 6 }}
                        min="0"
                      />
                    </td>
                    <td className="sd-td sd-text-right sd-text-bold" style={{ color: '#ea580c' }}>
                      {formatMoney(Number(item.quantity || 0) * Number(item.unitPrice || 0))}
                    </td>
                  </tr>
                ))}
              </tbody>
              <tfoot>
                <tr style={{ background: '#fff7ed', borderTop: '2px solid #fed7aa' }}>
                  <td colSpan="5" className="sd-td sd-text-right sd-text-bold" style={{ color: '#c2410c' }}>
                    Tổng giá trị đơn nhập:
                  </td>
                  <td className="sd-td sd-text-right sd-text-bold" style={{ fontSize: 15, color: '#ea580c' }}>
                    {formatMoney(totalAmount)}
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
          {isSubmitting ? 'Đang kiểm tra & lưu kho...' : 'Xác nhận nhập kho'}
        </button>
      </div>
    </div>
  );
}