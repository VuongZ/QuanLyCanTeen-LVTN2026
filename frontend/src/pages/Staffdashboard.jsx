import { useState } from 'react';
import './css/dashboard.css';
import { InventoryTab } from './shared/InventoryTab';
import { UnifiedScheduleTab } from './staff/UnifiedScheduleTab';
import { ProfileTab } from './staff/ProfileTab';
import { SalaryTab } from './staff/SalaryTab';
import { ShiftClosingReportTab } from './shared/ShiftClosingReportTab';

function getInitials(name = '') {
  return name
    .split(' ')
    .filter(Boolean)
    .slice(-2)
    .map((p) => p[0])
    .join('')
    .toUpperCase();
}

export function StaffDashboard({ branches, onLogout, onUserUpdated, user }) {
  const [activeTab, setActiveTab] = useState('schedule');
  const [isMenuOpen, setIsMenuOpen] = useState(false);

  const branch = branches?.find((b) => b.id === user.branchId);

  const getHeaderInfo = () => {
    switch (activeTab) {
      case 'profile':
        return { eyebrow: 'Tài khoản', title: 'Hồ sơ của tôi' };
      case 'schedule':
        return { eyebrow: 'Công việc', title: 'Lịch & đăng ký ca' };
      case 'salary':
        return { eyebrow: 'Thu nhập', title: 'Giờ làm & lương' };
      case 'inventory':
        return { eyebrow: 'Kho hàng', title: 'Tra cứu tồn kho' };
      case 'shiftClosing':
        return { eyebrow: 'Báo cáo cuối ca', title: 'Báo cáo kết ca' };
      default:
        return { eyebrow: '', title: '' };
    }
  };

  const headerInfo = getHeaderInfo();

  const navItems = [
    { id: 'schedule', icon: '🗓️', label: 'Lịch & đăng ký' },
    { id: 'inventory', icon: '📦', label: 'Tra cứu tồn kho' },
    { id: 'salary', icon: '💰', label: 'Giờ làm & lương' },
    { id: 'shiftClosing', icon: '📋', label: 'Báo cáo kết ca' },
    { id: 'profile', icon: '👤', label: 'Tài khoản' },
  ];

  return (
    <div className="sd-root sd-root--left-nav">
      <header className="sd-topbar">
        <div className="sd-brand">
          <button
            className="sd-hamburger"
            onClick={() => setIsMenuOpen(true)}
            type="button"
          >
            ☰
          </button>
          <span className="sd-brand-icon">CT</span>
          <span className="sd-brand-name">Canteen</span>
        </div>

        <button className="sd-logout-btn" onClick={onLogout} type="button">
          <span>Đăng xuất</span> ↩
        </button>
      </header>

      <div className="sd-layout">
        {isMenuOpen && (
          <div
            className="sd-menu-overlay"
            onClick={() => setIsMenuOpen(false)}
          />
        )}

        <nav className={`sd-left-nav ${isMenuOpen ? 'open' : ''}`}>
          <div className="sd-left-nav-user">
            <div className="sd-info-avatar sd-avatar-sm">
              {getInitials(user.fullName || user.username)}
            </div>
            <span className="sd-left-nav-name">
              {user.fullName || user.username}
            </span>
          </div>

          <div className="sd-left-nav-items">
            {navItems.map((item) => (
              <button
                key={item.id}
                className={`sd-left-nav-item ${activeTab === item.id ? 'active' : ''}`}
                onClick={() => {
                  setActiveTab(item.id);
                  setIsMenuOpen(false);
                }}
                type="button"
              >
                <span className="sd-nav-icon">{item.icon}</span>
                <span className="sd-nav-label">{item.label}</span>
              </button>
            ))}
          </div>

          <button
            className="sd-left-nav-logout"
            onClick={onLogout}
            type="button"
          >
            ↩ Đăng xuất
          </button>
        </nav>

        <main className="sd-main">
          <div className="sd-page-header">
            <div>
              <p className="sd-eyebrow">{headerInfo.eyebrow}</p>
              <h1>{headerInfo.title}</h1>
            </div>

            <div className="sd-branch-badge">
              📍 {branch?.name || user.branchName || 'Chưa gán'}
            </div>
          </div>

          <div className="sd-content">
            {activeTab === 'schedule' && <UnifiedScheduleTab user={user} />}

            {activeTab === 'profile' && (
              <ProfileTab
                branch={branch}
                onUserUpdated={onUserUpdated}
                user={user}
              />
            )}

            {activeTab === 'inventory' && (
              <InventoryTab currentUser={user} branches={branches} />
            )}

            {activeTab === 'salary' && <SalaryTab user={user} />}

            {activeTab === 'shiftClosing' && <ShiftClosingReportTab />}
          </div>
        </main>
      </div>
    </div>
  );
}