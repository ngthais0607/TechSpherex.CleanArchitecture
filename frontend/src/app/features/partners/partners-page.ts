import { Component, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { DemoRepository } from '../../demo/demo-repository';
import {
  saveCustomer,
  saveOperator,
  deactivatePartner,
} from '../../demo/commands/partner-commands';
import { Badge, DynamicForm, Field, Icon, Modal, Pagination } from '../../shared/ui';
@Component({
  selector: 'app-partners',
  imports: [FormsModule, RouterLink, Badge, DynamicForm, Icon, Modal, Pagination],
  templateUrl: './partners-page.html',
})
export class PartnersPage {
  store = inject(DemoRepository);
  tab = signal<'customer' | 'operator'>('customer');
  search = signal('');
  page = signal(1);
  editing = signal<Record<string, unknown> | null>(null);
  showForm = signal(false);
  error = signal('');
  errors = signal<Record<string, string>>({});
  customers = computed(() =>
    this.store
      .state()
      .customers.filter((c) =>
        `${c.name} ${c.taxCode}`.toLowerCase().includes(this.search().toLowerCase()),
      ),
  );
  operators = computed(() =>
    this.store
      .state()
      .operators.filter((o) =>
        `${o.name} ${o.code}`.toLowerCase().includes(this.search().toLowerCase()),
      ),
  );
  orderCount(id: string) {
    return this.store.state().orders.filter((o) => o.customerId === id).length;
  }
  activeCount(id: string) {
    return this.store
      .state()
      .orders.filter(
        (o) =>
          o.customerId === id && ['Còn hạn', 'Sắp hết hạn'].includes(this.store.orderStatus(o.id)),
      ).length;
  }
  containerCount(id: string) {
    return this.store
      .state()
      .lifecycles.filter(
        (l) =>
          !l.outAt && this.store.containers().find((c) => c.number === l.number)?.operatorId === id,
      ).length;
  }
  fields(): Field[] {
    return this.tab() === 'customer'
      ? [
          {
            key: 'taxCode',
            label: 'Mã số thuế',
            required: true,
            readonly: !!this.editing() && this.orderCount(String(this.editing()!['id'])) > 0,
          },
          { key: 'name', label: 'Tên khách hàng', required: true },
          { key: 'email', label: 'Email', type: 'email' },
          { key: 'phone', label: 'Số điện thoại', type: 'tel' },
          { key: 'address', label: 'Địa chỉ' },
        ]
      : [
          {
            key: 'code',
            label: 'Mã hãng',
            required: true,
            readonly: !!this.editing(),
            hint: '3 chữ cái, ví dụ MSK.',
          },
          { key: 'name', label: 'Tên hãng khai thác', required: true },
          { key: 'contact', label: 'Email liên hệ', type: 'email' },
        ];
  }
  open(value?: object) {
    this.editing.set(value ? { ...value } : null);
    this.errors.set({});
    this.error.set('');
    this.showForm.set(true);
  }
  save(value: Record<string, unknown>) {
    const input = {
      ...value,
      id: this.editing()?.['id'],
      isActive: true,
      email: value['email'] || '',
      phone: value['phone'] || '',
      address: value['address'] || '',
      contact: value['contact'] || '',
    };
    const r = this.store.run(
      (s) =>
        this.tab() === 'customer'
          ? saveCustomer(s, input as never)
          : saveOperator(s, input as never),
      'Đã lưu thông tin đối tác.',
    );
    if (!r.ok) {
      this.error.set(r.error.message);
      this.errors.set(r.error.fields || {});
    } else this.showForm.set(false);
  }
  deactivate(id: string) {
    if (confirm('Ngưng hoạt động đối tác này? Các dữ liệu lịch sử vẫn được giữ.')) {
      const r = this.store.run(
        (s) => deactivatePartner(s, this.tab(), id, this.store.now()),
        'Đã ngưng hoạt động đối tác.',
      );
      if (!r.ok) this.error.set(r.error.message);
    }
  }
  switch(tab: 'customer' | 'operator') {
    this.tab.set(tab);
    this.page.set(1);
    this.search.set('');
    this.error.set('');
  }
  formValue() {
    return this.editing() || {};
  }
  formCancel() {
    if (confirm('Bỏ các thay đổi chưa lưu?')) this.showForm.set(false);
  }
}
