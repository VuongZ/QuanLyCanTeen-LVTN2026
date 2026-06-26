import { useState, useEffect } from 'react';
import axios from 'axios';

export function AdminSupplierTab() {
  const [suppliers, setSuppliers] = useState([]);
  const [form, setForm] = useState({ supplierName: '', phone: '', address: '' });
  const [error, setError] = useState('');

  useEffect(() => {
    loadSuppliers();
  }, []);

  async function loadSuppliers() {
    try {
      const res = await axios.get('/api/Supplier');
      setSuppliers(res.data || []);
    } catch (e) {
      console.error("Lỗi tải nhà cung cấp", e);
    }
  }

  async function handleAdd(e) {
    e.preventDefault();
    setError('');
    try {
      await axios.post('/api/Supplier', form);
      setForm({ supplierName: '', phone: '', address: '' });
      loadSuppliers();
      alert("✅ Thêm nhà cung cấp thành công!");
    } catch (e) {
      setError(e.response?.data?.message || "Lỗi thêm nhà cung cấp");
    }
  }

  return (
    <div className="sd-profile-layout">
      <div className="sd-card">
        <div className="sd-card-header"><p className="sd-eyebrow">Quản trị</p><h2>Khai báo Nhà cung cấp mới</h2></div>
        <form onSubmit={handleAdd}>
          <div className="sd-field"><label>Tên nhà cung cấp *</label><input required value={form.supplierName} onChange={e => setForm({...form, supplierName: e.target.value})} /></div>
          <div className="sd-field"><label>Số điện thoại</label><input value={form.phone} onChange={e => setForm({...form, phone: e.target.value})} /></div>
          <div className="sd-field"><label>Địa chỉ</label><input value={form.address} onChange={e => setForm({...form, address: e.target.value})} /></div>
          {error && <p className="sd-status sd-status-error">{error}</p>}
          <button className="sd-btn-primary" type="submit">Thêm nhà cung cấp</button>
        </form>
      </div>

      <div className="sd-card">
        <div className="sd-card-header"><p className="sd-eyebrow">Danh mục</p><h2>Danh sách Nhà cung cấp hệ thống</h2></div>
        <div className="sd-table-wrap">
          <table className="sd-table">
            <thead>
              <tr><th className="sd-th">Tên nhà cung cấp</th><th className="sd-th">SĐT</th><th className="sd-th">Địa chỉ</th></tr>
            </thead>
            <tbody>
              {suppliers.map(s => (
                <tr key={s.id} className="sd-tr">
                  <td className="sd-td"><strong>{s.supplierName}</strong></td>
                  <td className="sd-td">{s.phone || '—'}</td>
                  <td className="sd-td">{s.address || '—'}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </div>
    </div>
  );
}