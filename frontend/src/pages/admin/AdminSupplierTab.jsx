import { useCallback, useEffect, useState } from 'react'
import {
  createSupplier,
  deleteSupplier,
  getAllSuppliers,
  getDeletedSuppliers,
  restoreSupplier,
} from '../../api/SupplierApi'

export function AdminSupplierTab() {
  const [suppliers, setSuppliers] = useState([])
  const [showDeleted, setShowDeleted] = useState(false)
  const [form, setForm] = useState({ supplierName: '', phone: '', address: '' })
  const [error, setError] = useState('')
  const [loading, setLoading] = useState(false)
  const [workingId, setWorkingId] = useState(null)

  const loadSuppliers = useCallback(async (deleted) => {
    setLoading(true)
    setError('')
    try {
      const data = deleted ? await getDeletedSuppliers() : await getAllSuppliers()
      setSuppliers(Array.isArray(data) ? data : [])
      setShowDeleted(deleted)
    } catch (err) {
      setError(err.response?.data?.message || 'Không thể tải danh sách nhà cung cấp.')
    } finally {
      setLoading(false)
    }
  }, [])

  useEffect(() => {
    // Loading the initial server state is the purpose of this mount-only effect.
    // eslint-disable-next-line react-hooks/set-state-in-effect
    loadSuppliers(false)
  }, [loadSuppliers])

  async function handleAdd(event) {
    event.preventDefault()
    setError('')
    try {
      await createSupplier(form)
      setForm({ supplierName: '', phone: '', address: '' })
      await loadSuppliers(false)
    } catch (err) {
      setError(err.response?.data?.message || 'Không thể thêm nhà cung cấp.')
    }
  }

  async function handleDelete(supplier) {
    if (!window.confirm(`Xóa nhà cung cấp “${supplier.supplierName}”? Bạn có thể khôi phục sau.`)) return

    setWorkingId(supplier.id)
    setError('')
    try {
      await deleteSupplier(supplier.id)
      setSuppliers((current) => current.filter((item) => item.id !== supplier.id))
    } catch (err) {
      setError(err.response?.data?.message || 'Không thể xóa nhà cung cấp.')
    } finally {
      setWorkingId(null)
    }
  }

  async function handleRestore(supplier) {
    setWorkingId(supplier.id)
    setError('')
    try {
      await restoreSupplier(supplier.id)
      setSuppliers((current) => current.filter((item) => item.id !== supplier.id))
    } catch (err) {
      setError(err.response?.data?.message || 'Không thể khôi phục nhà cung cấp.')
    } finally {
      setWorkingId(null)
    }
  }

  return (
    <div className="sd-profile-layout">
      {!showDeleted && (
        <div className="sd-card">
          <div className="sd-card-header">
            <p className="sd-eyebrow">Quản trị</p>
            <h2>Khai báo nhà cung cấp mới</h2>
          </div>
          <form onSubmit={handleAdd}>
            <div className="sd-field">
              <label>Tên nhà cung cấp *</label>
              <input required value={form.supplierName} onChange={(event) => setForm({ ...form, supplierName: event.target.value })} />
            </div>
            <div className="sd-field">
              <label>Số điện thoại</label>
              <input value={form.phone} onChange={(event) => setForm({ ...form, phone: event.target.value })} />
            </div>
            <div className="sd-field">
              <label>Địa chỉ</label>
              <input value={form.address} onChange={(event) => setForm({ ...form, address: event.target.value })} />
            </div>
            <button className="sd-btn-primary" type="submit">Thêm nhà cung cấp</button>
          </form>
        </div>
      )}

      <div className="sd-card">
        <div className="sd-card-header">
          <p className="sd-eyebrow">Danh mục</p>
          <h2>{showDeleted ? 'Nhà cung cấp đã xóa' : 'Danh sách nhà cung cấp hệ thống'}</h2>
        </div>

        <div className="sd-filter-chips" style={{ marginBottom: 16 }}>
          <button className={`sd-filter-chip ${!showDeleted ? 'active' : ''}`} onClick={() => loadSuppliers(false)}>
            Đang hoạt động
          </button>
          <button className={`sd-filter-chip sd-filter-chip-deleted ${showDeleted ? 'active' : ''}`} onClick={() => loadSuppliers(true)}>
            Đã xóa
          </button>
        </div>

        {error && <p className="sd-status sd-status-error">{error}</p>}

        <div className="sd-table-wrap">
          <table className="sd-table">
            <thead>
              <tr>
                <th className="sd-th">Tên nhà cung cấp</th>
                <th className="sd-th">SĐT</th>
                <th className="sd-th">Địa chỉ</th>
                <th className="sd-th sd-th-action">Thao tác</th>
              </tr>
            </thead>
            <tbody>
              {!loading && suppliers.map((supplier) => (
                <tr key={supplier.id} className="sd-tr">
                  <td className="sd-td"><strong>{supplier.supplierName}</strong></td>
                  <td className="sd-td">{supplier.phone || '—'}</td>
                  <td className="sd-td">{supplier.address || '—'}</td>
                  <td className="sd-td sd-td-action">
                    {showDeleted ? (
                      <button className="sd-btn-restore" type="button" disabled={workingId === supplier.id} onClick={() => handleRestore(supplier)}>
                        {workingId === supplier.id ? 'Đang khôi phục...' : '↻ Khôi phục'}
                      </button>
                    ) : (
                      <button className="sd-btn-ghost btn-delete" type="button" disabled={workingId === supplier.id} onClick={() => handleDelete(supplier)}>
                        {workingId === supplier.id ? 'Đang xóa...' : 'Xóa'}
                      </button>
                    )}
                  </td>
                </tr>
              ))}
              {(loading || suppliers.length === 0) && (
                <tr>
                  <td colSpan="4" className="sd-td-empty">
                    {loading ? 'Đang tải danh sách...' : showDeleted ? 'Không có nhà cung cấp đã xóa.' : 'Chưa có nhà cung cấp.'}
                  </td>
                </tr>
              )}
            </tbody>
          </table>
        </div>
      </div>
    </div>
  )
}
