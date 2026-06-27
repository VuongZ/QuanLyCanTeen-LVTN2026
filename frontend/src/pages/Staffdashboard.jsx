import { useState, useEffect } from 'react';
import './css/dashboard.css';
import { InventoryTab } from './shared/InventoryTab';


// 👉 IMPORT CÁC COMPONENT ĐÃ ĐƯỢC TÁCH RA FILE RIÊNG
import { UnifiedScheduleTab } from './staff/UnifiedScheduleTab';
import { ProfileTab } from './staff/ProfileTab';
import { PasswordForm } from './shared/PasswordForm';

// Hàm tiện ích tạo Avatar
function getInitials(name = '') {
  return name.split(' ').filter(Boolean).slice(-2).map((p) => p[0]).join('').toUpperCase();
}



export function StaffDashboard({ branches, onLogout, onUserUpdated, user }) {
  const [activeTab, setActiveTab] = useState('schedule');
  const [isMenuOpen, setIsMenuOpen] = useState(false); 

  const branch = branches?.find((b) => b.id === user.branchId);

  const getHeaderInfo = () => {
    switch (activeTab) {
      case 'profile': return { eyebrow: 'Tài khoản', title: 'Hồ sơ của tôi' };
      case 'schedule': return { eyebrow: 'Công việc', title: 'Lịch & Đăng ký ca' };
      case 'security': return { eyebrow: 'Cài đặt', title: 'Bảo mật tài khoản' };
      default: return { eyebrow: '', title: '' };
    }
  };
  const headerInfo = getHeaderInfo();

 const NAV_ITEMS = [
  { id: 'schedule', icon: '🗓️', label: 'Lịch & Đăng ký' },
  { id: 'inventory', icon: '📦', label: 'Tra cứu tồn kho' }, // 👈 Thêm mục Tồn kho cho Staff
  { id: 'profile', icon: '◎', label: 'Tài khoản' },
  { id: 'security', icon: '🔒', label: 'Bảo mật' },
];

  return (
    <div className="sd-root sd-root--left-nav">
      {/* --- TOPBAR --- */}
      <header className="sd-topbar">
        <div className="sd-brand">
          <button className="sd-hamburger" onClick={() => setIsMenuOpen(true)}>☰</button>
          <span className="sd-brand-icon">CT</span>
          <span className="sd-brand-name">Canteen</span>
        </div>
        <button className="sd-logout-btn" onClick={onLogout}>
          <span>Đăng xuất</span> ↩
        </button>
      </header>

      <div className="sd-layout">
        {isMenuOpen && <div className="sd-menu-overlay" onClick={() => setIsMenuOpen(false)}></div>}

        {/* --- MENU TRÁI (SIDEBAR) --- */}
        <nav className={`sd-left-nav ${isMenuOpen ? 'open' : ''}`}>
          <div className="sd-left-nav-user">
            <div className="sd-info-avatar sd-avatar-sm">{getInitials(user.fullName || user.username)}</div>
            <span className="sd-left-nav-name">{user.fullName || user.username}</span>
          </div>

          <div className="sd-left-nav-items">
            {NAV_ITEMS.map((item) => (
              <button
                key={item.id}
                className={`sd-left-nav-item ${activeTab === item.id ? 'active' : ''}`}
                onClick={() => { setActiveTab(item.id); setIsMenuOpen(false); }}
                type="button"
              >
                <span className="sd-nav-icon">{item.icon}</span>
                <span className="sd-nav-label">{item.label}</span>
              </button>
            ))}
          </div>
          <button className="sd-left-nav-logout" onClick={onLogout}>↩ Đăng xuất</button>
        </nav>

        {/* --- KHU VỰC NỘI DUNG CHÍNH --- */}
        <main className="sd-main">
          <div className="sd-page-header">
            <div>
              <p className="sd-eyebrow">{headerInfo.eyebrow}</p>
              <h1>{headerInfo.title}</h1>
            </div>
            <div className="sd-branch-badge">📍 {branch?.name || user.branchName || 'Chưa gán'}</div>
          </div>

          <div className="sd-content">
            {/* 👉 GỌI CÁC COMPONENT ĐÃ TÁCH Ở ĐÂY TÙY THEO TAB ĐANG CHỌN */}
            {activeTab === 'schedule' && <UnifiedScheduleTab user={user} />}
            {activeTab === 'profile' && <ProfileTab branch={branch} user={user} />}
            {activeTab === 'inventory' && <InventoryTab currentUser={user} branches={branches} />}
            
            {activeTab === 'security' && (
              <div className="sd-profile-layout">
                <div className="sd-card">
                  <div className="sd-card-header">
                    <p className="sd-eyebrow">Bảo mật</p>
                    <h2>Đổi mật khẩu</h2>
                  </div>
                  {/* Gọi PasswordForm tái sử dụng */}
                  <PasswordForm onUserUpdated={onUserUpdated} user={user} />
                </div>
              </div>
            )}
          </div>
        </main>
      </div>
    </div>
  );
}