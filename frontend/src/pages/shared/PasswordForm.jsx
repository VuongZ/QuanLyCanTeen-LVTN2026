import { useState } from 'react';
import { changePassword, requestChangePasswordOtp } from '../../api/UserApi';

export function PasswordForm({ onUserUpdated, user }) {
  const [form, setForm] = useState({
    currentPassword: '',
    newPassword: '',
    confirmPassword: '',
    otp: '',
  })
  const [status, setStatus] = useState(null)
  const [isSaving, setIsSaving] = useState(false)
  const [isSendingOtp, setIsSendingOtp] = useState(false)

  function handleChange(e) {
    setForm((f) => ({ ...f, [e.target.name]: e.target.value }))
  }

  async function handleRequestOtp() {
    setStatus(null)
    try {
      setIsSendingOtp(true)
      const response = await requestChangePasswordOtp(user.id)
      setStatus({ type: 'success', msg: response.message || 'Đã gửi mã OTP về email.' })
    } catch (err) {
      setStatus({ type: 'error', msg: err.response?.data?.message || err.message || 'Không thể gửi OTP.' })
    } finally {
      setIsSendingOtp(false)
    }
  }

  async function handleSubmit(e) {
    e.preventDefault()
    setStatus(null)

    if (!form.currentPassword) {
      setStatus({ type: 'error', msg: 'Vui lòng nhập mật khẩu hiện tại.' })
      return
    }

    if (!form.otp.trim()) {
      setStatus({ type: 'error', msg: 'Vui lòng nhập mã OTP đã gửi về email.' })
      return
    }

    if (form.newPassword.length < 4) {
      setStatus({ type: 'error', msg: 'Mật khẩu mới cần tối thiểu 4 ký tự.' })
      return
    }

    if (form.newPassword !== form.confirmPassword) {
      setStatus({ type: 'error', msg: 'Nhập lại mật khẩu chưa khớp.' })
      return
    }

    try {
      setIsSaving(true)
      await changePassword(user.id, {
        currentPassword: form.currentPassword,
        newPassword: form.newPassword,
        otp: form.otp.trim(),
      })
      onUserUpdated(user)
      setForm({ currentPassword: '', newPassword: '', confirmPassword: '', otp: '' })
      setStatus({ type: 'success', msg: 'Đã cập nhật mật khẩu thành công.' })
    } catch (err) {
      setStatus({ type: 'error', msg: err.response?.data?.message || err.message || 'Lỗi cập nhật.' })
    } finally {
      setIsSaving(false)
    }
  }

  const fields = [
    { name: 'currentPassword', label: 'Mật khẩu hiện tại', autoComplete: 'current-password' },
    { name: 'newPassword', label: 'Mật khẩu mới', autoComplete: 'new-password' },
    { name: 'confirmPassword', label: 'Nhập lại mật khẩu', autoComplete: 'new-password' },
  ]

  return (
    <form className="sd-pw-form" onSubmit={handleSubmit}>
      {fields.map((field) => (
        <div key={field.name} className="sd-field">
          <label>{field.label}</label>
          <input
            autoComplete={field.autoComplete}
            name={field.name}
            onChange={handleChange}
            type="password"
            value={form[field.name]}
          />
        </div>
      ))}

      <button className="sd-btn-secondary" disabled={isSendingOtp || isSaving} onClick={handleRequestOtp} type="button">
        {isSendingOtp ? 'Đang gửi OTP...' : 'Gửi mã OTP về email'}
      </button>

      <div className="sd-field">
        <label>Mã OTP</label>
        <input
          autoComplete="one-time-code"
          inputMode="numeric"
          maxLength={6}
          name="otp"
          onChange={handleChange}
          type="text"
          value={form.otp}
        />
      </div>

      {status && <p className={`sd-status sd-status-${status.type}`}>{status.msg}</p>}
      <button className="sd-btn-primary" disabled={isSaving || isSendingOtp} type="submit">
        {isSaving ? 'Đang lưu...' : 'Cập nhật mật khẩu'}
      </button>
    </form>
  )
}
