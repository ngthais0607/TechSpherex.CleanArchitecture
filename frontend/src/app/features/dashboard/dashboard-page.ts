import { Component, computed, inject } from '@angular/core';
import { DatePipe, DecimalPipe } from '@angular/common';
import { RouterLink } from '@angular/router';
import { DemoRepository } from '../../demo/demo-repository';
import { dashboard } from '../../demo/queries/analytics';
import { Badge, Icon } from '../../shared/ui';
import { trend } from '../../demo/queries/report';
import { dayKey } from '../../demo/rules/container-rules';
@Component({
  selector: 'app-dashboard',
  imports: [DatePipe, DecimalPipe, RouterLink, Icon, Badge],
  templateUrl: './dashboard-page.html',
  styles: `
    .traffic-chart {
      display: flex;
      gap: 18px;
      align-items: flex-end;
      height: 210px;
      padding: 20px 8px 0;
    }
    .chart-column {
      flex: 1;
      height: 100%;
      display: flex;
      flex-direction: column;
      justify-content: flex-end;
      align-items: center;
      gap: 10px;
      min-width: 0;
    }
    .chart-bars {
      height: 165px;
      display: flex;
      align-items: flex-end;
      justify-content: center;
      gap: 5px;
      width: 100%;
      border-bottom: 1px solid var(--border);
    }
    .bar {
      width: 15px;
      max-width: 32%;
      min-height: 3px;
      border-radius: 4px 4px 0 0;
      background: #0095ad;
      position: relative;
    }
    .bar.out {
      background: #bdc8f3;
    }
    .chart-column small {
      font-size: 10px;
      color: var(--muted);
    }
    .legend {
      display: flex;
      align-items: center;
      gap: 18px;
      font-size: 11px;
      color: var(--muted);
    }
    .legend i {
      display: inline-block;
      width: 9px;
      height: 9px;
      border-radius: 3px;
      margin-right: 5px;
      background: #0095ad;
    }
    .legend i.out {
      background: #bdc8f3;
    }
    .block-line {
      margin-bottom: 22px;
    }
    .block-line:last-child {
      margin-bottom: 0;
    }
    .activity {
      display: flex;
      align-items: center;
      gap: 14px;
      padding: 15px 22px;
      border-bottom: 1px solid #edf1f5;
    }
    .activity-icon {
      width: 34px;
      height: 34px;
      border-radius: 9px;
      background: #eaf9fc;
      color: var(--teal);
      display: grid;
      place-items: center;
    }
    .activity-copy {
      flex: 1;
      font-size: 12px;
    }
    .activity-copy small {
      display: block;
      color: var(--muted);
      margin-top: 4px;
    }
    .activity time {
      font-size: 11px;
      color: var(--muted);
    }
    .quick-note {
      display: grid;
      gap: 14px;
      padding: 22px;
    }
    .quick-note a {
      padding: 14px;
      border: 1px solid var(--border);
      border-radius: 9px;
      display: flex;
      align-items: center;
      justify-content: space-between;
      font-size: 12px;
    }
    .expiry-row {
      display: flex;
      align-items: center;
      gap: 14px;
      padding: 15px 22px;
      border-bottom: 1px solid #edf1f5;
      flex-wrap: wrap;
    }
    .expiry-row a {
      font-weight: 650;
    }
    .expiry-row .customer {
      flex: 1;
      font-size: 12px;
      color: var(--muted);
    }
  `,
})
export class DashboardPage {
  store = inject(DemoRepository);
  data = computed(() => dashboard(this.store.state(), this.store.now()));
  expiring = computed(() =>
    this.store
      .state()
      .orders.filter((o) => ['Sắp hết hạn', 'Hết hạn'].includes(this.store.orderStatus(o.id))),
  );
  recent = computed(() =>
    [...this.store.state().gateTransactions].sort((a, b) => b.at.localeCompare(a.at)).slice(0, 5),
  );
  maxTraffic = computed(() => Math.max(1, ...this.data().traffic.flatMap((t) => [t.in, t.out])));
  blockMetrics = computed(() =>
    this.store
      .state()
      .blocks.filter((b) => !b.virtual)
      .map((b) => {
        const used = this.store
          .state()
          .lifecycles.filter((l) => !l.outAt && l.position?.blockId === b.id)
          .reduce(
            (v, l) =>
              v + (this.store.containers().find((c) => c.number === l.number)!.size === 40 ? 2 : 1),
            0,
          );
        return {
          ...b,
          used,
          total: b.oddBays * b.rows * b.tiers,
          percent: (used / (b.oddBays * b.rows * b.tiers)) * 100,
        };
      }),
  );
  heldExpiring = computed(
    () =>
      this.store
        .state()
        .bookings.filter(
          (b) => b.status === 'Reserved' && this.store.orderStatus(b.orderId) === 'Sắp hết hạn',
        ).length,
  );
  changes = computed(() => {
    const d = this.data();
    return [
      trend(d.inYard, d.previous.inYard),
      trend(d.inToday, d.previous.inToday),
      trend(d.outToday, d.previous.outToday),
      trend(d.occupancy, d.previous.occupancy),
    ];
  });
  metrics = computed(() => {
    const d = this.data();
    return [
      {
        icon: 'box',
        value: d.inYard.toLocaleString('vi-VN'),
        label: 'Container trong bãi',
        note: 'Gồm container tại block ảo',
      },
      {
        icon: 'gate',
        value: String(d.inToday),
        label: 'Nhập bãi hôm nay',
        note: 'Giao dịch nhập chưa hủy',
      },
      {
        icon: 'truck',
        value: String(d.outToday),
        label: 'Xuất bãi hôm nay',
        note: 'Giao dịch xuất chưa hủy',
      },
      {
        icon: 'chart',
        value: d.occupancy.toLocaleString('vi-VN', { maximumFractionDigits: 1 }) + '%',
        label: 'Tỷ lệ lấp đầy',
        note: `${d.usedTeu} / ${d.capacity} TEU bãi thật`,
      },
    ];
  });
}
