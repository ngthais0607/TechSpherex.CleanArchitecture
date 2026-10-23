import {
  Component,
  input,
  output,
  ElementRef,
  viewChild,
  afterNextRender,
  inject,
  Injectable,
  signal,
  OnDestroy,
  OnInit,
} from '@angular/core';
import { DatePipe, DecimalPipe } from '@angular/common';
import { ReactiveFormsModule, FormControl, FormGroup, Validators } from '@angular/forms';
@Component({
  selector: 'app-icon',
  template: `<svg
    viewBox="0 0 24 24"
    fill="none"
    stroke="currentColor"
    stroke-width="1.7"
    stroke-linecap="round"
    stroke-linejoin="round"
    aria-hidden="true"
  >
    <path [attr.d]="paths[name()] || paths['box']" />
  </svg>`,
  styles: `
    :host {
      display: inline-flex;
      width: 20px;
      height: 20px;
      flex-shrink: 0;
    }
    svg {
      width: 100%;
      height: 100%;
    }
  `,
})
export class Icon {
  name = input('box');
  paths: Record<string, string> = {
    box: 'M12 3 3 8v9l9 5 9-5V8L12 3ZM3 8l9 5 9-5M12 13v9',
    grid: 'M3 3h7v7H3zM14 3h7v7h-7zM3 14h7v7H3zM14 14h7v7h-7z',
    gate: 'M4 21V3h16v18M8 21V7h8v14M8 12h8M8 17h8',
    orders: 'M9 5H5v16h14V5h-4M9 3h6v5H9zM8 12h8M8 16h5',
    map: 'M3 5l6-2 6 2 6-2v16l-6 2-6-2-6 2V5ZM9 3v16M15 5v16',
    users:
      'M16 21v-2a4 4 0 0 0-4-4H6a4 4 0 0 0-4 4v2M9 11a4 4 0 1 0 0-8 4 4 0 0 0 0 8M22 21v-2a4 4 0 0 0-3-3.87M16 3a4 4 0 0 1 0 8',
    chart: 'M3 3v18h18M7 15l4-5 4 3 6-8',
    plus: 'M12 5v14M5 12h14',
    search: 'M21 21l-5-5M18 10a8 8 0 1 1-16 0 8 8 0 0 1 16 0',
    logout: 'M9 5H3v14h6M14 8l4 4-4 4M7 12h11',
    menu: 'M3 6h18M3 12h18M3 18h18',
    close: 'M6 6l12 12M18 6 6 18',
    arrow: 'M5 12h14M13 6l6 6-6 6',
    download: 'M12 3v12M7 10l5 5 5-5M4 17v4h16v-4',
    bell: 'M18 8a6 6 0 0 0-12 0c0 7-3 7-3 9h18c0-2-3-2-3-9M10 21h4',
    truck:
      'M1 5h13v12H1zM14 9h5l3 4v4h-8M7 18a2 2 0 1 1-4 0 2 2 0 0 1 4 0M20 18a2 2 0 1 1-4 0 2 2 0 0 1 4 0',
  };
}
@Component({
  selector: 'app-badge',
  template: `<span
    class="badge"
    [class.warn]="value().includes('Sắp') || value().includes('Chờ')"
    [class.danger]="value().includes('Hết') || value().includes('hỏng')"
    [class.muted]="value().includes('hủy') || value().includes('Ngưng')"
    >{{ value() }}</span
  >`,
})
export class Badge {
  value = input('');
}
@Component({
  selector: 'app-modal',
  imports: [Icon],
  template: `<dialog #dialog (cancel)="cancel($event)" (click)="backdrop($event)">
    <div class="modal-frame" [style.max-width.px]="width()">
      <header>
        <div>
          <span class="eyebrow">SNP DEPOT</span>
          <h2>{{ title() }}</h2>
        </div>
        <button class="icon-button" aria-label="Đóng" (click)="requestClose()">
          <app-icon name="close" />
        </button>
      </header>
      <section class="modal-body"><ng-content /></section>
      <footer><ng-content select="[footer]" /></footer>
    </div>
  </dialog>`,
  styles: `
    dialog {
      border: 0;
      padding: 0;
      background: transparent;
      width: calc(100% - 32px);
      max-width: 1100px;
      overflow: visible;
    }
    dialog::backdrop {
      background: #10223880;
      backdrop-filter: blur(4px);
    }
    .modal-frame {
      margin: auto;
      background: white;
      border-radius: 18px;
      box-shadow: 0 24px 80px #11283c40;
      max-height: calc(100dvh - 48px);
      display: flex;
      flex-direction: column;
      overflow: hidden;
    }
    header {
      display: flex;
      align-items: center;
      justify-content: space-between;
      padding: 20px 24px;
      border-bottom: 1px solid var(--border);
      flex-shrink: 0;
    }
    h2 {
      margin: 4px 0 0;
      font-size: 21px;
    }
    .modal-body {
      padding: 24px;
      overflow: auto;
      min-height: 0;
    }
    footer {
      display: flex;
      justify-content: flex-end;
      gap: 10px;
      padding: 16px 24px;
      border-top: 1px solid var(--border);
      flex-shrink: 0;
    }
    footer:empty {
      display: none;
    }
    @media (max-width: 800px) {
      .modal-body {
        padding: 18px;
      }
      header {
        padding: 16px 18px;
      }
    }
  `,
})
export class Modal implements OnDestroy {
  title = input('');
  width = input(880);
  dirty = input(false);
  closed = output();
  el = viewChild.required<ElementRef<HTMLDialogElement>>('dialog');
  constructor() {
    afterNextRender(() => this.el().nativeElement.showModal());
  }
  cancel(e: Event) {
    e.preventDefault();
    this.requestClose();
  }
  backdrop(e: MouseEvent) {
    if (e.target === this.el().nativeElement) this.requestClose();
  }
  requestClose() {
    if (!this.dirty() || confirm('Bỏ các thay đổi chưa lưu?')) {
      this.el().nativeElement.close();
      this.closed.emit();
    }
  }
  ngOnDestroy() {
    this.el()?.nativeElement.close();
  }
}
export interface Field {
  key: string;
  label: string;
  type?: string;
  required?: boolean;
  readonly?: boolean;
  options?: { value: string | number; label: string }[];
  hint?: string;
  min?: number;
  max?: number;
  section?: string;
}
@Component({
  selector: 'app-form',
  imports: [ReactiveFormsModule],
  template: `<form [formGroup]="form" (ngSubmit)="submit()" [id]="formId()">
    <div class="form-grid">
      @for (field of fields(); track field.key) {
        @if (field.section) {
          <h3 class="form-section">{{ field.section }}</h3>
        }
        <div class="field">
          <label [for]="formId() + '-' + field.key"
            >{{ field.label }}
            @if (field.required) {
              <span class="required">*</span>
            }
          </label>
          @if (field.options) {
            <select
              [id]="formId() + '-' + field.key"
              [formControlName]="field.key"
              [attr.aria-describedby]="field.key + '-error'"
            >
              <option value="">Chọn...</option>
              @for (option of field.options; track option.value) {
                <option [ngValue]="option.value">{{ option.label }}</option>
              }
            </select>
          } @else {
            <input
              [id]="formId() + '-' + field.key"
              [type]="field.type || 'text'"
              [formControlName]="field.key"
              [readOnly]="field.readonly || false"
              [attr.min]="field.min"
              [attr.max]="field.max"
              [attr.aria-describedby]="field.key + '-error'"
            />
          }
          @if (field.hint) {
            <small>{{ field.hint }}</small>
          }
          @if (errors()[field.key]) {
            <small class="field-error" [id]="field.key + '-error'">{{ errors()[field.key] }}</small>
          } @else if (form.get(field.key)?.invalid && form.get(field.key)?.touched) {
            <small class="field-error">Vui lòng nhập {{ field.label.toLowerCase() }}.</small>
          }
        </div>
      }
    </div>
    <ng-content />
  </form>`,
})
export class DynamicForm implements OnInit {
  fields = input.required<Field[]>();
  value = input<Record<string, unknown>>({});
  errors = input<Record<string, string>>({});
  formId = input('entry-form');
  saved = output<Record<string, unknown>>();
  form = new FormGroup<Record<string, FormControl>>({});
  ngOnInit() {
    for (const field of this.fields())
      this.form.addControl(
        field.key,
        new FormControl(this.value()[field.key] ?? '', field.required ? [Validators.required] : []),
      );
    if (this.form.contains('number') && this.form.contains('ownerCode'))
      this.form.controls['number'].valueChanges.subscribe((value) =>
        this.form.controls['ownerCode'].setValue(
          String(value).replace(/\s/g, '').toUpperCase().slice(0, 4),
          { emitEvent: false },
        ),
      );
  }
  submit() {
    this.form.markAllAsTouched();
    if (this.form.valid) this.saved.emit(this.form.getRawValue());
  }
  get dirty() {
    return this.form.dirty;
  }
}
@Component({
  selector: 'app-pagination',
  template: `<div class="pagination">
    <span
      >Hiển thị {{ total() ? (page() - 1) * size() + 1 : 0 }}–{{
        Math.min(page() * size(), total())
      }}
      / {{ total() }} bản ghi</span
    >
    <div>
      <button class="button small" [disabled]="page() <= 1" (click)="changed.emit(page() - 1)">
        Trước</button
      ><span class="page-number">{{ page() }}</span
      ><button
        class="button small"
        [disabled]="page() * size() >= total()"
        (click)="changed.emit(page() + 1)"
      >
        Sau
      </button>
    </div>
  </div>`,
})
export class Pagination {
  total = input(0);
  page = input(1);
  size = input(8);
  changed = output<number>();
  Math = Math;
}
