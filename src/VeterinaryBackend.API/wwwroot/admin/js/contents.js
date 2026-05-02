// Contents tab — list, create, edit, delete.
const Contents = {
  state: { page: 1, pageSize: 10, totalPages: 1, totalCount: 0, blocks: [], blockSeq: 0, currentLang: 'Uz' },

  init() {
    document.getElementById('new-content-btn').addEventListener('click', () => this.openModal());
    document.getElementById('contents-prev').addEventListener('click', () => this.prev());
    document.getElementById('contents-next').addEventListener('click', () => this.next());

    // Modal
    document.querySelectorAll('#content-modal .modal-close').forEach(b =>
      b.addEventListener('click', () => this.closeModal()));
    document.getElementById('content-modal').addEventListener('click', (e) => {
      if (e.target.id === 'content-modal') this.closeModal();
    });

    // Language tabs
    document.querySelectorAll('#content-modal .lang-tab').forEach(tab => {
      tab.addEventListener('click', () => this.switchLang(tab.dataset.lang));
    });

    // Save
    document.getElementById('content-save-btn').addEventListener('click', () => this.save());

    // Cover image picker has been removed from the content form — cover is auto-derived
    // from the first image block instead.

    // "Bo'limlar tabida yarating" link inside content modal
    document.getElementById('content-jump-to-sections').addEventListener('click', () => {
      this.closeModal();
      App.switchTab('sections');
    });

    // Lang tab "filled" indicator — listen to all title/description inputs
    document.querySelectorAll('#content-form input[name^="title"], #content-form textarea[name^="description"]').forEach(el => {
      el.addEventListener('input', () => this.updateLangFillStatus());
    });

    // Multi-block editor: add buttons
    const addTextBtn = document.getElementById('add-text-block-btn');
    if (addTextBtn) addTextBtn.addEventListener('click', () => this.addBlock('text'));
    const addImgBtn = document.getElementById('add-image-block-btn');
    if (addImgBtn) addImgBtn.addEventListener('click', () => this.addBlock('image'));
  },

  updateLangFillStatus() {
    const form = document.getElementById('content-form');
    let filled = 0;
    ['Uz', 'Ru', 'En'].forEach(lang => {
      const title = (form.querySelector(`[name="title${lang}"]`)?.value || '').trim();
      const langKey = lang.toLowerCase();
      const hasContent = this.state.blocks.some(b => b.type === 'image'
        ? !!b.url
        : !!(b[langKey] || '').trim());
      const isFilled = title.length > 0 || hasContent;
      const dot = document.querySelector(`#content-modal .lang-tab-dot[data-dot="${lang}"]`);
      if (dot) dot.classList.toggle('hidden', !isFilled);
      if (isFilled) filled++;
    });
    const status = document.getElementById('lang-fill-status');
    if (status) {
      status.textContent = `${filled}/3 til to'ldirilgan (faqat UZ majburiy)`;
      status.className = filled >= 1
        ? 'text-xs text-emerald-600 dark:text-emerald-400 font-medium'
        : 'text-xs text-slate-500 dark:text-slate-400';
    }
  },

  // ─────────── Multi-block body editor ───────────
  addBlock(type, data = {}) {
    const block = type === 'image'
      ? { id: ++this.state.blockSeq, type: 'image', url: data.url || '' }
      : { id: ++this.state.blockSeq, type: 'text', uz: data.uz || '', ru: data.ru || '', en: data.en || '' };
    this.state.blocks.push(block);
    this.renderBlocks();
    this.updateLangFillStatus();
  },

  removeBlock(id) {
    this.state.blocks = this.state.blocks.filter(b => b.id !== id);
    this.renderBlocks();
    this.updateLangFillStatus();
  },

  moveBlock(id, dir) {
    const idx = this.state.blocks.findIndex(b => b.id === id);
    if (idx < 0) return;
    const target = idx + dir;
    if (target < 0 || target >= this.state.blocks.length) return;
    const [b] = this.state.blocks.splice(idx, 1);
    this.state.blocks.splice(target, 0, b);
    this.renderBlocks();
  },

  updateBlockField(id, field, value) {
    const b = this.state.blocks.find(x => x.id === id);
    if (!b) return;
    b[field] = value;
    this.updateLangFillStatus();
  },

  renderBlocks() {
    const list = document.getElementById('blocks-list');
    const counter = document.getElementById('blocks-count');
    if (!list) return;

    counter.textContent = `${this.state.blocks.length} ta blok`;

    if (this.state.blocks.length === 0) {
      list.innerHTML = `<div class="text-center py-8 text-sm text-slate-400 italic border-2 border-dashed border-slate-200 dark:border-slate-700 rounded-lg">Blok yo'q. "Matn qo'shish" yoki "Rasm qo'shish" tugmasini bosing.</div>`;
      return;
    }

    list.innerHTML = this.state.blocks.map((b, idx) => this.renderBlockEditor(b, idx)).join('');

    // Wire all interactions
    list.querySelectorAll('[data-action="up"]').forEach(el =>
      el.addEventListener('click', () => this.moveBlock(parseInt(el.dataset.id, 10), -1)));
    list.querySelectorAll('[data-action="down"]').forEach(el =>
      el.addEventListener('click', () => this.moveBlock(parseInt(el.dataset.id, 10), 1)));
    list.querySelectorAll('[data-action="remove"]').forEach(el =>
      el.addEventListener('click', () => this.removeBlock(parseInt(el.dataset.id, 10))));
    list.querySelectorAll('[data-action="text-input"]').forEach(el =>
      el.addEventListener('input', () => this.updateBlockField(parseInt(el.dataset.id, 10), el.dataset.field, el.value)));
    list.querySelectorAll('[data-action="pick-image"]').forEach(el =>
      el.addEventListener('click', () => this.openMediaPickerForBlock(parseInt(el.dataset.id, 10))));
    list.querySelectorAll('[data-action="upload-image"]').forEach(el =>
      el.addEventListener('change', (e) => this.quickUploadForBlock(parseInt(el.dataset.id, 10), e)));
    list.querySelectorAll('[data-action="remove-image"]').forEach(el =>
      el.addEventListener('click', () => { this.updateBlockField(parseInt(el.dataset.id, 10), 'url', ''); this.renderBlocks(); }));
  },

  renderBlockEditor(b, idx) {
    const lang = this.state.currentLang;
    const langKey = lang.toLowerCase();
    const langLabel = { uz: "O'zbek", ru: 'Русский', en: 'English' }[langKey];
    const isFirst = idx === 0;
    const isLast = idx === this.state.blocks.length - 1;
    const moveBtns = `
      <button type="button" data-action="up" data-id="${b.id}" class="p-1.5 rounded hover:bg-slate-200 dark:hover:bg-slate-700 ${isFirst ? 'opacity-30 cursor-not-allowed' : ''}" title="Yuqoriga" ${isFirst ? 'disabled' : ''}>
        <svg xmlns="http://www.w3.org/2000/svg" class="w-4 h-4" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2.5"><path stroke-linecap="round" stroke-linejoin="round" d="M5 15l7-7 7 7"/></svg>
      </button>
      <button type="button" data-action="down" data-id="${b.id}" class="p-1.5 rounded hover:bg-slate-200 dark:hover:bg-slate-700 ${isLast ? 'opacity-30 cursor-not-allowed' : ''}" title="Pastga" ${isLast ? 'disabled' : ''}>
        <svg xmlns="http://www.w3.org/2000/svg" class="w-4 h-4" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2.5"><path stroke-linecap="round" stroke-linejoin="round" d="M19 9l-7 7-7-7"/></svg>
      </button>
      <button type="button" data-action="remove" data-id="${b.id}" class="p-1.5 rounded text-rose-600 hover:bg-rose-100 dark:hover:bg-rose-900/30" title="O'chirish">
        <svg xmlns="http://www.w3.org/2000/svg" class="w-4 h-4" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2.5"><path stroke-linecap="round" stroke-linejoin="round" d="M6 18L18 6M6 6l12 12"/></svg>
      </button>`;

    if (b.type === 'image') {
      const url = b.url || '';
      const previewSrc = url ? (url.startsWith('http') ? url : (window.AppConfig?.API_BASE_URL?.replace(/\/$/, '') || '') + url) : '';
      return `
        <div class="border border-slate-200 dark:border-slate-700 rounded-lg p-3 bg-warm-50/30 dark:bg-warm-900/10">
          <div class="flex items-center justify-between mb-2">
            <div class="flex items-center gap-2 text-sm font-semibold text-warm-700 dark:text-warm-400">
              <svg xmlns="http://www.w3.org/2000/svg" class="w-4 h-4" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2"><path stroke-linecap="round" stroke-linejoin="round" d="M4 16l4.586-4.586a2 2 0 012.828 0L16 16m-2-2l1.586-1.586a2 2 0 012.828 0L20 14m-6-6h.01M6 20h12a2 2 0 002-2V6a2 2 0 00-2-2H6a2 2 0 00-2 2v12a2 2 0 002 2z"/></svg>
              <span>Rasm bloki #${idx + 1}</span>
            </div>
            <div class="flex gap-1">${moveBtns}</div>
          </div>
          ${url ? `
            <div class="flex items-start gap-3">
              <img src="${escapeHtml(previewSrc)}" alt="" class="w-32 h-24 object-cover rounded border border-slate-300 dark:border-slate-600" />
              <div class="flex flex-col gap-2 flex-1">
                <button type="button" data-action="remove-image" data-id="${b.id}" class="px-3 py-1.5 text-xs bg-rose-50 dark:bg-rose-900/30 text-rose-600 dark:text-rose-400 border border-rose-200 dark:border-rose-800 rounded hover:bg-rose-100">Rasm olib tashlash</button>
              </div>
            </div>
          ` : `
            <label class="inline-flex items-center gap-2 px-4 py-2.5 text-sm bg-brand-600 hover:bg-brand-700 text-white rounded-lg font-semibold cursor-pointer transition shadow-sm">
              <svg xmlns="http://www.w3.org/2000/svg" class="w-4 h-4" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2"><path stroke-linecap="round" stroke-linejoin="round" d="M4 16v1a3 3 0 003 3h10a3 3 0 003-3v-1m-4-8l-4-4m0 0L8 8m4-4v12"/></svg>
              <span>Kompyuterdan yuklash</span>
              <input type="file" accept="image/*" data-action="upload-image" data-id="${b.id}" class="hidden" />
            </label>
          `}
        </div>
      `;
    }

    // text block
    return `
      <div class="border border-slate-200 dark:border-slate-700 rounded-lg p-3 bg-brand-50/30 dark:bg-brand-900/10">
        <div class="flex items-center justify-between mb-2">
          <div class="flex items-center gap-2 text-sm font-semibold text-brand-700 dark:text-brand-400">
            <svg xmlns="http://www.w3.org/2000/svg" class="w-4 h-4" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2"><path stroke-linecap="round" stroke-linejoin="round" d="M4 6h16M4 12h16M4 18h7"/></svg>
            <span>Matn bloki #${idx + 1} <span class="text-xs font-normal text-slate-500 dark:text-slate-400">(${langLabel} tahrirlanmoqda)</span></span>
          </div>
          <div class="flex gap-1">${moveBtns}</div>
        </div>
        <textarea data-action="text-input" data-id="${b.id}" data-field="${langKey}" rows="4" placeholder="Matnni shu yerga yozing..." class="w-full px-3 py-2 border border-slate-300 dark:border-slate-700 dark:bg-slate-800 dark:text-white rounded text-sm font-mono focus:ring-2 focus:ring-brand-500 focus:border-brand-500 outline-none">${escapeHtml(b[langKey] || '')}</textarea>
      </div>
    `;
  },

  openMediaPickerForBlock(blockId) {
    this._pickerTargetBlockId = blockId;
    this.openMediaPicker();
  },

  async quickUploadForBlock(blockId, e) {
    const file = e.target.files?.[0];
    if (!file) return;
    try {
      const result = await Api.uploadMedia(file);
      this.updateBlockField(blockId, 'url', result.url);
      this.renderBlocks();
      UI.toast('Rasm yuklandi', 'success');
    } catch (err) {
      UI.toast(err.message || 'Rasm yuklanmadi', 'error');
    } finally {
      e.target.value = '';
    }
  },

  setImage(url) {
    // Picker is now used solely from the multi-block editor.
    if (this._pickerTargetBlockId) {
      this.updateBlockField(this._pickerTargetBlockId, 'url', url || '');
      this._pickerTargetBlockId = null;
      this.renderBlocks();
      return;
    }
    // Legacy cover field (hidden) — keep in sync just in case.
    const hidden = document.querySelector('#content-form [name="imageUrl"]');
    if (hidden) hidden.value = url || '';
  },

  showFormError(errorBox, message) {
    errorBox.textContent = message;
    errorBox.classList.remove('hidden');
    // Scroll the error into view (form has overflow-y-auto)
    errorBox.scrollIntoView({ behavior: 'smooth', block: 'center' });
  },

  async refreshSectionDropdown() {
    const select = document.querySelector('#content-form [name="sectionId"]');
    const previous = select.value;
    select.innerHTML = '<option value="">— Bo\'lim tanlang —</option>';
    try {
      const sections = await Api.listSections({ onlyActive: false });
      sections.forEach(s => {
        const opt = document.createElement('option');
        opt.value = s.id;
        opt.textContent = `${s.title}${s.isActive ? '' : ' (yashirin)'}`;
        select.appendChild(opt);
      });
      if (previous) select.value = previous;
    } catch (err) {
      console.warn('Section dropdown load failed', err);
    }
  },

  async load() {
    const tbody = document.getElementById('contents-tbody');
    tbody.innerHTML = `<tr><td colspan="6" class="px-4 py-12 text-center text-slate-400">
      <svg class="inline animate-spin h-5 w-5 mr-2 text-slate-400" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24">
        <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"></circle>
        <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8v4a4 4 0 00-4 4H4z"></path>
      </svg>Yuklanmoqda...</td></tr>`;

    try {
      const data = await Api.listContents({ page: this.state.page, pageSize: this.state.pageSize, onlyActive: false });
      this.state.totalPages = data.totalPages;
      this.state.totalCount = data.totalCount;
      this.renderRows(data.items);
      this.renderPagination(data);
      this.updateStats(data);
    } catch (err) {
      tbody.innerHTML = `<tr><td colspan="6" class="px-4 py-12 text-center text-rose-500">${escapeHtml(err.message)}</td></tr>`;
    }
  },

  updateStats(data) {
    const setStat = (key, val) => {
      const el = document.querySelector(`[data-stat="${key}"]`);
      if (el) el.textContent = val;
    };
    setStat('contents-total', data.totalCount);
    setStat('contents-active', (data.items || []).filter(c => c.isActive).length);
    const today = new Date().toDateString();
    setStat('contents-recent', (data.items || []).filter(c => new Date(c.createdAt).toDateString() === today).length);
  },

  async renderRows(items) {
    const tbody = document.getElementById('contents-tbody');
    if (!items.length) {
      // Check if there are any sections at all — if not, guide user to create one first
      let sectionsExist = true;
      try {
        const secs = await Api.listSections({ onlyActive: false });
        sectionsExist = secs.length > 0;
      } catch { /* ignore */ }

      if (!sectionsExist) {
        tbody.innerHTML = `<tr><td colspan="6" class="px-4 py-12">
          <div class="max-w-md mx-auto text-center">
            <div class="w-14 h-14 mx-auto rounded-2xl bg-warm-100 dark:bg-warm-700/30 flex items-center justify-center mb-4">
              <svg xmlns="http://www.w3.org/2000/svg" class="w-7 h-7 text-warm-600 dark:text-warm-400" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2"><path stroke-linecap="round" stroke-linejoin="round" d="M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-3L13.732 4c-.77-1.333-2.694-1.333-3.464 0L3.34 16c-.77 1.333.192 3 1.732 3z"/></svg>
            </div>
            <h3 class="font-display text-lg font-bold text-slate-900 dark:text-white mb-1">Avval bo'lim yarating</h3>
            <p class="text-sm text-slate-500 dark:text-slate-400 mb-4">Kontent bloki har doim biror bo'limga tegishli bo'ladi. Avval <strong>1-tab — Bo'limlar</strong>'ga o'tib, kamida bitta bo'lim yarating.</p>
            <button id="goto-sections-btn" class="inline-flex items-center gap-2 bg-brand-600 hover:bg-brand-700 text-white px-5 py-2.5 rounded-xl text-sm font-medium transition shadow-md shadow-brand-600/30">
              Bo'limlar tabiga o'tish
              <svg xmlns="http://www.w3.org/2000/svg" class="w-4 h-4" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2.5"><path stroke-linecap="round" stroke-linejoin="round" d="M17 8l4 4m0 0l-4 4m4-4H3"/></svg>
            </button>
          </div>
        </td></tr>`;
        const btn = document.getElementById('goto-sections-btn');
        if (btn) btn.addEventListener('click', () => App.switchTab('sections'));
      } else {
        tbody.innerHTML = `<tr><td colspan="6" class="px-4 py-12">
          <div class="max-w-md mx-auto text-center">
            <div class="w-14 h-14 mx-auto rounded-2xl bg-brand-100 dark:bg-brand-900/40 flex items-center justify-center mb-4">
              <svg xmlns="http://www.w3.org/2000/svg" class="w-7 h-7 text-brand-600 dark:text-brand-400" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="1.5"><path stroke-linecap="round" stroke-linejoin="round" d="M20 13V6a2 2 0 00-2-2H6a2 2 0 00-2 2v7m16 0v5a2 2 0 01-2 2H6a2 2 0 01-2-2v-5m16 0h-2.586a1 1 0 00-.707.293l-2.414 2.414a1 1 0 01-.707.293h-3.172a1 1 0 01-.707-.293l-2.414-2.414A1 1 0 006.586 13H4"/></svg>
            </div>
            <h3 class="font-display text-lg font-bold text-slate-900 dark:text-white mb-1">Hali blok yo'q</h3>
            <p class="text-sm text-slate-500 dark:text-slate-400 mb-4">Bo'limlar tayyor — endi ulardan biriga <strong>rasm + matn</strong> blokini qo'shing.</p>
            <button id="empty-new-btn" class="inline-flex items-center gap-2 bg-brand-600 hover:bg-brand-700 text-white px-5 py-2.5 rounded-xl text-sm font-medium transition shadow-md shadow-brand-600/30">
              <svg xmlns="http://www.w3.org/2000/svg" class="w-4 h-4" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2.5"><path stroke-linecap="round" stroke-linejoin="round" d="M12 4v16m8-8H4"/></svg>
              Birinchi blokni qo'shish
            </button>
          </div>
        </td></tr>`;
        const btn = document.getElementById('empty-new-btn');
        if (btn) btn.addEventListener('click', () => this.openModal());
      }
      return;
    }

    tbody.innerHTML = items.map(item => `
      <tr class="hover:bg-slate-50 dark:hover:bg-slate-800/40 transition">
        <td class="px-4 py-3">
          <img src="${escapeHtml(item.imageUrl)}" alt=""
               class="w-12 h-12 rounded-lg object-cover bg-slate-100 dark:bg-slate-800 border border-slate-200 dark:border-slate-700"
               onerror="this.style.display='none'; this.nextElementSibling.style.display='flex';" />
          <div class="hidden w-12 h-12 rounded-lg bg-slate-100 dark:bg-slate-800 border border-slate-200 dark:border-slate-700 items-center justify-center">
            <svg xmlns="http://www.w3.org/2000/svg" class="w-5 h-5 text-slate-400" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2"><path stroke-linecap="round" stroke-linejoin="round" d="M4 16l4.586-4.586a2 2 0 012.828 0L16 16m-2-2l1.586-1.586a2 2 0 012.828 0L20 14m-6-6h.01"/></svg>
          </div>
        </td>
        <td class="px-4 py-3">
          <div class="font-medium text-slate-900 dark:text-white truncate max-w-xs">${escapeHtml(item.title)}</div>
          <div class="text-xs text-slate-500 dark:text-slate-400 truncate-2 max-w-md mt-0.5">${escapeHtml(stripHtml(item.description).slice(0, 120))}</div>
          ${item.sectionTitle
            ? `<span class="inline-block mt-1 px-2 py-0.5 bg-brand-50 dark:bg-brand-900/40 text-brand-700 dark:text-brand-300 text-[10px] font-medium rounded">${escapeHtml(item.sectionTitle)}</span>`
            : '<span class="inline-block mt-1 px-2 py-0.5 bg-amber-50 dark:bg-amber-900/40 text-amber-700 dark:text-amber-300 text-[10px] font-medium rounded">Bo\'limsiz</span>'}
        </td>
        <td class="px-4 py-3 hidden md:table-cell">
          <span class="inline-block px-2 py-0.5 bg-slate-100 dark:bg-slate-800 text-slate-700 dark:text-slate-300 text-xs font-mono rounded">${item.sortOrder}</span>
        </td>
        <td class="px-4 py-3 hidden md:table-cell">
          ${item.isActive
            ? '<span class="inline-flex items-center gap-1 px-2 py-0.5 bg-emerald-50 dark:bg-emerald-900/40 text-emerald-700 dark:text-emerald-300 text-xs font-medium rounded-full"><span class="w-1.5 h-1.5 rounded-full bg-emerald-500"></span>Faol</span>'
            : '<span class="inline-flex items-center gap-1 px-2 py-0.5 bg-slate-100 dark:bg-slate-800 text-slate-600 dark:text-slate-400 text-xs font-medium rounded-full"><span class="w-1.5 h-1.5 rounded-full bg-slate-400"></span>Yashirin</span>'}
        </td>
        <td class="px-4 py-3 hidden lg:table-cell text-sm text-slate-500 dark:text-slate-400">${UI.formatDate(item.createdAt)}</td>
        <td class="px-4 py-3 text-right">
          <div class="inline-flex gap-1">
            <button data-id="${item.id}" class="edit-btn p-1.5 text-slate-500 dark:text-slate-400 hover:text-brand-600 hover:bg-brand-50 dark:hover:bg-brand-900/30 rounded-lg transition" title="Tahrirlash">
              <svg xmlns="http://www.w3.org/2000/svg" class="w-4 h-4" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2"><path stroke-linecap="round" stroke-linejoin="round" d="M15.232 5.232l3.536 3.536m-2.036-5.036a2.5 2.5 0 113.536 3.536L6.5 21.036H3v-3.572L16.732 3.732z"/></svg>
            </button>
            <button data-id="${item.id}" class="delete-btn p-1.5 text-slate-500 dark:text-slate-400 hover:text-rose-600 hover:bg-rose-50 dark:hover:bg-rose-900/30 rounded-lg transition" title="O'chirish">
              <svg xmlns="http://www.w3.org/2000/svg" class="w-4 h-4" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2"><path stroke-linecap="round" stroke-linejoin="round" d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6M1 7h22M9 7V4a1 1 0 011-1h4a1 1 0 011 1v3"/></svg>
            </button>
          </div>
        </td>
      </tr>
    `).join('');

    tbody.querySelectorAll('.edit-btn').forEach(b =>
      b.addEventListener('click', () => this.openModal(parseInt(b.dataset.id, 10))));
    tbody.querySelectorAll('.delete-btn').forEach(b =>
      b.addEventListener('click', () => this.deleteRow(parseInt(b.dataset.id, 10))));
  },

  renderPagination(data) {
    const wrap = document.getElementById('contents-pagination');
    if (data.totalCount === 0) { wrap.classList.add('hidden'); return; }
    wrap.classList.remove('hidden');
    document.getElementById('contents-pagination-info').textContent =
      `${data.totalCount} ta yozuv • Sahifa ${data.page}/${data.totalPages}`;
    document.getElementById('contents-prev').disabled = !data.hasPrevious;
    document.getElementById('contents-next').disabled = !data.hasNext;
  },

  prev() { if (this.state.page > 1) { this.state.page--; this.load(); } },
  next() { if (this.state.page < this.state.totalPages) { this.state.page++; this.load(); } },

  async openModal(id = null) {
    const form = document.getElementById('content-form');
    const saveBtn = document.getElementById('content-save-btn');
    form.reset();
    document.getElementById('content-form-error').classList.add('hidden');
    this.state.blocks = [];
    this.state.blockSeq = 0;
    this.switchLang('Uz');
    this.updatePreview('');
    this.renderBlocks();

    document.getElementById('content-modal-title').textContent = id ? 'Kontentni tahrirlash' : 'Yangi kontent';
    form.querySelector('[name="id"]').value = id || '';

    // Refresh section dropdown each time modal opens (sections may have changed)
    await this.refreshSectionDropdown();
    this.updateLangFillStatus();

    // Show modal immediately so user sees feedback
    document.getElementById('content-modal').classList.remove('hidden');

    if (id) {
      saveBtn.disabled = true;
      try {
        const detail = await Api.getContentDetail(id);
        Object.entries(detail).forEach(([k, v]) => {
          if (k === 'descriptionUz' || k === 'descriptionRu' || k === 'descriptionEn') return;
          const radios = form.querySelectorAll(`input[type="radio"][name="${k}"]`);
          if (radios.length > 0) {
            const target = String(v ?? '');
            radios.forEach(r => { r.checked = r.value === target; });
            return;
          }
          const el = form.querySelector(`[name="${k}"]`);
          if (!el) return;
          if (el.type === 'checkbox') el.checked = !!v;
          else el.value = v ?? '';
        });
        this.state.blocks = this.deserializeBlocks(detail.descriptionUz, detail.descriptionRu, detail.descriptionEn);
        this.state.blockSeq = this.state.blocks.length;
        this.state.blocks.forEach((b, i) => { b.id = i + 1; });
        this.renderBlocks();
        this.updatePreview(detail.imageUrl);
        this.updateLangFillStatus();
      } catch (err) {
        UI.toast(err.message || 'Kontent yuklanmadi', 'error');
        this.closeModal();
      } finally {
        saveBtn.disabled = false;
      }
    } else {
      form.querySelector('[name="isActive"]').checked = true;
      form.querySelector('[name="sortOrder"]').value = 0;
      const topRadio = form.querySelector('input[name="imagePosition"][value="top"]');
      if (topRadio) topRadio.checked = true;
    }
  },

  closeModal() {
    document.getElementById('content-modal').classList.add('hidden');
  },

  switchLang(lang) {
    this.state.currentLang = lang;
    document.querySelectorAll('#content-modal .lang-tab').forEach(t => t.classList.toggle('active', t.dataset.lang === lang));
    document.querySelectorAll('#content-modal .lang-pane').forEach(p => p.classList.toggle('hidden', p.dataset.lang !== lang));
    this.renderBlocks();
  },

  // Blocks JSON marker so we can detect new format vs legacy plain text
  BLOCKS_MARKER: '__VS_BLOCKS_V1__',

  // Build the description string for one language out of state.blocks.
  serializeBlocks(langKey) {
    const arr = this.state.blocks.map(b => b.type === 'image'
      ? { t: 'image', u: b.url || '' }
      : { t: 'text', v: b[langKey] || '' });
    return this.BLOCKS_MARKER + JSON.stringify(arr);
  },

  // Parse 3 description fields into a unified blocks array.
  // If descriptions are plain text (legacy), wrap each as a single text block.
  deserializeBlocks(uz, ru, en) {
    const parsed = { uz: this.parseDescription(uz), ru: this.parseDescription(ru), en: this.parseDescription(en) };
    // If any lang has structured blocks, use UZ as the canonical structure.
    if (parsed.uz.isBlocks || parsed.ru.isBlocks || parsed.en.isBlocks) {
      const ref = (parsed.uz.isBlocks ? parsed.uz.blocks : (parsed.ru.isBlocks ? parsed.ru.blocks : parsed.en.blocks));
      return ref.map((b, i) => {
        if (b.t === 'image') return { type: 'image', url: b.u || '' };
        return {
          type: 'text',
          uz: parsed.uz.isBlocks ? (parsed.uz.blocks[i]?.v || '') : '',
          ru: parsed.ru.isBlocks ? (parsed.ru.blocks[i]?.v || '') : '',
          en: parsed.en.isBlocks ? (parsed.en.blocks[i]?.v || '') : ''
        };
      });
    }
    // Legacy plain text → one text block per existing language value.
    const uzText = (uz || '').trim();
    const ruText = (ru || '').trim();
    const enText = (en || '').trim();
    if (uzText || ruText || enText) {
      return [{ type: 'text', uz: uzText, ru: ruText, en: enText }];
    }
    return [];
  },

  parseDescription(s) {
    const text = (s || '').trim();
    if (!text.startsWith(this.BLOCKS_MARKER)) return { isBlocks: false, blocks: [] };
    try {
      const arr = JSON.parse(text.slice(this.BLOCKS_MARKER.length));
      if (!Array.isArray(arr)) return { isBlocks: false, blocks: [] };
      return { isBlocks: true, blocks: arr };
    } catch {
      return { isBlocks: false, blocks: [] };
    }
  },

  async save() {
    const form = document.getElementById('content-form');
    const errorBox = document.getElementById('content-form-error');
    const btn = document.getElementById('content-save-btn');
    const btnText = btn.querySelector('.save-btn-text');
    const btnSpinner = btn.querySelector('.save-btn-spinner');

    errorBox.classList.add('hidden');

    const fd = new FormData(form);
    const sectionIdRaw = (fd.get('sectionId') || '').trim();

    // 1) Section must be selected
    if (!sectionIdRaw) {
      this.showFormError(errorBox, '📂 Iltimos, bo\'lim tanlang.');
      return;
    }

    // 2) At least one block required
    if (this.state.blocks.length === 0) {
      this.showFormError(errorBox, '🧩 Kamida bitta matn yoki rasm bloki qo\'shing.');
      return;
    }

    // Cover image is auto-derived from the first image block; falls back to empty.
    const firstImage = this.state.blocks.find(b => b.type === 'image' && b.url);
    const imageUrl = firstImage ? firstImage.url : '';

    // 4) Every image block must have a URL
    const emptyImg = this.state.blocks.find(b => b.type === 'image' && !b.url);
    if (emptyImg) {
      this.showFormError(errorBox, '🖼️ Rasm bloklaridan biri bo\'sh — rasm tanlang yoki blokni o\'chiring.');
      return;
    }

    // 5) Only UZ is required (RU/EN are optional translations).
    const titleUz = (fd.get('titleUz') || '').trim();
    if (!titleUz) {
      this.switchLang('Uz');
      this.showFormError(errorBox, "🌐 O'zbek tilidagi sarlavhani to'ldiring (majburiy).");
      return;
    }
    const hasUzText = this.state.blocks.some(b => b.type === 'text' && (b.uz || '').trim());
    const hasAnyImage = this.state.blocks.some(b => b.type === 'image' && b.url);
    if (!hasUzText && !hasAnyImage) {
      this.switchLang('Uz');
      this.showFormError(errorBox, "🌐 Kamida bitta o'zbek tilidagi matn yoki rasm bloki bo'lishi shart.");
      return;
    }
    // If a text block exists, its UZ value must not be empty (RU/EN can be blank → UZ is used).
    const emptyUzText = this.state.blocks.some(b => b.type === 'text' && !(b.uz || '').trim());
    if (emptyUzText) {
      this.switchLang('Uz');
      this.showFormError(errorBox, "🌐 Har bir matn bloki o'zbek tilida to'ldirilishi shart.");
      return;
    }

    // 6) Final native validation pass
    if (!form.checkValidity()) {
      form.reportValidity();
      return;
    }

    const id = fd.get('id');
    const payload = {
      sectionId: parseInt(sectionIdRaw, 10),
      titleUz: fd.get('titleUz').trim(),
      titleRu: fd.get('titleRu').trim(),
      titleEn: fd.get('titleEn').trim(),
      descriptionUz: this.serializeBlocks('uz'),
      descriptionRu: this.serializeBlocks('ru'),
      descriptionEn: this.serializeBlocks('en'),
      imageUrl,
      imagePosition: 'top',
      sortOrder: parseInt(fd.get('sortOrder'), 10) || 0,
      isActive: fd.get('isActive') === 'on'
    };

    btn.disabled = true;
    btnText.textContent = 'Saqlanmoqda...';
    btnSpinner.classList.remove('hidden');

    try {
      if (id) {
        await Api.updateContent(parseInt(id, 10), payload);
        UI.toast('Kontent yangilandi', 'success');
      } else {
        await Api.createContent(payload);
        UI.toast('Yangi kontent qo\'shildi', 'success');
      }
      this.closeModal();
      this.load();
    } catch (err) {
      errorBox.textContent = err.formatErrors();
      errorBox.classList.remove('hidden');
    } finally {
      btn.disabled = false;
      btnText.textContent = 'Saqlash';
      btnSpinner.classList.add('hidden');
    }
  },

  async deleteRow(id) {
    const ok = await UI.confirm({
      title: 'Kontentni o\'chirish',
      message: 'Ushbu yozuvni butunlay o\'chirmoqchimisiz? Bu amalni qaytarib bo\'lmaydi.',
      okText: 'Ha, o\'chirish'
    });
    if (!ok) return;

    try {
      await Api.deleteContent(id);
      UI.toast('Kontent o\'chirildi', 'success');
      this.load();
    } catch (err) {
      UI.toast(err.message, 'error');
    }
  },

  updatePreview(_url) {
    // Cover preview UI was removed; no-op kept for callsite compatibility.
  },

  async openMediaPicker() {
    const modal = document.getElementById('media-picker-modal');
    const grid = document.getElementById('media-picker-grid');
    grid.innerHTML = '<div class="col-span-full text-center text-slate-400 py-12">Yuklanmoqda...</div>';
    modal.classList.remove('hidden');

    const close = () => modal.classList.add('hidden');
    modal.querySelectorAll('.picker-close').forEach(b => b.onclick = close);
    modal.onclick = (e) => { if (e.target === modal) close(); };

    try {
      const data = await Api.listMedia({ page: 1, pageSize: 60 });
      if (!data.items.length) {
        grid.innerHTML = '<div class="col-span-full text-center text-slate-400 py-12">Hali yuklangan fayl yo\'q</div>';
        return;
      }
      grid.innerHTML = data.items.map(m => `
        <button type="button" class="picker-item media-tile aspect-square flex items-center justify-center" data-url="${escapeHtml(m.url)}">
          ${UI.isImage(m.contentType)
            ? `<img src="${escapeHtml(m.url)}" alt="${escapeHtml(m.originalFileName)}" class="w-full h-full object-cover" />`
            : `<div class="flex flex-col items-center">${UI.fileIconSvg(m.contentType, m.extension)}</div>`}
          <div class="absolute bottom-0 left-0 right-0 bg-slate-900/80 text-white text-[10px] px-2 py-1 truncate">${escapeHtml(m.originalFileName)}</div>
        </button>
      `).join('');

      grid.querySelectorAll('.picker-item').forEach(b => {
        b.addEventListener('click', () => {
          this.setImage(b.dataset.url);
          close();
        });
      });
    } catch (err) {
      grid.innerHTML = `<div class="col-span-full text-center text-rose-500 py-12">${escapeHtml(err.message)}</div>`;
    }
  },

  async quickUpload(e) {
    const file = e.target.files?.[0];
    e.target.value = '';
    if (!file) return;
    UI.toast(`"${file.name}" yuklanmoqda...`, 'info', 2000);
    try {
      const dto = await Api.uploadMedia(file);
      this.setImage(dto.url);
      UI.toast('Fayl yuklandi', 'success');
    } catch (err) {
      UI.toast(err.formatErrors(), 'error', 5000);
    }
  }
};

function stripHtml(s) {
  if (!s) return '';
  const div = document.createElement('div');
  div.innerHTML = s;
  return div.textContent || '';
}
