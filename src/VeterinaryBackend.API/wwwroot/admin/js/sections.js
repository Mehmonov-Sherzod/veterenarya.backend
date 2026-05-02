// Sections tab — list, create, edit, delete navigation sections.
const Sections = {
  cache: [],

  init() {
    document.getElementById('new-section-btn').addEventListener('click', () => this.openModal());
    document.querySelectorAll('#section-modal .modal-close').forEach(b =>
      b.addEventListener('click', () => this.closeModal()));
    document.getElementById('section-modal').addEventListener('click', (e) => {
      if (e.target.id === 'section-modal') this.closeModal();
    });
    document.getElementById('section-save-btn').addEventListener('click', () => this.save());
  },

  async load() {
    const tbody = document.getElementById('sections-tbody');
    tbody.innerHTML = `<tr><td colspan="5" class="px-4 py-12 text-center text-slate-400">
      <svg class="inline animate-spin h-5 w-5 mr-2" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24">
        <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"></circle>
        <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8v4a4 4 0 00-4 4H4z"></path>
      </svg>Yuklanmoqda...</td></tr>`;
    try {
      const list = await Api.listSections({ onlyActive: false });
      this.cache = list;
      this.renderRows(list);
      this.updateStats(list);
    } catch (err) {
      tbody.innerHTML = `<tr><td colspan="5" class="px-4 py-12 text-center text-rose-500">${escapeHtml(err.message)}</td></tr>`;
    }
  },

  updateStats(list) {
    const setStat = (key, val) => {
      const el = document.querySelector(`[data-stat="${key}"]`);
      if (el) el.textContent = val;
    };
    setStat('sections-total', list.length);
    setStat('sections-active', list.filter(s => s.isActive).length);
    setStat('sections-content-total', list.reduce((s, x) => s + (x.contentCount || 0), 0));
    setStat('sections-empty', list.filter(s => (s.contentCount || 0) === 0).length);
  },

  renderRows(items) {
    const tbody = document.getElementById('sections-tbody');
    if (!items.length) {
      tbody.innerHTML = `<tr><td colspan="5" class="px-4 py-12">
        <div class="max-w-2xl mx-auto">
          <div class="text-center mb-6">
            <div class="text-4xl mb-2">👋</div>
            <h3 class="font-display text-xl font-bold text-slate-900 dark:text-white mb-1">Salom, Admin!</h3>
            <p class="text-sm text-slate-500 dark:text-slate-400">Saytga ma'lumot qo'shish uchun 3 ta oddiy qadam:</p>
          </div>

          <div class="space-y-3 mb-6">
            <div class="quickstart-step">
              <div class="quickstart-num">1</div>
              <div class="flex-1 text-left">
                <div class="font-semibold text-slate-900 dark:text-white text-sm">Bo'lim yarating</div>
                <div class="text-xs text-slate-500 dark:text-slate-400 mt-0.5">Saytdagi navigatsiya bandi (masalan: <em>Xizmatlar, Doktorlar, Galereya</em>)</div>
              </div>
            </div>
            <div class="quickstart-step">
              <div class="quickstart-num">2</div>
              <div class="flex-1 text-left">
                <div class="font-semibold text-slate-900 dark:text-white text-sm">Bo'lim ichiga blok qo'shing</div>
                <div class="text-xs text-slate-500 dark:text-slate-400 mt-0.5">Har bir blok = <strong>1 ta rasm + 3 tilda matn</strong>. Saytda navbat-navbat chiqadi.</div>
              </div>
            </div>
            <div class="quickstart-step">
              <div class="quickstart-num">3</div>
              <div class="flex-1 text-left">
                <div class="font-semibold text-slate-900 dark:text-white text-sm">Tayyor!</div>
                <div class="text-xs text-slate-500 dark:text-slate-400 mt-0.5">Saytga kiring va natijani ko'ring. Istalgan vaqtda tahrirlay olasiz.</div>
              </div>
            </div>
          </div>

          <div class="text-center">
            <button id="quickstart-cta" class="inline-flex items-center gap-2 bg-brand-600 hover:bg-brand-700 text-white px-6 py-3 rounded-xl text-sm font-medium transition shadow-lg shadow-brand-600/30">
              Birinchi bo'limni yarataylik
              <svg xmlns="http://www.w3.org/2000/svg" class="w-4 h-4" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2.5"><path stroke-linecap="round" stroke-linejoin="round" d="M17 8l4 4m0 0l-4 4m4-4H3"/></svg>
            </button>
          </div>
        </div>
      </td></tr>`;
      const cta = document.getElementById('quickstart-cta');
      if (cta) cta.addEventListener('click', () => this.openModal());
      return;
    }

    tbody.innerHTML = items.map((s, i) => `
      <tr class="fancy-row">
        <td class="px-4 py-3">
          <div class="flex items-center gap-3">
            <div class="row-avatar row-avatar-${(i % 8) + 1}">${escapeHtml((s.title || '?').charAt(0).toUpperCase())}</div>
            <div>
              <div class="font-display font-bold text-slate-900 dark:text-white">${escapeHtml(s.title)}</div>
              <div class="text-xs text-slate-500 dark:text-slate-400 font-mono mt-0.5">/${escapeHtml(s.slug)}</div>
            </div>
          </div>
        </td>
        <td class="px-4 py-3 hidden md:table-cell">
          <span class="inline-block px-2 py-0.5 bg-slate-100 dark:bg-slate-800 text-slate-700 dark:text-slate-300 text-xs font-mono rounded">${s.sortOrder}</span>
        </td>
        <td class="px-4 py-3 hidden md:table-cell">
          <span class="inline-flex items-center gap-1 px-2 py-0.5 bg-brand-50 dark:bg-brand-900/40 text-brand-700 dark:text-brand-300 text-xs font-medium rounded-full">
            ${s.contentCount} ta
          </span>
        </td>
        <td class="px-4 py-3 hidden md:table-cell">
          ${s.isActive
            ? '<span class="inline-flex items-center gap-1 px-2 py-0.5 bg-emerald-50 dark:bg-emerald-900/40 text-emerald-700 dark:text-emerald-300 text-xs font-medium rounded-full"><span class="w-1.5 h-1.5 rounded-full bg-emerald-500"></span>Faol</span>'
            : '<span class="inline-flex items-center gap-1 px-2 py-0.5 bg-slate-100 dark:bg-slate-800 text-slate-600 dark:text-slate-400 text-xs font-medium rounded-full"><span class="w-1.5 h-1.5 rounded-full bg-slate-400"></span>Yashirin</span>'}
        </td>
        <td class="px-4 py-3 text-right">
          <div class="inline-flex gap-1">
            <button data-id="${s.id}" class="edit-btn p-1.5 text-slate-500 dark:text-slate-400 hover:text-brand-600 hover:bg-brand-50 dark:hover:bg-brand-900/30 rounded-lg transition" title="Tahrirlash">
              <svg xmlns="http://www.w3.org/2000/svg" class="w-4 h-4" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2"><path stroke-linecap="round" stroke-linejoin="round" d="M15.232 5.232l3.536 3.536m-2.036-5.036a2.5 2.5 0 113.536 3.536L6.5 21.036H3v-3.572L16.732 3.732z"/></svg>
            </button>
            <button data-id="${s.id}" data-count="${s.contentCount}" class="delete-btn p-1.5 text-slate-500 dark:text-slate-400 hover:text-rose-600 hover:bg-rose-50 dark:hover:bg-rose-900/30 rounded-lg transition" title="O'chirish">
              <svg xmlns="http://www.w3.org/2000/svg" class="w-4 h-4" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2"><path stroke-linecap="round" stroke-linejoin="round" d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6M1 7h22M9 7V4a1 1 0 011-1h4a1 1 0 011 1v3"/></svg>
            </button>
          </div>
        </td>
      </tr>
    `).join('');

    tbody.querySelectorAll('.edit-btn').forEach(b =>
      b.addEventListener('click', () => this.openModal(parseInt(b.dataset.id, 10))));
    tbody.querySelectorAll('.delete-btn').forEach(b =>
      b.addEventListener('click', () => this.deleteRow(parseInt(b.dataset.id, 10), parseInt(b.dataset.count, 10))));
  },

  async openModal(id = null) {
    const form = document.getElementById('section-form');
    const saveBtn = document.getElementById('section-save-btn');
    form.reset();
    document.getElementById('section-form-error').classList.add('hidden');

    document.getElementById('section-modal-title').textContent = id ? 'Bo\'limni tahrirlash' : 'Yangi bo\'lim';
    form.querySelector('[name="id"]').value = id || '';
    // isActive is now a hidden input that always submits "on" — leave its value alone.
    form.querySelector('[name="sortOrder"]').value = 0;

    document.getElementById('section-modal').classList.remove('hidden');

    if (id) {
      saveBtn.disabled = true;
      try {
        const detail = await Api.getSectionDetail(id);
        Object.entries(detail).forEach(([k, v]) => {
          // Skip isActive — UI no longer exposes it; the form always sends "on".
          if (k === 'isActive') return;
          const el = form.querySelector(`[name="${k}"]`);
          if (!el) return;
          if (el.type === 'checkbox') el.checked = !!v;
          else el.value = v ?? '';
        });
      } catch (err) {
        UI.toast(err.message || 'Bo\'lim yuklanmadi', 'error');
        this.closeModal();
      } finally {
        saveBtn.disabled = false;
      }
    }
  },

  closeModal() {
    document.getElementById('section-modal').classList.add('hidden');
  },

  async save() {
    const form = document.getElementById('section-form');
    const errorBox = document.getElementById('section-form-error');
    const btn = document.getElementById('section-save-btn');
    const btnText = btn.querySelector('.save-btn-text');
    const btnSpinner = btn.querySelector('.save-btn-spinner');
    errorBox.classList.add('hidden');

    if (!form.checkValidity()) { form.reportValidity(); return; }

    const fd = new FormData(form);
    const id = fd.get('id');
    const payload = {
      slug: (fd.get('slug') || '').trim() || null,
      titleUz: fd.get('titleUz').trim(),
      titleRu: fd.get('titleRu').trim(),
      titleEn: fd.get('titleEn').trim(),
      sortOrder: parseInt(fd.get('sortOrder'), 10) || 0,
      isActive: fd.get('isActive') === 'on'
    };

    btn.disabled = true;
    btnText.textContent = 'Saqlanmoqda...';
    btnSpinner.classList.remove('hidden');

    try {
      if (id) {
        await Api.updateSection(parseInt(id, 10), payload);
        UI.toast('Bo\'lim yangilandi', 'success');
      } else {
        await Api.createSection(payload);
        UI.toast('Yangi bo\'lim qo\'shildi', 'success');
      }
      this.closeModal();
      this.load();
      Contents.refreshSectionDropdown?.();
    } catch (err) {
      errorBox.textContent = err.formatErrors();
      errorBox.classList.remove('hidden');
    } finally {
      btn.disabled = false;
      btnText.textContent = 'Saqlash';
      btnSpinner.classList.add('hidden');
    }
  },

  async deleteRow(id, count) {
    const message = count > 0
      ? `Bu bo'lim ichida ${count} ta kontent bor. O'chirsangiz, ular ham birgalikda o'chiriladi. Davom etamizmi?`
      : 'Ushbu bo\'limni o\'chirmoqchimisiz?';

    const ok = await UI.confirm({
      title: 'Bo\'limni o\'chirish',
      message,
      okText: 'Ha, o\'chirish'
    });
    if (!ok) return;

    try {
      await Api.deleteSection(id);
      UI.toast('Bo\'lim o\'chirildi', 'success');
      this.load();
      Contents.refreshSectionDropdown?.();
    } catch (err) {
      UI.toast(err.message, 'error');
    }
  }
};
