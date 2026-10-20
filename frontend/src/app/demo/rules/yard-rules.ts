import { DepotSnapshot, Position, Result } from '../../core/models/entities';
import { activeLife, fail, ok, positionText, uid } from './container-rules';
export function slots(p: Position, size: number): number[] {
  return p.kind === 'virtual' ? [] : size === 20 ? [(p.bay + 1) / 2] : [p.bay / 2, p.bay / 2 + 1];
}
export function isBlocked(s: DepotSnapshot, number: string): boolean {
  const life = activeLife(s, number),
    c = s.containers.find((c) => c.number === number);
  if (!life?.position || life.position.kind === 'virtual' || !c) return false;
  const p = life.position,
    occupied = slots(p, c.size);
  return s.lifecycles.some(
    (l) =>
      !l.outAt &&
      l.position?.kind === 'physical' &&
      l.position.blockId === p.blockId &&
      l.position.row === p.row &&
      l.position.tier === p.tier + 1 &&
      slots(l.position, s.containers.find((c) => c.number === l.number)!.size).some((v) =>
        occupied.includes(v),
      ),
  );
}
export function placeCheck(s: DepotSnapshot, number: string, p: Position): Result<void> {
  const b = s.blocks.find((b) => b.id === p.blockId),
    c = s.containers.find((c) => c.number === number);
  if (!b || !c) return fail('Y2', 'Không tìm thấy block hoặc container.');
  if (p.kind === 'virtual')
    return b.virtual ? ok(undefined) : fail('Y7', 'Block này không phải block ảo.');
  const use = slots(p, c.size),
    row = p.row.charCodeAt(0) - 65;
  if (
    b.virtual ||
    !Number.isInteger(p.bay) ||
    p.bay < 1 ||
    (c.size === 20 ? p.bay % 2 !== 1 : p.bay % 2 !== 0)
  )
    return fail('Y1', '20ft dùng bay lẻ, 40ft dùng bay chẵn.');
  if (
    !/^[A-Z]$/.test(p.row) ||
    !Number.isInteger(p.tier) ||
    use.some((v) => v < 1 || v > b.oddBays) ||
    row < 0 ||
    row >= b.rows ||
    p.tier < 1 ||
    p.tier > b.tiers
  )
    return fail('Y2', 'Vị trí ngoài giới hạn block hoặc Row/Tier không hợp lệ.');
  const peers = s.lifecycles.filter(
    (l) =>
      !l.outAt &&
      l.number !== number &&
      l.position?.kind === 'physical' &&
      l.position.blockId === p.blockId &&
      l.position.row === p.row,
  );
  if (
    peers.some(
      (l) =>
        l.position?.kind === 'physical' &&
        l.position.tier === p.tier &&
        slots(l.position, s.containers.find((c) => c.number === l.number)!.size).some((v) =>
          use.includes(v),
        ),
    )
  )
    return fail('Y3', 'Vị trí đã có container.');
  if (p.tier > 1) {
    const below = peers.filter(
      (l) => l.position?.kind === 'physical' && l.position.tier === p.tier - 1,
    );
    const support = below.filter((l) =>
      slots(l.position!, s.containers.find((c) => c.number === l.number)!.size).some((v) =>
        use.includes(v),
      ),
    );
    if (
      !use.every((v) =>
        support.some((l) =>
          slots(l.position!, s.containers.find((c) => c.number === l.number)!.size).includes(v),
        ),
      ) ||
      (c.size === 20 &&
        support.some((l) => s.containers.find((c) => c.number === l.number)!.size === 40)) ||
      (c.size === 40 &&
        support.some(
          (l) =>
            s.containers.find((c) => c.number === l.number)!.size === 40 &&
            slots(l.position!, 40).join() !== use.join(),
        ))
    )
      return fail('Y4', 'Tầng dưới phải đỡ toàn bộ; không xếp 20ft lên 40ft.');
  }
  return ok(undefined);
}
export function move(
  s: DepotSnapshot,
  input: { number: string; target: Position },
  now: Date,
): Result<void> {
  const l = activeLife(s, input.number);
  if (!l) return fail('G2', 'Container không ở trong bãi.');
  if (isBlocked(s, input.number))
    return fail('Y5', 'Container đang bị đè, cần di chuyển tầng trên trước.');
  const r = placeCheck(s, input.number, input.target);
  if (!r.ok) return r;
  s.movements.push({
    id: uid(),
    number: input.number,
    at: now.toISOString(),
    from: l.position,
    to: input.target,
  });
  l.position = input.target;
  return ok(undefined);
}
export function resize(
  s: DepotSnapshot,
  input: { blockId: string; oddBays: number; rows: number; tiers: number },
): Result<void> {
  const b = s.blocks.find((b) => b.id === input.blockId);
  if (!b || b.virtual) return fail('Y2', 'Block không hợp lệ.');
  if (
    [input.oddBays, input.rows, input.tiers].some((n) => !Number.isInteger(n) || n < 1) ||
    input.rows > 26 ||
    input.oddBays > 50 ||
    input.tiers > 10
  )
    return fail('Y2', 'Bay 1–50, Row 1–26, Tier 1–10.');
  const blocker = s.lifecycles.find(
    (l) =>
      !l.outAt &&
      l.position?.kind === 'physical' &&
      l.position.blockId === b.id &&
      (slots(l.position, s.containers.find((c) => c.number === l.number)!.size).some(
        (v) => v > input.oddBays,
      ) ||
        l.position.row.charCodeAt(0) - 65 >= input.rows ||
        l.position.tier > input.tiers),
  );
  if (blocker)
    return fail(
      'Y6',
      `${blocker.number} tại ${positionText(blocker.position)} nằm ngoài vùng mới.`,
    );
  Object.assign(b, { oddBays: input.oddBays, rows: input.rows, tiers: input.tiers });
  return ok(undefined);
}
export function suggest(s: DepotSnapshot, number: string, blockId: string): Result<Position> {
  const b = s.blocks.find((b) => b.id === blockId),
    c = s.containers.find((c) => c.number === number);
  if (!b || !c) return fail('Y2', 'Chọn container và block.');
  if (b.virtual) return ok({ kind: 'virtual', blockId });
  for (let tier = 1; tier <= b.tiers; tier++)
    for (let row = 0; row < b.rows; row++)
      for (let bay = c.size === 20 ? 1 : 2; bay <= b.oddBays * 2 - 1; bay += 2) {
        const p: Position = {
          kind: 'physical',
          blockId,
          bay,
          row: String.fromCharCode(65 + row),
          tier,
        };
        if (placeCheck(s, number, p).ok) return ok(p);
      }
  return fail('Y3', 'Không còn vị trí phù hợp.');
}
