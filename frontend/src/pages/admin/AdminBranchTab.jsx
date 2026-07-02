import { useState, useEffect } from 'react'
import axios from 'axios'
import { getAllBranches, createBranch, updateBranch, deleteBranch } from '../../api/BranchApi'
import { getAllShifts, createShift, updateShift, deleteShift } from '../../api/ShiftApi'

const EN_DAYS_ORDER = ['Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday', 'Saturday', 'Sunday']
const VN_DAYS = ['Thứ 2', 'Thứ 3', 'Thứ 4', 'Thứ 5', 'Thứ 6', 'Thứ 7', 'Chủ nhật']

export function AdminBranchTab({ branches, setBranches }) {
  const [selectedBranch, setSelectedBranch] = useState(null)
  const [branchModal, setBranchModal] = useState(null)
  const [branchForm, setBranchForm] = useState({ name: '', address: '', latitude: '', longitude: '' })
  const [search, setSearch] = useState('')
  const [shifts, setShifts] = useState([])

  //  STATE MỚI CHO TÍNH NĂNG CẤU HÌNH NGÀY
  const [shiftConfigs, setShiftConfigs] = useState([])
  const [configModal, setConfigModal] = useState(null)
  const [configForm, setConfigForm] = useState([])

  const [shiftModal, setShiftModal] = useState(null)
  const [shiftForm, setShiftForm] = useState({ shiftName: '', startTime: '', endTime: '', maxStaff: 0, isOt: false })
  const [modalShift, setModalShift] = useState(null)
  const [saving, setSaving] = useState(false)
  const [error, setError] = useState('')

  useEffect(() => {
    getAllShifts().then((data) => setShifts(Array.isArray(data) ? data : [])).catch(() => { })
    axios.get('/api/BranchShiftConfig').then(res => setShiftConfigs(res.data || [])).catch(() => { })
  }, [])

  const displayedBranches = branches.filter((b) =>
    (b.name?.toLowerCase() || '').includes(search.toLowerCase()) ||
    (b.address?.toLowerCase() || '').includes(search.toLowerCase())
  )

  function openAddBranch() {
    setBranchForm({ name: '', address: '', latitude: '', longitude: '' })
    setError(''); setBranchModal('add')
  }

  function openEditBranch(b) {
    setBranchForm({ ...b })
    setError(''); setBranchModal('edit')
  }

  function openDeleteBranch() {
    setError(''); setBranchModal('delete')
  }

  async function handleSaveBranch() {
    if (!branchForm.name || !branchForm.address) return setError('Vui lòng nhập tên và địa chỉ cơ sở')
    setSaving(true); setError('')
    try {
      const payload = {
        ...branchForm,
        latitude: branchForm.latitude === '' ? null : parseFloat(branchForm.latitude),
        longitude: branchForm.longitude === '' ? null : parseFloat(branchForm.longitude),
      }
      if (branchModal === 'add') {
        await createBranch(payload)
      } else {
        await updateBranch(branchForm.id, payload)
        if (selectedBranch) setSelectedBranch({ ...selectedBranch, ...payload })
      }
      const newData = await getAllBranches()
      setBranches(Array.isArray(newData) ? newData : [])
      setBranchModal(null)
    } catch { setError('Lỗi lưu cơ sở!') } finally { setSaving(false) }
  }

  async function handleDeleteBranch() {
    setSaving(true); setError('')
    try {
      await deleteBranch(selectedBranch.id)
      setBranches((prev) => prev.filter((b) => b.id !== selectedBranch.id))
      setSelectedBranch(null); setBranchModal(null)
    } catch { setError('Lỗi xóa cơ sở!') } finally { setSaving(false) }
  }

  const displayedShifts = selectedBranch ? shifts.filter((s) => s.branchId === selectedBranch.id) : []

  function openAddShift() {
    setShiftForm({ shiftName: '', startTime: '', endTime: '', maxStaff: 0, isOt: false })
    setError(''); setShiftModal('add')
  }

  function openEditShift(s) {
    setShiftForm({ ...s })
    setError(''); setModalShift(s); setShiftModal('edit')
  }

  function openDeleteShift(s) {
    setModalShift(s)
    setError(''); setShiftModal('delete')
  }

  async function handleSaveShift() {
    if (!shiftForm.shiftName || !shiftForm.startTime || !shiftForm.endTime) return setError('Vui lòng nhập Tên ca và Giờ')
    setSaving(true); setError('')
    try {
      const formatTime = (time) => (time.length === 5 ? `${time}:00` : time)
      const payloadShift = {
        ...shiftForm,
        startTime: formatTime(shiftForm.startTime),
        endTime: formatTime(shiftForm.endTime),
        maxStaff: shiftForm.maxStaff === '' ? 0 : parseInt(shiftForm.maxStaff, 10),
      }
      if (shiftModal === 'add') {
        await createShift({ ...payloadShift, branchId: selectedBranch.id })
      } else {
        await updateShift(shiftForm.id, payloadShift)
      }
      const newData = await getAllShifts()
      setShifts(Array.isArray(newData) ? newData : [])

      const configData = await axios.get('/api/BranchShiftConfig')
      setShiftConfigs(configData.data || [])
      setShiftModal(null)
    } catch { setError('Dữ liệu không hợp lệ!') } finally { setSaving(false) }
  }

  async function handleDeleteShift() {
    setSaving(true); setError('')
    try {
      await deleteShift(modalShift.id)
      setShifts((prev) => prev.filter((s) => s.id !== modalShift.id))
      setShiftModal(null)
    } catch { setError('Lỗi khi xóa ca.') } finally { setSaving(false) }
  }

  function openConfigShift(shift) {
    setError('')
    const formState = EN_DAYS_ORDER.map(dayEn => {
      const existing = shiftConfigs.find(c =>
        c.shiftId === shift.id && String(c.dayOfWeek).toLowerCase() === dayEn.toLowerCase()
      )
      return {
        id: existing?.id,
        dayOfWeek: dayEn,
        maxStaff: existing ? existing.maxStaff : (shift.maxStaff || 0)
      }
    })
    setConfigForm(formState)
    setConfigModal(shift)
  }

  async function handleSaveConfig() {
    setSaving(true)
    setError('')
    try {
      const apiCalls = configForm.map(cfg => {
        const safeMaxStaff = (cfg.maxStaff === '' || isNaN(cfg.maxStaff)) ? 0 : parseInt(cfg.maxStaff, 10)
        const payload = {
          shiftId: configModal.id,
          dayOfWeek: cfg.dayOfWeek,
          maxStaff: safeMaxStaff
        }
        if (cfg.id) {
          return axios.put(`/api/BranchShiftConfig/${cfg.id}`, payload)
        } else {
          return axios.post(`/api/BranchShiftConfig`, payload)
        }
      })
      await Promise.all(apiCalls)

      const res = await axios.get('/api/BranchShiftConfig')
      setShiftConfigs(res.data || [])
      setConfigModal(null)
      alert("✅ Đã cập nhật cấu hình nhân sự cho từng ngày thành công!")
    } catch (e) {
      console.error(e)
      setError(e.response?.data?.message || 'Dữ liệu không hợp lệ. Vui lòng kiểm tra lại!')
    } finally {
      setSaving(false)
    }
  }

  return (
    <div className="sd-users-page">
      {!selectedBranch ? (
        <>
          <div className="sd-users-toolbar">
            <div className="sd-users-toolbar-left">
              <div className="sd-search-wrap">
                <span className="sd-search-icon">⌕</span>
                <input
                  className="sd-input-search"
                  placeholder="Tìm tên cơ sở, địa chỉ..."
                  value={search}
                  onChange={(e) => setSearch(e.target.value)}
                />
                {search && (
                  <button className="sd-search-clear" onClick={() => setSearch('')}>✕</button>
                )}
              </div>
            </div>
            <div className="sd-users-toolbar-right">
              <span className="sd-result-count">{displayedBranches.length} cơ sở</span>
              <button className="sd-btn-add" onClick={openAddBranch}>
                <span>＋</span> Thêm cơ sở
              </button>
            </div>
          </div>
          <div className="sd-table-wrap">
            <table className="sd-table">
              <thead>
                <tr>
                  <th className="sd-th sd-text-center sd-hide-mobile" style={{ width: 60 }}>ID</th>
                  <th className="sd-th sd-td-name-col">Tên Cơ Sở</th>
                  <th className="sd-th sd-td-info-col sd-hide-mobile">Địa Chỉ</th>
                </tr>
              </thead>
              <tbody>
                {displayedBranches.map((b) => (
                  <tr key={b.id} className="sd-tr" onClick={() => setSelectedBranch(b)} style={{ cursor: 'pointer' }}>
                    <td className="sd-td sd-text-center sd-text-bold sd-text-muted sd-hide-mobile" style={{ width: 60 }}>#{b.id}</td>
                    <td className="sd-td sd-td-name-col"><span className="sd-td-name sd-text-primary">{b.name}</span></td>
                    <td className="sd-td sd-td-info-col sd-hide-mobile"><span className="sd-text-sm sd-text-muted">{b.address}</span></td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </>
      ) : (
        <div>
          <button className="sd-btn-back" onClick={() => setSelectedBranch(null)}>← Quay lại danh sách cơ sở</button>
          <div className="sd-card">
            <div className="sd-card-header append-flex">
              <div><p className="sd-eyebrow">Chi tiết</p><h2>{selectedBranch.name}</h2></div>
              <div className="sd-flex-start">
                <button className="sd-action-btn sd-action-edit" onClick={() => openEditBranch(selectedBranch)}>✎</button>
                <button className="sd-action-btn sd-action-delete" onClick={openDeleteBranch}>✕</button>
              </div>
            </div>
            <div className="sd-flex-column sd-text-muted" style={{ fontSize: 14 }}>
              <p style={{ margin: 0 }}>📍 <strong className="sd-text-bold">Địa chỉ:</strong> {selectedBranch.address}</p>
              <p style={{ margin: 0 }}>🗺️ <strong className="sd-text-bold">Tọa độ GPS:</strong> {selectedBranch.latitude || '—'},{' '}{selectedBranch.longitude || '—'}</p>
            </div>
          </div>
          <div className="sd-card">
            <div className="sd-card-header sd-flex-between" style={{ marginBottom: 20 }}>
              <div><p className="sd-eyebrow">Cấu hình</p><h2>Ca làm việc</h2></div>
              <button className="sd-btn-add" onClick={openAddShift}><span>＋</span> Thêm ca</button>
            </div>
            <div className="sd-table-wrap sd-box-bordered">
              <table className="sd-table">
                <thead style={{ background: '#f8fafc' }}>
                  <tr>
                    <th className="sd-th sd-td-name-col">Tên Ca</th>
                    <th className="sd-th">Thời Gian</th>
                    <th className="sd-th sd-text-center sd-hide-mobile">Tăng Ca (OT)</th>
                    <th className="sd-th sd-text-center sd-hide-mobile">Cấu hình từng ngày</th>
                    <th className="sd-th sd-th-actions">Thao tác</th>
                  </tr>
                </thead>
                <tbody>
                  {displayedShifts.length === 0 && (
                    <tr><td colSpan={5} className="sd-td-empty sd-td-empty-sm"><div className="sd-empty-state"><span className="sd-empty-icon">⏱️</span><p>Chưa có ca làm nào</p></div></td></tr>
                  )}
                  {displayedShifts.map((s) => (
                    <tr key={s.id} className="sd-tr">
                      <td className="sd-td sd-td-name-col"><strong style={{ color: '#1e293b' }}>{s.shiftName}</strong></td>
                      <td className="sd-td sd-td-info-col"><span className="sd-badge-time">{s.startTime?.slice(0, 5)} - {s.endTime?.slice(0, 5)}</span></td>
                      <td className="sd-td sd-text-center sd-hide-mobile">
                        <span className={`sd-role-pill ${s.isOt ? 'sd-badge-success' : 'sd-badge-neutral'}`}>{s.isOt ? 'Có' : 'Không'}</span>
                      </td>
                      <td className="sd-td sd-text-center sd-hide-mobile">
                        <button className="sd-btn-ghost" style={{ fontSize: 12, padding: '4px 10px', background: '#f8fafc', border: '1px solid #e2e8f0' }} onClick={() => openConfigShift(s)}>
                          ⚙️ Cấu hình tuần
                        </button>
                      </td>
                      <td className="sd-td sd-td-actions">
                        <button className="sd-action-btn sd-show-mobile-only" style={{ color: '#475569' }} onClick={() => openConfigShift(s)}>⚙️</button>
                        <button className="sd-action-btn sd-action-edit" onClick={() => openEditShift(s)}>✎</button>
                        <button className="sd-action-btn sd-action-delete" onClick={() => openDeleteShift(s)}>✕</button>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          </div>
        </div>
      )}

      {configModal && (
        <div className="sd-overlay" onClick={() => setConfigModal(null)}>
          <div className="sd-modal" onClick={e => e.stopPropagation()}>
            <div className="sd-modal-header">
              <h2>Cấu hình số lượng: {configModal.shiftName}</h2>
              <button onClick={() => setConfigModal(null)}>✕</button>
            </div>
            <div className="sd-modal-body">
              <p style={{ fontSize: 13, color: '#64748b', marginBottom: 16 }}>Thiết lập số lượng nhân viên cần thiết cho từng ngày trong tuần. <strong>Nhập 0 nếu ca đó nghỉ.</strong></p>
              <div className="sd-modal-grid">
                {configForm.map((cfg, index) => {
                  const isOff = cfg.maxStaff == 0 || cfg.maxStaff === '';
                  return (
                    <div className="sd-field" key={cfg.dayOfWeek}>
                      <label style={{ color: isOff ? '#ef4444' : '#1e293b' }}>{VN_DAYS[index]} {isOff ? '(Không Có Ca Làm)' : ''}</label>
                      <input type="number" min="0" value={cfg.maxStaff} style={{ borderColor: isOff ? '#fecaca' : '', background: isOff ? '#fef2f2' : '' }} onChange={e => { const val = e.target.value; setConfigForm(prev => prev.map((item, i) => i === index ? { ...item, maxStaff: val } : item)) }} />
                    </div>
                  )
                })}
              </div>
              {error && <p className="sd-status sd-status-error">{error}</p>}
            </div>
            <div className="sd-modal-footer">
              <button className="sd-btn-ghost" onClick={() => setConfigModal(null)}>Huỷ</button>
              <button className="sd-btn-primary" disabled={saving} onClick={handleSaveConfig}>{saving ? 'Đang lưu...' : 'Lưu cấu hình'}</button>
            </div>
          </div>
        </div>
      )}

      {(branchModal === 'add' || branchModal === 'edit') && (
        <div className="sd-overlay" onClick={() => setBranchModal(null)}>
          <div className="sd-modal" onClick={(e) => e.stopPropagation()}>
            <div className="sd-modal-header"><h2>{branchModal === 'add' ? 'Thêm cơ sở' : 'Sửa cơ sở'}</h2><button onClick={() => setBranchModal(null)}>✕</button></div>
            <div className="sd-modal-body">
              <div className="sd-field"><label>Tên cơ sở *</label><input value={branchForm.name} onChange={(e) => setBranchForm({ ...branchForm, name: e.target.value })} /></div>
              <div className="sd-field"><label>Địa chỉ *</label><input value={branchForm.address} onChange={(e) => setBranchForm({ ...branchForm, address: e.target.value })} /></div>
              <div className="sd-modal-grid">
                <div className="sd-field"><label>Vĩ độ (Lat)</label><input type="number" value={branchForm.latitude} onChange={(e) => setBranchForm({ ...branchForm, latitude: e.target.value })} /></div>
                <div className="sd-field"><label>Kinh độ (Lng)</label><input type="number" value={branchForm.longitude} onChange={(e) => setBranchForm({ ...branchForm, longitude: e.target.value })} /></div>
              </div>
              {error && <p className="sd-status sd-status-error">{error}</p>}
            </div>
            <div className="sd-modal-footer">
              <button className="sd-btn-ghost" onClick={() => setBranchModal(null)}>Huỷ</button>
              <button className="sd-btn-primary" disabled={saving} onClick={handleSaveBranch}>{saving ? 'Đang lưu...' : 'Lưu lại'}</button>
            </div>
          </div>
        </div>
      )}

      {branchModal === 'delete' && (
        <div className="sd-overlay" onClick={() => setBranchModal(null)}>
          <div className="sd-modal" onClick={(e) => e.stopPropagation()}>
            <div className="sd-modal-header"><h2>Xác nhận xoá</h2><button onClick={() => setBranchModal(null)}>✕</button></div>
            <div className="sd-modal-body"><p>Xoá cơ sở <strong className="sd-text-primary">{selectedBranch?.name}</strong>?</p>{error && <p className="sd-status sd-status-error">{error}</p>}</div>
            <div className="sd-modal-footer">
              <button className="sd-btn-ghost" onClick={() => setBranchModal(null)}>Huỷ</button>
              <button className="sd-btn-primary btn-danger" disabled={saving} onClick={handleDeleteBranch}>Xoá ngay</button>
            </div>
          </div>
        </div>
      )}

      {(shiftModal === 'add' || shiftModal === 'edit') && (
        <div className="sd-overlay" onClick={() => setShiftModal(null)}>
          <div className="sd-modal" onClick={(e) => e.stopPropagation()}>
            <div className="sd-modal-header"><h2>{shiftModal === 'add' ? 'Thêm ca làm' : 'Sửa ca làm'}</h2><button onClick={() => setShiftModal(null)}>✕</button></div>
            <div className="sd-modal-body">
              <div className="sd-field"><label>Tên ca (VD: Ca Sáng) *</label><input value={shiftForm.shiftName} onChange={(e) => setShiftForm({ ...shiftForm, shiftName: e.target.value })} /></div>
              <div className="sd-modal-grid">
                <div className="sd-field"><label>Giờ bắt đầu *</label><input type="time" value={shiftForm.startTime?.slice(0, 5)} onChange={(e) => setShiftForm({ ...shiftForm, startTime: e.target.value })} /></div>
                <div className="sd-field"><label>Giờ kết thúc *</label><input type="time" value={shiftForm.endTime?.slice(0, 5)} onChange={(e) => setShiftForm({ ...shiftForm, endTime: e.target.value })} /></div>
              </div>
              <div className="sd-modal-grid">
                <div className="sd-field"><label>Số NV tối đa mặc định</label><input type="number" value={shiftForm.maxStaff} onChange={(e) => setShiftForm({ ...shiftForm, maxStaff: e.target.value })} /></div>
                <div className="sd-field sd-flex-center">
                  <label style={{ display: 'flex', alignItems: 'center', gap: 8, cursor: 'pointer', marginTop: 24 }}>
                    <input type="checkbox" checked={shiftForm.isOt} onChange={(e) => setShiftForm({ ...shiftForm, isOt: e.target.checked })} style={{ width: 18, height: 18 }} />
                    Ca tính Tăng ca (OT)?
                  </label>
                </div>
              </div>
              {error && <p className="sd-status sd-status-error">{error}</p>}
            </div>
            <div className="sd-modal-footer">
              <button className="sd-btn-ghost" onClick={() => setShiftModal(null)}>Huỷ</button>
              <button className="sd-btn-primary" disabled={saving} onClick={handleSaveShift}>{saving ? 'Đang lưu...' : 'Lưu lại'}</button>
            </div>
          </div>
        </div>
      )}

      {shiftModal === 'delete' && (
        <div className="sd-overlay" onClick={() => setShiftModal(null)}>
          <div className="sd-modal" onClick={(e) => e.stopPropagation()}>
            <div className="sd-modal-header"><h2>Xác nhận xoá</h2><button onClick={() => setShiftModal(null)}>✕</button></div>
            <div className="sd-modal-body"><p>Xoá ca <strong className="sd-text-primary">{modalShift?.shiftName}</strong>?</p>{error && <p className="sd-status sd-status-error">{error}</p>}</div>
            <div className="sd-modal-footer">
              <button className="sd-btn-ghost" onClick={() => setShiftModal(null)}>Huỷ</button>
              <button className="sd-btn-primary btn-danger" disabled={saving} onClick={handleDeleteShift}>Xoá ngay</button>
            </div>
          </div>
        </div>
      )}
    </div>
  )
}
