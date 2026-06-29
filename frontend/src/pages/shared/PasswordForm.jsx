import { useState } from 'react';
import { changePassword } from '../../api/UserApi';

export function PasswordForm({ onUserUpdated, user }) {
  const [form, setForm] = useState({ currentPassword: '', newPassword: '', confirmPassword: '' })
  const [status, setStatus] = useState(null)
  const [isSaving, setIsSaving] = useState(false)

  function handleChange(e) { setForm((f) => ({ ...f, [e.target.name]: e.target.value })) }

  async function handleSubmit(e) {
    e.preventDefault(); setStatus(null)
    if (form.newPassword.length < 4) return setStatus({ type: 'error', msg: 'Mật khẩu mới cần tối thiểu 4 ký tự' })
    if (form.newPassword !== form.confirmPassword) return setStatus({ type: 'error', msg: 'Nhập lại mật khẩu chưa khớp' })
    try {
      setIsSaving(true)
      await changePassword(user.id, {
        currentPassword: form.currentPassword,
        newPassword: form.newPassword,
      })
      onUserUpdated(user)
      setForm({ currentPassword: '', newPassword: '', confirmPassword: '' })
      setStatus({ type: 'success', msg: 'Đã cập nhật mật khẩu thành công' })
    } catch (err) { setStatus({ type: 'error', msg: err.response?.data?.message || err.message || 'Lỗi cập nhật' }) } finally { setIsSaving(false) }
  }

  return (
    <form className="sd-pw-form" onSubmit={handleSubmit}>
      {['currentPassword', 'newPassword', 'confirmPassword'].map((field) => (
        <div key={field} className="sd-field">
          <label>{field === 'currentPassword' ? 'Mật khẩu hiện tại' : field === 'newPassword' ? 'Mật khẩu mới' : 'Nhập lại mật khẩu'}</label>
          <input type="password" name={field} value={form[field]} onChange={handleChange} />
        </div>
      ))}
      {status && <p className={`sd-status sd-status-${status.type}`}>{status.msg}</p>}
      <button className="sd-btn-primary" disabled={isSaving} type="submit">{isSaving ? 'Đang lưu…' : 'Cập nhật mật khẩu'}</button>
    </form>
  )
}
