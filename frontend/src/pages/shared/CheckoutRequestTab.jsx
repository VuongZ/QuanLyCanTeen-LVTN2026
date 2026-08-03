import {
  useEffect,
  useMemo,
  useState
} from 'react';

import axios from 'axios';

import {
  formatVietnamDateTime
} from '../../utils/vietnamDateTime';

import './checkout-request.css';


const STATUS = {
  AWAITING_EMPLOYEE: [
    'Cần xác nhận',
    'awaiting'
  ],

  PENDING: [
    'Chờ duyệt',
    'pending'
  ],

  APPROVED: [
    'Đã duyệt',
    'approved'
  ],

  REJECTED: [
    'Bị từ chối',
    'rejected'
  ]
};


// Chuyển DateTime Backend thành:
// YYYY-MM-DDTHH:mm
function localInputValue(value) {
  if (!value) {
    return '';
  }

  const match =
    String(value).match(
      /^(\d{4}-\d{2}-\d{2})[T ](\d{2}:\d{2})/
    );

  return match
    ? `${match[1]}T${match[2]}`
    : '';
}


// Tách DateTime thành ngày và giờ riêng.
function splitLocalDateTime(value) {
  const normalizedValue =
    localInputValue(value);

  if (!normalizedValue) {
    return {
      date: '',
      time: ''
    };
  }

  const [
    date,
    time
  ] = normalizedValue.split('T');

  return {
    date: date || '',
    time: time || ''
  };
}


// Ghép ngày và giờ trước khi gửi Backend.
function buildLocalDateTime(
  date,
  time
) {
  if (!date || !time) {
    return '';
  }

  return `${date}T${time}:00`;
}


// Chuyển chuỗi giờ cục bộ thành Date.
// Không dùng UTC để tránh lệch múi giờ.
function parseLocalDateTime(value) {
  const normalizedValue =
    localInputValue(value);

  if (!normalizedValue) {
    return null;
  }

  const [
    datePart,
    timePart
  ] = normalizedValue.split('T');

  const [
    year,
    month,
    day
  ] = datePart
    .split('-')
    .map(Number);

  const [
    hour,
    minute
  ] = timePart
    .split(':')
    .map(Number);

  if (
    !year ||
    !month ||
    !day ||
    !Number.isInteger(hour) ||
    !Number.isInteger(minute)
  ) {
    return null;
  }

  return new Date(
    year,
    month - 1,
    day,
    hour,
    minute,
    0,
    0
  );
}


// Chuyển Date thành YYYY-MM-DDTHH:mm.
function toLocalInputValue(date) {
  if (
    !(date instanceof Date) ||
    Number.isNaN(date.getTime())
  ) {
    return '';
  }

  const year =
    date.getFullYear();

  const month =
    String(
      date.getMonth() + 1
    ).padStart(2, '0');

  const day =
    String(
      date.getDate()
    ).padStart(2, '0');

  const hour =
    String(
      date.getHours()
    ).padStart(2, '0');

  const minute =
    String(
      date.getMinutes()
    ).padStart(2, '0');

  return (
    `${year}-${month}-${day}` +
    `T${hour}:${minute}`
  );
}


// Hiển thị ngày giờ ngắn gọn.
function formatLocalLimit(value) {
  const date =
    parseLocalDateTime(value);

  if (!date) {
    return '—';
  }

  return new Intl.DateTimeFormat(
    'vi-VN',
    {
      dateStyle: 'short',
      timeStyle: 'short'
    }
  ).format(date);
}


// Tính khoảng checkout hợp lệ.
//
// Backend quy định:
// - Không trước check-in.
// - Không vượt quá 18 giờ từ check-in.
// - Không nằm quá 15 phút trong tương lai.
function getCheckoutLimits(item) {
  const checkIn =
    parseLocalDateTime(
      item.checkInTime
    );

  if (!checkIn) {
    return {
      minValue: '',
      maxValue: ''
    };
  }

  const maxByDuration =
    new Date(
      checkIn.getTime() +
      18 * 60 * 60 * 1000
    );

  const maxByCurrentTime =
    new Date(
      Date.now() +
      15 * 60 * 1000
    );

  const maximumDate =
    maxByDuration <
    maxByCurrentTime
      ? maxByDuration
      : maxByCurrentTime;

  return {
    minValue:
      toLocalInputValue(checkIn),

    maxValue:
      toLocalInputValue(maximumDate)
  };
}


// So sánh hai thời điểm theo phút.
function differenceInMinutes(
  firstValue,
  secondValue
) {
  const first =
    parseLocalDateTime(firstValue);

  const second =
    parseLocalDateTime(secondValue);

  if (!first || !second) {
    return 0;
  }

  return Math.abs(
    (
      first.getTime() -
      second.getTime()
    ) /
    60000
  );
}


function RequestCard({
  item,
  mode,
  onChanged
}) {
  const initialDateTime =
    item.requestedCheckOutTime ||
    item.proposedCheckOutTime;

  const initialParts =
    splitLocalDateTime(
      initialDateTime
    );

  const [
    checkoutDate,
    setCheckoutDate
  ] = useState(
    initialParts.date
  );

  const [
    checkoutClock,
    setCheckoutClock
  ] = useState(
    initialParts.time
  );

  const [
    reason,
    setReason
  ] = useState(
    item.reason || ''
  );

  const [
    rejectReason,
    setRejectReason
  ] = useState('');

  const [
    busy,
    setBusy
  ] = useState(false);

  const [
    error,
    setError
  ] = useState('');


  // Đồng bộ lại form khi dữ liệu từ Backend thay đổi.
  useEffect(() => {
    const nextDateTime =
      item.requestedCheckOutTime ||
      item.proposedCheckOutTime;

    const nextParts =
      splitLocalDateTime(
        nextDateTime
      );

    setCheckoutDate(
      nextParts.date
    );

    setCheckoutClock(
      nextParts.time
    );

    setReason(
      item.reason || ''
    );

    setError('');
  }, [
    item.id,
    item.status,
    item.requestedCheckOutTime,
    item.proposedCheckOutTime,
    item.reason
  ]);


  const [
    label,
    className
  ] =
    STATUS[item.status] ||
    [
      item.status,
      'neutral'
    ];


  const editable =
    mode === 'mine' &&
    [
      'AWAITING_EMPLOYEE',
      'REJECTED'
    ].includes(
      item.status
    );


  const limits =
    useMemo(
      () =>
        getCheckoutLimits(item),
      [
        item.checkInTime
      ]
    );


  const minParts =
    splitLocalDateTime(
      limits.minValue
    );

  const maxParts =
    splitLocalDateTime(
      limits.maxValue
    );


  const checkoutDateTime =
    buildLocalDateTime(
      checkoutDate,
      checkoutClock
    );


  // Chỉ giới hạn giờ khi ngày đang chọn
  // là ngày bắt đầu hoặc ngày tối đa.
  const minimumClock =
    checkoutDate ===
    minParts.date
      ? minParts.time
      : undefined;

  const maximumClock =
    checkoutDate ===
    maxParts.date
      ? maxParts.time
      : undefined;


  async function act(
    action,
    body
  ) {
    setBusy(true);
    setError('');

    try {
      await axios.put(
        `/api/checkout-requests/` +
        `${item.id}/${action}`,
        body
      );

      await onChanged();
    } catch (err) {
      setError(
        err.response?.data?.message ||
        'Không thể xử lý yêu cầu.'
      );
    } finally {
      setBusy(false);
    }
  }


  async function handleSubmitRequest() {
    setError('');

    if (
      !checkoutDate ||
      !checkoutClock
    ) {
      setError(
        'Vui lòng chọn đầy đủ ngày và giờ checkout.'
      );

      return;
    }

    const selectedDateTime =
      parseLocalDateTime(
        checkoutDateTime
      );

    const minimumDateTime =
      parseLocalDateTime(
        limits.minValue
      );

    const maximumDateTime =
      parseLocalDateTime(
        limits.maxValue
      );

    if (!selectedDateTime) {
      setError(
        'Giờ checkout không hợp lệ.'
      );

      return;
    }

    if (
      minimumDateTime &&
      selectedDateTime <
        minimumDateTime
    ) {
      setError(
        'Giờ checkout không được trước giờ check-in.'
      );

      return;
    }

    if (
      maximumDateTime &&
      selectedDateTime >
        maximumDateTime
    ) {
      setError(
        'Giờ checkout tối đa của ca này là ' +
        `${formatLocalLimit(
          limits.maxValue
        )}.`
      );

      return;
    }

    const proposedValue =
      localInputValue(
        item.proposedCheckOutTime
      );

    const checkoutWasChanged =
      differenceInMinutes(
        checkoutDateTime,
        proposedValue
      ) > 1;

    if (
      checkoutWasChanged &&
      !reason.trim()
    ) {
      setError(
        'Vui lòng nhập lý do khi thay đổi giờ checkout tạm.'
      );

      return;
    }

    await act(
      'submit',
      {
        checkOutTime:
          checkoutDateTime,

        reason:
          reason.trim() ||
          null
      }
    );
  }


  return (
    <article className="co-card">
      <div className="co-card-head">
        <div>
          <strong>
            {mode === 'review'
              ? item.fullName
              : item.shiftName}
          </strong>

          <span>
            {item.workDate}
            {' · '}
            {item.shiftName}
            {' ('}
            {item.startTime}
            {'–'}
            {item.endTime}
            {')'}
          </span>
        </div>

        <span
          className={
            `co-status ${className}`
          }
        >
          {label}
        </span>
      </div>


      <div className="co-times">
        <div>
          <span>Check-in</span>

          <strong>
            {formatVietnamDateTime(
              item.checkInTime
            )}
          </strong>
        </div>

        <div>
          <span>Checkout tạm</span>

          <strong>
            {formatVietnamDateTime(
              item.proposedCheckOutTime
            )}
          </strong>
        </div>

        {item.requestedCheckOutTime && (
          <div>
            <span>Giờ đề nghị</span>

            <strong>
              {formatVietnamDateTime(
                item.requestedCheckOutTime
              )}
            </strong>
          </div>
        )}
      </div>


      {mode === 'review' && (
        <p className="co-meta">
          {item.roleName}
          {' · '}
          {item.branchName ||
            'Chưa rõ cơ sở'}
          {' · Lý do: '}
          {item.reason ||
            'Xác nhận giờ tạm'}
        </p>
      )}


      {item.rejectReason && (
        <p className="co-alert">
          Lý do từ chối:{' '}
          {item.rejectReason}
        </p>
      )}


      {editable && (
        <>
          <div className="co-checkout-limit">
            <strong>
              Khoảng thời gian được phép:
            </strong>

            <span>
              {formatLocalLimit(
                limits.minValue
              )}
              {' đến '}
              {formatLocalLimit(
                limits.maxValue
              )}
            </span>
          </div>

          <div className="co-form">
            <div className="co-datetime-fields">
              <label>
                Ngày checkout

                <input
                  disabled={busy}
                  max={
                    maxParts.date ||
                    undefined
                  }
                  min={
                    minParts.date ||
                    undefined
                  }
                  onChange={(event) => {
                    setCheckoutDate(
                      event.target.value
                    );

                    setError('');
                  }}
                  type="date"
                  value={checkoutDate}
                />
              </label>

              <label>
                Giờ checkout

                <input
                  disabled={busy}
                  max={maximumClock}
                  min={minimumClock}
                  onChange={(event) => {
                    setCheckoutClock(
                      event.target.value
                    );

                    setError('');
                  }}
                  step="60"
                  type="time"
                  value={checkoutClock}
                />
              </label>
            </div>

            <label>
              Lý do

              <textarea
                disabled={busy}
                maxLength={500}
                onChange={(event) => {
                  setReason(
                    event.target.value
                  );

                  setError('');
                }}
                placeholder={
                  'Bắt buộc nếu thay đổi giờ checkout tạm'
                }
                rows="2"
                value={reason}
              />
            </label>

            <button
              disabled={
                busy ||
                !checkoutDate ||
                !checkoutClock
              }
              onClick={
                handleSubmitRequest
              }
              type="button"
            >
              {busy
                ? 'Đang gửi...'
                : 'Xác nhận và gửi duyệt'}
            </button>
          </div>
        </>
      )}


      {mode === 'review' && (
        <div className="co-review-actions">
          <button
            className="approve"
            disabled={busy}
            onClick={() =>
              act('approve')
            }
            type="button"
          >
            Duyệt checkout
          </button>

          <input
            disabled={busy}
            onChange={(event) =>
              setRejectReason(
                event.target.value
              )
            }
            placeholder="Lý do từ chối"
            value={rejectReason}
          />

          <button
            className="reject"
            disabled={
              busy ||
              !rejectReason.trim()
            }
            onClick={() =>
              act(
                'reject',
                {
                  reason:
                    rejectReason.trim()
                }
              )
            }
            type="button"
          >
            Từ chối
          </button>
        </div>
      )}


      {error && (
        <p className="co-error">
          {error}
        </p>
      )}
    </article>
  );
}


export function CheckoutRequestTab({
  canReview = false
}) {
  const [
    mode,
    setMode
  ] = useState(
    canReview
      ? 'review'
      : 'mine'
  );

  const [
    mine,
    setMine
  ] = useState([]);

  const [
    review,
    setReview
  ] = useState([]);

  const [
    loading,
    setLoading
  ] = useState(true);

  const [
    error,
    setError
  ] = useState('');


  async function load() {
    setLoading(true);
    setError('');

    try {
      const calls = [
        axios.get(
          '/api/checkout-requests/mine'
        )
      ];

      if (canReview) {
        calls.push(
          axios.get(
            '/api/checkout-requests/review'
          )
        );
      }

      const [
        mineResponse,
        reviewResponse
      ] = await Promise.all(
        calls
      );

      setMine(
        Array.isArray(
          mineResponse.data
        )
          ? mineResponse.data
          : []
      );

      if (reviewResponse) {
        setReview(
          Array.isArray(
            reviewResponse.data
          )
            ? reviewResponse.data
            : []
        );
      }
    } catch (err) {
      setError(
        err.response?.data?.message ||
        'Không thể tải yêu cầu checkout.'
      );
    } finally {
      setLoading(false);
    }
  }


  useEffect(() => {
    let cancelled = false;

    const calls = [
      axios.get(
        '/api/checkout-requests/mine'
      )
    ];

    if (canReview) {
      calls.push(
        axios.get(
          '/api/checkout-requests/review'
        )
      );
    }

    Promise.all(calls)
      .then(
        ([
          mineResponse,
          reviewResponse
        ]) => {
          if (cancelled) {
            return;
          }

          setMine(
            Array.isArray(
              mineResponse.data
            )
              ? mineResponse.data
              : []
          );

          if (reviewResponse) {
            setReview(
              Array.isArray(
                reviewResponse.data
              )
                ? reviewResponse.data
                : []
            );
          }
        }
      )
      .catch((err) => {
        if (!cancelled) {
          setError(
            err.response
              ?.data?.message ||
            'Không thể tải yêu cầu checkout.'
          );
        }
      })
      .finally(() => {
        if (!cancelled) {
          setLoading(false);
        }
      });

    return () => {
      cancelled = true;
    };
  }, [
    canReview
  ]);


  const items =
    useMemo(
      () =>
        mode === 'review'
          ? review
          : mine,
      [
        mode,
        review,
        mine
      ]
    );


  return (
    <section className="co-page">
      <div className="co-intro">
        <div>
          <p className="sd-eyebrow">
            Chấm công
          </p>

          <h2>
            Xử lý quên checkout
          </h2>
        </div>

        <button
          className="co-refresh"
          disabled={loading}
          onClick={load}
          type="button"
        >
          Làm mới
        </button>
      </div>

      <p className="co-description">
        Checkout tạm được tạo 30 phút sau
        khi ca kết thúc. Giờ làm chỉ được cộng
        vào lương sau khi yêu cầu được duyệt.
      </p>

      {canReview && (
        <div className="co-tabs">
          <button
            className={
              mode === 'review'
                ? 'active'
                : ''
            }
            onClick={() =>
              setMode('review')
            }
            type="button"
          >
            Chờ tôi duyệt ({review.length})
          </button>

          <button
            className={
              mode === 'mine'
                ? 'active'
                : ''
            }
            onClick={() =>
              setMode('mine')
            }
            type="button"
          >
            Yêu cầu của tôi ({mine.length})
          </button>
        </div>
      )}

      {error && (
        <p className="co-error">
          {error}
        </p>
      )}

      {loading ? (
        <p className="co-empty">
          Đang tải...
        </p>
      ) : items.length === 0 ? (
        <p className="co-empty">
          Không có yêu cầu nào.
        </p>
      ) : (
        <div className="co-list">
          {items.map((item) => (
            <RequestCard
              item={item}
              key={item.id}
              mode={mode}
              onChanged={load}
            />
          ))}
        </div>
      )}
    </section>
  );
}