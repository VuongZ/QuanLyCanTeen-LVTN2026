import { useEffect, useMemo, useState } from 'react';
import axios from 'axios';
import { formatVietnamDateTime } from '../../utils/vietnamDateTime';
import './checkout-request.css';

const STATUS = {
  AWAITING_EMPLOYEE: ['Cần xác nhận', 'awaiting'],
  PENDING: ['Chờ duyệt', 'pending'],
  APPROVED: ['Đã duyệt', 'approved'],
  REJECTED: ['Bị từ chối', 'rejected'],
};

function localInputValue(value) {
  if (!value) return '';
  const match = String(value).match(
    /^(\d{4}-\d{2}-\d{2})[T ](\d{2}:\d{2})/
  );
  return match ? `${match[1]}T${match[2]}` : '';
}

function RequestCard({ item, mode, onChanged }) {
  const [checkoutTime, setCheckoutTime] = useState(localInputValue(item.requestedCheckOutTime || item.proposedCheckOutTime));
  const [reason, setReason] = useState(item.reason || '');
  const [rejectReason, setRejectReason] = useState('');
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState('');
  const [label, className] = STATUS[item.status] || [item.status, 'neutral'];
  const editable = mode === 'mine' && ['AWAITING_EMPLOYEE', 'REJECTED'].includes(item.status);

  async function act(action, body) {
    setBusy(true); setError('');
    try {
      await axios.put(`/api/checkout-requests/${item.id}/${action}`, body);
      await onChanged();
    } catch (err) {
      setError(err.response?.data?.message || 'Không thể xử lý yêu cầu.');
    } finally { setBusy(false); }
  }

  return (
    <article className="co-card">
      <div className="co-card-head">
        <div>
          <strong>{mode === 'review' ? item.fullName : item.shiftName}</strong>
          <span>{item.workDate} · {item.shiftName} ({item.startTime}–{item.endTime})</span>
        </div>
        <span className={`co-status ${className}`}>{label}</span>
      </div>

      <div className="co-times">
        <div><span>Check-in</span><strong>{formatVietnamDateTime(item.checkInTime)}</strong></div>
        <div><span>Checkout tạm</span><strong>{formatVietnamDateTime(item.proposedCheckOutTime)}</strong></div>
        {item.requestedCheckOutTime && <div><span>Giờ đề nghị</span><strong>{formatVietnamDateTime(item.requestedCheckOutTime)}</strong></div>}
      </div>

      {mode === 'review' && <p className="co-meta">{item.roleName} · {item.branchName || 'Chưa rõ cơ sở'} · Lý do: {item.reason || 'Xác nhận giờ tạm'}</p>}
      {item.rejectReason && <p className="co-alert">Lý do từ chối: {item.rejectReason}</p>}

      {editable && (
        <div className="co-form">
          <label>Giờ checkout thực tế<input type="datetime-local" value={checkoutTime} onChange={(e) => setCheckoutTime(e.target.value)} /></label>
          <label>Lý do<textarea rows="2" value={reason} onChange={(e) => setReason(e.target.value)} placeholder="Bắt buộc nếu thay đổi giờ checkout tạm" /></label>
          <button disabled={busy || !checkoutTime} onClick={() => act('submit', { checkOutTime: checkoutTime, reason })} type="button">
            {busy ? 'Đang gửi...' : 'Xác nhận và gửi duyệt'}
          </button>
        </div>
      )}

      {mode === 'review' && (
        <div className="co-review-actions">
          <button className="approve" disabled={busy} onClick={() => act('approve')} type="button">Duyệt checkout</button>
          <input value={rejectReason} onChange={(e) => setRejectReason(e.target.value)} placeholder="Lý do từ chối" />
          <button className="reject" disabled={busy || !rejectReason.trim()} onClick={() => act('reject', { reason: rejectReason })} type="button">Từ chối</button>
        </div>
      )}
      {error && <p className="co-error">{error}</p>}
    </article>
  );
}

export function CheckoutRequestTab({ canReview = false }) {
  const [mode, setMode] = useState(canReview ? 'review' : 'mine');
  const [mine, setMine] = useState([]);
  const [review, setReview] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');

  async function load() {
    setLoading(true); setError('');
    try {
      const calls = [axios.get('/api/checkout-requests/mine')];
      if (canReview) calls.push(axios.get('/api/checkout-requests/review'));
      const [mineRes, reviewRes] = await Promise.all(calls);
      setMine(Array.isArray(mineRes.data) ? mineRes.data : []);
      if (reviewRes) setReview(Array.isArray(reviewRes.data) ? reviewRes.data : []);
    } catch (err) {
      setError(err.response?.data?.message || 'Không thể tải yêu cầu checkout. Hãy kiểm tra migration cơ sở dữ liệu.');
    } finally { setLoading(false); }
  }

  useEffect(() => {
    let cancelled = false;
    const calls = [axios.get('/api/checkout-requests/mine')];
    if (canReview) calls.push(axios.get('/api/checkout-requests/review'));

    Promise.all(calls)
      .then(([mineRes, reviewRes]) => {
        if (cancelled) return;
        setMine(Array.isArray(mineRes.data) ? mineRes.data : []);
        if (reviewRes) setReview(Array.isArray(reviewRes.data) ? reviewRes.data : []);
      })
      .catch((err) => {
        if (!cancelled) setError(err.response?.data?.message || 'Không thể tải yêu cầu checkout. Hãy kiểm tra migration cơ sở dữ liệu.');
      })
      .finally(() => { if (!cancelled) setLoading(false); });

    return () => { cancelled = true; };
  }, [canReview]);
  const items = useMemo(() => mode === 'review' ? review : mine, [mode, review, mine]);

  return (
    <section className="co-page">
      <div className="co-intro">
        <div><p className="sd-eyebrow">Chấm công</p><h2>Xử lý quên checkout</h2></div>
        <button className="co-refresh" onClick={load} disabled={loading} type="button">Làm mới</button>
      </div>
      <p className="co-description">Checkout tạm được tạo 30 phút sau khi ca kết thúc. Giờ làm chỉ được cộng vào lương sau khi yêu cầu được duyệt.</p>
      {canReview && (
        <div className="co-tabs">
          <button className={mode === 'review' ? 'active' : ''} onClick={() => setMode('review')} type="button">Chờ tôi duyệt ({review.length})</button>
          <button className={mode === 'mine' ? 'active' : ''} onClick={() => setMode('mine')} type="button">Yêu cầu của tôi ({mine.length})</button>
        </div>
      )}
      {error && <p className="co-error">{error}</p>}
      {loading ? <p className="co-empty">Đang tải...</p> : items.length === 0 ? <p className="co-empty">Không có yêu cầu nào.</p> : (
        <div className="co-list">{items.map((item) => <RequestCard key={item.id} item={item} mode={mode} onChanged={load} />)}</div>
      )}
    </section>
  );
}
