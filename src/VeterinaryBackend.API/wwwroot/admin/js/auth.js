// Login/logout flow + screen toggling.
const Auth = {
  showLogin() {
    document.getElementById('login-screen').classList.remove('hidden');
    document.getElementById('login-screen').classList.add('flex');
    document.getElementById('dashboard').classList.add('hidden');
    document.getElementById('dashboard').classList.remove('flex');
  },

  showDashboard() {
    document.getElementById('login-screen').classList.add('hidden');
    document.getElementById('login-screen').classList.remove('flex');
    document.getElementById('dashboard').classList.remove('hidden');
    document.getElementById('dashboard').classList.add('flex');

    const username = Api.getUsername();
    document.getElementById('user-name').textContent = username;
    document.getElementById('user-initial').textContent = (username[0] || 'A').toUpperCase();
  },

  closeAllOverlays() {
    ['user-menu', 'section-modal', 'content-modal', 'media-picker-modal', 'confirm-modal', 'change-password-modal', 'reset-password-modal'].forEach(id => {
      const el = document.getElementById(id);
      if (el) el.classList.add('hidden');
    });
  },

  init() {
    const form = document.getElementById('login-form');
    const errorBox = document.getElementById('login-error');
    const btn = document.getElementById('login-btn');
    const btnText = btn.querySelector('.login-btn-text');
    const btnSpinner = btn.querySelector('.login-btn-spinner');

    form.addEventListener('submit', async (e) => {
      e.preventDefault();
      errorBox.classList.add('hidden');

      const fd = new FormData(form);
      const username = (fd.get('username') || '').trim();
      const password = fd.get('password') || '';

      if (!username || !password) {
        errorBox.textContent = 'Foydalanuvchi nomi va parolni kiriting';
        errorBox.classList.remove('hidden');
        return;
      }

      btn.disabled = true;
      btnText.textContent = 'Tekshirilmoqda...';
      btnSpinner.classList.remove('hidden');

      try {
        const tokens = await Api.login(username, password);
        Api.setSession(tokens);
        form.reset();
        this.showDashboard();
        App.boot();
        UI.toast(`Xush kelibsiz, ${tokens.username}!`, 'success');
      } catch (err) {
        let msg;
        if (err.status === 401) msg = 'Foydalanuvchi nomi yoki parol noto\'g\'ri';
        else if (err.status === 0) msg = 'Server bilan ulanib bo\'lmadi';
        else msg = err.message || 'Login xatosi';
        errorBox.textContent = msg;
        errorBox.classList.remove('hidden');
      } finally {
        btn.disabled = false;
        btnText.textContent = 'Kirish';
        btnSpinner.classList.add('hidden');
      }
    });

    document.getElementById('logout-btn').addEventListener('click', () => this.logout());

    // Wire change-password (logged in) and forgot-password (login screen) flows
    this.bindChangePassword();
    this.bindResetPassword();
    this.bindBackToSite();

    document.getElementById('user-menu-btn').addEventListener('click', (e) => {
      e.stopPropagation();
      document.getElementById('user-menu').classList.toggle('hidden');
    });
    document.addEventListener('click', () => {
      document.getElementById('user-menu').classList.add('hidden');
    });

    window.addEventListener('auth:expired', () => {
      this.closeAllOverlays();
      UI.toast('Sessiya tugadi. Qayta kiring.', 'error');
      this.showLogin();
    });
  },

  logout() {
    Api.clearSession();
    this.closeAllOverlays();
    this.showLogin();
    UI.toast('Tizimdan chiqildi', 'info', 2000);
  },

  // ─────────── Back to site button (reads URL from /api/v1/config/public) ───────────
  async bindBackToSite() {
    const btn = document.getElementById('back-to-site-btn');
    if (!btn) return;
    try {
      const cfg = await Api.getPublicConfig();
      if (cfg && cfg.frontendUrl) btn.setAttribute('href', cfg.frontendUrl);
    } catch { /* keep default '/' */ }
  },

  // ─────────── Change password (logged in) ───────────
  bindChangePassword() {
    const open = document.getElementById('change-password-btn');
    const modal = document.getElementById('change-password-modal');
    const form = document.getElementById('change-password-form');
    const errorBox = document.getElementById('cp-error');
    const saveBtn = document.getElementById('cp-save-btn');
    const txt = saveBtn.querySelector('.cp-text');
    const spinner = saveBtn.querySelector('.cp-spinner');

    open.addEventListener('click', () => {
      form.reset();
      errorBox.classList.add('hidden');
      modal.classList.remove('hidden');
      document.getElementById('user-menu').classList.add('hidden');
    });
    modal.querySelectorAll('.cp-close').forEach(b => b.addEventListener('click', () => modal.classList.add('hidden')));
    modal.addEventListener('click', (e) => { if (e.target === modal) modal.classList.add('hidden'); });

    saveBtn.addEventListener('click', async () => {
      errorBox.classList.add('hidden');
      const fd = new FormData(form);
      const cur = (fd.get('currentPassword') || '').toString();
      const ne  = (fd.get('newPassword') || '').toString();
      const cf  = (fd.get('confirmPassword') || '').toString();

      if (!cur || !ne || !cf) {
        errorBox.textContent = 'Hamma maydonlarni to\'ldiring.';
        errorBox.classList.remove('hidden'); return;
      }
      if (ne.length < 6) {
        errorBox.textContent = 'Yangi parol kamida 6 ta belgi bo\'lishi kerak.';
        errorBox.classList.remove('hidden'); return;
      }
      if (ne !== cf) {
        errorBox.textContent = 'Yangi parollar mos kelmadi.';
        errorBox.classList.remove('hidden'); return;
      }

      saveBtn.disabled = true; txt.textContent = 'Saqlanmoqda...'; spinner.classList.remove('hidden');
      try {
        await Api.changePassword(cur, ne);
        modal.classList.add('hidden');
        UI.toast('Parol muvaffaqiyatli o\'zgartirildi. Qayta kiring.', 'success', 4000);
        // Force re-login because old token might still be valid but stale
        setTimeout(() => this.logout(), 1500);
      } catch (err) {
        errorBox.textContent = err.message || 'Xato yuz berdi';
        errorBox.classList.remove('hidden');
      } finally {
        saveBtn.disabled = false; txt.textContent = 'O\'zgartirish'; spinner.classList.add('hidden');
      }
    });
  },

  // ─────────── Reset password (forgot password — uses recovery key) ───────────
  bindResetPassword() {
    const open = document.getElementById('forgot-password-btn');
    const modal = document.getElementById('reset-password-modal');
    const form = document.getElementById('reset-password-form');
    const errorBox = document.getElementById('rp-error');
    const saveBtn = document.getElementById('rp-save-btn');
    const txt = saveBtn.querySelector('.rp-text');
    const spinner = saveBtn.querySelector('.rp-spinner');

    open.addEventListener('click', () => {
      form.reset();
      errorBox.classList.add('hidden');
      modal.classList.remove('hidden');
    });
    modal.querySelectorAll('.rp-close').forEach(b => b.addEventListener('click', () => modal.classList.add('hidden')));
    modal.addEventListener('click', (e) => { if (e.target === modal) modal.classList.add('hidden'); });

    saveBtn.addEventListener('click', async () => {
      errorBox.classList.add('hidden');
      const fd = new FormData(form);
      const key = (fd.get('recoveryKey') || '').toString().trim();
      const ne  = (fd.get('newPassword') || '').toString();
      const cf  = (fd.get('confirmPassword') || '').toString();

      if (!key || !ne || !cf) {
        errorBox.textContent = 'Hamma maydonlarni to\'ldiring.';
        errorBox.classList.remove('hidden'); return;
      }
      if (ne.length < 6) {
        errorBox.textContent = 'Yangi parol kamida 6 ta belgi bo\'lishi kerak.';
        errorBox.classList.remove('hidden'); return;
      }
      if (ne !== cf) {
        errorBox.textContent = 'Yangi parollar mos kelmadi.';
        errorBox.classList.remove('hidden'); return;
      }

      saveBtn.disabled = true; txt.textContent = 'Tiklanmoqda...'; spinner.classList.remove('hidden');
      try {
        await Api.resetPassword(key, ne);
        modal.classList.add('hidden');
        UI.toast('Parol muvaffaqiyatli tiklandi! Endi yangi parol bilan kiring.', 'success', 5000);
      } catch (err) {
        let msg;
        if (err.status === 401) msg = 'Tiklash kaliti noto\'g\'ri. Server administratoridan to\'g\'ri kalitni oling.';
        else msg = err.message || 'Xato yuz berdi';
        errorBox.textContent = msg;
        errorBox.classList.remove('hidden');
      } finally {
        saveBtn.disabled = false; txt.textContent = 'Tiklash'; spinner.classList.add('hidden');
      }
    });
  }
};
