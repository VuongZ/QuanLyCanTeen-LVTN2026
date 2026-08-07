import { useCallback, useEffect, useState } from 'react'
import {
  createSupplier,
  deleteSupplier,
  getAllSuppliers,
  getDeletedSuppliers,
  restoreSupplier,
  updateSupplier,
} from '../../api/SupplierApi'
import {
  deactivateProduct,
  getAdminProducts,
  restoreProduct,
} from '../../api/KhoImportApi'
import '../css/AdminSupplierTab.css'

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

  const [products, setProducts] = useState([])
  const [showInactiveProducts, setShowInactiveProducts] = useState(false)
  const [productLoading, setProductLoading] = useState(false)
  const [productError, setProductError] = useState('')
  const [productWorkingId, setProductWorkingId] = useState(null)

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
        'Không thể tải danh sách nhà cung cấp.'
      )
    } finally {
      setLoading(false)
    }
  }, [])

  const loadProducts = useCallback(async (inactive) => {
    setProductLoading(true)
    setProductError('')

    try {
      const data = await getAdminProducts(!inactive)
      setProducts(Array.isArray(data) ? data : [])
      setShowInactiveProducts(inactive)
    } catch (err) {
      setProducts([])
      setProductError(
        err.response?.data?.message ||
        'Không thể tải danh sách sản phẩm.'
      )
    } finally {
      setProductLoading(false)
    }
  }, [])

  useEffect(() => {
    loadSuppliers(false)
    loadProducts(false)
  }, [loadSuppliers, loadProducts])

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
      setError('Vui lòng nhập tên nhà cung cấp.')
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
          ? 'Không thể cập nhật nhà cung cấp.'
          : 'Không thể thêm nhà cung cấp.')
      )
    } finally {
      setSaving(false)
    }
  }

  async function handleDelete(supplier) {
    const confirmed = window.confirm(
      `Ngừng hoạt động nhà cung cấp “${supplier.supplierName}”? Bạn có thể khôi phục sau.`
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
        'Không thể ngừng hoạt động nhà cung cấp.'
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
        'Không thể khôi phục nhà cung cấp.'
      )
    } finally {
      setWorkingId(null)
    }
  }

  async function handleDeactivateProduct(product) {
    const totalInventory =
      Number(product.totalInventory || 0)
    const totalFrontStock =
      Number(product.totalFrontStock || 0)
    const totalStock =
      totalInventory + totalFrontStock

    const reason = window.prompt(
      `Lý do ngừng hoạt động sản phẩm “${product.productName}”:`,
      ''
    )

    if (reason === null) return

    const stockWarning =
      totalStock > 0
        ? (
            `\n\nSản phẩm hiện vẫn còn hàng: ` +
            `tồn kho ${totalInventory}, ` +
            `tồn quầy ${totalFrontStock}. ` +
            `Số lượng này vẫn được giữ để theo dõi và kiểm kê.`
          )
        : ''

    const confirmed = window.confirm(
      `Ngừng hoạt động sản phẩm “${product.productName}”? ` +
      `Sau khi ngừng, sản phẩm sẽ bị khóa nhập kho và xuất ra quầy.` +
      stockWarning
    )

    if (!confirmed) return

    setProductWorkingId(product.id)
    setProductError('')

    try {
      await deactivateProduct(product.id, reason.trim())
      setProducts((current) =>
        current.filter((item) => item.id !== product.id)
      )
    } catch (err) {
      setProductError(
        err.response?.data?.message ||
        'Không thể ngừng hoạt động sản phẩm.'
      )
    } finally {
      setProductWorkingId(null)
    }
  }

  async function handleRestoreProduct(product) {
    setProductWorkingId(product.id)
    setProductError('')

    try {
      await restoreProduct(product.id)
      setProducts((current) =>
        current.filter((item) => item.id !== product.id)
      )
    } catch (err) {
      setProductError(
        err.response?.data?.message ||
        'Không thể khôi phục sản phẩm.'
      )
    } finally {
      setProductWorkingId(null)
    }
  }

  return (
    <div className="supplier-page">
      <section className="supplier-hero">
        <div>
          <p className="supplier-eyebrow">Quản trị danh mục</p>
          <h2>Nhà cung cấp & sản phẩm</h2>
          <p className="supplier-hero-text">
            Quản lý nhà cung cấp và trạng thái kinh doanh của sản phẩm
            trong toàn hệ thống.
          </p>
        </div>

        <div className="supplier-summary">
          <div className="supplier-summary-item">
            <span>Nhà cung cấp</span>
            <strong>{loading ? '...' : suppliers.length}</strong>
            <small>
              {showDeleted ? 'đã ngừng' : 'đang hiển thị'}
            </small>
          </div>

          <div className="supplier-summary-item">
            <span>Sản phẩm</span>
            <strong>{productLoading ? '...' : products.length}</strong>
            <small>
              {showInactiveProducts ? 'đã ngừng' : 'đang hiển thị'}
            </small>
          </div>
        </div>
      </section>

      <section
        className={`supplier-master-grid ${
          showDeleted ? 'supplier-master-grid--list-only' : ''
        }`}
      >
        {!showDeleted && (
          <article className="supplier-panel supplier-form-panel">
            <div className="supplier-panel-header supplier-panel-header--stack">
              <div>
                <p className="supplier-eyebrow">
                  {isEditing ? 'Chỉnh sửa' : 'Khai báo'}
                </p>
                <h3>
                  {isEditing
                    ? 'Cập nhật nhà cung cấp'
                    : 'Thêm nhà cung cấp'}
                </h3>
              </div>

              {isEditing && (
                <span className="supplier-edit-badge">
                  Đang chỉnh sửa
                </span>
              )}
            </div>

            <form
              className="supplier-form"
              onSubmit={handleSubmit}
            >
              <div className="supplier-field">
                <label htmlFor="supplier-name">
                  Tên nhà cung cấp
                  <span aria-hidden="true"> *</span>
                </label>
                <input
                  id="supplier-name"
                  name="supplierName"
                  required
                  value={form.supplierName}
                  onChange={handleFormChange}
                  disabled={saving}
                  placeholder="Nhập tên nhà cung cấp"
                />
              </div>

              <div className="supplier-field">
                <label htmlFor="supplier-phone">
                  Số điện thoại
                </label>
                <input
                  id="supplier-phone"
                  name="phone"
                  value={form.phone}
                  onChange={handleFormChange}
                  disabled={saving}
                  placeholder="Nhập số điện thoại"
                />
              </div>

              <div className="supplier-field">
                <label htmlFor="supplier-address">
                  Địa chỉ
                </label>
                <input
                  id="supplier-address"
                  name="address"
                  value={form.address}
                  onChange={handleFormChange}
                  disabled={saving}
                  placeholder="Nhập địa chỉ"
                />
              </div>

              <div className="supplier-form-actions">
                <button
                  className="supplier-btn supplier-btn--primary"
                  type="submit"
                  disabled={saving}
                >
                  {saving
                    ? 'Đang lưu...'
                    : isEditing
                      ? 'Lưu thay đổi'
                      : 'Thêm nhà cung cấp'}
                </button>

                {isEditing && (
                  <button
                    className="supplier-btn supplier-btn--secondary"
                    type="button"
                    onClick={resetForm}
                    disabled={saving}
                  >
                    Hủy
                  </button>
                )}
              </div>
            </form>
          </article>
        )}

        <article className="supplier-panel supplier-list-panel">
          <div className="supplier-panel-header">
            <div>
              <p className="supplier-eyebrow">Nhà cung cấp</p>
              <h3>
                {showDeleted
                  ? 'Nhà cung cấp đã ngừng'
                  : 'Danh sách nhà cung cấp'}
              </h3>
            </div>

            <div
              className="supplier-tabs"
              role="group"
              aria-label="Lọc nhà cung cấp theo trạng thái"
            >
              <button
                className={`supplier-tab ${
                  !showDeleted ? 'is-active' : ''
                }`}
                type="button"
                onClick={() => handleChangeList(false)}
                disabled={loading}
              >
                Đang hoạt động
              </button>

              <button
                className={`supplier-tab ${
                  showDeleted ? 'is-active' : ''
                }`}
                type="button"
                onClick={() => handleChangeList(true)}
                disabled={loading}
              >
                Đã ngừng
              </button>
            </div>
          </div>

          {error && (
            <div className="supplier-alert supplier-alert--error">
              {error}
            </div>
          )}

          <div className="supplier-table-wrap">
            <table className="supplier-table">
              <thead>
                <tr>
                  <th>Tên nhà cung cấp</th>
                  <th>SĐT</th>
                  <th>Địa chỉ</th>
                  <th className="supplier-table-action-head">
                    Thao tác
                  </th>
                </tr>
              </thead>

              <tbody>
                {!loading &&
                  suppliers.map((supplier) => (
                    <tr key={supplier.id}>
                      <td data-label="Nhà cung cấp">
                        <strong>{supplier.supplierName}</strong>
                      </td>

                      <td data-label="SĐT">
                        {supplier.phone || '—'}
                      </td>

                      <td data-label="Địa chỉ">
                        {supplier.address || '—'}
                      </td>

                      <td
                        data-label="Thao tác"
                        className="supplier-table-actions"
                      >
                        {showDeleted ? (
                          <button
                            className="supplier-action supplier-action--restore"
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
                              : 'Khôi phục'}
                          </button>
                        ) : (
                          <div className="supplier-action-group">
                            <button
                              className="supplier-action supplier-action--edit"
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
                              className="supplier-action supplier-action--danger"
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
                                : 'Ngừng'}
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
                      className="supplier-empty"
                    >
                      {loading
                        ? 'Đang tải danh sách...'
                        : showDeleted
                          ? 'Không có nhà cung cấp đã ngừng hoạt động.'
                          : 'Chưa có nhà cung cấp.'}
                    </td>
                  </tr>
                )}
              </tbody>
            </table>
          </div>
        </article>
      </section>

      <section className="supplier-panel supplier-products-panel">
        <div className="supplier-panel-header supplier-products-header">
          <div>
            <p className="supplier-eyebrow">Sản phẩm</p>
            <h3>
              {showInactiveProducts
                ? 'Sản phẩm đã ngừng kinh doanh'
                : 'Danh sách sản phẩm'}
            </h3>
            <p className="supplier-section-note">
              Sản phẩm ngừng kinh doanh sẽ bị khóa nhập kho và
              xuất ra quầy nhưng vẫn giữ dữ liệu tồn và lịch sử.
            </p>
          </div>

          <div
            className="supplier-tabs"
            role="group"
            aria-label="Lọc sản phẩm theo trạng thái"
          >
            <button
              className={`supplier-tab ${
                !showInactiveProducts ? 'is-active' : ''
              }`}
              type="button"
              onClick={() => loadProducts(false)}
              disabled={productLoading}
            >
              Đang hoạt động
            </button>

            <button
              className={`supplier-tab ${
                showInactiveProducts ? 'is-active' : ''
              }`}
              type="button"
              onClick={() => loadProducts(true)}
              disabled={productLoading}
            >
              Đã ngừng
            </button>
          </div>
        </div>

        {productError && (
          <div className="supplier-alert supplier-alert--error">
            {productError}
          </div>
        )}

        <div className="supplier-table-wrap">
          <table className="supplier-table supplier-product-table">
            <thead>
              <tr>
                <th>Mã SP</th>
                <th>Tên sản phẩm</th>
                <th>Nhà cung cấp</th>
                <th className="supplier-number-head">
                  Tồn kho
                </th>
                <th className="supplier-number-head">
                  Tồn quầy
                </th>
                <th className="supplier-table-action-head">
                  Thao tác
                </th>
              </tr>
            </thead>

            <tbody>
              {!productLoading &&
                products.map((product) => {
                  const totalInventory =
                    Number(product.totalInventory || 0)
                  const totalFrontStock =
                    Number(product.totalFrontStock || 0)
                  const hasStock =
                    totalInventory + totalFrontStock > 0

                  return (
                    <tr key={product.id}>
                      <td data-label="Mã SP">
                        <span className="supplier-code">
                          {product.productCode || '—'}
                        </span>
                      </td>

                      <td data-label="Sản phẩm">
                        <strong>{product.productName}</strong>
                      </td>

                      <td data-label="Nhà cung cấp">
                        {product.supplierName || '—'}
                      </td>

                      <td
                        data-label="Tồn kho"
                        className="supplier-number-cell"
                      >
                        <span
                          className={`supplier-stock ${
                            totalInventory > 0
                              ? 'supplier-stock--has'
                              : ''
                          }`}
                        >
                          {totalInventory}
                        </span>
                      </td>

                      <td
                        data-label="Tồn quầy"
                        className="supplier-number-cell"
                      >
                        <span
                          className={`supplier-stock ${
                            totalFrontStock > 0
                              ? 'supplier-stock--has'
                              : ''
                          }`}
                        >
                          {totalFrontStock}
                        </span>
                      </td>

                      <td
                        data-label="Thao tác"
                        className="supplier-table-actions"
                      >
                        {showInactiveProducts ? (
                          <button
                            className="supplier-action supplier-action--restore"
                            type="button"
                            disabled={
                              productWorkingId ===
                              product.id
                            }
                            onClick={() =>
                              handleRestoreProduct(product)
                            }
                          >
                            {productWorkingId === product.id
                              ? 'Đang khôi phục...'
                              : 'Khôi phục'}
                          </button>
                        ) : (
                          <button
                            className="supplier-action supplier-action--danger"
                            type="button"
                            disabled={
                              productWorkingId ===
                              product.id
                            }
                            title={
                              hasStock
                                ? 'Sản phẩm còn tồn nhưng vẫn có thể ngừng ngay. Tồn hiện tại được giữ để theo dõi và kiểm kê.'
                                : 'Ngừng sản phẩm để chặn giao dịch nhập và xuất mới.'
                            }
                            onClick={() =>
                              handleDeactivateProduct(product)
                            }
                          >
                            {productWorkingId === product.id
                              ? 'Đang xử lý...'
                              : 'Ngừng kinh doanh'}
                          </button>
                        )}
                      </td>
                    </tr>
                  )
                })}

              {(productLoading || products.length === 0) && (
                <tr>
                  <td
                    colSpan="6"
                    className="supplier-empty"
                  >
                    {productLoading
                      ? 'Đang tải sản phẩm...'
                      : showInactiveProducts
                        ? 'Không có sản phẩm đã ngừng hoạt động.'
                        : 'Chưa có sản phẩm.'}
                  </td>
                </tr>
              )}
            </tbody>
          </table>
        </div>
      </section>
    </div>
  )
}