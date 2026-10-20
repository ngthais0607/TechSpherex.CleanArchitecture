import { Component, computed, inject, signal } from '@angular/core';
import { DatePipe, DecimalPipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { DemoRepository } from '../../demo/demo-repository';
import { inventory } from '../../demo/queries/analytics';
import { TYPES } from '../../core/models/entities';
import { Icon } from '../../shared/ui';
import { downloadWorkbook } from '../../core/export/workbook-export';
@Component({
  selector: 'app-inventory',
  imports: [DatePipe, DecimalPipe, FormsModule, RouterLink, Icon],
  templateUrl: './inventory-page.html',
})
export class InventoryPage {
  store = inject(DemoRepository);
  operator = signal('');
  type = signal('');
  size = signal('');
  types = TYPES;
  busy = signal(false);
  error = signal('');
  data = computed(() =>
    inventory(
      this.store.state(),
      { operatorId: this.operator(), type: this.type(), size: this.size() },
      this.store.now(),
    ),
  );
  floor = Math.floor;
  async export() {
    this.busy.set(true);
    this.error.set('');
    try {
      await downloadWorkbook(
        'Tồn bãi',
        [
          'Số container',
          'Hãng khai thác',
          'Loại',
          'Kích thước',
          'Vị trí',
          'Ngày nhập',
          'Số ngày lưu',
        ],
        this.data().rows.map((r) => [
          r.number,
          this.store.operator(r.container.operatorId),
          r.container.type,
          r.container.size + 'ft',
          this.store.location(r.number),
          new Date(r.inAt),
          r.days,
        ]),
        `Hãng: ${this.operator() ? this.store.operator(this.operator()) : 'Tất cả'} · Loại: ${this.type() || 'Tất cả'} · Cỡ: ${this.size() || 'Tất cả'}`,
      );
    } catch {
      this.error.set('Không xuất được Excel. Vui lòng thử lại.');
    } finally {
      this.busy.set(false);
    }
  }
}
