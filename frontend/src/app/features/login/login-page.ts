import { Component, inject, signal } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { AuthService } from '../../core/auth/auth-service';
import { safeReturnUrl } from '../../core/auth/safe-return-url';
import { ApiError } from '../../core/api/api-error';
import { finalize } from 'rxjs';
import { Icon } from '../../shared/ui';
@Component({
  selector: 'app-login',
  imports: [ReactiveFormsModule, Icon],
  template: `<div class="login-layout">
    <section class="login-story">
      <a class="brand light"
        ><span class="brand-mark"><app-icon name="box" /></span
        ><span>SNP Depot<small>MANAGEMENT</small></span></a
      >
      <div class="story-copy">
        <span class="eyebrow">KẾT NỐI MỌI HOẠT ĐỘNG</span>
        <h1>Mỗi container.<br />Một hành trình<br /><em>thông suốt.</em></h1>
        <p>Không gian điều hành tập trung cho quản lý container, giao nhận và khai thác bãi.</p>
        <div class="story-lines">
          <span>01 / Theo dõi tài sản</span><span>02 / Điều phối giao nhận</span
          ><span>03 / Tối ưu công suất</span>
        </div>
      </div>
      <small>SNP DEPOT · OPERATION CENTER</small>
    </section>
    <section class="login-form">
      <div class="login-inner">
        <span class="pill">TRUNG TÂM ĐIỀU HÀNH</span>
        <h2>Chào mừng trở lại</h2>
        <p>Đăng nhập để bắt đầu ca vận hành của bạn.</p>
        @if (session.status() === 'error' && session.storage.read()) {
          <div class="banner warning" role="alert">
            Chưa thể khôi phục phiên do lỗi kết nối máy chủ. Bạn có thể thử lại phiên hiện tại.
            <button
              class="button"
              aria-label="Thử khôi phục phiên"
              [disabled]="loading()"
              (click)="retryRestore()"
            >
              Thử lại phiên
            </button>
          </div>
        }
        <form [formGroup]="form" (ngSubmit)="login()">
          <div class="field">
            <label for="username">Tên đăng nhập</label
            ><input id="username" formControlName="username" autocomplete="username" />
          </div>
          <div class="field">
            <label for="password">Mật khẩu</label
            ><input
              id="password"
              type="password"
              formControlName="password"
              autocomplete="current-password"
            />
          </div>
          @if (error()) {
            <p class="banner error" role="alert">{{ error() }}</p>
          }
          <button
            class="button primary login-submit"
            type="submit"
            [disabled]="loading() || form.invalid"
          >
            {{ loading() ? 'Đang đăng nhập…' : 'Đăng nhập' }} <app-icon name="arrow" />
          </button>
        </form>
        @if (session.storage.warning()) {
          <p class="banner warning">{{ session.storage.warning() }}</p>
        }
      </div>
    </section>
  </div>`,
  styles: `
    .login-layout {
      min-height: 100dvh;
      display: grid;
      grid-template-columns: 1fr 1fr;
    }
    .login-story {
      background: #0c263c;
      color: white;
      padding: 44px 60px;
      display: flex;
      flex-direction: column;
      justify-content: space-between;
      overflow: hidden;
      position: relative;
    }
    .login-story:before {
      content: '';
      position: absolute;
      width: 500px;
      height: 500px;
      border: 1px solid #ffffff12;
      transform: rotate(35deg);
      right: -300px;
      top: 180px;
      box-shadow:
        0 0 0 60px #ffffff04,
        0 0 0 120px #ffffff03;
    }
    .story-copy {
      z-index: 1;
      margin: 80px 0;
    }
    .story-copy h1 {
      font-size: clamp(36px, 4vw, 60px);
      line-height: 1.18;
      letter-spacing: -2px;
      font-weight: 650;
      margin: 24px 0;
    }
    .story-copy em {
      font-style: normal;
      color: #60d7df;
    }
    .story-copy p {
      max-width: 390px;
      color: #bbcedd;
      line-height: 1.8;
    }
    .story-lines {
      display: grid;
      gap: 15px;
      margin-top: 32px;
      font-size: 12px;
      letter-spacing: 1px;
      color: #8cabbc;
    }
    .login-form {
      display: grid;
      place-items: center;
      padding: 40px;
    }
    .login-inner {
      width: 100%;
      max-width: 400px;
    }
    .login-inner h2 {
      font-size: 30px;
      margin: 22px 0 8px;
      letter-spacing: -1px;
    }
    .login-inner p {
      color: var(--muted);
    }
    form {
      display: grid;
      gap: 22px;
      margin-top: 32px;
    }
    .login-submit {
      justify-content: space-between;
      margin-top: 4px;
      width: 100%;
    }
    .demo-credentials {
      display: grid;
      gap: 8px;
      padding: 18px;
      background: #f3f8fa;
      border-radius: 12px;
      margin-top: 28px;
      font-size: 12px;
    }
    .demo-credentials small {
      color: var(--muted);
    }
    @media (max-width: 850px) {
      .login-layout {
        grid-template-columns: 1fr;
      }
      .login-story {
        display: none;
      }
      .login-form {
        min-height: 100dvh;
        padding: 24px;
      }
    }
  `,
})
export class LoginPage {
  session = inject(AuthService);
  router = inject(Router);
  route = inject(ActivatedRoute);
  error = signal('');
  loading = signal(false);
  form = new FormGroup({
    username: new FormControl('', Validators.required),
    password: new FormControl('', Validators.required),
  });
  retryRestore(): void {
    if (this.loading()) return;
    this.loading.set(true);
    this.error.set('');
    this.session
      .restore()
      .pipe(finalize(() => this.loading.set(false)))
      .subscribe(() => {
        if (this.session.status() === 'authenticated') {
          void this.router.navigateByUrl(
            safeReturnUrl(this.route.snapshot.queryParamMap.get('returnUrl')),
          );
        } else if (this.session.status() === 'error') {
          this.error.set('Máy chủ chưa phản hồi. Vui lòng thử khôi phục phiên lại sau.');
        }
      });
  }
  login() {
    if (this.form.invalid || this.loading()) {
      this.form.markAllAsTouched();
      return;
    }
    this.error.set('');
    this.loading.set(true);
    this.session
      .login({
        userName: this.form.value.username?.trim() || '',
        password: this.form.value.password || '',
      })
      .pipe(finalize(() => this.loading.set(false)))
      .subscribe({
        next: () => {
          this.form.controls.password.reset();
          this.router.navigateByUrl(
            safeReturnUrl(this.route.snapshot.queryParamMap.get('returnUrl')),
          );
        },
        error: (error: ApiError) => {
          this.form.controls.password.reset();
          this.error.set(
            error.status === 401
              ? 'Tên đăng nhập hoặc mật khẩu không đúng.'
              : error.status === 429
                ? `Đăng nhập quá nhiều lần. ${error.retryAfterSeconds ? 'Thử lại sau ' + error.retryAfterSeconds + ' giây.' : 'Vui lòng chờ rồi thử lại.'}`
                : error.detail || 'Không thể đăng nhập. Vui lòng thử lại.',
          );
        },
      });
  }
}
