import { useEffect, useState } from 'react';
import QRCode from 'qrcode';
import { updateUserProfile } from '../../api/UserApi';
import { PasswordForm } from '../shared/PasswordForm';

function getInitials(name = '') {
  return name.split(' ').filter(Boolean).slice(-2).map((p) => p[0]).join('').toUpperCase();
}

function formatDate(value) {
  if (!value) return 'Chưa có';
  return new Intl.DateTimeFormat('vi-VN').format(new Date(value));
}

function InfoRow({ label, value }) {
  return <div className="sd-info-row"><dt>{label}</dt><dd>{value}</dd></div>;
}

function getPhone(user) {
  return user?.phoneNumber || user?.phone || '';
}

function buildEmployeeQrPayload(user) {
  return JSON.stringify({
    type: 'EMPLOYEE',
    id: user.id,
    identifier: user.email || user.phoneNumber || user.phone || user.username,
    fullName: user.fullName,
    roleName: user.roleName,
    branchId: user.branchId,
    branchName: user.branchName,
    hireDate: user.hireDate,
  });
}

export function EmployeeQrCard({ user }) {
  const [qrUrl, setQrUrl] = useState('');
  const qrPayload = buildEmployeeQrPayload(user);

  useEffect(() => {
    let isMounted = true;
    QRCode.toDataURL(qrPayload, {
      errorCorrectionLevel: 'M',
      margin: 2,
      width: 220,
      color: { dark: '#1e293b', light: '#ffffff' },
    })
      .then((url) => { if (isMounted) setQrUrl(url); })
      .catch(() => { if (isMounted) setQrUrl(''); });

    return () => { isMounted = false; };
  }, [qrPayload]);

  function downloadQr() {
    if (!qrUrl) return;
    const link = document.createElement('a');
    link.href = qrUrl;
    link.download = `employee-${user.email || user.phoneNumber || user.id}-qr.png`;
    link.click();
  }

  return (
    <div className="sd-card sd-employee-qr-card">
      <div className="sd-employee-qr-info">
        <div className="sd-info-avatar">{getInitials(user.fullName || user.username)}</div>
        <div>
          <p className="sd-eyebrow">Mã QR nhân viên</p>
          <h2>{user.fullName || user.username}</h2>
          <dl className="sd-employee-qr-list">
            <InfoRow label="Email/SĐT" value={user.email || user.phoneNumber || user.phone || '---'} />
            <InfoRow label="Chức vụ" value={user.roleName || '---'} />
            <InfoRow label="Chi nhánh" value={user.branchName || 'Chưa gán'} />
          </dl>
        </div>
      </div>

      <div className="sd-employee-qr-box">
        {qrUrl ? (
          <img alt="Mã QR nhân viên" src={qrUrl} />
        ) : (
          <div className="sd-employee-qr-placeholder">Đang tạo QR...</div>
        )}
        <button className="sd-btn-primary sd-employee-qr-download" disabled={!qrUrl} onClick={downloadQr} type="button">
          Tải QR
        </button>
      </div>
    </div>
  );
}

function ProfileInfoCard({ branch, user }) {
  return (
    <div className="sd-card">
      <div className="sd-card-header">
        <p className="sd-eyebrow">Chi tiết</p>
        <h2>Hồ sơ nhân viên</h2>
      </div>
      <div className="sd-info-hero">
        <div className="sd-info-avatar">{getInitials(user.fullName || user.username)}</div>
        <div>
          <h3>{user.fullName || user.username}</h3>
          <span className="sd-role-badge">{user.roleName || 'Nhân viên'}</span>
        </div>
      </div>
      <dl className="sd-dl">
        <InfoRow label="Họ và tên" value={user.fullName || '—'} />
        <InfoRow label="SĐT" value={getPhone(user) || 'Chưa có'} />
        <InfoRow label="Ngân hàng" value={user.bankName || 'Chưa có'} />
        <InfoRow label="Số tài khoản" value={user.bankAccountNumber || 'Chưa có'} />
        <InfoRow label="Tên tài khoản" value={user.bankAccountName || 'Chưa có'} />
        <InfoRow label="Chi nhánh" value={branch?.name || user.branchName || 'Chưa có'} />
        <InfoRow label="Loại nhân viên" value={user.employmentType === 'FULL_TIME' ? 'Full-time' : 'Part-time'} />
        <InfoRow label="Ngày vào làm" value={formatDate(user.hireDate)} />
      </dl>
    </div>
  );
}

export function ProfileTab({ branch, onUserUpdated, user }) {
  const [form, setForm] = useState({
    phoneNumber: getPhone(user),
    bankName: user.bankName || '',
    bankAccountNumber: user.bankAccountNumber || '',
    bankAccountName: user.bankAccountName || '',
  });
  const [status, setStatus] = useState(null);
  const [isSaving, setIsSaving] = useState(false);

  useEffect(() => {
    setForm({
      phoneNumber: getPhone(user),
      bankName: user.bankName || '',
      bankAccountNumber: user.bankAccountNumber || '',
      bankAccountName: user.bankAccountName || '',
    });
  }, [user]);

  function handleChange(e) {
    const { name, value } = e.target;
    setForm((current) => ({ ...current, [name]: value }));
  }

  async function handleSubmit(e) {
    e.preventDefault();
    setStatus(null);
    setIsSaving(true);

    const payload = {
      phoneNumber: form.phoneNumber.trim(),
      bankName: form.bankName.trim(),
      bankAccountNumber: form.bankAccountNumber.trim(),
      bankAccountName: form.bankAccountName.trim(),
    };

    try {
      const savedUser = await updateUserProfile(user.id, payload);
      onUserUpdated({ ...user, ...payload, ...savedUser, phone: payload.phoneNumber });
      setStatus({ type: 'success', msg: 'Đã cập nhật thông tin tài khoản' });
    } catch (err) {
      setStatus({ type: 'error', msg: err.response?.data?.message || err.message || 'Không thể cập nhật thông tin' });
    } finally {
      setIsSaving(false);
    }
  }

  return (
    <div className="sd-profile-layout sd-profile-layout--account">
      <div className="sd-account-column">
        <EmployeeQrCard user={user} />

        <div className="sd-card">
          <div className="sd-card-header">
            <p className="sd-eyebrow">Cập nhật</p>
            <h2>Liên hệ & ngân hàng</h2>
          </div>
          <form className="sd-pw-form" onSubmit={handleSubmit}>
            <div className="sd-field">
              <label>SĐT</label>
              <input name="phoneNumber" value={form.phoneNumber} onChange={handleChange} />
            </div>
            <div className="sd-field">
              <label>Ngân hàng</label>
              <input name="bankName" value={form.bankName} onChange={handleChange} />
            </div>
            <div className="sd-field">
              <label>Số tài khoản</label>
              <input name="bankAccountNumber" value={form.bankAccountNumber} onChange={handleChange} />
            </div>
            <div className="sd-field">
              <label>Tên tài khoản</label>
              <input name="bankAccountName" value={form.bankAccountName} onChange={handleChange} />
            </div>
            {status && <p className={`sd-status sd-status-${status.type}`}>{status.msg}</p>}
            <button className="sd-btn-primary" disabled={isSaving} type="submit">
              {isSaving ? 'Đang lưu...' : 'Lưu thông tin'}
            </button>
          </form>
        </div>
      </div>

      <div className="sd-account-column">
        <ProfileInfoCard branch={branch} user={user} />

        <div className="sd-card">
          <div className="sd-card-header">
            <p className="sd-eyebrow">Bảo mật</p>
            <h2>Đổi mật khẩu</h2>
          </div>
          <PasswordForm onUserUpdated={onUserUpdated} user={user} />
        </div>
      </div>
    </div>
  );
}
