import { Component, computed, inject, signal } from '@angular/core';
import { DecimalPipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { DemoRepository } from '../../demo/demo-repository';
import { Container, Position } from '../../core/models/entities';
import { normalizeNumber } from '../../demo/rules/container-rules';
import { move, resize, slots, isBlocked } from '../../demo/rules/yard-rules';
import { Badge, DynamicForm, Field, Icon, Modal } from '../../shared/ui';
@Component({
  selector: 'app-yard',
  imports: [DecimalPipe, FormsModule, RouterLink, Badge, DynamicForm, Icon, Modal],
  templateUrl: './yard-page.html',
  styleUrl: './yard-page.scss',
})
export class YardPage {
  store = inject(DemoRepository);
  route = inject(ActivatedRoute);
  router = inject(Router);
  blockId = signal('A');
  tier = signal(1);
  mode = signal('size');
  view = signal('grid');
  selected = signal('');
  search = '';
  error = signal('');
  moving = signal(false);
  resizing = signal(false);
  constructor() {
    this.route.queryParamMap.subscribe((q) => {
      this.blockId.set(
        this.store.state().blocks.some((b) => b.id === q.get('block')) ? q.get('block')! : 'A',
      );
      this.tier.set(Math.max(1, Math.min(Number(q.get('tier')) || 1, this.block()?.tiers || 1)));
      this.selected.set(q.get('container') || '');
    });
  }
  block = computed(() => this.store.state().blocks.find((b) => b.id === this.blockId())!);
  container = computed(() => this.store.containers().find((c) => c.number === this.selected()));
  blockContainers = computed(() =>
    this.store
      .containers()
      .filter((c) => this.store.position(c.number)?.blockId === this.blockId()),
  );
  stats(id: string) {
    const b = this.store.state().blocks.find((b) => b.id === id)!,
      cs = this.store.containers().filter((c) => this.store.position(c.number)?.blockId === id),
      used = cs.reduce((n, c) => n + (c.size === 40 ? 2 : 1), 0),
      capacity = b.oddBays * b.rows * b.tiers;
    return { count: cs.length, used, capacity, percent: capacity ? (used / capacity) * 100 : 0 };
  }
  rows() {
    return Array.from({ length: this.block().rows }, (_, i) => String.fromCharCode(65 + i));
  }
  bays() {
    return Array.from({ length: this.block().oddBays }, (_, i) => i * 2 + 1);
  }
  tiers() {
    return Array.from({ length: this.block().tiers }, (_, i) => i + 1);
  }
  cells(row: string): { slot: number; span: number; container?: Container }[] {
    const result: { slot: number; span: number; container?: Container }[] = [];
    for (let slot = 1; slot <= this.block().oddBays; slot++) {
      const c = this.blockContainers().find((c) => {
        const p = this.store.position(c.number);
        return (
          p?.kind === 'physical' &&
          p.row === row &&
          p.tier === this.tier() &&
          slots(p, c.size).includes(slot)
        );
      });
      result.push({ slot, span: c?.size === 40 ? 2 : 1, container: c });
      if (c?.size === 40) slot++;
    }
    return result;
  }
  chooseBlock(id: string) {
    this.error.set('');
    this.router.navigate([], {
      queryParams: { block: id, tier: 1, container: null },
      queryParamsHandling: 'merge',
    });
  }
  chooseTier(t: number) {
    this.router.navigate([], { queryParams: { tier: t }, queryParamsHandling: 'merge' });
  }
  choose(n: string) {
    this.router.navigate([], { queryParams: { container: n }, queryParamsHandling: 'merge' });
  }
  find() {
    const n = normalizeNumber(this.search),
      c = this.store.containers().find((c) => c.number === n);
    if (!c) {
      this.error.set('Không tìm thấy container trong danh mục.');
      return;
    }
    const p = this.store.position(n);
    if (!p) {
      this.error.set('Container hiện ngoài bãi.');
      return;
    }
    this.error.set('');
    this.router.navigate([], {
      queryParams: { block: p.blockId, tier: p.kind === 'physical' ? p.tier : 1, container: n },
      queryParamsHandling: 'merge',
    });
  }
  color(c: Container) {
    if (this.mode() === 'operator')
      return this.store.state().operators.find((o) => o.id === c.operatorId)?.color || '#008ca5';
    if (this.mode() === 'type')
      return ['#0097b2', '#009979', '#c38b2e', '#8061bc', '#dd795c', '#427cac', '#687c98'][
        ['Dry', 'Reefer', 'Open Top', 'Flat Rack', 'Bunker', 'Ventilated', 'Specialized'].indexOf(
          c.type,
        )
      ];
    if (this.mode() === 'booking') {
      const b = this.store.booking(c.number);
      if (!b) return '#6b8798';
      return this.store.orderStatus(b.orderId) === 'Sắp hết hạn' ? '#c99429' : '#008fac';
    }
    return c.type === 'Reefer' ? '#009b77' : c.size === 40 ? '#6650df' : '#0095b0';
  }
  legend() {
    if (this.mode() === 'operator')
      return this.store.state().operators.map((o) => ({ label: o.name, color: o.color }));
    if (this.mode() === 'booking')
      return [
        { label: 'Đang giữ chỗ', color: '#008fac' },
        { label: 'Chưa gán lệnh', color: '#6b8798' },
        { label: 'Sắp hết hạn', color: '#c99429' },
      ];
    if (this.mode() === 'type')
      return ['Dry', 'Reefer', 'Open Top', 'Flat Rack', 'Bunker', 'Ventilated', 'Specialized'].map(
        (label, i) => ({
          label,
          color: ['#0097b2', '#009979', '#c38b2e', '#8061bc', '#dd795c', '#427cac', '#687c98'][i],
        }),
      );
    return [
      { label: '20ft', color: '#0095b0' },
      { label: '40ft', color: '#6650df' },
      { label: 'Reefer', color: '#009b77' },
    ];
  }
  blocked = isBlocked;
  moveFields(): Field[] {
    return [
      {
        key: 'number',
        label: 'Container',
        required: true,
        options: this.store
          .containers()
          .filter((c) => !!this.store.position(c.number))
          .map((c) => ({
            value: c.number,
            label: c.number + ' · ' + this.store.location(c.number),
          })),
      },
      {
        key: 'blockId',
        label: 'Block đích',
        required: true,
        options: this.store.state().blocks.map((b) => ({ value: b.id, label: b.name })),
      },
      { key: 'bay', label: 'Bay (bỏ qua khi chọn block ảo)', type: 'number', min: 1 },
      { key: 'row', label: 'Row (bỏ qua khi chọn block ảo)' },
      { key: 'tier', label: 'Tier (bỏ qua khi chọn block ảo)', type: 'number', min: 1 },
    ];
  }
  moveValue() {
    return { number: this.selected(), blockId: this.blockId(), bay: 1, row: 'A', tier: 1 };
  }
  move(v: Record<string, unknown>) {
    const blockId = String(v['blockId']),
      b = this.store.state().blocks.find((b) => b.id === blockId);
    const target: Position = b?.virtual
      ? { kind: 'virtual', blockId }
      : {
          kind: 'physical',
          blockId,
          bay: Number(v['bay']),
          row: String(v['row']).trim().toUpperCase(),
          tier: Number(v['tier']),
        };
    const r = this.store.run(
      (s) => move(s, { number: String(v['number']), target }, this.store.now()),
      'Đã di chuyển container.',
    );
    if (!r.ok) this.error.set(`[${r.error.code}] ${r.error.message}`);
    else {
      this.moving.set(false);
      this.router.navigate([], {
        queryParams: {
          block: blockId,
          tier: target.kind === 'physical' ? target.tier : 1,
          container: v['number'],
        },
        queryParamsHandling: 'merge',
      });
    }
  }
  resizeFields: Field[] = [
    {
      key: 'oddBays',
      label: 'Số bay lẻ (slot 20ft)',
      type: 'number',
      required: true,
      min: 1,
      max: 50,
    },
    { key: 'rows', label: 'Số Row', type: 'number', required: true, min: 1, max: 26 },
    { key: 'tiers', label: 'Số Tier', type: 'number', required: true, min: 1, max: 10 },
  ];
  resizeValue() {
    const b = this.block();
    return { oddBays: b.oddBays, rows: b.rows, tiers: b.tiers };
  }
  resize(v: Record<string, unknown>) {
    const r = this.store.run(
      (s) =>
        resize(s, {
          blockId: this.blockId(),
          oddBays: Number(v['oddBays']),
          rows: Number(v['rows']),
          tiers: Number(v['tiers']),
        }),
      'Đã cập nhật kích thước block.',
    );
    if (!r.ok) this.error.set(`[${r.error.code}] ${r.error.message}`);
    else this.resizing.set(false);
  }
  order(number: string) {
    const b = this.store.booking(number);
    return b ? this.store.state().orders.find((o) => o.id === b.orderId) : null;
  }
}
