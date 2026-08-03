import { useEffect, useState } from 'react';
import './css/dashboard.css';

import { InventoryTab } from './shared/InventoryTab';
import { UnifiedScheduleTab } from './staff/UnifiedScheduleTab';
import { ProfileTab } from './staff/ProfileTab';
import { SalaryTab } from './staff/SalaryTab';
import {
  StaffSocialInsuranceTab
} from './staff/StaffSocialInsuranceTab';
import { ShiftClosingReportTab } from './shared/ShiftClosingReportTab';
import { CheckoutRequestTab } from './shared/CheckoutRequestTab';
import { ShiftDelegationTab } from './shared/ShiftDelegationTab';

import {
  getShiftDelegations
} from '../api/ShiftDelegationApi';


function getInitials(name = '') {
  return name
    .split(' ')
    .filter(Boolean)
    .slice(-2)
    .map((part) => part[0])
    .join('')
    .toUpperCase();
}


export function StaffDashboard({
  branches,
  onLogout,
  onUserUpdated,
  user,
  users
}) {
  const [activeTab, setActiveTab] =
    useState('schedule');

  const [isMenuOpen, setIsMenuOpen] =
    useState(false);

  const [
    pendingDelegationCount,
    setPendingDelegationCount
  ] = useState(0);


  const branch =
    branches?.find(
      (item) =>
        item.id === user.branchId
    );


  useEffect(
    () => {
      let mounted = true;

      async function
        refreshDelegationNotifications() {
        try {
          const items =
            await getShiftDelegations();

          if (mounted) {
            const normalizedItems =
              Array.isArray(items)
                ? items
                : [];

            const pendingCount =
              normalizedItems.filter(
                (item) =>
                  item.delegateUserId ===
                    user.id &&
                  item.status === 'PENDING'
              ).length;

            setPendingDelegationCount(
              pendingCount
            );
          }
        } catch {
          if (mounted) {
            setPendingDelegationCount(0);
          }
        }
      }

      refreshDelegationNotifications();

      const timer =
        window.setInterval(
          refreshDelegationNotifications,
          60000
        );

      return () => {
        mounted = false;

        window.clearInterval(timer);
      };
    },
    [
      activeTab,
      user.id
    ]
  );


  const getHeaderInfo = () => {
    switch (activeTab) {
      case 'profile':
        return {
          eyebrow: 'Tài khoản',
          title: 'Hồ sơ của tôi'
        };

      case 'schedule':
        return {
          eyebrow: 'Công việc',
          title: 'Lịch & đăng ký ca'
        };

      case 'salary':
        return {
          eyebrow: 'Thu nhập',
          title: 'Giờ làm & lương'
        };

      case 'socialInsurance':
        return {
          eyebrow: 'Phúc lợi',
          title: 'Bảo hiểm xã hội'
        };

      case 'inventory':
        return {
          eyebrow: 'Kho hàng',
          title: 'Tra cứu tồn kho'
        };

      case 'shiftClosing':
        return {
          eyebrow: 'Báo cáo cuối ca',
          title: 'Báo cáo kết ca'
        };

      case 'forgotCheckout':
        return {
          eyebrow: 'Chấm công',
          title: 'Xử lý quên checkout'
        };

      case 'shiftDelegation':
        return {
          eyebrow: 'Phân quyền',
          title: 'Ủy quyền trưởng ca'
        };

      default:
        return {
          eyebrow: '',
          title: ''
        };
    }
  };


  const headerInfo =
    getHeaderInfo();


  const navItems = [
    {
      id: 'schedule',
      icon: '🗓️',
      label: 'Lịch & đăng ký'
    },
    {
      id: 'inventory',
      icon: '📦',
      label: 'Tra cứu tồn kho'
    },
    {
      id: 'salary',
      icon: '💰',
      label: 'Giờ làm & lương'
    },
    {
      id: 'socialInsurance',
      icon: '🛡️',
      label: 'Bảo hiểm xã hội'
    },
    {
      id: 'shiftClosing',
      icon: '📋',
      label: 'Báo cáo kết ca'
    },
    {
      id: 'forgotCheckout',
      icon: '⏱',
      label: 'Quên checkout'
    },
    {
      id: 'shiftDelegation',
      icon: '🛡',
      label:
        pendingDelegationCount > 0
          ? `Ủy quyền ca (${pendingDelegationCount})`
          : 'Ủy quyền ca'
    },
    {
      id: 'profile',
      icon: '👤',
      label: 'Tài khoản'
    }
  ];


  return (
    <div className="sd-root sd-root--left-nav">
      <header className="sd-topbar">
        <div className="sd-brand">
          <button
            className="sd-hamburger"
            onClick={
              () => setIsMenuOpen(true)
            }
            type="button"
          >
            ☰
          </button>

          <span className="sd-brand-icon">
            CT
          </span>

          <span className="sd-brand-name">
            Canteen
          </span>
        </div>

        <button
          className="sd-logout-btn"
          onClick={onLogout}
          type="button"
        >
          <span>Đăng xuất</span> ↩
        </button>
      </header>

      <div className="sd-layout">
        {isMenuOpen && (
          <div
            className="sd-menu-overlay"
            onClick={
              () => setIsMenuOpen(false)
            }
            role="presentation"
          />
        )}

        <nav
          className={
            `sd-left-nav ${
              isMenuOpen
                ? 'open'
                : ''
            }`
          }
        >
          <div className="sd-left-nav-user">
            <div className="sd-info-avatar sd-avatar-sm">
              {getInitials(
                user.fullName ||
                user.username
              )}
            </div>

            <span className="sd-left-nav-name">
              {user.fullName ||
                user.username}
            </span>
          </div>

          <div className="sd-left-nav-items">
            {navItems.map(
              (item) => (
                <button
                  key={item.id}
                  className={
                    `sd-left-nav-item ${
                      activeTab === item.id
                        ? 'active'
                        : ''
                    }`
                  }
                  onClick={() => {
                    setActiveTab(item.id);
                    setIsMenuOpen(false);
                  }}
                  type="button"
                >
                  <span className="sd-nav-icon">
                    {item.icon}
                  </span>

                  <span className="sd-nav-label">
                    {item.label}
                  </span>
                </button>
              )
            )}
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
              <p className="sd-eyebrow">
                {headerInfo.eyebrow}
              </p>

              <h1>
                {headerInfo.title}
              </h1>
            </div>

            <div className="sd-branch-badge">
              📍{' '}
              {branch?.name ||
                user.branchName ||
                'Chưa gán'}
            </div>
          </div>

          <div className="sd-content">
            {activeTab === 'schedule' && (
              <UnifiedScheduleTab
                user={user}
              />
            )}

            {activeTab === 'profile' && (
              <ProfileTab
                branch={branch}
                onUserUpdated={
                  onUserUpdated
                }
                user={user}
              />
            )}

            {activeTab === 'inventory' && (
              <InventoryTab
                currentUser={user}
                branches={branches}
              />
            )}

            {activeTab === 'salary' && (
              <SalaryTab
                user={user}
              />
            )}

            {activeTab ===
              'socialInsurance' && (
              <StaffSocialInsuranceTab />
            )}

            {activeTab ===
              'shiftClosing' && (
              <ShiftClosingReportTab />
            )}

            {activeTab ===
              'forgotCheckout' && (
              <CheckoutRequestTab />
            )}

            {activeTab ===
              'shiftDelegation' && (
              <ShiftDelegationTab
                branches={branches}
                user={user}
                users={users}
              />
            )}
          </div>
        </main>
      </div>
    </div>
  );
}