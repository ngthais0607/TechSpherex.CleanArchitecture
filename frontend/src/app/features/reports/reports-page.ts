import { Component, computed, inject, signal } from '@angular/core';
import { DecimalPipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { DemoRepository } from '../../demo/demo-repository';
import { report, trend } from '../../demo/queries/report';
import { dayKey } from '../../demo/rules/container-rules';
import { Icon } from '../../shared/ui';
import { downloadWorkbook } from '../../core/export/workbook-export';
@Component({
  selector: 'app-reports',
  imports: [DecimalPipe, FormsModule, Icon],
  templateUrl: './reports-page.html',
})
export class ReportsPage {
  store = inject(DemoRepository);
  from = dayKey(new Date(Date.now() - 6 * 86400000));
  to = dayKey(new Date());
  kind = 'summary';
  applied = signal({ from: this.from, to: this.to });
  error = signal('');
  busy = signal(false);
  trend = trend;
  data = computed(() => report(this.store.state(), this.applied(), this.store.now()));
  apply() {
    if (!this.from || !this.to || this.from > this.to) {
      this.error.set('Ngày bắt đầu phải trước hoặc bằng ngày kết thúc.');
      return;
    }
    this.error.set('');
    this.applied.set({ from: this.from, to: this.to });
  }
  async export() {
    this.busy.set(true);
    try {
      await downloadWorkbook(
        'Báo cáo hoạt động',
        ['Hãng khai thác', 'Nhập bãi', 'Xuất bãi', 'Đang lưu hiện tại', 'Tổng lượt'],
        this.data().rows.map((r) => [r.name, r.in, r.out, r.stored, r.total]),
        `${this.data().from} đến ${this.data().to}`,
      );
    } catch {
      this.error.set('Không xuất được Excel. Vui lòng thử lại.');
    } finally {
      this.busy.set(false);
    }
  }
}
