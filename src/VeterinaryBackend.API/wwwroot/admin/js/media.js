// Media tab — list, upload (button + drag/drop), delete.
const Media = {
  state: { page: 1, pageSize: 24, totalPages: 1, totalCount: 0 },

  init() {
    // Media tab UI was removed — guard against missing DOM nodes so admin still boots.
    const upload = document.getElementById('media-upload-input');
    if (!upload) return;

    upload.addEventListener('change', (e) => this.handleFiles(e.target.files));
    document.getElementById('media-prev')?.addEventListener('click', () => this.prev());
    document.getElementById('media-next')?.addEventListener('click', () => this.next());

    const dz = document.getElementById('media-dropzone');
    if (!dz) return;
    dz.addEventListener('click', () => upload.click());
    ['dragenter', 'dragover'].forEach(ev => dz.addEventListener(ev, (e) => {
      e.preventDefault();
      dz.classList.add('dropzone-active');
    }));
    ['dragleave', 'drop'].forEach(ev => dz.addEventListener(ev, (e) => {
      e.preventDefault();
      dz.classList.remove('dropzone-active');
    }));
    dz.addEventListener('drop', (e) => {
      e.preventDefault();
      this.handleFiles(e.dataTransfer.files);
    });
  },

  async load() {
    const grid = document.getElementById('media-grid');
    if (!grid) return; // Media tab UI removed
    grid.innerHTML = `<div class="col-span-full text-center text-slate-400 py-12">
      <svg class="inline animate-spin h-5 w-5 mr-2 text-slate-400" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24">
        <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"></circle>
        <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8v4a4 4 0 00-4 4H4z"></path>
      </svg>Yuklanmoqda...</div>`;

    try {
      const data = await Api.listMedia({ page: this.state.page, pageSize: this.state.pageSize });
      this.state.totalPages = data.totalPages;
      this.state.totalCount = data.totalCount;
      this.renderGrid(data.items);
      this.renderPagination(data);
      this.updateStats(data);
    } catch (err) {
      grid.innerHTML = `<div class="col-span-full text-center text-rose-500 py-12">${escapeHtml(err.message)}</div>`;
    }
  },

  updateStats(data) {
    const setStat = (key, val) => {
      const el = document.querySelector(`[data-stat="${key}"]`);
      if (el) el.textContent = val;
    };
    setStat('media-total', data.totalCount);
    const items = data.items || [];
    setStat('media-images', items.filter(m => UI.isImage(m.contentType)).length);
    setStat('media-other', items.filter(m => !UI.isImage(m.contentType)).length);
    const totalBytes = items.reduce((s, m) => s + (m.sizeBytes || 0), 0);
    setStat('media-size', UI.formatBytes(totalBytes));
  },

  renderGrid(items) {
    const grid = document.getElementById('media-grid');
    if (!items.length) {
      grid.innerHTML = `<div class="col-span-full text-center py-12">
        <div class="max-w-sm mx-auto">
          <div class="w-14 h-14 mx-auto rounded-2xl bg-brand-100 dark:bg-brand-900/40 flex items-center justify-center mb-4">
            <svg xmlns="http://www.w3.org/2000/svg" class="w-7 h-7 text-brand-600 dark:text-brand-400" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="1.5"><path stroke-linecap="round" stroke-linejoin="round" d="M4 16l4.586-4.586a2 2 0 012.828 0L16 16m-2-2l1.586-1.586a2 2 0 012.828 0L20 14m-6-6h.01M6 20h12a2 2 0 002-2V6a2 2 0 00-2-2H6a2 2 0 00-2 2v12a2 2 0 002 2z"/></svg>
          </div>
          <h3 class="font-display text-lg font-bold text-slate-900 dark:text-white mb-1">Galereya bo'sh</h3>
          <p class="text-sm text-slate-500 dark:text-slate-400 mb-4">Yuqoridagi <strong>"Sudrab tashlang"</strong> maydoniga fayl tashlang yoki <strong>"Fayl yuklash"</strong> tugmasini bosing.</p>
          <p class="text-xs text-slate-400 dark:text-slate-500">Qabul qilinadi: rasm, PDF, Excel, JSON, XML va boshqa formatlar</p>
        </div>
      </div>`;
      return;
    }

    grid.innerHTML = items.map(m => `
      <div class="media-tile" data-id="${m.id}" data-url="${escapeHtml(m.url)}">
        <div class="aspect-square bg-slate-50 flex items-center justify-center">
          ${UI.isImage(m.contentType)
            ? `<img src="${escapeHtml(m.url)}" alt="${escapeHtml(m.originalFileName)}" class="w-full h-full object-cover" />`
            : `<div class="flex flex-col items-center">${UI.fileIconSvg(m.contentType, m.extension)}</div>`}
        </div>
        <div class="px-2.5 py-2 border-t border-slate-100">
          <div class="text-xs font-medium text-slate-700 truncate" title="${escapeHtml(m.originalFileName)}">${escapeHtml(m.originalFileName)}</div>
          <div class="text-[10px] text-slate-400 mt-0.5">${UI.formatBytes(m.sizeBytes)}</div>
        </div>
        <div class="media-overlay">
          <button class="copy-btn p-1.5 bg-white/90 hover:bg-white rounded-lg" title="URL nusxalash">
            <svg xmlns="http://www.w3.org/2000/svg" class="w-4 h-4 text-slate-700" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2"><path stroke-linecap="round" stroke-linejoin="round" d="M8 16H6a2 2 0 01-2-2V6a2 2 0 012-2h8a2 2 0 012 2v2m-6 12h8a2 2 0 002-2v-8a2 2 0 00-2-2h-8a2 2 0 00-2 2v8a2 2 0 002 2z"/></svg>
          </button>
          <a href="${escapeHtml(m.url)}" target="_blank" class="p-1.5 bg-white/90 hover:bg-white rounded-lg" title="Ochish" onclick="event.stopPropagation()">
            <svg xmlns="http://www.w3.org/2000/svg" class="w-4 h-4 text-slate-700" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2"><path stroke-linecap="round" stroke-linejoin="round" d="M10 6H6a2 2 0 00-2 2v10a2 2 0 002 2h10a2 2 0 002-2v-4M14 4h6m0 0v6m0-6L10 14"/></svg>
          </a>
          <button class="delete-btn p-1.5 bg-white/90 hover:bg-rose-50 rounded-lg" title="O'chirish">
            <svg xmlns="http://www.w3.org/2000/svg" class="w-4 h-4 text-rose-600" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2"><path stroke-linecap="round" stroke-linejoin="round" d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6M1 7h22M9 7V4a1 1 0 011-1h4a1 1 0 011 1v3"/></svg>
          </button>
        </div>
      </div>
    `).join('');

    grid.querySelectorAll('.media-tile').forEach(tile => {
      const id = parseInt(tile.dataset.id, 10);
      const url = tile.dataset.url;
      tile.querySelector('.copy-btn').addEventListener('click', (e) => {
        e.stopPropagation();
        copyToClipboard(window.location.origin + url);
      });
      tile.querySelector('.delete-btn').addEventListener('click', (e) => {
        e.stopPropagation();
        this.deleteOne(id);
      });
    });
  },

  renderPagination(data) {
    const wrap = document.getElementById('media-pagination');
    if (data.totalCount === 0) { wrap.classList.add('hidden'); return; }
    wrap.classList.remove('hidden');
    document.getElementById('media-pagination-info').textContent =
      `${data.totalCount} ta fayl • Sahifa ${data.page}/${data.totalPages}`;
    document.getElementById('media-prev').disabled = !data.hasPrevious;
    document.getElementById('media-next').disabled = !data.hasNext;
  },

  prev() { if (this.state.page > 1) { this.state.page--; this.load(); } },
  next() { if (this.state.page < this.state.totalPages) { this.state.page++; this.load(); } },

  async handleFiles(fileList) {
    if (!fileList || !fileList.length) return;
    const files = Array.from(fileList);
    const total = files.length;
    let success = 0;
    let failed = 0;

    for (let i = 0; i < files.length; i++) {
      const f = files[i];
      UI.toast(`(${i + 1}/${total}) "${f.name}" yuklanmoqda...`, 'info', 2500);
      try {
        await Api.uploadMedia(f);
        success++;
      } catch (err) {
        failed++;
        UI.toast(`"${f.name}": ${err.message}`, 'error', 5000);
      }
    }

    if (success > 0) UI.toast(`${success} ta fayl muvaffaqiyatli yuklandi`, 'success');
    document.getElementById('media-upload-input').value = '';
    this.state.page = 1;
    this.load();
  },

  async deleteOne(id) {
    const ok = await UI.confirm({
      title: 'Faylni o\'chirish',
      message: 'Bu faylni serverdan butunlay o\'chirmoqchimisiz?',
      okText: 'Ha, o\'chirish'
    });
    if (!ok) return;

    try {
      await Api.deleteMedia(id);
      UI.toast('Fayl o\'chirildi', 'success');
      this.load();
    } catch (err) {
      UI.toast(err.message, 'error');
    }
  }
};
