import { useCallback, useEffect, useState } from 'react'
import {
  createSupplier,
  deleteSupplier,
  getAllSuppliers,
  getDeletedSuppliers,
  restoreSupplier,
  updateSupplier,
} from '../../api/SupplierApi'

const EMPTY_FORM = {
  supplierName: '',
  phone: '',
  address: '',
}

export function AdminSupplierTab() {
  const [suppliers, setSuppliers] = useState([])
  const [showDeleted, setShowDeleted] = useState(false)
  const [form, setForm] = useState(EMPTY_FORM)
  const [error, setError] = useState('')
  const [loading, setLoading] = useState(false)
  const [workingId, setWorkingId] = useState(null)
  const [editingId, setEditingId] = useState(null)
  const [saving, setSaving] = useState(false)

  const isEditing = editingId !== null

  const loadSuppliers = useCallback(async (deleted) => {
    setLoading(true)
    setError('')

    try {
      const data = deleted
        ? await getDeletedSuppliers()
        : await getAllSuppliers()

      setSuppliers(Array.isArray(data) ? data : [])
      setShowDeleted(deleted)
    } catch (err) {
      setSuppliers([])
      setError(
        err.response?.data?.message ||
        'Không thể tải danh sách nhà phân phối.'
      )
    } finally {
      setLoading(false)
    }
  }, [])

  useEffect(() => {
    loadSuppliers(false)
  }, [loadSuppliers])

  function resetForm() {
    setForm(EMPTY_FORM)
    setEditingId(null)
  }

  function handleFormChange(event) {
    const { name, value } = event.target

    setForm((current) => ({
      ...current,
      [name]: value,
    }))
  }

  function handleEdit(supplier) {
    setEditingId(supplier.id)

    setForm({
      supplierName: supplier.supplierName || '',
      phone: supplier.phone || '',
      address: supplier.address || '',
    })

    setError('')
  }

  function handleChangeList(deleted) {
    if (deleted) {
      resetForm()
    }

    loadSuppliers(deleted)
  }

  async function handleSubmit(event) {
    event.preventDefault()

    const supplierName = form.supplierName.trim()

    if (!supplierName) {
      setError('Vui lòng nhập tên nhà phân phối.')
      return
    }

    const payload = {
      supplierName,
      phone: form.phone.trim(),
      address: form.address.trim(),
    }

    setError('')
    setSaving(true)

    try {
      if (isEditing) {
        await updateSupplier(editingId, payload)
      } else {
        await createSupplier(payload)
      }

      resetForm()
      await loadSuppliers(false)
    } catch (err) {
      setError(
        err.response?.data?.message ||
        (isEditing
          ? 'Không thể cập nhật nhà phân phối.'
          : 'Không thể thêm nhà phân phối.')
      )
    } finally {
      setSaving(false)
    }
  }

  async function handleDelete(supplier) {
    const confirmed = window.confirm(
      `Ngừng hoạt động nhà phân phối “${supplier.supplierName}”? Bạn có thể khôi phục sau.`
    )

    if (!confirmed) return

    setWorkingId(supplier.id)
    setError('')

    try {
      await deleteSupplier(supplier.id)

      setSuppliers((current) =>
        current.filter((item) => item.id !== supplier.id)
      )

      if (editingId === supplier.id) {
        resetForm()
      }
    } catch (err) {
      setError(
        err.response?.data?.message ||
        'Không thể ngừng hoạt động nhà phân phối.'
      )
    } finally {
      setWorkingId(null)
    }
  }

  async function handleRestore(supplier) {
    setWorkingId(supplier.id)
    setError('')

    try {
      await restoreSupplier(supplier.id)

      setSuppliers((current) =>
        current.filter((item) => item.id !== supplier.id)
      )
    } catch (err) {
      setError(
        err.response?.data?.message ||
        'Không thể khôi phục nhà phân phối.'
      )
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

            <h2>
              {isEditing
                ? 'Cập nhật nhà phân phối'
                : 'Khai báo nhà phân phối mới'}
            </h2>
          </div>

          <form onSubmit={handleSubmit}>
            <div className="sd-field">
              <label htmlFor="supplier-name">
                Tên nhà phân phối *
              </label>

              <input
                id="supplier-name"
                name="supplierName"
                required
                value={form.supplierName}
                onChange={handleFormChange}
                disabled={saving}
              />
            </div>

            <div className="sd-field">
              <label htmlFor="supplier-phone">
                Số điện thoại
              </label>

              <input
                id="supplier-phone"
                name="phone"
                value={form.phone}
                onChange={handleFormChange}
                disabled={saving}
              />
            </div>

            <div className="sd-field">
              <label htmlFor="supplier-address">
                Địa chỉ
              </label>

              <input
                id="supplier-address"
                name="address"
                value={form.address}
                onChange={handleFormChange}
                disabled={saving}
              />
            </div>

            <button
              className="sd-btn-primary"
              type="submit"
              disabled={saving}
            >
              {saving
                ? 'Đang lưu...'
                : isEditing
                  ? 'Lưu thay đổi'
                  : 'Thêm nhà phân phối'}
            </button>

            {isEditing && (
              <button
                className="sd-btn-ghost"
                type="button"
                onClick={resetForm}
                disabled={saving}
                style={{ marginTop: 8 }}
              >
                Hủy chỉnh sửa
              </button>
            )}
          </form>
        </div>
      )}

      <div className="sd-card">
        <div className="sd-card-header">
          <p className="sd-eyebrow">Danh mục</p>

          <h2>
            {showDeleted
              ? 'Nhà phân phối đã ngừng hoạt động'
              : 'Danh sách nhà phân phối hệ thống'}
          </h2>
        </div>

        <div
          className="sd-filter-chips"
          style={{ marginBottom: 16 }}
        >
          <button
            className={`sd-filter-chip ${
              !showDeleted ? 'active' : ''
            }`}
            type="button"
            onClick={() => handleChangeList(false)}
            disabled={loading}
          >
            Đang hoạt động
          </button>

          <button
            className={`sd-filter-chip sd-filter-chip-deleted ${
              showDeleted ? 'active' : ''
            }`}
            type="button"
            onClick={() => handleChangeList(true)}
            disabled={loading}
          >
            Đã ngừng hoạt động
          </button>
        </div>

        {error && (
          <p className="sd-status sd-status-error">
            {error}
          </p>
        )}

        <div className="sd-table-wrap">
          <table className="sd-table">
            <thead>
              <tr>
                <th className="sd-th">
                  Tên nhà phân phối
                </th>

                <th className="sd-th">
                  SĐT
                </th>

                <th className="sd-th">
                  Địa chỉ
                </th>

                <th className="sd-th sd-th-action">
                  Thao tác
                </th>
              </tr>
            </thead>

            <tbody>
              {!loading &&
                suppliers.map((supplier) => (
                  <tr
                    key={supplier.id}
                    className="sd-tr"
                  >
                    <td className="sd-td">
                      <strong>
                        {supplier.supplierName}
                      </strong>
                    </td>

                    <td className="sd-td">
                      {supplier.phone || '—'}
                    </td>

                    <td className="sd-td">
                      {supplier.address || '—'}
                    </td>

                    <td className="sd-td sd-td-action">
                      {showDeleted ? (
                        <button
                          className="sd-btn-restore"
                          type="button"
                          disabled={
                            workingId === supplier.id ||
                            saving
                          }
                          onClick={() =>
                            handleRestore(supplier)
                          }
                        >
                          {workingId === supplier.id
                            ? 'Đang khôi phục...'
                            : '↻ Khôi phục'}
                        </button>
                      ) : (
                        <div
                          style={{
                            display: 'flex',
                            justifyContent: 'flex-end',
                            gap: 8,
                          }}
                        >
                          <button
                            className="sd-btn-ghost btn-edit"
                            type="button"
                            disabled={
                              workingId === supplier.id ||
                              saving
                            }
                            onClick={() =>
                              handleEdit(supplier)
                            }
                          >
                            Sửa
                          </button>

                          <button
                            className="sd-btn-ghost btn-delete"
                            type="button"
                            disabled={
                              workingId === supplier.id ||
                              saving
                            }
                            onClick={() =>
                              handleDelete(supplier)
                            }
                          >
                            {workingId === supplier.id
                              ? 'Đang xử lý...'
                              : 'Ngừng hoạt động'}
                          </button>
                        </div>
                      )}
                    </td>
                  </tr>
                ))}

              {(loading || suppliers.length === 0) && (
                <tr>
                  <td
                    colSpan="4"
                    className="sd-td-empty"
                  >
                    {loading
                      ? 'Đang tải danh sách...'
                      : showDeleted
                        ? 'Không có nhà phân phối đã ngừng hoạt động.'
                        : 'Chưa có nhà phân phối.'}
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