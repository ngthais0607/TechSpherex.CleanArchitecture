import { DepotSnapshot, Filters } from '../../core/models/entities';
import { dayKey } from '../rules/container-rules';
export function inventory(s: DepotSnapshot, f: Filters, now: Date) {
  const rows = s.lifecycles
    .filter((l) => !l.outAt)
    .map((l) => ({
      ...l,
      container: s.containers.find((c) => c.number === l.number)!,
      days: Math.max(0, (now.getTime() - Date.parse(l.inAt)) / 86400000),
    }))
    .filter(
      (r) =>
        (!f.operatorId || r.container.operatorId === f.operatorId) &&
        (!f.type || r.container.type === f.type) &&
        (!f.size || r.container.size === Number(f.size)),
    );
  const byOperator = s.operators.map((o) => {
    const subset = rows.filter((r) => r.container.operatorId === o.id);
    return {
      ...o,
      count: subset.length,
      avg: subset.length ? subset.reduce((v, r) => v + r.days, 0) / subset.length : 0,
      max: Math.max(0, ...subset.map((r) => r.days)),
      longStay: subset.filter((r) => r.days >= 3).length,
    };
  });
  return {
    rows,
    total: rows.length,
    average: rows.length ? rows.reduce((v, r) => v + r.days, 0) / rows.length : 0,
    longStay: rows.filter((r) => r.days >= 3).length,
    byOperator,
    top: [...rows].sort((a, b) => b.days - a.days || a.number.localeCompare(b.number)).slice(0, 5),
  };
}
export function dashboard(s: DepotSnapshot, now: Date) {
  const current = s.lifecycles.filter((l) => !l.outAt),
    real = current.filter((l) => l.position?.kind === 'physical');
  const usedTeu = real.reduce(
      (v, l) => v + (s.containers.find((c) => c.number === l.number)!.size === 40 ? 2 : 1),
      0,
    ),
    capacity = s.blocks
      .filter((b) => !b.virtual)
      .reduce((v, b) => v + b.oddBays * b.rows * b.tiers, 0);
  const today = s.gateTransactions.filter(
    (t) => t.status === 'Active' && dayKey(new Date(t.at)) === dayKey(now),
  );
  const traffic = Array.from({ length: 7 }, (_, i) => {
    const date = dayKey(new Date(now.getTime() - (6 - i) * 86400000)),
      tx = s.gateTransactions.filter(
        (t) => t.status === 'Active' && dayKey(new Date(t.at)) === date,
      );
    return {
      date,
      in: tx.filter((t) => t.direction === 'In').length,
      out: tx.filter((t) => t.direction === 'Out').length,
    };
  });
  const previousTime = now.getTime() - 86400000,
    previousDay = dayKey(new Date(previousTime));
  const priorLives = s.lifecycles.filter(
    (l) => Date.parse(l.inAt) <= previousTime && (!l.outAt || Date.parse(l.outAt) > previousTime),
  );
  const priorTeu = priorLives.reduce((n, l) => {
    const p =
      l.position ||
      s.gateTransactions.find(
        (t) => t.lifecycleId === l.id && t.direction === 'Out' && t.status === 'Active',
      )?.previousPosition;
    return (
      n +
      (p?.kind === 'physical'
        ? s.containers.find((c) => c.number === l.number)!.size === 40
          ? 2
          : 1
        : 0)
    );
  }, 0);
  const previous = {
    inYard: priorLives.length,
    inToday: s.gateTransactions.filter(
      (t) =>
        t.status === 'Active' && t.direction === 'In' && dayKey(new Date(t.at)) === previousDay,
    ).length,
    outToday: s.gateTransactions.filter(
      (t) =>
        t.status === 'Active' && t.direction === 'Out' && dayKey(new Date(t.at)) === previousDay,
    ).length,
    occupancy: capacity ? (priorTeu / capacity) * 100 : 0,
  };
  return {
    inYard: current.length,
    usedTeu,
    capacity,
    occupancy: capacity ? (usedTeu / capacity) * 100 : 0,
    inToday: today.filter((t) => t.direction === 'In').length,
    outToday: today.filter((t) => t.direction === 'Out').length,
    traffic,
    previous,
  };
}
