// Bootstrap + tab routing.
const App = {
  currentTab: 'sections',

  init() {
    Auth.init();
    Sections.init();
    Contents.init();
    Media.init();
    LabHeads.init();
    if (typeof SectionHeads !== 'undefined') SectionHeads.init();
    if (typeof Bot !== 'undefined') Bot.init();
    this.bindThemeToggle();

    document.querySelectorAll('[data-tab]').forEach(b =>
      b.addEventListener('click', () => this.switchTab(b.dataset.tab)));

    document.getElementById('sidebar-toggle')?.addEventListener('click', () => {
      document.getElementById('sidebar').classList.toggle('hidden');
      document.getElementById('sidebar').classList.toggle('flex');
    });

    document.addEventListener('keydown', (e) => {
      if (e.key !== 'Escape') return;
      const order = ['media-picker-modal', 'change-password-modal', 'reset-password-modal', 'content-modal', 'section-modal', 'lab-head-modal', 'section-head-modal'];
      for (const id of order) {
        const m = document.getElementById(id);
        if (m && !m.classList.contains('hidden')) {
          if (id === 'content-modal') Contents.closeModal();
          else if (id === 'section-modal') Sections.closeModal();
          else if (id === 'lab-head-modal') LabHeads.closeModal();
          else if (id === 'section-head-modal' && typeof SectionHeads !== 'undefined') SectionHeads.closeModal();
          else m.classList.add('hidden');
          return;
        }
      }
    });

    if (Api.isAuthenticated()) {
      Auth.showDashboard();
      this.boot();
    } else {
      Auth.showLogin();
    }
  },

  boot() {
    this.switchTab('sections');
  },

  bindThemeToggle() {
    const btn = document.getElementById('theme-btn');
    if (!btn) return;
    const sun = btn.querySelector('.theme-icon-sun');
    const moon = btn.querySelector('.theme-icon-moon');

    const apply = (dark) => {
      document.documentElement.classList.toggle('dark', dark);
      sun.classList.toggle('hidden', !dark);
      moon.classList.toggle('hidden', dark);
    };
    apply(document.documentElement.classList.contains('dark'));

    btn.addEventListener('click', () => {
      const dark = !document.documentElement.classList.contains('dark');
      localStorage.setItem('admin_theme', dark ? 'dark' : 'light');
      apply(dark);
    });
  },

  switchTab(tab) {
    this.currentTab = tab;
    document.querySelectorAll('[data-tab]').forEach(b => b.classList.toggle('active', b.dataset.tab === tab));
    document.querySelectorAll('.tab-panel').forEach(p => p.classList.add('hidden'));
    document.getElementById(`tab-${tab}`).classList.remove('hidden');

    if (tab === 'sections') Sections.load();
    if (tab === 'contents') Contents.load();
    if (tab === 'media') Media.load();
    if (tab === 'lab-heads') LabHeads.load();
    if (tab === 'section-heads' && typeof SectionHeads !== 'undefined') SectionHeads.load();
    if (tab === 'bot' && typeof Bot !== 'undefined') Bot.load();
  }
};

document.addEventListener('DOMContentLoaded', () => App.init());
