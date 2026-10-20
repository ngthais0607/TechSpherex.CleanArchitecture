import { Component, inject, signal } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { DemoSession, safeReturnUrl } from '../../core/session/demo-session';
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
        <span class="pill">BẢN DEMO TƯƠNG TÁC</span>
        <h2>Chào mừng trở lại</h2>
        <p>Đăng nhập để bắt đầu ca vận hành của bạn.</p>
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
          <button class="button primary login-submit" type="submit">
            Đăng nhập <app-icon name="arrow" />
          </button>
        </form>
        <div class="demo-credentials">
          <strong>Tài khoản trải nghiệm</strong
          ><span>Tên đăng nhập: <b>demo</b> · Mật khẩu: <b>Demo&#64;123</b></span
          ><small>Dữ liệu demo được lưu riêng trong trình duyệt của bạn.</small>
        </div>
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
  session = inject(DemoSession);
  router = inject(Router);
  route = inject(ActivatedRoute);
  error = signal('');
  form = new FormGroup({
    username: new FormControl('', Validators.required),
    password: new FormControl('', Validators.required),
  });
  login() {
    const r = this.session.login(this.form.value.username || '', this.form.value.password || '');
    if (!r.ok) this.error.set(r.error.message);
    else
      this.router.navigateByUrl(safeReturnUrl(this.route.snapshot.queryParamMap.get('returnUrl')));
  }
}
