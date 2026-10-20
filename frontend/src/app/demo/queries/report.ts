import { DepotSnapshot, Filters } from '../../core/models/entities';
import { dayKey } from '../rules/container-rules';
export function report(s: DepotSnapshot, f: Filters, now: Date) {
  const from = f.from || dayKey(new Date(now.getTime() - 6 * 86400000)),
    to = f.to || dayKey(now);
  const count = (start: string, end: string) =>
    s.gateTransactions.filter(
      (t) =>
        t.status === 'Active' && dayKey(new Date(t.at)) >= start && dayKey(new Date(t.at)) <= end,
    );
  const tx = count(from, to),
    days = (Date.parse(to) - Date.parse(from)) / 86400000 + 1;
  const prior = count(
    new Date(Date.parse(from) - days * 86400000).toISOString().slice(0, 10),
    new Date(Date.parse(from) - 86400000).toISOString().slice(0, 10),
  );
  const totals = (items: typeof tx) => ({
    in: items.filter((t) => t.direction === 'In').length,
    out: items.filter((t) => t.direction === 'Out').length,
  });
  const current = totals(tx),
    previous = totals(prior);
  const dwell = tx
    .filter((t) => t.direction === 'Out')
    .map((t) => {
      const l = s.lifecycles.find((l) => l.id === t.lifecycleId)!;
      return (Date.parse(t.at) - Date.parse(l.inAt)) / 86400000;
    });
  const rows = s.operators.map((o) => {
    const subset = tx.filter(
        (t) => s.containers.find((c) => c.number === t.number)?.operatorId === o.id,
      ),
      a = totals(subset);
    return {
      name: o.name,
      in: a.in,
      out: a.out,
      stored: s.lifecycles.filter(
        (l) =>
          !l.outAt &&
          !l.cancelled &&
          s.containers.find((c) => c.number === l.number)?.operatorId === o.id,
      ).length,
      total: subset.length,
    };
  });
  return {
    from,
    to,
    ...current,
    average: dwell.length ? dwell.reduce((a, b) => a + b, 0) / dwell.length : null,
    previous,
    rows,
  };
}
export function trend(current: number, previous: number) {
  return !previous
    ? current
      ? 'Chưa có cơ sở so sánh'
      : '0%'
    : `${current >= previous ? '+' : ''}${(((current - previous) / previous) * 100).toLocaleString('vi-VN', { maximumFractionDigits: 1 })}%`;
}
