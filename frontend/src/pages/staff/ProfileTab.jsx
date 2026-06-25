import { useState, useEffect } from 'react';
import QRCode from 'qrcode';

// --- HÀM TIỆN ÍCH ---
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

function buildEmployeeQrPayload(user) {
  return JSON.stringify({
    type: 'EMPLOYEE',
    id: user.id,
    username: user.username,
    fullName: user.fullName,
    roleName: user.roleName,
    branchId: user.branchId,
    branchName: user.branchName,
    hireDate: user.hireDate,
  });
}

// --- COMPONENT: THẺ QR NHÂN VIÊN ---
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
    link.download = `employee-${user.username || user.id}-qr.png`;
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
            <InfoRow label="Username" value={user.username || '---'} />
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

// --- COMPONENT CHÍNH: TAB HỒ SƠ ---
// Nhớ phải có chữ export ở đây nhé
export function ProfileTab({ branch, user }) {
  return (
    <div className="sd-profile-layout">
      <EmployeeQrCard user={user} />
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
          <InfoRow label="Chi nhánh" value={branch?.name || user.branchName || 'Chưa có'} />
          <InfoRow label="Ngày vào làm" value={formatDate(user.hireDate)} />
        </dl>
      </div>
    </div>
  );
}