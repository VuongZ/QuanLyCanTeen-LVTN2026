function parseVietnamDatabaseDateTime(value) {
  if (!value) return null;

  const text = String(value).trim();
  const match = text.match(
    /^(\d{4})-(\d{2})-(\d{2})[T ](\d{2}):(\d{2})/
  );

  if (!match) return null;

  const [, year, month, day, hour, minute] = match;
  return { year, month, day, hour, minute };
}

export function formatVietnamDateTime(value) {
  const parts = parseVietnamDatabaseDateTime(value);
  if (!parts) return '—';

  return `${parts.hour}:${parts.minute} ${parts.day}/${parts.month}/${parts.year}`;
}

export function formatVietnamTime(value) {
  const parts = parseVietnamDatabaseDateTime(value);
  if (!parts) return '—';

  return `${parts.hour}:${parts.minute}`;
}
